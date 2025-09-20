using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Session;
using MauiAppDisertatieVacantaAI.Classes.Library;
using System.Collections.ObjectModel;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class NewSugestiePage : ContentPage
{
    private readonly SugestieRepository _sugestieRepo = new SugestieRepository();
    private readonly DestinatieRepository _destinatieRepo = new DestinatieRepository();
    private readonly ObservableCollection<Destinatie> _destinations = new ObservableCollection<Destinatie>();
    private int _userId;

    public NewSugestiePage()
    {
        InitializeComponent();
        LoadDestinations();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeUserAsync();
    }

    private async Task InitializeUserAsync()
    {
        try
        {
            var idStr = await UserSession.GetUserIdAsync();
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out _userId))
            {
                await DisplayAlert("Eroare", "Nu s-a putut identifica utilizatorul. Te rog sa te loghezi din nou.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Eroare la initializarea utilizatorului: {ex.Message}", "OK");
        }
    }

    private async void LoadDestinations()
    {
        try
        {
            var destinations = _destinatieRepo.GetAll();
            _destinations.Clear();
            
            foreach (var destination in destinations.OrderBy(d => d.Denumire))
            {
                _destinations.Add(destination);
            }

            var picker = this.FindByName<Picker>("DestinatiePicker");
            picker.ItemsSource = _destinations;
            picker.ItemDisplayBinding = new Binding("Denumire");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Nu s-au putut incarca destinatiile: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!ValidateForm())
            return;

        try
        {
            SetLoadingState(true);

            var picker = this.FindByName<Picker>("DestinatiePicker");
            var selectedDestination = picker.SelectedItem as Destinatie;
            
            var titluEntry = this.FindByName<Entry>("TitluEntry");
            var descriereEditor = this.FindByName<Editor>("DescriereEditor");
            var bugetEntry = this.FindByName<Entry>("BugetEntry");
            
            var newSugestie = new Sugestie
            {
                Titlu = titluEntry.Text.Trim(),
                Descriere = descriereEditor.Text.Trim(),
                Buget_Estimat = decimal.Parse(bugetEntry.Text.Trim()),
                Id_Destinatie = selectedDestination.Id_Destinatie,
                Id_Utilizator = _userId,
                Data_Inregistrare = DateTime.Now,
                EsteGenerataDeAI = 0, // Manual creation
                EstePublic = 0, // Default to private as requested
                CodPartajare = null // Will be generated after we get the ID
            };

            // Insert the suggestion first to get the generated ID
            _sugestieRepo.Insert(newSugestie);

            // Now generate the share code using the ID and title
            var shareCode = GenerateShareCode(newSugestie.Id_Sugestie, newSugestie.Titlu);
            
            // Update the suggestion with the share code
            newSugestie.CodPartajare = shareCode;
            _sugestieRepo.Update(newSugestie);

            await DisplayAlert("✅ Succes", "Sugestia a fost salvata cu succes! 🎉", "Grozav!");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("❌ Eroare", $"Nu s-a putut salva sugestia:\n{ex.Message}", "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        var titluEntry = this.FindByName<Entry>("TitluEntry");
        var descriereEditor = this.FindByName<Editor>("DescriereEditor");
        var bugetEntry = this.FindByName<Entry>("BugetEntry");
        var picker = this.FindByName<Picker>("DestinatiePicker");
        
        // Check if form has data
        bool hasData = !string.IsNullOrWhiteSpace(titluEntry.Text) ||
                      !string.IsNullOrWhiteSpace(descriereEditor.Text) ||
                      !string.IsNullOrWhiteSpace(bugetEntry.Text) ||
                      picker.SelectedItem != null;

        if (hasData)
        {
            bool confirm = await DisplayAlert("⚠️ Confirmare", 
                "Esti sigur ca vrei sa anulezi? Toate datele introduse vor fi pierdute.", 
                "Da, anuleaza", "Nu, continua");
            
            if (!confirm)
                return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private bool ValidateForm()
    {
        // Reset previous error states
        ClearValidationErrors();

        bool isValid = true;
        var errors = new List<string>();

        var titluEntry = this.FindByName<Entry>("TitluEntry");
        var picker = this.FindByName<Picker>("DestinatiePicker");
        var bugetEntry = this.FindByName<Entry>("BugetEntry");
        var descriereEditor = this.FindByName<Editor>("DescriereEditor");

        // Validate title
        if (string.IsNullOrWhiteSpace(titluEntry.Text))
        {
            errors.Add("📝 Titlul este obligatoriu");
            HighlightError(titluEntry);
            isValid = false;
        }
        else if (titluEntry.Text.Trim().Length < 3)
        {
            errors.Add("📝 Titlul trebuie sa aiba cel putin 3 caractere");
            HighlightError(titluEntry);
            isValid = false;
        }

        // Validate destination
        if (picker.SelectedItem == null)
        {
            errors.Add("🌍 Selectarea unei destinatii este obligatorie");
            isValid = false;
        }

        // Validate budget
        if (string.IsNullOrWhiteSpace(bugetEntry.Text))
        {
            errors.Add("💰 Bugetul este obligatoriu");
            HighlightError(bugetEntry);
            isValid = false;
        }
        else if (!decimal.TryParse(bugetEntry.Text.Trim(), out decimal budget) || budget <= 0)
        {
            errors.Add("💰 Bugetul trebuie sa fie un numar pozitiv valid");
            HighlightError(bugetEntry);
            isValid = false;
        }

        // Validate description - only check if it's not empty
        if (string.IsNullOrWhiteSpace(descriereEditor.Text))
        {
            errors.Add("📄 Descrierea este obligatorie");
            HighlightError(descriereEditor);
            isValid = false;
        }

        if (!isValid)
        {
            var errorMessage = string.Join("\n", errors);
            DisplayAlert("⚠️ Erori de validare", errorMessage, "OK");
        }

        return isValid;
    }

    private void HighlightError(View control)
    {
        if (control.Parent is Frame parentFrame)
        {
            parentFrame.BorderColor = Colors.Red;
            
            // Reset border color after a delay
            Device.StartTimer(TimeSpan.FromSeconds(3), () =>
            {
                parentFrame.BorderColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                    Color.FromArgb("#404040") : Color.FromArgb("#E8E8E8");
                return false;
            });
        }
    }

    private void ClearValidationErrors()
    {
        try
        {
            var titluEntry = this.FindByName<Entry>("TitluEntry");
            var bugetEntry = this.FindByName<Entry>("BugetEntry");
            var descriereEditor = this.FindByName<Editor>("DescriereEditor");

            ResetFrameBorderColor(titluEntry);
            ResetFrameBorderColor(bugetEntry);
            ResetFrameBorderColor(descriereEditor);
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    private void ResetFrameBorderColor(View control)
    {
        if (control?.Parent is Frame parentFrame)
        {
            parentFrame.BorderColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                Color.FromArgb("#404040") : Color.FromArgb("#E8E8E8");
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        LoadingOverlay.IsVisible = isLoading;
        SaveButton.IsEnabled = !isLoading;
        CancelButton.IsEnabled = !isLoading;
        
        var titluEntry = this.FindByName<Entry>("TitluEntry");
        var picker = this.FindByName<Picker>("DestinatiePicker");
        var bugetEntry = this.FindByName<Entry>("BugetEntry");
        var descriereEditor = this.FindByName<Editor>("DescriereEditor");

        titluEntry.IsEnabled = !isLoading;
        picker.IsEnabled = !isLoading;
        bugetEntry.IsEnabled = !isLoading;
        descriereEditor.IsEnabled = !isLoading;
    }

    private string GenerateShareCode(int id, string title)
    {
        try
        {
            // Create a string combining the primary key and title
            var combinedString = $"{id}_{title.Trim()}";
            
            // Encrypt the combined string using EncryptionUtils
            var encryptedCode = EncryptionUtils.Encrypt(combinedString);
            
            // Return a truncated version for easier sharing (first 12 characters)
            // and replace any characters that might cause issues in URLs
            return encryptedCode.Replace("/", "-").Replace("+", "_").Substring(0, Math.Min(12, encryptedCode.Length));
        }
        catch (Exception ex)
        {
            // Fallback to a simpler approach if encryption fails
            System.Diagnostics.Debug.WriteLine($"Share code generation failed: {ex.Message}");
            return $"S{id}_{title.Substring(0, Math.Min(3, title.Length)).ToUpper()}";
        }
    }

    private void OnBack(object sender, EventArgs e)
    {
        OnCancelClicked(sender, e);
    }
}