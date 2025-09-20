using System.Collections.ObjectModel;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public class DestinationImageItem
{
    public string ImagineUrl { get; set; }
}

public class FacilityDisplayItem
{
    public string Denumire { get; set; }
    public string Descriere { get; set; }
    public bool HasDescription => !string.IsNullOrWhiteSpace(Descriere);
}

public class DestinationCategoryDisplayItem
{
    public int Id_CategorieVacanta { get; set; }
    public string Denumire { get; set; }
    public string ImagineUrl { get; set; }
}

public class PoiDisplayItem
{
    public string Denumire { get; set; }
    public string Descriere { get; set; }
    public string Tip { get; set; }
    public ObservableCollection<DestinationImageItem> Images { get; set; } = new();
    public bool HasDescription => !string.IsNullOrWhiteSpace(Descriere);
    public bool HasTip => !string.IsNullOrWhiteSpace(Tip);
}

public class ReviewDisplayItem
{
    public int Id_Recenzie { get; set; }
    public string UserName { get; set; }
    public string UserProfilePhoto { get; set; }
    public string UserInitials { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedDate { get; set; }
    public string FormattedDate => CreatedDate.ToString("dd MMM yyyy");
    public string StarsDisplay => new string('★', Rating) + new string('☆', 5 - Rating);
    public bool HasComment => !string.IsNullOrWhiteSpace(Comment);
    public bool HasProfilePhoto => !string.IsNullOrWhiteSpace(UserProfilePhoto);
}

[QueryProperty(nameof(DestinationId), "destinationId")]
public partial class DestinationDetailsPage : ContentPage
{
    private readonly ObservableCollection<DestinationImageItem> _destinationImages = new();
    private readonly ObservableCollection<FacilityDisplayItem> _facilities = new();
    private readonly ObservableCollection<DestinationCategoryDisplayItem> _categories = new();
    private readonly ObservableCollection<PoiDisplayItem> _pointsOfInterest = new();
    private readonly ObservableCollection<ReviewDisplayItem> _reviews = new();
    
    // Repositories
    private readonly DestinatieRepository _destinatieRepo = new();
    private readonly ImaginiDestinatieRepository _imaginiDestRepo = new();
    private readonly FacilitateRepository _facilitateRepo = new();
    private readonly DestinatieFacilitateRepository _destFacilitateRepo = new();
    private readonly PunctDeInteresRepository _poiRepo = new();
    private readonly ImaginiPunctDeInteresRepository _imaginiPoiRepo = new();
    private readonly CategorieVacanta_DestinatieRepository _catDestRepo = new();
    private readonly CategorieVacantaRepository _categorieRepo = new();
    private readonly RecenzieRepository _recenzieRepo = new();
    private readonly UtilizatorRepository _utilizatorRepo = new();
    private readonly FavoriteRepository _favoriteRepo = new();
    
    // Cancellation support for cleanup
    private CancellationTokenSource _cancellationTokenSource = new();
    
    private int _destinationId;
    private Destinatie _currentDestination;
    private int _currentUserId;
    private bool _isFavorite;

    public int DestinationId
    {
        get => _destinationId;
        set
        {
            Debug.WriteLine($"[DestinationDetailsPage] DestinationId setter called with value: {value}");
            _destinationId = value;
            OnPropertyChanged();
        }
    }

    public DestinationDetailsPage()
    {
        InitializeComponent();
        
        // Set up data sources
        ImagesCarousel.ItemsSource = _destinationImages;
        FacilitiesCollectionView.ItemsSource = _facilities;
        CategoriesCollectionView.ItemsSource = _categories;
        PointsOfInterestCollectionView.ItemsSource = _pointsOfInterest;
        ReviewsCollectionView.ItemsSource = _reviews;
        
        // Initially hide sections until data is loaded
        FacilitiesSection.IsVisible = false;
        CategoriesSection.IsVisible = false;
        PointsOfInterestSection.IsVisible = false;
        ReviewsSection.IsVisible = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        Debug.WriteLine($"[DestinationDetailsPage] OnAppearing called with destinationId: {_destinationId}");
        
        if (_destinationId > 0)
        {
            await LoadDestinationDetailsAsync();
        }
        else
        {
            Debug.WriteLine($"[DestinationDetailsPage] Invalid destination ID: {_destinationId}");
            await DisplayAlert("Eroare", "ID destinatie invalid.", "OK");
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
        
        Debug.WriteLine($"[DestinationDetailsPage] OnDisappearing - operations cancelled");
    }

    private async Task LoadDestinationDetailsAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Starting to load details for destination ID: {_destinationId}");
            LoadingOverlay.IsVisible = true;
            Debug.WriteLine($"[DestinationDetailsPage] LoadingOverlay set to visible");
            
            // Add a short delay to ensure UI has updated
            await Task.Delay(100, _cancellationTokenSource.Token);
            
            // Load destination basic info
            await LoadDestinationInfoAsync();
            
            // Load all related data in parallel with cancellation support
            await Task.WhenAll(
                LoadDestinationImagesAsync(),
                LoadCategoriesAsync(),
                LoadFacilitiesAsync(),
                LoadPointsOfInterestAsync(),
                LoadReviewsAsync(),
                LoadFavoriteStatusAsync()
            );
            
            Debug.WriteLine($"[DestinationDetailsPage] Successfully loaded all destination details");
            
            // Force UI update
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Debug.WriteLine($"[DestinationDetailsPage] Forcing UI refresh");
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[DestinationDetailsPage] Loading cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading destination details: {ex.Message}");
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await DisplayAlert("Eroare", "Nu s-au putut incarca detaliile destinatiei.", "OK");
            }
        }
        finally
        {
            Debug.WriteLine($"[DestinationDetailsPage] Setting LoadingOverlay to hidden");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LoadingOverlay.IsVisible = false;
                Debug.WriteLine($"[DestinationDetailsPage] LoadingOverlay.IsVisible = {LoadingOverlay.IsVisible}");
            });
        }
    }

    private async Task LoadDestinationInfoAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Loading destination info for ID: {_destinationId}");
            _currentDestination = await Task.Run(() => _destinatieRepo.GetById(_destinationId), _cancellationTokenSource.Token).ConfigureAwait(false);
            
            if (_currentDestination == null)
            {
                Debug.WriteLine($"[DestinationDetailsPage] Destination with ID {_destinationId} not found in database");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Eroare", "Destinatia nu a fost gasita.", "OK");
                    await Shell.Current.GoToAsync("..");
                });
                return;
            }

            Debug.WriteLine($"[DestinationDetailsPage] Found destination: {_currentDestination.Denumire}");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                DestinationNameLabel.Text = _currentDestination.Denumire;
                LocationLabel.Text = $"{_currentDestination.Oras}, {_currentDestination.Regiune}, {_currentDestination.Tara}";
                AdultPriceLabel.Text = $"{_currentDestination.PretAdult:C}";
                ChildPriceLabel.Text = $"{_currentDestination.PretMinor:C}";
                
                // Update description
                if (!string.IsNullOrWhiteSpace(_currentDestination.Descriere))
                {
                    DescriptionLabel.Text = _currentDestination.Descriere;
                    DescriptionSection.IsVisible = true;
                    Debug.WriteLine($"[DestinationDetailsPage] Set description: {_currentDestination.Descriere.Substring(0, Math.Min(50, _currentDestination.Descriere.Length))}...");
                }
                else
                {
                    DescriptionSection.IsVisible = false;
                    Debug.WriteLine($"[DestinationDetailsPage] No description available");
                }
                
                // Update page title
                Title = _currentDestination.Denumire;
                Debug.WriteLine($"[DestinationDetailsPage] Updated UI with destination info");
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[DestinationDetailsPage] Destination info loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading destination info: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task LoadDestinationImagesAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Starting to load images for destination ID: {_destinationId}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ImagesLoadingIndicator.IsVisible = true;
                ImagesLoadingIndicator.IsRunning = true;
            });
            
            var images = await Task.Run(() => _imaginiDestRepo.GetByDestinationId(_destinationId), _cancellationTokenSource.Token).ConfigureAwait(false);
            Debug.WriteLine($"[DestinationDetailsPage] Found {images?.Count() ?? 0} images");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _destinationImages.Clear();
                
                if (images?.Any() == true)
                {
                    foreach (var image in images)
                    {
                        _destinationImages.Add(new DestinationImageItem
                        {
                            ImagineUrl = PrepareImageUrl(image.ImagineUrl)
                        });
                    }
                    
                    NoImagesLayout.IsVisible = false;
                    ImagesCarousel.IsVisible = true;
                    IndicatorView.IsVisible = _destinationImages.Count > 1;
                    Debug.WriteLine($"[DestinationDetailsPage] Added {_destinationImages.Count} images to carousel");
                }
                else
                {
                    // Add placeholder image if no images found
                    _destinationImages.Add(new DestinationImageItem
                    {
                        ImagineUrl = "https://via.placeholder.com/400x250/E0E0E0/999999?text=No+Image"
                    });
                    
                    NoImagesLayout.IsVisible = true;
                    ImagesCarousel.IsVisible = false;
                    IndicatorView.IsVisible = false;
                    Debug.WriteLine($"[DestinationDetailsPage] No images found, showing placeholder");
                }
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[DestinationDetailsPage] Images loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading destination images: {ex.Message}");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Show placeholder on error
                _destinationImages.Clear();
                _destinationImages.Add(new DestinationImageItem
                {
                    ImagineUrl = "https://via.placeholder.com/400x250/E0E0E0/999999?text=No+Image"
                });
                
                NoImagesLayout.IsVisible = true;
                ImagesCarousel.IsVisible = false;
                IndicatorView.IsVisible = false;
            });
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ImagesLoadingIndicator.IsVisible = false;
                ImagesLoadingIndicator.IsRunning = false;
                Debug.WriteLine($"[DestinationDetailsPage] Images loading completed");
            });
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Starting to load categories for destination ID: {_destinationId}");
            
            var categoryRelations = await Task.Run(() => _catDestRepo.GetByDestinationId(_destinationId), _cancellationTokenSource.Token).ConfigureAwait(false);
            Debug.WriteLine($"[DestinationDetailsPage] Found {categoryRelations?.Count() ?? 0} category relations");
            
            if (!categoryRelations?.Any() == true)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _categories.Clear();
                    CategoriesSection.IsVisible = false; // Hide section completely when no categories
                    Debug.WriteLine($"[DestinationDetailsPage] No categories found, hiding section");
                });
                return;
            }
            
            var categoryIds = categoryRelations.Select(cr => cr.Id_CategorieVacanta).Take(5).ToList();
            
            var categories = await Task.Run(() =>
            {
                return categoryIds.Select(id => _categorieRepo.GetById(id))
                                 .Where(c => c != null)
                                 .ToList();
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            
            Debug.WriteLine($"[DestinationDetailsPage] Loaded {categories.Count} categories");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _categories.Clear();
                
                if (categories.Any())
                {
                    foreach (var category in categories)
                    {
                        var categoryItem = new DestinationCategoryDisplayItem
                        {
                            Id_CategorieVacanta = category.Id_CategorieVacanta,
                            Denumire = category.Denumire,
                            ImagineUrl = PrepareImageUrl(category.ImagineUrl)
                        };
                        
                        _categories.Add(categoryItem);
                    }
                    
                    CategoriesSection.IsVisible = true;
                    Debug.WriteLine($"[DestinationDetailsPage] Added {_categories.Count} categories to UI");
                }
                else
                {
                    CategoriesSection.IsVisible = false;
                    Debug.WriteLine($"[DestinationDetailsPage] No valid categories, hiding section");
                }
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[DestinationDetailsPage] Categories loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading categories: {ex.Message}");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _categories.Clear();
                CategoriesSection.IsVisible = false; // Hide on error
            });
        }
    }

    private async Task LoadFacilitiesAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Starting to load facilities for destination ID: {_destinationId}");
            // Get destination facilities through the junction table
            var destFacilities = await Task.Run(() => _destFacilitateRepo.GetByDestinationId(_destinationId), _cancellationTokenSource.Token).ConfigureAwait(false);
            var facilityIds = destFacilities.Select(df => df.Id_Facilitate).ToList();
            Debug.WriteLine($"[DestinationDetailsPage] Found {facilityIds.Count} facility associations");
            
            if (!facilityIds.Any())
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _facilities.Clear();
                    FacilitiesSection.IsVisible = false; // Hide section completely when no facilities
                    Debug.WriteLine($"[DestinationDetailsPage] No facilities found, hiding section");
                });
                return;
            }
            
            var facilities = await Task.Run(() =>
            {
                return facilityIds.Select(id => _facilitateRepo.GetById(id))
                                 .Where(f => f != null)
                                 .ToList();
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            
            Debug.WriteLine($"[DestinationDetailsPage] Loaded {facilities.Count} facilities");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _facilities.Clear();
                
                if (facilities.Any())
                {
                    foreach (var facility in facilities)
                    {
                        _facilities.Add(new FacilityDisplayItem
                        {
                            Denumire = facility.Denumire,
                            Descriere = facility.Descriere
                        });
                    }
                    
                    FacilitiesSection.IsVisible = true;
                    Debug.WriteLine($"[DestinationDetailsPage] Added {_facilities.Count} facilities to UI");
                }
                else
                {
                    FacilitiesSection.IsVisible = false;
                    Debug.WriteLine($"[DestinationDetailsPage] No valid facilities, hiding section");
                }
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[DestinationDetailsPage] Facilities loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading facilities: {ex.Message}");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _facilities.Clear();
                FacilitiesSection.IsVisible = false; // Hide on error
            });
        }
    }

    private async Task LoadPointsOfInterestAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Starting to load POIs for destination ID: {_destinationId}");
            var pois = await Task.Run(() => _poiRepo.GetByDestinationId(_destinationId), _cancellationTokenSource.Token).ConfigureAwait(false);
            Debug.WriteLine($"[DestinationDetailsPage] Found {pois?.Count() ?? 0} POIs");
            
            if (!pois?.Any() == true)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _pointsOfInterest.Clear();
                    PointsOfInterestSection.IsVisible = false; // Hide section completely when no POIs
                    Debug.WriteLine($"[DestinationDetailsPage] No POIs found, hiding section");
                });
                return;
            }
            
            // Batch collect POIs to reduce UI thread calls
            var poiDisplayItems = new List<PoiDisplayItem>();
            
            // Load POIs with their images
            foreach (var poi in pois)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;
                    
                try
                {
                    var poiImages = await Task.Run(() => _imaginiPoiRepo.GetByPointOfInterestId(poi.Id_PunctDeInteres), _cancellationTokenSource.Token).ConfigureAwait(false);
                    Debug.WriteLine($"[DestinationDetailsPage] POI '{poi.Denumire}' has {poiImages?.Count() ?? 0} images");
                    
                    var poiDisplayItem = new PoiDisplayItem
                    {
                        Denumire = poi.Denumire ?? "Punct de interes",
                        Descriere = poi.Descriere,
                        Tip = poi.Tip
                    };
                    
                    // Add images to POI
                    if (poiImages?.Any() == true)
                    {
                        // Limit to maximum 3 images for cleaner display
                        var imagesToAdd = poiImages.Take(3);
                        foreach (var image in imagesToAdd)
                        {
                            poiDisplayItem.Images.Add(new DestinationImageItem
                            {
                                ImagineUrl = PrepareImageUrl(image.ImagineUrl)
                            });
                        }
                    }
                    else
                    {
                        // Add placeholder if no images
                        poiDisplayItem.Images.Add(new DestinationImageItem
                        {
                            ImagineUrl = "https://via.placeholder.com/100x100/E0E0E0/999999?text=No+Image"
                        });
                    }
                    
                    poiDisplayItems.Add(poiDisplayItem);
                    Debug.WriteLine($"[DestinationDetailsPage] Prepared POI '{poiDisplayItem.Denumire}' for UI");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading POI {poi.Id_PunctDeInteres}: {ex.Message}");
                    
                    // Add POI without images on error
                    var poiDisplayItem = new PoiDisplayItem
                    {
                        Denumire = poi.Denumire ?? "Punct de interes",
                        Descriere = poi.Descriere,
                        Tip = poi.Tip
                    };
                    
                    poiDisplayItem.Images.Add(new DestinationImageItem
                    {
                        ImagineUrl = "https://via.placeholder.com/100x100/E0E0E0/999999?text=No+Image"
                    });
                    
                    poiDisplayItems.Add(poiDisplayItem);
                }
            }
            
            // Batch update UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _pointsOfInterest.Clear();
                foreach (var poi in poiDisplayItems)
                {
                    _pointsOfInterest.Add(poi);
                }
                
                if (_pointsOfInterest.Any())
                {
                    PointsOfInterestSection.IsVisible = true;
                    Debug.WriteLine($"[DestinationDetailsPage] POI section visible with {_pointsOfInterest.Count} items");
                }
                else
                {
                    PointsOfInterestSection.IsVisible = false;
                    Debug.WriteLine($"[DestinationDetailsPage] No valid POIs, hiding section");
                }
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[DestinationDetailsPage] POIs loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading points of interest: {ex.Message}");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _pointsOfInterest.Clear();
                PointsOfInterestSection.IsVisible = false; // Hide on error
            });
        }
    }

    private async Task LoadReviewsAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Starting to load reviews for destination ID: {_destinationId}");
            
            var reviews = await Task.Run(() => _recenzieRepo.GetByDestinationWithDetails(_destinationId), _cancellationTokenSource.Token).ConfigureAwait(false);
            Debug.WriteLine($"[DestinationDetailsPage] Found {reviews?.Count() ?? 0} reviews");
            
            // ALWAYS show the reviews section with the "Add Review" button
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ReviewsSection.IsVisible = true; // Show section regardless of reviews
                Debug.WriteLine($"[DestinationDetailsPage] Reviews section is now visible");
            });
            
            if (!reviews?.Any() == true)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _reviews.Clear();
                    UpdateRatingDisplay(0, 0); // No rating available
                    Debug.WriteLine($"[DestinationDetailsPage] No reviews found, but section remains visible");
                });
                return;
            }
            
            // Take only last 5 reviews and calculate average rating
            var recentReviews = reviews.OrderByDescending(r => r.Data_Creare).Take(5).ToList();
            var averageRating = reviews.Average(r => r.Nota);
            var totalReviews = reviews.Count();
            
            Debug.WriteLine($"[DestinationDetailsPage] Processing {recentReviews.Count} recent reviews, average: {averageRating:F1}");
            
            var reviewDisplayItems = new List<ReviewDisplayItem>();
            
            foreach (var review in recentReviews)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;
                
                // Prepare profile photo URL
                string profilePhotoUrl = PrepareProfileImageUrl(review.Utilizator?.PozaProfil);
                
                // Generate user initials as fallback
                var userInitials = "";
                if (review.Utilizator != null)
                {
                    var firstName = review.Utilizator.Nume ?? "";
                    var lastName = review.Utilizator.Prenume ?? "";
                    userInitials = $"{(firstName.Length > 0 ? firstName[0] : '?')}{(lastName.Length > 0 ? lastName[0] : "")}".ToUpper();
                }
                
                var reviewItem = new ReviewDisplayItem
                {
                    Id_Recenzie = review.Id_Recenzie,
                    UserName = $"{review.Utilizator?.Nume} {review.Utilizator?.Prenume}".Trim(),
                    UserProfilePhoto = profilePhotoUrl,
                    UserInitials = userInitials,
                    Rating = review.Nota,
                    Comment = review.Comentariu,
                    CreatedDate = review.Data_Creare
                };
                
                reviewDisplayItems.Add(reviewItem);
                Debug.WriteLine($"[DestinationDetailsPage] Prepared review from {reviewItem.UserName} with rating {reviewItem.Rating}, photo: {reviewItem.HasProfilePhoto}");
            }
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _reviews.Clear();
                foreach (var review in reviewDisplayItems)
                {
                    _reviews.Add(review);
                }
                
                UpdateRatingDisplay(averageRating, totalReviews);
                Debug.WriteLine($"[DestinationDetailsPage] Reviews section visible with {_reviews.Count} items");
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[DestinationDetailsPage] Reviews loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading reviews: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _reviews.Clear();
                ReviewsSection.IsVisible = true; // Still show section with "Add Review" button
                UpdateRatingDisplay(0, 0);
                Debug.WriteLine($"[DestinationDetailsPage] Error occurred but reviews section remains visible");
            });
        }
    }

    private void UpdateRatingDisplay(double averageRating, int totalReviews)
    {
        if (totalReviews > 0)
        {
            AverageRatingLabel.Text = $"⭐ {averageRating:F1}";
            ReviewCountLabel.Text = $"({totalReviews} recenzi{(totalReviews == 1 ? "e" : "i")})";
            RatingOverlay.IsVisible = true;
        }
        else
        {
            RatingOverlay.IsVisible = false;
        }
    }

    private async void OnAddReviewClicked(object sender, EventArgs e)
    {
        try
        {
            if (_currentDestination == null)
            {
                await DisplayAlert("Eroare", "Informațiile destinației nu sunt disponibile.", "OK");
                return;
            }

            var destinationName = Uri.EscapeDataString(_currentDestination.Denumire);
            var location = Uri.EscapeDataString($"{_currentDestination.Oras}, {_currentDestination.Tara}");
            
            Debug.WriteLine($"[DestinationDetailsPage] Navigating to add review for destination ID: {_destinationId}");
            await Shell.Current.GoToAsync($"{nameof(AddReviewPage)}?destinationId={_destinationId}&destinationName={destinationName}&location={location}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error navigating to add review: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut deschide pagina de recenzie.", "OK");
        }
    }

    private async Task LoadFavoriteStatusAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Starting to load favorite status for destination ID: {_destinationId}");
            
            // Get current user ID
            var userIdStr = await UserSession.GetUserIdAsync();
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out _currentUserId))
            {
                Debug.WriteLine($"[DestinationDetailsPage] No valid user ID found, favorite functionality disabled");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Show button but as disabled state (gray heart)
                    FavoriteButton.Text = "🤍";
                    FavoriteButton.TextColor = Colors.Gray;
                    FavoriteButton.IsEnabled = false;
                    Debug.WriteLine($"[DestinationDetailsPage] Showing disabled favorite button (not authenticated)");
                });
                return;
            }
            
            // Check favorite status
            _isFavorite = await Task.Run(() => 
                _favoriteRepo.IsFavorite(_currentUserId, "Destinatie", _destinationId), 
                _cancellationTokenSource.Token).ConfigureAwait(false);
            
            Debug.WriteLine($"[DestinationDetailsPage] Favorite status loaded: {_isFavorite}");
            
            // Update UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FavoriteButton.IsEnabled = true;
                UpdateFavoriteButtonUI();
                Debug.WriteLine($"[DestinationDetailsPage] Favorite button enabled with status: {_isFavorite}");
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[DestinationDetailsPage] Favorite status loading cancelled");
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
                Debug.WriteLine($"[DestinationDetailsPage] Showing error favorite button");
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
                    _favoriteRepo.ToggleFavorite(_currentUserId, "Destinatie", _destinationId), 
                    _cancellationTokenSource.Token);

                // Verify the result matches our optimistic update
                if (isNowFavorite != _isFavorite)
                {
                    _isFavorite = isNowFavorite;
                    UpdateFavoriteButtonUI();
                }

                Debug.WriteLine($"[DestinationDetailsPage] Favorite toggled successfully: {_isFavorite}");

                // Optional: Show subtle success feedback
                var message = _isFavorite ? "Adăugat la favorite ♥" : "Șters din favorite";
                // You can add a toast notification here if you have one implemented
            }
            catch (Exception dbEx)
            {
                Debug.WriteLine($"[DestinationDetailsPage] Database error toggling favorite: {dbEx.Message}");
                
                // Revert optimistic update on failure
                _isFavorite = !_isFavorite;
                UpdateFavoriteButtonUI();
                
                await DisplayAlert("Eroare", "Nu s-a putut actualiza statusul de favorit. Te rog încearcă din nou.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OnFavoriteClicked: {ex.Message}");
            await DisplayAlert("Eroare", "A apărut o eroare neașteptată.", "OK");
        }
    }

    private string PrepareImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return "https://via.placeholder.com/80x80/E0E0E0/999999?text=No+Image";

        // If it's already a full URL, return as-is
        if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return imageUrl;

        // If it's a blob storage filename, construct the full URL
        if (imageUrl.Contains(".jpg") || imageUrl.Contains(".png") || imageUrl.Contains(".jpeg") || imageUrl.Contains(".webp"))
        {
            return $"https://vacantaai.blob.core.windows.net/vacantaai/{imageUrl}";
        }

        return imageUrl;
    }

    private string PrepareProfileImageUrl(string profileImagePath)
    {
        if (string.IsNullOrWhiteSpace(profileImagePath))
            return "profile_default.png";

        // If it's already a full URL, return as-is
        if (profileImagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return profileImagePath;

        // If it's a relative path, construct the full URL
        return $"https://vacantaai.blob.core.windows.net/vacantaai/{profileImagePath}";
    }

}