using MauiAppDisertatieVacantaAI.Pages;

namespace MauiAppDisertatieVacantaAI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Auth / onboarding flow pages (not Shell roots)
        Routing.RegisterRoute(nameof(RegisterNamePage), typeof(RegisterNamePage));
        Routing.RegisterRoute(nameof(RegisterContactPage), typeof(RegisterContactPage));
        Routing.RegisterRoute(nameof(RegisterBirthPage), typeof(RegisterBirthPage));
        Routing.RegisterRoute(nameof(ProfilePhotoPage), typeof(ProfilePhotoPage));
        Routing.RegisterRoute(nameof(WelcomePage), typeof(WelcomePage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        // Close the flyout menu first
        Shell.Current.FlyoutIsPresented = false;
        
        // Show confirmation dialog
        bool answer = await Shell.Current.DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        
        if (answer)
        {
            await LogoutAsync();
        }
    }

    private async void OnHomeMenuClicked(object sender, EventArgs e)
    {
        try
        {
            Shell.Current.FlyoutIsPresented = false;
            // Navigate to the tabbed Home root (double slash ensures shell section root)
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Home navigation error: {ex.Message}");
        }
    }

    public static async Task LogoutAsync()
    {
        try
        {
            // Șterge datele de autentificare
            SecureStorage.Remove("IsLoggedIn");
            SecureStorage.Remove("UserEmail");
            SecureStorage.Remove("UserId");
            SecureStorage.Remove("UserName");
            
            // Navighează la pagina de login
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            // Log error if needed
            System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
            // Try alternative navigation
            try
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch
            {
                // Force restart if navigation fails
                Application.Current?.Quit();
            }
        }
    }
}
