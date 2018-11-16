using System;
using TenantManager.Core.Kubernetes;
using TenantManager.Core.ServiceFabric;

namespace TenantManager.Core
{
    public static class TenantManagerFactory
    {
        public static ITenantManager GetTenantManager(string orchestrator)
        {
            switch (orchestrator)
            {
                case "kubernetes":
                    return new KubernetesTenantManager();
                case "servicefabric":
                    return new ServiceFabricTenantManager();
                default:
                    throw new InvalidOperationException("Invalid orchestrator type in configuration");
            }
        }
    }
}
