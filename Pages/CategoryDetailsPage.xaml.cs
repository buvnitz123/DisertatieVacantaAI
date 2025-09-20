using System.Collections.ObjectModel;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public class CategoryDestinationDisplayItem
{
    public int Id_Destinatie { get; set; }
    public string Denumire { get; set; }
    public string Location { get; set; }
    public string ImagineUrl { get; set; }
    public decimal PretAdult { get; set; }
    public decimal PretMinor { get; set; }
    public string FormattedPrice => $"{PretAdult:N0} €";
}

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class CategoryDetailsPage : ContentPage
{
    private readonly ObservableCollection<CategoryDestinationDisplayItem> _destinations = new();
    
    // Repositories
    private readonly CategorieVacantaRepository _categorieRepo = new();
    private readonly CategorieVacanta_DestinatieRepository _catDestRepo = new();
    private readonly DestinatieRepository _destinatieRepo = new();
    private readonly ImaginiDestinatieRepository _imaginiDestRepo = new();
    private readonly FavoriteRepository _favoriteRepo = new();
    
    // Cancellation support for cleanup
    private CancellationTokenSource _cancellationTokenSource = new();
    
    // Pagination
    private int _currentPage = 0;
    private const int _pageSize = 10;
    private bool _hasMoreResults = false;
    private bool _isLoading = false;
    
    private int _categoryId;
    private CategorieVacanta _currentCategory;
    private int _currentUserId;
    private bool _isFavorite;

    // Basic caching for improved performance
    private static readonly Dictionary<int, (CategorieVacanta Category, DateTime CachedAt)> _categoryCache = new();
    private static readonly Dictionary<string, string> _imageUrlCache = new();
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public int CategoryId
    {
        get => _categoryId;
        set
        {
            Debug.WriteLine($"[CategoryDetailsPage] CategoryId setter called with value: {value}");
            _categoryId = value;
            OnPropertyChanged();
        }
    }

    public CategoryDetailsPage()
    {
        InitializeComponent();
        
        // Set up data sources
        DestinationsCollectionView.ItemsSource = _destinations;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        Debug.WriteLine($"[CategoryDetailsPage] OnAppearing called with categoryId: {_categoryId}");
        
        if (_categoryId > 0)
        {
            // Check if we already have data for this category to avoid reloading
            if (_currentCategory?.Id_CategorieVacanta != _categoryId || !_destinations.Any())
            {
                await LoadCategoryDetailsAsync();
            }
            else
            {
                Debug.WriteLine($"[CategoryDetailsPage] Category data already loaded, skipping reload");
            }
        }
        else
        {
            Debug.WriteLine($"[CategoryDetailsPage] Invalid category ID: {_categoryId}");
            await DisplayAlert("Eroare", "ID categorie invalid.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Cancel any ongoing operations
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Cleanup expired cache when leaving the page
        ClearExpiredCache();
        
        Debug.WriteLine($"[CategoryDetailsPage] OnDisappearing - operations cancelled and cache cleaned");
    }

    private async Task LoadCategoryDetailsAsync()
    {
        try
        {
            Debug.WriteLine($"[CategoryDetailsPage] Starting to load details for category ID: {_categoryId}");
            LoadingOverlay.IsVisible = true;
            
            // Reset pagination and clear existing data
            _currentPage = 0;
            _hasMoreResults = false;
            _destinations.Clear();
            
            // Add a short delay to ensure UI has updated
            await Task.Delay(100, _cancellationTokenSource.Token);
            
            // Load category basic info
            await LoadCategoryInfoAsync();
            
            // Load destinations for this category
            await LoadDestinationsAsync();
            
            // Load favorite status
            await LoadFavoriteStatusAsync();
            
            Debug.WriteLine($"[CategoryDetailsPage] Successfully loaded all category details");
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[CategoryDetailsPage] Loading cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading category details: {ex.Message}");
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await DisplayAlert("Eroare", "Nu s-au putut încărca detaliile categoriei.", "OK");
            }
        }
        finally
        {
            Debug.WriteLine($"[CategoryDetailsPage] Setting LoadingOverlay to hidden");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LoadingOverlay.IsVisible = false;
                Debug.WriteLine($"[CategoryDetailsPage] LoadingOverlay.IsVisible = {LoadingOverlay.IsVisible}");
            });
        }
    }

    private async Task LoadCategoryInfoAsync()
    {
        try
        {
            Debug.WriteLine($"[CategoryDetailsPage] Loading category info for ID: {_categoryId}");
            
            // Check cache first
            if (_categoryCache.TryGetValue(_categoryId, out var cachedData) && 
                DateTime.Now - cachedData.CachedAt < _cacheExpiration)
            {
                _currentCategory = cachedData.Category;
                Debug.WriteLine($"[CategoryDetailsPage] Using cached category: {_currentCategory.Denumire}");
            }
            else
            {
                _currentCategory = await Task.Run(() => _categorieRepo.GetById(_categoryId), _cancellationTokenSource.Token).ConfigureAwait(false);
                
                if (_currentCategory != null)
                {
                    // Cache the result
                    _categoryCache[_categoryId] = (_currentCategory, DateTime.Now);
                    Debug.WriteLine($"[CategoryDetailsPage] Cached category: {_currentCategory.Denumire}");
                }
            }
            
            if (_currentCategory == null)
            {
                Debug.WriteLine($"[CategoryDetailsPage] Category with ID {_categoryId} not found in database");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Eroare", "Categoria nu a fost găsită.", "OK");
                    await Shell.Current.GoToAsync("..");
                });
                return;
            }

            Debug.WriteLine($"[CategoryDetailsPage] Found category: {_currentCategory.Denumire}");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CategoryTitleLabel.Text = _currentCategory.Denumire;
                CategoryHeroImage.Source = PrepareImageUrl(_currentCategory.ImagineUrl);
                
                // Update page title
                Title = _currentCategory.Denumire;
                
                Debug.WriteLine($"[CategoryDetailsPage] Updated UI with category info");
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[CategoryDetailsPage] Category info loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading category info: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task LoadDestinationsAsync()
    {
        if (_isLoading) return;
        
        try
        {
            _isLoading = true;
            
            Debug.WriteLine($"[CategoryDetailsPage] Loading destinations for category ID: {_categoryId}, Page: {_currentPage}");
            
            // Get destinations for this category through the junction table
            var categoryDestinations = await Task.Run(() => 
                _catDestRepo.GetByCategoryId(_categoryId), _cancellationTokenSource.Token).ConfigureAwait(false);
            
            var destinationIds = categoryDestinations.Select(cd => cd.Id_Destinatie).ToList();
            Debug.WriteLine($"[CategoryDetailsPage] Found {destinationIds.Count} destination associations");
            
            if (!destinationIds.Any())
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    EmptyStateLayout.IsVisible = true;
                    LoadMoreButton.IsVisible = false;
                    Debug.WriteLine($"[CategoryDetailsPage] No destinations found, showing empty state");
                });
                return;
            }
            
            // Apply pagination to destination IDs
            var pagedDestinationIds = destinationIds
                .Skip(_currentPage * _pageSize)
                .Take(_pageSize)
                .ToList();
            
            Debug.WriteLine($"[CategoryDetailsPage] Loading {pagedDestinationIds.Count} destinations for page {_currentPage}");
            
            // Batch load destination details instead of individual calls
            var destinations = await Task.Run(() =>
            {
                // Use a single context for all database operations
                return pagedDestinationIds.Select(id => _destinatieRepo.GetById(id))
                                         .Where(d => d != null)
                                         .ToList();
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            
            Debug.WriteLine($"[CategoryDetailsPage] Retrieved {destinations.Count} destination objects");
            
            // Batch load all images for these destinations in one operation
            var allImages = await Task.Run(() =>
            {
                var imageDict = new Dictionary<int, string>();
                foreach (var destinationId in pagedDestinationIds)
                {
                    try
                    {
                        var images = _imaginiDestRepo.GetByDestinationId(destinationId);
                        var firstImage = images?.FirstOrDefault()?.ImagineUrl;
                        if (!string.IsNullOrEmpty(firstImage))
                        {
                            imageDict[destinationId] = firstImage;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading images for destination {destinationId}: {ex.Message}");
                    }
                }
                return imageDict;
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            
            Debug.WriteLine($"[CategoryDetailsPage] Batch loaded images for {allImages.Count} destinations");
            
            // Create display items efficiently using pre-loaded data
            var displayItems = destinations.Select(destination =>
            {
                // Get pre-loaded image URL or use placeholder
                allImages.TryGetValue(destination.Id_Destinatie, out var imageUrl);
                
                return new CategoryDestinationDisplayItem
                {
                    Id_Destinatie = destination.Id_Destinatie,
                    Denumire = destination.Denumire,
                    Location = $"{destination.Oras}, {destination.Tara}",
                    ImagineUrl = PrepareImageUrl(imageUrl),
                    PretAdult = destination.PretAdult,
                    PretMinor = destination.PretMinor
                };
            }).ToList();
            
            Debug.WriteLine($"[CategoryDetailsPage] Created {displayItems.Count} display items efficiently");
            
            // Update UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Add items to collection
                foreach (var item in displayItems)
                {
                    _destinations.Add(item);
                }
                
                // Update counters and pagination
                var totalDestinations = destinationIds.Count;
                var loadedDestinations = _destinations.Count;

                // Check if there are more results
                _hasMoreResults = loadedDestinations < totalDestinations;
                LoadMoreButton.IsVisible = _hasMoreResults;
                LoadMoreButton.Text = $"Încarcă mai multe ({Math.Min(_pageSize, totalDestinations - loadedDestinations)})";
                
                // Show/hide empty state
                EmptyStateLayout.IsVisible = !_destinations.Any();
                
                Debug.WriteLine($"[CategoryDetailsPage] UI updated - Total: {totalDestinations}, Loaded: {loadedDestinations}, Has more: {_hasMoreResults}");
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[CategoryDetailsPage] Destinations loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading destinations: {ex.Message}");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_currentPage == 0)
                {
                    EmptyStateLayout.IsVisible = true;
                    LoadMoreButton.IsVisible = false;
                }
            });
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task<string> GetFirstDestinationImageAsync(int destinationId)
    {
        try
        {
            var images = await Task.Run(() => _imaginiDestRepo.GetByDestinationId(destinationId), _cancellationTokenSource.Token).ConfigureAwait(false);
            return images?.FirstOrDefault()?.ImagineUrl;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting destination image: {ex.Message}");
            return null;
        }
    }

    private string PrepareImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return "https://via.placeholder.com/100x80/E0E0E0/999999?text=No+Image";

        // Check cache first
        if (_imageUrlCache.TryGetValue(imageUrl, out var cachedUrl))
        {
            return cachedUrl;
        }

        string preparedUrl;

        // If it's already a full URL, return as-is
        if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            preparedUrl = imageUrl;
        }
        // If it's a blob storage filename, construct the full URL
        else if (imageUrl.Contains(".jpg") || imageUrl.Contains(".png") || imageUrl.Contains(".jpeg") || imageUrl.Contains(".webp"))
        {
            preparedUrl = $"https://vacantaai.blob.core.windows.net/vacantaai/{imageUrl}";
        }
        else
        {
            preparedUrl = imageUrl;
        }

        // Cache the result for future use
        _imageUrlCache[imageUrl] = preparedUrl;
        
        return preparedUrl;
    }

    // Cache cleanup methods for memory management
    public static void ClearExpiredCache()
    {
        var expiredCategories = _categoryCache
            .Where(kvp => DateTime.Now - kvp.Value.CachedAt > _cacheExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredCategories)
        {
            _categoryCache.Remove(key);
        }

        // Clear image URL cache periodically (simple approach)
        if (_imageUrlCache.Count > 100)
        {
            _imageUrlCache.Clear();
        }

        Debug.WriteLine($"[CategoryDetailsPage] Cache cleanup: removed {expiredCategories.Count} expired categories");
    }

    private async void OnLoadMoreClicked(object sender, EventArgs e)
    {
        if (_hasMoreResults && !_isLoading)
        {
            _currentPage++;
            await LoadDestinationsAsync();
        }
    }

    private async void OnDestinationTapped(object sender, TappedEventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is CategoryDestinationDisplayItem destination)
        {
            try
            {
                // Visual feedback
                await frame.ScaleTo(0.95, 100);
                await frame.ScaleTo(1.0, 100);
                
                Debug.WriteLine($"[CategoryDetailsPage] Navigating to destination details with ID: {destination.Id_Destinatie}");
                await Shell.Current.GoToAsync($"{nameof(DestinationDetailsPage)}?destinationId={destination.Id_Destinatie}");
                Debug.WriteLine($"[CategoryDetailsPage] Navigation completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to destination details: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Eroare", "Nu s-a putut deschide pagina destinației.", "OK");
            }
        }
        else
        {
            Debug.WriteLine($"[CategoryDetailsPage] OnDestinationTapped called but sender or BindingContext is null");
            Debug.WriteLine($"[CategoryDetailsPage] Sender type: {sender?.GetType()?.Name}");
            Debug.WriteLine($"[CategoryDetailsPage] BindingContext type: {(sender as Frame)?.BindingContext?.GetType()?.Name}");
        }
    }

    private async Task LoadFavoriteStatusAsync()
    {
        try
        {
            Debug.WriteLine($"[CategoryDetailsPage] Starting to load favorite status for category ID: {_categoryId}");
            
            // Get current user ID
            var userIdStr = await UserSession.GetUserIdAsync();
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out _currentUserId))
            {
                Debug.WriteLine($"[CategoryDetailsPage] No valid user ID found, favorite functionality disabled");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Show button but as disabled state (gray heart)
                    FavoriteButton.Text = "🤍";
                    FavoriteButton.TextColor = Colors.Gray;
                    FavoriteButton.IsEnabled = false;
                    Debug.WriteLine($"[CategoryDetailsPage] Showing disabled favorite button (not authenticated)");
                });
                return;
            }
            
            // Check favorite status
            _isFavorite = await Task.Run(() => 
                _favoriteRepo.IsFavorite(_currentUserId, "CategorieVacanta", _categoryId), 
                _cancellationTokenSource.Token).ConfigureAwait(false);
            
            Debug.WriteLine($"[CategoryDetailsPage] Favorite status loaded: {_isFavorite}");
            
            // Update UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FavoriteButton.IsEnabled = true;
                UpdateFavoriteButtonUI();
                Debug.WriteLine($"[CategoryDetailsPage] Favorite button enabled with status: {_isFavorite}");
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[CategoryDetailsPage] Favorite status loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading favorite status: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Show button but as error state
                FavoriteButton.Text = "🤍";
                FavoriteButton.TextColor = Colors.Orange;
                FavoriteButton.IsEnabled = false;
                Debug.WriteLine($"[CategoryDetailsPage] Showing error favorite button");
            });
        }
    }

    private void UpdateFavoriteButtonUI()
    {
        if (_isFavorite)
        {
            FavoriteButton.Text = "❤️";
            FavoriteButton.TextColor = Color.FromArgb("#FF4444"); // Red
        }
        else
        {
            FavoriteButton.Text = "🤍";
            FavoriteButton.TextColor = Colors.White;
        }
    }

    private async void OnFavoriteClicked(object sender, EventArgs e)
    {
        try
        {
            if (_currentUserId <= 0)
            {
                await DisplayAlert("Info", "Trebuie să fii autentificat pentru a adăuga la favorite.", "OK");
                return;
            }

            // Visual feedback - animate button
            await FavoriteButton.ScaleTo(1.2, 100);
            await FavoriteButton.ScaleTo(1.0, 100);

            // Toggle favorite status (optimistic UI update)
            _isFavorite = !_isFavorite;
            UpdateFavoriteButtonUI();

            try
            {
                // Update in database
                var isNowFavorite = await Task.Run(() => 
                    _favoriteRepo.ToggleFavorite(_currentUserId, "CategorieVacanta", _categoryId), 
                    _cancellationTokenSource.Token);

                // Verify the result matches our optimistic update
                if (isNowFavorite != _isFavorite)
                {
                    _isFavorite = isNowFavorite;
                    UpdateFavoriteButtonUI();
                }

                Debug.WriteLine($"[CategoryDetailsPage] Favorite toggled successfully: {_isFavorite}");
            }
            catch (Exception dbEx)
            {
                Debug.WriteLine($"[CategoryDetailsPage] Database error toggling favorite: {dbEx.Message}");
                
                // Revert optimistic update on failure
                _isFavorite = !_isFavorite;
                UpdateFavoriteButtonUI();
                
                await DisplayAlert("Eroare", "Nu s-a putut actualiza statusul de favorit. Te rog încercați din nou.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OnFavoriteClicked: {ex.Message}");
            await DisplayAlert("Eroare", "A apărut o eroare neașteptată.", "OK");
        }
    }
}