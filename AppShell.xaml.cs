using MauiAppDisertatieVacantaAI.Pages;
using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(RegisterNamePage), typeof(RegisterNamePage));
        Routing.RegisterRoute(nameof(RegisterContactPage), typeof(RegisterContactPage));
        Routing.RegisterRoute(nameof(RegisterBirthPage), typeof(RegisterBirthPage));
        Routing.RegisterRoute(nameof(ProfilePhotoPage), typeof(ProfilePhotoPage));
        Routing.RegisterRoute(nameof(WelcomePage), typeof(WelcomePage));
        Routing.RegisterRoute(nameof(NewSugestiePage), typeof(NewSugestiePage));
        Routing.RegisterRoute(nameof(ChatConversationPage), typeof(ChatConversationPage));
        Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));
        Routing.RegisterRoute(nameof(DestinationDetailsPage), typeof(DestinationDetailsPage));
        Routing.RegisterRoute(nameof(SuggestionDetailsPage), typeof(SuggestionDetailsPage));
        Routing.RegisterRoute(nameof(CategoryDetailsPage), typeof(CategoryDetailsPage));

        SetFooterMeta();
    }

    private void SetFooterMeta()
    {
        try
        {
            VersionLabel.Text = $"v{AppInfo.Current.VersionString}";
            var year = DateTime.UtcNow.Year;
            CopyrightLabel.Text = $"© {year} Vacantion Planner AI";
        }
        catch { }
    }

    private async void OnHomeHeaderClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("//MainPage"); // keeps tab bar visible
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("//SettingsPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        bool answer = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (answer)
        {
            await LogoutAsync();
        }
    }

    public static async Task LogoutAsync()
    {
        try
        {
            // Use UserSession to properly clear both in-memory and persistent session
            UserSession.ClearSession();
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
            try
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch
            {
                Application.Current?.Quit();
            }
        }
    }
}
