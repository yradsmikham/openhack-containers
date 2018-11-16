using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TenantManager.Core.ServiceFabric
{
    public class ServiceFabricTenantManager : ITenantManager
    {
        public Task<IList<Tenant>> ListAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(string name)
        {
            throw new NotImplementedException();
        }
    }
}
