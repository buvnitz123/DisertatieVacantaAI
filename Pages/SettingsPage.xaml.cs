namespace MauiAppDisertatieVacantaAI.Pages;

public partial class SettingsPage : ContentPage
{
	private const string ThemePreferenceKey = "AppThemePreference"; // light | dark

	public SettingsPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		var pref = Preferences.Get(ThemePreferenceKey, "light");
		ThemeSwitch.Toggled -= OnThemeToggled; // prevent double firing
		ThemeSwitch.IsToggled = pref == "dark";
		ThemeSwitch.Toggled += OnThemeToggled;
		UpdateThemeStatus();
	}

	private void OnThemeToggled(object sender, ToggledEventArgs e)
	{
		var mode = e.Value ? "dark" : "light";
		App.SetTheme(mode);
		UpdateThemeStatus();
	}

	private void UpdateThemeStatus()
	{
		ThemeStatusLabel.Text = $"Tema curent?: {(Application.Current.UserAppTheme == AppTheme.Dark ? "Dark" : "Light")}";
	}
}