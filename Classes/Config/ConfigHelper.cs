using MauiAppDisertatieVacantaAI.Classes.Library;
using MauiAppDisertatieVacantaAI.Classes.Library.Services;
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

        // OpenAI API
        public static async Task<string> GetOpenAIApiKeyAsync()
        {
            return await EncryptionUtils.GetDecryptedAppSettingAsync("OpenAI.ApiKey");
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

        // Initialization check (now Azure Blob + Pexels + DB + OpenAI)
        public static async Task<bool> InitializeAppAsync()
        {
            try
            {
                var azureBlobConn = await GetAzureBlobConnectionStringAsync();
                var pexelsApiKey = await GetPexelsApiKeyAsync();
                var dbConnectionString = await GetDbConnectionStringAsync();
                var openAIApiKey = await GetOpenAIApiKeyAsync();

                bool basicConfigOk = !string.IsNullOrEmpty(azureBlobConn) &&
                                    !string.IsNullOrEmpty(pexelsApiKey) &&
                                    !string.IsNullOrEmpty(dbConnectionString) &&
                                    !string.IsNullOrEmpty(openAIApiKey);

                if (basicConfigOk)
                {
                    // Test OpenAI connection
                    var openAIService = new OpenAIService();
                    bool openAIWorking = await openAIService.InitializeAsync();
                    
                    if (!openAIWorking)
                    {
                        System.Diagnostics.Debug.WriteLine("OpenAI service failed to initialize, but continuing with app startup");
                    }
                }

                return basicConfigOk;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Configuration error: {ex.Message}");
                return false;
            }
        }
    }
}
