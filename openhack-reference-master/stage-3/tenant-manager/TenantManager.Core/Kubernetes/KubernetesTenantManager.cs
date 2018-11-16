using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TenantManager.Core.Kubernetes
{
    public class KubernetesTenantManager : ITenantManager
    {
        public async Task<IList<Tenant>> ListAllAsync()
        {
            var output = await Kubectl.GetAsync("services");
            var services = JObject.Parse(output);
            return services["items"]
                .Where(s => s["metadata"]?["labels"]?["chart"] != null)
                .OrderByDescending(s => s["metadata"]?.Value<DateTime>("creationTimestamp"))
                .Select(s => new Tenant
                {
                    Name = s["metadata"].Value<string>("name").Replace("-minecraft-server", ""),
                    Endpoints = new Dictionary<string, string>
                    {
                        { "minecraft", $"{s["status"]?["loadBalancer"]?["ingress"]?[0]?["ip"]}:{s["spec"]?["ports"]?.FirstOrDefault(p => p.Value<string>("name") == "minecraft")?["port"]}" },
                        { "rcon", $"{s["status"]?["loadBalancer"]?["ingress"]?[0]?["ip"]}:{s["spec"]?["ports"]?.FirstOrDefault(p => p.Value<string>("name") == "rcon")?["port"]}" },
                    }
                }).ToList();
        }

        public async Task CreateAsync()
        {
            var startupDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var chartPath = Path.Combine(startupDir, "Kubernetes", "minecraft-server-0.0.2.tgz");
            await Helm.InstallAsync(chartPath);
        }

        public async Task DeleteAsync(string name)
        {
            await Helm.DeleteAsync(name);
        }
    }
}
