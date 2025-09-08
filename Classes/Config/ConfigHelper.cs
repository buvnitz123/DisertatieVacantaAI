using MauiAppDisertatieVacantaAI.Classes.Library;
using System.Text.Json;

namespace MauiAppDisertatieVacantaAI.Classes.Config
{
    public static class ConfigHelper
    {
        // Azure Blob
        public static async Task<string> GetAzureBlobConnectionStringAsync()
        {
            return await EncryptionUtils.GetDecryptedAppSettingAsync("Azure.Blob.ConnectionString");
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

        // Helper for encrypting a value (dev usage)
        public static string EncryptValue(string plainText)
        {
            return EncryptionUtils.Encrypt(plainText);
        }

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

        // Initialization check (now Azure + Pexels + DB)
        public static async Task<bool> InitializeAppAsync()
        {
            try
            {
                var azureBlobConn = await GetAzureBlobConnectionStringAsync();
                var pexelsApiKey = await GetPexelsApiKeyAsync();
                var dbConnectionString = await GetDbConnectionStringAsync();

                return !string.IsNullOrEmpty(azureBlobConn) &&
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
