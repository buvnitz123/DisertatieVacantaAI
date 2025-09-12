using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            // Don't load user info in constructor - it won't refresh on navigation
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Load user info every time the page appears to ensure fresh data
            await LoadUserInfoAsync();
        }

        private async Task LoadUserInfoAsync()
        {
            try
            {
                // Prefer DB-backed user fetch
                var user = await UserSession.GetUserFromSessionAsync();
                if (user != null)
                {
                    WelcomeLabel.Text = $"Welcome, {user.Nume} {user.Prenume}".Trim();
                    return;
                }

                // Fallbacks if session/db not available
                string userName = await UserSession.GetUserNameAsync();
                if (!string.IsNullOrEmpty(userName))
                {
                    WelcomeLabel.Text = $"Welcome, {userName}";
                }
                else
                {
                    string userEmail = await UserSession.GetUserEmailAsync();
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        WelcomeLabel.Text = $"Welcome, {userEmail}";
                    }
                    else
                    {
                        WelcomeLabel.Text = "Welcome!";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user info: {ex.Message}");
                WelcomeLabel.Text = "Welcome!"; // Fallback display
            }
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}
