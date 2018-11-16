using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Threading;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Data;
using TenantManager.Helpers;
using Web.Helpers;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;
using Newtonsoft.Json.Linq;
using TenantManager.Common.Actors;
using TenantManager.Common.Utilities;

namespace Web.Controllers
{
    [Route("api/[Controller]")]
    public class TenantController : Controller
    {
        private readonly FabricClient fabricClient;
        private readonly IApplicationOperator applicationOperator;

        public TenantController(FabricClient fabricClient, IApplicationOperator applicationOperator)
        {
            this.fabricClient = fabricClient;
            this.applicationOperator = applicationOperator;
        }

        [HttpGet]
        public async Task<Object> Get()
        {
            var applicationList = await fabricClient.QueryManager.GetApplicationPagedListAsync(new ApplicationQueryDescription()
            {
                ApplicationTypeNameFilter = "mcType"
            });

            var tenants = applicationList
                .Select(x => new
                {
                    Name = x.ApplicationName,
                    Status = x.ApplicationStatus,
                    Version = x.ApplicationTypeVersion,
                    Health = x.HealthState,
                    Endpoints = new Dictionary<string,string>(),
                    Services = new List<Service>()
                }).ToList();

            foreach (var tenant in tenants)
            {
                var tenantName =  tenant.Name.ToString().GetName();

                ActorId actorId = new ActorId(tenantName);
                ITenantWorkflowActor actor = ActorProxy.Create<ITenantWorkflowActor>(actorId, TenantHelpers.GetWorkflowActorUri());
                var status = await actor.GetStatus();

                if (!string.IsNullOrEmpty(status.InternalIp) && !String.IsNullOrEmpty(status.RconEndpoint))
                {
                    // ports on external ip will be same as the internal as it is configured now.
                    var mcEndpoint = new Uri(status.InternalIp);
                    var rconEndpoint = new Uri(status.RconEndpoint);

                    tenant.Endpoints.Add("minecraft", $"{status.ExternalEndPoint}:{mcEndpoint.Port}");
                    tenant.Endpoints.Add("rcon", $"{status.ExternalEndPoint}:{rconEndpoint.Port}");
                }
            }

            return tenants;
        }
        
    }
}
