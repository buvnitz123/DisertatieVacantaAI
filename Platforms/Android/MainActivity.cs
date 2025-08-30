using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace MauiAppDisertatieVacantaAI;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Setează culoarea barierii de stare
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#222831")); // DarkGray
            Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#222831")); // DarkGray
        }
    }
}
