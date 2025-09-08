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
        if (!string.IsNullOrWhiteSpace(Name))
        {
            var decoded = Uri.UnescapeDataString(Name);
            MessageLabel.Text = $"Welcome, {decoded}!";
        }
    }

    private void OnDoneClicked(object sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//LoginPage");
    }
}