using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Collections.ObjectModel;
using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class SugestiiPage : ContentPage
{
    private readonly SugestieRepository _sugestieRepo = new SugestieRepository();
    private readonly UtilizatorRepository _utilizatorRepo = new UtilizatorRepository();
    private int _userId;
    private ObservableCollection<Sugestie> _items = new ObservableCollection<Sugestie>();

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
                _items.Add(s);
            }

            UpdateEmptyState();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Nu s-au putut incarca sugestiile: {ex.Message}", "OK");
        }
        finally
        {
            SetLoadingState(false);
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
            var sugestie = frame?.BindingContext as Sugestie;
            
            if (sugestie != null)
            {
                // Visual feedback
                await frame.ScaleTo(0.95, 100);
                await frame.ScaleTo(1.0, 100);
                
                // Show suggestion details in alert
                var message = $"📍 Destinație: {sugestie.Destinatie?.Denumire ?? "Necunoscută"}\n" +
                             $"💰 Buget: {sugestie.Buget_Estimat:N0} €\n" +
                             $"📅 Creată: {sugestie.Data_Inregistrare:dd/MM/yyyy}\n" +
                             $"🔒 Status: {(sugestie.EstePublic == 1 ? "Publică" : "Privată")}\n\n" +
                             $"📝 Descriere:\n{sugestie.Descriere}";

                await DisplayAlert(
                    $"✈️ {sugestie.Titlu}", 
                    message, 
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Nu s-au putut afișa detaliile sugestiei: {ex.Message}", "OK");
        }
    }

    private async void OnAddSugestie(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NewSugestiePage));
    }
}