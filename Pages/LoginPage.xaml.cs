using System;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class LoginPage : ContentPage
{
    private readonly UtilizatorRepository _utilizatorRepository;

    public LoginPage()
    {
        InitializeComponent();
        _utilizatorRepository = new UtilizatorRepository();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Validare input
        if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ShowError("Please enter both email and password.");
            return;
        }

        // Afișează loading
        SetLoadingState(true);
        System.Diagnostics.Debug.WriteLine("Loading state set to TRUE");

        try
        {
            // Add small delay to see loading indicator
            await Task.Delay(300);
            
            // Autentificare cu Entity Framework
            var user = _utilizatorRepository.GetByEmailAndPassword(EmailEntry.Text.Trim(), PasswordEntry.Text);

            if (user != null)
            {
                // ALWAYS clear any previous session data first
                UserSession.ClearSession();

                // Set current session (persist only if Remember Me is checked)
                bool persist = RememberMeCheckBox.IsChecked;
                UserSession.SetCurrentUser(
                    user.Id_Utilizator.ToString(),
                    user.Email,
                    user.Nume,
                    user.Prenume,
                    persist
                );

                if (persist)
                {
                    await UserSession.SetLoggedInAsync(true);
                }

                // Navigare la pagina principală
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                ShowError("Invalid email or password. Please check your credentials.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Login failed: {ex.Message}");
        }
        finally
        {
            SetLoadingState(false);
            System.Diagnostics.Debug.WriteLine("Loading state set to FALSE");
        }
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterNamePage));
    }

    private void SetLoadingState(bool isLoading)
    {
        System.Diagnostics.Debug.WriteLine($"SetLoadingState called with: {isLoading}");
        LoadingOverlay.IsVisible = isLoading;
        System.Diagnostics.Debug.WriteLine($"LoadingOverlay.IsVisible set to: {LoadingOverlay.IsVisible}");
        
        LoginButton.IsEnabled = !isLoading;
        EmailEntry.IsEnabled = !isLoading;
        PasswordEntry.IsEnabled = !isLoading;
        
        if (isLoading)
        {
            ErrorLabel.IsVisible = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void OnShowPasswordClicked(object sender, EventArgs e)
    {
        // Schimbă între afișarea și ascunderea parolei
        if (PasswordEntry is not null)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            
            // Schimbă iconița în funcție de starea parolei
            if (ShowPasswordButton is not null)
            {
                ShowPasswordButton.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Curăță mesajele de eroare când pagina apare
        ErrorLabel.IsVisible = false;
        
        // Focus pe câmpul email
        EmailEntry.Focus();
    }
}
