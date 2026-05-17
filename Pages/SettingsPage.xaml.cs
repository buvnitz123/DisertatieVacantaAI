namespace MauiAppDisertatieVacantaAI.Pages;

public partial class SettingsPage : ContentPage
{
	private const string ThemePreferenceKey = "AppThemePreference";
	private const string WeatherNotifKey = "WeatherNotificationsEnabled";

	public SettingsPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Theme
		var pref = Preferences.Get(ThemePreferenceKey, "light");
		ThemeSwitch.Toggled -= OnThemeToggled;
		ThemeSwitch.IsToggled = pref == "dark";
		ThemeSwitch.Toggled += OnThemeToggled;

		// Weather notifications
		WeatherSwitch.Toggled -= OnWeatherToggled;
		WeatherSwitch.IsToggled = Preferences.Get(WeatherNotifKey, true);
		WeatherSwitch.Toggled += OnWeatherToggled;

		// App info
		AppVersionLabel.Text = AppInfo.Current.VersionString;
		PlatformLabel.Text = DeviceInfo.Platform.ToString();
	}

	private void OnThemeToggled(object sender, ToggledEventArgs e)
	{
		var mode = e.Value ? "dark" : "light";
		App.SetTheme(mode);
	}

	private void OnWeatherToggled(object sender, ToggledEventArgs e)
	{
		Preferences.Set(WeatherNotifKey, e.Value);
	}

	private async void OnClearCacheClicked(object sender, EventArgs e)
	{
		bool confirm = await DisplayAlert("Șterge cache", "Ești sigur că vrei să ștergi cache-ul de imagini?", "Da", "Nu");
		if (!confirm) return;

		try
		{
			var cacheDir = FileSystem.CacheDirectory;
			if (Directory.Exists(cacheDir))
			{
				var files = Directory.GetFiles(cacheDir);
				foreach (var file in files)
				{
					try { File.Delete(file); } catch { }
				}
			}
			await DisplayAlert("Gata", "Cache-ul a fost șters cu succes.", "OK");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error clearing cache: {ex.Message}");
			await DisplayAlert("Eroare", "Nu s-a putut șterge cache-ul.", "OK");
		}
	}

	private async void OnBack(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}