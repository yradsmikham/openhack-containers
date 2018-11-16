using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Fabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Actors.Client;
using TenantManager.Common.Actors;
using TenantManager.Common.Utilities;

namespace Web.Controllers
{
    [Route("api/[Controller]")]
    public class CustomerController : Controller
    {

        [HttpGet]
        [Route("{tenantName}")]
        public async Task<Object> Get(string tenantName)
        {
            try
            {

                ActorId actorId = new ActorId(tenantName);
                ITenantWorkflowActor actor = ActorProxy.Create<ITenantWorkflowActor>(actorId, TenantHelpers.GetWorkflowActorUri());
                var status = await actor.GetStatus();
                return Ok(status);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
      
        }

       

        [HttpPost]
        [Route("{tenantName}")]
        public async Task<IActionResult> Post(string tenantName)
        {
            //TODO validations 

            ActorId actorId = new ActorId(tenantName);
            ITenantWorkflowActor actor = ActorProxy.Create<ITenantWorkflowActor>(actorId, TenantHelpers.GetWorkflowActorUri());
            var status = await actor.Create(tenantName);

            return Ok(status);
        }


    }
}
