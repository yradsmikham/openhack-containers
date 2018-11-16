# tenant-manager

## Requirements
Both of these tools should be on your path:

* kubectl - install via Azure Cli (check out previous stages for instructions)
* [helm](https://docs.helm.sh/using_helm/#installing-helm)

## Run the Tenant Manager Locally
Open the VS Solution, build and run the TenantManager.Web project.  If you have helm and kubectl on you path and configured to talk to you K8 cluster you should be able to create new minecraft tenants.

### Deploy the Tenant Manager onto your Cluster
```bash
# copy kube config for image to solution (only for challenge purposes)
cp ~/.kube/config ./stage-3/tenant-manager/

# build the docker file for the image and push it to repo
docker build -f .\TenantManager.Web\Dockerfile -t tm .
docker tag tm <yourrepo>/tm:latest
docker push <yourrepo/tm>

# deploy on k8s
kubectl run tenantmanager --image=<yourrepo>/tm --port=80
kubectl expose deployment tenantmanager --port=80 --type=LoadBalancer

# Watch the service to get the external IP as it's provisioned
kubectl get svc tenantmanager -w
```