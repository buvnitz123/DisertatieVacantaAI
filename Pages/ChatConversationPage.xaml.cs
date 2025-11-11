using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Models;
using MauiAppDisertatieVacantaAI.Classes.Library.Services;
using MauiAppDisertatieVacantaAI.Classes.Library.Session;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Pages;

[QueryProperty(nameof(ConversationId), "conversationId")]
[QueryProperty(nameof(ConversationName), "conversationName")]
public partial class ChatConversationPage : ContentPage
{
    private readonly MesajAIRepository _mesajRepo = new();
    private readonly GeminiService _geminiService = new();
    private readonly AIDestinationProcessorService _processorService = new();
    private readonly AISuggestionProcessorService _suggestionProcessor = new(); // NOU!
    private readonly DestinatieRepository _destinatieRepo = new();
    private readonly CategorieVacantaRepository _categorieRepo = new();
    private readonly ObservableCollection<ChatMessage> _messages = new();

    public string ConversationId { get; set; }
    public string ConversationName { get; set; }

    private int _conversationId;
    private int _currentUserId;
    private bool _isAIResponding = false;
    private bool _isFirstUserMessage = true; // NOU - track dacă e primul mesaj user

    // Pagination properties
    private const int MessagesPerPage = 10;
    private int _currentPage = 0;
    private bool _hasMoreMessages = true;
    private bool _isLoadingMessages = false;
    private readonly ConversatieAIRepository _conversatieRepo = new(); // NOU - pentru actualizare denumire

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

            // NOU: Verifică dacă există deja mesaje user în conversație
            var existingUserMessages = _mesajRepo.GetByConversationId(_conversationId)
                .Where(m => m.Mesaj_User == 1)
                .Count();

            _isFirstUserMessage = existingUserMessages == 0;
            Debug.WriteLine($"ℹ️ Conversation has {existingUserMessages} user messages. IsFirstMessage: {_isFirstUserMessage}");

            await LoadMessagesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading conversation: {ex.Message}");
            await DisplayAlert("Eroare", "Nu s-au putut incarca mesajele", "OK");
        }
    }

    private async Task LoadMessagesAsync(bool loadMore = false)
    {
        if (_isLoadingMessages) return;

        try
        {
            _isLoadingMessages = true;

            if (!loadMore)
            {
                // Reset pagination for initial load
                _currentPage = 0;
                _hasMoreMessages = true;
            }

            await Task.Run(() =>
            {
                // Get total count first
                var totalMessages = _mesajRepo.GetByConversationId(_conversationId).Count();

                // Calculate skip count (for DESC order, we need to get latest messages first)
                var skip = _currentPage * MessagesPerPage;

                var messages = _mesajRepo.GetByConversationId(_conversationId)
                    .OrderByDescending(m => m.Data_Creare) // Get newest first
                    .Skip(skip)
                    .Take(MessagesPerPage)
                    .OrderBy(m => m.Data_Creare) // Then order chronologically for display
                    .ToList();

                // Check if there are more messages
                var hasMore = totalMessages > (_currentPage + 1) * MessagesPerPage;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!loadMore)
                    {
                        // Initial load - clear and add welcome message if no messages
                        _messages.Clear();

                        if (!messages.Any())
                        {
                            _messages.Add(new ChatMessage
                            {
                                Text = "👋 Salut! Sunt Travel Assistant AI și sunt aici să te ajut să planifici vacanța perfectă!\n\n🌍 Pot să:\n• Răspund la întrebări despre călătorii\n• Creez destinații noi în aplicație (încearcă: \"Vreau să merg la Dubai\" sau \"Fă-mi o vacanță în Santorini\")\n• Ofer sfaturi și recomandări personalizate\n\nSpune-mi ce te interesează!",
                                IsUser = false,
                                Timestamp = DateTime.Now
                            });
                        }
                        else
                        {
                            // Add messages
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

                        // Scroll to last message on initial load
                        if (_messages.Count > 0)
                        {
                            MessagesView.ScrollTo(_messages.Last(), position: ScrollToPosition.End, animate: false);
                        }
                    }
                    else
                    {
                        // Load more - insert at beginning
                        var currentCount = _messages.Count;
                        var insertIndex = 0;

                        // Skip welcome message if present
                        if (currentCount > 0 && _messages[0].Text.Contains("Travel Assistant AI"))
                        {
                            insertIndex = 1;
                        }

                        for (int i = messages.Count - 1; i >= 0; i--)
                        {
                            var msg = messages[i];
                            _messages.Insert(insertIndex, new ChatMessage
                            {
                                Text = msg.Mesaj,
                                IsUser = msg.Mesaj_User == 1,
                                Timestamp = msg.Data_Creare
                            });
                        }

                        // Maintain scroll position (scroll to the message that was first before)
                        if (currentCount > insertIndex)
                        {
                            var targetIndex = insertIndex + messages.Count;
                            if (targetIndex < _messages.Count)
                            {
                                MessagesView.ScrollTo(_messages[targetIndex], position: ScrollToPosition.Start, animate: false);
                            }
                        }
                    }

                    _hasMoreMessages = hasMore;
                    _currentPage++;

                    // Update button visibility
                    UpdateLoadMoreButtonVisibility();
                });
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading messages: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!loadMore)
                {
                    _messages.Clear();
                    _messages.Add(new ChatMessage
                    {
                        Text = "Eroare la incarcarea mesajelor. Te rog incearca din nou.",
                        IsUser = false
                    });
                }
                UpdateLoadMoreButtonVisibility();
            });
        }
        finally
        {
            _isLoadingMessages = false;
            UpdateLoadMoreButtonVisibility();
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

            // NOU: Actualizează denumirea conversației cu primele 40 caractere din primul mesaj user
            if (_isFirstUserMessage)
            {
                await UpdateConversationNameFromFirstMessageAsync(text);
                _isFirstUserMessage = false;
            }

            // Show typing indicator
            var typingMessage = new ChatMessage
            {
                Text = "💭 Travel Assistant AI analizează...",
                IsUser = false,
                IsTyping = true,
                Timestamp = DateTime.Now
            };
            AddMessageAnimated(typingMessage);

            // Get AI response
            string aiResponseText;
            try
            {
                var existingDestinations = GetExistingDestinationsForPrompt();
                var availableCategories = GetAvailableCategoriesForPrompt();

                // Construiește istoricul conversației (ultimele 3 mesaje pentru a economisi tokeni)
                // Limitează fiecare mesaj la maxim 500 caractere pentru a nu supraîncărca context-ul
                var conversationHistory = _messages
                    .Where(m => !m.IsTyping && !m.Text.Contains("👋 Salut! Sunt Travel Assistant AI"))
                    .TakeLast(3) // Redus de la 5 la 3 pentru eficiență
                  .Select(m =>
                        {
                            var text = m.Text.Length > 500 ? m.Text.Substring(0, 500) + "..." : m.Text;
                            return $"{(m.IsUser ? "Utilizator" : "AI")}: {text}";
                        })
                    .ToList();

                Debug.WriteLine($"📝 Conversation history: {conversationHistory.Count} messages");
                if (conversationHistory.Any())
                {
                    var totalChars = conversationHistory.Sum(m => m.Length);
                    Debug.WriteLine($"📊 Total context size: {totalChars} characters");
                }

                var aiJsonResponse = await _geminiService.GetDestinationCreationResponseAsync(
                  text, existingDestinations, availableCategories, conversationHistory);

                // Detectează tipul de acțiune din JSON
                var actionType = DetectActionType(aiJsonResponse);
                Debug.WriteLine($"🎯 Detected action type: {actionType}");

                if (actionType == "create_suggestion")
                {
                    // Procesează SUGESTIE
                    Debug.WriteLine("Processing as suggestion...");
                    var suggestionResult = await _suggestionProcessor.ProcessAISuggestionAsync(aiJsonResponse, _currentUserId);
                    aiResponseText = suggestionResult.Message;
                }
                else
                {
                    // Procesează DESTINAȚIE sau CHAT
                    Debug.WriteLine("Processing as destination/chat...");
                    var result = await _processorService.ProcessAIResponseAsync(aiJsonResponse, _currentUserId);

                    if (result.IsGeneralChat)
                    {
                        Debug.WriteLine("Regular chat request detected");
                        typingMessage.Text = "💭 Travel Assistant AI scrie...";
                        aiResponseText = result.Message;
                    }
                    else if (result.Success || result.DestinationId > 0)
                    {
                        Debug.WriteLine($"Destination request detected. Response: {result.Message}");
                        aiResponseText = result.Message;
                    }
                    else
                    {
                        aiResponseText = result.Message;
                    }
                }
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

    private async Task UpdateConversationNameFromFirstMessageAsync(string text)
    {
        try
        {
            // Actualizează denumirea conversației în funcție de primle 40 caractere din mesaj
            var newName = text.Length > 40 ? text.Substring(0, 40) + "..." : text;

            await Task.Run(() =>
            {
                var conversation = _conversatieRepo.GetById(_conversationId);
                if (conversation != null)
                {
                    conversation.Denumire = newName; // ✅ Corect - numele câmpului e "Denumire"
                    _conversatieRepo.Update(conversation);
                }
            });

            // Update local prop for immediate effect
            ConversationName = newName;
            Title = newName;

            Debug.WriteLine($"Conversation name updated to: {newName}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating conversation name: {ex.Message}");
        }
    }

    private string GetExistingDestinationsForPrompt()
    {
        try
        {
            var destinations = _destinatieRepo.GetAll().Take(20);
            var destinationList = destinations.Select(d => $"{d.Denumire}, {d.Oras}, {d.Tara}").ToList();
            return string.Join(", ", destinationList);
        }
        catch
        {
            return "Nu există destinații în sistem";
        }
    }

    private string GetAvailableCategoriesForPrompt()
    {
        try
        {
            var categories = _categorieRepo.GetAll().Take(20);
            var categoryList = categories.Select(c => c.Denumire).ToList();

            if (!categoryList.Any())
            {
                Debug.WriteLine("No categories found in database!");
                return "Nu există categorizari în sistem";
            }

            return string.Join(", ", categoryList);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting categories: {ex.Message}");
            return "Nu există categorii în sistem";
        }
    }

    /// <summary>
    /// Detectează tipul de acțiune din răspunsul AI (pentru routing corect)
    /// </summary>
    private string DetectActionType(string jsonResponse)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonResponse))
                return "unknown";

            // Caută "action" în JSON
            var actionMatch = System.Text.RegularExpressions.Regex.Match(
              jsonResponse,
        @"""action""\s*:\s*""([^""]+)""",
           System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (actionMatch.Success)
            {
                return actionMatch.Groups[1].Value.ToLower();
            }

            return "unknown";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting action type: {ex.Message}");
            return "unknown";
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

    private async void OnLoadMoreClicked(object sender, EventArgs e)
    {
        await LoadOlderMessagesAsync();
    }

    private void UpdateLoadMoreButtonVisibility()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Show button only if there are more messages and we're not loading
            var shouldShow = _hasMoreMessages && !_isLoadingMessages && _messages.Count > 1;
            LoadMoreButton.IsVisible = shouldShow;
            LoadingMoreIndicator.IsVisible = _isLoadingMessages && _hasMoreMessages;
        });
    }

    private async Task LoadOlderMessagesAsync()
    {
        if (!_hasMoreMessages || _isLoadingMessages)
            return;

        try
 {
            UpdateLoadMoreButtonVisibility(); // Show loading indicator
          await LoadMessagesAsync(loadMore: true);
   }
        catch (Exception ex)
      {
  Debug.WriteLine($"Error loading older messages: {ex.Message}");
     await DisplayAlert("Eroare", "Nu s-au putut incarca mesajele mai vechi", "OK");
      }
    }
}