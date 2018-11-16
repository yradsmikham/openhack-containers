using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace TenantManager.Common.Services
{
    public interface ITenantManager : IService
    {
        Task QueueTenantCreation(string tenantName);
    }
}