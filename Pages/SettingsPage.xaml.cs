using MauiAppDisertatieVacantaAI.Classes.Library.Services;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class SettingsPage : ContentPage
{
	private const string ThemePreferenceKey = "AppThemePreference";
	private const string WeatherNotifKey = "WeatherNotificationsEnabled";

	private static readonly Dictionary<string, string> AIModels = new()
	{
		{ AIServiceFactory.Gemini25, "Gemini 2.5 Flash" },
		{ AIServiceFactory.Gemini3, "Gemini 3 Flash" },
		{ AIServiceFactory.GPT4, "GPT-4o" }
	};

	public SettingsPage()
	{
		InitializeComponent();
		AIModelPicker.ItemsSource = AIModels.Values.ToList();
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

		// AI Model
		AIModelPicker.SelectedIndexChanged -= OnAIModelChanged;
		var currentModel = AIServiceFactory.GetCurrentModelKey();
		var modelIndex = AIModels.Keys.ToList().IndexOf(currentModel);
		AIModelPicker.SelectedIndex = modelIndex >= 0 ? modelIndex : 0;
		AIModelPicker.SelectedIndexChanged += OnAIModelChanged;
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

	private void OnAIModelChanged(object sender, EventArgs e)
	{
		if (AIModelPicker.SelectedIndex < 0) return;
		var selectedKey = AIModels.Keys.ElementAt(AIModelPicker.SelectedIndex);
		AIServiceFactory.SetModel(selectedKey);
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