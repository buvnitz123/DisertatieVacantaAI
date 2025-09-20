using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Pages;

public class SuggestionImageItem
{
    public string ImagineUrl { get; set; }
}

public class SuggestionFacilityDisplayItem
{
    public int Id_Facilitate { get; set; }
    public string Denumire { get; set; }
    public string Descriere { get; set; }
    public bool HasDescription => !string.IsNullOrWhiteSpace(Descriere);
}

public class SuggestionCategoryDisplayItem
{
    public int Id_CategorieVacanta { get; set; }
    public string Denumire { get; set; }
    public string ImagineUrl { get; set; }
}

[QueryProperty(nameof(SuggestionId), "suggestionId")]
public partial class SuggestionDetailsPage : ContentPage
{
    private readonly SugestieRepository _sugestieRepo = new SugestieRepository();
    private readonly ImaginiDestinatieRepository _imaginiDestRepo = new ImaginiDestinatieRepository();
    private readonly CategorieVacanta_DestinatieRepository _catDestRepo = new CategorieVacanta_DestinatieRepository();
    private readonly CategorieVacantaRepository _categorieRepo = new CategorieVacantaRepository();
    private readonly DestinatieFacilitateRepository _destFacilitateRepo = new DestinatieFacilitateRepository();
    private readonly FacilitateRepository _facilitateRepo = new FacilitateRepository();
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    
    private int _suggestionId;
    private Sugestie _currentSuggestion;
    private ObservableCollection<SuggestionImageItem> _destinationImages = new ObservableCollection<SuggestionImageItem>();
    private ObservableCollection<SuggestionCategoryDisplayItem> _categories = new ObservableCollection<SuggestionCategoryDisplayItem>();
    private ObservableCollection<SuggestionFacilityDisplayItem> _facilities = new ObservableCollection<SuggestionFacilityDisplayItem>();
    
    public string SuggestionId 
    { 
        set 
        { 
            if (int.TryParse(value, out int id))
            {
                _suggestionId = id;
            }
        } 
    }

    public SuggestionDetailsPage()
    {
        InitializeComponent();
        ImagesCarousel.ItemsSource = _destinationImages;
        CategoriesCollectionView.ItemsSource = _categories;
        FacilitiesCollectionView.ItemsSource = _facilities;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSuggestionDetailsAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Cancel any ongoing operations
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        
        Debug.WriteLine($"[SuggestionDetailsPage] OnDisappearing - operations cancelled");
    }

    private async Task LoadSuggestionDetailsAsync()
    {
        try
        {
            SetLoadingState(true);
            
            Debug.WriteLine($"[SuggestionDetailsPage] Loading suggestion details for ID: {_suggestionId}");
            
            // Load suggestion with destination details
            _currentSuggestion = await Task.Run(() => _sugestieRepo.GetByIdWithDestination(_suggestionId), _cancellationTokenSource.Token).ConfigureAwait(false);
            
            if (_currentSuggestion == null)
            {
                Debug.WriteLine($"[SuggestionDetailsPage] Suggestion with ID {_suggestionId} not found in database");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Eroare", "Sugestia nu a fost gasita.", "OK");
                    await Shell.Current.GoToAsync("..");
                });
                return;
            }

            Debug.WriteLine($"[SuggestionDetailsPage] Found suggestion: {_currentSuggestion.Titlu}");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                UpdateUI();
            });
            
            // Load destination images asynchronously
            _ = LoadDestinationImagesAsync();
            
            // Load destination categories asynchronously
            _ = LoadDestinationCategoriesAsync();
            
            // Load destination facilities asynchronously
            _ = LoadDestinationFacilitiesAsync();
            
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Loading cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading suggestion details: {ex.Message}");
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Eroare", "Nu s-au putut incarca detaliile sugestiei.", "OK");
                });
            }
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                SetLoadingState(false);
            });
        }
    }

    private void UpdateUI()
    {
        if (_currentSuggestion == null) return;

        Debug.WriteLine($"[SuggestionDetailsPage] UpdateUI called for suggestion: {_currentSuggestion.Titlu}");
        Debug.WriteLine($"[SuggestionDetailsPage] Destination ID: {_currentSuggestion.Id_Destinatie}");
        Debug.WriteLine($"[SuggestionDetailsPage] Destination object: {_currentSuggestion.Destinatie?.Denumire ?? "NULL"}");

        // Update basic info
        SuggestionTitleLabel.Text = _currentSuggestion.Titlu;
        DestinationNameLabel.Text = _currentSuggestion.Destinatie?.Denumire ?? "Destinatie necunoscuta";
        BudgetLabel.Text = $"{_currentSuggestion.Buget_Estimat:N0} €";
        DescriptionLabel.Text = _currentSuggestion.Descriere;
        CreationDateLabel.Text = _currentSuggestion.Data_Inregistrare.ToString("dd/MM/yyyy");
        
        // Update visibility status with appropriate styling
        UpdateVisibilityUI();
        
        // Update generation type
        GenerationTypeLabel.Text = _currentSuggestion.EsteGenerataDeAI == 1 ? "🤖 AI" : "👤 Manual";
        
        // Update status indicators
        bool isAI = _currentSuggestion.EsteGenerataDeAI == 1;
        bool isNew = (DateTime.Now - _currentSuggestion.Data_Inregistrare).TotalDays <= 1;
        
        AIIndicatorFrame.IsVisible = isAI;
        ManualIndicatorFrame.IsVisible = !isAI;
        NewIndicatorFrame.IsVisible = isNew;
        
        // Update sharing section
        UpdateSharingSection();
        
        // Update page title
        Title = _currentSuggestion.Titlu;
        
        Debug.WriteLine($"[SuggestionDetailsPage] Updated UI with suggestion info");
    }

    private void UpdateVisibilityUI()
    {
        if (_currentSuggestion == null) return;

        bool isPublic = _currentSuggestion.EstePublic == 1;
        StatusLabel.Text = isPublic ? "Publică" : "Privată";
        
        // Update frame colors based on status
        if (isPublic)
        {
            VisibilityStatusFrame.BackgroundColor = Color.FromArgb("#4CAF50"); // Green for public
            StatusLabel.TextColor = Colors.White;
            var hintLabel = ((HorizontalStackLayout)StatusLabel.Parent).Children.OfType<Label>().FirstOrDefault(l => l.Text == "👆 Apasă");
            if (hintLabel != null)
                hintLabel.TextColor = Colors.White;
        }
        else
        {
            VisibilityStatusFrame.BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark 
                ? Color.FromArgb("#404040") 
                : Color.FromArgb("#CCCCCC");
            StatusLabel.TextColor = Application.Current.RequestedTheme == AppTheme.Dark 
                ? Colors.White 
                : Color.FromArgb("#333333");
            var hintLabel = ((HorizontalStackLayout)StatusLabel.Parent).Children.OfType<Label>().FirstOrDefault(l => l.Text == "👆 Apasă");
            if (hintLabel != null)
                hintLabel.TextColor = Application.Current.RequestedTheme == AppTheme.Dark 
                    ? Colors.White 
                    : Color.FromArgb("#333333");
        }
    }

    private void UpdateSharingSection()
    {
        if (_currentSuggestion?.EstePublic == 1 && !string.IsNullOrEmpty(_currentSuggestion.CodPartajare))
        {
            SharingCodeLabel.Text = _currentSuggestion.CodPartajare;
            SharingSection.IsVisible = true;
        }
        else
        {
            SharingSection.IsVisible = false;
        }
    }

    private async Task LoadDestinationImagesAsync()
    {
        try
        {
            if (_currentSuggestion?.Id_Destinatie == null || _currentSuggestion.Id_Destinatie <= 0)
            {
                Debug.WriteLine($"[SuggestionDetailsPage] Invalid destination ID: {_currentSuggestion?.Id_Destinatie}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ImagesCarousel.IsVisible = false;
                    IndicatorView.IsVisible = false;
                    NoImagesLayout.IsVisible = true;
                    ImagesLoadingIndicator.IsVisible = false;
                    ImagesLoadingIndicator.IsRunning = false;
                });
                return;
            }
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ImagesLoadingIndicator.IsVisible = true;
                ImagesLoadingIndicator.IsRunning = true;
                NoImagesLayout.IsVisible = false;
                ImagesCarousel.IsVisible = false;
                IndicatorView.IsVisible = false;
            });
            
            Debug.WriteLine($"[SuggestionDetailsPage] Loading images for destination ID: {_currentSuggestion.Id_Destinatie}");
            
            var images = await Task.Run(() => GetDestinationImagesSync(_currentSuggestion.Id_Destinatie), _cancellationTokenSource.Token);
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _destinationImages.Clear();
                
                Debug.WriteLine($"[SuggestionDetailsPage] Retrieved {images?.Count ?? 0} images from database");
                
                if (images?.Any() == true)
                {
                    foreach (var image in images)
                    {
                        Debug.WriteLine($"[SuggestionDetailsPage] Processing image: {image.ImagineUrl}");
                        
                        // Prepare the image URL before adding to collection
                        var preparedUrl = PrepareImageUrl(image.ImagineUrl);
                        Debug.WriteLine($"[SuggestionDetailsPage] Prepared URL: {preparedUrl}");
                        
                        var imageItem = new SuggestionImageItem
                        {
                            ImagineUrl = preparedUrl
                        };
                        
                        _destinationImages.Add(imageItem);
                    }
                    
                    ImagesCarousel.IsVisible = true;
                    IndicatorView.IsVisible = _destinationImages.Count > 1; // Only show indicators if multiple images
                    NoImagesLayout.IsVisible = false;
                    
                    Debug.WriteLine($"[SuggestionDetailsPage] Successfully loaded {_destinationImages.Count} images for destination carousel");
                }
                else
                {
                    // Show no images message
                    ImagesCarousel.IsVisible = false;
                    IndicatorView.IsVisible = false;
                    NoImagesLayout.IsVisible = true;
                    
                    Debug.WriteLine($"[SuggestionDetailsPage] No images found for destination {_currentSuggestion.Id_Destinatie}, showing no images layout");
                }
                
                ImagesLoadingIndicator.IsVisible = false;
                ImagesLoadingIndicator.IsRunning = false;
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Image loading cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Error loading destination images: {ex.Message}");
            Debug.WriteLine($"[SuggestionDetailsPage] Stack trace: {ex.StackTrace}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ImagesCarousel.IsVisible = false;
                IndicatorView.IsVisible = false;
                NoImagesLayout.IsVisible = true;
                ImagesLoadingIndicator.IsVisible = false;
                ImagesLoadingIndicator.IsRunning = false;
            });
        }
    }

    private List<ImaginiDestinatie> GetDestinationImagesSync(int destinationId)
    {
        try
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Querying database for images with destination ID: {destinationId}");
            
            var images = _imaginiDestRepo.GetByDestinationId(destinationId);
            var imagesList = images?.ToList() ?? new List<ImaginiDestinatie>();
            
            Debug.WriteLine($"[SuggestionDetailsPage] Database query returned {imagesList.Count} images for destination {destinationId}");
            
            foreach (var img in imagesList)
            {
                Debug.WriteLine($"[SuggestionDetailsPage] Found image: ID={img.Id_ImaginiDestinatie}, URL={img.ImagineUrl}");
            }
            
            return imagesList;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Error in GetDestinationImagesSync: {ex.Message}");
            Debug.WriteLine($"[SuggestionDetailsPage] Stack trace: {ex.StackTrace}");
            return new List<ImaginiDestinatie>();
        }
    }

    private string PrepareImageUrl(string imageUrl)
    {
        Debug.WriteLine($"[SuggestionDetailsPage] PrepareImageUrl called with: '{imageUrl}'");
        
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Image URL is null or empty, returning placeholder");
            return "https://via.placeholder.com/400x250/E0E0E0/999999?text=No+Image";
        }

        // If it's already a full URL, return as-is
        if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Image URL is already a full URL: {imageUrl}");
            return imageUrl;
        }

        // If it's a blob storage filename, construct the full URL
        if (imageUrl.Contains(".jpg") || imageUrl.Contains(".png") || imageUrl.Contains(".jpeg") || imageUrl.Contains(".webp"))
        {
            var fullUrl = $"https://vacantaai.blob.core.windows.net/vacantaai/{imageUrl}";
            Debug.WriteLine($"[SuggestionDetailsPage] Constructed blob storage URL: {fullUrl}");
            return fullUrl;
        }

        // Default case - assume it's a valid URL or filename
        Debug.WriteLine($"[SuggestionDetailsPage] Using image URL as-is: {imageUrl}");
        return imageUrl;
    }

    private async Task LoadDestinationFacilitiesAsync()
    {
        try
        {
            if (_currentSuggestion?.Id_Destinatie == null || _currentSuggestion.Id_Destinatie <= 0)
            {
                Debug.WriteLine($"[SuggestionDetailsPage] Invalid destination ID for facilities: {_currentSuggestion?.Id_Destinatie}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    FacilitiesSection.IsVisible = false;
                });
                return;
            }
            
            Debug.WriteLine($"[SuggestionDetailsPage] Loading facilities for destination ID: {_currentSuggestion.Id_Destinatie}");
            
            var facilityRelations = await Task.Run(() => _destFacilitateRepo.GetByDestinationId(_currentSuggestion.Id_Destinatie), _cancellationTokenSource.Token);
            
            if (!facilityRelations?.Any() == true)
            {
                Debug.WriteLine($"[SuggestionDetailsPage] No facility relations found for destination {_currentSuggestion.Id_Destinatie}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    FacilitiesSection.IsVisible = false;
                });
                return;
            }
            
            var facilityIds = facilityRelations.Select(fr => fr.Id_Facilitate).Take(5).ToList();
            Debug.WriteLine($"[SuggestionDetailsPage] Found {facilityIds.Count} facilities for destination");
            
            var facilities = await Task.Run(() => 
            {
                return facilityIds.Select(id => _facilitateRepo.GetById(id))
                                 .Where(f => f != null)
                                 .ToList();
            }, _cancellationTokenSource.Token);
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _facilities.Clear();
                
                if (facilities?.Any() == true)
                {
                    foreach (var facility in facilities)
                    {
                        var facilityItem = new SuggestionFacilityDisplayItem
                        {
                            Id_Facilitate = facility.Id_Facilitate,
                            Denumire = facility.Denumire,
                            Descriere = facility.Descriere
                        };
                        
                        _facilities.Add(facilityItem);
                        Debug.WriteLine($"[SuggestionDetailsPage] Added facility: {facility.Denumire}");
                    }
                    
                    FacilitiesSection.IsVisible = true;
                    Debug.WriteLine($"[SuggestionDetailsPage] Successfully loaded {_facilities.Count} facilities");
                }
                else
                {
                    FacilitiesSection.IsVisible = false;
                    Debug.WriteLine($"[SuggestionDetailsPage] No valid facilities found, hiding section");
                }
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Facilities loading cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Error loading destination facilities: {ex.Message}");
            Debug.WriteLine($"[SuggestionDetailsPage] Stack trace: {ex.StackTrace}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FacilitiesSection.IsVisible = false;
            });
        }
    }

    private async Task LoadDestinationCategoriesAsync()
    {
        try
        {
            if (_currentSuggestion?.Id_Destinatie == null || _currentSuggestion.Id_Destinatie <= 0)
            {
                Debug.WriteLine($"[SuggestionDetailsPage] Invalid destination ID for categories: {_currentSuggestion?.Id_Destinatie}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CategoriesSection.IsVisible = false;
                });
                return;
            }
            
            Debug.WriteLine($"[SuggestionDetailsPage] Loading categories for destination ID: {_currentSuggestion.Id_Destinatie}");
            
            var categoryRelations = await Task.Run(() => _catDestRepo.GetByDestinationId(_currentSuggestion.Id_Destinatie), _cancellationTokenSource.Token);
            
            if (!categoryRelations?.Any() == true)
            {
                Debug.WriteLine($"[SuggestionDetailsPage] No category relations found for destination {_currentSuggestion.Id_Destinatie}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CategoriesSection.IsVisible = false;
                });
                return;
            }
            
            var categoryIds = categoryRelations.Select(cr => cr.Id_CategorieVacanta).Take(5).ToList();
            Debug.WriteLine($"[SuggestionDetailsPage] Found {categoryIds.Count} categories for destination");
            
            var categories = await Task.Run(() => 
            {
                return categoryIds.Select(id => _categorieRepo.GetById(id))
                                 .Where(c => c != null)
                                 .ToList();
            }, _cancellationTokenSource.Token);
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _categories.Clear();
                
                if (categories?.Any() == true)
                {
                    foreach (var category in categories)
                    {
                        var categoryItem = new SuggestionCategoryDisplayItem
                        {
                            Id_CategorieVacanta = category.Id_CategorieVacanta,
                            Denumire = category.Denumire,
                            ImagineUrl = PrepareImageUrl(category.ImagineUrl)
                        };
                        
                        _categories.Add(categoryItem);
                        Debug.WriteLine($"[SuggestionDetailsPage] Added category: {category.Denumire}");
                    }
                    
                    CategoriesSection.IsVisible = true;
                    Debug.WriteLine($"[SuggestionDetailsPage] Successfully loaded {_categories.Count} categories");
                }
                else
                {
                    CategoriesSection.IsVisible = false;
                    Debug.WriteLine($"[SuggestionDetailsPage] No valid categories found, hiding section");
                }
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Categories loading cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Error loading destination categories: {ex.Message}");
            Debug.WriteLine($"[SuggestionDetailsPage] Stack trace: {ex.StackTrace}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CategoriesSection.IsVisible = false;
            });
        }
    }

    private async void OnToggleVisibility(object sender, TappedEventArgs e)
    {
        try
        {
            if (_currentSuggestion == null) return;

            // Visual feedback
            await VisibilityStatusFrame.ScaleTo(0.95, 100);
            await VisibilityStatusFrame.ScaleTo(1.0, 100);

            bool isCurrentlyPublic = _currentSuggestion.EstePublic == 1;
            string action = isCurrentlyPublic ? "privată" : "publică";
            string message = isCurrentlyPublic 
                ? "Vrei să faci această sugestie privată? Va fi vizibilă doar pentru tine."
                : "Vrei să faci această sugestie publică? Va fi vizibilă pentru toți utilizatorii și va primi un cod de partajare.";

            bool confirm = await DisplayAlert(
                "Schimbă Vizibilitate", 
                message, 
                $"Da, fă-o {action}", 
                "Anulează");

            if (confirm)
            {
                SetLoadingState(true);

                // Toggle visibility
                _currentSuggestion.EstePublic = isCurrentlyPublic ? 0 : 1;

                // Generate sharing code if making public
                if (_currentSuggestion.EstePublic == 1 && string.IsNullOrEmpty(_currentSuggestion.CodPartajare))
                {
                    _currentSuggestion.CodPartajare = GenerateShareCode(_currentSuggestion.Id_Sugestie, _currentSuggestion.Titlu);
                }

                // Update in database
                await Task.Run(() => _sugestieRepo.Update(_currentSuggestion), _cancellationTokenSource.Token);

                // Update UI
                UpdateVisibilityUI();
                UpdateSharingSection();

                // Success message without showing the code
                string successMessage = _currentSuggestion.EstePublic == 1 
                    ? "Sugestia este acum publică și poate fi partajată!"
                    : "Sugestia este acum privată.";

                await DisplayAlert("Succes", successMessage, "OK");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Toggle visibility cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error toggling visibility: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut schimba vizibilitatea sugestiei. Te rog încearcă din nou.", "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
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
            Debug.WriteLine($"Share code generation failed: {ex.Message}");
            return $"S{id}_{title.Substring(0, Math.Min(3, title.Length)).ToUpper()}";
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        LoadingOverlay.IsVisible = isLoading;
    }

    private async void OnCopySharingCode(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentSuggestion?.CodPartajare))
            {
                await DisplayAlert("Informatie", "Nu existe cod de partajare disponibil.", "OK");
                return;
            }

            await Clipboard.SetTextAsync(_currentSuggestion.CodPartajare);
            
            // Visual feedback
            var button = sender as Button;
            var originalText = button.Text;
            button.Text = "✅ Copiat!";
            await Task.Delay(2000);
            button.Text = originalText;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error copying sharing code: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut copia codul de partajare.", "OK");
        }
    }

    private async void OnEditSuggestion(object sender, EventArgs e)
    {
        try
        {
            // Navigate to edit suggestion page (you might need to create this)
            await DisplayAlert("Functionalitate", "Editarea sugestiilor va fi implementata in viitor.", "OK");
            
            // TODO: Implement navigation to edit page
            // await Shell.Current.GoToAsync($"{nameof(EditSuggestionPage)}?suggestionId={_suggestionId}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error navigating to edit suggestion: {ex.Message}");
        }
    }

    private async void OnDeleteSuggestion(object sender, EventArgs e)
    {
        try
        {
            bool confirm = await DisplayAlert(
                "Confirmare", 
                "Esti sigur ca vrei sa stergi aceasta sugestie? Actiunea nu poate fi anulata.", 
                "Da, Sterge", 
                "Anuleaza");

            if (confirm)
            {
                // Show loading
                SetLoadingState(true);
                
                // Delete from database
                await Task.Run(() => _sugestieRepo.Delete(_suggestionId), _cancellationTokenSource.Token);
                
                await DisplayAlert("Succes", "Sugestia a fost stearsa cu succes.", "OK");
                
                // Navigate back
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[SuggestionDetailsPage] Delete operation cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting suggestion: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut sterge sugestia. Te rog incearca din nou.", "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
    }
}