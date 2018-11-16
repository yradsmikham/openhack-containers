using System.Collections.Generic;

namespace TenantManager.Core
{
    public class Tenant
    {
        public string Name { get; set; }
        public Dictionary<string, string> Endpoints { get; set; } = new Dictionary<string, string>();
    }
}
