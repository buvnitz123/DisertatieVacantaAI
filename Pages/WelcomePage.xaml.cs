using MauiAppDisertatieVacantaAI.Classes.Library.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

[QueryProperty(nameof(Name), "name")]
public partial class WelcomePage : ContentPage
{
    public string Name { get; set; }

    public WelcomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Update welcome message with user name
        if (!string.IsNullOrWhiteSpace(Name))
        {
            var decoded = Uri.UnescapeDataString(Name);
            MessageLabel.Text = $"Bine ai venit, {decoded}! Contul tau a fost creat cu succes.";
        }
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        // Add button press animation
        await ((Button)sender).ScaleTo(0.95, 100);
        await ((Button)sender).ScaleTo(1, 100);
        
        // Clear any remaining session data
        RegistrationSession.Clear();
        
        // Navigate to login page
        await Shell.Current.GoToAsync("//LoginPage");
    }
}