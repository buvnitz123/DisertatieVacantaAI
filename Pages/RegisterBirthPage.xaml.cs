using MauiAppDisertatieVacantaAI.Classes.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

public partial class RegisterBirthPage : ContentPage
{
    public RegisterBirthPage()
    {
        InitializeComponent();
        
        // Set initial date
        if (RegistrationSession.Draft?.DataNastere != null)
        {
            BirthDatePicker.Date = RegistrationSession.Draft.DataNastere.Value;
        }
        else
        {
            // Default to a reasonable date (25 years old)
            BirthDatePicker.Date = DateTime.Today.AddYears(-25);
        }
        
        UpdateAgeDisplay();
    }

    private void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        DateFrame.BorderColor = Colors.Transparent;
        UpdateAgeDisplay();
        ErrorLabel.IsVisible = false;
    }

    private void OnQuickDate90s(object sender, EventArgs e)
    {
        // Set date to middle of 90s (1995)
        BirthDatePicker.Date = new DateTime(1995, 6, 15);
        UpdateAgeDisplay();
    }

    private void OnQuickDate2000s(object sender, EventArgs e)
    {
        // Set date to middle of 2000s (2005)
        BirthDatePicker.Date = new DateTime(2005, 6, 15);
        UpdateAgeDisplay();
    }

    private void OnQuickDateOther(object sender, EventArgs e)
    {
        // Set to 30 years ago
        BirthDatePicker.Date = DateTime.Today.AddYears(-30);
        UpdateAgeDisplay();
    }

    private void UpdateAgeDisplay()
    {
        var selectedDate = BirthDatePicker.Date;
        var age = CalculateAge(selectedDate);
        
        AgeLabel.Text = $"Varsta: {age} ani";
        
        // Visual feedback based on age
        if (age < 10)
        {
            AgeLabel.TextColor = Color.FromArgb("#FF6B6B");
        }
        else if (age > 100)
        {
            AgeLabel.TextColor = Color.FromArgb("#FFD93D");
        }
        else
        {
            AgeLabel.TextColor = Color.FromArgb("#4CAF50");
        }
    }

    private int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        
        // Adjust if birthday hasn't occurred this year
        if (birthDate.Date > today.AddYears(-age))
            age--;
            
        return age;
    }

    private async void OnNext(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        
        var selectedDate = BirthDatePicker.Date;
        var age = CalculateAge(selectedDate);
        
        // Age validation with better messaging
        if (age < 10)
        {
            ShowError("Trebuie sa ai cel putin 10 ani pentru a te inregistra");
            return;
        }
        
        if (age > 120)
        {
            ShowError("Te rugam sa verifici data nasterii introdusa");
            return;
        }
        
        if (selectedDate > DateTime.Today)
        {
            ShowError("Data nasterii nu poate fi in viitor");
            return;
        }

        // Show loading state
        ContinueButton.IsEnabled = false;
        ContinueButton.Text = "Se pregateste...";
        
        try
        {
            // Small delay for better UX
            await Task.Delay(300);
            
            RegistrationSession.SetBirthDate(selectedDate);
            await Shell.Current.GoToAsync(nameof(ProfilePhotoPage));
        }
        catch (Exception ex)
        {
            ShowError($"Eroare: {ex.Message}");
        }
        finally
        {
            ContinueButton.IsEnabled = true;
            ContinueButton.Text = "Continua spre finalul inregistrarii";
        }
    }

    private async void OnBack(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnCancel(object sender, EventArgs e)
    {
        RegistrationSession.Clear();
        Shell.Current.GoToAsync("//LoginPage");
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
        
        // Visual feedback
        DateFrame.BorderColor = Color.FromArgb("#FF6B6B");
        
        // Remove border color after a delay
        Device.StartTimer(TimeSpan.FromSeconds(3), () =>
        {
            DateFrame.BorderColor = Colors.Transparent;
            return false;
        });
        
        // Gentle shake animation
        this.ScaleTo(0.98, 100)
            .ContinueWith(t => this.ScaleTo(1.0, 100));
    }
}