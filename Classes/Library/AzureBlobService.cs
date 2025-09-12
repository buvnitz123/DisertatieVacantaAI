using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace MauiAppDisertatieVacantaAI.Classes.Library
{
    // Kept the original class name (S3Utils) to avoid touching other code paths.
    // Internally now uses Azure Blob Storage.
    public static class AzureBlobService
    {
        private static readonly string ContainerName = "vacantaai";

        // Retrieves the Azure Storage connection string (encrypted in appsettings)
        private static string GetConnectionString()
        {
            // Expect a new encrypted key: Azure.Blob.ConnectionString
            var conn = EncryptionUtils.GetDecryptedAppSetting("Azure.Blob.ConnectionString");
            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("Azure Blob connection string not configured (Azure.Blob.ConnectionString).");
            return conn;
        }

        private static BlobContainerClient CreateContainerClient()
        {
            var connectionString = GetConnectionString();
            var container = new BlobContainerClient(connectionString, ContainerName);
            // We do not auto-create in production unless needed; can uncomment if required
            // container.CreateIfNotExists(PublicAccessType.None);
            return container;
        }

        public static async Task<string> UploadImageAsync(byte[] imageBytes, string fileName, string contentType)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Image bytes empty", nameof(imageBytes));

            try
            {
                var uniqueFileName = GenerateUniqueFileName(fileName, "categories/");
                Debug.WriteLine($"[AzureBlob] Generated unique filename: {uniqueFileName}");

                var container = CreateContainerClient();
                var blobClient = container.GetBlobClient(uniqueFileName);

                var headers = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(contentType)
                        ? GetContentTypeFromFileName(fileName)
                        : contentType
                };

                using var ms = new MemoryStream(imageBytes);
                await blobClient.UploadAsync(ms, new BlobUploadOptions
                {
                    HttpHeaders = headers
                });

                // Access: if container is private, generate a SAS URL instead. For now we return blob uri.
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AzureBlob] Upload error: {ex}");
                throw new Exception($"Failed to upload image to Azure Blob: {ex.Message}", ex);
            }
        }

        // Uploads to an exact blob name (overwrites existing) and sets content type
        public static async Task<string> UploadImageWithFixedNameAsync(byte[] imageBytes, string blobName, string contentType)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Image bytes empty", nameof(imageBytes));
            if (string.IsNullOrWhiteSpace(blobName))
                throw new ArgumentException("Blob name required", nameof(blobName));

            try
            {
                var container = CreateContainerClient();
                var blobClient = container.GetBlobClient(blobName);

                using var ms = new MemoryStream(imageBytes);
                // Overwrite existing if any
                await blobClient.UploadAsync(ms, overwrite: true);

                // Set content type
                var headers = new BlobHttpHeaders { ContentType = string.IsNullOrWhiteSpace(contentType) ? GetContentTypeFromFileName(blobName) : contentType };
                await blobClient.SetHttpHeadersAsync(headers);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AzureBlob] Upload (fixed name) error: {ex}");
                throw new Exception($"Failed to upload image to Azure Blob: {ex.Message}", ex);
            }
        }

        // Synchronous wrapper maintained for compatibility (executes async method synchronously).
        public static string UploadImage(byte[] imageBytes, string fileName, string contentType)
        {
            try
            {
                return UploadImageAsync(imageBytes, fileName, contentType).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AzureBlob] Sync upload failed: {ex}");
                throw;
            }
        }

        public static bool DeleteImage(string imageUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                    return true;

                var blobName = ExtractBlobNameFromUrl(imageUrl);
                if (string.IsNullOrEmpty(blobName))
                    return false;

                Debug.WriteLine($"[AzureBlob] Deleting blob: {blobName}");
                var container = CreateContainerClient();
                var blobClient = container.GetBlobClient(blobName);

                var response = blobClient.DeleteIfExists();
                Debug.WriteLine(response ? "[AzureBlob] Deleted successfully" : "[AzureBlob] Blob not found");
                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AzureBlob] Delete failed: {ex}");
                return false;
            }
        }

        private static string GenerateUniqueFileName(string originalFileName, string prefix = "")
        {
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            return $"{prefix}{timestamp}_{guid}{extension}";
        }

        public static byte[] ConvertBase64ToBytes(string base64String)
        {
            try
            {
                if (string.IsNullOrEmpty(base64String))
                    return null;

                var base64Data = base64String.Contains("base64,")
                    ? base64String.Split(',')[1]
                    : base64String;

                return Convert.FromBase64String(base64Data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AzureBlob] Base64 conversion failed: {ex}");
                return null;
            }
        }

        public static string GetContentTypeFromFileName(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        public static bool IsValidImageType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return false;
            var ct = contentType.ToLowerInvariant();
            return ct is "image/jpeg" or "image/png" or "image/gif" or "image/bmp" or "image/webp";
        }

        private static string ExtractBlobNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                // For formats like https://account.blob.core.windows.net/container/path/file.ext
                // AbsolutePath = /container/path/file.ext
                var path = uri.AbsolutePath.TrimStart('/');
                if (path.StartsWith($"{ContainerName}/", StringComparison.OrdinalIgnoreCase))
                    return path.Substring(ContainerName.Length + 1);
                // If path already is relative to container (when using custom domains)
                return path;
            }
            catch
            {
                return null;
            }
        }
    }
}
