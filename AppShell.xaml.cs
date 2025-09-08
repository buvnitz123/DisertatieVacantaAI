using MauiAppDisertatieVacantaAI.Pages;

namespace MauiAppDisertatieVacantaAI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Auth / onboarding flow pages (not Shell roots)
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
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
