using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ConsoleApp.ManagedIdentity;

/// <summary>
/// Console application demonstrating Azure Blob Storage access using Managed Identity authentication.
/// </summary>
public class Program
{
    private const string DefaultStorageAccountName = "your-storage-account-name";
    private const string DefaultContainerName = "your-container-name";
    
    public static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("Azure Blob Storage - Managed Identity Demo");
            Console.WriteLine("==========================================");

            // Parse command line arguments or use defaults
            var storageAccountName = GetArgumentValue(args, "--storage-account") ?? DefaultStorageAccountName;
            var containerName = GetArgumentValue(args, "--container") ?? DefaultContainerName;
            
            if (storageAccountName == DefaultStorageAccountName)
            {
                Console.WriteLine("Usage: --storage-account <account-name> [--container <container-name>]");
                Console.WriteLine($"Using default values: Account={storageAccountName}, Container={containerName}");
                Console.WriteLine("Please update these values or pass them as arguments.");
            }

            var blobService = new BlobStorageService(storageAccountName);
            
            // Test connection and list blobs
            await blobService.TestConnectionAsync(containerName);
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            
            return 1;
        }
    }
    
    private static string? GetArgumentValue(string[] args, string argumentName)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(argumentName, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}

/// <summary>
/// Service class for Azure Blob Storage operations using Managed Identity.
/// </summary>
public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _storageAccountName;

    public BlobStorageService(string storageAccountName)
    {
        _storageAccountName = storageAccountName ?? throw new ArgumentNullException(nameof(storageAccountName));
        
        // Create the BlobServiceClient using Managed Identity
        var blobServiceUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
        
        // DefaultAzureCredential automatically handles different credential types:
        // 1. Environment variables (for local development)
        // 2. Managed Identity (when deployed to Azure)
        // 3. Azure CLI (for local development)
        // 4. Visual Studio/VS Code (for local development)
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // Exclude interactive browser credential for headless environments
            ExcludeInteractiveBrowserCredential = true,
            // Set retry timeout
            Retry = { MaxRetries = 3, Delay = TimeSpan.FromSeconds(2) }
        });

        _blobServiceClient = new BlobServiceClient(blobServiceUri, credential);
        
        Console.WriteLine($"Initialized BlobServiceClient for account: {storageAccountName}");
    }

    /// <summary>
    /// Tests the connection to the storage account and performs basic operations.
    /// </summary>
    /// <param name="containerName">The name of the container to test with.</param>
    public async Task TestConnectionAsync(string containerName)
    {
        Console.WriteLine("\n--- Testing Managed Identity Connection ---");
        
        try
        {
            // Test 1: Get account info
            Console.WriteLine("1. Getting storage account information...");
            var accountInfo = await _blobServiceClient.GetAccountInfoAsync();
            Console.WriteLine($"   ✓ Connected successfully to storage account");
            Console.WriteLine($"   Account Kind: {accountInfo.Value.AccountKind}");
            Console.WriteLine($"   SKU Name: {accountInfo.Value.SkuName}");

            // Test 2: List containers
            Console.WriteLine("\n2. Listing containers...");
            var containers = new List<string>();
            await foreach (var container in _blobServiceClient.GetBlobContainersAsync())
            {
                containers.Add(container.Name);
                Console.WriteLine($"   - {container.Name} (Created: {container.Properties.LastModified:yyyy-MM-dd HH:mm:ss} UTC)");
            }

            if (containers.Count == 0)
            {
                Console.WriteLine("   No containers found in the storage account.");
                return;
            }

            // Test 3: Work with specific container
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            
            Console.WriteLine($"\n3. Working with container '{containerName}'...");
            
            // Check if container exists
            var containerExists = await containerClient.ExistsAsync();
            if (!containerExists.Value)
            {
                Console.WriteLine($"   Container '{containerName}' does not exist.");
                
                // Use the first available container for demo
                if (containers.Count > 0)
                {
                    containerName = containers[0];
                    containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                    Console.WriteLine($"   Using existing container '{containerName}' instead.");
                }
                else
                {
                    Console.WriteLine("   No containers available for testing.");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"   ✓ Container '{containerName}' exists");
            }

            // Test 4: List blobs in container
            Console.WriteLine($"\n4. Listing blobs in container '{containerName}'...");
            var blobCount = 0;
            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                blobCount++;
                Console.WriteLine($"   - {blob.Name}");
                Console.WriteLine($"     Size: {blob.Properties.ContentLength} bytes");
                Console.WriteLine($"     Last Modified: {blob.Properties.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"     Content Type: {blob.Properties.ContentType}");
                Console.WriteLine();
                
                // Limit output for demo purposes
                if (blobCount >= 5)
                {
                    var totalBlobs = await GetBlobCountAsync(containerClient);
                    Console.WriteLine($"   ... and {totalBlobs - 5} more blobs");
                    break;
                }
            }

            if (blobCount == 0)
            {
                Console.WriteLine("   No blobs found in the container.");
            }

            Console.WriteLine("\n✓ All tests completed successfully!");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            Console.WriteLine($"❌ Access denied. Please ensure the Managed Identity has appropriate permissions:");
            Console.WriteLine($"   - Storage Blob Data Reader (minimum)");
            Console.WriteLine($"   - Storage Blob Data Contributor (for write operations)");
            Console.WriteLine($"   Error: {ex.Message}");
            throw;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            Console.WriteLine($"❌ Storage account '{_storageAccountName}' not found or not accessible.");
            Console.WriteLine($"   Please verify the storage account name and network access.");
            Console.WriteLine($"   Error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error occurred: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the total count of blobs in a container.
    /// </summary>
    private static async Task<int> GetBlobCountAsync(BlobContainerClient containerClient)
    {
        var count = 0;
        await foreach (var _ in containerClient.GetBlobsAsync())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Demonstrates uploading a blob (optional method for extended functionality).
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobName">The blob name.</param>
    /// <param name="content">The content to upload.</param>
    public async Task<bool> UploadBlobAsync(string containerName, string blobName, string content)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            await blobClient.UploadAsync(stream, overwrite: true);

            Console.WriteLine($"✓ Successfully uploaded blob '{blobName}' to container '{containerName}'");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to upload blob: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Demonstrates downloading a blob (optional method for extended functionality).
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobName">The blob name.</param>
    public async Task<string?> DownloadBlobAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToString();

            Console.WriteLine($"✓ Successfully downloaded blob '{blobName}' from container '{containerName}'");
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to download blob: {ex.Message}");
            return null;
        }
    }
}
