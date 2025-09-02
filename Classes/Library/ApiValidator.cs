using System.Net.Http;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using MauiAppDisertatieVacantaAI.Classes.Config;

namespace MauiAppDisertatieVacantaAI.Classes.Library
{
    public static class ApiValidator
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        static ApiValidator()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public static async Task<ApiValidationResult> ValidateAllApisAsync()
        {
            var result = new ApiValidationResult();

            try
            {
                // Încarcă configurația criptată
                var awsAccessKey = await EncryptionUtils.GetDecryptedAppSettingAsync("AWS.AccessKey");
                var awsSecretKey = await EncryptionUtils.GetDecryptedAppSettingAsync("AWS.SecretKey");
                var pexelsApiKey = await EncryptionUtils.GetDecryptedAppSettingAsync("pexelsAPI");
                var dbConnectionString = await EncryptionUtils.GetDecryptedConnectionStringAsync("DbContext");

                // Încarcă valorile necriptate direct din configurație
                var config = await LoadConfigurationDirectAsync();
                var awsRegion = config?.AppSettings?.GetValueOrDefault("AWS.Region");
                var s3BucketName = config?.AppSettings?.GetValueOrDefault("AWS.S3.BucketName");

                // Verifică dacă valorile au fost decriptate cu succes
                if (string.IsNullOrEmpty(awsAccessKey) || string.IsNullOrEmpty(awsSecretKey) || 
                    string.IsNullOrEmpty(pexelsApiKey) || string.IsNullOrEmpty(dbConnectionString))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Failed to decrypt configuration values. Please check your encrypted settings.";
                    return result;
                }

                // Validează AWS S3
                var awsValid = await ValidateAwsS3Async(awsAccessKey, awsSecretKey, awsRegion, s3BucketName);
                if (!awsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "AWS S3 validation failed. Please check your AWS configuration.";
                    return result;
                }

                // Validează Pexels API
                var pexelsValid = await ValidatePexelsApiAsync(pexelsApiKey);
                if (!pexelsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Pexels API validation failed. Please check your Pexels API key.";
                    return result;
                }

                // Validează Database Connection
                var databaseValid = await ValidateDatabaseConnectionAsync(dbConnectionString);
                if (!databaseValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Database connection failed. Please check your database configuration.";
                    return result;
                }

                result.IsValid = true;
                result.ErrorMessage = "All API connections validated successfully.";
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"API validation error: {ex.Message}";
                return result;
            }
        }

        private static async Task<bool> ValidateAwsS3Async(string accessKey, string secretKey, string region, string bucketName)
        {
            try
            {
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                    return false;

                // Creează client S3
                var regionEndpoint = RegionEndpoint.GetBySystemName(region ?? "us-east-1");
                using var s3Client = new AmazonS3Client(accessKey, secretKey, regionEndpoint);

                // Testează conexiunea prin listarea bucket-urilor
                var listBucketsRequest = new ListBucketsRequest();
                var response = await s3Client.ListBucketsAsync(listBucketsRequest);

                // Verifică dacă bucket-ul specificat există
                if (!string.IsNullOrEmpty(bucketName))
                {
                    var bucketExists = response.Buckets.Any(b => b.BucketName == bucketName);
                    if (!bucketExists)
                    {
                        System.Diagnostics.Debug.WriteLine($"S3 Bucket '{bucketName}' not found");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AWS S3 validation error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ValidatePexelsApiAsync(string apiKey)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 10)
                    return false;

                // Testează API-ul Pexels cu un request simplu
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.pexels.com/v1/search?query=nature&per_page=1");
                request.Headers.Add("Authorization", apiKey);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pexels API validation error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ValidateDatabaseConnectionAsync(string connectionString)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                    return false;

                // Test conexiune prin Entity Framework
                using (var context = new MauiAppDisertatieVacantaAI.Classes.Database.AppContext())
                {
                    // Testează conexiunea prin încercarea de a accesa baza de date
                    var canConnect = await Task.Run(() => context.Database.Exists());
                    return canConnect;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database validation error: {ex.Message}");
                return false;
            }
        }

        private static async Task<ConfigurationData> LoadConfigurationDirectAsync()
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
                System.Diagnostics.Debug.WriteLine($"Failed to load configuration: {ex.Message}");
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
