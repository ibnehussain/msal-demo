# Lab: Run a .NET Console App on an Azure VM that Accesses Azure Blob Storage (App Registration)

## 1. Lab Overview and Objective

This lab walks you through running a .NET 8 console application on an Azure Virtual Machine (VM) that accesses Azure Blob Storage using Entra ID App Registration (client secret) authentication. The objective is to teach how to:

- Create and configure an Azure Storage account and container
- Create an Entra ID App Registration and a client secret
- Assign RBAC permissions so the App Registration can read blobs
- Provision an Azure VM and install the .NET SDK
- Configure the application with environment variables and run it on the VM

Target audience: beginners and classroom learners who want hands-on experience with service-to-service authentication using App Registrations.

---

## 2. Architecture Diagram

A flow diagram describing the authentication and storage access is included in the repository:

- App Registration flow and storage operations: [ConsoleApp.AppRegistration/FLOW_DIAGRAM.md](ConsoleApp.AppRegistration/FLOW_DIAGRAM.md)
- Managed Identity flow (reference): [ConsoleApp.ManagedIdentity/FLOW_DIAGRAM.md](ConsoleApp.ManagedIdentity/FLOW_DIAGRAM.md)

These diagrams use Mermaid syntax and visualize configuration, authentication, token retrieval, and blob operations.

---

## 3. Prerequisites

Before you begin, ensure you have the following:

- An Azure subscription with permission to create resources
- Azure CLI installed locally (for lab setup): https://docs.microsoft.com/cli/azure/install-azure-cli
- Git installed locally
- Basic familiarity with Azure Portal and terminal commands
- (Optional) An SSH client for Linux VMs or RDP for Windows VMs

---

## 4. Azure Setup Steps

This section shows how to prepare Azure resources used by the lab.

> Tip: You can perform these steps in the Azure Portal or with the Azure CLI. CLI examples are shown below.

### 4.1 Create an Azure Resource Group (recommended)

```bash
# Set variables
LOCATION=eastus
RG_NAME=msal-demo-rg

az group create --name $RG_NAME --location $LOCATION
```

### 4.2 Create an Azure Storage Account

```bash
STORAGE_NAME=<unique_storage_account_name>

az storage account create \
  --name $STORAGE_NAME \
  --resource-group $RG_NAME \
  --location $LOCATION \
  --kind StorageV2 \
  --sku Standard_LRS
```

Note: Storage account names must be globally unique and lowercase.

### 4.3 Create a Blob Container and Upload a Sample File

```bash
CONTAINER_NAME=documents

# Create container
az storage container create \
  --name $CONTAINER_NAME \
  --account-name $STORAGE_NAME

# Upload a sample file (using an account key to upload one-time)
SAMPLE_FILE_PATH=./sample.txt
echo "Hello from MSAL lab" > $SAMPLE_FILE_PATH

# Get storage account key
ACCOUNT_KEY=$(az storage account keys list --resource-group $RG_NAME --account-name $STORAGE_NAME --query "[0].value" -o tsv)

az storage blob upload \
  --account-name $STORAGE_NAME \
  --account-key $ACCOUNT_KEY \
  --container-name $CONTAINER_NAME \
  --name sample.txt \
  --file $SAMPLE_FILE_PATH
```

### 4.4 Create an Entra ID App Registration

```bash
APP_NAME=BlobStorageAppRegistration

# Create the app registration
APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
echo "App (client) ID: $APP_ID"

# Create a service principal for the app (creates the service principal object)
az ad sp create --id $APP_ID || true
```

### 4.5 Generate a Client Secret

```bash
# Create a new client secret (valid for 12 months by default)
SECRET_NAME=BlobStorageSecret
CLIENT_SECRET=$(az ad app credential reset --id $APP_ID --append --credential-description $SECRET_NAME --query password -o tsv)

# Do NOT commit this value. Store it securely (e.g., copy to a secure notes app for lab use).
echo "Client secret created (store it securely)."
```

### 4.6 Capture Tenant ID and Client ID

```bash
TENANT_ID=$(az account show --query tenantId -o tsv)
CLIENT_ID=$APP_ID

echo "Tenant ID: $TENANT_ID"
echo "Client ID: $CLIENT_ID"
```

### 4.7 Assign RBAC Role: Storage Blob Data Reader

Assign the `Storage Blob Data Reader` role to the service principal on the storage account so it can read blobs.

```bash
# Get service principal object id
SP_OBJECT_ID=$(az ad sp show --id $CLIENT_ID --query id -o tsv)

# Assign role at storage account scope
SCOPE=$(az storage account show --name $STORAGE_NAME --resource-group $RG_NAME --query id -o tsv)

az role assignment create \
  --assignee-object-id $SP_OBJECT_ID \
  --assignee $CLIENT_ID \
  --role "Storage Blob Data Reader" \
  --scope $SCOPE
```

It can take a few minutes for the role assignment to propagate.

---

## 5. Virtual Machine Setup

You will deploy a VM to run the .NET console app. This lab does not require a Managed Identity on the VM because authentication uses an App Registration (client secret).

### 5.1 Create an Azure VM (Windows or Linux)

Windows example (using RDP):

```bash
VM_NAME=msal-demo-vm
az vm create \
  --resource-group $RG_NAME \
  --name $VM_NAME \
  --image Win2022Datacenter \
  --admin-username azureuser \
  --admin-password "P@ssw0rd1234!" \
  --size Standard_B2s

# Open RDP port
az vm open-port --resource-group $RG_NAME --name $VM_NAME --port 3389
```

Linux example (using SSH):

```bash
VM_NAME=msal-demo-vm
az vm create \
  --resource-group $RG_NAME \
  --name $VM_NAME \
  --image UbuntuLTS \
  --admin-username azureuser \
  --generate-ssh-keys \
  --size Standard_B2s

# Open SSH port
az vm open-port --resource-group $RG_NAME --name $VM_NAME --port 22
```

### 5.2 Install .NET SDK and Git on the VM

Connect to the VM via RDP (Windows) or SSH (Linux) and run the following.

Windows (PowerShell as Administrator):

```powershell
# Install Chocolatey (if not installed)
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))

# Install .NET 8 SDK and Git
choco install -y dotnet-8.0-sdk git

# Verify
dotnet --version
git --version
```

Linux (Ubuntu):

```bash
# Update and install prerequisites
sudo apt update && sudo apt install -y wget apt-transport-https software-properties-common

# Install Microsoft package signing key and repo for dotnet
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update

# Install .NET SDK and git
sudo apt install -y dotnet-sdk-8.0 git

# Verify
dotnet --version
git --version
```

No managed identity is required for this lab because authentication uses the App Registration.

---

## 6. Application Setup on VM

These steps run on the VM.

### 6.1 Clone the GitHub Repository

```bash
# Replace with the repository URL
git clone https://github.com/ibnehussain/msal-demo.git
cd msal-demo/ConsoleApp.AppRegistration
```

### 6.2 Configure Authentication Using Environment Variables

Set the required environment variables on the VM before running the application. Do NOT commit secrets to source control.

Windows (PowerShell):

```powershell
$env:AZURE_TENANT_ID = "<your-tenant-id>"
$env:AZURE_CLIENT_ID = "<your-client-id>"
$env:AZURE_CLIENT_SECRET = "<your-client-secret>"
$env:STORAGE_ACCOUNT_NAME = "<your-storage-account>"
$env:CONTAINER_NAME = "documents"

# Optionally persist using system environment for current user (PowerShell):
setx AZURE_TENANT_ID $env:AZURE_TENANT_ID
setx AZURE_CLIENT_ID $env:AZURE_CLIENT_ID
setx AZURE_CLIENT_SECRET $env:AZURE_CLIENT_SECRET
setx STORAGE_ACCOUNT_NAME $env:STORAGE_ACCOUNT_NAME
setx CONTAINER_NAME $env:CONTAINER_NAME
```

Linux (bash):

```bash
export AZURE_TENANT_ID="<your-tenant-id>"
export AZURE_CLIENT_ID="<your-client-id>"
export AZURE_CLIENT_SECRET="<your-client-secret>"
export STORAGE_ACCOUNT_NAME="<your-storage-account>"
export CONTAINER_NAME="documents"

# To persist for your shell, add the export lines to ~/.bashrc or ~/.profile
```

> Security note: For production consider using Azure Key Vault to store client secrets and inject them into your app at runtime.

### 6.3 Build and Run the .NET Console App

```bash
# From the ConsoleApp.AppRegistration folder
dotnet restore
dotnet build --configuration Release

dotnet run --configuration Release
```

The app reads the environment variables, authenticates using the App Registration, connects to the storage account, lists containers and blobs, and displays blob metadata.

---

## 7. Expected Output and Validation

When the application runs successfully, you should see console output similar to:

```
Azure Blob Storage - App Registration Authentication Demo
======================================================
Tenant ID: <your-tenant-id>
Client ID: <your-client-id>
Storage Account: <your-storage-account>
Container: documents

✓ Initialized BlobServiceClient for account: <your-storage-account>
  Authentication: App Registration (Client ID: <your-client-id>)

--- Testing App Registration Authentication ---
1. Authenticating and getting storage account information...
   ✓ Successfully authenticated with App Registration
   Account Kind: StorageV2
   SKU Name: Standard_LRS

2. Listing containers...
   - documents
     Created: 2025-xx-xx ...

4. Listing blobs in container 'documents'...
   📄 sample.txt
      Size: 21 B
      Last Modified: ...

✅ All tests completed successfully!
```

Validation checklist:

- The application prints the tenant & client IDs you configured
- `GetAccountInfo` succeeds (no 401/403)
- The sample blob `sample.txt` is listed under the target container

---

## 8. Common Errors and Troubleshooting Tips

- Authentication failed (401):
  - Verify `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, and `AZURE_CLIENT_SECRET` are correct and the secret has not expired.
  - Confirm the App Registration has a service principal: `az ad sp show --id <client-id>`

- Authorization / Access Denied (403):
  - Confirm you assigned `Storage Blob Data Reader` to the App Registration's service principal on the storage account scope.
  - Wait a few minutes after assignment for propagation.

- Storage account not found (404):
  - Check the `STORAGE_ACCOUNT_NAME` value and ensure the account exists in the same subscription/region used above.
  - Confirm network rules or firewall settings on the storage account are not blocking access.

- Network / TLS errors on VM:
  - Ensure the VM has outbound network access to Azure endpoints.
  - Check system time on the VM; skewed time can break authentication (ensure NTP sync).

- Application crashes or missing dependencies:
  - Verify .NET SDK installation with `dotnet --info`.
  - Ensure the repo is cloned and you are in `ConsoleApp.AppRegistration` before building.

---

## 9. Cleanup Steps

To avoid ongoing charges, delete resources you created for this lab.

```bash
# Delete resource group and all contained resources
az group delete --name $RG_NAME --yes --no-wait
```

If you created any local files on your workstation (like `sample.txt`), delete them as appropriate.

---

## Appendix: Helpful Commands

- Show role assignments for the storage account:
```bash
az role assignment list --scope $SCOPE --output table
```

- Show the service principal for the app:
```bash
az ad sp show --id $CLIENT_ID
```

- Check role assignment propagation (may take a few minutes):
```bash
az role assignment list --assignee $CLIENT_ID --scope $SCOPE --query "[].roleDefinitionName" -o tsv
```

---

If you'd like, I can also add a quick PowerShell or Bash script to automate the Azure setup steps for this lab. Just tell me which environment you prefer (Windows PowerShell or Bash).