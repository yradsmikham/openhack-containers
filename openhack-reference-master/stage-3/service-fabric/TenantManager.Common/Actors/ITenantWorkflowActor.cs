using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace TenantManager.Common.Actors
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface ITenantWorkflowActor : IActor
    {
        Task<TenantStatus> Create(string tenantName);
        Task<TenantStatus> InternalCreated(string internalIp, string rconEndpoint);
        Task<TenantStatus> Delete();

        Task<TenantStatus> GetStatus();
        Task<TenantStatus> ExternalCreated(string externalEndpoint);
    }
}
