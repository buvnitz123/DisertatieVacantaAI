using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public static class AIServiceFactory
    {
        public const string PreferenceKey = "SelectedAIModel";
        public const string Gemini25 = "gemini-2.5-flash";
        public const string Gemini3 = "gemini-3-flash";
        public const string GPT4 = "gpt-4o";

        public static IAIService Create(string? modelKey = null)
        {
            modelKey ??= Preferences.Get(PreferenceKey, Gemini25);

            Debug.WriteLine($"[AIServiceFactory] Creating AI service for: {modelKey}");

            return modelKey switch
            {
                GPT4 => new GPTService(),  // gpt-4o
                Gemini3 => new GeminiService("gemini-3-flash-preview"),
                _ => new GeminiService("gemini-2.5-flash")
            };
        }

        public static string GetCurrentModelKey()
        {
            return Preferences.Get(PreferenceKey, Gemini25);
        }

        public static void SetModel(string modelKey)
        {
            Preferences.Set(PreferenceKey, modelKey);
            Debug.WriteLine($"[AIServiceFactory] Model changed to: {modelKey}");
        }
    }
}
