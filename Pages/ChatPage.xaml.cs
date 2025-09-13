using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Session;
using System.Diagnostics;

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

public class ChatMessage : INotifyPropertyChanged
{
    public string Text { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string TimeString => Timestamp.ToString("HH:mm:ss");
    public string BubbleColor => IsUser ? (Application.Current?.Resources["PrimaryBlue"] as Color)?.ToHex() ?? "#0092ca" : "#444444";
    public LayoutOptions HorizontalAlignment => IsUser ? LayoutOptions.End : LayoutOptions.Start;
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class ChatPage : ContentPage
{
    private readonly ConversatieAIRepository _conversatieRepo = new();
    private readonly MesajAIRepository _mesajRepo = new();
    private readonly ObservableCollection<ConversationDisplayItem> _conversations = new();
    private readonly ObservableCollection<ChatMessage> _messages = new();
    
    private ConversationDisplayItem _selectedConversation;
    private int _currentUserId;

    public ChatPage()
    {
        InitializeComponent();
        ConversationsCollection.ItemsSource = _conversations;
        MessagesView.ItemsSource = _messages;
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
            _selectedConversation = conversation;
            // Removed ChatTitleLabel since we eliminated the title from header

            // Load messages for this conversation
            await LoadMessagesAsync(conversation.Id);

            // Switch to chat view
            ConversationListView.IsVisible = false;
            ChatView.IsVisible = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening conversation: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut deschide conversatia", "OK");
        }
    }

    private async Task LoadMessagesAsync(int conversationId)
    {
        try
        {
            await Task.Run(() =>
            {
                var messages = _mesajRepo.GetByConversationId(conversationId)
                    .OrderBy(m => m.Data_Creare)
                    .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _messages.Clear();

                    if (!messages.Any())
                    {
                        // Add welcome message for empty conversations
                        _messages.Add(new ChatMessage 
                        { 
                            Text = "Salut! Aceasta este o conversatie noua. Pune-mi orice intrebare despre vacante!", 
                            IsUser = false,
                            Timestamp = DateTime.Now
                        });
                    }
                    else
                    {
                        foreach (var msg in messages)
                        {
                            _messages.Add(new ChatMessage
                            {
                                Text = msg.Mesaj,
                                IsUser = msg.Mesaj_User == 1,
                                Timestamp = msg.Data_Creare
                            });
                        }
                    }

                    // Scroll to last message
                    if (_messages.Count > 0)
                    {
                        MessagesView.ScrollTo(_messages.Last(), position: ScrollToPosition.End, animate: false);
                    }
                });
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading messages: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _messages.Clear();
                _messages.Add(new ChatMessage 
                { 
                    Text = "Eroare la incarcarea mesajelor. Te rog incearca din nou.", 
                    IsUser = false 
                });
            });
        }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (_selectedConversation == null) return;

        var text = MessageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            MessageEntry.Text = string.Empty;
            
            // Add user message to UI
            var userMessage = new ChatMessage { Text = text, IsUser = true };
            AddMessageAnimated(userMessage);

            // Save user message to database (ID will be auto-generated by repository)
            var userMesaj = new MesajAI
            {
                // Don't set Id_Mesaj - let the repository generate it
                Mesaj = text,
                Data_Creare = DateTime.Now,
                Mesaj_User = 1, // 1 = user message
                Id_ConversatieAI = _selectedConversation.Id
            };
            _mesajRepo.Insert(userMesaj);

            // Simulate AI response (since we don't have AI integration yet)
            await Task.Delay(600);
            
            var aiResponseText = GetSimulatedAIResponse(text);
            var aiMessage = new ChatMessage { Text = aiResponseText, IsUser = false };
            AddMessageAnimated(aiMessage);

            // Save AI response to database (ID will be auto-generated by repository)
            var aiMesaj = new MesajAI
            {
                // Don't set Id_Mesaj - let the repository generate it
                Mesaj = aiResponseText,
                Data_Creare = DateTime.Now,
                Mesaj_User = 0, // 0 = AI message
                Id_ConversatieAI = _selectedConversation.Id
            };
            _mesajRepo.Insert(aiMesaj);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error sending message: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut trimite mesajul", "OK");
        }
    }

    private string GetSimulatedAIResponse(string userMessage)
    {
        // Simple simulated responses based on keywords
        var message = userMessage.ToLower();
        
        if (message.Contains("salut") || message.Contains("buna"))
            return "Salut! Sunt aici sa te ajut cu planificarea vacantelor tale. Ce destinatie te intereseaza?";
        
        if (message.Contains("vacanta") || message.Contains("calatorie"))
            return "Excelent! Iti pot recomanda destinatii minunate. Ce tip de vacanta preferi - la mare, la munte, city break sau aventura?";
        
        if (message.Contains("mare") || message.Contains("plaja"))
            return "Plajele sunt perfecte pentru relaxare! Romania are litoral frumos la Marea Neagra, iar in strainatate poti incerca Grecia, Bulgaria sau Croatia.";
        
        if (message.Contains("munte"))
            return "Muntii sunt ideali pentru aventura si aer curat! Carpatii Romaniei sunt magnifici, dar poti explora si Alpii sau Pirinei.";
        
        if (message.Contains("buget") || message.Contains("pret"))
            return "Inteleg ca bugetul e important. Poti avea vacante minunate la orice buget - de la weekend-uri aproape de casa la calatorii internationale. Spune-mi ce suma ai in minte?";
        
        return "Foarte interesant! Iti voi gasi recomandari personalizate curand. Pentru moment, exploreaza destinatiile disponibile in aplicatie.";
    }

    private void AddMessageAnimated(ChatMessage msg)
    {
        _messages.Add(msg);
        MessagesView.ScrollTo(msg, position: ScrollToPosition.End, animate: true);
    }

    private async void OnBackToConversationsClicked(object sender, EventArgs e)
    {
        try
        {
            _selectedConversation = null;
            _messages.Clear();
            
            // Switch back to conversation list
            ChatView.IsVisible = false;
            ConversationListView.IsVisible = true;
            
            // Refresh conversations in case anything changed
            await LoadConversationsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error going back to conversations: {ex.Message}");
        }
    }
}