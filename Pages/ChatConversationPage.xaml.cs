using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Session;
using MauiAppDisertatieVacantaAI.Classes.Services;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Pages;

[QueryProperty(nameof(ConversationId), "conversationId")]
[QueryProperty(nameof(ConversationName), "conversationName")]
public partial class ChatConversationPage : ContentPage
{
    private readonly MesajAIRepository _mesajRepo = new();
    private readonly OpenAIService _openAIService = new();
    private readonly ObservableCollection<ChatMessage> _messages = new();
    
    public string ConversationId { get; set; }
    public string ConversationName { get; set; }
    
    private int _conversationId;
    private int _currentUserId;
    private bool _isAIResponding = false;

    public ChatConversationPage()
    {
        InitializeComponent();
        MessagesView.ItemsSource = _messages;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadConversationAsync();
    }

    private async Task LoadConversationAsync()
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

            // Parse conversation ID
            if (!int.TryParse(ConversationId, out _conversationId))
            {
                await DisplayAlert("Eroare", "ID conversatie invalid", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Set title if provided
            if (!string.IsNullOrEmpty(ConversationName))
            {
                Title = ConversationName;
            }

            await LoadMessagesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading conversation: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-au putut incarca mesajele", "OK");
        }
    }

    private async Task LoadMessagesAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var messages = _mesajRepo.GetByConversationId(_conversationId)
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
                            Text = "👋 Salut! Sunt Travel Assistant AI (powered by GPT-4o Mini) și sunt aici să te ajut să planifici vacanța perfectă! Spune-mi ce destinație te interesează sau ce fel de experiență de călătorie cauți.", 
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
        var text = MessageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text) || _isAIResponding) return;

        try
        {
            MessageEntry.Text = string.Empty;
            SetInputEnabled(false); // Disable input completely during AI response
            _isAIResponding = true;
            
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
                Id_ConversatieAI = _conversationId
            };
            _mesajRepo.Insert(userMesaj);

            // Show typing indicator
            var typingMessage = new ChatMessage 
            { 
                Text = "💭 Travel Assistant AI (GPT-4o Mini) scrie...", 
                IsUser = false,
                IsTyping = true,
                Timestamp = DateTime.Now
            };
            AddMessageAnimated(typingMessage);

            // Get conversation history for context
            var conversationHistory = _messages
                .Where(m => m.IsUser)
                .TakeLast(5) // Last 5 user messages for context
                .Select(m => m.Text)
                .ToList();

            // Get AI response
            string aiResponseText;
            try
            {
                aiResponseText = await _openAIService.GetChatResponseAsync(text, conversationHistory);
            }
            catch (Exception aiEx)
            {
                Debug.WriteLine($"Error getting AI response: {aiEx.Message}");
                aiResponseText = "Ne pare rău, serviciul AI nu este disponibil momentan. Te rog încearcă din nou mai târziu.";
            }

            // Remove typing indicator
            _messages.Remove(typingMessage);

            // Create AI message for animated typing
            var aiMessage = new ChatMessage { Text = "", IsUser = false, Timestamp = DateTime.Now };
            AddMessageAnimated(aiMessage);

            // Animate the AI response word by word
            await AnimateAIResponseAsync(aiMessage, aiResponseText);

            // Save AI response to database (ID will be auto-generated by repository)
            var aiMesaj = new MesajAI
            {
                // Don't set Id_Mesaj - let the repository generate it
                Mesaj = aiResponseText,
                Data_Creare = DateTime.Now,
                Mesaj_User = 0, // 0 = AI message
                Id_ConversatieAI = _conversationId
            };
            _mesajRepo.Insert(aiMesaj);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error sending message: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-a putut trimite mesajul", "OK");
        }
        finally
        {
            SetInputEnabled(true); // Re-enable input after AI response is complete
            _isAIResponding = false;
        }
    }

    private void SetInputEnabled(bool enabled)
    {
        MessageEntry.IsEnabled = enabled;
        SendButton.IsEnabled = enabled;
        MessageEntry.Placeholder = enabled ? "Scrie un mesaj..." : "AI răspunde...";
        
        // Visual feedback for disabled state
        MessageEntry.Opacity = enabled ? 1.0 : 0.6;
        SendButton.Opacity = enabled ? 1.0 : 0.6;
    }

    private async Task AnimateAIResponseAsync(ChatMessage aiMessage, string fullResponse)
    {
        if (string.IsNullOrEmpty(fullResponse))
        {
            aiMessage.Text = "Nu am putut genera un răspuns.";
            return;
        }

        Debug.WriteLine($"Starting animation for response: {fullResponse}");

        // Split response into words for word-by-word animation
        var words = fullResponse.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentText = "";

        Debug.WriteLine($"Animation will show {words.Length} words");

        foreach (var word in words)
        {
            currentText += (currentText.Length > 0 ? " " : "") + word;
            
            Debug.WriteLine($"Animating: '{currentText}'");
            
            // Update the message text on the main thread - now with proper property change notification
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                aiMessage.Text = currentText;
                
                // Auto-scroll to keep the latest content visible
                if (_messages.Count > 0)
                {
                    MessagesView.ScrollTo(_messages.Last(), position: ScrollToPosition.End, animate: false);
                }
            });

            // Delay between words - adjust speed as needed (faster for better UX)
            await Task.Delay(80); // 80ms between words for natural typing speed
        }

        // Ensure final text is set
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Debug.WriteLine($"Final text set: {fullResponse}");
            aiMessage.Text = fullResponse;
        });
    }

    private void AddMessageAnimated(ChatMessage msg)
    {
        _messages.Add(msg);
        MessagesView.ScrollTo(msg, position: ScrollToPosition.End, animate: true);
    }
}