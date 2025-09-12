using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library;
using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class ProfilePhotoPage : ContentPage
{
    private readonly UtilizatorRepository _repo;
    private byte[] _pendingImageBytes;
    private bool _isBusy;

    public ProfilePhotoPage()
    {
        InitializeComponent();
        _repo = new UtilizatorRepository();
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnPickOrCaptureTapped;
        PreviewImage.GestureRecognizers.Add(tap);
        TapHint.GestureRecognizers.Add(tap);
#if ANDROID
        // unified action sheet now
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (RegistrationSession.Draft == null)
        {
            ShowError("Registration session expired.");
        }
        else
        {
            StatusLabel.Text = $"User: {RegistrationSession.Draft.Nume} {RegistrationSession.Draft.Prenume}";
        }
        if (PreviewImage.Source == null)
        {
            PreviewImage.Source = "profile_default.png"; // default placeholder
        }
    }

    private async void OnPickOrCaptureTapped(object sender, EventArgs e)
    {
        if (_isBusy) return;
#if ANDROID
        string action = await DisplayActionSheet("Poza profil", "Anuleaza", null, "Fa o poza", "Alege din galerie");
        if (action == "Fa o poza")
        {
            await SafeRunAsync(CaptureAsync);
        }
        else if (action == "Alege din galerie")
        {
            await SafeRunAsync(PickImageAsync);
        }
#endif
    }

    private async Task SafeRunAsync(Func<Task> op)
    {
        try
        {
            await op();
        }
        catch (Exception ex)
        {
            var msg = $"Operation failed: {ex.Message}\n{ex}";
            Debug.WriteLine(msg);
            ShowError(msg);
        }
    }

    private async Task CaptureAsync()
    {
        HideError();
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                await LoadImageFromFileResult(photo);
                StatusLabel.Text = "Poza capturata.";
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Capture failed", ex);
        }
    }

    private async Task PickImageAsync()
    {
        HideError();
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Selecteaza imagine"
            });

            if (result != null)
            {
                await LoadImageFromFileResult(result);
                StatusLabel.Text = "Imagine pregatita.";
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Pick failed", ex);
        }
    }

    private async Task LoadImageFromFileResult(FileResult file)
    {
        try
        {
            using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _pendingImageBytes = ms.ToArray();
            PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_pendingImageBytes));
            TapHint.IsVisible = false;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Image load failed", ex);
        }
    }

    private async void OnFinishClicked(object sender, EventArgs e)
    {
        if (_isBusy) return;
        await FinalizeRegistrationAsync(includePhoto: true);
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
            ShowError("No registration data.");
            return;
        }

        try
        {
            SetBusy(true);
            var draft = RegistrationSession.Draft;

            if (_repo.EmailExists(draft.Email))
            {
                ShowError("Email already exists.");
                return;
            }

            int newId = _repo.GenerateNextId();
            Debug.WriteLine($"[ProfilePhoto] Generated next ID: {newId}");

            string relativePath = null;
            if (includePhoto && _pendingImageBytes != null) 
            {
                try
                {
                    // Upload with fixed name; store only relative path in DB
                    var blobName = $"profiles/profile_user_{newId}.jpg";
                    var contentType = AzureBlobService.GetContentTypeFromFileName(blobName);
                    var _ = await AzureBlobService.UploadImageWithFixedNameAsync(_pendingImageBytes, blobName, contentType);
                    relativePath = blobName;
                }
                catch (Exception ex)
                {
                    ShowError($"Photo upload failed: {ex.Message}");
                    return;
                }
            }

            var user = new Utilizator
            {
                Id_Utilizator = newId,
                Nume = draft.Nume,
                Prenume = draft.Prenume,
                Email = draft.Email,
                Parola = EncryptionUtils.Encrypt(draft.Parola),
                PozaProfil = relativePath, // store relative path to fit DB size
                Data_Nastere = draft.DataNastere ?? new DateTime(2000,1,1),
                Telefon = draft.Telefon,
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
                ShowError($"DB save failed: {exSave.Message}{(inner != null ? "\nInner: " + inner : "")}");
                Debug.WriteLine($"[ProfilePhoto] Insert exception: {exSave}");
                return;
            }

            var userName = Uri.EscapeDataString($"{user.Nume} {user.Prenume}");
            RegistrationSession.Clear();
            await Shell.Current.GoToAsync($"{nameof(WelcomePage)}?name={userName}");
        }
        catch (Exception ex)
        {
            ShowError($"Finalize failed: {ex.Message}\n{ex}");
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
    }

    private void HideError() => ErrorLabel.IsVisible = false;

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
    }
}