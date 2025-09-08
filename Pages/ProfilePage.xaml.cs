using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly UtilizatorRepository _repo = new UtilizatorRepository();

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var idStr = await SecureStorage.GetAsync("UserId");
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out var id))
            {
                NameLabel.Text = "Neautentificat";
                return;
            }
            var user = _repo.GetById(id);
            if (user == null)
            {
                NameLabel.Text = "Utilizator inexistent";
                return;
            }
            NameLabel.Text = $"{user.Nume} {user.Prenume}";
            EmailLabel.Text = user.Email;
            PhoneValue.Text = user.Telefon;
            BirthValue.Text = user.Data_Nastere.ToString("dd MMM yyyy");
            StatusValue.Text = user.EsteActiv == 1 ? "Activ" : "Inactiv";
            InitialsLabel.Text = $"{(string.IsNullOrWhiteSpace(user.Nume)?"?":user.Nume[0])}{(string.IsNullOrWhiteSpace(user.Prenume)?"":user.Prenume[0])}".ToUpper();

            if (!string.IsNullOrWhiteSpace(user.PozaProfil))
            {
                try
                {
                    // Build full URL if only relative path stored
                    var path = user.PozaProfil;
                    if (!path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        // hardcode base - replace with config if needed
                        var baseUrl = "https://vacantaai.blob.core.windows.net/vacantaai"; // container URL
                        path = $"{baseUrl}/{path}";
                    }
                    ProfileImage.Source = new UriImageSource { Uri = new Uri(path), CachingEnabled = true, CacheValidity = TimeSpan.FromHours(12) };
                    InitialsLabel.IsVisible = false;
                }
                catch (Exception exImg)
                {
                    Debug.WriteLine($"Image load error: {exImg.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Profile load error: {ex.Message}");
        }
    }

    private async void OnEditProfile(object sender, EventArgs e)
    {
        await DisplayAlert("Info", "Editare profil - in curand.", "OK");
    }
}