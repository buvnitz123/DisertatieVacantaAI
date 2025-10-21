using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public static class AIDestinationPromptTemplate
    {
        private static string _cachedPrompt = null;

        private static async Task<string> LoadPromptAsync()
        {
            if (!string.IsNullOrEmpty(_cachedPrompt))
            {
                return _cachedPrompt;
            }

            try
            {
                // Încearcă să citească din Resources/Raw/AIPrompt.txt
                using var stream = await FileSystem.OpenAppPackageFileAsync("AIPrompt.txt");
                using var reader = new StreamReader(stream);

                _cachedPrompt = await reader.ReadToEndAsync();

                Debug.WriteLine("✅ AI Prompt loaded successfully from AIPrompt.txt");
                Debug.WriteLine($"Prompt length: {_cachedPrompt.Length} characters");

                return _cachedPrompt;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Could not load AIPrompt.txt: {ex.Message}");
                Debug.WriteLine("Using fallback prompt instead");

                return _cachedPrompt;
            }
        }


        public static async Task<string> BuildPromptAsync(string userQuery, string existingDestinations, string availableCategories)
        {
            var basePrompt = await LoadPromptAsync();

            return basePrompt
            .Replace("{EXISTING_DESTINATIONS}", existingDestinations)
            .Replace("{AVAILABLE_CATEGORIES}", availableCategories) + $"\n\nCererea utilizatorului: {userQuery}";
        }

        public static void RefreshPrompt()
        {
            _cachedPrompt = null;
            Debug.WriteLine("🔄 AI Prompt cache cleared - will reload on next request");
        }

        public const string EXAMPLE_RESPONSE = @"
{
  ""action"": ""create_destination"",
  ""success"": true,
  ""message"": ""Grozav! Dubai este o destinație fascinantă!"",
  ""destination"": {
    ""denumire"": ""Dubai"",
    ""tara"": ""Emiratele Arabe Unite"",
    ""oras"": ""Dubai"",
  ""regiune"": ""Orientul Mijlociu"",
    ""descriere"": ""Dubai este o destinație de lux..."",
    ""pretAdult"": 2500.0,
    ""pretMinor"": 1500.0,
    ""categorii"": [""Lux"", ""Modern""],
    ""facilitati"": [""Hotel 5 stele"", ""Transport privat""],
    ""puncteDeInteres"": [
   {
        ""denumire"": ""Burj Khalifa"",
        ""descriere"": ""Cea mai înaltă clădire din lume"",
      ""tip"": ""Atracție"",
        ""photoSearchQueries"": [""Burj Khalifa Dubai""]
      }
    ],
    ""photoSearchQueries"": [""Dubai city skyline""]
  }
}";
    }
}