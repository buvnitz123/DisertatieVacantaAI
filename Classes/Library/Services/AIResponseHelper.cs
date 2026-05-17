using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public static class AIResponseHelper
    {
        public static string CleanAIResponse(string response)
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

        public static string CreateErrorResponse(string errorMessage)
        {
            return $@"{{
    ""action"": ""error"",
    ""success"": false,
    ""message"": ""{errorMessage}"",
    ""destination"": null
}}";
        }

        public static string BuildConversationContext(List<string>? conversationHistory, string basePrompt)
        {
            if (conversationHistory == null || !conversationHistory.Any())
                return basePrompt;

            var prompt = basePrompt + "\n\n📝 CONTEXT CONVERSAȚIE ANTERIOARĂ:\n";
            foreach (var message in conversationHistory.TakeLast(5))
            {
                prompt += $"- {message}\n";
            }
            prompt += "\n⚠️ Ține cont de mesajele anterioare când răspunzi!\n";
            return prompt;
        }

        public static async Task<string> InjectUserProfile(string prompt)
        {
            try
            {
                var userIdStr = await MauiAppDisertatieVacantaAI.Classes.Library.Session.UserSession.GetUserIdAsync();
                if (int.TryParse(userIdStr, out int currentUserId) && currentUserId > 0)
                {
                    var recommendationService = new RecommendationService();
                    var profileSummary = recommendationService.GetUserProfileSummaryText(currentUserId);
                    if (!string.IsNullOrEmpty(profileSummary))
                    {
                        prompt += $"\n\n👤 PROFIL UTILIZATOR (Context invizibil pentru el, folosește-l ca sa personalizezi rezultatul):\n{profileSummary}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error injecting user profile: {ex.Message}");
            }
            return prompt;
        }
    }
}
