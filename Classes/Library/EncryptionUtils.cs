using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Reflection;
using Microsoft.Maui.Storage;
using MauiAppDisertatieVacantaAI.Classes.Config;

namespace MauiAppDisertatieVacantaAI.Classes.Library
{
    public static class EncryptionUtils
    {
        private static readonly string EncryptionKey = "0123456789abcdef";
        private static ConfigurationData _config;
        private static readonly object _lockObject = new object();
        
        public static string Encrypt(string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }
                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        public static string Decrypt(string cipherText)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }

        private static async Task<ConfigurationData> LoadConfigurationAsync()
        {
            if (_config != null)
                return _config;

            // Check again outside of lock to avoid unnecessary locking
            if (_config != null)
                return _config;

            ConfigurationData tempConfig;
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                tempConfig = JsonSerializer.Deserialize<ConfigurationData>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load configuration file: {ex.Message}", ex);
            }

            // Use lock only for the assignment
            lock (_lockObject)
            {
                if (_config == null)
                {
                    _config = tempConfig;
                }
            }

            return _config;
        }

        public static async Task<string> GetDecryptedAppSettingAsync(string key)
        {
            var config = await LoadConfigurationAsync();
            
            if (config?.AppSettings?.TryGetValue(key, out string encryptedValue) != true || 
                string.IsNullOrEmpty(encryptedValue))
                return string.Empty;
            
            try
            {
                return Decrypt(encryptedValue);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to decrypt app setting '{key}': {ex.Message}", ex);
            }
        }

        public static async Task<string> GetDecryptedConnectionStringAsync(string name)
        {
            var config = await LoadConfigurationAsync();
            
            if (config?.ConnectionStrings?.TryGetValue(name, out string encryptedValue) != true || 
                string.IsNullOrEmpty(encryptedValue))
                return string.Empty;
            
            try
            {
                return Decrypt(encryptedValue);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to decrypt connection string '{name}': {ex.Message}", ex);
            }
        }

        // Synchronous versions for backward compatibility (use with caution)
        public static string GetDecryptedAppSetting(string key)
        {
            return GetDecryptedAppSettingAsync(key).GetAwaiter().GetResult();
        }

        public static string GetDecryptedConnectionString(string name)
        {
            return GetDecryptedConnectionStringAsync(name).GetAwaiter().GetResult();
        }

        // Helper method to encrypt and update configuration values
        public static async Task SetEncryptedAppSettingAsync(string key, string plainValue)
        {
            var config = await LoadConfigurationAsync();
            if (config.AppSettings == null)
                config.AppSettings = new Dictionary<string, string>();
                
            config.AppSettings[key] = Encrypt(plainValue);
            // Note: In a real app, you'd want to save this back to a writable location
        }

        // Enhanced test method with detailed diagnostics
        public static async Task<bool> TestConfigurationAsync()
        {
            try
            {
                // Test 1: Can we load the configuration file?
                ConfigurationData config;
                try
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync();
                    config = JsonSerializer.Deserialize<ConfigurationData>(json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Config file load failed: {ex.Message}");
                    return false;
                }

                // Test 2: Does the config have the required structure?
                if (config?.ConnectionStrings == null || 
                    !config.ConnectionStrings.ContainsKey("DbContext"))
                {
                    System.Diagnostics.Debug.WriteLine("Config structure invalid - missing DbContext");
                    return false;
                }

                // Test 3: Can we decrypt the connection string?
                try
                {
                    var encryptedValue = config.ConnectionStrings["DbContext"];
                    if (string.IsNullOrEmpty(encryptedValue))
                    {
                        System.Diagnostics.Debug.WriteLine("DbContext connection string is empty");
                        return false;
                    }

                    var decrypted = Decrypt(encryptedValue);
                    if (string.IsNullOrEmpty(decrypted))
                    {
                        System.Diagnostics.Debug.WriteLine("Decrypted connection string is empty");
                        return false;
                    }

                    System.Diagnostics.Debug.WriteLine("Configuration test passed");
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Decryption failed: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Configuration test failed: {ex.Message}");
                return false;
            }
        }

        // Simplified synchronous version for quick checks
        public static bool TestConfiguration()
        {
            try
            {
                return TestConfigurationAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sync config test failed: {ex.Message}");
                return false;
            }
        }

        // Method to clear cached configuration (for retry scenarios)
        public static void ClearCache()
        {
            lock (_lockObject)
            {
                _config = null;
            }
        }
    }
}
