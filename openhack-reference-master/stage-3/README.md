# Stage 3

## Kubernetes
Check out the [Readme](tenant-manager/README.md) in Tenant Manger folder.

The Kubernetes tenant manager uses a simple [Helm](https://helm.sh/) chart to deploy Minecraft in a repeatable way

To customize and package your own chart, perform these steps:

1. Install the [Helm client](https://docs.helm.sh/using_helm/#installing-helm) locally
1. Make any desired modifications to the chart inside the `k8s` folder. Make sure to bump the version number!
1. Package the chart using the command: `helm package minecraft-server` from the `k8s` folder
1. Copy your chart to the `tenant-manager/TenantManager.Core/Kubernetes` folder
1. Update the chart reference in `tenant-manager/TenantManager.Core/Kubernetes/KubernetesTenantManager.cs`

> Note: In a production environment, you probably wouldn't copy the package into your tenant manager application. Instead, you'd likely have a Helm chart repository that stores packages pushed by your CI/CD system, and then your cluster(s) would have access to retrieve & deploy them.

## Service Fabric 
Check out the [Readme](service-fabric/Readme.md) in the Service Fabric folder.

## Monitoring

There are some helpful commands to get the number of connected players for a given instance

```bash
# Using rcon-cli (https://github.com/itzg/rcon-cli)
rcon-cli --host 127.0.0.1 --port 25575 --password minecraft list

# Using mcstatus (https://github.com/Dinnerbone/mcstatus)
mcstatus 127.0.0.1 status
```