using MauiAppDisertatieVacantaAI.Classes.Library;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class RegisterNamePage : ContentPage
{
    public RegisterNamePage()
    {
        InitializeComponent();
        RegistrationSession.EnsureDraft();
        
        // Restore previous values if available
        if (!string.IsNullOrWhiteSpace(RegistrationSession.Draft?.Nume))
            NumeEntry.Text = RegistrationSession.Draft.Nume;
        if (!string.IsNullOrWhiteSpace(RegistrationSession.Draft?.Prenume))
            PrenumeEntry.Text = RegistrationSession.Draft.Prenume;
            
        ValidateForm();
    }

    private void OnNumeTextChanged(object sender, TextChangedEventArgs e)
    {
        // Reset frame border color
        NumeFrame.BorderColor = Colors.Transparent;
        ValidateForm();
    }

    private void OnPrenumeTextChanged(object sender, TextChangedEventArgs e)
    {
        // Reset frame border color
        PrenumeFrame.BorderColor = Colors.Transparent;
        ValidateForm();
    }

    private void ValidateForm()
    {
        bool isValid = ValidationUtils.IsValidNameForm(NumeEntry.Text, PrenumeEntry.Text);
        
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
        var nume = NumeEntry.Text?.Trim();
        var prenume = PrenumeEntry.Text?.Trim();
        
        ErrorLabel.IsVisible = false;
        
        // Validation with visual feedback using ValidationUtils
        if (!ValidationUtils.IsValidNume(nume))
        {
            var message = string.IsNullOrWhiteSpace(nume) 
                ? ValidationUtils.GetNameValidationMessage("numele de familie") 
                : ValidationUtils.GetNameLengthValidationMessage("Numele");
            ShowError(message, NumeFrame);
            NumeEntry.Focus();
            return;
        }
        
        if (!ValidationUtils.IsValidPrenume(prenume))
        {
            var message = string.IsNullOrWhiteSpace(prenume) 
                ? ValidationUtils.GetNameValidationMessage("prenumele") 
                : ValidationUtils.GetNameLengthValidationMessage("Prenumele");
            ShowError(message, PrenumeFrame);
            PrenumeEntry.Focus();
            return;
        }

        // Disable button and show loading state
        ContinueButton.IsEnabled = false;
        ContinueButton.Text = "Se încarc?...";
        
        try
        {
            RegistrationSession.SetName(nume, prenume);
            await Shell.Current.GoToAsync(nameof(RegisterContactPage));
        }
        catch (Exception ex)
        {
            ShowError($"Eroare: {ex.Message}");
        }
        finally
        {
            ContinueButton.IsEnabled = true;
            ContinueButton.Text = "Continu?";
        }
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

    private void OnCancel(object sender, EventArgs e)
    {
        RegistrationSession.Clear();
        Shell.Current.GoToAsync("//LoginPage");
    }
}