using System.Threading.Tasks;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Interfaces
{
    public interface INotificationService
    {
        Task<bool> RequestPermissionAsync();
        void ShowNotification(string title, string message, string bigText = null);
        void CancelNotification();
    }
}