using System;
using System.Collections.Generic;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using TenantManager.Common.Models;

namespace TenantManager.Common.Utilities
{
    public interface IApplicationOperator : IDisposable
    {
        Task<ApplicationDescription> CreateApplicationAsync(ApplicationDefinition applicationDescription, CancellationToken token);
        Task<(bool HasEndPoint, Dictionary<string,string> EndPoint)> GetServiceEndpoint( string serviceInstanceUri, CancellationToken token);
        Task<bool> ApplicationExistsAsync(string applicationInstanceName, CancellationToken token);
    }
}