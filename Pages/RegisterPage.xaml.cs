using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class RegisterPage : ContentPage
{
    private bool _isBusy;

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        if (_isBusy) return;
        HideError();

        if (!ValidateInputs(out string error))
        {
            ShowError(error);
            return;
        }

        try
        {
            SetBusy(true);

            // Store draft (no DB insert yet)
            var draft = new RegistrationDraft
            {
                Nume = NumeEntry.Text!.Trim(),
                Prenume = PrenumeEntry.Text!.Trim(),
                Email = EmailEntry.Text!.Trim(),
                Parola = PasswordEntry.Text!,   // TODO: hash later
                DataNastere = DataNasterePicker.Date,
                Telefon = TelefonEntry.Text!.Trim()
            };
            RegistrationSession.SetDraft(draft);
            Debug.WriteLine("[Register] Draft stored. Navigating to profile photo step.");

            // IMPORTANT: Use relative route (registered page), NOT //ProfilePhotoPage
            await Shell.Current.GoToAsync(nameof(ProfilePhotoPage));
        }
        catch (Exception ex)
        {
            ShowError($"Unexpected error: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        RegistrationSession.Clear();
        Shell.Current.GoToAsync("//LoginPage");
    }

    private bool ValidateInputs(out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(NumeEntry.Text)) { error = "First name required."; return false; }
        if (string.IsNullOrWhiteSpace(PrenumeEntry.Text)) { error = "Last name required."; return false; }
        if (string.IsNullOrWhiteSpace(EmailEntry.Text) || !EmailEntry.Text.Contains("@")) { error = "Valid email required."; return false; }
        if (string.IsNullOrWhiteSpace(TelefonEntry.Text)) { error = "Phone required."; return false; }
        if (PasswordEntry.Text?.Length < 4) { error = "Password too short."; return false; }
        if (PasswordEntry.Text != ConfirmPasswordEntry.Text) { error = "Passwords do not match."; return false; }
        var age = (int)((DateTime.Today - DataNasterePicker.Date).TotalDays / 365.25);
        if (age < 10) { error = "Age must be at least 10."; return false; }
        return true;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void HideError() => ErrorLabel.IsVisible = false;

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
    }
}