using MauiAppDisertatieVacantaAI.Classes.Session;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using System.Text.RegularExpressions;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class RegisterContactPage : ContentPage
{
    private readonly UtilizatorRepository _repo = new UtilizatorRepository();
    private bool _isPasswordVisible = false;

    public RegisterContactPage()
    {
        InitializeComponent();
        
        // Restore previous values if available
        if (!string.IsNullOrWhiteSpace(RegistrationSession.Draft?.Email))
            EmailEntry.Text = RegistrationSession.Draft.Email;
        if (!string.IsNullOrWhiteSpace(RegistrationSession.Draft?.Telefon))
            TelefonEntry.Text = RegistrationSession.Draft.Telefon;
            
        ValidateForm();
    }

    private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
    {
        EmailFrame.BorderColor = Colors.Transparent;
        ValidateEmail();
        ValidateForm();
    }

    private void OnTelefonTextChanged(object sender, TextChangedEventArgs e)
    {
        TelefonFrame.BorderColor = Colors.Transparent;
        ValidateForm();
    }

    private void OnParolaTextChanged(object sender, TextChangedEventArgs e)
    {
        ParolaFrame.BorderColor = Colors.Transparent;
        UpdatePasswordStrength();
        ValidatePasswordMatch();
        ValidateForm();
    }

    private void OnConfirmParolaTextChanged(object sender, TextChangedEventArgs e)
    {
        ConfirmParolaFrame.BorderColor = Colors.Transparent;
        ValidatePasswordMatch();
        ValidateForm();
    }

    private void OnShowPasswordClicked(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        ParolaEntry.IsPassword = !_isPasswordVisible;
        ConfirmParolaEntry.IsPassword = !_isPasswordVisible;
        ShowPasswordButton.Text = _isPasswordVisible ? "??" : "??";
    }

    private void ValidateEmail()
    {
        var email = EmailEntry.Text?.Trim();
        bool isValid = !string.IsNullOrEmpty(email) && IsValidEmail(email);
        EmailValidIcon.IsVisible = isValid;
    }

    private void UpdatePasswordStrength()
    {
        var password = ParolaEntry.Text ?? "";
        var strength = CalculatePasswordStrength(password);
        
        // Reset all bars
        StrengthBar1.BackgroundColor = Color.FromArgb("#EEEEEE");
        StrengthBar2.BackgroundColor = Color.FromArgb("#EEEEEE");
        StrengthBar3.BackgroundColor = Color.FromArgb("#EEEEEE");
        StrengthBar4.BackgroundColor = Color.FromArgb("#EEEEEE");
        
        switch (strength)
        {
            case 0:
                StrengthLabel.Text = "Introdu parola";
                StrengthLabel.TextColor = Color.FromArgb("#999999");
                break;
            case 1:
                StrengthBar1.BackgroundColor = Color.FromArgb("#FF6B6B");
                StrengthLabel.Text = "Slaba";
                StrengthLabel.TextColor = Color.FromArgb("#FF6B6B");
                break;
            case 2:
                StrengthBar1.BackgroundColor = Color.FromArgb("#FFD93D");
                StrengthBar2.BackgroundColor = Color.FromArgb("#FFD93D");
                StrengthLabel.Text = "Mediocra";
                StrengthLabel.TextColor = Color.FromArgb("#FFD93D");
                break;
            case 3:
                StrengthBar1.BackgroundColor = Color.FromArgb("#6BCF7F");
                StrengthBar2.BackgroundColor = Color.FromArgb("#6BCF7F");
                StrengthBar3.BackgroundColor = Color.FromArgb("#6BCF7F");
                StrengthLabel.Text = "Buna";
                StrengthLabel.TextColor = Color.FromArgb("#6BCF7F");
                break;
            case 4:
                StrengthBar1.BackgroundColor = Color.FromArgb("#4CAF50");
                StrengthBar2.BackgroundColor = Color.FromArgb("#4CAF50");
                StrengthBar3.BackgroundColor = Color.FromArgb("#4CAF50");
                StrengthBar4.BackgroundColor = Color.FromArgb("#4CAF50");
                StrengthLabel.Text = "Excelenta";
                StrengthLabel.TextColor = Color.FromArgb("#4CAF50");
                break;
        }
    }

    private void ValidatePasswordMatch()
    {
        var password = ParolaEntry.Text ?? "";
        var confirmPassword = ConfirmParolaEntry.Text ?? "";
        
        bool matches = !string.IsNullOrEmpty(password) && 
                      !string.IsNullOrEmpty(confirmPassword) && 
                      password == confirmPassword;
                      
        PasswordMatchIcon.IsVisible = matches;
    }

    private void ValidateForm()
    {
        var email = EmailEntry.Text?.Trim();
        var telefon = TelefonEntry.Text?.Trim();
        var parola = ParolaEntry.Text;
        var confirmParola = ConfirmParolaEntry.Text;

        bool isValid = IsValidEmail(email) &&
                      !string.IsNullOrWhiteSpace(telefon) &&
                      !string.IsNullOrWhiteSpace(parola) &&
                      parola.Length >= 6 &&
                      parola == confirmParola;

        ContinueButton.IsEnabled = isValid;
        ContinueButton.Opacity = isValid ? 1.0 : 0.6;

        // Hide error when form becomes valid
        if (isValid && ErrorLabel.IsVisible)
        {
            ErrorLabel.IsVisible = false;
        }
    }

    private async void OnNext(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var telefon = TelefonEntry.Text?.Trim();
        var parola = ParolaEntry.Text;
        var confirmParola = ConfirmParolaEntry.Text;

        ErrorLabel.IsVisible = false;

        // Detailed validation with visual feedback
        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
        {
            ShowError("Te rog introdu o adresa de email valida", EmailFrame);
            EmailEntry.Focus();
            return;
        }

        // Check if email already exists
        if (_repo.EmailExists(email))
        {
            ShowError("Aceasta adresa de email este deja folosita", EmailFrame);
            EmailEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(telefon))
        {
            ShowError("Te rog introdu numarul de telefon", TelefonFrame);
            TelefonEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(parola) || parola.Length < 6)
        {
            ShowError("Parola trebuie sa aiba cel putin 6 caractere", ParolaFrame);
            ParolaEntry.Focus();
            return;
        }

        if (parola != confirmParola)
        {
            ShowError("Parolele nu se potrivesc", ConfirmParolaFrame);
            ConfirmParolaEntry.Focus();
            return;
        }

        // Show loading state
        ContinueButton.IsEnabled = false;
        ContinueButton.Text = "Se verifica...";

        try
        {
            // Small delay to show loading state
            await Task.Delay(500);
            
            RegistrationSession.SetContact(email, telefon, parola);
            await Shell.Current.GoToAsync(nameof(RegisterBirthPage));
        }
        catch (Exception ex)
        {
            ShowError($"Eroare: {ex.Message}");
        }
        finally
        {
            ContinueButton.IsEnabled = true;
            ContinueButton.Text = "Continua";
        }
    }

    private async void OnBack(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnCancel(object sender, EventArgs e)
    {
        RegistrationSession.Clear();
        Shell.Current.GoToAsync("//LoginPage");
    }

    private void ShowError(string msg, Frame targetFrame = null)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;

        // Add visual feedback to the problematic field
        if (targetFrame != null)
        {
            targetFrame.BorderColor = Color.FromArgb("#FF6B6B");

            // Remove border color after a delay
            Device.StartTimer(TimeSpan.FromSeconds(3), () =>
            {
                targetFrame.BorderColor = Colors.Transparent;
                return false;
            });
        }

        // Gentle shake animation
        this.ScaleTo(0.98, 100)
            .ContinueWith(t => this.ScaleTo(1.0, 100));
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    private int CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return 0;

        int score = 0;

        // Length check
        if (password.Length >= 6) score++;
        if (password.Length >= 8) score++;

        // Character variety checks
        if (Regex.IsMatch(password, @"[a-z]") && Regex.IsMatch(password, @"[A-Z]")) score++;
        if (Regex.IsMatch(password, @"[0-9]")) score++;
        if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) score++;

        return Math.Min(score, 4);
    }
}