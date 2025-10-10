// Exemplu de integrare în pagina de chat
using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using System.Linq;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public class ChatDestinationHandler
    {
        private readonly OpenAIService _openAIService;
        private readonly AIDestinationProcessorService _processorService;
        private readonly DestinatieRepository _destinatieRepo;

        public ChatDestinationHandler()
        {
            _openAIService = new OpenAIService();
            _processorService = new AIDestinationProcessorService();
            _destinatieRepo = new DestinatieRepository();
        }

        /// <summary>
        /// Verifică dacă utilizatorul cere crearea unei destinații și o procesează
        /// </summary>
        public async Task<(bool IsDestinationRequest, string Response)> HandleUserMessageAsync(string userMessage)
        {
            // Trimitem TOATE mesajele către AI pentru a decide dacă e o cerere pentru destinație
            // AI-ul va decide singur dacă să creeze o destinație sau să răspundă normal
            return await ProcessDestinationRequestAsync(userMessage);
        }

        private async Task<(bool IsDestinationRequest, string Response)> ProcessDestinationRequestAsync(string userMessage)
        {
            try
            {
                // 1. Obține lista destinațiilor existente
                var existingDestinations = GetExistingDestinationsForPrompt();

                // 2. Obține lista categoriilor disponibile
                var availableCategories = GetAvailableCategoriesForPrompt();

                // 3. Trimite cererea către AI
                var aiJsonResponse = await _openAIService.GetDestinationCreationResponseAsync(
                    userMessage, existingDestinations, availableCategories);

                // 4. Procesează răspunsul AI
                var result = await _processorService.ProcessAIResponseAsync(aiJsonResponse);

                // 5. Verifică tipul de răspuns
                if (result.IsGeneralChat)
                {
                    // Este un răspuns de chat general, nu o destinație
                    return (false, result.Message);
                }
                else if (result.Success || result.DestinationId > 0)
                {
                    // Este o destinație (creată cu succes sau existentă)
                    return (true, result.Message);
                }
                else
                {
                    // Este o eroare la crearea destinației
                    return (true, result.Message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing destination request: {ex.Message}");
                
                // Fallback response
                var fallbackResponse = GenerateFallbackDestinationResponse(userMessage);
                return (false, fallbackResponse); // Returnează ca chat general în caz de eroare
            }
        }

        /// <summary>
        /// Generează un răspuns de rezervă când AI-ul nu funcționează corect
        /// </summary>
        private string GenerateFallbackDestinationResponse(string userMessage)
        {
            // Extrage numele destinației din mesaj pentru răspunsul personalizat
            var destinationName = ExtractDestinationName(userMessage);
            
            if (!string.IsNullOrEmpty(destinationName))
            {
                return $"Am înțeles că ești interesant de {destinationName}! Momentan nu pot crea automat destinația, dar poți explora destinațiile existente din aplicație sau să îmi spui mai multe detalii despre ce anume te interesează la această destinație.";
            }
            
            return "Îmi pare rău, momentan nu pot procesa cererea ta corespunzător. Poți să îmi spui mai multe detalii despre ce te interesează sau să explorezi destinațiile disponibile în aplicație?";
        }

        /// <summary>
        /// Încearcă să extraga numele destinației din mesajul utilizatorului
        /// </summary>
        private string ExtractDestinationName(string message)
        {
            try
            {
                var lowerMessage = message.ToLower();
                
                // Cuvinte cheie care indică o destinație
                var patterns = new[]
                {
                    @"(?:la|în|din)\s+([A-Za-zĂăÂâÎîȘșȚț\s]+?)(?:\s|$|!|\?|,|\.|;)",
                    @"(?:merg la|vizitez|călătoresc la|vacanță în)\s+([A-Za-zĂăÂâÎîȘșȚț\s]+?)(?:\s|$|!|\?|,|\.|;)",
                    @"([A-Za-zĂăÂâÎîȘșȚț]+?)(?:\s+(?:pentru|city|trip|vacation|vacanță))"
                };

                foreach (var pattern in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        var destination = match.Groups[1].Value.Trim();
                        if (destination.Length > 2 && destination.Length < 50) // Validare de bază
                        {
                            return destination;
                        }
                    }
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetExistingDestinationsForPrompt()
        {
            try
            {
                var destinations = _destinatieRepo.GetAll().Take(20); // Max 20 pentru prompt
                var destinationList = destinations.Select(d => 
                    $"{d.Denumire}, {d.Oras}, {d.Tara}").ToList();
                
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
                var categorieRepo = new CategorieVacantaRepository();
                var categories = categorieRepo.GetAll().Take(20); // Max 20 categorii pentru prompt
                var categoryList = categories.Select(c => c.Denumire).ToList();
                
                System.Diagnostics.Debug.WriteLine($"Available categories: {string.Join(", ", categoryList)}");
                
                if (!categoryList.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No categories found in database!");
                    return "Nu există categorii în sistem";
                }
                
                return string.Join(", ", categoryList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting categories: {ex.Message}");
                return "Nu există categorii în sistem";
            }
        }
    }
}