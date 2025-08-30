using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using System.Configuration;
using System.Web;

namespace MauiAppDisertatieVacantaAI.Classes.Library
{
    public static class S3Utils
    {
        private static readonly string BucketName = "vacantaai";
        private static readonly string Region = "us-east-1";
        
        public static async Task<string> UploadImageAsync(byte[] imageBytes, string fileName, string contentType)
        {
            try
            {
                Debug.WriteLine("[S3] Creating S3 client and transfer utility...");
                
                // Get AWS credentials from Web.config (encrypted)
                var accessKey = EncryptionUtils.GetDecryptedAppSetting("AWS.AccessKey");
                var secretKey = EncryptionUtils.GetDecryptedAppSetting("AWS.SecretKey");
                
                Debug.WriteLine($"[S3] AWS AccessKey configured: {!string.IsNullOrEmpty(accessKey)}");
                Debug.WriteLine($"[S3] AWS SecretKey configured: {!string.IsNullOrEmpty(secretKey)}");
                
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    throw new Exception("AWS credentials not configured in Web.config");
                }
                
                // Create temp file for upload
                var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                try
                {
                    // Write bytes to temp file (synchonous to avoid async/await issues in older .NET versions)
                    File.WriteAllBytes(tempFilePath, imageBytes);
                    
                    // Create client and transfer utility
                    using (var s3Client = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.USEast1))
                    using (var transferUtility = new TransferUtility(s3Client))
                    {
                        var uniqueFileName = GenerateUniqueFileName(fileName, "categories/");
                        Debug.WriteLine($"[S3] Generated unique filename: {uniqueFileName}");
                        
                        var uploadRequest = new TransferUtilityUploadRequest
                        {
                            BucketName = BucketName,
                            Key = uniqueFileName,
                            FilePath = tempFilePath,
                            ContentType = contentType,
                            CannedACL = S3CannedACL.PublicRead
                        };
                        
                        Debug.WriteLine("[S3] Starting upload to S3...");
                        await transferUtility.UploadAsync(uploadRequest);
                        Debug.WriteLine("[S3] Upload completed successfully");
                        
                        return $"https://{BucketName}.s3.{Region}.amazonaws.com/{uniqueFileName}";
                    }
                }
                finally
                {
                    // Clean up temp file
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[S3] Upload error: {ex.Message}");
                Debug.WriteLine($"[S3] Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to upload image to S3: {ex.Message}", ex);
            }
        }

        public static string UploadImage(byte[] imageBytes, string fileName, string contentType)
        {
            Debug.WriteLine($"[S3] Starting synchronous upload of {fileName} ({imageBytes?.Length ?? 0} bytes)");
            
            try
            {
                // Get AWS credentials from Web.config (encrypted)
                var accessKey = EncryptionUtils.GetDecryptedAppSetting("AWS.AccessKey");
                var secretKey = EncryptionUtils.GetDecryptedAppSetting("AWS.SecretKey");
                
                Debug.WriteLine($"[S3] AWS AccessKey configured: {!string.IsNullOrEmpty(accessKey)}");
                Debug.WriteLine($"[S3] AWS SecretKey configured: {!string.IsNullOrEmpty(secretKey)}");
                
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    throw new Exception("AWS credentials not configured in Web.config");
                }
                
                // Create S3 client with explicit timeout
                var config = new AmazonS3Config
                {
                    RegionEndpoint = Amazon.RegionEndpoint.USEast1,
                    Timeout = TimeSpan.FromSeconds(30)
                };
                
                using (var s3Client = new AmazonS3Client(accessKey, secretKey, config))
                {
                    var uniqueFileName = GenerateUniqueFileName(fileName, "categories/");
                    Debug.WriteLine($"[S3] Generated unique filename: {uniqueFileName}");
                    
                    using (var stream = new MemoryStream(imageBytes))
                    {
                        var request = new PutObjectRequest
                        {
                            BucketName = BucketName,
                            Key = uniqueFileName,
                            InputStream = stream,
                            ContentType = contentType
                            // CannedACL a fost eliminat deoarece bucket-ul nu permite ACL-uri
                        };
                        
                        Debug.WriteLine("[S3] Starting PutObject request...");
                        var response = s3Client.PutObjectAsync(request).GetAwaiter().GetResult();
                        Debug.WriteLine($"[S3] PutObject completed. Status: {response.HttpStatusCode}");
                        
                        return $"https://{BucketName}.s3.{Region}.amazonaws.com/{uniqueFileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[S3] Upload failed: {ex.Message}");
                Debug.WriteLine($"[S3] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[S3] Inner exception: {ex.InnerException.Message}");
                }
                throw new Exception($"Failed to upload image to S3: {ex.Message}", ex);
            }
        }

        private static string GenerateUniqueFileName(string originalFileName, string prefix = "")
        {
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            return $"{prefix}{timestamp}_{guid}{extension}";
        }

        public static bool DeleteImage(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return true;
                    
                var key = ExtractKeyFromUrl(imageUrl);
                if (string.IsNullOrEmpty(key))
                    return false;

                Debug.WriteLine($"[S3] Deleting image with key: {key}");
                var accessKey = EncryptionUtils.GetDecryptedAppSetting("AWS.AccessKey");
                var secretKey = EncryptionUtils.GetDecryptedAppSetting("AWS.SecretKey");
                
                using (var s3Client = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.USEast1))
                using (var transferUtility = new TransferUtility(s3Client))
                {
                    var request = new TransferUtilityOpenStreamRequest
                    {
                        BucketName = BucketName,
                        Key = key
                    };
                    
                    // Check if file exists before attempting to delete
                    try
                    {
                        using (transferUtility.OpenStream(request))
                        {
                            // File exists, proceed with delete
                        }
                    }
                    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Debug.WriteLine($"[S3] File not found: {key}");
                        return false;
                    }
                    
                    // Delete the file
                    transferUtility.S3Client.DeleteObjectAsync(BucketName, key).Wait();
                    Debug.WriteLine($"[S3] Successfully deleted: {key}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[S3] Delete image failed: {ex}");
                return false;
            }
        }

        public static byte[] ConvertBase64ToBytes(string base64String)
        {
            try
            {
                if (string.IsNullOrEmpty(base64String))
                    return null;
                    
                // Remove data URI scheme if present
                var base64Data = base64String.Contains("base64,") 
                    ? base64String.Split(',')[1] 
                    : base64String;
                    
                return Convert.FromBase64String(base64Data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[S3] Base64 conversion failed: {ex}");
                return null;
            }
        }

        public static string GetContentTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".webp":
                    return "image/webp";
                default:
                    return "application/octet-stream";
            }
        }

        public static bool IsValidImageType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;
                
            var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
            var lowerContentType = contentType.ToLowerInvariant();
            
            foreach (var type in validTypes)
            {
                if (string.Equals(type, lowerContentType, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            
            return false;
        }

        private static string ExtractKeyFromUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;
                
            try
            {
                var uri = new Uri(imageUrl);
                // Remove the leading slash if present
                return uri.AbsolutePath.TrimStart('/');
            }
            catch
            {
                return null;
            }
        }
    }
}
