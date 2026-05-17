using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using OpenAI.Chat;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public class GPTService : IAIService
    {
        private ChatClient? _chatClient;
        private bool _initialized = false;

        public string ModelName => "gpt-4o";

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var apiKey = AppSettingsRepository.GetValue("GptAPI");

                if (string.IsNullOrEmpty(apiKey))
                {
                    Debug.WriteLine("GPT API key is missing");
                    return false;
                }

                _chatClient = new ChatClient("gpt-4o", apiKey);
                _initialized = true;
                Debug.WriteLine("GPT service initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing GPT service: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetChatResponseAsync(string userMessage, List<string>? conversationHistory = null)
        {
            try
            {
                if (!_initialized || _chatClient == null)
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

                // Add conversation history
                if (conversationHistory != null)
                {
                    foreach (var msg in conversationHistory.TakeLast(3))
                    {
                        messages.Add(new UserChatMessage(msg));
                    }
                }

                messages.Add(new UserChatMessage(userMessage));

                var options = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 10000,
                    Temperature = 0.6f
                };

                Debug.WriteLine($"Sending request to GPT: {userMessage}");

                var sw = Stopwatch.StartNew();
                var response = await _chatClient.CompleteChatAsync(messages, options);
                sw.Stop();

                // Log performance
                var tokenIn = response.Value?.Usage?.InputTokenCount ?? 0;
                var tokenOut = response.Value?.Usage?.OutputTokenCount ?? 0;
                AIPerformanceLogger.Log(ModelName, (decimal)sw.Elapsed.TotalSeconds, tokenIn, tokenOut);

                var content = response.Value?.Content?.FirstOrDefault()?.Text;
                if (!string.IsNullOrEmpty(content))
                {
                    Debug.WriteLine($"Received response from GPT: {content}");
                    return content.Trim();
                }

                return "Ne pare rău, nu am primit un răspuns valid. Te rog încearcă din nou.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPT chat response: {ex.Message}");
                return HandleError(ex);
            }
        }

        public async Task<string> GetDestinationCreationResponseAsync(string userQuery, string existingDestinations, string availableCategories, List<string>? conversationHistory = null)
        {
            try
            {
                if (!_initialized || _chatClient == null)
                {
                    if (!await InitializeAsync())
                    {
                        return AIResponseHelper.CreateErrorResponse("Serviciul AI nu este disponibil momentan.");
                    }
                }

                var prompt = await AIDestinationPromptTemplate.BuildPromptAsync(userQuery, existingDestinations, availableCategories);
                prompt = await AIResponseHelper.InjectUserProfile(prompt);
                prompt = AIResponseHelper.BuildConversationContext(conversationHistory, prompt);

                prompt += $"\n\nAnalizează acest mesaj și decide dacă utilizatorul vrea să creeze o destinație sau doar pune întrebări generale:\n\n\"{userQuery}\"\n\nRăspunde cu JSON conform instrucțiunilor.";

                Debug.WriteLine($"Sending destination creation request to GPT: {userQuery}");

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("Ești un asistent AI pentru planificarea vacanțelor. Răspunde DOAR cu JSON valid conform schemei primite. Nu adăuga text suplimentar în afara JSON-ului."),
                    new UserChatMessage(prompt)
                };

                var jsonSchema = ChatResponseFormat.CreateJsonSchemaFormat(
                    "destination_response",
                    BinaryData.FromString(GetJsonSchema()),
                    jsonSchemaIsStrict: false
                );

                var options = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 10000,
                    Temperature = 0.3f,
                    ResponseFormat = jsonSchema
                };

                var sw = Stopwatch.StartNew();
                var response = await _chatClient.CompleteChatAsync(messages, options);
                sw.Stop();

                // Log performance
                var tokenIn = response.Value?.Usage?.InputTokenCount ?? 0;
                var tokenOut = response.Value?.Usage?.OutputTokenCount ?? 0;
                AIPerformanceLogger.Log(ModelName, (decimal)sw.Elapsed.TotalSeconds, tokenIn, tokenOut);

                var content = response.Value?.Content?.FirstOrDefault()?.Text;

                Debug.WriteLine($"API Response - IsEmpty: {string.IsNullOrEmpty(content)}");
                Debug.WriteLine($"API Response - Full Text: {content ?? "NULL"}");

                if (!string.IsNullOrEmpty(content))
                {
                    var cleanedResponse = AIResponseHelper.CleanAIResponse(content);
                    if (string.IsNullOrEmpty(cleanedResponse) || !cleanedResponse.Contains("{"))
                    {
                        return AIResponseHelper.CreateErrorResponse("AI-ul nu a generat un răspuns în format JSON valid.");
                    }
                    return cleanedResponse;
                }

                return AIResponseHelper.CreateErrorResponse("Nu am primit un răspuns valid de la AI.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPT destination response: {ex.Message}");
                return AIResponseHelper.CreateErrorResponse($"Eroare în comunicarea cu AI: {ex.Message}");
            }
        }

        private string GetJsonSchema()
        {
            return @"{
  ""type"": ""object"",
  ""properties"": {
    ""action"": { ""type"": ""string"" },
    ""success"": { ""type"": ""boolean"" },
    ""message"": { ""type"": ""string"" },
    ""suggestion"": {
      ""type"": [""object"", ""null""],
      ""properties"": {
        ""titlu"": { ""type"": ""string"" },
        ""bugetEstimat"": { ""type"": ""number"" },
        ""descriere"": { ""type"": ""string"" },
        ""destinatieDenumire"": { ""type"": ""string"" },
        ""destinatieTara"": { ""type"": ""string"" },
        ""destinatieOras"": { ""type"": ""string"" },
        ""estePublic"": { ""type"": [""integer"", ""null""] },
        ""destinatieData"": {
          ""type"": [""object"", ""null""],
          ""properties"": {
            ""regiune"": { ""type"": ""string"" },
            ""descriere"": { ""type"": ""string"" },
            ""pretAdult"": { ""type"": ""number"" },
            ""pretMinor"": { ""type"": ""number"" },
            ""categorii"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
            ""facilitati"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
            ""photoSearchQueries"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
            ""puncteDeInteres"": {
              ""type"": ""array"",
              ""items"": {
                ""type"": ""object"",
                ""properties"": {
                  ""denumire"": { ""type"": ""string"" },
                  ""descriere"": { ""type"": ""string"" },
                  ""tip"": { ""type"": ""string"" },
                  ""photoSearchQueries"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } }
                }
              }
            }
          }
        }
      }
    },
    ""suggestions"": {
      ""type"": [""array"", ""null""],
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""destinatieDenumire"": { ""type"": ""string"" },
          ""destinatieTara"": { ""type"": ""string"" },
          ""destinatieOras"": { ""type"": ""string"" },
          ""bugetEstimat"": { ""type"": ""number"" },
          ""descriereScurta"": { ""type"": ""string"" }
        }
      }
    }
  },
  ""required"": [""action"", ""success"", ""message""]
}";
        }

        private string HandleError(Exception ex)
        {
            if (ex.Message.Contains("401") || ex.Message.Contains("invalid_api_key"))
                return "Ne pare rău, cheia API nu este validă. Te rog contactează administratorul.";
            if (ex.Message.Contains("429") || ex.Message.Contains("rate_limit"))
                return "Prea multe cereri. Te rog încearcă din nou în câteva secunde.";
            if (ex.Message.Contains("insufficient_quota"))
                return "Creditul API a fost epuizat. Te rog contactează administratorul.";

            return "Ne pare rău, a apărut o eroare în comunicarea cu serviciul AI. Te rog încearcă din nou.";
        }

        public void Dispose()
        {
            _chatClient = null;
        }
    }
}
