using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MauiAppDisertatieVacantaAI.Classes.Library
{
    public static class EncryptionUtils
    {
        private static readonly string EncryptionKey = "0123456789abcdef";
        
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

        public static string GetDecryptedAppSetting(string key)
        {
            string encryptedValue = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(encryptedValue))
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

        public static string GetDecryptedConnectionString(string name)
        {
            string encryptedValue = ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
            if (string.IsNullOrEmpty(encryptedValue))
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
    }
}
