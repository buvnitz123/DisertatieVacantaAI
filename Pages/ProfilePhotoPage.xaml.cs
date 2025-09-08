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
        tap.Tapped += OnPickImageTapped;
        PreviewImage.GestureRecognizers.Add(tap);
        TapHint.GestureRecognizers.Add(tap);

#if ANDROID || IOS || MACCATALYST || WINDOWS
        var longPress = new TapGestureRecognizer { NumberOfTapsRequired = 2 }; // simulate long by double tap
        longPress.Tapped += OnCaptureImageTapped;
        PreviewImage.GestureRecognizers.Add(longPress);
        TapHint.GestureRecognizers.Add(longPress);
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
    }

    private async void OnPickImageTapped(object sender, EventArgs e)
    {
        if (_isBusy) return;
        await PickImageAsync();
    }

    private async void OnCaptureImageTapped(object sender, EventArgs e)
    {
#if ANDROID || IOS || MACCATALYST
        if (_isBusy) return;
        HideError();
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                using var stream = await photo.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                _pendingImageBytes = ms.ToArray();
                PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_pendingImageBytes));
                TapHint.IsVisible = false;
                StatusLabel.Text = "Imagine capturat?.";
            }
        }
        catch (Exception ex)
        {
            ShowError($"Camera error: {ex.Message}");
        }
#endif
    }

    private async Task PickImageAsync()
    {
        HideError();
#if ANDROID || IOS || MACCATALYST || WINDOWS
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Selecteaz? imagine"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                _pendingImageBytes = ms.ToArray();
                PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_pendingImageBytes));
                TapHint.IsVisible = false;
                StatusLabel.Text = "Imagine preg?tit?.";
            }
        }
        catch (Exception ex)
        {
            ShowError($"Image selection failed: {ex.Message}");
        }
#endif
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

            int newId = _repo.GenerateTimeBasedId();
            Debug.WriteLine($"[ProfilePhoto] Generated time-based ID: {newId}");

            string photoRelativePath = null;
            if (includePhoto && _pendingImageBytes != null)
            {
                try
                {
                    var fileName = $"profiles/profile_{newId}.jpg";
                    var contentType = S3Utils.GetContentTypeFromFileName("profile.jpg");
                    var _ = await S3Utils.UploadImageAsync(_pendingImageBytes, fileName, contentType);
                    photoRelativePath = fileName;
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
                PozaProfil = photoRelativePath,
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
            ShowError($"Finalize failed: {ex.Message}");
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