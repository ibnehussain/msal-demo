# Azure Blob Storage with Managed Identity - Console Application

This console application demonstrates how to connect to Azure Blob Storage using Managed Identity authentication with the Azure SDK for .NET.

## Features

- **Managed Identity Authentication**: Uses `DefaultAzureCredential` for seamless authentication across different environments
- **Production-Ready Code**: Includes comprehensive error handling, logging, and configuration
- **Azure Blob Storage Operations**: Lists containers, retrieves blob information, and includes optional upload/download methods
- **Cross-Environment Support**: Works in local development, Azure App Service, Azure Functions, Azure VMs, etc.

## Prerequisites

### For Local Development
1. Install [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
2. Sign in to Azure: `az login`
3. Ensure your Azure account has appropriate permissions on the target storage account

### For Azure Deployment
1. Enable System-assigned or User-assigned Managed Identity on your Azure resource
2. Assign appropriate RBAC roles to the Managed Identity

## Required RBAC Roles

The Managed Identity needs one of the following roles on the storage account:

- **Storage Blob Data Reader** (minimum) - for read operations
- **Storage Blob Data Contributor** - for read/write operations
- **Storage Blob Data Owner** - for full control

### Assigning Roles via Azure CLI

```bash
# Get your storage account resource ID
az storage account show --name <storage-account-name> --resource-group <resource-group-name> --query id --output tsv

# Assign role to Managed Identity (replace with your values)
az role assignment create \
  --role "Storage Blob Data Reader" \
  --assignee <managed-identity-principal-id> \
  --scope <storage-account-resource-id>
```

## Configuration

### Method 1: Command Line Arguments
```bash
dotnet run -- --storage-account mystorageaccount --container mycontainer
```

### Method 2: Update Constants in Code
Edit the constants in `Program.cs`:
```csharp
private const string DefaultStorageAccountName = "your-storage-account-name";
private const string DefaultContainerName = "your-container-name";
```

## Usage

### Build and Run
```bash
dotnet build
dotnet run -- --storage-account <your-storage-account> --container <your-container>
```

### Example Output
```
Azure Blob Storage - Managed Identity Demo
==========================================
Initialized BlobServiceClient for account: mystorageaccount

--- Testing Managed Identity Connection ---
1. Getting storage account information...
   ✓ Connected successfully to storage account
   Account Kind: StorageV2
   SKU Name: Standard_LRS

2. Listing containers...
   - documents (Created: 2024-01-15 10:30:25 UTC)
   - images (Created: 2024-01-20 14:22:10 UTC)

3. Working with container 'documents'...
   ✓ Container 'documents' exists

4. Listing blobs in container 'documents'...
   - file1.txt
     Size: 1024 bytes
     Last Modified: 2024-01-25 09:15:30 UTC
     Content Type: text/plain

✓ All tests completed successfully!
```

## Architecture

### DefaultAzureCredential Chain
The application uses `DefaultAzureCredential` which attempts authentication in this order:

1. **Environment Variables** - For service principal authentication
2. **Managed Identity** - When running on Azure services
3. **Azure CLI** - For local development
4. **Visual Studio/VS Code** - For IDE-based development

### Key Components

- **Program.cs**: Main entry point with argument parsing and error handling
- **BlobStorageService**: Encapsulates Azure Blob Storage operations
- **Production Features**:
  - Comprehensive error handling with specific Azure exception handling
  - Structured logging and user-friendly messages
  - Command-line argument support
  - Async/await patterns throughout

## Error Handling

The application includes specific handling for common scenarios:

- **403 Forbidden**: Indicates insufficient RBAC permissions
- **404 Not Found**: Storage account doesn't exist or isn't accessible
- **General Exceptions**: Wrapped with user-friendly messages

## Deployment Scenarios

### Azure App Service
1. Enable System-assigned Managed Identity in the App Service
2. Assign RBAC roles to the Managed Identity
3. Deploy the application

### Azure Functions
1. Enable Managed Identity in the Function App
2. Configure RBAC roles
3. Deploy as a Function or use in a Timer-triggered function

### Azure Virtual Machine
1. Enable System-assigned Managed Identity on the VM
2. Configure RBAC roles
3. Run the application on the VM

### Azure Container Instances
1. Create ACI with Managed Identity enabled
2. Configure RBAC roles
3. Deploy the containerized application

## Security Best Practices

1. **Principle of Least Privilege**: Grant only the minimum required permissions
2. **Use System-assigned Managed Identity** when possible for better security
3. **Monitor Access**: Enable Azure Monitor and Log Analytics for audit trails
4. **Network Security**: Consider using Private Endpoints for storage access

## Extending the Application

The `BlobStorageService` class includes additional methods for:

- `UploadBlobAsync()`: Upload content to a blob
- `DownloadBlobAsync()`: Download blob content

These can be called from the main program or extended further based on your requirements.

## Troubleshooting

### Common Issues

1. **Access Denied (403)**
   - Verify RBAC role assignments
   - Check if Managed Identity is enabled
   - Ensure the correct identity is assigned

2. **Storage Account Not Found (404)**
   - Verify the storage account name
   - Check network access rules
   - Confirm the storage account exists in the correct subscription

3. **Authentication Timeout**
   - Check network connectivity
   - Verify Azure metadata endpoint accessibility (for Managed Identity)

### Debug Steps

1. Enable detailed logging by setting environment variable:
   ```bash
   set AZURE_LOG_LEVEL=verbose
   ```

2. Verify Managed Identity is working:
   ```bash
   curl "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://storage.azure.com/" -H "Metadata: true"
   ```

## Resources

- [Azure Identity SDK](https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme)
- [Azure Storage Blobs SDK](https://docs.microsoft.com/dotnet/api/overview/azure/storage.blobs-readme)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [Azure RBAC for Storage](https://docs.microsoft.com/azure/storage/blobs/assign-azure-role-data-access)