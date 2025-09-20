using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.Library;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class EditProfilePage : ContentPage
{
    private readonly UtilizatorRepository _repo = new UtilizatorRepository();
    private int _userId;
    private Utilizator _currentUser;
    private bool _isBusy = false;

    public EditProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserDataAsync();
    }

    private async Task LoadUserDataAsync()
    {
        try
        {
            var idStr = await UserSession.GetUserIdAsync();
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out _userId))
            {
                await DisplayAlert("Eroare", "Sesiune invalid?. Te rog autentific?-te din nou.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            _currentUser = _repo.GetById(_userId);
            if (_currentUser == null)
            {
                await DisplayAlert("Eroare", "Utilizatorul nu a fost g?sit.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            // Populate form fields (without telefon)
            NumeEntry.Text = _currentUser.Nume;
            PrenumeEntry.Text = _currentUser.Prenume;
            EmailEntry.Text = _currentUser.Email;
            DataNasteriiPicker.Date = _currentUser.Data_Nastere;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading user data: {ex.Message}");
            ShowError("Eroare la înc?rcarea datelor utilizatorului.");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_isBusy) return;

        try
        {
            SetBusy(true);
            HideError();

            if (!ValidateForm())
                return;

            // Check if email is unique (excluding current user)
            if (_repo.EmailExistsForOtherUser(EmailEntry.Text.Trim(), _userId))
            {
                ShowError("Aceast? adres? de email este deja folosit? de alt utilizator.", EmailFrame);
                return;
            }

            // Update user data (without telefon)
            _currentUser.Nume = NumeEntry.Text.Trim();
            _currentUser.Prenume = PrenumeEntry.Text.Trim();
            _currentUser.Email = EmailEntry.Text.Trim();
            _currentUser.Data_Nastere = DataNasteriiPicker.Date;

            // Handle password change if provided
            if (!string.IsNullOrWhiteSpace(CurrentPasswordEntry.Text))
            {
                if (!ValidatePasswordChange())
                    return;

                _currentUser.Parola = EncryptionUtils.Encrypt(NewPasswordEntry.Text);
            }

            // Save to database
            _repo.Update(_currentUser);

            await DisplayAlert("Succes", "Profilul a fost actualizat cu succes!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving user data: {ex.Message}");
            ShowError("Eroare la salvarea datelor. Te rog încearc? din nou.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private bool ValidateForm()
    {
        // Validate nume
        if (!ValidationUtils.IsValidNume(NumeEntry.Text))
        {
            var message = string.IsNullOrWhiteSpace(NumeEntry.Text) 
                ? ValidationUtils.GetNameValidationMessage("numele") 
                : ValidationUtils.GetNameLengthValidationMessage("Numele");
            ShowError(message, NumeFrame);
            NumeEntry.Focus();
            return false;
        }

        // Validate prenume
        if (!ValidationUtils.IsValidPrenume(PrenumeEntry.Text))
        {
            var message = string.IsNullOrWhiteSpace(PrenumeEntry.Text) 
                ? ValidationUtils.GetNameValidationMessage("prenumele") 
                : ValidationUtils.GetNameLengthValidationMessage("Prenumele");
            ShowError(message, PrenumeFrame);
            PrenumeEntry.Focus();
            return false;
        }

        // Validate email
        if (!ValidationUtils.IsValidEmail(EmailEntry.Text))
        {
            var message = string.IsNullOrWhiteSpace(EmailEntry.Text) 
                ? "Te rog introdu adresa de email." 
                : ValidationUtils.GetEmailValidationMessage();
            ShowError(message, EmailFrame);
            EmailEntry.Focus();
            return false;
        }

        // Validate data nasterii (only check for future date)
        if (!ValidationUtils.IsValidBirthDate(DataNasteriiPicker.Date))
        {
            ShowError(ValidationUtils.GetFutureDateValidationMessage(), DataNasteriiFrame);
            return false;
        }

        return true;
    }

    private bool ValidatePasswordChange()
    {
        // Verify current password
        var encryptedCurrentPassword = EncryptionUtils.Encrypt(CurrentPasswordEntry.Text);
        if (encryptedCurrentPassword != _currentUser.Parola)
        {
            ShowError("Parola curent? nu este corect?.");
            CurrentPasswordEntry.Focus();
            return false;
        }

        // Validate new password (simplified - only check if not empty)
        if (!ValidationUtils.IsValidPassword(NewPasswordEntry.Text))
        {
            ShowError(ValidationUtils.GetPasswordValidationMessage());
            NewPasswordEntry.Focus();
            return false;
        }

        // Confirm password match
        if (!ValidationUtils.DoPasswordsMatch(NewPasswordEntry.Text, ConfirmPasswordEntry.Text))
        {
            ShowError(ValidationUtils.GetPasswordMismatchMessage());
            ConfirmPasswordEntry.Focus();
            return false;
        }

        return true;
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Shell.Current.GoToAsync("..");
    }

    private void ShowError(string message, Frame targetFrame = null)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;

        // Add visual feedback to the problematic field
        if (targetFrame != null)
        {
            targetFrame.BorderColor = Color.FromArgb("#FF6B6B");
            targetFrame.HasShadow = false;

            // Remove border color after a delay
            Device.StartTimer(TimeSpan.FromSeconds(3), () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    targetFrame.BorderColor = Colors.Transparent;
                    targetFrame.HasShadow = true;
                });
                return false;
            });
        }

        // Gentle shake animation
        this.ScaleTo(0.98, 100)
            .ContinueWith(t => this.ScaleTo(1.0, 100));
    }

    private void HideError()
    {
        ErrorLabel.IsVisible = false;
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;

        // Disable form during busy state (removed telefon references)
        SaveButton.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
        NumeEntry.IsEnabled = !busy;
        PrenumeEntry.IsEnabled = !busy;
        EmailEntry.IsEnabled = !busy;
        DataNasteriiPicker.IsEnabled = !busy;
        CurrentPasswordEntry.IsEnabled = !busy;
        NewPasswordEntry.IsEnabled = !busy;
        ConfirmPasswordEntry.IsEnabled = !busy;
    }
}