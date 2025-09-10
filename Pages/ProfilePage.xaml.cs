using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly UtilizatorRepository _repo = new UtilizatorRepository();
    private int _userId;

    public ProfilePage()
    {
        InitializeComponent();
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnProfileImageTapped;
        ProfileImage.GestureRecognizers.Add(tap);
        InitialsLabel.GestureRecognizers.Add(tap);
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
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out _userId))
            {
                NameLabel.Text = "Neautentificat";
                return;
            }
            var user = _repo.GetById(_userId);
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
                    var path = user.PozaProfil;
                    if (!path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        var baseUrl = "https://vacantaai.blob.core.windows.net/vacantaai";
                        path = $"{baseUrl}/{path}";
                    }
                    ProfileImage.Source = new UriImageSource { Uri = new Uri(path), CachingEnabled = true, CacheValidity = TimeSpan.FromHours(12) };
                    InitialsLabel.IsVisible = false;
                }
                catch (Exception exImg)
                {
                    Debug.WriteLine($"Image load error: {exImg.Message}\n{exImg}");
                    SetDefaultImage();
                }
            }
            else
            {
                SetDefaultImage();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Profile load error: {ex.Message}\n{ex}");
            SetDefaultImage();
        }
    }

    private void SetDefaultImage()
    {
        ProfileImage.Source = "profile_default.png"; // default placeholder image
        InitialsLabel.IsVisible = false;
    }

    private async void OnProfileImageTapped(object sender, TappedEventArgs e)
    {
#if ANDROID
        try
        {
            string action = await DisplayActionSheet("Foto profil", "Anuleaza", null, "Fa o poza", "Alege din galerie");
            if (action == "Fa o poza")
            {
                try
                {
                    var photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null)
                    {
                        using var stream = await photo.OpenReadAsync();
                        ProfileImage.Source = ImageSource.FromStream(() => stream);
                        InitialsLabel.IsVisible = false;
                    }
                }
                catch (Exception exCam)
                {
                    await DisplayAlert("Eroare", $"Camera indisponibila: {exCam.Message}", "OK");
                    Debug.WriteLine($"Camera capture error: {exCam}\n{exCam.StackTrace}");
                }
            }
            else if (action == "Alege din galerie")
            {
                try
                {
                    var result = await FilePicker.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Images });
                    if (result != null)
                    {
                        using var stream = await result.OpenReadAsync();
                        ProfileImage.Source = ImageSource.FromStream(() => stream);
                        InitialsLabel.IsVisible = false;
                    }
                }
                catch (Exception exPick)
                {
                    await DisplayAlert("Eroare", $"Selectie esuata: {exPick.Message}", "OK");
                    Debug.WriteLine($"Gallery pick error: {exPick}\n{exPick.StackTrace}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Profile image tap flow error: {ex}\n{ex.StackTrace}");
        }
#endif
    }

    private async void OnEditProfile(object sender, EventArgs e)
    {
        await DisplayAlert("Info", "Editare profil - in curand.", "OK");
    }
}