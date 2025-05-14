using backend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface INotificationService
    {
        Task<bool> CreateNotificationAsync(int userId, string content, string type = "system", string title = "系统通知", int? relatedId = null);
        Task<List<NotificationDTO>> GetNotificationsByUserIdAsync(int userId);
        Task<NotificationDTO> GetNotificationByIdAsync(int notificationId);
        Task MarkAsHandled(int notificationId);
    }
}
