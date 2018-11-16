# Kubernetes

## Helm chart

[minecraft-server](minecraft-server/README.md) contains a Helm chart to spin up a new Minecraft server instance

Use the following commands to build and deploy from the command line

```bash
# Sanity check to make sure the chart is valid
helm lint minecraft-server

# Package up the chart
helm package minecraft-server

# Deploy the chart onto your cluster
helm install minecraft-server
```