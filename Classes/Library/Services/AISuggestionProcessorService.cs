using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.DTO.AI;
using MauiAppDisertatieVacantaAI.Classes.Services;
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
        /// Găsește o destinație existentă sau creează una nouă minimă/completă
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

                // Creează destinație (minimă sau completă în funcție de DestinatieData)
                System.Diagnostics.Debug.WriteLine("Destination doesn't exist - creating new destination...");

                var newDestination = new Destinatie
                {
                    Denumire = suggestionData.DestinatieDenumire ?? suggestionData.DestinatieOras,
                    Tara = suggestionData.DestinatieTara,
                    Oras = suggestionData.DestinatieOras,
                    Regiune = suggestionData.DestinatieData?.Regiune ?? "Necunoscut",
                    Descriere = suggestionData.DestinatieData?.Descriere ?? $"Destinație pentru {suggestionData.DestinatieOras}, {suggestionData.DestinatieTara}",
                    PretAdult = suggestionData.DestinatieData?.PretAdult ?? suggestionData.BugetEstimat,
                    PretMinor = suggestionData.DestinatieData?.PretMinor ?? (suggestionData.BugetEstimat / 2),
                    Data_Inregistrare = DateTime.Now
                };

                // Validează lungimile
                if (newDestination.Denumire.Length > 50) newDestination.Denumire = newDestination.Denumire.Substring(0, 50);
                if (newDestination.Tara.Length > 50) newDestination.Tara = newDestination.Tara.Substring(0, 50);
                if (newDestination.Oras.Length > 50) newDestination.Oras = newDestination.Oras.Substring(0, 50);
                if (newDestination.Descriere?.Length > 4000) newDestination.Descriere = newDestination.Descriere.Substring(0, 4000);

                _destinatieRepo.Insert(newDestination);

                System.Diagnostics.Debug.WriteLine($"✅ Created new destination with ID: {newDestination.Id_Destinatie}");

                // Dacă avem DestinatieData, procesează și datele suplimentare (imagini, POI-uri, etc.)
                if (suggestionData.DestinatieData != null)
                {
                    System.Diagnostics.Debug.WriteLine("Processing full destination data (images, POIs, facilities, categories)...");

                    // Procesează imaginile
                    if (suggestionData.DestinatieData.PhotoSearchQueries != null && suggestionData.DestinatieData.PhotoSearchQueries.Any())
                    {
                        await AddDestinationImagesAsync(newDestination.Id_Destinatie, suggestionData.DestinatieData.PhotoSearchQueries);
                    }

                    // Procesează categoriile
                    if (suggestionData.DestinatieData.Categorii != null && suggestionData.DestinatieData.Categorii.Any())
                    {
                        await ProcessCategoriesAsync(newDestination.Id_Destinatie, suggestionData.DestinatieData.Categorii);
                    }

                    // Procesează facilitățile
                    if (suggestionData.DestinatieData.Facilitati != null && suggestionData.DestinatieData.Facilitati.Any())
                    {
                        await ProcessFacilitiesAsync(newDestination.Id_Destinatie, suggestionData.DestinatieData.Facilitati);
                    }

                    // Procesează punctele de interes
                    if (suggestionData.DestinatieData.PuncteDeInteres != null && suggestionData.DestinatieData.PuncteDeInteres.Any())
                    {
                        await ProcessPointsOfInterestAsync(newDestination.Id_Destinatie, suggestionData.DestinatieData.PuncteDeInteres);
                    }
                }
                else
                {

                    // Creează query-uri de căutare bazate pe destinație (separat: Țară și Oraș)
                    var fallbackQueries = new List<string>
                    {
                        suggestionData.DestinatieTara,     // Ex: "Franța"
                        suggestionData.DestinatieOras,     // Ex: "Paris"
                    };
                    await AddDestinationImagesAsync(newDestination.Id_Destinatie, fallbackQueries);
                }

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
        /// Adaugă imagini pentru destinație folosind Pexels API
        /// </summary>
        private async Task AddDestinationImagesAsync(int destinatieId, List<string> searchQueries)
        {
            try
            {
                if (searchQueries == null || !searchQueries.Any()) return;

                var imaginiRepo = new ImaginiDestinatieRepository();

                foreach (var query in searchQueries.Take(3)) // Max 3 queries
                {
                    var photos = PhotoAPIUtils.SearchPhotos(query, 2, 1); // 2 photos per query

                    if (photos?.Photos != null)
                    {
                        foreach (var photo in photos.Photos.Take(2))
                        {
                            var imageUrl = photo.Src?.Medium ?? photo.Src?.Original;
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                var imagineDestinatie = new ImaginiDestinatie
                                {
                                    Id_Destinatie = destinatieId,
                                    ImagineUrl = imageUrl
                                };

                                imaginiRepo.Insert(imagineDestinatie);
                                System.Diagnostics.Debug.WriteLine($"Added image for destination {destinatieId}: {imageUrl}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding destination images: {ex.Message}");
            }
        }

        /// <summary>
        /// Procesează categoriile destinației (doar existente din DB)
        /// </summary>
        private async Task ProcessCategoriesAsync(int destinatieId, List<string> categorii)
        {
            try
            {
                if (categorii == null || !categorii.Any()) return;

                var categorieRepo = new CategorieVacantaRepository();
                var catDestRepo = new CategorieVacanta_DestinatieRepository();
                var existingCategories = categorieRepo.GetAll().ToList();

                foreach (var categorieName in categorii)
                {
                    var existingCategory = existingCategories.FirstOrDefault(c =>
 c.Denumire.Equals(categorieName, StringComparison.OrdinalIgnoreCase));

                    if (existingCategory != null)
                    {
                        var catDest = new CategorieVacanta_Destinatie
                        {
                            Id_Destinatie = destinatieId,
                            Id_CategorieVacanta = existingCategory.Id_CategorieVacanta
                        };

                        catDestRepo.Insert(catDest);
                        System.Diagnostics.Debug.WriteLine($"Linked category '{categorieName}' to destination {destinatieId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing categories: {ex.Message}");
            }
        }

        /// <summary>
        /// Procesează facilitățile (existente sau noi)
        /// </summary>
        private async Task ProcessFacilitiesAsync(int destinatieId, List<string> facilitati)
        {
            try
            {
                if (facilitati == null || !facilitati.Any()) return;

                var facilitateRepo = new FacilitateRepository();
                var destFacilRepo = new DestinatieFacilitateRepository();
                var existingFacilities = facilitateRepo.GetAll().ToList();

                foreach (var facilityName in facilitati)
                {
                    var existingFacility = existingFacilities.FirstOrDefault(f =>
       f.Denumire.Equals(facilityName, StringComparison.OrdinalIgnoreCase));

                    int facilityId;

                    if (existingFacility != null)
                    {
                        facilityId = existingFacility.Id_Facilitate;
                    }
                    else
                    {
                        // Creează facilitate nouă
                        var newFacility = new Facilitate
                        {
                            Denumire = facilityName,
                            Descriere = $"Facilitate pentru {facilityName}"
                        };

                        facilitateRepo.Insert(newFacility);
                        facilityId = newFacility.Id_Facilitate;
                        System.Diagnostics.Debug.WriteLine($"Created new facility: {facilityName} with ID: {facilityId}");
                    }

                    // Leagă facilitea de destinație
                    var destFacil = new DestinatieFacilitate
                    {
                        Id_Destinatie = destinatieId,
                        Id_Facilitate = facilityId
                    };

                    destFacilRepo.Insert(destFacil);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing facilities: {ex.Message}");
            }
        }

        /// <summary>
        /// Procesează punctele de interes cu imagini
        /// </summary>
        private async Task ProcessPointsOfInterestAsync(int destinatieId, List<PointOfInterestData> puncteDeInteres)
        {
            try
            {
                if (puncteDeInteres == null || !puncteDeInteres.Any()) return;

                var poiRepo = new PunctDeInteresRepository();
                var imaginiPoiRepo = new ImaginiPunctDeInteresRepository();

                foreach (var poi in puncteDeInteres)
                {
                    // Creează punctul de interes
                    var punctDeInteres = new PunctDeInteres
                    {
                        Denumire = poi.Denumire,
                        Descriere = poi.Descriere,
                        Tip = poi.Tip,
                        Id_Destinatie = destinatieId
                    };

                    poiRepo.Insert(punctDeInteres);
                    var poiId = punctDeInteres.Id_PunctDeInteres;

                    System.Diagnostics.Debug.WriteLine($"Created POI: {poi.Denumire} with ID: {poiId}");

                    // Adaugă imagini pentru punctul de interes
                    if (poi.PhotoSearchQueries != null && poi.PhotoSearchQueries.Any())
                    {
                        foreach (var query in poi.PhotoSearchQueries.Take(2))
                        {
                            try
                            {
                                var photos = PhotoAPIUtils.SearchPhotos(query, 1, 1);

                                if (photos?.Photos != null && photos.Photos.Any())
                                {
                                    var photo = photos.Photos.FirstOrDefault();
                                    var imageUrl = photo?.Src?.Medium ?? photo?.Src?.Original;

                                    if (!string.IsNullOrEmpty(imageUrl))
                                    {
                                        var imaginePoi = new ImaginiPunctDeInteres
                                        {
                                            Id_PunctDeInteres = poiId,
                                            ImagineUrl = imageUrl
                                        };

                                        imaginiPoiRepo.Insert(imaginePoi);
                                        System.Diagnostics.Debug.WriteLine($"Added image for POI {poiId}: {imageUrl}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error processing photo for POI {poiId}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing points of interest: {ex.Message}");
            }
        }

        /// <summary>
        /// Curăță răspunsul JSON de la AI
        /// </summary>
        private string CleanJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            // ❌ NU ELIMINA DIACRITICE! Păstrează doar caracterele JSON invalide
            // var cleaned = System.Text.RegularExpressions.Regex.Replace(json, @"[^\u0000-\u007F]+", string.Empty);

            // ✅ Doar elimină caracterele de control problematice (null, backspace, etc.)
            var cleaned = System.Text.RegularExpressions.Regex.Replace(json, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", string.Empty);

            return cleaned.Trim();
        }

        /// <summary>
        /// Înregistrează excepția în log (ex: ZexceptionLogger)
        /// </summary>
        private void LogException(Exception ex)
        {
            try
            {
                // Exemplu simplu de logare a excepției
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Aici poți adăuga și logica pentru a trimite excepția către un serviciu extern de logare
                // ex: ZexceptionLogger.Log(ex);
            }
            catch (Exception loggingEx)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging exception: {loggingEx.Message}");
            }
        }
    }

    /// <summary>
    /// Rezultatul procesării sugestiilor
    /// </summary>
    public class SuggestionProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int SuggestionId { get; set; }
        public int DestinationId { get; set; }
    }
}
