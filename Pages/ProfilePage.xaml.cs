using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.Session;
using System.Diagnostics;
using System.IO;
using MauiAppDisertatieVacantaAI.Classes.Services;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly UtilizatorRepository _repo = new UtilizatorRepository();
    private int _userId;
    private bool _hasCustomPhoto;
    private string _currentPhotoUrl;

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
            var idStr = await UserSession.GetUserIdAsync();
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out _userId))
            {
                NameLabel.Text = "Neautentificat";
                _hasCustomPhoto = false;
                _currentPhotoUrl = null;
                return;
            }
            var user = _repo.GetById(_userId);
            if (user == null)
            {
                NameLabel.Text = "Utilizator inexistent";
                _hasCustomPhoto = false;
                _currentPhotoUrl = null;
                return;
            }
            NameLabel.Text = $"{user.Nume} {user.Prenume}";
            EmailLabel.Text = user.Email;
            BirthValue.Text = user.Data_Nastere.ToString("dd MMM yyyy");
            StatusValue.Text = user.EsteActiv == 1 ? "Activ" : "Inactiv";
            InitialsLabel.Text = $"{(string.IsNullOrWhiteSpace(user.Nume)?"?":user.Nume[0])}{(string.IsNullOrWhiteSpace(user.Prenume)?"":user.Prenume[0])}".ToUpper();

            if (!string.IsNullOrWhiteSpace(user.PozaProfil))
            {
                try
                {
                    var path = user.PozaProfil;
                    // Normalize old relative values to absolute for display/state
                    if (!path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        var baseUrl = "https://vacantaai.blob.core.windows.net/vacantaai";
                        path = $"{baseUrl}/{path}";
                    }
                    // Cache busting to ensure latest image is fetched after updates
                    var uri = new Uri(path + (path.Contains("?") ? "&" : "?") + "v=" + DateTime.UtcNow.Ticks);
                    ProfileImage.Source = new UriImageSource { Uri = uri, CachingEnabled = true, CacheValidity = TimeSpan.FromHours(12) };
                    InitialsLabel.IsVisible = false;
                    _hasCustomPhoto = true;
                    _currentPhotoUrl = path;
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
        InitialsLabel.IsVisible = true;
        _hasCustomPhoto = false;
        _currentPhotoUrl = null;
    }

    private async Task<bool> EnsureCameraAndStoragePermissionsAsync()
    {
#if ANDROID
        var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (cameraStatus != PermissionStatus.Granted)
        {
            cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
                return false;
        }

        // For picking from gallery on Android 13+
        var readImagesStatus = await Permissions.CheckStatusAsync<Permissions.Photos>();
        if (readImagesStatus != PermissionStatus.Granted)
        {
            readImagesStatus = await Permissions.RequestAsync<Permissions.Photos>();
            if (readImagesStatus != PermissionStatus.Granted)
                return false;
        }
#endif
        return true;
    }

    private async Task SetProfileImageAsync(FileResult file)
    {
        if (file == null) return;
        try
        {
            // Prefer using FullPath if available and accessible (opens a fresh stream when needed)
            if (!string.IsNullOrEmpty(file.FullPath) && File.Exists(file.FullPath))
            {
                ProfileImage.Source = ImageSource.FromFile(file.FullPath);
                InitialsLabel.IsVisible = false;
                await UploadProfilePhotoAsync(file);
                return;
            }

            // Fallback: copy to memory to avoid using a disposed stream
            using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();
            ProfileImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            InitialsLabel.IsVisible = false;

            await UploadProfilePhotoAsync(bytes);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Nu s-a putut încărca imaginea: {ex.Message}", "OK");
            Debug.WriteLine($"SetProfileImageAsync error: {ex}\n{ex.StackTrace}");
        }
    }

    private async Task UploadProfilePhotoAsync(FileResult file)
    {
        try
        {
            using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            await UploadProfilePhotoAsync(ms.ToArray());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UploadProfilePhotoAsync(file) error: {ex}");
        }
    }

    private async Task UploadProfilePhotoAsync(byte[] bytes)
    {
        try
        {
            if (_userId <= 0) return;
            var blobName = $"profiles/profile_user_{_userId}.jpg";
            var contentType = AzureBlobService.GetContentTypeFromFileName(blobName);
            var url = await AzureBlobService.UploadImageWithFixedNameAsync(bytes, blobName, contentType);

            // Persist relative path to fit DB (50 chars)
            var relativePath = blobName;
            var user = _repo.GetById(_userId);
            if (user != null)
            {
                user.PozaProfil = relativePath;
                _repo.Update(user);
            }
            _hasCustomPhoto = true;
            _currentPhotoUrl = $"https://vacantaai.blob.core.windows.net/vacantaai/{relativePath}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Upload eșuat: {ex.Message}", "OK");
            Debug.WriteLine($"UploadProfilePhotoAsync(bytes) error: {ex}");
        }
    }

    private async Task DeleteProfilePhotoAsync()
    {
        try
        {
            if (!_hasCustomPhoto)
            {
                await DisplayAlert("Info", "Nu există o poză de profil de șters.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Confirmare", "Sigur ștergi poza de profil?", "Da", "Nu");
            if (!confirm) return;

            // Get the latest user state
            var user = _repo.GetById(_userId);
            if (user == null)
            {
                await DisplayAlert("Eroare", "Utilizator inexistent.", "OK");
                return;
            }

            var urlOrRelative = user.PozaProfil;
            if (string.IsNullOrWhiteSpace(urlOrRelative))
            {
                await DisplayAlert("Info", "Nu există o poză de profil de șters.", "OK");
                return;
            }

            // Normalize to absolute URL if relative
            var absoluteUrl = urlOrRelative.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? urlOrRelative
                : $"https://vacantaai.blob.core.windows.net/vacantaai/{urlOrRelative}";

            // Attempt blob deletion (best-effort)
            _ = AzureBlobService.DeleteImage(absoluteUrl);

            // Update DB: set null
            user.PozaProfil = null;
            _repo.Update(user);

            // UI: reset to default
            SetDefaultImage();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Ștergere eșuată: {ex.Message}", "OK");
            Debug.WriteLine($"DeleteProfilePhotoAsync error: {ex}");
        }
    }

    private async void OnProfileImageTapped(object sender, TappedEventArgs e)
    {
#if ANDROID
        try
        {
            // Always show the delete option; it will no-op on default image
            string action = await DisplayActionSheet("Foto profil", "Anulează", null, "Fă o poză", "Alege din galerie", "Șterge poza");
            if (action == "Fă o poză")
            {   
                try
                {
                    if (!await EnsureCameraAndStoragePermissionsAsync())
                    {
                        await DisplayAlert("Permisiuni", "Permisiunile pentru cameră/imagini sunt necesare.", "OK");
                        return;
                    }

                    var photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null)
                    {
                        await SetProfileImageAsync(photo);
                    }
                }
                catch (Exception exCam)
                {
                    await DisplayAlert("Eroare", $"Cameră indisponibilă: {exCam.Message}", "OK");
                    Debug.WriteLine($"Camera capture error: {exCam}\n{exCam.StackTrace}");
                }
            }
            else if (action == "Alege din galerie")
            {
                try
                {
                    if (!await EnsureCameraAndStoragePermissionsAsync())
                    {
                        await DisplayAlert("Permisiuni", "Permisiunile pentru acces la imagini sunt necesare.", "OK");
                        return;
                    }

                    var result = await FilePicker.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Images });
                    if (result != null)
                    {
                        await SetProfileImageAsync(result);
                    }
                }
                catch (Exception exPick)
                {
                    await DisplayAlert("Eroare", $"Selecție eșuată: {exPick.Message}", "OK");
                    Debug.WriteLine($"Gallery pick error: {exPick}\n{exPick.StackTrace}");
                }
            }
            else if (action == "Șterge poza")
            {
                await DeleteProfilePhotoAsync();
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
        await Shell.Current.GoToAsync(nameof(EditProfilePage));
    }

    private async void OnLogout(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Confirmare", "Sigur vrei să te deconectezi?", "Da", "Nu");
        if (confirm)
        {
            UserSession.ClearSession();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}