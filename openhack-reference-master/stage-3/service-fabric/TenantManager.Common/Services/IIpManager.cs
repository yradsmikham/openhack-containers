using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace TenantManager.Common.Services
{
    public interface IIpManager : IService
    {
        Task QueueIpCreation(string tenantName, string internalIp, string rconEndpoint);
    }
}