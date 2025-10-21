using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.Library;
using MauiAppDisertatieVacantaAI.Classes.Library.Services;
using System.Text.Json;

namespace MauiAppDisertatieVacantaAI.Classes.Config
{
    public static class ConfigHelper
    {
        public static async Task<string> GetAzureBlobConnectionStringAsync()
        {
            return await Task.Run(() => AppSettingsRepository.GetValue("AzureBlobConnection") ?? string.Empty);
        }

        public static async Task<string> GetPexelsApiKeyAsync()
        {
            return await Task.Run(() => AppSettingsRepository.GetValue("PexelsAPI") ?? string.Empty);
        }

        public static async Task<string> GetGeminiApiKeyAsync()
        {
            return await Task.Run(() => AppSettingsRepository.GetValue("GeminiAPI") ?? string.Empty);
        }

        public static async Task<string> GetWeatherApiKeyAsync()
        {
            return await Task.Run(() => AppSettingsRepository.GetValue("WeatherAPI") ?? string.Empty);
        }

        public static async Task<string> GetDbConnectionStringAsync()
        {
            return await EncryptionUtils.GetDecryptedConnectionStringAsync("DbContext");
        }

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

        public static async Task<bool> InitializeAppAsync()
        {
            try
            {
                var azureBlobConn = await GetAzureBlobConnectionStringAsync();
                var pexelsApiKey = await GetPexelsApiKeyAsync();
                var dbConnectionString = await GetDbConnectionStringAsync();
                var geminiApiKey = await GetGeminiApiKeyAsync();
                var weatherApiKey = await GetWeatherApiKeyAsync();

                bool basicConfigOk = !string.IsNullOrEmpty(azureBlobConn) &&
                                    !string.IsNullOrEmpty(pexelsApiKey) &&
                                    !string.IsNullOrEmpty(dbConnectionString) &&
                                    !string.IsNullOrEmpty(geminiApiKey) &&
                                    !string.IsNullOrEmpty(weatherApiKey);

                if (basicConfigOk)
                {
                    var geminiService = new GeminiService();
                    bool geminiWorking = await geminiService.InitializeAsync();

                    if (!geminiWorking)
                    {
                        System.Diagnostics.Debug.WriteLine("Gemini service failed to initialize, but continuing with app startup");
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
