using System.Net.Http;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.Library.PhotoAPIViews;
using Newtonsoft.Json;

namespace MauiAppDisertatieVacantaAI.Classes.Services
{
    public class PhotoAPIUtils
    {
        private const string BaseUrl = "https://api.pexels.com/v1/";

        private static readonly HttpClient _httpClient = new HttpClient();

        private static string GetApiKey()
        {
            return AppSettingsRepository.GetValue("PexelsAPI");
        }

        public static PexelsPhotoResponse SearchPhotos(string query, int perPage = 15, int page = 1)
        {
            return SearchPhotosAsync(query, perPage, page).GetAwaiter().GetResult();
        }

        public static async Task<PexelsPhotoResponse> SearchPhotosAsync(string query, int perPage = 15, int page = 1)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PhotoAPIUtils.SearchPhotos called with query: {query}");

                var apiKey = GetApiKey();
                System.Diagnostics.Debug.WriteLine($"API Key retrieved: {!string.IsNullOrEmpty(apiKey)}");

                string url = $"{BaseUrl}search?query={Uri.EscapeDataString(query)}&per_page={perPage}&page={page}";
                System.Diagnostics.Debug.WriteLine($"Pexels URL: {url}");

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("Authorization", apiKey);

                System.Diagnostics.Debug.WriteLine("Making HTTP request to Pexels...");
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Pexels response received, length: {json?.Length ?? 0}");

                var result = JsonConvert.DeserializeObject<PexelsPhotoResponse>(json);
                System.Diagnostics.Debug.WriteLine($"Deserialized result: {result != null}, Photos count: {result?.Photos?.Count ?? 0}");

                return result;
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
                string url = $"{BaseUrl}photos/{id}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("Authorization", GetApiKey());

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PexelsPhoto>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting photo by ID: {ex.Message}", ex);
            }
        }
    }
}