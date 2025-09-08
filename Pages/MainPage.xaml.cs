using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Pages
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        private async void LoadUserInfo()
        {
            try
            {
                string userName = await SecureStorage.GetAsync("UserName");

                if (!string.IsNullOrEmpty(userName))
                {
                    WelcomeLabel.Text = $"Welcome back, {userName}!";
                }
                else
                {
                    // Fallback to email if name is not available
                    string userEmail = await SecureStorage.GetAsync("UserEmail");
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        WelcomeLabel.Text = $"Welcome back, {userEmail}!";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user info: {ex.Message}");
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
