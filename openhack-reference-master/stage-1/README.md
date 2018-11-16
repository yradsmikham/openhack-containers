# Stage 1

Stage 1 is all about running containers on a single host

## Running locally

1. [Install Docker](https://docs.docker.com/engine/installation/)
1. Run the minecraft-server image

```bash
# Linux container
docker run --rm -d -p 25565:25565 -p 25575:25575 -e EULA=TRUE openhack/minecraft-server:1.0

# Windows container
docker run --rm -d -p 25565:25565 -p 25575:25575 -e EULA=TRUE openhack/minecraft-server:1.0
```

## Running on an Azure VM

### Using the [Azure Portal](https://portal.azure.com)

### Using Azure CLI (on Bash)
Be sure to run `az login` before running commands below.  Replace the resource group (`-g` parameter) below with your open hack resource group.

#### Linux
```bash
# Create a resource group
az group create -n openhack -l westus2

# Create a Linux VM
az vm create -n openhackvm -g openhack --image UbuntuLTS --size Standard_D2_v2

# Install Docker
az vm extension set --vm-name openhackvm -g openhack --name DockerExtension  --publisher Microsoft.Azure.Extensions --version 1.2.2

# Open ports
az vm open-port --name openhackvm -g openhack --port 25575
az vm open-port --name openhackvm -g openhack --port 25565 --priority 901

# SSH into machine and run docker command
ssh <your-username>:<machine-ip>

# run the docker image
docker run --rm -d -p 25565:25565 -p 25575:25575 -e EULA=TRUE openhack/minecraft-server:1.0
```

#### Windows
```bash
# Create a resource group
az group create -n openhack -l westus2

# Create a Windows 2016 VM
az vm create -n openhackvm -g openhack --image Windows
```

### Using Azure PowerShell

```powershell
```

### Using an Azure Resource Manager Template

## (Optional) Running on Azure Container Instances

### Linux

```
az container create --name minecraft --image openhack/minecraft-server:1.0 --memory 1.5 --ip-address public -g openhack --port 25565  -e EULA=TRUE -l eastus
```

### Windows
There is a bug in ACI for windows that causes http requests to fail.  Work around is to sleep for 10 seconds on start.

```
az container create --image openhack/minecraft-server:1.0-nanoserver --os-type Windows --memory 1.5 --port 25565 -e 'EULA=TRUE' -g openhack -n minecraft-windows -l eastus --ip-address Public --command-line "powershell.exe -Command 'Start-Sleep -s 10; & C:\\minecraft\\customstart.ps1'"
```