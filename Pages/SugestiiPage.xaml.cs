using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MauiAppDisertatieVacantaAI.Pages;

public class SugestieDisplayItem : INotifyPropertyChanged
{
    public int Id_Sugestie { get; set; }
    public string Titlu { get; set; }
    public string Descriere { get; set; }
    public decimal Buget_Estimat { get; set; }
    public DateTime Data_Inregistrare { get; set; }
    public int? EsteGenerataDeAI { get; set; }
    public int? EstePublic { get; set; }
    public string CodPartajare { get; set; }
    public Destinatie Destinatie { get; set; }

    private string _imageUrl = "https://via.placeholder.com/150x100/E0E0E0/999999?text=No+Image";
    public string ImageUrl
    {
        get => _imageUrl;
        set
        {
            // Handle both URLs and local filenames
            if (string.IsNullOrEmpty(value))
            {
                _imageUrl = "https://via.placeholder.com/150x100/E0E0E0/999999?text=No+Image";
            }
            else if (value.StartsWith("http"))
            {
                _imageUrl = value;
            }
            else if (value == "placeholder_image.png" || !value.Contains("."))
            {
                _imageUrl = "https://via.placeholder.com/150x100/E0E0E0/999999?text=No+Image";
            }
            else
            {
                // Assume it's a valid URL or file
                _imageUrl = value;
            }
            OnPropertyChanged();
        }
    }

    public bool IsAIGenerated => EsteGenerataDeAI == 1;
    public bool IsManualGenerated => EsteGenerataDeAI == 0;
    public bool IsNew => (DateTime.Now - Data_Inregistrare).TotalDays <= 1;

    // Always show AI or Manual indicator
    public bool ShowAIIndicator => EsteGenerataDeAI == 1;
    public bool ShowManualIndicator => EsteGenerataDeAI == 0;
    public bool ShowNewIndicator => (DateTime.Now - Data_Inregistrare).TotalDays <= 1;

    // Truncated title for display in list (max 15 characters)
    public string TitluTruncat
    {
        get
        {
            if (string.IsNullOrEmpty(Titlu))
                return string.Empty;

            if (Titlu.Length <= 15)
                return Titlu;

            return Titlu.Substring(0, 15) + "...";
        }
    }

    // Additional helper properties for better UX
    public string FormattedBudget => $"{Buget_Estimat:N0} lei";
    public string FormattedDate => Data_Inregistrare.ToString("dd/MM/yyyy");
    public string FormattedDateShort => Data_Inregistrare.ToString("dd/MM");
    public string StatusText => EstePublic == 1 ? "Publică" : "Privată";
    public string AIStatusText => IsAIGenerated ? "🤖 AI" : "👤 Manual";

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class SugestiiPage : ContentPage
{
    private readonly SugestieRepository _sugestieRepo = new SugestieRepository();
    private readonly UtilizatorRepository _utilizatorRepo = new UtilizatorRepository();
    private readonly ImaginiDestinatieRepository _imaginiDestRepo = new ImaginiDestinatieRepository();
    private int _userId;
    private ObservableCollection<SugestieDisplayItem> _items = new ObservableCollection<SugestieDisplayItem>();

    public SugestiiPage()
    {
        InitializeComponent();
        SugestiiCollection.BindingContext = _items;
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
            SetLoadingState(true);
            _items.Clear();

            var idStr = await UserSession.GetUserIdAsync();
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out _userId))
            {
                return; // not logged
            }

            var suggestions = _sugestieRepo.GetByUser(_userId);

            foreach (var s in suggestions)
            {
                var displayItem = new SugestieDisplayItem
                {
                    Id_Sugestie = s.Id_Sugestie,
                    Titlu = s.Titlu,
                    Descriere = s.Descriere,
                    Buget_Estimat = s.Buget_Estimat,
                    Data_Inregistrare = s.Data_Inregistrare,
                    EsteGenerataDeAI = s.EsteGenerataDeAI ?? 0, // Default to manual if null
                    EstePublic = s.EstePublic,
                    CodPartajare = s.CodPartajare,
                    Destinatie = s.Destinatie,
                    ImageUrl = null // Will trigger placeholder, then be updated async
                };

                _items.Add(displayItem);

                // Load image asynchronously and update the item
                _ = LoadImageForSuggestionAsync(displayItem, s.Id_Destinatie);

                // Debug info for indicators
                System.Diagnostics.Debug.WriteLine($"[SugestiiPage] Suggestion '{displayItem.Titlu}' - AI: {displayItem.ShowAIIndicator}, Manual: {displayItem.ShowManualIndicator}, New: {displayItem.ShowNewIndicator}, EsteGenerataDeAI: {displayItem.EsteGenerataDeAI}");
            }

            UpdateEmptyState();

            System.Diagnostics.Debug.WriteLine($"[SugestiiPage] Loaded {_items.Count} suggestions");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Nu s-au putut incarca planificarile: {ex.Message}", "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async Task LoadImageForSuggestionAsync(SugestieDisplayItem displayItem, int destinationId)
    {
        try
        {
            // Run on background thread
            var imageUrl = await Task.Run(() => GetFirstDestinationImageSync(destinationId));

            // Update on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Validate and prepare the image URL
                    var finalUrl = PrepareImageUrl(imageUrl);
                    displayItem.ImageUrl = finalUrl;
                    System.Diagnostics.Debug.WriteLine($"[SugestiiPage] Updated image for suggestion '{displayItem.Titlu}': {finalUrl}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SugestiiPage] No image found for suggestion '{displayItem.Titlu}', using placeholder");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading image for suggestion: {ex.Message}");
        }
    }

    private string PrepareImageUrl(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return null;

        // If it's already a full URL, return as-is
        if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return imageUrl;

        // If it's a blob storage filename, construct the full URL
        if (imageUrl.Contains(".jpg") || imageUrl.Contains(".png") || imageUrl.Contains(".jpeg"))
        {
            // Assuming Azure Blob Storage pattern (adjust as needed)
            return $"https://vacantaai.blob.core.windows.net/vacantaai/{imageUrl}";
        }

        // Default case
        return imageUrl;
    }

    private string GetFirstDestinationImageSync(int destinationId)
    {
        try
        {
            var images = _imaginiDestRepo.GetByDestinationId(destinationId);
            var imagesList = images?.ToList();
            var firstImage = imagesList?.FirstOrDefault()?.ImagineUrl;

            System.Diagnostics.Debug.WriteLine($"[SugestiiPage] Found {imagesList?.Count ?? 0} images for destination {destinationId}");
            if (firstImage != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SugestiiPage] First image URL: {firstImage}");
            }

            return firstImage;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting destination image: {ex.Message}");
            return null;
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;

        // Hide other views when loading
        if (isLoading)
        {
            SugestiiCollection.IsVisible = false;
            EmptyStateView.IsVisible = false;
        }
        else
        {
            UpdateEmptyState();
        }
    }

    private void UpdateEmptyState()
    {
        bool hasItems = _items.Any();
        SugestiiCollection.IsVisible = hasItems;
        EmptyStateView.IsVisible = !hasItems;
    }

    private async void OnSugestieSelected(object sender, TappedEventArgs e)
    {
        try
        {
            var frame = sender as Frame;
            var sugestie = frame?.BindingContext as SugestieDisplayItem;

            if (sugestie != null)
            {
                // Visual feedback
                await frame.ScaleTo(0.95, 100);
                await frame.ScaleTo(1.0, 100);

                // Navigate to suggestion details page
                await Shell.Current.GoToAsync($"{nameof(SuggestionDetailsPage)}?suggestionId={sugestie.Id_Sugestie}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Nu s-au putut afișa detaliile planificării: {ex.Message}", "OK");
        }
    }

    private async void OnAddSugestie(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NewSugestiePage));
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        try
        {
            string shareCode = await DisplayPromptAsync("Importă Plan", "Introdu codul de partajare primit:", "Importă", "Anulează", placeholder: "Ex: VAC-12345");

            if (string.IsNullOrWhiteSpace(shareCode))
                return;

            string userIdStr = await UserSession.GetUserIdAsync();
            if (!int.TryParse(userIdStr, out int currentUserId) || currentUserId <= 0)
            {
                await DisplayAlert("Eroare", "Trebuie să fii autentificat pentru a importa planuri.", "OK");
                return;
            }

            var importedSuggestion = await Task.Run(() => _sugestieRepo.GetByShareCode(shareCode));

            if (importedSuggestion == null)
            {
                await DisplayAlert("Eroare", "Cod invalid, planul nu există sau nu este public.", "OK");
                return;
            }

            if (importedSuggestion.Id_Utilizator == currentUserId)
            {
                await DisplayAlert("Info", "Acest plan îți aparține deja.", "OK");
                return;
            }

            // Create new unshared cloned suggestion
            string newTitle = $"{importedSuggestion.Titlu} (Importat)";
            if (newTitle.Length > 50)
            {
                newTitle = newTitle.Substring(0, 47) + "...";
            }

            var newSuggestion = new Sugestie
            {
                Id_Utilizator = currentUserId,
                Id_Destinatie = importedSuggestion.Id_Destinatie,
                Titlu = newTitle,
                Descriere = importedSuggestion.Descriere,
                Buget_Estimat = importedSuggestion.Buget_Estimat,
                EsteGenerataDeAI = importedSuggestion.EsteGenerataDeAI,
                EstePublic = 0,
                CodPartajare = null,
                Data_Inregistrare = DateTime.Now
            };

            await Task.Run(() => _sugestieRepo.Insert(newSuggestion));

            MauiAppDisertatieVacantaAI.Classes.Library.Utils.ActivityLogger.Log(currentUserId, MauiAppDisertatieVacantaAI.Classes.Enums.TipActivitate.Creare, MauiAppDisertatieVacantaAI.Classes.Enums.TipEntitate.Sugestie, newSuggestion.Id_Sugestie);

            await DisplayAlert("Succes!", $"Planul '{importedSuggestion.Titlu}' a fost importat cu succes.", "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SugestiiPage] Eroare la importul sugestiei: {ex.Message}");
            await DisplayAlert("Eroare", "A apărut o problemă la importare.", "OK");
        }
    }

    private async void OnMenuTapped(object sender, EventArgs e)
    {
        try
        {
            var label = sender as Label;
            var sugestie = label?.BindingContext as SugestieDisplayItem;

            if (sugestie == null) return;

            // Visual feedback
            await label.ScaleTo(1.2, 100);
            await label.ScaleTo(1.0, 100);

            // Show action sheet with options (no icons)
            // DisplayActionSheet(title, cancel, destroy, otherButtons...)
            string action = await DisplayActionSheet(
        $"Opțiuni pentru \"{sugestie.Titlu}\"",
     "Anulează",
                 "Șterge",
            "Vezi detalii"
             );

            switch (action)
            {
                case "Șterge":
                    await DeleteSuggestionAsync(sugestie);
                    break;
                case "Vezi detalii":
                    await Shell.Current.GoToAsync($"{nameof(SuggestionDetailsPage)}?suggestionId={sugestie.Id_Sugestie}");
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in menu: {ex.Message}");
            await DisplayAlert("Eroare", "A apărut o eroare. Te rog încearcă din nou.", "OK");
        }
    }

    private async Task DeleteSuggestionAsync(SugestieDisplayItem sugestie)
    {
        try
        {
            bool confirm = await DisplayAlert(
                "Confirmare Ștergere",
                $"Ești sigur că vrei să ștergi planificarea \"{sugestie.Titlu}\"?\n\nAceastă acțiune nu poate fi anulată.",
                "Da, Șterge",
                "Anulează");

            if (!confirm) return;

            SetLoadingState(true);

            // Delete from database
            await Task.Run(() => _sugestieRepo.Delete(sugestie.Id_Sugestie));

            // Remove from collection
            _items.Remove(sugestie);

            // Update UI
            UpdateEmptyState();

            await DisplayAlert("Succes", "Planificarea a fost ștearsă cu succes! 🗑️", "OK");

            System.Diagnostics.Debug.WriteLine($"[SugestiiPage] Deleted suggestion: {sugestie.Titlu} (ID: {sugestie.Id_Sugestie})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting suggestion: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut șterge planificarea. Te rog încearcă din nou.", "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async void OnGalleryImageTapped(object sender, EventArgs e)
    {
        var imageUrl = (e as TappedEventArgs)?.Parameter as string;
        if (string.IsNullOrEmpty(imageUrl)) return;

        var overlay = new Grid { BackgroundColor = Color.FromArgb("#CC000000"), ZIndex = 999 };
        var image = new Image
        {
            Source = new UriImageSource { Uri = new Uri(imageUrl), CachingEnabled = true },
            Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
            MaximumWidthRequest = 350, MaximumHeightRequest = 500
        };
        var close = new Label { Text = "✕", FontSize = 28, TextColor = Colors.White, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 50, 20, 0) };
        overlay.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => { await overlay.FadeTo(0, 200); if (Content is Grid g) g.Children.Remove(overlay); }) });
        overlay.Children.Add(image);
        overlay.Children.Add(close);
        overlay.Opacity = 0;
        if (Content is Grid mainGrid) { Grid.SetRowSpan(overlay, Math.Max(mainGrid.RowDefinitions.Count, 1)); Grid.SetColumnSpan(overlay, Math.Max(mainGrid.ColumnDefinitions.Count, 1)); mainGrid.Children.Add(overlay); }
        await overlay.FadeTo(1, 200);
    }
}