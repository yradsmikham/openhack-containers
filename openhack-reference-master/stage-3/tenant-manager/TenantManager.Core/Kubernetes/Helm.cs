using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TenantManager.Core.Kubernetes
{
    public static class Helm
    {
        public static async Task InstallAsync(string chart, int timeout = 30000)
        {
            var helm = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "helm.exe" : "helm";
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = helm,
                    Arguments = $@"install {chart}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            proc.Start();

            if (!proc.WaitForExit(timeout))
                throw new Exception($"helm timed out after {timeout}ms");

            if (proc.ExitCode != 0)
                throw new Exception($"helm failed with exit code {proc.ExitCode} - {proc.StandardError.ReadToEnd()}");
        }

        public static async Task DeleteAsync(string name, int timeout = 10000)
        {
            var helm = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "helm.exe" : "helm";
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = helm,
                    Arguments = $@"delete {name}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
            };

            proc.Start();

            if (!proc.WaitForExit(timeout))
                throw new Exception($"helm timed out after {timeout}ms");

            if (proc.ExitCode != 0)
                throw new Exception($"helm failed with exit code {proc.ExitCode} - {proc.StandardError.ReadToEnd()}");
        }
    }
}
