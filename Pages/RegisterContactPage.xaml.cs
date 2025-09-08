using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class RegisterContactPage : ContentPage
{
    public RegisterContactPage()
    {
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(RegistrationSession.Draft?.Email))
            EmailEntry.Text = RegistrationSession.Draft.Email;
        if (!string.IsNullOrWhiteSpace(RegistrationSession.Draft?.Telefon))
            TelefonEntry.Text = RegistrationSession.Draft.Telefon;
    }

    private async void OnNext(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        if (string.IsNullOrWhiteSpace(EmailEntry.Text) || !EmailEntry.Text.Contains("@")) { ShowError("Email invalid."); return; }
        if (string.IsNullOrWhiteSpace(TelefonEntry.Text)) { ShowError("Telefon necesar."); return; }
        if (string.IsNullOrWhiteSpace(ParolaEntry.Text) || ParolaEntry.Text.Length < 4) { ShowError("Parola prea scurta."); return; }
        if (ParolaEntry.Text != ConfirmParolaEntry.Text) { ShowError("Parolele nu coincid."); return; }
        RegistrationSession.SetContact(EmailEntry.Text.Trim(), TelefonEntry.Text.Trim(), ParolaEntry.Text);
        await Shell.Current.GoToAsync(nameof(RegisterBirthPage));
    }

    private async void OnBack(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(".." );
    }

    private void OnCancel(object sender, EventArgs e)
    {
        RegistrationSession.Clear();
        Shell.Current.GoToAsync("//LoginPage");
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }
}