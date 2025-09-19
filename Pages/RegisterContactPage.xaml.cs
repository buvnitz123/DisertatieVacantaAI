using MauiAppDisertatieVacantaAI.Classes.Session;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.Library;

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
            
        ValidateForm();
    }

    private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
    {
        EmailFrame.BorderColor = Colors.Transparent;
        ValidateEmail();
        ValidateForm();
    }

    private void OnParolaTextChanged(object sender, TextChangedEventArgs e)
    {
        ParolaFrame.BorderColor = Colors.Transparent;
        UpdatePasswordStrength(); // Only for UI feedback, not validation
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
        ShowPasswordButton.Text = _isPasswordVisible ? "🙈" : "👁️";
    }

    private void ValidateEmail()
    {
        var email = EmailEntry.Text?.Trim();
        bool isValid = ValidationUtils.IsValidEmail(email);
        EmailValidIcon.IsVisible = isValid;
    }

    private void UpdatePasswordStrength()
    {
        var password = ParolaEntry.Text ?? "";
        var strength = ValidationUtils.CalculatePasswordStrength(password);
        
        // Reset all bars
        StrengthBar1.BackgroundColor = Color.FromArgb("#EEEEEE");
        StrengthBar2.BackgroundColor = Color.FromArgb("#EEEEEE");
        StrengthBar3.BackgroundColor = Color.FromArgb("#EEEEEE");
        StrengthBar4.BackgroundColor = Color.FromArgb("#EEEEEE");
        
        var description = ValidationUtils.GetPasswordStrengthDescription(strength);
        StrengthLabel.Text = description;
        
        switch (strength)
        {
            case 0:
                StrengthLabel.TextColor = Color.FromArgb("#999999");
                break;
            case 1:
                StrengthBar1.BackgroundColor = Color.FromArgb("#FF6B6B");
                StrengthLabel.TextColor = Color.FromArgb("#FF6B6B");
                break;
            case 2:
                StrengthBar1.BackgroundColor = Color.FromArgb("#FFD93D");
                StrengthBar2.BackgroundColor = Color.FromArgb("#FFD93D");
                StrengthLabel.TextColor = Color.FromArgb("#FFD93D");
                break;
            case 3:
                StrengthBar1.BackgroundColor = Color.FromArgb("#6BCF7F");
                StrengthBar2.BackgroundColor = Color.FromArgb("#6BCF7F");
                StrengthBar3.BackgroundColor = Color.FromArgb("#6BCF7F");
                StrengthLabel.TextColor = Color.FromArgb("#6BCF7F");
                break;
            case 4:
                StrengthBar1.BackgroundColor = Color.FromArgb("#4CAF50");
                StrengthBar2.BackgroundColor = Color.FromArgb("#4CAF50");
                StrengthBar3.BackgroundColor = Color.FromArgb("#4CAF50");
                StrengthBar4.BackgroundColor = Color.FromArgb("#4CAF50");
                StrengthLabel.TextColor = Color.FromArgb("#4CAF50");
                break;
        }
    }

    private void ValidatePasswordMatch()
    {
        var password = ParolaEntry.Text ?? "";
        var confirmPassword = ConfirmParolaEntry.Text ?? "";
        
        bool matches = ValidationUtils.DoPasswordsMatch(password, confirmPassword) && 
                      !string.IsNullOrEmpty(password);
                      
        PasswordMatchIcon.IsVisible = matches;
    }

    private void ValidateForm()
    {
        var email = EmailEntry.Text?.Trim();
        var parola = ParolaEntry.Text;
        var confirmParola = ConfirmParolaEntry.Text;

        // Only check for empty fields and email format, no other restrictions
        bool isValid = ValidationUtils.IsValidContactForm(email, parola, confirmParola);

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
        var parola = ParolaEntry.Text;
        var confirmParola = ConfirmParolaEntry.Text;

        ErrorLabel.IsVisible = false;

        // Simplified validation - only check for empty fields and basic format
        if (!ValidationUtils.IsValidEmail(email))
        {
            var message = string.IsNullOrWhiteSpace(email) 
                ? "Te rog introdu adresa de email" 
                : ValidationUtils.GetEmailValidationMessage();
            ShowError(message, EmailFrame);
            EmailEntry.Focus();
            return;
        }

        // Check if email already exists
        if (_repo.EmailExists(email))
        {
            ShowError("Această adresă de email este deja folosită", EmailFrame);
            EmailEntry.Focus();
            return;
        }

        if (!ValidationUtils.IsValidPassword(parola))
        {
            ShowError(ValidationUtils.GetPasswordValidationMessage(), ParolaFrame);
            ParolaEntry.Focus();
            return;
        }

        if (!ValidationUtils.DoPasswordsMatch(parola, confirmParola))
        {
            ShowError(ValidationUtils.GetPasswordMismatchMessage(), ConfirmParolaFrame);
            ConfirmParolaEntry.Focus();
            return;
        }

        // Show loading state
        ContinueButton.IsEnabled = false;
        ContinueButton.Text = "Se verifică...";

        try
        {
            // Small delay to show loading state
            await Task.Delay(500);
            
            // Remove telefon from session - set empty string as placeholder
            RegistrationSession.SetContact(email, "", parola);
            await Shell.Current.GoToAsync(nameof(RegisterBirthPage));
        }
        catch (Exception ex)
        {
            ShowError($"Eroare: {ex.Message}");
        }
        finally
        {
            ContinueButton.IsEnabled = true;
            ContinueButton.Text = "Continuă";
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
}