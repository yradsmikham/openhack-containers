using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TenantManager.Common.Models
{
    public class ApplicationDefinition
    {
        public string ApplicationName { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }

        public List<ServiceDefinition> Services { get; set; }
    }

    public class ServiceDefinition
    {
        public string ServiceName { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }

        public Dictionary<string, int> Ports { get; set; }
    }
}
