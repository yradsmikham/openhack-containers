using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using TenantManager.Common.Models;

namespace TenantManager.Helpers
{
    public class Constants
    {
        public const string QUEUE_DEPLOYMENTS = "QUEUE_DEPLOYMENTS";
        public const string AVALIABLE_PORTS = "AVALIABLE_PORTS";
    }

    public static class  DictionaryHelpers
    {
        public static async Task<IReliableConcurrentQueue<TenantDeployment>> GetTenantDeloymentsQueue(this IReliableStateManager stateManager)
        {
            return await stateManager.GetOrAddAsync<IReliableConcurrentQueue<TenantDeployment>>(Constants.QUEUE_DEPLOYMENTS);
        }

        public static async Task<IReliableDictionary<string, int>> GetAvaliablePortsDictionary(this IReliableStateManager stateManager)
        {
            return await stateManager.GetOrAddAsync<IReliableDictionary<string, int>>(Constants.AVALIABLE_PORTS);
        }
    }
}
