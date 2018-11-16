using System;
using System.Security.Cryptography;
using System.Text;

namespace TenantManager.Common.Utilities
{
    public static class TenantHelpers{
        public static string GetName(this string s)
        {
            return s.Replace("fabric:/tenants/", "");
        }

        public static string GetApplicationName(this string s)
        {
            return $"fabric:/tenants/{s}";
        }

        public static Uri GetWorkflowActorUri()
        {
            return new Uri("fabric:/MultiTenant/TenantWorkflowActorService");
        }

        public static Int64 GetPartionKey(this string s)
        {
            var md5 = MD5.Create();
            var value = md5.ComputeHash(Encoding.ASCII.GetBytes(s));
            return BitConverter.ToInt64(value, 0);
        }
    }
}