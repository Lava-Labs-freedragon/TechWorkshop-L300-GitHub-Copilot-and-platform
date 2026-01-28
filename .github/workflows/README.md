# GitHub Actions deployment

This workflow builds the .NET app as a container, pushes it to Azure Container Registry, and deploys it to Azure App Service for Containers.

## Required GitHub secrets

- **AZURE_CREDENTIALS**: Service principal credentials JSON with access to the resource group.
  - Example command to create:
    - `az ad sp create-for-rbac --name "github-actions-sp" --role contributor --scopes /subscriptions/<subscription-id>/resourceGroups/<resource-group> --json-auth`

## Required GitHub variables

- **AZURE_CONTAINER_REGISTRY_NAME**: ACR name (without `.azurecr.io`).
- **AZURE_APP_SERVICE_NAME**: App Service name (e.g., `azapp...`).

## Notes

- The Dockerfile is located at `src/Dockerfile`.
- The image is tagged with both `${{ github.sha }}` and `latest`.
