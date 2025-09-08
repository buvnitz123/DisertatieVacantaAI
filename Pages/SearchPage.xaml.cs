using System.Collections.ObjectModel;

namespace MauiAppDisertatieVacantaAI.Pages;

public class SearchItem
{
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string Type { get; set; } // Destinatie / Sugestie
}

public partial class SearchPage : ContentPage
{
    private readonly ObservableCollection<SearchItem> _results = new();
    private readonly List<SearchItem> _all = new();
    private string _activeFilter = "All";

    public SearchPage()
    {
        InitializeComponent();
        ResultsView.ItemsSource = _results;
        SeedDummy();
    }

    private void SeedDummy()
    {
        _all.AddRange(new[]
        {
            new SearchItem{ Title="Paris", Subtitle="Destinatie - Franta", Type="Destinatii"},
            new SearchItem{ Title="Santorini Escape", Subtitle="Sugestie AI", Type="Sugestii"},
            new SearchItem{ Title="Viena", Subtitle="Destinatie - Austria", Type="Destinatii"},
            new SearchItem{ Title="Weekend la munte", Subtitle="Sugestie manuala", Type="Sugestii"},
        });
    }

    private void OnFilter(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            _activeFilter = btn.Text switch
            {
                "Destinatii" => "Destinatii",
                "Sugestii" => "Sugestii",
                _ => "All"
            };
            UpdateFilterButtons();
            ExecuteSearch(SearchEntry.Text);
        }
    }

    private void UpdateFilterButtons()
    {
        AllFilter.BackgroundColor = _activeFilter == "All" ? (Color)Application.Current.Resources["PrimaryBlue"] : (Color)Application.Current.Resources["MediumGray"];
        DestFilter.BackgroundColor = _activeFilter == "Destinatii" ? (Color)Application.Current.Resources["PrimaryBlue"] : (Color)Application.Current.Resources["MediumGray"];
        SugFilter.BackgroundColor = _activeFilter == "Sugestii" ? (Color)Application.Current.Resources["PrimaryBlue"] : (Color)Application.Current.Resources["MediumGray"];
    }

    private void OnSearch(object sender, EventArgs e)
    {
        ExecuteSearch(SearchEntry.Text);
    }

    private void ExecuteSearch(string query)
    {
        _results.Clear();
        if (string.IsNullOrWhiteSpace(query)) return;
        var q = query.Trim().ToLowerInvariant();
        var filtered = _all.Where(i =>
            ( _activeFilter == "All" || i.Type == _activeFilter ) &&
            (i.Title.ToLowerInvariant().Contains(q) || i.Subtitle.ToLowerInvariant().Contains(q))
        );
        foreach (var item in filtered)
            _results.Add(item);
    }
}