namespace MauiAppDisertatieVacantaAI.Pages;

public partial class NewSugestiePage : ContentPage
{
    public NewSugestiePage()
    {
        InitializeComponent();
    }

    private async void OnBack(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(".." );
    }
}