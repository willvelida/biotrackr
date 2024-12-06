#!/bin/bash

# Variables
ACR_NAME=${ACR_NAME}
RESOURCE_GROUP=${RESOURCE_GROUP}

# Get the list of repositories
repositories=$(az acr repository list --name $ACR_NAME --resource-group $RESOURCE_GROUP --output tsv)

# Loop through each repository and purge old images
for repository in $repositories; do
    echo "Purging old images in repository: $repository"
    az acr run --registry $ACR_NAME --cmd "acr purge --filter '$repository:.*' --ago 0d --keep 3 --untagged" /dev/null
done