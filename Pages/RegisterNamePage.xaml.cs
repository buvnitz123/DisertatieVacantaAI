using MauiAppDisertatieVacantaAI.Classes.Session;

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
        bool isValid = !string.IsNullOrWhiteSpace(NumeEntry.Text?.Trim()) && 
                      !string.IsNullOrWhiteSpace(PrenumeEntry.Text?.Trim());
        
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
        
        // Validation with visual feedback
        if (string.IsNullOrWhiteSpace(nume))
        {
            ShowError("Te rog introdu numele de familie", NumeFrame);
            NumeEntry.Focus();
            return;
        }
        
        if (string.IsNullOrWhiteSpace(prenume))
        {
            ShowError("Te rog introdu prenumele", PrenumeFrame);
            PrenumeEntry.Focus();
            return;
        }

        // Additional validation
        if (nume.Length < 2)
        {
            ShowError("Numele trebuie sa aiba cel putin 2 caractere", NumeFrame);
            NumeEntry.Focus();
            return;
        }

        if (prenume.Length < 2)
        {
            ShowError("Prenumele trebuie sa aiba cel putin 2 caractere", PrenumeFrame);
            PrenumeEntry.Focus();
            return;
        }

        // Disable button and show loading state
        ContinueButton.IsEnabled = false;
        ContinueButton.Text = "Se incarca...";
        
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
            ContinueButton.Text = "Continua";
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