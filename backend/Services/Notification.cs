using backend.DTOs;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly string _connectionString;

        public NotificationService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task CreateNotificationAsync(int userId, string content)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "INSERT INTO Notifications (UserId, Content, CreatedAt, IsHandled) VALUES (@UserId, @Content, @CreatedAt, 0)",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Content", content);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<NotificationDTO>> GetNotificationsByUserIdAsync(int userId)
        {
            var notifications = new List<NotificationDTO>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "SELECT NotificationId, UserId, Content, CreatedAt, IsHandled FROM Notifications WHERE UserId = @UserId",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        notifications.Add(new NotificationDTO
                        {
                            NotificationId = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            Content = reader.GetString(2),
                            CreatedAt = reader.GetDateTime(3),
                            IsHandled = reader.IsDBNull(4) ? (bool?)null : reader.GetBoolean(4)
                        });
                    }
                }
            }
            return notifications;
        }

        public async Task<NotificationDTO> GetNotificationByIdAsync(int notificationId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "SELECT NotificationId, UserId, Content, CreatedAt, IsHandled FROM Notifications WHERE NotificationId = @NotificationId",
                    connection);
                command.Parameters.AddWithValue("@NotificationId", notificationId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new NotificationDTO
                        {
                            NotificationId = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            Content = reader.GetString(2),
                            CreatedAt = reader.GetDateTime(3),
                            IsHandled = reader.IsDBNull(4) ? (bool?)null : reader.GetBoolean(4)
                        };
                    }
                }
            }
            return null;
        }

        public async Task MarkAsHandled(int notificationId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "UPDATE Notifications SET IsHandled = 1 WHERE NotificationId = @NotificationId",
                    connection);
                command.Parameters.AddWithValue("@NotificationId", notificationId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
