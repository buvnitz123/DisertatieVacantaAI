using System.Net;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.Library.PhotoAPIViews;
using Newtonsoft.Json;

namespace MauiAppDisertatieVacantaAI.Classes.Services
{
    public class PhotoAPIUtils
    {
        private const string BaseUrl = "https://api.pexels.com/v1/";

        private static string GetApiKey()
        {
            return AppSettingsRepository.GetValue("PexelsAPI");
        }

        public static PexelsPhotoResponse SearchPhotos(string query, int perPage = 15, int page = 1)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PhotoAPIUtils.SearchPhotos called with query: {query}");
                
                var apiKey = GetApiKey();
                System.Diagnostics.Debug.WriteLine($"API Key retrieved: {!string.IsNullOrEmpty(apiKey)}");
                
                using (var client = new WebClient())
                {
                    client.Headers.Clear();
                    client.Headers.Add("Authorization", apiKey);

                    string url = $"{BaseUrl}search?query={Uri.EscapeDataString(query)}&per_page={perPage}&page={page}";
                    System.Diagnostics.Debug.WriteLine($"Pexels URL: {url}");
                    
                    System.Diagnostics.Debug.WriteLine("Making HTTP request to Pexels...");
                    string json = client.DownloadString(url);
                    System.Diagnostics.Debug.WriteLine($"Pexels response received, length: {json?.Length ?? 0}");
                    
                    var result = JsonConvert.DeserializeObject<PexelsPhotoResponse>(json);
                    System.Diagnostics.Debug.WriteLine($"Deserialized result: {result != null}, Photos count: {result?.Photos?.Count ?? 0}");
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in PhotoAPIUtils.SearchPhotos: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Error searching Pexels photos: {ex.Message}", ex);
            }
        }

        public static async Task<PexelsPhoto> GetPhotoByIdAsync(int id)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Clear();
                    client.Headers.Add("Authorization", GetApiKey());

                    string url = $"{BaseUrl}photos/{id}";
                    string json = await client.DownloadStringTaskAsync(url);
                    return JsonConvert.DeserializeObject<PexelsPhoto>(json);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting photo by ID: {ex.Message}", ex);
            }
        }
    }
}