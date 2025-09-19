using System.Collections.ObjectModel;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using System.Diagnostics;

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

public class PoiDisplayItem
{
    public string Denumire { get; set; }
    public string Descriere { get; set; }
    public string Tip { get; set; }
    public ObservableCollection<DestinationImageItem> Images { get; set; } = new();
    public bool HasDescription => !string.IsNullOrWhiteSpace(Descriere);
    public bool HasTip => !string.IsNullOrWhiteSpace(Tip);
}

[QueryProperty(nameof(DestinationId), "destinationId")]
public partial class DestinationDetailsPage : ContentPage
{
    private readonly ObservableCollection<DestinationImageItem> _destinationImages = new();
    private readonly ObservableCollection<FacilityDisplayItem> _facilities = new();
    private readonly ObservableCollection<PoiDisplayItem> _pointsOfInterest = new();
    
    // Repositories
    private readonly DestinatieRepository _destinatieRepo = new();
    private readonly ImaginiDestinatieRepository _imaginiDestRepo = new();
    private readonly FacilitateRepository _facilitateRepo = new();
    private readonly DestinatieFacilitateRepository _destFacilitateRepo = new();
    private readonly PunctDeInteresRepository _poiRepo = new();
    private readonly ImaginiPunctDeInteresRepository _imaginiPoiRepo = new();
    
    private int _destinationId;
    private Destinatie _currentDestination;

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
        PointsOfInterestCollectionView.ItemsSource = _pointsOfInterest;
        
        // Initially hide sections until data is loaded
        FacilitiesSection.IsVisible = false;
        PointsOfInterestSection.IsVisible = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        Debug.WriteLine($"[DestinationDetailsPage] OnAppearing called with destinationId: {_destinationId}");
        
        // TEMP: For testing - use first destination ID if none provided
        if (_destinationId <= 0)
        {
            Debug.WriteLine($"[DestinationDetailsPage] No valid destination ID provided, trying to get first destination for testing");
            try
            {
                var firstDestination = _destinatieRepo.GetAll().FirstOrDefault();
                if (firstDestination != null)
                {
                    _destinationId = firstDestination.Id_Destinatie;
                    Debug.WriteLine($"[DestinationDetailsPage] Using first destination ID for testing: {_destinationId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DestinationDetailsPage] Error getting first destination: {ex.Message}");
            }
        }
        
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

    private async Task LoadDestinationDetailsAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Starting to load details for destination ID: {_destinationId}");
            LoadingOverlay.IsVisible = true;
            Debug.WriteLine($"[DestinationDetailsPage] LoadingOverlay set to visible");
            
            // Add a short delay to ensure UI has updated
            await Task.Delay(100);
            
            // Load destination basic info
            await LoadDestinationInfoAsync();
            
            // Load all related data in parallel
            await Task.WhenAll(
                LoadDestinationImagesAsync(),
                LoadFacilitiesAsync(),
                LoadPointsOfInterestAsync()
            );
            
            Debug.WriteLine($"[DestinationDetailsPage] Successfully loaded all destination details");
            
            // Force UI update
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Debug.WriteLine($"[DestinationDetailsPage] Forcing UI refresh");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading destination details: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-au putut incarca detaliile destinatiei.", "OK");
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
            _currentDestination = await Task.Run(() => _destinatieRepo.GetById(_destinationId));
            
            if (_currentDestination == null)
            {
                Debug.WriteLine($"[DestinationDetailsPage] Destination with ID {_destinationId} not found in database");
                await DisplayAlert("Eroare", "Destinatia nu a fost gasita.", "OK");
                await Shell.Current.GoToAsync("..");
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
            ImagesLoadingIndicator.IsVisible = true;
            ImagesLoadingIndicator.IsRunning = true;
            
            var images = await Task.Run(() => _imaginiDestRepo.GetByDestinationId(_destinationId));
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
                            ImagineUrl = image.ImagineUrl ?? "placeholder_image.png"
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
                        ImagineUrl = "placeholder_image.png"
                    });
                    
                    NoImagesLayout.IsVisible = true;
                    ImagesCarousel.IsVisible = false;
                    IndicatorView.IsVisible = false;
                    Debug.WriteLine($"[DestinationDetailsPage] No images found, showing placeholder");
                }
            });
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
                    ImagineUrl = "placeholder_image.png"
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

    private async Task LoadFacilitiesAsync()
    {
        try
        {
            Debug.WriteLine($"[DestinationDetailsPage] Starting to load facilities for destination ID: {_destinationId}");
            // Get destination facilities through the junction table
            var destFacilities = await Task.Run(() => _destFacilitateRepo.GetByDestinationId(_destinationId));
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
            });
            
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
            var pois = await Task.Run(() => _poiRepo.GetByDestinationId(_destinationId));
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
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _pointsOfInterest.Clear();
            });
            
            // Load POIs with their images
            foreach (var poi in pois)
            {
                try
                {
                    var poiImages = await Task.Run(() => _imaginiPoiRepo.GetByPointOfInterestId(poi.Id_PunctDeInteres));
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
                                ImagineUrl = image.ImagineUrl ?? "placeholder_image.png"
                            });
                        }
                    }
                    else
                    {
                        // Add placeholder if no images
                        poiDisplayItem.Images.Add(new DestinationImageItem
                        {
                            ImagineUrl = "placeholder_image.png"
                        });
                    }
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _pointsOfInterest.Add(poiDisplayItem);
                        Debug.WriteLine($"[DestinationDetailsPage] Added POI '{poiDisplayItem.Denumire}' to UI");
                    });
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
                        ImagineUrl = "placeholder_image.png"
                    });
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _pointsOfInterest.Add(poiDisplayItem);
                    });
                }
            }
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
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
}