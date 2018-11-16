# Challenge 2 - Let's get ready to cluster!


## Background
Containers are extremely useful on their own, but their flexibility and potential is multiplied when deployed to an orchestrator/cluster.

You can learn more about the value of orchestrators at [docs.microsoft.com](https://docs.microsoft.com), or more specifically the following links:

- [Service Fabric and Containers](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-containers-overview)
- [Introduction to Azure Container Service (AKS)](https://docs.microsoft.com/en-us/azure/aks/intro-kubernetes)

Once you have a cluster configured, you can react quickly to demand and leverage the extended functionality of the underlying platform to best suit your needs whether that's:

- Deploying quickly and reliably.
- Scaling on at will to meet demand.
- Rolling out new features or upgrades.
- Utilizing only what resources you need for your current provision.

## Challenge

Your team's goal in this challenge is to deploy the same container you used in challenge 1, to a cluster in Azure either with [Service Fabric](https://docs.microsoft.com/en-us/azure/service-fabric/) or [Azure Container Service (AKS)](https://docs.microsoft.com/en-us/azure/aks/) in the _EastUS_ Azure region.

## Success Criteria

- Create a cluster in Azure, running v1.0 of your chosen container, in the _EastUS_ Azure region.


## References
- You can find the Minecraft containers [on Docker Hub](https://hub.docker.com/r/openhack/minecraft-server/)
- HINT: There is a second port on a Minecraft server for RCON (Remote Console) **25575**, in addition to the default connection port (25565). The hack portal uses this to verify your server!!

Some other useful resources in addition to the ones in challenge 1 are:

- [Azure resource naming best practices](https://docs.microsoft.com/en-us/azure/architecture/best-practices/naming-conventions)
- [Azure CLI reference](https://docs.microsoft.com/en-us/cli/azure/get-started-with-azure-cli)
- [Kubectl overview](https://kubernetes.io/docs/user-guide/kubectl-overview/)
- [Service Fabric Containers Overview](	https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-containers-overview)

## Solution

Using Service Fabric, create a shell script:

```
#!/bin/bash

# Variables
ResourceGroupName="yvradsmi-minecraft-rg" 
ClusterName="yvradsmi-minecraft-cluster" 
Location="eastus" 
Password="whatsyourpassword?" 
Subject="yvradsmi-minecraft-cluster.eastus.cloudapp.azure.com" 
VaultName="yvradsmi-minecraft-vault" 
VmPassword="whatsyourpassword?"
VmUserName="yvradsmi-admin"

# Create resource group
az group create --name $ResourceGroupName --location $Location 

# Create secure five node Linux cluster. Creates a key vault in a resource group
# and creates a certficate in the key vault. The certificate's subject name must match 
# the domain that you use to access the Service Fabric cluster.  The certificate is downloaded locally.
az sf cluster create --resource-group $ResourceGroupName --location $Location \ 
  --certificate-output-folder . --certificate-password $Password --certificate-subject-name $Subject \
  --cluster-name $ClusterName --cluster-size 5 --os UbuntuServer1604 --vault-name $VaultName \ 
  --vault-resource-group $ResourceGroupName --vm-password $VmPassword --vm-user-name $VmUserName
```

Using AKS
