using MauiAppDisertatieVacantaAI.Classes.Library;
using System.Text.Json;

namespace MauiAppDisertatieVacantaAI.Classes.Config
{
    public static class ConfigHelper
    {
        // AWS Configuration
        public static async Task<string> GetAwsAccessKeyAsync()
        {
            return await EncryptionUtils.GetDecryptedAppSettingAsync("AWS.AccessKey");
        }

        public static async Task<string> GetAwsSecretKeyAsync()
        {
            return await EncryptionUtils.GetDecryptedAppSettingAsync("AWS.SecretKey");
        }

        public static async Task<string> GetAwsRegionAsync()
        {
            var config = await LoadConfigurationDirectAsync();
            return config?.AppSettings?.GetValueOrDefault("AWS.Region") ?? "us-east-1";
        }

        public static async Task<string> GetS3BucketNameAsync()
        {
            var config = await LoadConfigurationDirectAsync();
            return config?.AppSettings?.GetValueOrDefault("AWS.S3.BucketName") ?? "";
        }

        // Pexels API
        public static async Task<string> GetPexelsApiKeyAsync()
        {
            return await EncryptionUtils.GetDecryptedAppSettingAsync("pexelsAPI");
        }

        // Database Connection
        public static async Task<string> GetDbConnectionStringAsync()
        {
            return await EncryptionUtils.GetDecryptedConnectionStringAsync("DbContext");
        }

        // Helper pentru a cripta și salva valori noi (pentru development)
        public static string EncryptValue(string plainText)
        {
            return EncryptionUtils.Encrypt(plainText);
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

        // Exemplu de utilizare în cod
        public static async Task<bool> InitializeAppAsync()
        {
            try
            {
                var awsAccessKey = await GetAwsAccessKeyAsync();
                var awsSecretKey = await GetAwsSecretKeyAsync();
                var pexelsApiKey = await GetPexelsApiKeyAsync();
                var dbConnectionString = await GetDbConnectionStringAsync();

                // Verifică dacă valorile au fost decriptate cu succes
                return !string.IsNullOrEmpty(awsAccessKey) && 
                       !string.IsNullOrEmpty(awsSecretKey) && 
                       !string.IsNullOrEmpty(pexelsApiKey) &&
                       !string.IsNullOrEmpty(dbConnectionString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Configuration error: {ex.Message}");
                return false;
            }
        }
    }
}
