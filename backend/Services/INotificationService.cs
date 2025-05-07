using backend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string content);
        Task<List<NotificationDTO>> GetNotificationsByUserIdAsync(int userId);
        Task<NotificationDTO> GetNotificationByIdAsync(int notificationId);
        Task MarkAsHandled(int notificationId);
    }
}
