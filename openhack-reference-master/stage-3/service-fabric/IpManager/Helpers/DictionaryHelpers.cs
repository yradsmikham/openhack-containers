using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using TenantManager.Common.Models;

namespace IpManager.Helpers
{
    public class Constants
    {
        public const string QUEUE_DEPLOYMENTS = "IpDeployments";
    }

    public static class DictionaryHelpers
    {
        public static async Task<IReliableConcurrentQueue<TenantDeployment>> GetIpDeployments(this IReliableStateManager stateManager)
        {
            return await stateManager.GetOrAddAsync<IReliableConcurrentQueue<TenantDeployment>>(Constants.QUEUE_DEPLOYMENTS);
        }
    }
}