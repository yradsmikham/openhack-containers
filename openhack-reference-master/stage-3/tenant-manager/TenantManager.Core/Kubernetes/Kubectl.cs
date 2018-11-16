using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TenantManager.Core.Kubernetes
{
    public static class Kubectl
    {
        public static async Task<string> GetAsync(string command, int timeout = 10000)
        {
            var kubectl = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "kubectl.exe" : "kubectl";
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = kubectl,
                    Arguments = $@"get {command} -o json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            proc.Start();

            // Need to continuously read the stdout buffer or kubectl will hang
            // See: https://stackoverflow.com/questions/439617/hanging-process-when-run-with-net-process-start-whats-wrong
            var stdout = Task.Run(() =>
            {
                var sb = new StringBuilder();
                while (!proc.StandardOutput.EndOfStream)
                {
                    sb.AppendLine(proc.StandardOutput.ReadLine());
                }
                return sb.ToString();
            });

            if (!proc.WaitForExit(timeout))
                throw new Exception($"kubectl timed out after {timeout}ms");
            
            if (proc.ExitCode != 0)
                throw new Exception($"kubectl failed with exit code {proc.ExitCode} - {proc.StandardError.ReadToEnd()}");

            return await stdout;
        }
    }
}
