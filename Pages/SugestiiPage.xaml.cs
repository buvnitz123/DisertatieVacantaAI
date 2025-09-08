using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Collections.ObjectModel;

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
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            _items.Clear();

            var idStr = await SecureStorage.GetAsync("UserId");
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out _userId))
            {
                return; // not logged
            }

            var suggestions = _sugestieRepo.GetByUser(_userId);
            foreach (var s in suggestions)
            {
                _items.Add(s);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Nu s-au putut incarca sugestiile: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }
}