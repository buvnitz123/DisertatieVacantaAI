using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public static class AIPerformanceLogger
    {
        private static readonly ModelPerformantaRepository _repo = new();

        public static int LastLogId { get; private set; } = -1;

        public static int Log(string modelName, decimal secondsDuration, int tokenInput, int tokenOutput)
        {
            try
            {
                var id = GenerateId();
                var entry = new ModelPerformanta
                {
                    Id_ModelPerformanta = id,
                    Data_Inregistrare = DateTime.Now,
                    NumeModel = modelName,
                    SecundeDurate = secondsDuration,
                    TokenInput = tokenInput,
                    TokenOutput = tokenOutput,
                    ApreciereUser = null
                };

                _repo.Insert(entry);
                Debug.WriteLine($"[AIPerformance] {modelName}: {secondsDuration:F3}s, {tokenInput} in / {tokenOutput} out");
                LastLogId = id;
                return id;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AIPerformance] Error logging: {ex.Message}");
                return -1;
            }
        }

        public static void UpdateApreciere(int id, int valoare)
        {
            try
            {
                var entry = _repo.GetById(id);
                if (entry != null)
                {
                    entry.ApreciereUser = valoare;
                    _repo.Update(entry);
                    Debug.WriteLine($"[AIPerformance] Updated rating for {id}: {valoare}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AIPerformance] Error updating rating: {ex.Message}");
            }
        }

        private static int GenerateId()
        {
            // Timestamp-based unique ID
            return (int)(DateTime.Now.Ticks % int.MaxValue);
        }
    }
}
