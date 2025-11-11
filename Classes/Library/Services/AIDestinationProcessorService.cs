using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.DTO.AI;
using MauiAppDisertatieVacantaAI.Classes.Services;
using Newtonsoft.Json;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public class AIDestinationProcessorService
    {
        private readonly DestinatieRepository _destinatieRepo;
        private readonly ImaginiDestinatieRepository _imaginiDestRepo;
        private readonly CategorieVacantaRepository _categorieRepo;
        private readonly FacilitateRepository _facilitateRepo;
        private readonly CategorieVacanta_DestinatieRepository _catDestRepo;
        private readonly DestinatieFacilitateRepository _destFacilRepo;
        private readonly PunctDeInteresRepository _poiRepo;
        private readonly ImaginiPunctDeInteresRepository _imaginiPoiRepo;

        public AIDestinationProcessorService()
        {
            _destinatieRepo = new DestinatieRepository();
            _imaginiDestRepo = new ImaginiDestinatieRepository();
            _categorieRepo = new CategorieVacantaRepository();
            _facilitateRepo = new FacilitateRepository();
            _catDestRepo = new CategorieVacanta_DestinatieRepository();
            _destFacilRepo = new DestinatieFacilitateRepository();
            _poiRepo = new PunctDeInteresRepository();
            _imaginiPoiRepo = new ImaginiPunctDeInteresRepository();
        }

        /// <summary>
        /// Procesează răspunsul JSON de la AI și creează destinația SAU sugestia în baza de date
        /// </summary>
        public async Task<ProcessResult> ProcessAIResponseAsync(string jsonResponse, int userId)
        {
            try
            {
                // Log răspunsul pentru debugging
                System.Diagnostics.Debug.WriteLine($"Raw AI response: {jsonResponse}");
                System.Diagnostics.Debug.WriteLine($"Processing for user ID: {userId}");

                // Curăță și extrage JSON-ul din răspuns
                var cleanedJson = ExtractAndCleanJson(jsonResponse);

                if (string.IsNullOrEmpty(cleanedJson))
                {
                    return new ProcessResult
                    {
                        Success = false,
                        Message = "Nu s-a putut extrage un JSON valid din răspunsul AI-ului"
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Cleaned JSON: {cleanedJson}");

                // Verifică dacă JSON-ul pare valid înainte de a încerca parsing-ul
                if (!IsValidJsonStructure(cleanedJson))
                {
                    System.Diagnostics.Debug.WriteLine($"JSON structure validation failed for: {cleanedJson}");
                    return new ProcessResult
                    {
                        Success = false,
                        Message = "Răspunsul AI-ului nu are o structură JSON validă. Te rog încearcă din nou cu o cerere mai specifică."
                    };
                }

                var aiResponse = JsonConvert.DeserializeObject<AIDestinationResponse>(cleanedJson);

                if (aiResponse == null)
                {
                    return new ProcessResult
                    {
                        Success = false,
                        Message = "Răspunsul AI nu a putut fi procesat corect"
                    };
                }

                // Verifică dacă răspunsul este valid
                if (!aiResponse.Success)
                {
                    return new ProcessResult
                    {
                        Success = false,
                        Message = aiResponse.Message ?? "AI-ul a raportat o eroare"
                    };
                }

                switch (aiResponse.Action?.ToLower())
                {
                    case "create_destination":
                        if (aiResponse.Destination == null)
                        {
                            return new ProcessResult
                            {
                                Success = false,
                                Message = "Datele destinației lipsesc din răspunsul AI-ului"
                            };
                        }
                        // Trimite mesajul din AI împreună cu datele destinației
                        return await CreateDestinationAsync(aiResponse.Destination, aiResponse.Message);

                    case "create_suggestion":
                        // Sugestiile sunt procesate de AISuggestionProcessorService
                        return new ProcessResult
                        {
                            Success = false,
                            Message = "Sugestiile trebuie procesate prin AISuggestionProcessorService"
                        };

                    case "ask_preference":
                        // NOU - AI cere preferințe utilizatorului
                        return new ProcessResult
                        {
                            Success = true,
                            Message = aiResponse.Message ?? "Am câteva recomandări pentru tine!",
                            DestinationId = 0,
                            IsGeneralChat = false,
                            IsAskPreference = true // NOU - flag pentru UI
                        };

                    case "destination_exists":
                        return new ProcessResult
                        {
                            Success = true,
                            Message = aiResponse.Message,
                            DestinationId = aiResponse.Destination != null ? await FindExistingDestinationAsync(aiResponse.Destination) : 0
                        };

                    case "general_chat":
                        return new ProcessResult
                        {
                            Success = false, // Marchează ca false pentru a nu fi tratat ca destinație
                            Message = aiResponse.Message ?? "Răspuns general de la AI",
                            DestinationId = 0,
                            IsGeneralChat = true
                        };

                    case "error":
                        return new ProcessResult
                        {
                            Success = false,
                            Message = aiResponse.Message ?? "AI-ul a raportat o eroare necunoscută"
                        };

                    default:
                        return new ProcessResult
                        {
                            Success = false,
                            Message = $"Acțiune necunoscută din partea AI-ului: {aiResponse.Action}"
                        };
                }
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Problematic JSON: {jsonResponse}");
                return new ProcessResult
                {
                    Success = false,
                    Message = "Răspunsul AI-ului nu este în format JSON valid. Te rog încearcă din nou cu o cerere mai specifică."
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing AI response: {ex.Message}");
                return new ProcessResult
                {
                    Success = false,
                    Message = $"Eroare la procesarea răspunsului: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Verifică dacă string-ul are o structură JSON validă de bază
        /// </summary>
        private bool IsValidJsonStructure(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            var trimmed = json.Trim();

            // Verifică dacă începe și se termină cu acolade
            if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
                return false;

            // Verifică pentru caractere problematice care pot cauza erori de parsing
            var problematicChars = new[] { "\\\n", "\\\r", "\\\\n", "\\\\r", "\\\\t" };
            if (problematicChars.Any(c => trimmed.Contains(c)))
                return false;

            // Verifică pentru ghilimele neînchise (aproximativ)
            var quoteCount = trimmed.Count(c => c == '"');
            if (quoteCount % 2 != 0)
                return false;

            return true;
        }

        /// <summary>
        /// Extrage și curăță JSON-ul din răspunsul AI-ului
        /// </summary>
        private string ExtractAndCleanJson(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            try
            {
                // Găsește primul { și ultimul }
                var startIndex = response.IndexOf('{');
                var lastIndex = response.LastIndexOf('}');

                if (startIndex == -1 || lastIndex == -1 || startIndex >= lastIndex)
                {
                    // Încearcă să găsească ```json sau ``` blocks
                    var jsonBlockStart = response.IndexOf("```json");
                    if (jsonBlockStart != -1)
                    {
                        var jsonStart = response.IndexOf('{', jsonBlockStart);
                        var jsonBlockEnd = response.IndexOf("```", jsonBlockStart + 7);
                        if (jsonStart != -1 && jsonBlockEnd != -1)
                        {
                            var jsonEnd = response.LastIndexOf('}', jsonBlockEnd);
                            if (jsonEnd > jsonStart)
                            {
                                return response.Substring(jsonStart, jsonEnd - jsonStart + 1).Trim();
                            }
                        }
                    }

                    // Dacă nu găsește delimitatori, returnează răspunsul original curățat
                    return response.Trim();
                }

                var jsonString = response.Substring(startIndex, lastIndex - startIndex + 1);

                // Nu modifică caracterele din interiorul JSON-ului - doar returnează string-ul extras
                return jsonString.Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting JSON: {ex.Message}");
                return response.Trim();
            }
        }

        private async Task<ProcessResult> CreateDestinationAsync(DestinationData destinationData, string aiGeneratedMessage = null)
        {
            try
            {
                // Validare date destinație
                if (string.IsNullOrWhiteSpace(destinationData?.Denumire) ||
                    string.IsNullOrWhiteSpace(destinationData?.Oras) ||
                    string.IsNullOrWhiteSpace(destinationData?.Tara))
                {
                    return new ProcessResult
                    {
                        Success = false,
                        Message = "Date incomplete pentru destinație. Lipsesc denumirea, orașul sau țara."
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Creating destination: {destinationData.Denumire}, {destinationData.Oras}, {destinationData.Tara}");
                System.Diagnostics.Debug.WriteLine($"AI Generated Message: {aiGeneratedMessage ?? "NULL"}");

                // 1. Verifică dacă destinația există deja
                System.Diagnostics.Debug.WriteLine("Checking if destination already exists...");
                var existingDest = await FindExistingDestinationAsync(destinationData);
                if (existingDest > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Destination already exists with ID: {existingDest}");
                    return new ProcessResult
                    {
                        Success = true,
                        Message = GenerateFriendlyExistingMessage(destinationData.Denumire),
                        DestinationId = existingDest
                    };
                }

                // 2. Creează destinația cu validări pentru proprietăți null
                var destinatie = new Destinatie
                {
                    Denumire = destinationData.Denumire.Trim(),
                    Tara = destinationData.Tara?.Trim() ?? "Necunoscut",
                    Oras = destinationData.Oras?.Trim() ?? "Necunoscut",
                    Regiune = destinationData.Regiune?.Trim() ?? "Necunoscut",
                    Descriere = destinationData.Descriere?.Trim() ?? $"Destinație în {destinationData.Oras}, {destinationData.Tara}",
                    PretAdult = Math.Max(destinationData.PretAdult, 0),
                    PretMinor = Math.Max(destinationData.PretMinor, 0),
                    Data_Inregistrare = DateTime.Now
                };

                // Ensure strings don't exceed maximum length
                if (destinatie.Denumire.Length > 50) destinatie.Denumire = destinatie.Denumire.Substring(0, 50);
                if (destinatie.Tara.Length > 50) destinatie.Tara = destinatie.Tara.Substring(0, 50);
                if (destinatie.Oras.Length > 50) destinatie.Oras = destinatie.Oras.Substring(0, 50);
                if (destinatie.Regiune.Length > 50) destinatie.Regiune = destinatie.Regiune.Substring(0, 50);
                if (destinatie.Descriere?.Length > 4000) destinatie.Descriere = destinatie.Descriere.Substring(0, 4000);

                System.Diagnostics.Debug.WriteLine($"Inserting destination with: Name={destinatie.Denumire}, Country={destinatie.Tara}, City={destinatie.Oras}");

                try
                {
                    System.Diagnostics.Debug.WriteLine($"About to insert destination: {destinatie.Denumire}");
                    _destinatieRepo.Insert(destinatie);
                    System.Diagnostics.Debug.WriteLine("Destination inserted successfully");

                    // ID-ul a fost generat manual de repository și este acum disponibil
                    System.Diagnostics.Debug.WriteLine($"Destination created with ID: {destinatie.Id_Destinatie}");
                }
                catch (Exception insertEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error inserting destination: {insertEx.Message}");

                    // Log complete exception chain
                    var currentEx = insertEx;
                    var level = 0;
                    while (currentEx != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Exception level {level}: {currentEx.GetType().Name}: {currentEx.Message}");
                        if (!string.IsNullOrEmpty(currentEx.StackTrace))
                        {
                            System.Diagnostics.Debug.WriteLine($"Stack trace level {level}: {currentEx.StackTrace}");
                        }
                        currentEx = currentEx.InnerException;
                        level++;
                    }

                    throw;
                }

                // ID-ul este acum disponibil direct din obiectul destinatie
                var destinatieId = destinatie.Id_Destinatie;
                if (destinatieId <= 0)
                {
                    throw new Exception($"ID-ul destinației nu a fost generat corect: {destinatieId}");
                }

                System.Diagnostics.Debug.WriteLine($"Proceeding with destination ID: {destinatieId}");
                System.Diagnostics.Debug.WriteLine($"Destination created with ID: {destinatieId}");

                var errors = new List<string>();

                // Pentru debugging, să încercăm să creăm o destinație minimă mai întâi
                System.Diagnostics.Debug.WriteLine("=== Starting to process additional destination data ===");

                // 3. Adaugă imagini folosind Pexels (doar dacă există query-uri)
                try
                {
                    System.Diagnostics.Debug.WriteLine("Starting image processing...");
                    if (destinationData.PhotoSearchQueries != null && destinationData.PhotoSearchQueries.Any())
                    {
                        await AddDestinationImagesAsync(destinatieId, destinationData.PhotoSearchQueries);
                    }
                    else
                    {
                        // Fallback search queries
                        var fallbackQueries = new List<string>
                        {
                            $"{destinationData.Oras} {destinationData.Tara}",
                            $"{destinationData.Oras} tourism",
                            $"{destinationData.Tara} travel"
                        };
                        await AddDestinationImagesAsync(destinatieId, fallbackQueries);
                    }
                    System.Diagnostics.Debug.WriteLine("Image processing completed successfully");
                }
                catch (Exception imgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding images: {imgEx.Message}");
                    errors.Add("imagini");
                }

                // 4. Procesează categoriile (doar dacă există)
                try
                {
                    if (destinationData.Categorii != null && destinationData.Categorii.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Processing categories: {string.Join(", ", destinationData.Categorii)}");
                        await ProcessCategoriesAsync(destinatieId, destinationData.Categorii);
                    }
                }
                catch (Exception catEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing categories: {catEx.Message}");
                    errors.Add("categorii");
                }

                // 5. Procesează facilitățile (doar dacă există)
                try
                {
                    if (destinationData.Facilitati != null && destinationData.Facilitati.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Processing facilities: {string.Join(", ", destinationData.Facilitati)}");
                        await ProcessFacilitiesAsync(destinatieId, destinationData.Facilitati);
                    }
                }
                catch (Exception facEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing facilities: {facEx.Message}");
                    errors.Add("facilități");
                }

                // 6. Adaugă puncte de interes (doar dacă există)
                try
                {
                    if (destinationData.PuncteDeInteres != null && destinationData.PuncteDeInteres.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Processing {destinationData.PuncteDeInteres.Count} points of interest");
                        await ProcessPointsOfInterestAsync(destinatieId, destinationData.PuncteDeInteres);
                    }
                }
                catch (Exception poiEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing points of interest: {poiEx.Message}");
                    errors.Add("puncte de interes");
                }

                // FOLOSEȘTE MESAJUL DIN AI (obligatoriu!)
                string finalMessage;
                if (!string.IsNullOrWhiteSpace(aiGeneratedMessage))
                {
                    System.Diagnostics.Debug.WriteLine("Using AI-generated message from JSON");
                    finalMessage = aiGeneratedMessage;

                    // Adaugă warning pentru erori parțiale dacă există
                    if (errors.Any())
                    {
                        finalMessage += $"\n\n⚠️ Notă: Unele detalii ({string.Join(", ", errors)}) nu au putut fi adăugate, dar destinația principală este funcțională!";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ WARNING: AI message was empty!");
                    finalMessage = $"Am creat destinația {destinationData.Denumire}, dar mesajul detaliat lipsește. Verifică destinația în aplicație.";
                }

                return new ProcessResult
                {
                    Success = true,
                    Message = finalMessage,
                    DestinationId = destinatieId
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating destination: {ex.Message}");

                // Log complete exception chain
                var currentEx = ex;
                var level = 0;
                while (currentEx != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception level {level}: {currentEx.GetType().Name}: {currentEx.Message}");
                    if (!string.IsNullOrEmpty(currentEx.StackTrace))
                    {
                        // Only log first few lines of stack trace to avoid spam
                        var stackLines = currentEx.StackTrace.Split('\n').Take(3);
                        System.Diagnostics.Debug.WriteLine($"Stack trace level {level}: {string.Join(" | ", stackLines)}");
                    }
                    currentEx = currentEx.InnerException;
                    level++;
                }

                return new ProcessResult
                {
                    Success = false,
                    Message = GenerateFriendlyErrorMessage(ex.Message, destinationData?.Denumire)
                };
            }
        }

        private async Task<int> FindExistingDestinationAsync(DestinationData destinationData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Searching for existing destination: {destinationData.Oras}, {destinationData.Tara}");

                var destinations = _destinatieRepo.GetAll();
                System.Diagnostics.Debug.WriteLine($"Total destinations in database: {destinations.Count()}");

                var existing = destinations.FirstOrDefault(d =>
                    d.Oras.Equals(destinationData.Oras, StringComparison.OrdinalIgnoreCase) &&
                    d.Tara.Equals(destinationData.Tara, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found existing destination: ID={existing.Id_Destinatie}, Name={existing.Denumire}");
                    return existing.Id_Destinatie;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No existing destination found");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching for existing destination: {ex.Message}");
                return 0;
            }
        }

        private async Task AddDestinationImagesAsync(int destinatieId, List<string> searchQueries)
        {
            try
            {
                if (searchQueries == null || !searchQueries.Any()) return;

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

                                _imaginiDestRepo.Insert(imagineDestinatie);
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

        private async Task ProcessCategoriesAsync(int destinatieId, List<string> categorii)
        {
            try
            {
                if (categorii == null || !categorii.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No categories to process");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Getting all existing categories from database...");
                var existingCategories = _categorieRepo.GetAll().ToList();
                System.Diagnostics.Debug.WriteLine($"Found {existingCategories.Count} existing categories in database");

                if (existingCategories.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Existing categories: {string.Join(", ", existingCategories.Select(c => $"\"{c.Denumire}\" (ID: {c.Id_CategorieVacanta})"))}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No categories found in database!");
                }

                foreach (var categorieName in categorii)
                {
                    System.Diagnostics.Debug.WriteLine($"Processing category: '{categorieName}'");

                    // Caută doar în categoriile existente
                    var existingCategory = existingCategories.FirstOrDefault(c =>
                        c.Denumire.Equals(categorieName, StringComparison.OrdinalIgnoreCase));

                    if (existingCategory != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found matching category: '{existingCategory.Denumire}' with ID: {existingCategory.Id_CategorieVacanta}");

                        // Verifică dacă relația există deja
                        System.Diagnostics.Debug.WriteLine($"Checking if relationship already exists for destination {destinatieId} and category {existingCategory.Id_CategorieVacanta}");

                        var existingRelation = _catDestRepo.GetByDestinationId(destinatieId)
                            .FirstOrDefault(cd => cd.Id_CategorieVacanta == existingCategory.Id_CategorieVacanta);

                        if (existingRelation == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Creating new relationship between destination {destinatieId} and category {existingCategory.Id_CategorieVacanta}");

                            // Creează relația many-to-many
                            var catDest = new CategorieVacanta_Destinatie
                            {
                                Id_Destinatie = destinatieId,
                                Id_CategorieVacanta = existingCategory.Id_CategorieVacanta
                            };

                            try
                            {
                                _catDestRepo.Insert(catDest);
                                System.Diagnostics.Debug.WriteLine($"Successfully linked category '{categorieName}' to destination");
                            }
                            catch (Exception linkEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error linking category '{categorieName}': {linkEx.Message}");
                                if (linkEx.InnerException != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Inner exception: {linkEx.InnerException.Message}");
                                }
                                throw;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Relationship already exists for category '{categorieName}'");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Categoria '{categorieName}' nu există în DB și nu va fi adăugată.");
                        System.Diagnostics.Debug.WriteLine($"Available categories were: {string.Join(", ", existingCategories.Select(c => $"\"{c.Denumire}\""))}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing categories: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private async Task ProcessFacilitiesAsync(int destinatieId, List<string> facilitati)
        {
            try
            {
                if (facilitati == null || !facilitati.Any()) return;

                var existingFacilities = _facilitateRepo.GetAll().ToList();

                foreach (var facilityName in facilitati)
                {
                    var existingFacility = existingFacilities.FirstOrDefault(f =>
                        f.Denumire.Equals(facilityName, StringComparison.OrdinalIgnoreCase));

                    int facilityId;

                    if (existingFacility != null)
                    {
                        facilityId = existingFacility.Id_Facilitate;
                        System.Diagnostics.Debug.WriteLine($"Found existing facility: {facilityName} with ID: {facilityId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Creating new facility: {facilityName}");

                        // Creează facilitea nouă
                        var newFacility = new Facilitate
                        {
                            Denumire = facilityName,
                            Descriere = $"Facilitate pentru {facilityName}"
                        };

                        _facilitateRepo.Insert(newFacility);

                        // Refresh lista și găsește facilitea nou creată
                        existingFacilities = _facilitateRepo.GetAll().ToList();
                        var createdFacility = existingFacilities.FirstOrDefault(f =>
                            f.Denumire.Equals(facilityName, StringComparison.OrdinalIgnoreCase));

                        if (createdFacility == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to find newly created facility: {facilityName}");
                            continue; // Skip this facility if it couldn't be created
                        }

                        facilityId = createdFacility.Id_Facilitate;
                        System.Diagnostics.Debug.WriteLine($"Created new facility: {facilityName} with ID: {facilityId}");
                    }

                    if (facilityId <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid facility ID for: {facilityName}");
                        continue;
                    }

                    // Verifică dacă relația există deja
                    var existingRelation = _destFacilRepo.GetByDestinationId(destinatieId)
                        .FirstOrDefault(df => df.Id_Facilitate == facilityId);

                    if (existingRelation == null)
                    {
                        // Leagă facilitea de destinație
                        var destFacil = new DestinatieFacilitate
                        {
                            Id_Destinatie = destinatieId,
                            Id_Facilitate = facilityId
                        };

                        System.Diagnostics.Debug.WriteLine($"Linking facility {facilityId} to destination {destinatieId}");
                        _destFacilRepo.Insert(destFacil);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Facility {facilityName} already linked to destination");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing facilities: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private async Task ProcessPointsOfInterestAsync(int destinatieId, List<PointOfInterestData> puncteDeInteres)
        {
            try
            {
                if (puncteDeInteres == null || !puncteDeInteres.Any()) return;

                foreach (var poi in puncteDeInteres)
                {
                    System.Diagnostics.Debug.WriteLine($"Creating POI: {poi.Denumire} for destination {destinatieId}");

                    // Creează punctul de interes
                    var punctDeInteres = new PunctDeInteres
                    {
                        Denumire = poi.Denumire,
                        Descriere = poi.Descriere,
                        Tip = poi.Tip,
                        Id_Destinatie = destinatieId
                    };

                    _poiRepo.Insert(punctDeInteres);

                    // ID-ul este acum disponibil direct din obiectul punctDeInteres după inserare
                    var poiId = punctDeInteres.Id_PunctDeInteres;
                    System.Diagnostics.Debug.WriteLine($"POI created with ID: {poiId}");

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
                                    var photo = photos.Photos.First();
                                    var imageUrl = photo.Src?.Medium ?? photo.Src?.Original;

                                    if (!string.IsNullOrEmpty(imageUrl))
                                    {
                                        var imaginePoi = new ImaginiPunctDeInteres
                                        {
                                            Id_PunctDeInteres = poiId,
                                            ImagineUrl = imageUrl
                                        };
                                        _imaginiPoiRepo.Insert(imaginePoi);
                                        System.Diagnostics.Debug.WriteLine($"Added image for POI {poiId}: {imageUrl}");
                                    }
                                }
                            }
                            catch (Exception photoEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error adding photo for POI {poi.Denumire}: {photoEx.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing points of interest: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generează un mesaj prietenos pentru destinații care există deja
        /// </summary>
        private string GenerateFriendlyExistingMessage(string destinationName)
        {
            // Mesaj simplu - AI-ul ar trebui să gestioneze asta
            return $"Destinația {destinationName} există deja în aplicație. Poți să o explorezi în secțiunea Destinații!";
        }

        /// <summary>
        /// Generează un mesaj prietenos pentru erori
        /// </summary>
        private string GenerateFriendlyErrorMessage(string errorDetails, string destinationName = null)
        {
            var message = "Ne pare rău, dar am întâmpinat o problemă tehnică. Te rog încearcă din nou.";
            
            if (!string.IsNullOrEmpty(destinationName))
            {
                message += $" (Destinație: {destinationName})";
            }
  
     return message;
    }
    }

    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int DestinationId { get; set; }
        public int SuggestionId { get; set; } // NOU - ID-ul sugestiei create
        public bool IsGeneralChat { get; set; } = false;
        public bool IsAskPreference { get; set; } = false; // NOU - AI cere preferințe
    }
}