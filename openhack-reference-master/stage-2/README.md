# Stage 2

Stage 2 moves from single-host development to a distributed architecture

## Kubernetes

### Create a cluster
Be sure to run `az login` before running commands below.  Replace the resource group (`-g` parameter) below with your open hack resource group.

```bash
# Create a resource group
az group create -n openhack -l southcentralus 

# Create a Kubernetes cluster
az acs create -n openhackk8s -t kubernetes -g openhack --service-principal <clientid> --client-secret <client-secret>

#install the kubectl cli
az acs kubernetes install-cli

# Get the connection info for the cluster
az acs kubernetes get-credentials -n openhackk8s -g openhack

# Verify you are connected to the correct cluster (should see your name of your cluster)
kubectl config current-context
```

### Quick start

These commands will get you started with a basic Minecraft instance running on your cluster

```bash
# Create a Kubernetes deployment that will run a single pod
kubectl run minecraft --image=openhack/minecraft-server:1.0 --port=25565 --env EULA=TRUE

# Expose the instance to the world via Azure Load Balancer
# Note: this will take a few minutes
kubectl expose deployment minecraft --port=25565 --type=LoadBalancer

# Watch the service to get the external IP as it's provisioned
kubectl get svc minecraft -w
```

At this point, you can use the external IP to connect to your Minecraft instance

### Exposing more ports

You can manipulate the Kubernetes objects in-place to expose additional ports. The Minecraft image supports RCON, so let's wire that up. `kubectl edit` will open up the definition in your default editor and will validate/commit your changes after you close.

```bash
# Add port 25575 (takes your pod offline)
kubectl edit deploy minecraft

## add the following to the file under spec/ports
##- containerPort: 25575
##  protocol: TCP

# Add port 25575
# Note: When exposing multiple ports, you must give them names!
kubectl edit svc minecraft

## update the following under spec/ports
##- nodeport:<whatever is there>
##  name: minecraft
##  protocol: TCP
##  port: 25565
##  targetPort: 25565
##- nodeport:<increment mc by one>
##  name: rcon
##  protocol: TCP
##  port: 25575
##  targetPort: 25575
```

1. Download `rcon-cli` from [itzg/rcon-cli](https://github.com/itzg/rcon-cli/releases)
2. Run the following command to list the players on the server
```bash
rcon-cli --host <your_external_ip> --port 25575 --password cheesesteakjimmys list
```

> You can find more Minecraft commands on the [official wiki](https://minecraft.gamepedia.com/Commands)


### Persisting data

Minecraft with a world that goes away on every server restart is no good. Use the following commands and the corresponding `minecraft.yml` file to create a reliable Minecraft instance with a persistent world.

#### Creating an Azure File share

Kubernetes needs somewhere to put the Minecraft data, so we'll use Azure Files

```bash
# Create a storage account
az storage account create -n openhackk8s -g openhack --sku Standard_LRS

# Create a file share to mount into the container
# Since creating a share is a data-plane operation, we need the connection string to call into the Azure Storage API's
# The Azure CLI will automatically pick up the $AZURE_STORAGE_CONNECTION_STRING for use in storage operations
export AZURE_STORAGE_CONNECTION_STRING=`az storage account show-connection-string -n openhackk8s -g openhack -o tsv`
az storage share create -n minecraft-data
```

#### minecraft.yml

`minecraft.yml` includes everything you need for a persistent Minecraft deployment. You just need to plug in the secrets for your storage account

```bash
# Get base64 encoded values for yaml file
# Note: the flags are very important! Extra whitespace/newlines in a connection string can be difficult to troubleshoot
echo -n openhackk8s | base64 -w 0
echo -n `az storage account keys list -n openhackk8s -g openhack --query '[0].value' -o tsv` | base64 -w 0
```

> You remembered to update `minecraft.yml`, didn't you?

```bash
# Apply the configuration to K8s to create/modify resources
cd stage-2
kubectl apply -f minecraft.yml

# Wait for the Load Balancer public IP
kubectl get svc -w
```

## Service Fabric

### Prerequisites
 `TODO: Also cover Azure PowerShell, or no?`
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
- [Visual Studio 2017](https://www.visualstudio.com/downloads/)
- [Service Fabric SDK and Tools](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started)

### Create a cluster

`TODO: How much do we want to repeat here versus just linking to docs? The following link doesn't fully cover the Secure option.`

[docs.microsoft.com reference](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started-azure-cluster)


### Quick start

1. In Visual Studio , Select `File > New Project`. 
1. Use the `Service Fabric Application` template to create the application, and then use the `Container` template for the default service which the application contains.
1. Use `openhack/minecraft-server:2.0` for the Image Name.
1. Refer to the reference files in [ApplicationPackageRoot](./ApplicationPackageRoot/). noting the following changes.

To pass `-e EULA=TRUE`, add the following to `ServiceManifest.xml` as a child of the `<CodePackage>` element. The default template should already have a commented out `EnvironmentVariable` to help you determine the correct location, or you can refer to the [ServiceManifest.xml](./ApplicationPackageRoot/ServiceManifest.xml) contained in this reference.
```
    <EnvironmentVariables>
      <EnvironmentVariable Name="EULA" Value="TRUE"/>
    </EnvironmentVariables>
```

To expose the ports, replace the Endpoint in `ServiceManifest.xml` with the following. The default template should already have an Endpoint to help you determine the correct location, or you can refer to the [ServiceManifest.xml](./ApplicationPackageRoot/ServiceManifest.xml) contained in this reference.
```
    <Endpoint Name="minecraftTypeEndpoint" Port="25565" Protocol="tcp" />
    <Endpoint Name="rconEndpoint" Port="25575" Protocol="tcp" />
```

You will also need to publish the ports as part of the `ContainerHostPolicies` in the `ApplicationManifest.xml`. These policies are a child of the `ServiceManifestImport`, a sibling of the `ServiceManifestRef`. Refer to the [ApplicationManifest.xml](./ApplicationPackageRoot/ApplicationManifest.xml) contained in this reference if you need help confirming the location.

```
<Policies>
    <ContainerHostPolicies CodePackageRef="Code">
    <PortBinding ContainerPort="25565" EndpointRef="minecraftTypeEndpoint" />
    <PortBinding ContainerPort="25575" EndpointRef="rconEndpoint" />
    </ContainerHostPolicies>
</Policies>
```

### Deploy to your cluster

The easiest way to do this is to right click your SF project in Visual Studio and select Publish. Enter the cluster connection endpoint for the cluster you created above, and use the Cloud Publish Profile.

### Create a load balancer rule for the ports

The following will list the existing load balancer rules applied to the VM scale set in your cluster. 

`az network lb rule list --resource-group [your-group] --lb-name [your-lb]`

By default, the load balancer name is LB-[your-cluster-name]-nt1, but you can also find it by using `az network lb list --resource-group [your-group]`.

To create a new rule, use the following command. 
`az network lb rule create --resource-group [your-group] --lb-name [your-lb] --name LBMinecraftRule --protocol tcp --frontend-port 25565 --backend-port 25565 --load-distribution SourceIPProtocol`

Repeat this to create the rcon rule
`az network lb rule create --resource-group [your-group] --lb-name [your-lb] --name LBRconRule --protocol tcp --frontend-port 25575 --backend-port 25575 --load-distribution SourceIPProtocol`

You can read about other options for this command at [docs.microsoft.com](https://docs.microsoft.com/en-us/azure/load-balancer/load-balancer-get-started-internet-arm-cli).

### Persisting data

Minecraft with a world that goes away on every server restart is no good. Add the following to your `ServiceManifest`, in the `ContainerHostPolicies` as a sibling of the `PortBinding` elements, to create a reliable Minecraft instance with a persistent world.

```
<Volume Source="minecraft" Destination="c:\data" Driver="local" />
```

Note that this volume will still be on the local disk of each node in your cluster. If the VM is destroyed, you'll still lose the data. Furthermore, the data is not shared across the nodes of your cluster. But it's good enough for now to demonstrate how to mount a volume.