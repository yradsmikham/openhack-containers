// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). Modified from https://github.com/Azure-Samples/service-fabric-dotnet-management-party-clusterd
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Newtonsoft.Json.Linq;
using TenantManager.Common.Models;

namespace TenantManager.Common.Utilities
{
    public class FabricClientApplicationOperator : IApplicationOperator
    {
        private readonly TimeSpan readOperationTimeout = TimeSpan.FromSeconds(5);
        private readonly TimeSpan writeOperationTimeout = TimeSpan.FromMinutes(1);
        private bool disposing = false;

        private readonly FabricClient fabricClient;

        public FabricClientApplicationOperator()
        {
            this.fabricClient = new FabricClient();
        }

        public async Task<(bool HasEndPoint, Dictionary<string, string> EndPoint)> GetServiceEndpoint(string serviceName, CancellationToken token)
        {

            var serviceInstanceUri = new Uri(serviceName);

            ResolvedServicePartition rsp = await fabricClient.ServiceManager.ResolveServicePartitionAsync(serviceInstanceUri, this.readOperationTimeout, token);
            ResolvedServiceEndpoint endpoint = rsp.GetEndpoint();

            // This assumes the service uses the Reliable Services framework,
            // where the endpoint is always a JSON object that can contain multiple endpoints.
            JObject endpointJson = JObject.Parse(endpoint.Address);

            if (endpointJson["Endpoints"].HasValues)
            {
                var endpoints = new Dictionary<string, string>();

                endpoints.Add("MCTypeEndpoint", endpointJson["Endpoints"]["MCTypeEndpoint"].Value<string>());
                endpoints.Add("MCrcon", endpointJson["Endpoints"]["MCrcon"].Value<string>());

                return (true, endpoints);
            }

            return (false, new Dictionary<string, string>());
        }

        /// <summary>
        /// Creates an instance of an application.
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="applicationTypeName"></param>
        /// <param name="applicationTypeVersion"></param>
        /// <param name="token"></param>
        /// <param name="cluster"></param>
        /// <param name="applicationInstanceName"></param>
        /// <returns></returns>
        public async Task<ApplicationDescription> CreateApplicationAsync(ApplicationDefinition applicationDefinition, CancellationToken token)
        {
            var app = new ApplicationDescription()
            {
                ApplicationName = new Uri(applicationDefinition.ApplicationName),
                ApplicationTypeName = applicationDefinition.Type,
                ApplicationTypeVersion = applicationDefinition.Version
            };

            //maybe move this some where else as seperate step?
            var accountName = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME");
            var accountKey = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_KEY");

            var connectionstring = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

            var name = applicationDefinition.ApplicationName.GetName();
            await CreateAzureFileStorage(name, connectionstring);

            // set up env varaialbes for save world files to azure storage
            app.ApplicationParameters.Add("worldtolink_env", $@"\\{accountName}.file.core.windows.net\{name}");
            app.ApplicationParameters.Add("passcode_env", accountKey);
            app.ApplicationParameters.Add("MOTD_env", $"service fabric minecraft: {applicationDefinition.ApplicationName}");

            // set up the ports.
            var serviceDefinition = applicationDefinition.Services.First();

            var mcPort = serviceDefinition.Ports["MCPORT"];
            var rconPort = serviceDefinition.Ports["RCONPORT"];
            app.ApplicationParameters.Add("MCPORT", mcPort.ToString());
            app.ApplicationParameters.Add("RCONPORT", rconPort.ToString());

            try
            {
                await fabricClient.ApplicationManager.CreateApplicationAsync(app,writeOperationTimeout, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            // Create the stateless service description.  For stateful services, use a StatefulServiceDescription object.
            StatelessServiceDescription serviceDescription = new StatelessServiceDescription();
            serviceDescription.ApplicationName = app.ApplicationName;
            serviceDescription.InstanceCount = 1;
            serviceDescription.PartitionSchemeDescription = new SingletonPartitionSchemeDescription();
            serviceDescription.ServiceName = new Uri($"{app.ApplicationName}/{serviceDefinition.ServiceName}");
            serviceDescription.ServiceTypeName = serviceDefinition.Type;
            

            try
            {
                await fabricClient.ServiceManager.CreateServiceAsync(serviceDescription, this.writeOperationTimeout, token);
                return app;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }



        private async Task CreateAzureFileStorage(string name, string connectionstring)
        {
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionstring);
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(name);

            try
            {
                await share.CreateIfNotExistsAsync();
            }
            catch (StorageException ex)
            {
               Console.WriteLine($"Error while creating storage account '{name}': {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ApplicationExistsAsync(string applicationInstanceName, CancellationToken token)
        {
            Uri appName = new Uri(applicationInstanceName);

            ApplicationList applications = await fabricClient.QueryManager.GetApplicationListAsync(appName, TimeSpan.FromSeconds(30), token);

            return applications.Any(x => x.ApplicationName == appName);
        }

        public void Dispose()
        {
            if (!this.disposing)
            {
                this.disposing = true;
           
                try
                {
                    ((FabricClient)this.fabricClient).Dispose();
                }
                catch
                {
                    // move on
                }
            }
        }
    }
}