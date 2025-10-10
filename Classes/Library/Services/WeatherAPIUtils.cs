using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MauiAppDisertatieVacantaAI.Classes.Services
{
    public class WeatherAPIUtils
    {
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://api.openweathermap.org/data/2.5";
        private readonly HttpClient _httpClient;

        public WeatherAPIUtils(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<WeatherResponse> GetCurrentWeatherAsync(string city)
        {
            try
            {
                var url = $"{_baseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric&lang=ro";
                var response = await _httpClient.GetStringAsync(url);
                return JsonConvert.DeserializeObject<WeatherResponse>(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Eroare la ob?inerea vremii pentru {city}: {ex.Message}");
            }
        }

        public async Task<WeatherResponse> GetCurrentWeatherByCoordinatesAsync(double latitude, double longitude)
        {
            try
            {
                var url = $"{_baseUrl}/weather?lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&appid={_apiKey}&units=metric&lang=ro";
                var response = await _httpClient.GetStringAsync(url);
                return JsonConvert.DeserializeObject<WeatherResponse>(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Eroare la ob?inerea vremii pentru coordonatele {latitude}, {longitude}: {ex.Message}");
            }
        }

        public async Task<ForecastResponse> GetForecastAsync(string city)
        {
            try
            {
                var url = $"{_baseUrl}/forecast?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric&lang=ro";
                var response = await _httpClient.GetStringAsync(url);
                return JsonConvert.DeserializeObject<ForecastResponse>(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Eroare la ob?inerea prognozei pentru {city}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Weather Data Models
    public class WeatherResponse
    {
        public WeatherMain Main { get; set; }
        public WeatherDescription[] Weather { get; set; }
        public WeatherWind Wind { get; set; }
        public WeatherClouds Clouds { get; set; }
        public int Visibility { get; set; }
        public string Name { get; set; }
        public WeatherSys Sys { get; set; }
    }

    public class WeatherMain
    {
        public double Temp { get; set; }
        public double Feels_like { get; set; }
        public double Temp_min { get; set; }
        public double Temp_max { get; set; }
        public int Pressure { get; set; }
        public int Humidity { get; set; }
    }

    public class WeatherDescription
    {
        public string Main { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
    }

    public class WeatherWind
    {
        public double Speed { get; set; }
        public int Deg { get; set; }
    }

    public class WeatherClouds
    {
        public int All { get; set; }
    }

    public class WeatherSys
    {
        public string Country { get; set; }
        public long Sunrise { get; set; }
        public long Sunset { get; set; }
    }

    public class ForecastResponse
    {
        public ForecastItem[] List { get; set; }
        public ForecastCity City { get; set; }
    }

    public class ForecastItem
    {
        public long Dt { get; set; }
        public WeatherMain Main { get; set; }
        public WeatherDescription[] Weather { get; set; }
        public WeatherWind Wind { get; set; }
        public string Dt_txt { get; set; }
    }

    public class ForecastCity
    {
        public string Name { get; set; }
        public string Country { get; set; }
    }
}