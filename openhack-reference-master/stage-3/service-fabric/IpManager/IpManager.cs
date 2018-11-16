using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpManager.Helpers;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using TenantManager.Common.Actors;
using TenantManager.Common.Models;
using TenantManager.Common.Services;
using TenantManager.Common.Utilities;

namespace IpManager
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class IpManager : StatefulService, IIpManager
    {
        public IpManager(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener>()
            {
                new ServiceReplicaListener((context) => this.CreateServiceRemotingListener(context), "ServiceEndpoint")
            };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
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
                            "TimeoutException while processing ip deployment queue. {0}",
                            te.ToString());
                    }
                    catch (FabricTransientException fte)
                    {
                        // Log this and continue processing the next cluster.
                        ServiceEventSource.Current.ServiceMessage(
                            this.Context,
                            "FabricTransientException while processing ip deployment queue. {0}",
                            fte.ToString());
                    }

                    // The queue was empty, delay for a little while before looping again
                    await Task.Delay(delayTime, cancellationToken);

                    //TODO: periodically remove completed deployments so the dictionary doesn't grow indefinitely.
                }
            }
            finally
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "Application ip request processing ended.");
            }
        }

        private async Task<bool> TryDequeueAndProcessAsync(CancellationToken cancellationToken)
        {
            var queue = await this.StateManager.GetIpDeployments();

            Stopwatch sw = Stopwatch.StartNew();

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<TenantDeployment> workItem = await queue.TryDequeueAsync(tx, cancellationToken);
                if (!workItem.HasValue)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "No new ip deployment requests.");
                    return false;
                }

                TenantDeployment processedDeployment = await this.ProcessIpDeploymentAsync(workItem.Value, cancellationToken);

                if (processedDeployment.CreationStatus == CreationStatus.Created)
                {
                    //Trigger workflow
                    ActorId actorId = new ActorId(processedDeployment.Name);
                    ITenantWorkflowActor actor = ActorProxy.Create<ITenantWorkflowActor>(actorId, TenantHelpers.GetWorkflowActorUri());
                    await actor.ExternalCreated(processedDeployment.ExternalEndpoint);

                    ServiceEventSource.Current.ServiceMessage(this.Context, "ip Created");
                }
                else
                {
                    // The deployment hasn't completed or failed, so queue up the next stage of deployment
                    await queue.EnqueueAsync(tx, workItem.Value, cancellationToken);

                    ServiceEventSource.Current.ServiceMessage(this.Context, "ip enqueed again");
                }

                await tx.CommitAsync();
            }

            return true;
        }

        /// <summary>
        /// TODO: Clean this up.  check and errors.
        /// 
        /// Should check the loadbalancer.inner.status to see if update is already in progress.  
        /// Also should batch the queue items, add and configure multiple up configurations at once if they are in the queue.
        /// </summary>
        /// <param name="tenantDeployment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<TenantDeployment> ProcessIpDeploymentAsync(TenantDeployment tenantDeployment, CancellationToken cancellationToken)
        {
            var spId = Environment.GetEnvironmentVariable("SERVICE_PRINCIPAL_ID");
            var spSecret = Environment.GetEnvironmentVariable("SERVICE_PRINCIPAL_SECRET");
            var spTenantId = Environment.GetEnvironmentVariable("SERVICE_PRINCIPAL_TENANT_ID");
            var subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");

            var creds = new AzureCredentialsFactory().FromServicePrincipal(spId, spSecret, spTenantId, AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Authenticate(creds).WithSubscription(subscriptionId);

            string loadBalancerName = Environment.GetEnvironmentVariable("LOAD_BALANCER");
            string backendPool = Environment.GetEnvironmentVariable("BACKENDPOOL");
            string resourcegroup = Environment.GetEnvironmentVariable("RESOURCEGROUP");
            string tenantname = $"deployment-{tenantDeployment.Name}";

            //TODO check provisioning state of load balancer and delay if currently busy
            //var provisioningState = azure.ResourceGroups.GetByName(loadBalancer.ResourceGroupName).ProvisioningState;

            try
            {
                var loadBalancer = await azure.LoadBalancers.GetByResourceGroupAsync(resourcegroup, loadBalancerName, cancellationToken);
                ILoadBalancerPublicFrontend publicFrontend = loadBalancer.PublicFrontends.First().Value;
                var publicIpAddress = publicFrontend.GetPublicIPAddress();

                await loadBalancer.Update()
                        .DefineTcpProbe(tenantname + "_mc")
                            .WithPort(tenantDeployment.InternalPort)
                            .Attach()
                        .DefineLoadBalancingRule($"{tenantname}_mc")
                            .WithProtocol(TransportProtocol.Tcp)
                            .FromExistingPublicIPAddress(publicIpAddress)
                            .FromFrontendPort(tenantDeployment.InternalPort)
                            .ToBackend(backendPool)
                            .ToBackendPort(tenantDeployment.InternalPort)
                            .WithProbe(tenantname + "_mc")
                            .Attach()
                        .DefineTcpProbe(tenantname + "_rcon")
                            .WithPort(tenantDeployment.RconPort)
                            .Attach()
                        .DefineLoadBalancingRule($"{tenantname}_rcon")
                            .WithProtocol(TransportProtocol.Tcp)
                            .FromExistingPublicIPAddress(publicIpAddress)
                            .FromFrontendPort(tenantDeployment.RconPort)
                            .ToBackend(backendPool)
                            .ToBackendPort(tenantDeployment.RconPort)
                            .WithProbe(tenantname + "_rcon")
                            .Attach()
                        .ApplyAsync(cancellationToken);

                return new TenantDeployment(CreationStatus.Created, tenantDeployment, publicIpAddress.Fqdn);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "error provisioning ip addresses.", e.Message);
                return new TenantDeployment(CreationStatus.InProcess, tenantDeployment);
            }
        }

        public async Task QueueIpCreation(string tenantName, string internalIp, string rconEndpoint)
        {
            ServiceEventSource.Current.Message($"enqueue the tenant for ip address: {tenantName}, {internalIp}");
            var queue = await this.StateManager.GetIpDeployments();
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                var tenant = TenantDeployment.CreateNew(tenantName, internalIp, rconEndpoint);

                await queue.EnqueueAsync(tx, tenant);

                await tx.CommitAsync();
            }
        }
    }
}
