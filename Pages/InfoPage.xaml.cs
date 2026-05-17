namespace MauiAppDisertatieVacantaAI.Pages;

public partial class InfoPage : ContentPage
{
    public InfoPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        VersionLabel.Text = $"Versiune {AppInfo.Current.VersionString}";
    }
}
