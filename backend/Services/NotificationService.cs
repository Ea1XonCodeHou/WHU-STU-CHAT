using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using backend.DTOs;

namespace backend.Services
{
    /// <summary>
    /// 通知服务实现
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly string _connectionString;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置</param>
        public NotificationService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// 创建通知
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="content">通知内容</param>
        /// <param name="type">通知类型</param>
        /// <param name="title">通知标题</param>
        /// <param name="relatedId">相关ID</param>
        /// <returns>创建结果</returns>
        public async Task<bool> CreateNotificationAsync(int userId, string content, string type = "system", string title = "系统通知", int? relatedId = null)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // 检查表中是否有RelatedId字段
                    bool hasRelatedIdField = false;
                    try 
                    {
                        var schemaCommand = new MySqlCommand(
                            "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'notifications' AND COLUMN_NAME = 'RelatedId'",
                            connection);
                        var count = Convert.ToInt32(await schemaCommand.ExecuteScalarAsync());
                        hasRelatedIdField = count > 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"检查RelatedId字段出错: {ex.Message}");
                    }
                    
                    string sql;
                    if (hasRelatedIdField)
                    {
                        sql = @"INSERT INTO notifications (UserId, Title, Content, Type, IsRead, IsHandled, RelatedId, CreatedAt) 
                                VALUES (@UserId, @Title, @Content, @Type, 0, 0, @RelatedId, @CreatedAt)";
                    }
                    else
                    {
                        sql = @"INSERT INTO notifications (UserId, Title, Content, Type, IsRead, IsHandled, CreatedAt) 
                                VALUES (@UserId, @Title, @Content, @Type, 0, 0, @CreatedAt)";
                    }
                    
                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@Title", title);
                        command.Parameters.AddWithValue("@Content", content);
                        command.Parameters.AddWithValue("@Type", type);
                        command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        
                        if (hasRelatedIdField)
                        {
                            command.Parameters.AddWithValue("@RelatedId", relatedId != null ? relatedId : DBNull.Value);
                        }
                        
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建通知失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取用户所有通知
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>通知列表</returns>
        public async Task<List<NotificationDTO>> GetNotificationsByUserIdAsync(int userId)
        {
            var notifications = new List<NotificationDTO>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 检查表中是否有RelatedId字段
                bool hasRelatedIdField = false;
                try 
                {
                    var schemaCommand = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'notifications' AND COLUMN_NAME = 'RelatedId'",
                        connection);
                    var count = Convert.ToInt32(await schemaCommand.ExecuteScalarAsync());
                    hasRelatedIdField = count > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"检查RelatedId字段出错: {ex.Message}");
                }
                
                string sql;
                if (hasRelatedIdField)
                {
                    sql = "SELECT NotificationId, UserId, Content, Title, Type, CreatedAt, IsRead, IsHandled, RelatedId FROM notifications WHERE UserId = @UserId";
                }
                else
                {
                    sql = "SELECT NotificationId, UserId, Content, Title, Type, CreatedAt, IsRead, IsHandled FROM notifications WHERE UserId = @UserId";
                }
                
                var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var notification = new NotificationDTO
                        {
                            NotificationId = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            Content = reader.GetString(2),
                            Title = reader.GetString(3),
                            Type = reader.GetString(4),
                            CreatedAt = reader.GetDateTime(5),
                            IsRead = reader.IsDBNull(6) ? false : reader.GetBoolean(6),
                            IsHandled = reader.IsDBNull(7) ? false : reader.GetBoolean(7)
                        };
                        
                        // 如果存在RelatedId字段，尝试读取
                        if (hasRelatedIdField && reader.FieldCount > 8 && !reader.IsDBNull(8))
                        {
                            notification.RelatedId = reader.GetInt32(8);
                        }
                        
                        notifications.Add(notification);
                    }
                }
            }
            return notifications;
        }

        /// <summary>
        /// 获取指定ID的通知
        /// </summary>
        /// <param name="notificationId">通知ID</param>
        /// <returns>通知详情</returns>
        public async Task<NotificationDTO> GetNotificationByIdAsync(int notificationId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 检查表中是否有RelatedId字段
                bool hasRelatedIdField = false;
                try 
                {
                    var schemaCommand = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'notifications' AND COLUMN_NAME = 'RelatedId'",
                        connection);
                    var count = Convert.ToInt32(await schemaCommand.ExecuteScalarAsync());
                    hasRelatedIdField = count > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"检查RelatedId字段出错: {ex.Message}");
                }
                
                string sql;
                if (hasRelatedIdField)
                {
                    sql = "SELECT NotificationId, UserId, Content, Title, Type, CreatedAt, IsRead, IsHandled, RelatedId FROM notifications WHERE NotificationId = @NotificationId";
                }
                else
                {
                    sql = "SELECT NotificationId, UserId, Content, Title, Type, CreatedAt, IsRead, IsHandled FROM notifications WHERE NotificationId = @NotificationId";
                }
                
                var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@NotificationId", notificationId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var notification = new NotificationDTO
                        {
                            NotificationId = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            Content = reader.GetString(2),
                            Title = reader.GetString(3),
                            Type = reader.GetString(4),
                            CreatedAt = reader.GetDateTime(5),
                            IsRead = reader.IsDBNull(6) ? false : reader.GetBoolean(6),
                            IsHandled = reader.IsDBNull(7) ? false : reader.GetBoolean(7)
                        };
                        
                        // 如果存在RelatedId字段，尝试读取
                        if (hasRelatedIdField && reader.FieldCount > 8 && !reader.IsDBNull(8))
                        {
                            notification.RelatedId = reader.GetInt32(8);
                        }
                        
                        return notification;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 标记通知为已处理
        /// </summary>
        /// <param name="notificationId">通知ID</param>
        /// <returns>处理结果</returns>
        public async Task MarkAsHandled(int notificationId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "UPDATE notifications SET IsHandled = 1, IsRead = 1 WHERE NotificationId = @NotificationId",
                    connection);
                command.Parameters.AddWithValue("@NotificationId", notificationId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
} 