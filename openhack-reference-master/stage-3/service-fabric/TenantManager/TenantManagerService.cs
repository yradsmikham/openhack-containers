using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Newtonsoft.Json.Linq;
using TenantManager.Common.Actors;
using TenantManager.Common.Models;
using TenantManager.Common.Services;
using TenantManager.Common.Utilities;
using TenantManager.Helpers;
using Web;
using Web.Helpers;
using StatefulService = Microsoft.ServiceFabric.Services.Runtime.StatefulService;

namespace TenantManager
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class TenantManagerService : StatefulService, ITenantManager
    {
        private readonly IApplicationOperator applicationOperator;
        private readonly TimeSpan transactionTimeout = TimeSpan.FromSeconds(4);

        public TenantManagerService(StatefulServiceContext context, IApplicationOperator applicationOperator)
            : base(context)
        {
            this.applicationOperator = applicationOperator;
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener>()
            {
                new ServiceReplicaListener( (context) => this.CreateServiceRemotingListener(context), "RemoteListener"),
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "kestrel", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<FabricClient>(new FabricClient())
                                            .AddSingleton<IReliableStateManager>(this.StateManager)
                                            .AddSingleton<IApplicationOperator>(this.applicationOperator))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url)
                                    .Build();
                    }), "kestrel")
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {

            TimeSpan delayTime = TimeSpan.FromSeconds(5);

            try
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "Application deployment request processing started.");

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        bool performedWork = await this.TryDequeueAndProcessAsync(cancellationToken);

                        delayTime = performedWork
                            ? TimeSpan.FromSeconds(1)
                            : TimeSpan.FromSeconds(2);
                    }
                    catch (TimeoutException te)
                    {
                        // Log this and continue processing the next cluster.
                        ServiceEventSource.Current.ServiceMessage(
                            this.Context,
                            "TimeoutException while processing application deployment queue. {0}",
                            te.ToString());
                    }
                    catch (FabricTransientException fte)
                    {
                        // Log this and continue processing the next cluster.
                        ServiceEventSource.Current.ServiceMessage(
                            this.Context,
                            "FabricTransientException while processing application deployment queue. {0}",
                            fte.ToString());
                    }
                    catch (Exception ex)
                    {
                        ServiceEventSource.Current.ServiceMessage(
                            this.Context,
                            "Something went really wrong....",
                            ex.ToString());
                        throw;
                    }

                    // The queue was empty, delay for a little while before looping again
                    await Task.Delay(delayTime, cancellationToken);

                    //TODO: periodically remove completed deployments so the dictionary doesn't grow indefinitely.
                }
            }
            finally
            {
                this.applicationOperator.Dispose();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Application deployment request processing ended.");
            }

        }

      


        public async Task<bool> TryDequeueAndProcessAsync(CancellationToken cancellationToken)
        {
            var queue = await this.StateManager.GetTenantDeloymentsQueue();

            Stopwatch sw = Stopwatch.StartNew();

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<TenantDeployment> workItem = await queue.TryDequeueAsync(tx, cancellationToken);
                if (!workItem.HasValue)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "No new application deployment requests.");
                    return false;
                }
        
                TenantDeployment processedDeployment = await this.ProcessTenantDeploymentAsync(workItem.Value, cancellationToken);

                if (processedDeployment.CreationStatus == CreationStatus.Created)
                {
                    //Trigger workflow
                    ActorId actorId = new ActorId(processedDeployment.Name);
                    ITenantWorkflowActor actor = ActorProxy.Create<ITenantWorkflowActor>(actorId, TenantHelpers.GetWorkflowActorUri());
                    await actor.InternalCreated(processedDeployment.InternalMineCraftEndpoint, processedDeployment.InternalRconEndpoint);

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Application Created");
                }
                else
                {
                    // The deployment hasn't completed or failed, so queue up the next stage of deployment
                    await queue.EnqueueAsync(tx, workItem.Value, cancellationToken);

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Application enqueed again");
                }

                await tx.CommitAsync();
            }

            return true;
        }

        private async Task<TenantDeployment> ProcessTenantDeploymentAsync(TenantDeployment tenantDeploymentDeployment, CancellationToken cancellationToken)
        {
            var exists = await this.applicationOperator.ApplicationExistsAsync(tenantDeploymentDeployment.ApplicationName, cancellationToken);

            var serviceDefinition = new ServiceDefinition()
            {
                ServiceName = "MC",
                Type = "MCType",
                Version = "1.0.0"
            };

            var applicationDefinition = new ApplicationDefinition()
            {
                ApplicationName = tenantDeploymentDeployment.ApplicationName,
                Type = "mcType",
                Version = "1.0.0",
                Services = new List<ServiceDefinition>()
                {
                    serviceDefinition
                }

            };

            if (exists)
            {
                string serviceName = $"{tenantDeploymentDeployment.ApplicationName}/{serviceDefinition.ServiceName}";
              

                var serviceEndPoint = await this.applicationOperator.GetServiceEndpoint(serviceName, cancellationToken);
                if (serviceEndPoint.HasEndPoint)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "Found new internal endpoint");
                    return new TenantDeployment(CreationStatus.Created, serviceEndPoint.EndPoint["MCTypeEndpoint"], serviceEndPoint.EndPoint["MCrcon"], tenantDeploymentDeployment);
                }

                // no endpoint yet leave alone.
                return new TenantDeployment(CreationStatus.InProcess, tenantDeploymentDeployment);
            }
            else
            {
                var (mcPort, rconPort) = await GetNextAvaliablePortsAsync();

                serviceDefinition.Ports = new Dictionary<string, int>()
                {
                    {"MCPORT", mcPort},
                    {"RCONPORT", rconPort}
                };

                await this.applicationOperator.CreateApplicationAsync(applicationDefinition, cancellationToken);

                ServiceEventSource.Current.ServiceMessage(this.Context, "Application created");
                return new TenantDeployment(CreationStatus.New, tenantDeploymentDeployment);
            }
        }

        private async Task<(int mcPort, int rconPort)> GetNextAvaliablePortsAsync()
        {
            // !!! this is a really simple and niave way of getting next set of avaliable ports !!!
            // 
            // The previous solution used SF's dynamic port assignment, which works until a service gets moved (or crashed and restarts) at which time it gets a new set of ports.
            // Updating the ports in side the load balancer is an expensive operation (4+ mins) which causes long down time, plus maintaince of know if a service crashes.  
            // If instead track which ports are used inside the system, then the same port will always be used for a given service eliminating the 
            // need for load balancer updates.  
            // 
            // Pushing this out two its own service might be a good option or proxy and IP Per container and avoid tracking all together (see networking modes below)
            // 
            // A few things to consider are:
            //  Can your app support port sharing? then this might not be an issue  (ASP.NET can, but for Minecraft (or some legacy app) modifing source might not be an option)
            //  What if need to change service ports internally?
            //  What happens if a service is removed? 
            //  What happens when there is a request for two new services at same time accross partitions? 
            //      -currently the tenant manager is single partion stateful but if made made it partitioned how to track the ports across partions?
            //
            // some light reading:
            //      https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-service-manifest-resources#overriding-endpoints-in-servicemanifestxml
            //      https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reverseproxy#special-handling-for-port-sharing-services
            //      https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-services-communication-aspnetcore#weblistener-in-reliable-services
            //      https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-networking-modes

            var avaliablePortsDictionary = await this.StateManager.GetAvaliablePortsDictionary();
            int port1;
            int port2;

            using (var txt = this.StateManager.CreateTransaction())
            {
                int lastPortUsed = await avaliablePortsDictionary.GetOrAddAsync(txt, "ports", 20000);

                port1 = lastPortUsed + 1;
                port2 = port1 + 1;

                await avaliablePortsDictionary.SetAsync(txt, "ports", port2);

                await txt.CommitAsync();
            }

            return (port1, port2);
        }

        public async Task QueueTenantCreation(string tenantName)
        {
            var queue = await this.StateManager.GetTenantDeloymentsQueue();

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                var tenant = TenantDeployment.CreateNew(tenantName);

                await queue.EnqueueAsync(tx, tenant);

                await tx.CommitAsync();
            }
        }
    }

}
