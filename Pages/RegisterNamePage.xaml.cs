using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class RegisterNamePage : ContentPage
{
    public RegisterNamePage()
    {
        InitializeComponent();
        RegistrationSession.EnsureDraft();
        if (!string.IsNullOrWhiteSpace(RegistrationSession.Draft?.Nume))
            NumeEntry.Text = RegistrationSession.Draft.Nume;
        if (!string.IsNullOrWhiteSpace(RegistrationSession.Draft?.Prenume))
            PrenumeEntry.Text = RegistrationSession.Draft.Prenume;
    }

    private async void OnNext(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        if (string.IsNullOrWhiteSpace(NumeEntry.Text)) { ShowError("Introdu numele."); return; }
        if (string.IsNullOrWhiteSpace(PrenumeEntry.Text)) { ShowError("Introdu prenumele."); return; }
        RegistrationSession.SetName(NumeEntry.Text.Trim(), PrenumeEntry.Text.Trim());
        await Shell.Current.GoToAsync(nameof(RegisterContactPage));
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }

    private void OnCancel(object sender, EventArgs e)
    {
        RegistrationSession.Clear();
        Shell.Current.GoToAsync("//LoginPage");
    }
}