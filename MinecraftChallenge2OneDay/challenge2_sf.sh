#!/bin/bash

# Variables
ResourceGroupName="yvradsmi-openhack-containers" 
ClusterName="yvradsmisfcluster" 
Location="westus" 
Password="P@ssw0rd12345" 
Subject="yvradsmisfcluster.westus.cloudapp.azure.com" 
VaultName="yvradsmi-vault" 
VmPassword="P@ssw0rd12345"
VmUserName="yvradsmi"

# Create resource group
az group create --name $ResourceGroupName --location $Location 

# Create secure five node Linux cluster. Creates a key vault in a resource group
# and creates a certficate in the key vault. The certificate's subject name must match 
# the domain that you use to access the Service Fabric cluster.  The certificate is downloaded locally.
az sf cluster create --resource-group $ResourceGroupName --location $Location --certificate-output-folder . --certificate-password $Password --certificate-subject-name $Subject --cluster-name $ClusterName --cluster-size 1 --os UbuntuServer1604 --vault-name $VaultName --vault-resource-group $ResourceGroupName --vm-password $VmPassword --vm-user-name $VmUserName
