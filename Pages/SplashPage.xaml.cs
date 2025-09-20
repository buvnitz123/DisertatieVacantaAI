using MauiAppDisertatieVacantaAI.Classes.Library;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class SplashPage : ContentPage
{
    // Enhanced travel tips with more engaging content (no diacritics)
    private static readonly string[] FunMessages = new[]
    {
        "Stiati ca cel mai scurt zbor comercial dureaza doar 90 de secunde? Este intre doua insule din Scotia!",
        "In Japonia exista hoteluri-capsula - perfecte pentru o noapte scurta si o experienta unica!",
        "Plaja cu nisip roz din Bahamas (Harbour Island) isi ia culoarea de la cochiliile de corali!",
        "Cautati zboruri marti sau miercuri - de obicei sunt mai ieftine decat in weekend!",
        "Islanda are mai multe izvoare termale naturale decat orice alta tara europeana!",
        "Venetia are peste 400 de poduri care conecteaza 118 mici insule!",
        "Impachetati pe straturi - este cheia pentru a va adapta usor la orice vreme!",
        "In Finlanda, saunele publice sunt o traditie - gasiti una in aproape fiecare cartier!",
        "Cel mai vechi hotel din lume (din Japonia) functioneaza din anul 705 - peste 1300 de ani!",
        "O mini-trusa cu cabluri si incarcatoare va poate salva de mult stres in calatorie!",
        "Parisul are peste 1.700 de patiserii - o croissant pe zi este aproape obligatoriu!",
        "Marele Recif de Corali din Australia este atat de mare ca poate fi vazut din spatiu!",
        "Cartea de imbarcare salvata offline va poate scuti de emotii la poarta de imbarcare!",
        "In Portugalia gasiti sate intregi din piatra, perfect conservate - ca intr-o poveste!",
        "Cele mai lungi plaje neintrerupte depasesc 200 km - perfecte pentru plimbari lungi!",
        "Santorini isi datoreaza culorile albastre si albe traditiei de a vopsi casele pentru dezinfectie!",
        "In Norvegia, soarele de miezul noptii straluceste 24/7 vara - perfect pentru aventurieri!",
        "Machu Picchu a fost ascuns de spaniolii conquistadori si redescoperit abia in 1911!",
        "In Bali, ritul traditiei 'Nyepi' inseamna o zi de liniste completa - fara zgomot sau lumini!",
        "Dubai are cel mai inalt hotel din lume - Burj Al Arab, construit pe o insula artificiala!"
    };

    private static readonly string[] LoadingSteps = new[]
    {
        "Se initiaza aplicatia...",
        "Se conecteaza la servicii...",
        "Se pregatesc datele...",
        "Aproape gata..."
    };

    private static string GetRandomFunMessage() => FunMessages.Length == 0 ? string.Empty : FunMessages[Random.Shared.Next(FunMessages.Length)];

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PlayIntroAnimationsAsync();
        _ = Task.Delay(500).ContinueWith(_ => StartValidation());
    }

    private async Task PlayIntroAnimationsAsync()
    {
        try
        {
            // Phase 1: Title and subtitle appear
            await Task.WhenAll(
                TitleLabel.FadeTo(1.0, 800, Easing.CubicOut),
                SubtitleLabel.FadeTo(0.8, 1000, Easing.CubicOut)
            );

            await Task.Delay(300);

            // Phase 2: Logo background and main logo
            await Task.WhenAll(
                LogoBackground.FadeTo(0.1, 600, Easing.CubicOut),
                LogoImage.FadeTo(1.0, 800, Easing.CubicOut),
                LogoImage.ScaleTo(1.1, 600, Easing.BounceOut).ContinueWith(_ => 
                    LogoImage.ScaleTo(1.0, 400, Easing.CubicIn))
            );

            await Task.Delay(200);

            // Phase 3: Travel icons appear with staggered animation
            var iconTasks = new List<Task>
            {
                AnimateIconAsync(Icon1, 0),
                AnimateIconAsync(Icon2, 150),
                AnimateIconAsync(Icon3, 300),
                AnimateIconAsync(Icon4, 450)
            };

            await Task.WhenAll(iconTasks);

            // Phase 4: Status frame and progress section
            await Task.WhenAll(
                StatusFrame.FadeTo(1.0, 600, Easing.CubicOut),
                ProgressSection.FadeTo(1.0, 600, Easing.CubicOut),
                VersionLabel.FadeTo(0.6, 800, Easing.CubicOut)
            );

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
        }
    }

    private async Task AnimateIconAsync(Label icon, int delay)
    {
        if (icon == null) return;
        
        await Task.Delay(delay);
        await Task.WhenAll(
            icon.FadeTo(0.7, 400, Easing.CubicOut),
            icon.ScaleTo(1.2, 200, Easing.CubicOut).ContinueWith(_ =>
                icon.ScaleTo(1.0, 200, Easing.CubicIn))
        );
    }

    private async void StartValidation()
    {
        await Task.Delay(200);
        
        try
        {
            // Show initial tip on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusLabel.Text = GetRandomFunMessage();
            });
            
            // Step 1: Initialize - Quick config check
            UpdateProgressStep(0, "Se verifica configuratia...");
            await SmoothAdvanceTo(0.25, 400);
            
            // Quick configuration test
            bool configOk = false;
            try
            {
                configOk = await Task.Run(async () =>
                {
                    try
                    {
                        return await EncryptionUtils.TestConfigurationAsync();
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            catch
            {
                configOk = false;
            }

            if (!configOk)
            {
                // Configuration failed - proceed in offline mode
                UpdateProgressStep(1, "Mod offline - se sare configuratia...");
                await SmoothAdvanceTo(0.90, 800);
                await Task.Delay(500);
                await SmoothAdvanceTo(1.0, 200);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LoadingIndicator.IsRunning = false;
                });
                
                await NavigateToNextPageOffline();
                return;
            }
            
            // Step 2: Try database initialization (optional)
            UpdateProgressStep(1, "Se conecteaza la baza de date...");
            await SmoothAdvanceTo(0.50, 500);
            
            bool dbOk = false;
            try
            {
                dbOk = await Task.Run(() =>
                {
                    try
                    {
                        var repo = new Classes.Database.Repositories.UtilizatorRepository();
                        repo.Initialize();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Database init failed (non-critical): {ex.Message}");
                        return false;
                    }
                });
            }
            catch
            {
                dbOk = false;
            }

            // Step 3: Finalize
            if (dbOk)
            {
                UpdateProgressStep(2, "Toate serviciile sunt gata!");
                await SmoothAdvanceTo(0.90, 400);
            }
            else
            {
                UpdateProgressStep(2, "Mod limitat - unele functii nu sunt disponibile");
                await SmoothAdvanceTo(0.90, 400);
            }
            
            // Step 4: Complete
            UpdateProgressStep(3, "Aplicatia este pregatita!");
            await SmoothAdvanceTo(1.0, 300);
            
            // Success
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LoadingIndicator.IsRunning = false;
            });
            
            await Task.Delay(300);
            
            // Navigate based on availability
            if (dbOk)
            {
                await NavigateToNextPage();
            }
            else
            {
                await NavigateToNextPageOffline();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartValidation error: {ex}");
            
            // Show error on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ShowError("Aplicatia are unele probleme de configurare, dar poti continua in mod limitat.");
            });
        }
    }

    private async Task NavigateToNextPageOffline()
    {
        // In offline mode, skip session checks and go directly to login
        try
        {
            System.Diagnostics.Debug.WriteLine("Navigating in offline mode");
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Offline navigation error: {ex.Message}");
            ShowError($"Eroare de navigare: {ex.Message}");
        }
    }

    private void UpdateProgressStep(int stepIndex, string description)
    {
        // Ensure this runs on main thread since it updates UI
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ProgressDescriptionLabel.Text = description;
            
            // Update step indicators
            var steps = new[] { Step1, Step2, Step3, Step4 };
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] != null)
                {
                    var color = i <= stepIndex ? 
                        Color.FromArgb("#0092ca") : // PrimaryBlue
                        Color.FromArgb("#EEEEEE");  // LightGray
                    steps[i].BackgroundColor = color;
                }
            }
        });
    }

    private async Task SmoothAdvanceTo(double target, int durationMs)
    {
        target = Math.Clamp(target, 0, 1);
        double start = ProgressBar.Progress;
        int steps = Math.Max(1, durationMs / 16);
        
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            // Use easing for smoother animation
            double easedT = Easing.CubicOut.Ease(t);
            double value = start + (target - start) * easedT;
            
            // Ensure UI updates happen on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ProgressBar.Progress = value;
                ProgressLabel.Text = $"{Math.Round(value * 100)}%";
            });
            
            await Task.Delay(16);
        }
        
        // Final update on main thread
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ProgressBar.Progress = target;
            ProgressLabel.Text = $"{Math.Round(target * 100)}%";
        });
    }

    private async Task AnimateProgressWhile(Task workTask, double target, int tickMs = 50, double deltaPerTick = 0.003)
    {
        target = Math.Clamp(target, 0, 1);
        
        while (!workTask.IsCompleted)
        {
            double p = ProgressBar.Progress + deltaPerTick;
            if (p > target) p = target;
            
            // Update UI on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ProgressBar.Progress = p;
                ProgressLabel.Text = $"{Math.Round(p * 100)}%";
            });
            
            if (p >= target) break;
            
            try 
            { 
                await Task.WhenAny(workTask, Task.Delay(tickMs)); 
            } 
            catch 
            { 
                /* ignore timing issues */ 
            }
        }
        
        await SmoothAdvanceTo(target, 300);
        
        try 
        { 
            await workTask; 
        } 
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Background task error: {ex.Message}");
        }
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
            ShowError($"Eroare de navigare: {ex.Message}");
        }
    }

    private async void ShowError(string errorMessage)
    {
        LoadingIndicator.IsRunning = false;
        ProgressBar.Progress = 0;
        ProgressLabel.Text = "Atentie";
        ProgressDescriptionLabel.Text = "Am intampinat o problema...";

        StatusLabel.Text = "Problema de configurare";
        ErrorLabel.Text = errorMessage;

        // Animate error appearance
        ErrorFrame.IsVisible = true;
        ErrorFrame.Opacity = 0;
        await ErrorFrame.FadeTo(1.0, 400, Easing.CubicOut);

        // Automatically show bypass option after 2 seconds
        _ = Task.Delay(2000).ContinueWith(async _ =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (ErrorFrame.IsVisible)
                {
                    await AddBypassOption();
                }
            });
        });
    }

    private async Task AddBypassOption()
    {
        try
        {
            // Add a bypass button to the error frame
            if (RetryButton.Parent is VerticalStackLayout parent)
            {
                // Check if bypass button already exists
                var existingBypass = parent.Children.OfType<Button>()
                    .FirstOrDefault(b => b.Text.Contains("Continua"));
                
                if (existingBypass == null)
                {
                    var bypassButton = new Button
                    {
                        Text = "? Continua oricum",
                        BackgroundColor = Color.FromArgb("#4CAF50"), // Green
                        TextColor = Colors.White,
                        CornerRadius = 12,
                        HeightRequest = 45,
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        Margin = new Thickness(0, 8, 0, 0)
                    };

                    bypassButton.Clicked += async (s, e) =>
                    {
                        try
                        {
                            // Hide error and go directly to login
                            await ErrorFrame.FadeTo(0, 300);
                            ErrorFrame.IsVisible = false;
                            await Shell.Current.GoToAsync("//LoginPage");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Bypass navigation error: {ex.Message}");
                        }
                    };

                    parent.Add(bypassButton);

                    // Animate the new button
                    bypassButton.Opacity = 0;
                    await bypassButton.FadeTo(1.0, 400, Easing.CubicOut);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddBypassOption error: {ex.Message}");
        }
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        try
        {
            // Hide error with animation
            await ErrorFrame.FadeTo(0, 300);
            ErrorFrame.IsVisible = false;
            
            // Reset UI state
            LoadingIndicator.IsRunning = true;
            ProgressBar.Progress = 0;
            ProgressLabel.Text = "0%";
            ProgressDescriptionLabel.Text = "Se reinitiaza...";
            
            // Reset all step indicators
            UpdateProgressStep(-1, "Se reinitiaza...");
            
            // Show new tip
            StatusLabel.Text = GetRandomFunMessage();
            
            // Clear cached configuration and force fresh load
            try
            {
                EncryptionUtils.ClearCache();
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache clearing error (non-critical): {ex.Message}");
            }
            
            // Add a delay before restarting
            await Task.Delay(800);
            
            // Restart the validation process
            StartValidation();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnRetryClicked error: {ex.Message}");
            
            // If retry fails, show bypass option immediately
            ShowError("Problema persista. Te rog foloseste optiunea de bypass.");
        }
    }
}
