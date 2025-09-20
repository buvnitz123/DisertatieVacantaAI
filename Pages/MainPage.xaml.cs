using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using System.Diagnostics;
using System.Collections.ObjectModel;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;

namespace MauiAppDisertatieVacantaAI.Pages
{
    // ViewModel classes for data binding
    public class CategoryDisplayItem
    {
        public int Id { get; set; }
        public string Denumire { get; set; }
        public string ImageUrl { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class DestinationDisplayItem
    {
        public int Id { get; set; }
        public string Denumire { get; set; }
        public string Location { get; set; }
        public string ImageUrl { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class PointOfInterestDisplayItem
    {
        public int Id { get; set; }
        public string Denumire { get; set; }
        public string Tip { get; set; }
        public string ImageUrl { get; set; }
        public int Id_Destinatie { get; set; } // Add destination ID for direct navigation
    }

    public partial class MainPage : ContentPage
    {
        private readonly CategorieVacantaRepository _categorieRepo = new();
        private readonly DestinatieRepository _destinatieRepo = new();
        private readonly PunctDeInteresRepository _poiRepo = new();
        private readonly ImaginiDestinatieRepository _imaginiDestRepo = new();
        private readonly ImaginiPunctDeInteresRepository _imaginiPoiRepo = new();
        private readonly FavoriteRepository _favoriteRepo = new();

        // Cancellation support for cleanup
        private CancellationTokenSource _cancellationTokenSource = new();

        private ObservableCollection<DestinationDisplayItem> _destinations = new();
        private ObservableCollection<PointOfInterestDisplayItem> _pointsOfInterest = new();
        private int _currentUserId;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserInfoAsync();
            await LoadHomeContentAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Cancel any ongoing operations
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            
            Debug.WriteLine($"[MainPage] OnDisappearing - operations cancelled");
        }

        private async Task LoadUserInfoAsync()
        {
            try
            {
                // Prefer DB-backed user fetch
                var user = await UserSession.GetUserFromSessionAsync();
                if (user != null)
                {
                    WelcomeLabel.Text = $"Bine ai venit, {user.Nume}!".Trim();
                    _currentUserId = user.Id_Utilizator; // Store user ID for favorites
                    return;
                }

                // Fallbacks if session/db not available
                string userName = await UserSession.GetUserNameAsync();
                if (!string.IsNullOrEmpty(userName))
                {
                    WelcomeLabel.Text = $"Bine ai venit, {userName}!";
                }
                else
                {
                    WelcomeLabel.Text = "Bine ai venit!";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user info: {ex.Message}");
                WelcomeLabel.Text = "Bine ai venit!"; // Fallback display
            }
        }

        private async Task LoadHomeContentAsync()
        {
            try
            {
                LoadingIndicator.IsRunning = true;

                // Load all content in parallel
                await Task.WhenAll(
                    LoadCategoriesAsync(),
                    LoadDestinationsAsync(),
                    LoadPointsOfInterestAsync()
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading home content: {ex.Message}");
                await DisplayAlert("Eroare", "Nu s-au putut incarca datele", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                // Get current user ID for favorites
                var userIdStr = await UserSession.GetUserIdAsync();
                int.TryParse(userIdStr, out _currentUserId);
                
                var categories = await Task.Run(() => _categorieRepo.GetAll().Take(4).ToList(), _cancellationTokenSource.Token).ConfigureAwait(false);
                
                // Get user favorites for categories in batch
                Dictionary<int, bool> favoriteStatus = new();
                if (_currentUserId > 0 && categories.Any())
                {
                    var categoryIds = categories.Select(c => c.Id_CategorieVacanta);
                    favoriteStatus = await Task.Run(() => 
                        _favoriteRepo.GetFavoritesStatusBatch(_currentUserId, "CategorieVacanta", categoryIds), 
                        _cancellationTokenSource.Token).ConfigureAwait(false);
                }
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CategoriesGrid.Children.Clear();
                    
                    if (!categories.Any())
                    {
                        NoCategoriesLabel.IsVisible = true;
                        return;
                    }

                    NoCategoriesLabel.IsVisible = false;

                    for (int i = 0; i < categories.Count; i++)
                    {
                        var category = categories[i];
                        var row = i / 2;
                        var col = i % 2;

                        var isFavorite = favoriteStatus.GetValueOrDefault(category.Id_CategorieVacanta, false);
                        var categoryFrame = CreateCategoryFrame(category, isFavorite);
                        Grid.SetRow(categoryFrame, row);
                        Grid.SetColumn(categoryFrame, col);
                        CategoriesGrid.Children.Add(categoryFrame);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[MainPage] Categories loading cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading categories: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    NoCategoriesLabel.IsVisible = true;
                });
            }
        }

        private async Task LoadDestinationsAsync()
        {
            try
            {
                var destinations = await Task.Run(() => _destinatieRepo.GetAll().Take(5).ToList(), _cancellationTokenSource.Token).ConfigureAwait(false);
                
                // Get user favorites for destinations in batch
                Dictionary<int, bool> favoriteStatus = new();
                if (_currentUserId > 0 && destinations.Any())
                {
                    var destinationIds = destinations.Select(d => d.Id_Destinatie);
                    favoriteStatus = await Task.Run(() => 
                        _favoriteRepo.GetFavoritesStatusBatch(_currentUserId, "Destinatie", destinationIds), 
                        _cancellationTokenSource.Token).ConfigureAwait(false);
                }
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _destinations.Clear();

                    if (!destinations.Any())
                    {
                        NoDestinationsLabel.IsVisible = true;
                        DestinationsCarousel.IsVisible = false;
                        return;
                    }

                    NoDestinationsLabel.IsVisible = false;
                    DestinationsCarousel.IsVisible = true;

                    foreach (var dest in destinations)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                            break;
                            
                        var imageUrl = await GetFirstDestinationImageAsync(dest.Id_Destinatie);
                        var isFavorite = favoriteStatus.GetValueOrDefault(dest.Id_Destinatie, false);
                        
                        var displayItem = new DestinationDisplayItem
                        {
                            Id = dest.Id_Destinatie,
                            Denumire = dest.Denumire,
                            Location = $"{dest.Oras}, {dest.Tara}",
                            ImageUrl = imageUrl ?? "placeholder_image.png",
                            IsFavorite = isFavorite
                        };
                        _destinations.Add(displayItem);
                    }

                    DestinationsCarousel.ItemsSource = _destinations;
                });
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[MainPage] Destinations loading cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading destinations: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    NoDestinationsLabel.IsVisible = true;
                    DestinationsCarousel.IsVisible = false;
                });
            }
        }

        private async Task LoadPointsOfInterestAsync()
        {
            try
            {
                var pois = await Task.Run(() => _poiRepo.GetAll().Take(5).ToList(), _cancellationTokenSource.Token).ConfigureAwait(false);
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _pointsOfInterest.Clear();

                    if (!pois.Any())
                    {
                        NoPointsLabel.IsVisible = true;
                        PointsOfInterestCarousel.IsVisible = false;
                        return;
                    }

                    NoPointsLabel.IsVisible = false;
                    PointsOfInterestCarousel.IsVisible = true;

                    foreach (var poi in pois)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                            break;
                            
                        var imageUrl = await GetFirstPoiImageAsync(poi.Id_PunctDeInteres);
                        var displayItem = new PointOfInterestDisplayItem
                        {
                            Id = poi.Id_PunctDeInteres,
                            Denumire = poi.Denumire,
                            Tip = poi.Tip ?? "Atractie",
                            ImageUrl = imageUrl ?? "placeholder_image.png",
                            Id_Destinatie = poi.Id_Destinatie // Store destination ID directly
                        };
                        _pointsOfInterest.Add(displayItem);
                    }

                    PointsOfInterestCarousel.ItemsSource = _pointsOfInterest;
                });
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[MainPage] POIs loading cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading points of interest: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    NoPointsLabel.IsVisible = true;
                    PointsOfInterestCarousel.IsVisible = false;
                });
            }
        }

        private Frame CreateCategoryFrame(CategorieVacanta category, bool isFavorite)
        {
            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 12,
                Padding = 0,
                HasShadow = true,
                BorderColor = Colors.Transparent
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnCategoryTapped(category);
            frame.GestureRecognizers.Add(tapGesture);

            var grid = new Grid();

            var image = new Image
            {
                Source = !string.IsNullOrEmpty(category.ImagineUrl) ? category.ImagineUrl : "placeholder_image.png",
                Aspect = Aspect.AspectFill,
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill
            };

            var overlay = new BoxView
            {
                Color = Color.FromArgb("#80000000"),
                VerticalOptions = LayoutOptions.End,
                HeightRequest = 50
            };

            var label = new Label
            {
                Text = category.Denumire,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(8, 0, 8, 10),
                LineBreakMode = LineBreakMode.TailTruncation
            };

            // Add favorite heart - always visible (empty or filled)
            var favoriteHeart = new Label
            {
                Text = isFavorite ? "❤️" : "🤍",
                FontSize = 16,
                TextColor = isFavorite ? Color.FromArgb("#FF4444") : Colors.White,
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.End,
                Padding = new Thickness(0, 8, 8, 0)
            };

            grid.Children.Add(image);
            grid.Children.Add(overlay);
            grid.Children.Add(label);
            grid.Children.Add(favoriteHeart);
            frame.Content = grid;

            return frame;
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

        private async Task<string> GetFirstPoiImageAsync(int poiId)
        {
            try
            {
                var images = await Task.Run(() => _imaginiPoiRepo.GetByPointOfInterestId(poiId), _cancellationTokenSource.Token).ConfigureAwait(false);
                return images?.FirstOrDefault()?.ImagineUrl;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting POI image: {ex.Message}");
                return null;
            }
        }

        // Event handlers
        private async void OnCategoryTapped(CategorieVacanta category)
        {
            try
            {
                Debug.WriteLine($"[MainPage] Navigating to category details with ID: {category.Id_CategorieVacanta}");
                await Shell.Current.GoToAsync($"{nameof(CategoryDetailsPage)}?categoryId={category.Id_CategorieVacanta}");
                Debug.WriteLine($"[MainPage] Category navigation completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to category details: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Eroare", "Nu s-a putut deschide pagina categoriei.", "OK");
            }
        }

        private async void OnDestinationTapped(object sender, TappedEventArgs e)
        {
            if (sender is Element element && element.BindingContext is DestinationDisplayItem destination)
            {
                try
                {
                    Debug.WriteLine($"[MainPage] Navigating to destination details with ID: {destination.Id}");
                    await Shell.Current.GoToAsync($"{nameof(DestinationDetailsPage)}?destinationId={destination.Id}");
                    Debug.WriteLine($"[MainPage] Navigation completed successfully");
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
                Debug.WriteLine($"[MainPage] OnDestinationTapped called but sender or BindingContext is null");
                Debug.WriteLine($"[MainPage] Sender type: {sender?.GetType()?.Name}");
                Debug.WriteLine($"[MainPage] BindingContext type: {(sender as Element)?.BindingContext?.GetType()?.Name}");
            }
        }

        private async void OnPointOfInterestTapped(object sender, TappedEventArgs e)
        {
            if (sender is Element element && element.BindingContext is PointOfInterestDisplayItem poi)
            {
                try
                {
                    // Use the pre-loaded destination ID for direct navigation
                    if (poi.Id_Destinatie > 0)
                    {
                        Debug.WriteLine($"[MainPage] Navigating from POI '{poi.Denumire}' to destination details with ID: {poi.Id_Destinatie}");
                        await Shell.Current.GoToAsync($"{nameof(DestinationDetailsPage)}?destinationId={poi.Id_Destinatie}");
                        Debug.WriteLine($"[MainPage] POI to destination navigation completed successfully");
                    }
                    else
                    {
                        Debug.WriteLine($"[MainPage] POI '{poi.Denumire}' has invalid destination ID: {poi.Id_Destinatie}");
                        await DisplayAlert("Eroare", "Nu se poate găsi destinația asociată acestui punct de interes.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error navigating from POI to destination: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    await DisplayAlert("Eroare", "Nu s-a putut deschide pagina destinației.", "OK");
                }
            }
            else
            {
                Debug.WriteLine($"[MainPage] OnPointOfInterestTapped called but sender or BindingContext is null");
                Debug.WriteLine($"[MainPage] Sender type: {sender?.GetType()?.Name}");
                Debug.WriteLine($"[MainPage] BindingContext type: {(sender as Element)?.BindingContext?.GetType()?.Name}");
            }
        }
    }
}
