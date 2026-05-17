namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public interface IAIService
    {
        string ModelName { get; }
        Task<bool> InitializeAsync();
        Task<string> GetChatResponseAsync(string userMessage, List<string>? conversationHistory = null);
        Task<string> GetDestinationCreationResponseAsync(string userQuery, string existingDestinations, string availableCategories, List<string>? conversationHistory = null);
        void Dispose();
    }
}
