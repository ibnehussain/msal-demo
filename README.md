# MSAL Demo Applications

This repository contains demonstration applications showing how to authenticate to Azure Blob Storage using Microsoft Authentication Library (MSAL) with different authentication methods.

## Applications

### 1. ConsoleApp.ManagedIdentity
Demonstrates Azure Blob Storage access using **Managed Identity** authentication.
- Uses `DefaultAzureCredential` for seamless authentication
- Works in Azure App Service, Azure Functions, Azure VMs, and local development
- No secrets required in code

### 2. ConsoleApp.AppRegistration  
Demonstrates Azure Blob Storage access using **Entra ID App Registration** authentication.
- Uses `ClientSecretCredential` for service-to-service authentication
- Suitable for applications running outside Azure or requiring specific identity control
- Uses environment variables for secure credential management

## Features

- ✅ **Production-ready code** with comprehensive error handling
- ✅ **Security best practices** for credential management
- ✅ **Detailed logging** and user-friendly console output
- ✅ **Cross-environment support** (local development, Azure deployment)
- ✅ **Comprehensive Azure Blob Storage operations** (list containers, analyze blobs, upload/download)

## Quick Start

### 1. Clone Repository
```bash
git clone https://github.com/ibnehussain/msal-demo.git
cd msal-demo
```

### 2. Install Dependencies
```bash
# Restore packages for both projects
dotnet restore ConsoleApp.ManagedIdentity/ConsoleApp.ManagedIdentity.csproj
dotnet restore ConsoleApp.AppRegistration/ConsoleApp.AppRegistration.csproj
```

### 3. Run Applications

Each application folder contains its own README.md with detailed setup and usage instructions.

#### Managed Identity App
```bash
cd ConsoleApp.ManagedIdentity
dotnet build
dotnet run -- --storage-account <account-name> --container <container-name>
```

#### App Registration App
```bash
cd ConsoleApp.AppRegistration
# Set environment variables first (see README.md)
dotnet build
dotnet run
```

## Prerequisites

### Install .NET 8.0 SDK

#### Windows
```powershell
# Option 1: Download from Microsoft
# Visit: https://dotnet.microsoft.com/download/dotnet/8.0

# Option 2: Using Chocolatey
choco install dotnet-8.0-sdk

# Option 3: Using winget
winget install Microsoft.DotNet.SDK.8
```

#### Linux (Ubuntu/Debian)
```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update

# Install .NET SDK
sudo apt install -y dotnet-sdk-8.0
```

#### macOS
```bash
# Option 1: Using Homebrew
brew install dotnet

# Option 2: Download from Microsoft
# Visit: https://dotnet.microsoft.com/download/dotnet/8.0
```

### Verify Installation
```bash
dotnet --version
# Should output: 8.0.x
```

### Install Project Dependencies

After cloning the repository, restore NuGet packages for each project:

```bash
# For Managed Identity app
cd ConsoleApp.ManagedIdentity
dotnet restore

# For App Registration app  
cd ../ConsoleApp.AppRegistration
dotnet restore
```

### Azure Requirements
- Azure Storage Account with appropriate RBAC permissions
- For Managed Identity: Azure resource with Managed Identity enabled
- For App Registration: Entra ID App Registration with client secret

## RBAC Permissions Required

Grant one of these roles to your identity on the storage account:
- **Storage Blob Data Reader** (minimum for read operations)
- **Storage Blob Data Contributor** (for read/write operations)

## Architecture

Both applications use the Azure SDK for .NET with:
- **Azure.Identity** for authentication
- **Azure.Storage.Blobs** for blob operations
- **Microsoft.Extensions.Configuration** for configuration management

## Security

- ❌ No secrets in source code
- ✅ Environment variable configuration
- ✅ Proper credential lifecycle management
- ✅ Least privilege access patterns

## Resources

- [Azure Identity Documentation](https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme)
- [Azure Storage Blobs SDK](https://docs.microsoft.com/dotnet/api/overview/azure/storage.blobs-readme)
- [Managed Identity Best Practices](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [Azure RBAC for Storage](https://docs.microsoft.com/azure/storage/blobs/assign-azure-role-data-access)