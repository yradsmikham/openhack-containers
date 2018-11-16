using System;
using System.Fabric.Query;
using System.Runtime.Serialization;
using TenantManager.Common.Utilities;

namespace TenantManager.Common.Models
{
    [DataContract]
    public struct TenantDeployment
    {

        public static TenantDeployment CreateNew(string tenantName)
        {
            return new TenantDeployment(Guid.NewGuid().ToString(), tenantName.GetApplicationName(), tenantName, CreationStatus.New, null, null, null);
        }


        public static TenantDeployment CreateNew(string tenantName, string internalIp, string rconEndpoint)
        {
            return new TenantDeployment(Guid.NewGuid().ToString(), tenantName.GetApplicationName(), tenantName, CreationStatus.New, internalIp, rconEndpoint, null);
        }

        public TenantDeployment(CreationStatus creationStatus, TenantDeployment copyFrom)
            : this(copyFrom.InternalName,
                copyFrom.ApplicationName,
                copyFrom.Name,
                creationStatus,
                copyFrom.InternalMineCraftEndpoint,
                copyFrom.InternalRconEndpoint,
                copyFrom.ExternalEndpoint)
        {
        }


        public TenantDeployment(CreationStatus creationStatus, string internalEndpoint,string rconEndpoint, TenantDeployment copyFrom) 
            : this(copyFrom.InternalName,
            copyFrom.ApplicationName,
            copyFrom.Name,
                creationStatus,
                internalEndpoint,
                rconEndpoint,
                copyFrom.ExternalEndpoint)
        {
        }

        public TenantDeployment(CreationStatus creationStatus,  TenantDeployment copyFrom, string externalEndpoint)
            : this(copyFrom.InternalName,
                copyFrom.ApplicationName,
                copyFrom.Name,
                creationStatus,
                copyFrom.InternalMineCraftEndpoint,
                copyFrom.InternalRconEndpoint,
                externalEndpoint)
        {
        }

        public TenantDeployment(
            string internalName,
            string applicationName,
            string name,
            CreationStatus status,
            string internalMineCraftEndpoint,
            string internalRconEndpoint,
            string externalEndpoint)
        {
            this.InternalName = internalName;
            this.ApplicationName = applicationName;
            this.Name = name;
            this.CreationStatus = status;
            this.InternalMineCraftEndpoint = internalMineCraftEndpoint;
            this.InternalRconEndpoint = internalRconEndpoint;
            this.ExternalEndpoint = externalEndpoint;
        }


        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public string InternalName { get; private set; }

        [DataMember]
        public CreationStatus CreationStatus { get; private set; }

        [DataMember]
        public string InternalMineCraftEndpoint { get; private set; }

        [DataMember]
        public string InternalRconEndpoint { get; private set; }


        [DataMember]
        public string ApplicationName { get; private set; }

       
        public int InternalPort => new Uri(InternalMineCraftEndpoint).Port;
        public int RconPort => new Uri(InternalRconEndpoint).Port;
        public bool HasInternalEndpoint => string.IsNullOrEmpty(this.InternalMineCraftEndpoint);
        public string ExternalEndpoint { get; set; }
    }
}