using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.DTO.AI;
using Newtonsoft.Json;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    /// <summary>
    /// Serviciu pentru procesarea sugestiilor de vacanță generate de AI
    /// </summary>
    public class AISuggestionProcessorService
    {
        private readonly SugestieRepository _sugestieRepo;
        private readonly DestinatieRepository _destinatieRepo;

        public AISuggestionProcessorService()
        {
            _sugestieRepo = new SugestieRepository();
            _destinatieRepo = new DestinatieRepository();
        }

        /// <summary>
        /// Procesează răspunsul AI pentru sugestii și creează sugestia în baza de date
        /// </summary>
        public async Task<SuggestionProcessResult> ProcessAISuggestionAsync(string jsonResponse, int userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Processing AI Suggestion ===");
                System.Diagnostics.Debug.WriteLine($"User ID: {userId}");
                System.Diagnostics.Debug.WriteLine($"Raw response length: {jsonResponse?.Length ?? 0}");

                // Curăță JSON-ul
                var cleanedJson = CleanJson(jsonResponse);
                if (string.IsNullOrEmpty(cleanedJson))
                {
                    return new SuggestionProcessResult
                    {
                        Success = false,
                        Message = "Nu s-a putut extrage un JSON valid din răspunsul AI-ului"
                    };
                }

                // Parsează răspunsul
                var aiResponse = JsonConvert.DeserializeObject<AIDestinationResponse>(cleanedJson);
                if (aiResponse == null || aiResponse.Suggestion == null)
                {
                    return new SuggestionProcessResult
                    {
                        Success = false,
                        Message = "Datele sugestiei lipsesc din răspunsul AI-ului"
                    };
                }

                // Creează sugestia
                return await CreateSuggestionAsync(aiResponse.Suggestion, aiResponse.Message, userId);
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
                return new SuggestionProcessResult
                {
                    Success = false,
                    Message = "Răspunsul AI-ului nu este în format JSON valid"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing suggestion: {ex.Message}");
                return new SuggestionProcessResult
                {
                    Success = false,
                    Message = $"Eroare la procesarea sugestiei: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Creează o sugestie de vacanță (cu destinația aferentă dacă nu există)
        /// </summary>
        private async Task<SuggestionProcessResult> CreateSuggestionAsync(SuggestionData suggestionData, string aiGeneratedMessage, int userId)
        {
            try
            {
                // Validare date
                if (string.IsNullOrWhiteSpace(suggestionData?.Titlu) ||
                     string.IsNullOrWhiteSpace(suggestionData?.Descriere) ||
                           string.IsNullOrWhiteSpace(suggestionData?.DestinatieOras) ||
                             string.IsNullOrWhiteSpace(suggestionData?.DestinatieTara))
                {
                    return new SuggestionProcessResult
                    {
                        Success = false,
                        Message = "Date incomplete pentru sugestie. Lipsesc titlul, descrierea sau destinația."
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Creating suggestion: '{suggestionData.Titlu}'");
                System.Diagnostics.Debug.WriteLine($"Destination: {suggestionData.DestinatieOras}, {suggestionData.DestinatieTara}");
                System.Diagnostics.Debug.WriteLine($"Budget: {suggestionData.BugetEstimat} EUR");

                // ===== PASUL 1: GĂSEȘTE SAU CREEAZĂ DESTINAȚIA =====
                int destinationId = await GetOrCreateDestinationAsync(suggestionData);

                if (destinationId <= 0)
                {
                    return new SuggestionProcessResult
                    {
                        Success = false,
                        Message = "Nu s-a putut găsi sau crea destinația pentru această sugestie"
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Using destination ID: {destinationId}");

                // ===== PASUL 2: CREEAZĂ SUGESTIA =====
                var sugestie = new Sugestie
                {
                    Titlu = suggestionData.Titlu.Length > 50 ? suggestionData.Titlu.Substring(0, 50) : suggestionData.Titlu,
                    Buget_Estimat = suggestionData.BugetEstimat,
                    Descriere = suggestionData.Descriere.Length > 4000 ? suggestionData.Descriere.Substring(0, 4000) : suggestionData.Descriere,
                    Id_Destinatie = destinationId,
                    Id_Utilizator = userId,
                    EsteGenerataDeAI = 1, // Generată de AI
                    EstePublic = suggestionData.EstePublic ?? 0, // Default privat
                    Data_Inregistrare = DateTime.Now,
                    CodPartajare = null // Nu generăm cod de partajare acum
                };

                _sugestieRepo.Insert(sugestie);

                System.Diagnostics.Debug.WriteLine($"✅ Suggestion created successfully with ID: {sugestie.Id_Sugestie}");

                // ===== PASUL 3: RETURNEAZĂ REZULTATUL =====
                string finalMessage = !string.IsNullOrWhiteSpace(aiGeneratedMessage)
                     ? aiGeneratedMessage
                  : $"🎉 Perfect! Am creat planul tău \"{sugestie.Titlu}\" pentru {suggestionData.DestinatieOras}!\n\n✨ Verifică sugestia în secțiunea Sugestii pentru detalii complete.";

                return new SuggestionProcessResult
                {
                    Success = true,
                    Message = finalMessage,
                    SuggestionId = sugestie.Id_Sugestie,
                    DestinationId = destinationId
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating suggestion: {ex.Message}");
                LogException(ex);

                return new SuggestionProcessResult
                {
                    Success = false,
                    Message = "Ne pare rău, nu am putut crea planul de vacanță. Te rog încearcă din nou."
                };
            }
        }

        /// <summary>
        /// Găsește o destinație existentă sau creează una nouă minimă
        /// </summary>
        private async Task<int> GetOrCreateDestinationAsync(SuggestionData suggestionData)
        {
            try
            {
                // Caută destinația existentă
                var existingDestination = _destinatieRepo.GetAll()
               .FirstOrDefault(d =>
                 d.Oras.Equals(suggestionData.DestinatieOras, StringComparison.OrdinalIgnoreCase) &&
            d.Tara.Equals(suggestionData.DestinatieTara, StringComparison.OrdinalIgnoreCase));

                if (existingDestination != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found existing destination: {existingDestination.Denumire} (ID: {existingDestination.Id_Destinatie})");
                    return existingDestination.Id_Destinatie;
                }

                // Creează destinație minimă
                System.Diagnostics.Debug.WriteLine("Destination doesn't exist - creating minimal destination...");

                var newDestination = new Destinatie
                {
                    Denumire = suggestionData.DestinatieDenumire ?? suggestionData.DestinatieOras,
                    Tara = suggestionData.DestinatieTara,
                    Oras = suggestionData.DestinatieOras,
                    Regiune = "Necunoscut",
                    Descriere = $"Destinație pentru {suggestionData.DestinatieOras}, {suggestionData.DestinatieTara}",
                    PretAdult = suggestionData.BugetEstimat,
                    PretMinor = suggestionData.BugetEstimat / 2,
                    Data_Inregistrare = DateTime.Now
                };

                // Validează lungimile
                if (newDestination.Denumire.Length > 50) newDestination.Denumire = newDestination.Denumire.Substring(0, 50);
                if (newDestination.Tara.Length > 50) newDestination.Tara = newDestination.Tara.Substring(0, 50);
                if (newDestination.Oras.Length > 50) newDestination.Oras = newDestination.Oras.Substring(0, 50);

                _destinatieRepo.Insert(newDestination);

                System.Diagnostics.Debug.WriteLine($"✅ Created new destination with ID: {newDestination.Id_Destinatie}");
                return newDestination.Id_Destinatie;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting/creating destination: {ex.Message}");
                LogException(ex);
                return 0;
            }
        }

        /// <summary>
        /// Curăță JSON-ul din răspunsul AI
        /// </summary>
        private string CleanJson(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            try
            {
                var startIndex = response.IndexOf('{');
                var lastIndex = response.LastIndexOf('}');

                if (startIndex == -1 || lastIndex == -1 || startIndex >= lastIndex)
                    return response.Trim();

                return response.Substring(startIndex, lastIndex - startIndex + 1).Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning JSON: {ex.Message}");
                return response.Trim();
            }
        }

        /// <summary>
        /// Loghează excepțiile cu detalii complete
        /// </summary>
        private void LogException(Exception ex)
        {
            var currentEx = ex;
            var level = 0;
            while (currentEx != null)
            {
                System.Diagnostics.Debug.WriteLine($"Exception level {level}: {currentEx.GetType().Name}: {currentEx.Message}");
                currentEx = currentEx.InnerException;
                level++;
            }
        }
    }

    /// <summary>
    /// Rezultatul procesării unei sugestii
    /// </summary>
    public class SuggestionProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int SuggestionId { get; set; }
        public int DestinationId { get; set; }
    }
}
