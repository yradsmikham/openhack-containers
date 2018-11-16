## To build the custom MC docker image 
Custom image is needed for handling Azure FileShare

1. Build base image:

```
cd minecraft-docker\nanoserver
docker build -m 2GB -t minecraft-server:nanoserver .
```

2. Build custom image:

```
cd minecraft-docker\nanoserver-custom
docker build -m 2GB -t mc .
```

Push this custom image to a Image Repository and update `mc\ApplicationPackageRoot\MCPkg\ServiceManifest.xml` with your custom image name.

## Azure set up
There are several settings that need to be set for the Tenant Manager App:

- [Azure storage account](https://docs.microsoft.com/en-us/azure/storage/common/storage-create-storage-account?toc=%2fazure%2fstorage%2ffiles%2ftoc.json):
    - Storage account name
    - Storage account key

- Service Fabric Resources (look in the RG that SF was created in)
    - LoadBalancer Name
    - LoadBalancer Backend Pool Name
    - Resource Group Name

- [Service Principal](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md)
    - service principal id
    - service principal secret
    - service principal tenantid

- You Azure subscription Id

Once you have all of these you can edit your `cloud publish profile` at `mc\PublishProfiles\Cloud.xml` with the parameter names:

```xml
<Parameter Name="LOAD_BALANCER_env" Value="LB-mcsfjs-main" />
<Parameter Name="BACKENDPOOL_env" Value="LoadBalancerBEAddressPool" />
<Parameter Name="RESOURCEGROUP_env" Value="mc-sf" />
<Parameter Name="STORAGE_ACCOUNT_NAME_env" Value="minecraftworld" />
<Parameter Name="STORAGE_ACCOUNT_KEY_env" Value="" />
<Parameter Name="SERVICE_PRINCIPAL_ID_env" Value="" />
<Parameter Name="SERVICE_PRINCIPAL_SECRET_env" Value="" />
<Parameter Name="SERVICE_PRINCIPAL_TENANT_ID_env" Value="" />
<Parameter Name="SUBSCRIPTION_ID_env" Value="" />
```

## Azure Service Fabric Ports:
Make sure you have port `8827` open in [Load balancer](https://docs.microsoft.com/en-us/azure/service-fabric/create-load-balancer-rule) to enable access to the Tenant Manager app.

## Deploy
Once all the settings are configured you can deploy the solution.  [Setting up a CI/CD pipeline](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-tutorial-deploy-app-with-cicd-vsts) if preferable via VSTS (or some other tool).

For quick demonstration/dev you can use publish from VS:

1. Publish the `mc` Service Fabric project
2. Publish the `TenantManagerApp` Service Fabric project.

## Use 
There are two endpoints:

1. Tenant info endpoint: http://<your-SF-url>.eastus.cloudapp.azure.com:8827/api/tenant

    ```
    [
        {
            "name": "fabric:/tenants/game12",
            "status": 1,
            "version": "1.0.0",
            "health": 1,
            "endpoints": {
                "minecraft": "mcsfjs.eastus.cloudapp.azure.com:20001",
                "rcon": "mcsfjs.eastus.cloudapp.azure.com:20002"
                },
            "services": []
        }
    ]
    ```
2. Tenant Creation endpoint: http://<your-SF-url>.eastus.cloudapp.azure.com:8827/api/customer/<gamename>
    - A `GET` shows you the full status of the creationg progress

        ```
        {
            "currentStatus": "CustomerReady",
            "history": {
                "401d7842-180c-449b-811a-920bcccf7288": {
                    "time": 1506693388,
                    "state": "ExternalIpCreating"
                },
                "5adba25d-1052-4563-a74a-6ef075d38439": {
                    "time": 1506693901,
                    "state": "CustomerReady"
                },
                "68448790-0c4c-44bc-bd8f-872dae623ffc": {
                    "time": 1506693245,
                    "state": "New"
                },
                "a926956f-ca80-4dcb-9016-9e16ecb5e4ba": {
                    "time": 1506693388,
                    "state": "InternalReady"
                },
                "cb768984-651e-4af1-952b-f3c9f0dd9e8b": {
                    "time": 1506693245,
                    "state": "Creating"
                }
            },
            "avaliable": [
                "New",
                "Creating",
                "InternalReady",
                "ExternalIpCreating",
                "CustomerReady",
                "Deleted",
                "Deleting",
                "Error"
            ],
            "internalIp": "tcp://10.0.0.9:20001/",
            "tenantName": "game12",
            "externalEndPoint": "mcsfjs.eastus.cloudapp.azure.com",
            "rconEndpoint": "tcp://10.0.0.9:20002/"
        }
        ```
        
    - A `POST` will create a new tenant
