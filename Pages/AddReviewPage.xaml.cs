using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

[QueryProperty(nameof(DestinationId), "destinationId")]
[QueryProperty(nameof(DestinationName), "destinationName")]
[QueryProperty(nameof(Location), "location")]
public partial class AddReviewPage : ContentPage
{
    private readonly RecenzieRepository _recenzieRepo = new();
    private readonly DestinatieRepository _destinatieRepo = new();
    
    private int _destinationId;
    private int _selectedRating = 0;
    private int _currentUserId;
    
    public int DestinationId
    {
        get => _destinationId;
        set => _destinationId = value;
    }
    
    public string DestinationName { get; set; }
    public string Location { get; set; }

    public AddReviewPage()
    {
        InitializeComponent();
        CreateStarRating();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Get current user
            var userIdStr = await UserSession.GetUserIdAsync();
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out _currentUserId))
            {
                await DisplayAlert("Eroare", "Trebuie să fii autentificat pentru a scrie o recenzie.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Check if user already has a review for this destination
            var existingReviews = _recenzieRepo.GetByDestinationId(_destinationId);
            var userHasReview = existingReviews.Any(r => r.Id_Utilizator == _currentUserId);
            
            if (userHasReview)
            {
                await DisplayAlert("Informație", "Ai deja o recenzie pentru această destinație. Poți avea doar o recenzie per destinație.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Set destination info
            DestinationNameLabel.Text = DestinationName ?? "Destinație necunoscută";
            LocationLabel.Text = Location ?? "Locație necunoscută";
            
            Debug.WriteLine($"[AddReviewPage] Initialized for destination ID: {_destinationId}, User ID: {_currentUserId}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AddReviewPage] Error initializing: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut inițializa pagina de recenzie.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private void CreateStarRating()
    {
        StarsContainer.Children.Clear();
        
        for (int i = 1; i <= 5; i++)
        {
            var star = new Label
            {
                Text = "☆",
                FontSize = 40,
                TextColor = Color.FromArgb("#CCCCCC"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            
            // Add tap gesture for each star
            var tapGesture = new TapGestureRecognizer();
            var starValue = i; // Capture the star value
            tapGesture.Tapped += (s, e) => OnStarTapped(starValue);
            star.GestureRecognizers.Add(tapGesture);
            
            StarsContainer.Children.Add(star);
        }
    }

    private void OnStarTapped(int rating)
    {
        _selectedRating = rating;
        UpdateStarDisplay();
        UpdateSubmitButtonState();
        
        // Update rating label
        var ratingTexts = new[] { "", "⭐ Foarte Slabă", "⭐⭐ Slabă", "⭐⭐⭐ Acceptabilă", "⭐⭐⭐⭐ Bună", "⭐⭐⭐⭐⭐ Excelentă" };
        RatingLabel.Text = ratingTexts[rating];
        RatingLabel.TextColor = Color.FromArgb("#4CAF50");
        
        Debug.WriteLine($"[AddReviewPage] User selected rating: {rating}");
    }

    private void UpdateStarDisplay()
    {
        for (int i = 0; i < StarsContainer.Children.Count; i++)
        {
            if (StarsContainer.Children[i] is Label star)
            {
                if (i < _selectedRating)
                {
                    star.Text = "★";
                    star.TextColor = Color.FromArgb("#FFD700"); // Gold color
                }
                else
                {
                    star.Text = "☆";
                    star.TextColor = Color.FromArgb("#CCCCCC"); // Gray color
                }
            }
        }
    }

    private void OnCommentTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue ?? "";
        var charCount = text.Length;
        
        CharacterCountLabel.Text = $"{charCount}/250 caractere";
        
        // Change color when approaching limit
        if (charCount > 200)
        {
            CharacterCountLabel.TextColor = Color.FromArgb("#F44336"); // Red
        }
        else if (charCount > 150)
        {
            CharacterCountLabel.TextColor = Color.FromArgb("#FF9800"); // Orange
        }
        else
        {
            CharacterCountLabel.TextColor = Color.FromArgb("#888888"); // Gray
        }
        
        UpdateSubmitButtonState();
    }

    private void UpdateSubmitButtonState()
    {
        // Enable submit button only if rating is selected
        var canSubmit = _selectedRating > 0;
        SubmitButton.IsEnabled = canSubmit;
        SubmitButton.Opacity = canSubmit ? 1.0 : 0.6;
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        if (_selectedRating == 0)
        {
            await DisplayAlert("Eroare", "Te rog să selectezi o notă înainte de a publica recenzia.", "OK");
            return;
        }

        try
        {
            LoadingOverlay.IsVisible = true;
            
            var newReview = new Recenzie
            {
                Nota = _selectedRating,
                Comentariu = CommentEditor.Text?.Trim(),
                Data_Creare = DateTime.Now,
                Id_Destinatie = _destinationId,
                Id_Utilizator = _currentUserId
            };

            Debug.WriteLine($"[AddReviewPage] Creating review: Rating={_selectedRating}, Comment='{newReview.Comentariu}', DestinationId={_destinationId}, UserId={_currentUserId}");

            _recenzieRepo.Insert(newReview);
            
            await DisplayAlert("Succes", "Recenzia ta a fost publicată cu succes! 🎉", "Grozav!");
            
            Debug.WriteLine($"[AddReviewPage] Review saved successfully with ID: {newReview.Id_Recenzie}");
            
            // Navigate back to destination details
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AddReviewPage] Error saving review: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut salva recenzia. Te rog încearcă din nou.", "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Check if user has started writing
        bool hasContent = _selectedRating > 0 || !string.IsNullOrWhiteSpace(CommentEditor.Text);
        
        if (hasContent)
        {
            bool shouldDiscard = await DisplayAlert(
                "Confirmare", 
                "Ești sigur că vrei să anulezi? Recenzia nu va fi salvată.", 
                "Da, anulează", 
                "Continuă editarea");
            
            if (!shouldDiscard)
                return;
        }
        
        await Shell.Current.GoToAsync("..");
    }
}