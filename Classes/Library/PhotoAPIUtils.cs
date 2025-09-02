using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;
using System.IO;
using MauiAppDisertatieVacantaAI.Classes.Library.PhotoAPIViews;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Library
{
    public class PhotoAPIUtils
    {
        private const string BaseUrl = "https://api.pexels.com/v1/";
        private static WebClient _httpClient = new WebClient();

        private static string GetApiKey()
        {
            return EncryptionUtils.GetDecryptedAppSetting("pexelsAPI");
        }

        public static PexelsPhotoResponse SearchPhotos(string query, int perPage = 15, int page = 1)
        {
            try
            {
                Debug.WriteLine($"PhotoAPIUtils.SearchPhotos called with query: {query}");
                
                var apiKey = GetApiKey();
                Debug.WriteLine($"API Key retrieved: {!string.IsNullOrEmpty(apiKey)}");
                
                using (var client = new WebClient())
                {
                    client.Headers.Clear();
                    client.Headers.Add("Authorization", apiKey);

                    string url = $"{BaseUrl}search?query={Uri.EscapeDataString(query)}&per_page={perPage}&page={page}";
                    Debug.WriteLine($"Pexels URL: {url}");
                    
                    Debug.WriteLine("Making HTTP request to Pexels...");
                    string json = client.DownloadString(url);
                    Debug.WriteLine($"Pexels response received, length: {json?.Length ?? 0}");
                    
                    var result = JsonConvert.DeserializeObject<PexelsPhotoResponse>(json);
                    Debug.WriteLine($"Deserialized result: {result != null}, Photos count: {result?.Photos?.Count ?? 0}");
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in PhotoAPIUtils.SearchPhotos: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Error searching Pexels photos: {ex.Message}", ex);
            }
        }

        public static async Task<PexelsPhoto> GetPhotoByIdAsync(int id)
        {
            try
            {
                _httpClient.Headers.Clear();
                _httpClient.Headers.Add("Authorization", GetApiKey());

                string url = $"{BaseUrl}photos/{id}";
                string json = await _httpClient.DownloadStringTaskAsync(url);
                return JsonConvert.DeserializeObject<PexelsPhoto>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting photo by ID: {ex.Message}", ex);
            }
        }
    }
}