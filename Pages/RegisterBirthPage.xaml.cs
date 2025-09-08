using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class RegisterBirthPage : ContentPage
{
    public RegisterBirthPage()
    {
        InitializeComponent();
        if (RegistrationSession.Draft?.DataNastere != null)
            BirthDatePicker.Date = RegistrationSession.Draft.DataNastere.Value;
        else
            BirthDatePicker.Date = new DateTime(2000,1,1);
    }

    private async void OnNext(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var date = BirthDatePicker.Date;
        var age = (int)((DateTime.Today - date).TotalDays / 365.25);
        if (age < 10)
        {
            ShowError("Trebuie s? ai minim 10 ani.");
            return;
        }
        RegistrationSession.SetBirthDate(date);
        await Shell.Current.GoToAsync(nameof(ProfilePhotoPage));
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