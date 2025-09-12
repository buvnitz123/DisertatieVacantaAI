using MauiAppDisertatieVacantaAI.Classes.Library;
using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class SplashPage : ContentPage
{
    // Friendly vacation tips / fun facts displayed during loading (no diacritics)
    private static readonly string[] FunMessages = new[]
    {
        "Stiati ca cel mai scurt zbor comercial dureaza aproximativ 90 de secunde (Westray-Papa Westray, Scotia)?",
        "Fun fact: In Japonia exista hoteluri-capsula, perfecte pentru o noapte scurta intre doua zboruri.",
        "Stiati ca exista o plaja cu nisip roz in Bahamas (Harbour Island)?",
        "Pont: cautati zboruri marti sau miercuri pentru tarife mai bune (nu e regula, dar adesea ajuta).",
        "Stiati ca Islanda are mai multe izvoare termale naturale decat aproape orice alta tara?",
        "Fun fact: Venetia are peste 400 de poduri si aproximativ 150 de canale.",
        "Pont: impachetati pe straturi - e mai usor sa va adaptati la schimbarile de temperatura.",
        "Stiati ca in Finlanda exista saune publice aproape in fiecare cartier?",
        "Fun fact: cel mai vechi hotel din lume (Nishiyama Onsen Keiunkan, Japonia) functioneaza din anul 705!",
        "Pont: folositi o mini-trusa cu cabluri/incarcatoare pentru a evita incurcatura in bagaj.",
        "Stiati ca Parisul are peste 1.700 de patiserii? O croissant pe zi nu se pune la dieta de vacanta :)",
        "Fun fact: in Australia, Marele Recif de Corali este vizibil chiar si din spatiu.",
        "Pont: cartea de imbarcare salvata offline te poate scuti de emotii la gate.",
        "Stiati ca in Portugalia poti gasi sate intregi din piatra, perfect conservate pentru plimbari?",
        "Fun fact: cele mai lungi plaje neintrerupte depasesc 200 km (ex: Praia do Cassino, Brazilia)."
    };

    private static string GetRandomFunMessage() => FunMessages.Length == 0 ? string.Empty : FunMessages[Random.Shared.Next(FunMessages.Length)];

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PlayIntroAnimationsParallelAsync();
        StartValidation();
    }
    private async Task PlayIntroAnimationsParallelAsync()
    {
        try
        {
            await Task.Delay(2000);

            var tasks = new List<Task>(6);
            if (TitleLabel is not null) tasks.Add(TitleLabel.FadeTo(1.0, 1100, Easing.CubicInOut));
            if (LogoImage is not null) tasks.Add(LogoImage.FadeTo(1.0, 3100, Easing.CubicInOut));
            if (StatusLabel is not null) tasks.Add(StatusLabel.FadeTo(0.8, 1100, Easing.CubicInOut));
            if (ProgressBar is not null) tasks.Add(ProgressBar.FadeTo(1.0, 1100, Easing.CubicInOut));
            if (ProgressLabel is not null) tasks.Add(ProgressLabel.FadeTo(0.7, 1100, Easing.CubicInOut));
            if (LoadingIndicator is not null) tasks.Add(LoadingIndicator.FadeTo(1.0, 1100, Easing.CubicInOut));

            await Task.WhenAll(tasks);
        }
        catch { }
    }

    private async void StartValidation()
    {
        await Task.Delay(150);
        
        try
        {
            StatusLabel.Text = GetRandomFunMessage();
            await SmoothAdvanceTo(0.25, 700);
            var warmUpTask = Task.Run(() =>
            {
                var repo = new Classes.Database.Repositories.UtilizatorRepository();
                repo.Initialize();
            });
            await AnimateProgressWhile(warmUpTask, 0.55, 45, 0.004);
            await SmoothAdvanceTo(0.75, 600);
            await SmoothAdvanceTo(1.0, 800);
            LoadingIndicator.IsRunning = false;
            await Task.Delay(200);
            await NavigateToNextPage();
        }
        catch (Exception ex)
        {
            ShowError($"Initialization failed: {ex.Message}");
        }
    }
    private async Task SmoothAdvanceTo(double target, int durationMs)
    {
        target = Math.Clamp(target, 0, 1);
        double start = ProgressBar.Progress;
        int steps = Math.Max(1, durationMs / 16);
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            double value = start + (target - start) * t;
            ProgressBar.Progress = value;
            ProgressLabel.Text = $"{Math.Round(value * 100)}%";
            await Task.Delay(16);
        }
        ProgressBar.Progress = target;
        ProgressLabel.Text = $"{Math.Round(target * 100)}%";
    }
    private async Task AnimateProgressWhile(Task workTask, double target, int tickMs = 45, double deltaPerTick = 0.004)
    {
        target = Math.Clamp(target, 0, 1);
        while (!workTask.IsCompleted)
        {
            double p = ProgressBar.Progress + deltaPerTick;
            if (p > target) p = target;
            ProgressBar.Progress = p;
            ProgressLabel.Text = $"{Math.Round(p * 100)}%";
            if (p >= target) break;
            try { await Task.WhenAny(workTask, Task.Delay(tickMs)); } catch { /* ignore */ }
        }
        await SmoothAdvanceTo(target, 300);
        try { await workTask; } catch { /* errors handled by caller */ }
    }

    private async Task NavigateToNextPage()
    {
        try
        {
            // Check if user has an active in-memory session first (current user)
            var currentUserId = UserSession.CurrentUserId;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                // User has active in-memory session, validate and proceed
                try
                {
                    var user = await UserSession.GetUserFromSessionAsync();
                    if (user != null && user.EsteActiv == 1)
                    {
                        await Shell.Current.GoToAsync("//MainPage");
                        return;
                    }
                    else
                    {
                        // Invalid user, clear session
                        UserSession.ClearSession();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Current user validation error: {ex.Message}");
                    UserSession.ClearSession();
                }
            }

            // No active in-memory session, check persistent storage (Remember me)
            bool isLoggedIn = await UserSession.IsLoggedInAsync();
            if (isLoggedIn)
            {
                try
                {
                    var user = await UserSession.GetUserFromSessionAsync();
                    
                    if (user != null && user.EsteActiv == 1)
                    {
                        // Valid remembered user, restore current session
                        UserSession.SetCurrentUser(
                            user.Id_Utilizator.ToString(),
                            user.Email,
                            user.Nume,
                            user.Prenume,
                            persist: false // Don't overwrite persistent storage
                        );
                        await Shell.Current.GoToAsync("//MainPage");
                        return;
                    }
                    else
                    {
                        // Invalid remembered user, clear all session data
                        UserSession.ClearSession();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Remembered user validation error: {ex.Message}");
                    UserSession.ClearSession();
                }
            }
            
            // No valid session found, go to login
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            ShowError($"Navigation error: {ex.Message}");
        }
    }

    private void ShowError(string errorMessage)
    {
        LoadingIndicator.IsRunning = false;
        ProgressBar.Progress = 0;
        ProgressLabel.Text = "Error";
        
        StatusLabel.Text = "Initialization failed";
        ErrorLabel.Text = errorMessage;
        ErrorFrame.IsVisible = true;
        RetryButton.IsVisible = true;
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        ErrorFrame.IsVisible = false;
        RetryButton.IsVisible = false;
        LoadingIndicator.IsRunning = true;
        ProgressBar.Progress = 0;
        ProgressLabel.Text = "0%";
        
        StartValidation();
        _ = PlayIntroAnimationsParallelAsync();
    }
}
