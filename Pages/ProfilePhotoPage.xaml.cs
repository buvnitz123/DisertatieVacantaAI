using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library;
using MauiAppDisertatieVacantaAI.Classes.Services;
using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class ProfilePhotoPage : ContentPage
{
    private readonly UtilizatorRepository _repo;
    private byte[] _pendingImageBytes;
    private bool _isBusy;
    private bool _hasSelectedPhoto = false;

    public ProfilePhotoPage()
    {
        InitializeComponent();
        _repo = new UtilizatorRepository();
        
        // Add tap gesture to the photo area
        var photoTapGesture = new TapGestureRecognizer();
        photoTapGesture.Tapped += OnPhotoAreaTapped;
        PhotoBorder.GestureRecognizers.Add(photoTapGesture);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        if (RegistrationSession.Draft == null)
        {
            ShowError("Sesiunea de înregistrare a expirat.");
            return;
        }
        
        // Show user preview
        var draft = RegistrationSession.Draft;
        UserPreviewLabel.Text = $"{draft.Nume} {draft.Prenume}";
        
        // Set default photo
        if (!_hasSelectedPhoto)
        {
            SetDefaultPhotoState();
        }
    }

    private void SetDefaultPhotoState()
    {
        PreviewImage.IsVisible = false;
        PlaceholderContent.IsVisible = true;
        PhotoActionsGrid.IsVisible = false;
        StatusLabel.IsVisible = false;
        _hasSelectedPhoto = false;
        
        // Update button text
        FinishButton.Text = "?? Finalizeaz? înregistrarea";
        SkipButton.Text = "Sari acest pas ?i finalizeaz?";
    }

    private void SetPhotoSelectedState()
    {
        PreviewImage.IsVisible = true;
        PlaceholderContent.IsVisible = false;
        PhotoActionsGrid.IsVisible = true;
        StatusLabel.IsVisible = true;
        StatusLabel.Text = "Poza selectata cu succes! ?";
        StatusLabel.TextColor = Color.FromArgb("#4CAF50");
        _hasSelectedPhoto = true;
        
        // Update button text
        FinishButton.Text = "?? Finalizeaza cu poza selectata";
        SkipButton.Text = "Finalizeaza fara poza";
    }

    private async void OnPhotoAreaTapped(object sender, TappedEventArgs e)
    {
        await ShowPhotoOptionsAsync();
    }

    private async void OnAddPhotoClicked(object sender, EventArgs e)
    {
        await ShowPhotoOptionsAsync();
    }

    private async Task ShowPhotoOptionsAsync()
    {
        if (_isBusy) return;

#if ANDROID
        string action = await DisplayActionSheet(
            "Selecteaz? sursa pentru poza de profil", 
            "Anuleaz?", 
            null, 
            "?? F? o poz?", 
            "??? Alege din galerie");

        if (action == "?? F? o poz?")
        {
            await CameraClickedAsync();
        }
        else if (action == "??? Alege din galerie")
        {
            await GalleryClickedAsync();
        }
#endif
    }

    private async Task CameraClickedAsync()
    {
        if (_isBusy) return;
        
        try
        {
            if (!await EnsurePermissionsAsync())
            {
                ShowError("Permisiunile pentru camer? sunt necesare pentru a face o poza.");
                return;
            }

            await SafeRunAsync(CapturePhotoAsync);
        }
        catch (Exception ex)
        {
            ShowError($"Eroare la accesarea camerei: {ex.Message}");
        }
    }

    private async Task GalleryClickedAsync()
    {
        if (_isBusy) return;
        
        try
        {
            if (!await EnsurePermissionsAsync())
            {
                ShowError("Permisiunile pentru acces la imagini sunt necesare.");
                return;
            }

            await SafeRunAsync(PickFromGalleryAsync);
        }
        catch (Exception ex)
        {
            ShowError($"Eroare la accesarea galeriei: {ex.Message}");
        }
    }

    private async void OnCameraClicked(object sender, EventArgs e)
    {
        await CameraClickedAsync();
    }

    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        await GalleryClickedAsync();
    }

    private async Task<bool> EnsurePermissionsAsync()
    {
#if ANDROID
        var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (cameraStatus != PermissionStatus.Granted)
        {
            cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
                return false;
        }

        var photosStatus = await Permissions.CheckStatusAsync<Permissions.Photos>();
        if (photosStatus != PermissionStatus.Granted)
        {
            photosStatus = await Permissions.RequestAsync<Permissions.Photos>();
            if (photosStatus != PermissionStatus.Granted)
                return false;
        }
#endif
        return true;
    }

    private async Task SafeRunAsync(Func<Task> operation)
    {
        try
        {
            HideError();
            SetBusy(true);
            await operation();
        }
        catch (Exception ex)
        {
            var msg = $"Opera?ia a e?uat: {ex.Message}";
            Debug.WriteLine(msg);
            ShowError(msg);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CapturePhotoAsync()
    {
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                await LoadImageFromFileResultAsync(photo);
                SetPhotoSelectedState();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Capturarea imaginii a e?uat", ex);
        }
    }

    private async Task PickFromGalleryAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Selecteaz? imagine pentru profil"
            });

            if (result != null)
            {
                await LoadImageFromFileResultAsync(result);
                SetPhotoSelectedState();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Selectarea imaginii a e?uat", ex);
        }
    }

    private async Task LoadImageFromFileResultAsync(FileResult file)
    {
        try
        {
            using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _pendingImageBytes = ms.ToArray();
            
            // Display the image
            PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_pendingImageBytes));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Înc?rcarea imaginii a e?uat", ex);
        }
    }

    private async void OnFinishClicked(object sender, EventArgs e)
    {
        if (_isBusy) return;
        await FinalizeRegistrationAsync(includePhoto: _hasSelectedPhoto);
    }

    private async void OnSkipClicked(object sender, EventArgs e)
    {
        if (_isBusy) return;
        await FinalizeRegistrationAsync(includePhoto: false);
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        RegistrationSession.Clear();
        Shell.Current.GoToAsync("//LoginPage");
    }

    private async Task FinalizeRegistrationAsync(bool includePhoto)
    {
        if (RegistrationSession.Draft == null)
        {
            ShowError("Nu exista date de inregistrare.");
            return;
        }

        try
        {
            SetBusy(true, "Se finalizeaza inregistrarea...");
            var draft = RegistrationSession.Draft;

            // Check if email already exists
            if (_repo.EmailExists(draft.Email))
            {
                ShowError("Aceasta adresa de email este deja folosita.");
                return;
            }

            int newId = _repo.GenerateNextId();
            Debug.WriteLine($"[ProfilePhoto] Generated next ID: {newId}");

            string relativePath = null;
            if (includePhoto && _pendingImageBytes != null)
            {
                try
                {
                    SetBusy(true, "Se incarca poza de profil...");
                    
                    // Upload with fixed name; store only relative path in DB
                    var blobName = $"profiles/profile_user_{newId}.jpg";
                    var contentType = AzureBlobService.GetContentTypeFromFileName(blobName);
                    var _ = await AzureBlobService.UploadImageWithFixedNameAsync(_pendingImageBytes, blobName, contentType);
                    relativePath = blobName;
                }
                catch (Exception ex)
                {
                    ShowError($"Incarcarea pozei a esuat: {ex.Message}");
                    return;
                }
            }

            SetBusy(true, "Se salveaza datele utilizatorului...");

            var user = new Utilizator
            {
                Id_Utilizator = newId,
                Nume = draft.Nume,
                Prenume = draft.Prenume,
                Email = draft.Email,
                Parola = EncryptionUtils.Encrypt(draft.Parola),
                PozaProfil = relativePath,
                Data_Nastere = draft.DataNastere ?? new DateTime(2000, 1, 1),
                EsteActiv = 1
            };

            try
            {
                _repo.Insert(user);
                Debug.WriteLine($"[ProfilePhoto] User inserted with ID: {user.Id_Utilizator}");
            }
            catch (Exception exSave)
            {
                var inner = exSave.InnerException?.ToString();
                ShowError($"Salvarea in baza de date a esuat: {exSave.Message}{(inner != null ? "\nInner: " + inner : "")}");
                Debug.WriteLine($"[ProfilePhoto] Insert exception: {exSave}");
                return;
            }

            // Navigate to welcome page
            var userName = Uri.EscapeDataString($"{user.Nume} {user.Prenume}");
            RegistrationSession.Clear();
            await Shell.Current.GoToAsync($"{nameof(WelcomePage)}?name={userName}");
        }
        catch (Exception ex)
        {
            ShowError($"Finalizarea inregistrarii a esuat: {ex.Message}");
            Debug.WriteLine($"[ProfilePhoto] Finalization error: {ex}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
        
        // Gentle shake animation
        this.ScaleTo(0.98, 100)
            .ContinueWith(t => this.ScaleTo(1.0, 100));
    }

    private void HideError()
    {
        ErrorLabel.IsVisible = false;
    }

    private void SetBusy(bool busy, string message = "Se incarca...")
    {
        _isBusy = busy;
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
        
        // Disable buttons during busy state
        FinishButton.IsEnabled = !busy;
        SkipButton.IsEnabled = !busy;
        AddPhotoButton.IsEnabled = !busy;
        
        if (busy)
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = Color.FromArgb("#0092ca");
            StatusLabel.IsVisible = true;
        }
    }
}