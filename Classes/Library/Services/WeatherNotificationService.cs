using System;
using System.Threading.Tasks;
using MauiAppDisertatieVacantaAI.Classes.Config;
using MauiAppDisertatieVacantaAI.Classes.Services;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public class WeatherNotificationService
    {
        private Timer _weatherTimer;
        private readonly INotificationService _notificationService;
        private const string LAST_NOTIFICATION_DATE_KEY = "LastWeatherNotificationDate";

        public WeatherNotificationService()
        {
            _notificationService = new NotificationService();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var apiKey = await ConfigHelper.GetWeatherApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    System.Diagnostics.Debug.WriteLine("Weather API key not found");
                    return;
                }

                // Încearcă să obții permisiunea, dar nu face nimic dacă e refuzată
                await _notificationService.RequestPermissionAsync();

                // Start periodic weather checks (every 2 hours)
                _weatherTimer = new Timer(async _ => await CheckWeatherAsync(), null, TimeSpan.Zero, TimeSpan.FromHours(2));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize weather service: {ex.Message}");
            }
        }

        public async Task CheckWeatherManuallyAsync()
        {
            // Verifică dacă a mai fost afișată notificarea astăzi
            if (await HasNotificationBeenShownTodayAsync())
            {
                System.Diagnostics.Debug.WriteLine("Weather notification already shown today, skipping...");
                return;
            }

            await CheckWeatherAsync();
        }

        private async Task CheckWeatherAsync()
        {
            try
            {
                // Pentru verificările automate (la fiecare 2 ore), verifică dacă a mai fost afișată astăzi
                if (await HasNotificationBeenShownTodayAsync())
                {
                    System.Diagnostics.Debug.WriteLine("Weather notification already shown today, skipping automatic check...");
                    return;
                }

                var location = await GetCurrentLocationAsync();
                if (location == null)
                {
                    System.Diagnostics.Debug.WriteLine("Could not get location for weather check");
                    return;
                }

                var apiKey = await ConfigHelper.GetWeatherApiKeyAsync();
                var weatherService = new WeatherAPIUtils(apiKey);
                
                var weather = await weatherService.GetCurrentWeatherByCoordinatesAsync(location.Latitude, location.Longitude);
                
                if (weather != null)
                {
                    await ShowWeatherNotificationAsync(weather, location);
                    // Marchează că notificarea a fost afișată astăzi
                    await MarkNotificationAsShownTodayAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking weather: {ex.Message}");
            }
        }

        private async Task<Location> GetCurrentLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var location = await Geolocation.GetLocationAsync(request);
                return location;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting location: {ex.Message}");
                return null;
            }
        }

        private async Task ShowWeatherNotificationAsync(WeatherResponse weather, Location location)
        {
            try
            {
                var temp = Math.Round(weather.Main.Temp);
                var description = weather.Weather[0].Description;
                var cityName = weather.Name;

                string notificationTitle = "🌤️ Vremea în zona ta";
                string notificationMessage = $"{cityName}: {temp}°C, {description}";

                // Add weather-based travel recommendations
                string recommendation = GetTravelRecommendation(weather);
                string bigText = notificationMessage;
                if (!string.IsNullOrEmpty(recommendation))
                {
                    bigText += $"\n\n💡 {recommendation}";
                }

                // Show native notification instead of alert
                _notificationService.ShowNotification(notificationTitle, notificationMessage, bigText);
                
                System.Diagnostics.Debug.WriteLine($"Weather notification sent: {notificationMessage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing weather notification: {ex.Message}");
            }
        }

        private string GetTravelRecommendation(WeatherResponse weather)
        {
            var temp = weather.Main.Temp;
            var weatherMain = weather.Weather[0].Main.ToLower();
            var humidity = weather.Main.Humidity;

            if (weatherMain.Contains("rain") || weatherMain.Contains("drizzle"))
            {
                return "Este o zi ploioasă - perfectă pentru muzee și activități indoor!";
            }
            else if (weatherMain.Contains("snow"))
            {
                return "Ninge afară - o vreme perfectă pentru sporturi de iarnă!";
            }
            else if (temp > 25 && weatherMain.Contains("clear"))
            {
                return "Vremea este frumoasă - o zi perfectă pentru plimbări și activități outdoor!";
            }
            else if (temp < 5)
            {
                return "Este destul de frig - nu uita să te îmbraci gros!";
            }
            else if (humidity > 80)
            {
                return "Umiditatea este ridicată - rămâi hidratat!";
            }
            else if (temp >= 15 && temp <= 25 && weatherMain.Contains("clear"))
            {
                return "Vremea este ideală pentru explorarea orașului!";
            }

            return string.Empty;
        }

        private async Task<bool> HasNotificationBeenShownTodayAsync()
        {
            try
            {
                var lastNotificationDate = await SecureStorage.GetAsync(LAST_NOTIFICATION_DATE_KEY);
                if (string.IsNullOrEmpty(lastNotificationDate))
                {
                    return false; // Niciodată nu a fost afișată
                }

                if (DateTime.TryParse(lastNotificationDate, out var lastDate))
                {
                    // Verifică dacă data de astăzi este aceeași cu ultima dată când a fost afișată notificarea
                    return lastDate.Date == DateTime.Today;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking notification date: {ex.Message}");
                return false; // În caz de eroare, permite afișarea notificării
            }
        }

        private async Task MarkNotificationAsShownTodayAsync()
        {
            try
            {
                await SecureStorage.SetAsync(LAST_NOTIFICATION_DATE_KEY, DateTime.Today.ToString("yyyy-MM-dd"));
                System.Diagnostics.Debug.WriteLine($"Weather notification marked as shown for today: {DateTime.Today:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving notification date: {ex.Message}");
            }
        }

        public void StopWeatherChecks()
        {
            _weatherTimer?.Dispose();
            _weatherTimer = null;
            _notificationService?.CancelNotification();
        }

        // Metodă pentru resetarea cache-ului (utilă pentru testare)
        public async Task ResetNotificationCacheAsync()
        {
            try
            {
                SecureStorage.Remove(LAST_NOTIFICATION_DATE_KEY);
                System.Diagnostics.Debug.WriteLine("Weather notification cache reset");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting notification cache: {ex.Message}");
            }
        }
    }
}