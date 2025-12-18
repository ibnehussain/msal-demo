using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp.AppRegistration;

/// <summary>
/// Console application demonstrating Azure Blob Storage access using Entra ID App Registration authentication.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("Azure Blob Storage - App Registration Authentication Demo");
            Console.WriteLine("======================================================");

            // Load configuration from environment variables
            var configuration = LoadConfiguration();
            var appConfig = new AppRegistrationConfig(configuration);
            
            // Validate that all required environment variables are set
            if (!appConfig.IsValid())
            {
                Console.WriteLine("🔍 Checking environment variables...");
                CheckEnvironmentVariables();
                DisplayConfigurationHelp();
                return 1;
            }

            Console.WriteLine($"Tenant ID: {appConfig.TenantId}");
            Console.WriteLine($"Client ID: {appConfig.ClientId}");
            Console.WriteLine($"Storage Account: {appConfig.StorageAccountName}");
            Console.WriteLine($"Container: {appConfig.ContainerName}");
            Console.WriteLine();

            // Create the blob service with App Registration authentication
            var blobService = new BlobStorageService(appConfig);
            
            // Test connection and list blobs
            await blobService.TestConnectionAsync();
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            
            Console.WriteLine("\nFor troubleshooting help, see the README.md file.");
            return 1;
        }
    }

    private static IConfiguration LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .AddEnvironmentVariables() // Primary source - environment variables
            .AddCommandLine(Environment.GetCommandLineArgs()) // Secondary source - command line args
            .Build();
    }

    private static void DisplayConfigurationHelp()
    {
        Console.WriteLine("❌ Missing required configuration. Please set the following environment variables:");
        Console.WriteLine();
        Console.WriteLine("Required Environment Variables:");
        Console.WriteLine("  AZURE_TENANT_ID=your-tenant-id");
        Console.WriteLine("  AZURE_CLIENT_ID=your-client-id");
        Console.WriteLine("  AZURE_CLIENT_SECRET=your-client-secret");
        Console.WriteLine("  STORAGE_ACCOUNT_NAME=your-storage-account");
        Console.WriteLine("  CONTAINER_NAME=your-container-name");
        Console.WriteLine();
        Console.WriteLine("Windows Example:");
        Console.WriteLine("  set AZURE_TENANT_ID=12345678-1234-1234-1234-123456789012");
        Console.WriteLine("  set AZURE_CLIENT_ID=87654321-4321-4321-4321-210987654321");
        Console.WriteLine("  set AZURE_CLIENT_SECRET=your-secret-value");
        Console.WriteLine("  set STORAGE_ACCOUNT_NAME=mystorageaccount");
        Console.WriteLine("  set CONTAINER_NAME=documents");
        Console.WriteLine();
        Console.WriteLine("Alternative: Use command line arguments with the same names");
    }

    private static void CheckEnvironmentVariables()
    {
        var requiredVars = new[] 
        {
            "AZURE_TENANT_ID",
            "AZURE_CLIENT_ID", 
            "AZURE_CLIENT_SECRET",
            "STORAGE_ACCOUNT_NAME",
            "CONTAINER_NAME"
        };

        Console.WriteLine("Environment Variable Status:");
        foreach (var varName in requiredVars)
        {
            var value = Environment.GetEnvironmentVariable(varName);
            var status = string.IsNullOrEmpty(value) ? "❌ Not Set" : "✅ Set";
            var displayValue = string.IsNullOrEmpty(value) ? "(not set)" : 
                varName.Contains("SECRET") ? "****" : 
                value.Length > 20 ? $"{value[..8]}..." : value;
            
            Console.WriteLine($"  {varName}: {status} - {displayValue}");
        }
        Console.WriteLine();
    }
}

/// <summary>
/// Configuration class for App Registration authentication settings.
/// </summary>
public class AppRegistrationConfig
{
    public string? TenantId { get; }
    public string? ClientId { get; }
    public string? ClientSecret { get; }
    public string? StorageAccountName { get; }
    public string? ContainerName { get; }

    public AppRegistrationConfig(IConfiguration configuration)
    {
        TenantId = configuration["AZURE_TENANT_ID"];
        ClientId = configuration["AZURE_CLIENT_ID"];
        ClientSecret = configuration["AZURE_CLIENT_SECRET"];
        StorageAccountName = configuration["STORAGE_ACCOUNT_NAME"];
        ContainerName = configuration["CONTAINER_NAME"] ?? "default-container";
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(TenantId) &&
               !string.IsNullOrEmpty(ClientId) &&
               !string.IsNullOrEmpty(ClientSecret) &&
               !string.IsNullOrEmpty(StorageAccountName);
    }
}

/// <summary>
/// Service class for Azure Blob Storage operations using App Registration authentication.
/// </summary>
public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AppRegistrationConfig _config;

    public BlobStorageService(AppRegistrationConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        if (!config.IsValid())
        {
            throw new ArgumentException("Invalid configuration provided", nameof(config));
        }

        // Create the BlobServiceClient using App Registration (Client Secret) authentication
        var blobServiceUri = new Uri($"https://{config.StorageAccountName}.blob.core.windows.net");
        
        // Use ClientSecretCredential for App Registration authentication
        var credential = new ClientSecretCredential(
            tenantId: config.TenantId,
            clientId: config.ClientId,
            clientSecret: config.ClientSecret,
            options: new ClientSecretCredentialOptions
            {
                Retry = { MaxRetries = 3, Delay = TimeSpan.FromSeconds(2) },
                // Enable logging for debugging if needed
                Diagnostics = { IsAccountIdentifierLoggingEnabled = true }
            });

        _blobServiceClient = new BlobServiceClient(blobServiceUri, credential);
        
        Console.WriteLine($"✓ Initialized BlobServiceClient for account: {config.StorageAccountName}");
        Console.WriteLine($"  Authentication: App Registration (Client ID: {config.ClientId})");
    }

    /// <summary>
    /// Tests the connection to the storage account and performs basic operations.
    /// </summary>
    public async Task TestConnectionAsync()
    {
        Console.WriteLine("\n--- Testing App Registration Authentication ---");
        
        try
        {
            // Test 1: Get account info to verify authentication
            Console.WriteLine("1. Authenticating and getting storage account information...");
            var accountInfo = await _blobServiceClient.GetAccountInfoAsync();
            Console.WriteLine($"   ✓ Successfully authenticated with App Registration");
            Console.WriteLine($"   Account Kind: {accountInfo.Value.AccountKind}");
            Console.WriteLine($"   SKU Name: {accountInfo.Value.SkuName}");

            // Test 2: List containers
            Console.WriteLine("\n2. Listing containers...");
            var containers = new List<BlobContainerItem>();
            await foreach (var container in _blobServiceClient.GetBlobContainersAsync())
            {
                containers.Add(container);
                Console.WriteLine($"   - {container.Name}");
                Console.WriteLine($"     Created: {container.Properties.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"     Public Access: {container.Properties.PublicAccess}");
            }

            if (containers.Count == 0)
            {
                Console.WriteLine("   No containers found in the storage account.");
                Console.WriteLine("   Note: The App Registration might not have sufficient permissions.");
                return;
            }

            Console.WriteLine($"   Total containers: {containers.Count}");

            // Test 3: Work with specific container
            var targetContainer = GetTargetContainer(containers);
            if (targetContainer != null)
            {
                await TestContainerOperations(targetContainer.Name);
            }

            // Test 4: Demonstrate additional operations
            await DemonstrateAdvancedOperations();

            Console.WriteLine("\n✅ All tests completed successfully!");
            Console.WriteLine("\n💡 Your App Registration is properly configured for Azure Blob Storage access.");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 401)
        {
            Console.WriteLine($"❌ Authentication failed. Please verify your App Registration credentials:");
            Console.WriteLine($"   - Tenant ID: {_config.TenantId}");
            Console.WriteLine($"   - Client ID: {_config.ClientId}");
            Console.WriteLine($"   - Client Secret: [Check if valid and not expired]");
            Console.WriteLine($"   Error: {ex.Message}");
            throw;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            Console.WriteLine($"❌ Access denied. Please ensure your App Registration has appropriate permissions:");
            Console.WriteLine($"   - Storage Blob Data Reader (minimum for read operations)");
            Console.WriteLine($"   - Storage Blob Data Contributor (for write operations)");
            Console.WriteLine($"   - Or custom role with required permissions");
            Console.WriteLine($"   Error: {ex.Message}");
            throw;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            Console.WriteLine($"❌ Storage account '{_config.StorageAccountName}' not found or not accessible.");
            Console.WriteLine($"   Please verify:");
            Console.WriteLine($"   - Storage account name is correct");
            Console.WriteLine($"   - Storage account exists in the correct subscription");
            Console.WriteLine($"   - Network access rules allow access");
            Console.WriteLine($"   Error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error occurred: {ex.Message}");
            Console.WriteLine($"\nDebugging information:");
            Console.WriteLine($"   Storage Account URI: https://{_config.StorageAccountName}.blob.core.windows.net");
            Console.WriteLine($"   Tenant ID: {_config.TenantId}");
            Console.WriteLine($"   Client ID: {_config.ClientId}");
            throw;
        }
    }

    private BlobContainerItem? GetTargetContainer(List<BlobContainerItem> containers)
    {
        // Try to find the specified container first
        var targetContainer = containers.FirstOrDefault(c => c.Name == _config.ContainerName);
        
        if (targetContainer != null)
        {
            Console.WriteLine($"\n3. Using specified container '{_config.ContainerName}'...");
            return targetContainer;
        }
        
        // If not found, use the first available container
        if (containers.Count > 0)
        {
            targetContainer = containers[0];
            Console.WriteLine($"\n3. Container '{_config.ContainerName}' not found.");
            Console.WriteLine($"   Using first available container '{targetContainer.Name}' for testing...");
            return targetContainer;
        }

        Console.WriteLine($"\n3. No containers available for testing.");
        return null;
    }

    private async Task TestContainerOperations(string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        
        Console.WriteLine($"\n4. Listing blobs in container '{containerName}'...");
        var blobCount = 0;
        var totalSize = 0L;
        
        await foreach (var blob in containerClient.GetBlobsAsync(BlobTraits.All))
        {
            blobCount++;
            totalSize += blob.Properties.ContentLength ?? 0;
            
            Console.WriteLine($"   📄 {blob.Name}");
            Console.WriteLine($"      Size: {FormatFileSize(blob.Properties.ContentLength ?? 0)}");
            Console.WriteLine($"      Last Modified: {blob.Properties.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"      Content Type: {blob.Properties.ContentType}");
            Console.WriteLine($"      ETag: {blob.Properties.ETag}");
            
            if (blob.Properties.AccessTier != null)
            {
                Console.WriteLine($"      Access Tier: {blob.Properties.AccessTier}");
            }
            
            Console.WriteLine();
            
            // Limit output for demo purposes
            if (blobCount >= 5)
            {
                var remainingBlobs = await GetRemainingBlobCount(containerClient, blobCount);
                if (remainingBlobs > 0)
                {
                    Console.WriteLine($"   ... and {remainingBlobs} more blobs");
                }
                break;
            }
        }

        if (blobCount == 0)
        {
            Console.WriteLine("   No blobs found in the container.");
        }
        else
        {
            Console.WriteLine($"   📊 Summary: {blobCount}+ blobs, Total size: {FormatFileSize(totalSize)}");
        }
    }

    private async Task DemonstrateAdvancedOperations()
    {
        Console.WriteLine("\n5. Testing advanced operations...");
        
        try
        {
            // Test container properties
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.ContainerName!);
            var containerExists = await containerClient.ExistsAsync();
            
            if (containerExists.Value)
            {
                var properties = await containerClient.GetPropertiesAsync();
                Console.WriteLine($"   ✓ Container properties retrieved");
                Console.WriteLine($"     Last Modified: {properties.Value.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"     ETag: {properties.Value.ETag}");
                
                if (properties.Value.Metadata.Any())
                {
                    Console.WriteLine($"     Metadata: {properties.Value.Metadata.Count} items");
                }
            }
            
            // Test service properties (if permissions allow)
            try
            {
                var serviceProperties = await _blobServiceClient.GetPropertiesAsync();
                Console.WriteLine($"   ✓ Storage service properties accessible");
                Console.WriteLine($"     Default Service Version: {serviceProperties.Value.DefaultServiceVersion}");
                
                if (serviceProperties.Value.StaticWebsite != null)
                {
                    Console.WriteLine($"     Static Website: {(serviceProperties.Value.StaticWebsite.Enabled ? "Enabled" : "Disabled")}");
                }
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 403)
            {
                Console.WriteLine($"   ⚠️  Service properties not accessible (insufficient permissions)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Some advanced operations failed: {ex.Message}");
        }
    }

    private static async Task<int> GetRemainingBlobCount(BlobContainerClient containerClient, int alreadyCounted)
    {
        var totalCount = 0;
        await foreach (var _ in containerClient.GetBlobsAsync())
        {
            totalCount++;
        }
        return Math.Max(0, totalCount - alreadyCounted);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        var counter = 0;
        var number = (decimal)bytes;
        
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        
        return $"{number:n1} {suffixes[counter]}";
    }

    /// <summary>
    /// Demonstrates uploading a blob with App Registration authentication.
    /// </summary>
    public async Task<bool> UploadBlobAsync(string containerName, string blobName, string content)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            var response = await blobClient.UploadAsync(stream, overwrite: true);

            Console.WriteLine($"✓ Successfully uploaded blob '{blobName}' to container '{containerName}'");
            Console.WriteLine($"  ETag: {response.Value.ETag}");
            Console.WriteLine($"  Last Modified: {response.Value.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to upload blob: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Demonstrates downloading a blob with App Registration authentication.
    /// </summary>
    public async Task<string?> DownloadBlobAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToString();

            Console.WriteLine($"✓ Successfully downloaded blob '{blobName}' from container '{containerName}'");
            Console.WriteLine($"  Size: {FormatFileSize(content.Length)}");
            Console.WriteLine($"  Content Type: {response.Value.Details.ContentType}");
            
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to download blob: {ex.Message}");
            return null;
        }
    }
}
