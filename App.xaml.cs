using Microsoft.Maui.Storage;

namespace MauiAppDisertatieVacantaAI
{
    public partial class App : Application
    {
        private const string ThemePreferenceKey = "AppThemePreference"; // values: light | dark

        public App()
        {
            InitializeComponent();
            ApplySavedTheme();
        }

        private void ApplySavedTheme()
        {
            var pref = Preferences.Get(ThemePreferenceKey, "light");
            UserAppTheme = pref == "dark" ? AppTheme.Dark : AppTheme.Light;
        }

        public static void SetTheme(string mode)
        {
            Preferences.Set(ThemePreferenceKey, mode);
            Application.Current.UserAppTheme = mode == "dark" ? AppTheme.Dark : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}