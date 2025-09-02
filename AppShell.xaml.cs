namespace MauiAppDisertatieVacantaAI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Navigate to splash page for API validation
            NavigateToSplash();
            // Load user info for flyout header
            LoadUserInfoForFlyout();
        }

        private async void LoadUserInfoForFlyout()
        {
            try
            {
                string userName = await SecureStorage.GetAsync("UserName");
                if (!string.IsNullOrEmpty(userName))
                {
                    UserNameLabel.Text = $"Welcome, {userName}!";
                }
                else
                {
                    string userEmail = await SecureStorage.GetAsync("UserEmail");
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        UserNameLabel.Text = $"Welcome, {userEmail}!";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading user info for flyout: {ex.Message}");
            }
        }

        private async void NavigateToSplash()
        {
            try
            {
                // Small delay to ensure Shell is initialized
                await Task.Delay(100);
                await Shell.Current.GoToAsync("//SplashPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation to splash error: {ex.Message}");
            }
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
}
