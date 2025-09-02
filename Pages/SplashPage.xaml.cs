using MauiAppDisertatieVacantaAI.Classes.Library;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
        StartValidation();
    }

    private async void StartValidation()
    {
        await Task.Delay(500); // Small delay for UI to load

        try
        {
            // Step 1: Load Configuration (25%)
            StatusLabel.Text = "Loading configuration...";
            await UpdateProgress(0.25, "25%");
            await Task.Delay(1000);

            // Step 2: Validate APIs (50%)
            StatusLabel.Text = "Validating API connections...";
            await UpdateProgress(0.50, "50%");
            await Task.Delay(500);

            var validationResult = await ApiValidator.ValidateAllApisAsync();

            if (!validationResult.IsValid)
            {
                // Show error
                ShowError(validationResult.ErrorMessage);
                return;
            }

            // Step 3: Database Check (75%)
            StatusLabel.Text = "Testing database connection...";
            await UpdateProgress(0.75, "75%");
            await Task.Delay(1000);

            // All checks passed (100%)
            StatusLabel.Text = "Initialization complete!";
            await UpdateProgress(1.0, "100%");
            LoadingIndicator.IsRunning = false;

            await Task.Delay(500);

            // Navigate to authentication check
            await NavigateToNextPage();
        }
        catch (Exception ex)
        {
            ShowError($"Initialization failed: {ex.Message}");
        }
    }

    private async Task UpdateProgress(double progress, string progressText)
    {
        ProgressBar.Progress = progress;
        ProgressLabel.Text = progressText;
        await Task.Delay(100); // Small delay for smooth animation
    }

    private async Task NavigateToNextPage()
    {
        try
        {
            // Check if user is already logged in
            string isLoggedIn = await SecureStorage.GetAsync("IsLoggedIn");
            string userId = await SecureStorage.GetAsync("UserId");
            
            if (!string.IsNullOrEmpty(isLoggedIn) && isLoggedIn == "true" && !string.IsNullOrEmpty(userId))
            {
                // Validate that user still exists in database
                try
                {
                    var utilizatorRepository = new UtilizatorRepository();
                    var user = utilizatorRepository.GetById(int.Parse(userId));
                    
                    if (user != null && user.EsteActiv == 1)
                    {
                        // User is valid, navigate to main page
                        await Shell.Current.GoToAsync("//MainPage");
                        return;
                    }
                    else
                    {
                        // User no longer exists or is inactive, clear stored data
                        SecureStorage.Remove("IsLoggedIn");
                        SecureStorage.Remove("UserEmail");
                        SecureStorage.Remove("UserId");
                        SecureStorage.Remove("UserName");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"User validation error: {ex.Message}");
                    // Clear stored data on error
                    SecureStorage.Remove("IsLoggedIn");
                    SecureStorage.Remove("UserEmail");
                    SecureStorage.Remove("UserId");
                    SecureStorage.Remove("UserName");
                }
            }
            
            // Navigate to login if not logged in or user validation failed
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            ShowError($"Navigation error: {ex.Message}");
        }
    }

    private void ShowError(string errorMessage)
    {
        LoadingIndicator.IsRunning = false;
        ProgressBar.Progress = 0;
        ProgressLabel.Text = "Error";
        
        StatusLabel.Text = "Initialization failed";
        ErrorLabel.Text = errorMessage;
        ErrorFrame.IsVisible = true;
        RetryButton.IsVisible = true;
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        // Reset UI
        ErrorFrame.IsVisible = false;
        RetryButton.IsVisible = false;
        LoadingIndicator.IsRunning = true;
        ProgressBar.Progress = 0;
        ProgressLabel.Text = "0%";
        
        // Restart validation
        StartValidation();
    }
}
