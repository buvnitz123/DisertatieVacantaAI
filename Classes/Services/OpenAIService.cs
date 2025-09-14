using MauiAppDisertatieVacantaAI.Classes.Config;
using System.Diagnostics;
using OpenAI;
using OpenAI.Chat;

namespace MauiAppDisertatieVacantaAI.Classes.Services
{
    public class OpenAIService
    {
        private OpenAIClient? _openAIClient;
        private bool _initialized = false;

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var apiKey = await ConfigHelper.GetOpenAIApiKeyAsync();

                if (string.IsNullOrEmpty(apiKey))
                {
                    Debug.WriteLine("OpenAI API key is missing");
                    return false;
                }

                _openAIClient = new OpenAIClient(apiKey);
                _initialized = true;
                Debug.WriteLine("OpenAI service initialized successfully with official SDK");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing OpenAI service: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetChatResponseAsync(string userMessage, List<string>? conversationHistory = null)
        {
            try
            {
                if (!_initialized || _openAIClient == null)
                {
                    if (!await InitializeAsync())
                    {
                        return "Ne pare r?u, serviciul AI nu este disponibil momentan. Te rog încearc? din nou mai târziu.";
                    }
                }

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(@"E?ti un asistent AI specializat în planificarea vacan?elor ?i c?l?toriilor. 
                        Nume: Travel Assistant AI
                        Rolul t?u: S? aju?i utilizatorii s? planifice vacan?e perfecte, s? oferi recomand?ri de destina?ii, sfaturi de c?l?torie ?i informa?ii utile.
                        
                        Instruc?ii:
                        - R?spunde întotdeauna în român?
                        - Fii prietenos, entuziast ?i util
                        - Ofer? recomand?ri personalizate bazate pe preferin?ele utilizatorului
                        - Include sfaturi practice pentru c?l?torie (buget, transport, cazare, activit??i)
                        - Dac? nu ?tii ceva specific, recunoa?te ?i ofer? alternative
                        - Întreab? pentru detalii suplimentare când e necesar pentru a da recomand?ri mai bune
                        - Fii concis dar informativ - r?spunsuri de 2-4 propozi?ii în general
                        
                        Când vorbe?ti despre destina?ii, include:
                        - Atrac?ii principale
                        - Perioada optim? pentru vizit?
                        - Aproximative de buget
                        - Modalit??i de transport
                        - Sfaturi practice")
                };

                // Add conversation history if provided
                if (conversationHistory != null && conversationHistory.Any())
                {
                    foreach (var message in conversationHistory.TakeLast(3)) // Keep last 3 messages for context (cost optimization)
                    {
                        messages.Add(new UserChatMessage(message));
                    }
                }

                // Add current user message
                messages.Add(new UserChatMessage(userMessage));

                Debug.WriteLine($"Sending request to OpenAI: {userMessage}");

                var chatCompletionOptions = new ChatCompletionOptions()
                {
                    MaxOutputTokenCount = 250, // Optimized for travel recommendations and cost
                    Temperature = 0.6f, // Slightly reduced for more consistent travel advice  
                    FrequencyPenalty = 0.1f, // Reduce repetition in recommendations
                    PresencePenalty = 0.1f // Encourage diverse travel topics
                };

                var completion = await _openAIClient.GetChatClient("gpt-4o-mini").CompleteChatAsync(messages, chatCompletionOptions);
                
                if (completion?.Value?.Content?.Count > 0)
                {
                    var aiResponse = completion.Value.Content[0].Text;
                    Debug.WriteLine($"Received response from OpenAI: {aiResponse}");
                    return aiResponse ?? "Ne pare r?u, nu am putut genera un r?spuns. Te rog încearc? din nou.";
                }
                else
                {
                    Debug.WriteLine("No valid response content found");
                    return "Ne pare r?u, nu am primit un r?spuns valid. Te rog încearc? din nou.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting chat response: {ex.Message}");
                
                // Handle specific OpenAI exceptions for better user experience
                if (ex.Message.Contains("unauthorized") || ex.Message.Contains("401"))
                {
                    return "Ne pare r?u, cheia API nu este valid?. Te rog contacteaz? administratorul.";
                }
                else if (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
                {
                    return "Prea multe cereri. Te rog încearc? din nou în câteva secunde.";
                }
                else if (ex.Message.Contains("insufficient") || ex.Message.Contains("402"))
                {
                    return "Creditul API a fost epuizat. Te rog contacteaz? administratorul.";
                }
                
                return "Ne pare r?u, a ap?rut o eroare în comunicarea cu serviciul AI. Te rog încearc? din nou.";
            }
        }

        public async Task<string> GetVacationRecommendationAsync(string destination, string budget, string duration, string travelStyle)
        {
            var prompt = $@"Vreau s? planific o vacan?? la {destination}. 
                          Bugetul meu este {budget}, durata c?l?toriei {duration}, 
                          ?i prefer un stil de c?l?torie {travelStyle}. 
                          Po?i s?-mi dai o recomandare detaliat? cu atrac?ii, cazare, transport ?i sfaturi de buget?";
            
            return await GetChatResponseAsync(prompt);
        }

        public void Dispose()
        {
            // OpenAI SDK handles disposal internally
            _openAIClient = null;
        }
    }
}