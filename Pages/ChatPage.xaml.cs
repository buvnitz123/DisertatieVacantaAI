using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Diagnostics;
using System.Windows.Input;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;

namespace MauiAppDisertatieVacantaAI.Pages;

// ViewModel for conversation display
public class ConversationDisplayItem : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Denumire { get; set; }
    public DateTime Data_Creare { get; set; }
    public string FormattedDate => Data_Creare.ToString("dd MMM yyyy, HH:mm:ss");

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class ChatPage : ContentPage
{
    private readonly ConversatieAIRepository _conversatieRepo = new();
    private readonly MesajAIRepository _mesajRepo = new();
    private readonly ObservableCollection<ConversationDisplayItem> _conversations = new();
    
    private int _currentUserId;

    public ICommand EditConversationCommand { get; private set; }
    public ICommand DeleteConversationCommand { get; private set; }

    public ChatPage()
    {
        InitializeComponent();
        ConversationsCollection.ItemsSource = _conversations;
        
        // Initialize commands
        EditConversationCommand = new Command<ConversationDisplayItem>(async (conversation) => await EditConversationNameAsync(conversation));
        DeleteConversationCommand = new Command<ConversationDisplayItem>(async (conversation) => await DeleteConversationAsync(conversation));
        
        // Set binding context for commands
        ConversationsCollection.BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserAndConversationsAsync();
    }

    private async Task LoadUserAndConversationsAsync()
    {
        try
        {
            // Get current user
            var userIdStr = await UserSession.GetUserIdAsync();
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out _currentUserId))
            {
                await DisplayAlert("Eroare", "Trebuie sa fii autentificat pentru a accesa chat-ul", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            Debug.WriteLine($"User ID loaded: {_currentUserId}");

            // Test database connection by trying to get a count of conversations
            try
            {
                var testCount = _conversatieRepo.GetAll().Count();
                Debug.WriteLine($"Database connection test successful. Total conversations: {testCount}");
            }
            catch (Exception dbEx)
            {
                Debug.WriteLine($"Database connection test failed: {dbEx.Message}");
                await DisplayAlert("Eroare", "Nu se poate conecta la baza de date", "OK");
                return;
            }

            await LoadConversationsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading user and conversations: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-au putut incarca conversatiile", "OK");
        }
    }

    private async Task LoadConversationsAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            await Task.Run(() =>
            {
                var conversations = _conversatieRepo.GetByUserId(_currentUserId)
                    .OrderByDescending(c => c.Data_Creare)
                    .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _conversations.Clear();
                    
                    if (!conversations.Any())
                    {
                        NoConversationsLabel.IsVisible = true;
                        ConversationsCollection.IsVisible = false;
                    }
                    else
                    {
                        NoConversationsLabel.IsVisible = false;
                        ConversationsCollection.IsVisible = true;

                        foreach (var conv in conversations)
                        {
                            _conversations.Add(new ConversationDisplayItem
                            {
                                Id = conv.Id_ConversatieAI,
                                Denumire = conv.Denumire,
                                Data_Creare = conv.Data_Creare
                            });
                        }
                    }
                });
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading conversations: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Eroare", "Nu s-au putut incarca conversatiile", "OK");
            });
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            });
        }
    }

    private async void OnNewConversationClicked(object sender, EventArgs e)
    {
        try
        {
            // Verify user is still authenticated
            if (_currentUserId <= 0)
            {
                await DisplayAlert("Eroare", "ID utilizator invalid. Te rog sa te autentifici din nou.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            string conversationName = await DisplayPromptAsync(
                "Conversatie Noua", 
                "Introdu numele conversatiei:",
                "OK", 
                "Anuleaza",
                "Conversatie noua",
                50);

            if (string.IsNullOrWhiteSpace(conversationName))
                return;

            Debug.WriteLine($"Creating conversation with name: {conversationName.Trim()}");
            Debug.WriteLine($"Current user ID: {_currentUserId}");

            // Create new conversation (ID will be auto-generated by repository)
            var newConversation = new ConversatieAI
            {
                // Don't set Id_ConversatieAI - let the repository generate it
                Denumire = conversationName.Trim(),
                Data_Creare = DateTime.Now,
                Id_Utilizator = _currentUserId
            };

            Debug.WriteLine($"ConversatieAI object created: Id_Utilizator={newConversation.Id_Utilizator}, Denumire={newConversation.Denumire}, Data_Creare={newConversation.Data_Creare}");

            // Insert the conversation - the repository will assign the ID
            _conversatieRepo.Insert(newConversation);
            Debug.WriteLine($"ConversatieAI inserted successfully with ID: {newConversation.Id_ConversatieAI}");

            // Reload conversations to get the new one with ID
            await LoadConversationsAsync();

            // Find and select the newly created conversation
            var createdConversation = _conversations.FirstOrDefault(c => c.Denumire == conversationName.Trim());
            if (createdConversation != null)
            {
                Debug.WriteLine($"Found created conversation with ID: {createdConversation.Id}");
                await OpenConversationAsync(createdConversation);
            }
            else
            {
                Debug.WriteLine("Created conversation not found after reload");
                await DisplayAlert("Avertisment", "Conversatia a fost creata dar nu a putut fi deschisa automat. Te rog reincarca pagina.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating new conversation: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Debug.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
            }
            await DisplayAlert("Eroare", $"Nu s-a putut crea conversatia: {ex.Message}", "OK");
        }
    }

    private async void OnConversationTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Element element && element.BindingContext is ConversationDisplayItem conversation)
            {
                await OpenConversationAsync(conversation);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening conversation: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut deschide conversatia", "OK");
        }
    }

    private async Task OpenConversationAsync(ConversationDisplayItem conversation)
    {
        try
        {
            // Navigate to the new ChatConversationPage with parameters
            await Shell.Current.GoToAsync($"{nameof(ChatConversationPage)}?conversationId={conversation.Id}&conversationName={Uri.EscapeDataString(conversation.Denumire)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening conversation: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut deschide conversatia", "OK");
        }
    }

    private async Task EditConversationNameAsync(ConversationDisplayItem conversation)
    {
        try
        {
            string newName = await DisplayPromptAsync(
                "Modifica numele",
                "Introdu noul nume pentru conversatie:",
                "Salveaza",
                "Anuleaza",
                conversation.Denumire,
                50);

            if (string.IsNullOrWhiteSpace(newName) || newName.Trim() == conversation.Denumire)
                return;

            Debug.WriteLine($"Updating conversation {conversation.Id} name from '{conversation.Denumire}' to '{newName.Trim()}'");

            // Get the conversation entity from database
            var conversationEntity = _conversatieRepo.GetById(conversation.Id);
            if (conversationEntity == null)
            {
                await DisplayAlert("Eroare", "Conversatia nu a fost gasita", "OK");
                return;
            }

            // Update the name
            conversationEntity.Denumire = newName.Trim();
            _conversatieRepo.Update(conversationEntity);

            Debug.WriteLine("Conversation name updated successfully");

            // Reload the conversations list
            await LoadConversationsAsync();

            await DisplayAlert("Succes", "Numele conversatiei a fost modificat cu succes!", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating conversation name: {ex.Message}");
            await DisplayAlert("Eroare", $"Nu s-a putut modifica numele: {ex.Message}", "OK");
        }
    }

    private async Task DeleteConversationAsync(ConversationDisplayItem conversation)
    {
        try
        {
            bool confirmDelete = await DisplayAlert(
                "Confirmare stergere",
                $"Esti sigur ca vrei sa stergi conversatia '{conversation.Denumire}'?\n\nAceasta actiune va sterge si toate mesajele din conversatie si nu poate fi anulata.",
                "Da, sterge",
                "Anuleaza");

            if (!confirmDelete)
                return;

            Debug.WriteLine($"Deleting conversation {conversation.Id} and all its messages");

            // First delete all messages from this conversation
            _mesajRepo.DeleteByConversationId(conversation.Id);
            Debug.WriteLine("All messages deleted successfully");

            // Then delete the conversation itself
            _conversatieRepo.Delete(conversation.Id);
            Debug.WriteLine("Conversation deleted successfully");

            // Reload the conversations list
            await LoadConversationsAsync();

            await DisplayAlert("Succes", "Conversatia si toate mesajele au fost sterse cu succes!", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting conversation: {ex.Message}");
            await DisplayAlert("Eroare", $"Nu s-a putut sterge conversatia: {ex.Message}", "OK");
        }
    }
}