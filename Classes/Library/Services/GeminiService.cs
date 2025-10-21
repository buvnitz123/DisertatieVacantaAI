using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using Mscc.GenerativeAI;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public class GeminiService
    {
        private GoogleAI? _geminiClient;
        private GenerativeModel? _model;
        private bool _initialized = false;

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var apiKey = AppSettingsRepository.GetValue("GeminiAPI");

                if (string.IsNullOrEmpty(apiKey))
                {
                    Debug.WriteLine("Gemini API key is missing");
                    return false;
                }

                _geminiClient = new GoogleAI(apiKey);
                _model = _geminiClient.GenerativeModel(model: "gemini-2.5-flash");
                _initialized = true;
                Debug.WriteLine("Gemini service initialized successfully with Gemini 2.5 Flash");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Gemini service: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetChatResponseAsync(string userMessage, List<string>? conversationHistory = null)
        {
            try
            {
                if (!_initialized || _model == null)
                {
                    if (!await InitializeAsync())
                    {
                        return "Ne pare rău, serviciul AI nu este disponibil momentan. Te rog încercați din nou mai târziu.";
                    }
                }

                var systemPrompt = @"Ești un asistent AI specializat în planificarea vacanțelor și călătoriilor. 
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
- Sfaturi practice";

                var fullPrompt = systemPrompt + "\n\n";

                if (conversationHistory != null && conversationHistory.Any())
                {
                    fullPrompt += "Context conversație:\n";
                    foreach (var message in conversationHistory.TakeLast(3))
                    {
                        fullPrompt += $"Utilizator: {message}\n";
                    }
                }

                fullPrompt += $"\nUtilizator: {userMessage}\n\nAsistent:";

                Debug.WriteLine($"Sending request to Gemini: {userMessage}");

                var generationConfig = new GenerationConfig
                {
                    MaxOutputTokens = 10000,
                    Temperature = 0.6f
                };

                var response = await _model.GenerateContent(fullPrompt, generationConfig);

                if (!string.IsNullOrEmpty(response?.Text))
                {
                    var aiResponse = response.Text.Trim();
                    Debug.WriteLine($"Received response from Gemini: {aiResponse}");
                    return aiResponse;
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

                if (ex.Message.Contains("API_KEY_INVALID") || ex.Message.Contains("401"))
                {
                    return "Ne pare rău, cheia API nu este validă. Te rog contactează administratorul.";
                }
                else if (ex.Message.Contains("RESOURCE_EXHAUSTED") || ex.Message.Contains("429"))
                {
                    return "Prea multe cereri. Te rog încearcă din nou în câteva secunde.";
                }
                else if (ex.Message.Contains("QUOTA_EXCEEDED"))
                {
                    return "Creditul API a fost epuizat. Te rog contactează administratorul.";
                }

                return "Ne pare rău, a apărut o eroare în comunicarea cu serviciul AI. Te rog încearcă din nou.";
            }
        }

        public async Task<string> GetDestinationCreationResponseAsync(string userQuery, string existingDestinations, string availableCategories, List<string>? conversationHistory = null)
        {
            try
            {
                if (!_initialized || _model == null)
                {
                    if (!await InitializeAsync())
                    {
                        return CreateErrorResponse("Serviciul AI nu este disponibil momentan.");
                    }
                }

                var prompt = await AIDestinationPromptTemplate.BuildPromptAsync(userQuery, existingDestinations, availableCategories);

                // Adaugă context conversațional dacă există
                if (conversationHistory != null && conversationHistory.Any())
                {
                    prompt += "\n\n📝 CONTEXT CONVERSAȚIE ANTERIOARĂ:\n";
                    foreach (var message in conversationHistory.TakeLast(10)) // Ultimele 3 mesaje
                    {
                        prompt += $"- {message}\n";
                    }
                    prompt += "\n⚠️ Ține cont de mesajele anterioare când răspunzi!\n";
                }

                Debug.WriteLine("=== PROMPT BEING SENT TO GEMINI ===");
                Debug.WriteLine($"Prompt length: {prompt.Length} characters");
                Debug.WriteLine($"User query: {userQuery}");
                Debug.WriteLine($"Existing destinations: {existingDestinations}");
                Debug.WriteLine($"Available categories: {availableCategories}");
                Debug.WriteLine($"Conversation history messages: {conversationHistory?.Count ?? 0}");
                Debug.WriteLine("=== FULL PROMPT START ===");
                Debug.WriteLine(prompt);
                Debug.WriteLine("=== FULL PROMPT END ===");

                prompt += $"\n\nAnalizează acest mesaj și decide dacă utilizatorul vrea să creeze o destinație sau doar pune întrebări generale:\n\n\"{userQuery}\"\n\nRăspunde cu JSON conform instrucțiunilor.";

                Debug.WriteLine($"Final prompt length: {prompt.Length} characters");
                Debug.WriteLine($"Sending destination creation request to Gemini: {userQuery}");

                var generationConfig = new GenerationConfig
                {
                    MaxOutputTokens = 10000,
                    Temperature = 0.3f
                };

                var response = await _model.GenerateContent(prompt, generationConfig);

                Debug.WriteLine($"API Response - IsEmpty: {response == null}");
                Debug.WriteLine($"API Response - Text IsEmpty: {string.IsNullOrEmpty(response?.Text)}");
                Debug.WriteLine($"API Response - Text Length: {response?.Text?.Length ?? 0}");
                Debug.WriteLine($"API Response - Full Text: {response?.Text ?? "NULL"}");

                if (!string.IsNullOrEmpty(response?.Text))
                {
                    var aiResponse = response.Text.Trim();
                    Debug.WriteLine($"Received destination creation response from Gemini: {aiResponse}");

                    var cleanedResponse = CleanAIResponse(aiResponse);
                    if (string.IsNullOrEmpty(cleanedResponse))
                    {
                        return CreateErrorResponse("Răspuns gol de la AI.");
                    }

                    if (!cleanedResponse.Contains("{") || !cleanedResponse.Contains("}"))
                    {
                        Debug.WriteLine($"Response doesn't appear to be JSON: {cleanedResponse}");
                        return CreateErrorResponse("AI-ul nu a generat un răspuns în format JSON valid.");
                    }

                    return cleanedResponse;
                }
                else
                {
                    Debug.WriteLine("No valid response content found for destination creation");
                    return CreateErrorResponse("Nu am primit un răspuns valid de la AI.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting destination creation response: {ex.Message}");
                return CreateErrorResponse($"Eroare în comunicarea cu AI: {ex.Message}");
            }
        }

        private string CleanAIResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            try
            {
                var cleaned = response
                    .Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Replace("\\\\", "\\")
                    .Replace("\\\n", " ")
                    .Replace("\\\r", " ")
                    .Replace("\\n\\r", " ")
                    .Replace("\\r\\n", " ")
                    .Replace("\\t", " ")
                    .Trim();

                return cleaned;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning AI response: {ex.Message}");
                return response.Trim();
            }
        }

        private string CreateErrorResponse(string errorMessage)
        {
            return $@"{{
    ""action"": ""error"",
    ""success"": false,
    ""message"": ""{errorMessage}"",
    ""destination"": null
}}";
        }

        public void Dispose()
        {
            _model = null;
            _geminiClient = null;
        }
    }
}
