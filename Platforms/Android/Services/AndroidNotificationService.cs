using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Android.OS;
using Android.Graphics;


namespace MauiAppDisertatieVacantaAI.Platforms.Android.Services
{
    public class AndroidNotificationService
    {
        private const string CHANNEL_ID = "weather_notifications";
        private const string CHANNEL_NAME = "Weather Notifications";
        private const string CHANNEL_DESCRIPTION = "Notifications about weather conditions";
        private const int NOTIFICATION_ID = 1001;

        private readonly Context _context;
        private NotificationManagerCompat _notificationManager;

        public AndroidNotificationService(Context context)
        {
            _context = context;
            _notificationManager = NotificationManagerCompat.From(_context);
            CreateNotificationChannel();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.Default)
                {
                    Description = CHANNEL_DESCRIPTION
                };
                channel.EnableLights(true);
                channel.EnableVibration(true);
                channel.SetVibrationPattern(new long[] { 100, 200, 300, 400, 500, 400, 300, 200, 400 });

                var notificationManager = _context.GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        public void ShowWeatherNotification(string title, string message, string bigText = null)
        {
            try
            {
                var intent = new Intent(_context, typeof(MainActivity));
                intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                
                var pendingIntent = PendingIntent.GetActivity(
                    _context, 
                    0, 
                    intent, 
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                );
                var notificationBuilder = new NotificationCompat.Builder(_context, CHANNEL_ID)
                    //set location icon
                    .SetSmallIcon(Resource.Drawable.sun)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetAutoCancel(true)
                    .SetContentIntent(pendingIntent)
                    .SetPriority(NotificationCompat.PriorityDefault)
                    .SetVibrate(new long[] { 100, 200, 300, 400, 500, 400, 300, 200, 400 });
                // Dacă avem text mai lung, folosește BigTextStyle
                if (!string.IsNullOrEmpty(bigText))
                {
                    notificationBuilder.SetStyle(new NotificationCompat.BigTextStyle()
                        .BigText(bigText)
                        .SetSummaryText("Vremea în zona ta"));
                }

                var notification = notificationBuilder.Build();
                _notificationManager.Notify(NOTIFICATION_ID, notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing notification: {ex.Message}");
            }
        }

        public void CancelWeatherNotification()
        {
            _notificationManager.Cancel(NOTIFICATION_ID);
        }

        public async Task<bool> RequestNotificationPermissionAsync()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+
                {
                    var permission = await Permissions.RequestAsync<Permissions.PostNotifications>();
                    return permission == PermissionStatus.Granted;
                }
                return true; // Pentru versiuni mai vechi, permisiunea nu este necesară
            }
            catch
            {
                return false;
            }
        }
    }
}