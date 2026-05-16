using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Enums;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Utils
{
    public static class ActivityLogger
    {
        public static void Log(int userId, TipActivitate tipActivitate, TipEntitate tipEntitate, int? idEntitate = null)
        {
            Task.Run(() =>
            {
                try
                {
                    var repo = new LogActivitateRepository();
                    var allLogs = repo.GetAll().ToList();
                    int nextId = allLogs.Any() ? allLogs.Max(l => l.Id_LogActivitate) + 1 : 1;

                    var log = new LogActivitate
                    {
                        Id_LogActivitate = nextId,
                        Id_Utilizator = userId,
                        TipActivitate = tipActivitate.ToString(),
                        TipEntitate = tipEntitate.ToString(),
                        IdEntitate = idEntitate,
                        DataInregistrare = DateTime.Now
                    };

                    repo.Insert(log);
                    Debug.WriteLine($"[ActivityLogger] Logged: User={userId}, Action={tipActivitate}, Entity={tipEntitate} ({idEntitate})");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ActivityLogger] Failed to log activity: {ex.Message}");
                }
            });
        }
    }
}
