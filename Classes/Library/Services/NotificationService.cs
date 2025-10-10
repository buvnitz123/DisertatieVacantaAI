using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;
using System.Threading.Tasks;

#if ANDROID
using MauiAppDisertatieVacantaAI.Platforms.Android.Services;
#endif

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public class NotificationService : INotificationService
    {
#if ANDROID
        private AndroidNotificationService _androidService;

        public NotificationService()
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            _androidService = new AndroidNotificationService(context);
        }
#endif

        public async Task<bool> RequestPermissionAsync()
        {
#if ANDROID
            return await _androidService.RequestNotificationPermissionAsync();
#else
            return true;
#endif
        }

        public void ShowNotification(string title, string message, string bigText = null)
        {
#if ANDROID
            _androidService.ShowWeatherNotification(title, message, bigText);
#else
            // Pentru alte platforme, poți implementa logica specifică
            System.Diagnostics.Debug.WriteLine($"Notification: {title} - {message}");
#endif
        }

        public void CancelNotification()
        {
#if ANDROID
            _androidService.CancelWeatherNotification();
#else
            System.Diagnostics.Debug.WriteLine("Notification cancelled");
#endif
        }
    }
}