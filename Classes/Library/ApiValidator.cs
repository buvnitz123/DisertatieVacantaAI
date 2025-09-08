using System.Net.Http;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using MauiAppDisertatieVacantaAI.Classes.Config;

namespace MauiAppDisertatieVacantaAI.Classes.Library
{
    public static class ApiValidator
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string AzureContainerName = "vacantaai";

        static ApiValidator()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public static async Task<ApiValidationResult> ValidateAllApisAsync()
        {
            var result = new ApiValidationResult();

            try
            {
                // Decrypt required settings
                var azureBlobConnection = await EncryptionUtils.GetDecryptedAppSettingAsync("Azure.Blob.ConnectionString");
                var pexelsApiKey = await EncryptionUtils.GetDecryptedAppSettingAsync("pexelsAPI");
                var dbConnectionString = await EncryptionUtils.GetDecryptedConnectionStringAsync("DbContext");

                if (string.IsNullOrEmpty(azureBlobConnection) ||
                    string.IsNullOrEmpty(pexelsApiKey) ||
                    string.IsNullOrEmpty(dbConnectionString))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Failed to decrypt one or more required configuration values (Azure Blob / Pexels / DB).";
                    return result;
                }

                // Azure Blob validation
                var blobValid = await ValidateAzureBlobAsync(azureBlobConnection, AzureContainerName);
                if (!blobValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Azure Blob validation failed. Check connection string or container name.";
                    return result;
                }

                // Pexels API validation
                var pexelsValid = await ValidatePexelsApiAsync(pexelsApiKey);
                if (!pexelsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Pexels API validation failed. Check API key.";
                    return result;
                }

                // Database validation
                var dbValid = await ValidateDatabaseConnectionAsync(dbConnectionString);
                if (!dbValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Database connection failed. Check DB configuration.";
                    return result;
                }

                result.IsValid = true;
                result.ErrorMessage = "All services validated successfully.";
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                return result;
            }
        }

        private static async Task<bool> ValidateAzureBlobAsync(string connectionString, string containerName)
        {
            try
            {
                var serviceClient = new BlobServiceClient(connectionString);
                var containerClient = serviceClient.GetBlobContainerClient(containerName);

                // Simple existence check (does not create)
                var exists = await containerClient.ExistsAsync();
                if (!exists)
                {
                    System.Diagnostics.Debug.WriteLine($"[AzureBlob] Container '{containerName}' not found.");
                    return false;
                }

                // Lightweight list to confirm access (limit to 1)
                await using var enumerator = containerClient.GetBlobsAsync().GetAsyncEnumerator();
                if (await enumerator.MoveNextAsync())
                {
                    // At least one blob exists or access is confirmed
                }

                return true;
            }
            catch (RequestFailedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AzureBlob] Request failed: {ex.ErrorCode} - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AzureBlob] Validation error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ValidatePexelsApiAsync(string apiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 10)
                    return false;

                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.pexels.com/v1/search?query=test&per_page=1");
                request.Headers.Add("Authorization", apiKey);

                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Pexels] Validation error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ValidateDatabaseConnectionAsync(string connectionString)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                    return false;

                using (var context = new MauiAppDisertatieVacantaAI.Classes.Database.AppContext())
                {
                    var canConnect = await Task.Run(() => context.Database.Exists());
                    return canConnect;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB] Validation error: {ex.Message}");
                return false;
            }
        }

        // (Still used for any future direct config access if needed)
        private static async Task<ConfigurationData?> LoadConfigurationDirectAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<ConfigurationData>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Config load failed: {ex.Message}");
                return null;
            }
        }
    }

    public class ApiValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
