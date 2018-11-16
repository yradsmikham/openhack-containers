using System.Collections.Generic;
using System.Threading.Tasks;

namespace TenantManager.Core
{
    public interface ITenantManager
    {
        Task<IList<Tenant>> ListAllAsync();
        Task CreateAsync();
        Task DeleteAsync(string name);
    }
}
