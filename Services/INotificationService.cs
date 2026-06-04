using WMSClean.Models;

namespace WMSClean.Services
{
    public interface INotificationService
    {
        Task AddNotificationAsync(string userId, string title, string message, string type = "Info", string? link = null);
        Task<List<Notification>> GetUserNotificationsAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task<int> GetUnreadCountAsync(string userId);
    }
}