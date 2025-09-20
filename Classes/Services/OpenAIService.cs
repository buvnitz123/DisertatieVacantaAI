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
                        return "Ne pare rău, serviciul AI nu este disponibil momentan. Te rog încearcă din nou mai târziu.";
                    }
                }

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(@"Ești un asistent AI specializat în planificarea vacanțelor și călătoriilor. 
                        Nume: Travel Assistant AI
                        Rolul tău: Să ajuți utilizatorii să planifice vacanțe perfecte, să oferi recomandări de destinații, sfaturi de călătorie și informații utile.
                        
                        Instrucțiuni:
                        - Răspunde întotdeauna în română
                        - Fii prietenos, entuziast și util
                        - Oferă recomandări personalizate bazate pe preferințele utilizatorului
                        - Include sfaturi practice pentru călătorie (buget, transport, cazare, activități)
                        - Dacă nu știi ceva specific, recunoaște și oferă alternative
                        - Întreabă pentru detalii suplimentare când e necesar pentru a da recomandări mai bune
                        - Fii concis dar informativ - răspunsuri de 2-4 propoziții în general
                        
                        Când vorbești despre destinații, include:
                        - Atracții principale
                        - Perioada optimă pentru vizită
                        - Aproximative de buget
                        - Modalități de transport
                        - Sfaturi practice")
                };

                if (conversationHistory != null && conversationHistory.Any())
                {
                    foreach (var message in conversationHistory.TakeLast(3))
                    {
                        messages.Add(new UserChatMessage(message));
                    }
                }

                messages.Add(new UserChatMessage(userMessage));

                Debug.WriteLine($"Sending request to OpenAI: {userMessage}");

                var chatCompletionOptions = new ChatCompletionOptions()
                {
                    MaxOutputTokenCount = 250,
                    Temperature = 0.6f, 
                    FrequencyPenalty = 0.1f,
                    PresencePenalty = 0.1f
                };

                var completion = await _openAIClient.GetChatClient("gpt-4o-mini").CompleteChatAsync(messages, chatCompletionOptions);
                
                if (completion?.Value?.Content?.Count > 0)
                {
                    var aiResponse = completion.Value.Content[0].Text;
                    Debug.WriteLine($"Received response from OpenAI: {aiResponse}");
                    return aiResponse ?? "Ne pare rău, nu am putut genera un răspuns. Te rog încearcă din nou.";
                }
                else
                {
                    Debug.WriteLine("No valid response content found");
                    return "Ne pare rău, nu am primit un răspuns valid. Te rog încearcă din nou.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting chat response: {ex.Message}");
                
                if (ex.Message.Contains("unauthorized") || ex.Message.Contains("401"))
                {
                    return "Ne pare rău, cheia API nu este validă. Te rog contactează administratorul.";
                }
                else if (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
                {
                    return "Prea multe cereri. Te rog încearcă din nou în câteva secunde.";
                }
                else if (ex.Message.Contains("insufficient") || ex.Message.Contains("402"))
                {
                    return "Creditul API a fost epuizat. Te rog contactează administratorul.";
                }
                
                return "Ne pare rău, a apărut o eroare în comunicarea cu serviciul AI. Te rog încearcă din nou.";
            }
        }

        public async Task<string> GetVacationRecommendationAsync(string destination, string budget, string duration, string travelStyle)
        {
            var prompt = $@"Vreau să planific o vacanță la {destination}. 
                          Bugetul meu este {budget}, durata călătoriei {duration}, 
                          și prefer un stil de călătorie {travelStyle}. 
                          Poți să-mi dai o recomandare detaliată cu atracții, cazare, transport și sfaturi de buget?";
            
            return await GetChatResponseAsync(prompt);
        }

        public void Dispose()
        {
            _openAIClient = null;
        }
    }
}