using System.Collections.ObjectModel;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.Library;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Globalization;

namespace MauiAppDisertatieVacantaAI.Pages;

public class DestinationSearchResult
{
    public int Id_Destinatie { get; set; }
    public string Denumire { get; set; }
    public string Location { get; set; }
    public string ImageUrl { get; set; }
    public decimal PretAdult { get; set; }
    public decimal PretMinor { get; set; }
    public string Tara { get; set; }
    public string Oras { get; set; }
    public string Regiune { get; set; }
}

public class SearchCacheItem
{
    public List<DestinationSearchResult> Results { get; set; } = new();
    public DateTime CachedAt { get; set; }
    public bool HasMoreResults { get; set; }
}

public partial class SearchPage : ContentPage
{
    private readonly ObservableCollection<DestinationSearchResult> _results = new();
    
    // Repositories
    private readonly DestinatieRepository _destinatieRepo = new();
    private readonly ImaginiDestinatieRepository _imaginiRepo = new();
    private readonly CategorieVacantaRepository _categorieRepo = new();
    private readonly FacilitateRepository _facilitateRepo = new();
    private readonly PunctDeInteresRepository _poiRepo = new();
    private readonly CategorieVacanta_DestinatieRepository _catDestRepo = new();
    private readonly DestinatieFacilitateRepository _destFacRepo = new();
    
    private string _activeFilter = "Toate";
    private string _currentSearchTerm = "";
    private int _currentPage = 0;
    private const int _pageSize = 10;
    private bool _hasMoreResults = false;
    private bool _isLoading = false;

    // Price range filtering
    private decimal _minPrice = 0;
    private decimal _maxPrice = decimal.MaxValue;
    private bool _hasPriceFilter = false;

    // Sorting
    private string _currentSort = "Nume"; // Nume, PretAsc, PretDesc, Location

    // Debouncing & Caching
    private Timer _searchTimer;
    private CancellationTokenSource _searchCancellationTokenSource = new();
    private static readonly ConcurrentDictionary<string, SearchCacheItem> _searchCache = new();
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(500);

    // Search History & Autocomplete
    private readonly List<string> _searchHistory = new();
    private readonly ObservableCollection<string> _autocompleteResults = new();
    private readonly ObservableCollection<string> _searchHistoryItems = new();
    private const int _maxHistoryItems = 10;

    public SearchPage()
    {
        InitializeComponent();
        ResultsCollectionView.ItemsSource = _results;
        SearchHistoryCollectionView.ItemsSource = _searchHistoryItems;
        LoadSearchHistory();
    }

    private void UpdateHistoryToggleButton()
    {
        var isHistoryVisible = SearchHistoryFrame.IsVisible;
        
        HistoryToggleButton.Text = isHistoryVisible ? "▲" : "▼";
        HistoryToggleButton.TextColor = isHistoryVisible ? 
            (Color)Application.Current.Resources["PrimaryBlue"] : 
            GetInactiveTextColor();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateFilterButtons();
        UpdateSortButton();
        
        // Clean expired cache periodically
        CleanExpiredCache();
    }

    private void UpdateSortButton()
    {
        Device.BeginInvokeOnMainThread(() =>
        {
            if (_currentSort == "Nume")
            {
                // Default state - inactive appearance
                SortButton.BackgroundColor = GetInactiveBackgroundColor();
                SortButton.TextColor = GetInactiveTextColor();
                SortButton.Text = "📊 Sortare";
            }
            else
            {
                // Active sort - blue appearance with specific text
                SortButton.BackgroundColor = (Color)Application.Current.Resources["PrimaryBlue"];
                SortButton.TextColor = Colors.White;
                
                SortButton.Text = _currentSort switch
                {
                    "PretAsc" => "💰⬆ Preț ↗",
                    "PretDesc" => "💰⬇ Preț ↘",
                    "Location" => "📍 Locație",
                    _ => "📊 Sortare"
                };
            }
            
            Debug.WriteLine($"[SearchPage] UpdateSortButton - Current sort: '{_currentSort}', Button text: '{SortButton.Text}'");
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Cancel any pending search operations with proper cleanup
        _searchTimer?.Dispose();
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource?.Dispose();
        _searchCancellationTokenSource = new CancellationTokenSource();
        
        Debug.WriteLine($"[SearchPage] OnDisappearing - operations cancelled and cleaned up");
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchTerm = e.NewTextValue?.Trim() ?? "";
        
        // Show/hide clear button
        ClearButton.IsVisible = !string.IsNullOrEmpty(searchTerm);
        
        // Cancel previous search timer and operations
        _searchTimer?.Dispose();
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource?.Dispose();
        _searchCancellationTokenSource = new CancellationTokenSource();
        
        // Update current search term
        _currentSearchTerm = searchTerm;
        
        // Reset pagination
        _currentPage = 0;
        _results.Clear();
        LoadMoreButton.IsVisible = false;
        
        if (string.IsNullOrEmpty(searchTerm))
        {
            EmptyStateLabel.Text = "Introdu un termen pentru a căuta destinații.";
            // History is now always available via toggle button - don't auto-show
            return;
        }

        // Start debounced search (history can still be toggled during search)
        _searchTimer = new Timer(async _ => await PerformDebouncedSearchAsync(), null, _debounceDelay, Timeout.InfiniteTimeSpan);
    }

    private async Task PerformDebouncedSearchAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (!string.IsNullOrEmpty(_currentSearchTerm))
            {
                await PerformSearchAsync();
            }
        });
    }

    private void OnClearSearch(object sender, EventArgs e)
    {
        SearchEntry.Text = "";
        ClearButton.IsVisible = false;
        _results.Clear();
        LoadMoreButton.IsVisible = false;
        EmptyStateLabel.Text = "Introdu un termen pentru a căuta destinații.";
        // History remains toggleable - no auto-show
    }

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            Debug.WriteLine($"[SearchPage] Filter clicked: '{button.Text}'");
            
            // Use button reference instead of text to avoid encoding issues
            if (button == AllFilter)
                _activeFilter = "Toate";
            else if (button == CategoriesFilter)
                _activeFilter = "Categorii";
            else if (button == FacilitiesFilter)
                _activeFilter = "Facilități";
            else if (button == PoiFilter)
                _activeFilter = "Puncte Interes";
            
            Debug.WriteLine($"[SearchPage] Active filter set to: '{_activeFilter}'");
            UpdateFilterButtons();
            
            // Reset pagination and research
            _currentPage = 0;
            _results.Clear();
            LoadMoreButton.IsVisible = false;
            
            if (!string.IsNullOrEmpty(_currentSearchTerm))
            {
                await PerformSearchAsync();
            }
        }
    }

    private void UpdateFilterButtons()
    {
        var primaryBlue = (Color)Application.Current.Resources["PrimaryBlue"];
        var inactiveBg = GetInactiveBackgroundColor();
        var inactiveText = GetInactiveTextColor();

        Debug.WriteLine($"[SearchPage] UpdateFilterButtons - Active filter: '{_activeFilter}'");

        // Reset all buttons
        AllFilter.BackgroundColor = _activeFilter == "Toate" ? primaryBlue : inactiveBg;
        AllFilter.TextColor = _activeFilter == "Toate" ? Colors.White : inactiveText;

        CategoriesFilter.BackgroundColor = _activeFilter == "Categorii" ? primaryBlue : inactiveBg;
        CategoriesFilter.TextColor = _activeFilter == "Categorii" ? Colors.White : inactiveText;

        var isFacilitiesActive = _activeFilter == "Facilități";
        Debug.WriteLine($"[SearchPage] Facilities active check: '{_activeFilter}' == 'Facilități' = {isFacilitiesActive}");
        
        FacilitiesFilter.BackgroundColor = isFacilitiesActive ? primaryBlue : inactiveBg;
        FacilitiesFilter.TextColor = isFacilitiesActive ? Colors.White : inactiveText;

        PoiFilter.BackgroundColor = _activeFilter == "Puncte Interes" ? primaryBlue : inactiveBg;
        PoiFilter.TextColor = _activeFilter == "Puncte Interes" ? Colors.White : inactiveText;
    }

    private Color GetInactiveBackgroundColor()
    {
        return Application.Current.UserAppTheme == AppTheme.Dark
            ? (Color)Application.Current.Resources["MediumGray"]
            : (Color)Application.Current.Resources["LightGray"];
    }

    private Color GetInactiveTextColor()
    {
        return Application.Current.UserAppTheme == AppTheme.Dark
            ? (Color)Application.Current.Resources["LightGray"]
            : (Color)Application.Current.Resources["DarkGray"];
    }

    private async Task PerformSearchAsync()
    {
        if (_isLoading || string.IsNullOrEmpty(_currentSearchTerm))
            return;

        // Check cache first
        var cacheKey = $"{_currentSearchTerm}_{_activeFilter}_{_currentPage}_{_currentSort}_{_hasPriceFilter}_{_minPrice}_{_maxPrice}";
        if (_searchCache.TryGetValue(cacheKey, out var cachedItem) && 
            DateTime.Now - cachedItem.CachedAt < _cacheExpiration)
        {
            Debug.WriteLine($"[SearchPage] Using cached results for: {cacheKey}");
            
            foreach (var result in cachedItem.Results)
            {
                _results.Add(result);
            }
            
            _hasMoreResults = cachedItem.HasMoreResults;
            LoadMoreButton.IsVisible = _hasMoreResults;
            LoadMoreButton.Text = $"Încarcă mai multe ({_pageSize})";
            
            EmptyStateLabel.Text = _results.Any() ? "" : "Nu s-au găsit destinații pentru căutarea ta.";
            return;
        }

        try
        {
            _isLoading = true;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            // Use existing cancellation token
            var cancellationToken = _searchCancellationTokenSource.Token;

            var searchResults = await SearchDestinationsAsync(_currentSearchTerm, _activeFilter, _currentPage, _pageSize, cancellationToken);
            
            // Check if operation was cancelled
            if (cancellationToken.IsCancellationRequested)
                return;

            if (searchResults.Any())
            {
                foreach (var result in searchResults)
                {
                    _results.Add(result);
                }
                
                // Check if there might be more results
                _hasMoreResults = searchResults.Count == _pageSize;
                LoadMoreButton.IsVisible = _hasMoreResults;
                LoadMoreButton.Text = $"Încarcă mai multe ({_pageSize})";
                
                EmptyStateLabel.Text = "";

                // Cache the results
                _searchCache[cacheKey] = new SearchCacheItem
                {
                    Results = searchResults,
                    CachedAt = DateTime.Now,
                    HasMoreResults = _hasMoreResults
                };

                // Add to search history (only for first page)
                if (_currentPage == 0)
                {
                    AddToSearchHistory(_currentSearchTerm);
                }
            }
            else if (_currentPage == 0)
            {
                EmptyStateLabel.Text = "Nu s-au găsit destinații pentru căutarea ta.";
                LoadMoreButton.IsVisible = false;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[SearchPage] Search operation was cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error performing search: {ex.Message}");
            if (!_searchCancellationTokenSource.Token.IsCancellationRequested && _currentPage == 0)
            {
                EmptyStateLabel.Text = "A apărut o eroare la căutare. Te rog incearcă din nou.";
            }
        }
        finally
        {
            _isLoading = false;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async Task<List<DestinationSearchResult>> SearchDestinationsAsync(string searchTerm, string filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var destinationIds = new HashSet<int>();
                var searchLower = searchTerm.ToLowerInvariant();

                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Get all destinations first
                var allDestinations = _destinatieRepo.GetAll().ToList();

                // Filter by search term in destination properties
                var directMatches = allDestinations.Where(d =>
                    d.Denumire.ToLowerInvariant().Contains(searchLower) ||
                    d.Tara.ToLowerInvariant().Contains(searchLower) ||
                    d.Oras.ToLowerInvariant().Contains(searchLower) ||
                    d.Regiune.ToLowerInvariant().Contains(searchLower) ||
                    (d.Descriere?.ToLowerInvariant().Contains(searchLower) ?? false)
                ).Select(d => d.Id_Destinatie);

                destinationIds.UnionWith(directMatches);

                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Search by filter type
                if (filter == "Toate" || filter == "Categorii")
                {
                    var categories = _categorieRepo.GetAll()
                        .Where(c => c.Denumire.ToLowerInvariant().Contains(searchLower))
                        .ToList();

                    foreach (var category in categories)
                    {
                        var categoryDestinations = _catDestRepo.GetByCategoryId(category.Id_CategorieVacanta)
                            .Select(cd => cd.Id_Destinatie);
                        destinationIds.UnionWith(categoryDestinations);
                    }
                }

                if (filter == "Toate" || filter == "Facilități")
                {
                    var facilities = _facilitateRepo.GetAll()
                        .Where(f => f.Denumire.ToLowerInvariant().Contains(searchLower))
                        .ToList();

                    foreach (var facility in facilities)
                    {
                        var facilityDestinations = _destFacRepo.GetByFacilityId(facility.Id_Facilitate)
                            .Select(df => df.Id_Destinatie);
                        destinationIds.UnionWith(facilityDestinations);
                    }
                }

                if (filter == "Toate" || filter == "Puncte Interes")
                {
                    var pois = _poiRepo.GetAll()
                        .Where(p => (p.Denumire?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                                   (p.Tip?.ToLowerInvariant().Contains(searchLower) ?? false))
                        .Select(p => p.Id_Destinatie)
                        .Distinct();

                    destinationIds.UnionWith(pois);
                }

                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Get the final destinations with price filtering and sorting
                var filteredDestinations = allDestinations
                    .Where(d => destinationIds.Contains(d.Id_Destinatie));

                // Apply price filter if active
                if (_hasPriceFilter)
                {
                    filteredDestinations = filteredDestinations.Where(d => 
                        d.PretAdult >= _minPrice && d.PretAdult <= _maxPrice);
                }

                // Apply sorting
                filteredDestinations = _currentSort switch
                {
                    "PretAsc" => filteredDestinations.OrderBy(d => d.PretAdult),
                    "PretDesc" => filteredDestinations.OrderByDescending(d => d.PretAdult),
                    "Location" => filteredDestinations.OrderBy(d => d.Tara).ThenBy(d => d.Oras),
                    _ => filteredDestinations.OrderBy(d => d.Denumire) // Default: Nume
                };

                // Apply pagination
                var finalDestinations = filteredDestinations
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Convert to search results with images
                var results = new List<DestinationSearchResult>();
                
                foreach (var dest in finalDestinations)
                {
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var imageUrl = await GetFirstDestinationImageAsync(dest.Id_Destinatie).ConfigureAwait(false);
                    
                    results.Add(new DestinationSearchResult
                    {
                        Id_Destinatie = dest.Id_Destinatie,
                        Denumire = dest.Denumire,
                        Location = $"{dest.Oras}, {dest.Tara}",
                        ImageUrl = imageUrl ?? "dotnet_bot.png",
                        PretAdult = dest.PretAdult,
                        PretMinor = dest.PretMinor,
                        Tara = dest.Tara,
                        Oras = dest.Oras,
                        Regiune = dest.Regiune
                    });
                }

                return results;
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SearchDestinationsAsync: {ex.Message}");
                return new List<DestinationSearchResult>();
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetFirstDestinationImageAsync(int destinationId)
    {
        try
        {
            var images = await Task.Run(() => _imaginiRepo.GetByDestinationId(destinationId)).ConfigureAwait(false);
            return images?.FirstOrDefault()?.ImagineUrl;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting destination image: {ex.Message}");
            return null;
        }
    }

    private async void OnLoadMoreClicked(object sender, EventArgs e)
    {
        if (_hasMoreResults && !_isLoading)
        {
            _currentPage++;
            await PerformSearchAsync();
        }
    }

    private async void OnDestinationTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is DestinationSearchResult destination)
        {
            try
            {
                Debug.WriteLine($"[SearchPage] Navigating to destination details with ID: {destination.Id_Destinatie}");
                await Shell.Current.GoToAsync($"{nameof(DestinationDetailsPage)}?destinationId={destination.Id_Destinatie}");
                Debug.WriteLine($"[SearchPage] Navigation completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to destination details: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Eroare", "Nu s-a putut deschide pagina destinatiei.", "OK");
            }
        }
        else
        {
            Debug.WriteLine($"[SearchPage] OnDestinationTapped called but sender or BindingContext is null");
            Debug.WriteLine($"[SearchPage] Sender type: {sender?.GetType()?.Name}");
            Debug.WriteLine($"[SearchPage] BindingContext type: {(sender as Border)?.BindingContext?.GetType()?.Name}");
        }
    }

    #region Search History & Autocomplete

    private void AddToSearchHistory(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return;

        try
        {
            // Thread-safe check and modification
            lock (_searchHistory)
            {
                // Remove if already exists to avoid duplicates
                if (_searchHistory.Contains(searchTerm))
                {
                    _searchHistory.Remove(searchTerm);
                    _searchHistoryItems.Remove(searchTerm);
                }

                // Add to beginning
                _searchHistory.Insert(0, searchTerm);
                _searchHistoryItems.Insert(0, searchTerm);
                
                // Keep only recent items
                while (_searchHistory.Count > _maxHistoryItems)
                {
                    _searchHistory.RemoveAt(_searchHistory.Count - 1);
                    _searchHistoryItems.RemoveAt(_searchHistoryItems.Count - 1);
                }
                
                Debug.WriteLine($"[SearchPage] Added '{searchTerm}' to history. Total items: {_searchHistory.Count}");
            }

            SaveSearchHistory();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPage] Error adding to search history: {ex.Message}");
        }
    }

    private void LoadSearchHistory()
    {
        try
        {
            // Load from preferences (simple implementation)
            var historyJson = Preferences.Get("SearchHistory", "[]");
            var history = System.Text.Json.JsonSerializer.Deserialize<List<string>>(historyJson) ?? new List<string>();
            
            // Thread-safe update
            lock (_searchHistory)
            {
                _searchHistory.Clear();
                _searchHistory.AddRange(history.Take(_maxHistoryItems).Where(s => !string.IsNullOrWhiteSpace(s)));
                
                // Update observable collection on main thread
                Device.BeginInvokeOnMainThread(() =>
                {
                    _searchHistoryItems.Clear();
                    foreach (var item in _searchHistory)
                    {
                        _searchHistoryItems.Add(item);
                    }
                });
                
                Debug.WriteLine($"[SearchPage] Loaded {_searchHistory.Count} items from search history");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPage] Error loading search history: {ex.Message}");
            
            // Ensure we have empty but valid collections
            lock (_searchHistory)
            {
                _searchHistory.Clear();
                Device.BeginInvokeOnMainThread(() => _searchHistoryItems.Clear());
            }
        }
    }

    private void SaveSearchHistory()
    {
        try
        {
            lock (_searchHistory)
            {
                var historyJson = System.Text.Json.JsonSerializer.Serialize(_searchHistory);
                Preferences.Set("SearchHistory", historyJson);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving search history: {ex.Message}");
        }
    }

    private void ShowSearchHistory()
    {
        try
        {
            if (_searchHistory?.Any() == true)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    SearchHistoryFrame.IsVisible = true;
                    UpdateHistoryToggleButton();
                });
                Debug.WriteLine("[SearchPage] Search history shown");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPage] Error showing search history: {ex.Message}");
        }
    }

    private void HideSearchHistory()
    {
        try
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                SearchHistoryFrame.IsVisible = false;
                UpdateHistoryToggleButton();
            });
            Debug.WriteLine("[SearchPage] Search history hidden");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPage] Error hiding search history: {ex.Message}");
        }
    }

    #endregion

    #region Cache Management

    public static void ClearSearchCache()
    {
        _searchCache.Clear();
        Debug.WriteLine("[SearchPage] Search cache cleared");
    }

    public static void CleanExpiredCache()
    {
        var expiredKeys = _searchCache
            .Where(kvp => DateTime.Now - kvp.Value.CachedAt > _cacheExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _searchCache.TryRemove(key, out _);
        }

        if (expiredKeys.Any())
        {
            Debug.WriteLine($"[SearchPage] Cleaned {expiredKeys.Count} expired cache entries");
        }
    }

    #endregion

    private async void OnPriceRangeClicked(object sender, EventArgs e)
    {
        try
        {
            var action = await DisplayActionSheet(
                "Filtrare după preț", 
                "Anulează", 
                _hasPriceFilter ? "Șterge filtrul" : null,
                "💰 Sub 500 €",
                "💰💰 500-1500 €", 
                "💰💰💰 1500-3000 €",
                "💰💰💰💰 Peste 3000 €"
            );

            switch (action)
            {
                case "💰 Sub 500 €":
                    SetPriceRange(0, 500);
                    break;
                case "💰💰 500-1500 €":
                    SetPriceRange(500, 1500);
                    break;
                case "💰💰💰 1500-3000 €":
                    SetPriceRange(1500, 3000);
                    break;
                case "💰💰💰💰 Peste 3000 €":
                    SetPriceRange(3000, decimal.MaxValue);
                    break;
                case "Șterge filtrul":
                    ClearPriceFilter();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in price range selection: {ex.Message}");
        }
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        try
        {
            var hasActiveSort = _currentSort != "Nume";
            
            Debug.WriteLine($"[SearchPage] OnSortClicked - Current sort: '{_currentSort}', Has active sort: {hasActiveSort}");
            
            var action = await DisplayActionSheet(
                "Sortare rezultate", 
                "Anulează", 
                hasActiveSort ? "Șterge sortarea" : null,
                "📝 Nume (A-Z)",
                "💰⬆ Preț crescător", 
                "💰⬇ Preț descrescător",
                "📍 Locație (A-Z)"
            );

            Debug.WriteLine($"[SearchPage] User selected action: '{action}'");

            switch (action)
            {
                case "📝 Nume (A-Z)":
                    SetSort("Nume");
                    break;
                case "💰⬆ Preț crescător":
                    SetSort("PretAsc");
                    break;
                case "💰⬇ Preț descrescător":
                    SetSort("PretDesc");
                    break;
                case "📍 Locație (A-Z)":
                    SetSort("Location");
                    break;
                case "Șterge sortarea":
                    ClearSort();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in sort selection: {ex.Message}");
        }
    }

    private void SetPriceRange(decimal min, decimal max)
    {
        _minPrice = min;
        _maxPrice = max;
        _hasPriceFilter = true;
        
        Debug.WriteLine($"[SearchPage] Price range set: {min} - {max}");
        
        // Force refresh button appearance by first resetting to inactive, then setting to active
        Device.BeginInvokeOnMainThread(() =>
        {
            // Reset first
            PriceRangeButton.BackgroundColor = GetInactiveBackgroundColor();
            PriceRangeButton.TextColor = GetInactiveTextColor();
            
            // Then set to active
            PriceRangeButton.BackgroundColor = (Color)Application.Current.Resources["PrimaryBlue"];
            PriceRangeButton.TextColor = Colors.White;
            
            if (max == decimal.MaxValue)
                PriceRangeButton.Text = $"💰 {min:0}€+";
            else
                PriceRangeButton.Text = $"💰 {min:0}-{max:0}€";
        });

        ApplyFiltersAndSearch();
    }

    private void ClearPriceFilter()
    {
        _minPrice = 0;
        _maxPrice = decimal.MaxValue;
        _hasPriceFilter = false;
        
        // Reset button appearance
        PriceRangeButton.BackgroundColor = GetInactiveBackgroundColor();
        PriceRangeButton.TextColor = GetInactiveTextColor();
        PriceRangeButton.Text = "💰 Preț";

        ApplyFiltersAndSearch();
    }

    private void SetSort(string sortType)
    {
        _currentSort = sortType;
        
        Debug.WriteLine($"[SearchPage] Sort set to: '{_currentSort}'");
        
        // Update button appearance with proper text based on sort type
        Device.BeginInvokeOnMainThread(() =>
        {
            if (sortType == "Nume")
            {
                // For "Nume" selection, keep it inactive since it's the default
                SortButton.BackgroundColor = GetInactiveBackgroundColor();
                SortButton.TextColor = GetInactiveTextColor();
                SortButton.Text = "📊 Sortare";
            }
            else
            {
                // For other selections, make it active with specific text
                SortButton.BackgroundColor = (Color)Application.Current.Resources["PrimaryBlue"];
                SortButton.TextColor = Colors.White;
                
                SortButton.Text = sortType switch
                {
                    "PretAsc" => "💰⬆ Preț ↗",
                    "PretDesc" => "💰⬇ Preț ↘", 
                    "Location" => "📍 Locație",
                    _ => "📊 Sortare"
                };
            }
        });
        
        ApplyFiltersAndSearch();
    }

    private void ClearSort()
    {
        _currentSort = "Nume";
        
        // Reset button appearance to inactive state
        Device.BeginInvokeOnMainThread(() =>
        {
            SortButton.BackgroundColor = GetInactiveBackgroundColor();
            SortButton.TextColor = GetInactiveTextColor();
            SortButton.Text = "📊 Sortare";
        });

        ApplyFiltersAndSearch();
    }

    private void ApplyFiltersAndSearch()
    {
        // Reset pagination
        _currentPage = 0;
        _results.Clear();
        LoadMoreButton.IsVisible = false;
        
        // Perform the search with current filters
        PerformSearchAsync();
    }

    private void OnHistoryToggleClicked(object sender, EventArgs e)
    {
        try
        {
            if (SearchHistoryFrame.IsVisible)
            {
                // Hide history
                HideSearchHistory();
                Debug.WriteLine("[SearchPage] History hidden via toggle button");
            }
            else if (_searchHistory.Any())
            {
                // Show history
                ShowSearchHistory();
                Debug.WriteLine("[SearchPage] History shown via toggle button");
            }
            
            // Update button appearance is handled automatically by Show/Hide methods
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPage] Error in OnHistoryToggleClicked: {ex.Message}");
        }
    }

    private void OnHistoryItemTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Border border && border.BindingContext is string searchTerm)
            {
                SearchEntry.Text = searchTerm;
                // Don't auto-hide history when item is selected - let user manually toggle
                SearchEntry.Unfocus();
                Debug.WriteLine($"[SearchPage] History item tapped: '{searchTerm}'");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPage] Error in OnHistoryItemTapped: {ex.Message}");
        }
    }

    private void OnSearchEntryFocused(object sender, FocusEventArgs e)
    {
        try
        {
            // History is now always available via toggle button - no auto-show on focus
            Debug.WriteLine("[SearchPage] Search entry focused - history available via toggle");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPage] Error in OnSearchEntryFocused: {ex.Message}");
        }
    }

    private void OnSearchEntryUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            // History remains visible if user toggled it on - no auto-hide on unfocus
            Debug.WriteLine("[SearchPage] Search entry unfocused - history state preserved");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPage] Error in OnSearchEntryUnfocused: {ex.Message}");
        }
    }
}