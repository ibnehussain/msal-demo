# Azure Blob Storage with App Registration - Console Application

This console application demonstrates how to authenticate to Azure Blob Storage using an **Entra ID App Registration** (formerly Azure Active Directory App Registration) with client secret authentication.

## Features

- **App Registration Authentication**: Uses `ClientSecretCredential` for service-to-service authentication
- **Production-Ready Configuration**: Multiple configuration sources (JSON files, environment variables, command line)
- **Comprehensive Error Handling**: Specific handling for authentication and authorization failures
- **Detailed Blob Operations**: Lists containers, analyzes blob properties, and includes upload/download capabilities
- **Security Best Practices**: Secure credential handling and proper RBAC configuration

## Prerequisites

### 1. Create an Entra ID App Registration

#### Using Azure Portal:
1. Go to **Azure Portal** > **Entra ID** > **App registrations**
2. Click **"New registration"**
3. Provide:
   - **Name**: `BlobStorageAppRegistration` (or your preferred name)
   - **Supported account types**: Select appropriate option (typically "Accounts in this organizational directory only")
   - **Redirect URI**: Leave blank for console app
4. Click **"Register"**

#### Using Azure CLI:
```bash
# Create the app registration
az ad app create --display-name "BlobStorageAppRegistration"

# Get the app ID (you'll need this)
az ad app list --display-name "BlobStorageAppRegistration" --query "[0].appId" -o tsv
```

### 2. Create a Client Secret

#### Using Azure Portal:
1. Go to your **App registration** > **Certificates & secrets**
2. Click **"New client secret"**
3. Provide:
   - **Description**: `BlobStorageSecret`
   - **Expires**: Choose appropriate expiration (recommend 6-12 months for production)
4. Click **"Add"**
5. **IMPORTANT**: Copy the secret value immediately (it won't be shown again)

#### Using Azure CLI:
```bash
# Create a client secret (replace YOUR-APP-ID with actual app ID)
az ad app credential reset --id YOUR-APP-ID --append --display-name "BlobStorageSecret"
```

### 3. Assign RBAC Permissions

Your App Registration needs appropriate permissions on the storage account:

#### Using Azure Portal:
1. Go to **Azure Portal** > **Storage accounts** > Select your storage account
2. Click **"Access Control (IAM)"** in the left menu
3. Click **"+ Add"** > **"Add role assignment"**
4. In the **Role** tab:
    - Select **"Storage Blob Data Reader"** (for read operations) or **"Storage Blob Data Contributor"** (for read/write operations)
    - Click **"Next"**
5. In the **Members** tab:
    - Select **"Assign access to: User, group, or service principal"**
    - Click **"+ Select members"**
    - Search for your App Registration name (e.g., "BlobStorageAppRegistration")
    - Select it and click **"Select"**
    - Click **"Next"**
6. In the **Review + assign** tab:
    - Review the settings
    - Click **"Review + assign"**

#### Using Azure CLI:
```bash
# Get your app's object ID (service principal)
az ad sp show --id YOUR-CLIENT-ID --query "id" -o tsv

# Assign Storage Blob Data Reader role (minimum for read operations)
az role assignment create \
  --role "Storage Blob Data Reader" \
  --assignee YOUR-SERVICE-PRINCIPAL-OBJECT-ID \
  --scope "/subscriptions/YOUR-SUBSCRIPTION-ID/resourceGroups/YOUR-RG/providers/Microsoft.Storage/storageAccounts/YOUR-STORAGE-ACCOUNT"

# For write operations, assign Storage Blob Data Contributor instead
az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee YOUR-SERVICE-PRINCIPAL-OBJECT-ID \
  --scope "/subscriptions/YOUR-SUBSCRIPTION-ID/resourceGroups/YOUR-RG/providers/Microsoft.Storage/storageAccounts/YOUR-STORAGE-ACCOUNT"
```

#### Finding Required IDs for CLI:
```bash
# Get your subscription ID
az account show --query "id" -o tsv

# Get your storage account resource group and name
az storage account show --name YOUR-STORAGE-ACCOUNT --query "{resourceGroup:resourceGroup, name:name}"

# Get your app registration's service principal object ID
az ad sp list --display-name "BlobStorageAppRegistration" --query "[0].id" -o tsv
```
```

## Configuration

### Required Values
- **AZURE_TENANT_ID**: Your Azure tenant ID (Directory ID)
- **AZURE_CLIENT_ID**: App registration's Application (client) ID  
- **AZURE_CLIENT_SECRET**: The client secret value you created
- **STORAGE_ACCOUNT_NAME**: Name of your Azure Storage account
- **CONTAINER_NAME**: Container name to test with

### Configuration Methods

#### Method 1: Environment Variables (Recommended for Production)
```bash
# Windows
set AZURE_TENANT_ID=12345678-1234-1234-1234-123456789012
set AZURE_CLIENT_ID=87654321-4321-4321-4321-210987654321
set AZURE_CLIENT_SECRET=your-secret-value-here
set STORAGE_ACCOUNT_NAME=mystorageaccount
set CONTAINER_NAME=documents

# Linux/Mac
export AZURE_TENANT_ID=12345678-1234-1234-1234-123456789012
export AZURE_CLIENT_ID=87654321-4321-4321-4321-210987654321
export AZURE_CLIENT_SECRET=your-secret-value-here
export STORAGE_ACCOUNT_NAME=mystorageaccount
export CONTAINER_NAME=documents
```

#### Method 2: Local Configuration File (Development)
Create `appsettings.local.json` (this file is gitignored for security):

```json
{
  "AZURE_TENANT_ID": "12345678-1234-1234-1234-123456789012",
  "AZURE_CLIENT_ID": "87654321-4321-4321-4321-210987654321", 
  "AZURE_CLIENT_SECRET": "your-secret-value-here",
  "STORAGE_ACCOUNT_NAME": "mystorageaccount",
  "CONTAINER_NAME": "documents"
}
```

#### Method 3: Command Line Arguments
```bash
dotnet run -- --AZURE_TENANT_ID=your-tenant --AZURE_CLIENT_ID=your-client-id --AZURE_CLIENT_SECRET=your-secret --STORAGE_ACCOUNT_NAME=mystorageaccount --CONTAINER_NAME=documents
```

## Usage

### Build and Run
```bash
# Restore packages
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

### Example Output
```
Azure Blob Storage - App Registration Authentication Demo
======================================================
Tenant ID: 12345678-1234-1234-1234-123456789012
Client ID: 87654321-4321-4321-4321-210987654321
Storage Account: mystorageaccount
Container: documents

✓ Initialized BlobServiceClient for account: mystorageaccount
  Authentication: App Registration (Client ID: 87654321-4321-4321-4321-210987654321)

--- Testing App Registration Authentication ---
1. Authenticating and getting storage account information...
   ✓ Successfully authenticated with App Registration
   Account Kind: StorageV2
   SKU Name: Standard_LRS

2. Listing containers...
   - documents
     Created: 2024-01-15 10:30:25 UTC
     Public Access: None
   - images  
     Created: 2024-01-20 14:22:10 UTC
     Public Access: None
   Total containers: 2

3. Using specified container 'documents'...

4. Listing blobs in container 'documents'...
   📄 report.pdf
      Size: 2.3 MB
      Last Modified: 2024-01-25 09:15:30 UTC
      Content Type: application/pdf
      ETag: "0x8DC1E2F3B4A5C6D7"

   📄 data.csv
      Size: 156.2 KB  
      Last Modified: 2024-01-25 10:22:15 UTC
      Content Type: text/csv
      ETag: "0x8DC1E2F3B4A5C6D8"

   📊 Summary: 2+ blobs, Total size: 2.5 MB

5. Testing advanced operations...
   ✓ Container properties retrieved
     Last Modified: 2024-01-25 10:22:15 UTC
     ETag: "0x8DC1E2F3B4A5C6D9"
   ✓ Storage service properties accessible
     Default Service Version: 2023-11-03

✅ All tests completed successfully!

💡 Your App Registration is properly configured for Azure Blob Storage access.
```

## Security Best Practices

### 1. Client Secret Management
- **Never commit secrets to source control**
- **Use Azure Key Vault** for production secret storage
- **Rotate secrets regularly** (every 6-12 months)
- **Use different secrets for different environments**

### 2. RBAC Configuration
- **Principle of Least Privilege**: Grant minimum required permissions
- **Use built-in roles** when possible:
  - `Storage Blob Data Reader` - Read-only access
  - `Storage Blob Data Contributor` - Read/write access  
  - `Storage Blob Data Owner` - Full control (rarely needed)

### 3. Network Security
- **Configure storage account firewall** to restrict access
- **Use Private Endpoints** for enhanced security
- **Enable storage account logging** for audit trails

### 4. Application Security
- **Validate configuration** at startup
- **Handle authentication errors** gracefully
- **Log security events** for monitoring
- **Use HTTPS only** for all communications

## Certificate-Based Authentication (Alternative)

For enhanced security, you can use certificate authentication instead of client secrets:

```csharp
// Replace ClientSecretCredential with ClientCertificateCredential
var certificate = new X509Certificate2("path/to/certificate.pfx", "password");
var credential = new ClientCertificateCredential(
    tenantId: config.TenantId,
    clientId: config.ClientId, 
    clientCertificate: certificate);
```

## Troubleshooting

### Authentication Issues (401)

**Problem**: "Authentication failed" error
**Solutions**:
- Verify tenant ID, client ID, and client secret are correct
- Check if client secret has expired
- Ensure app registration exists and is enabled

### Authorization Issues (403)

**Problem**: "Access denied" error  
**Solutions**:
- Verify RBAC role assignments on storage account
- Check if service principal exists (may take a few minutes after creation)
- Ensure correct scope for role assignment

### Storage Account Issues (404)

**Problem**: "Storage account not found"
**Solutions**:
- Verify storage account name is correct
- Check storage account exists in expected subscription
- Verify network access rules allow access

### Configuration Issues

**Problem**: "Missing required configuration"
**Solutions**:
- Verify all required environment variables are set
- Check appsettings.local.json file exists and has correct format
- Validate command line arguments format

## Deployment Scenarios

### Azure App Service
1. Configure **Application Settings** in the App Service with your credentials
2. Deploy the application
3. App Service will automatically use the configured settings

### Azure Functions  
1. Set **Application Settings** in Function App configuration
2. Deploy as Console App or integrate into Function code
3. Use Timer trigger for scheduled operations

### Azure Container Instances
1. Set environment variables in container configuration
2. Deploy containerized application
3. Use Azure Key Vault integration for secrets

### On-Premises/Hybrid
1. Use local configuration files or environment variables
2. Ensure network connectivity to Azure
3. Consider VPN or ExpressRoute for production workloads

## Extending the Application

The `BlobStorageService` class provides methods for:
- `UploadBlobAsync()` - Upload content to blobs
- `DownloadBlobAsync()` - Download blob content
- Container management operations
- Blob metadata operations

Example usage:
```csharp
var blobService = new BlobStorageService(appConfig);
await blobService.UploadBlobAsync("mycontainer", "test.txt", "Hello World!");
var content = await blobService.DownloadBlobAsync("mycontainer", "test.txt");
```

## Resources

- [Entra ID App Registrations](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app)
- [Azure Identity SDK](https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme)
- [Azure Storage Blobs SDK](https://docs.microsoft.com/dotnet/api/overview/azure/storage.blobs-readme)
- [Azure RBAC for Storage](https://docs.microsoft.com/azure/storage/blobs/assign-azure-role-data-access)
- [Client Secret Best Practices](https://docs.microsoft.com/azure/active-directory/develop/howto-create-service-principal-portal)