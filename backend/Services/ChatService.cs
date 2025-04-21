using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTOs;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace backend.Services
{
    /// <summary>
    /// 聊天服务实现
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly string _connectionString;
        private readonly ILogger<ChatService> _logger;
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<int, UserDTO>> _onlineUsers = 
            new ConcurrentDictionary<int, ConcurrentDictionary<int, UserDTO>>();

        public ChatService(IConfiguration configuration, ILogger<ChatService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        /// <summary>
        /// 获取聊天室名称
        /// </summary>
        public async Task<string> GetRoomNameAsync(int roomId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand(
                        "SELECT RoomName FROM ChatRooms WHERE RoomId = @RoomId",
                        connection);
                    command.Parameters.AddWithValue("@RoomId", roomId);

                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取聊天室名称失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取聊天室历史消息
        /// </summary>
        public async Task<List<MessageDTO>> GetRoomMessagesAsync(int roomId, int count)
        {
            var messages = new List<MessageDTO>();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand(
                        @"SELECT m.MessageId, m.SenderId, u.Username AS SenderName, m.Content, m.CreateTime, m.RoomId
                          FROM RoomMessages m
                          JOIN Users u ON m.SenderId = u.UserId
                          WHERE m.RoomId = @RoomId
                          ORDER BY m.CreateTime DESC
                          LIMIT @Count",
                        connection);
                    command.Parameters.AddWithValue("@RoomId", roomId);
                    command.Parameters.AddWithValue("@Count", count);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            messages.Add(new MessageDTO
                            {
                                MessageId = reader.GetInt32(reader.GetOrdinal("MessageId")),
                                SenderId = reader.GetInt32(reader.GetOrdinal("SenderId")),
                                SenderName = reader.GetString(reader.GetOrdinal("SenderName")),
                                Content = reader.GetString(reader.GetOrdinal("Content")),
                                RoomId = reader.GetInt32(reader.GetOrdinal("RoomId")),
                                SendTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                                MessageType = "text",
                                IsRead = true
                            });
                        }
                    }
                }
                
                // 按时间正序排列消息
                messages.Reverse();
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取聊天室历史消息失败: {ex.Message}");
                return messages;
            }
        }

        /// <summary>
        /// 保存聊天室消息
        /// </summary>
        public async Task<int> SaveRoomMessageAsync(int roomId, int userId, string message)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand(
                        @"INSERT INTO RoomMessages (RoomId, SenderId, Content, CreateTime) 
                          VALUES (@RoomId, @SenderId, @Content, @CreateTime);
                          SELECT LAST_INSERT_ID();",
                        connection);
                    command.Parameters.AddWithValue("@RoomId", roomId);
                    command.Parameters.AddWithValue("@SenderId", userId);
                    command.Parameters.AddWithValue("@Content", message);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"保存聊天室消息失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取聊天室在线用户
        /// </summary>
        public async Task<List<UserDTO>> GetRoomOnlineUsersAsync(int roomId)
        {
            var users = new List<UserDTO>();
            
            try
            {
                // 先从内存中获取在线用户信息
                if (_onlineUsers.TryGetValue(roomId, out var roomUsers))
                {
                    users.AddRange(roomUsers.Values);
                }
                
                // 如果没有在线用户，从数据库获取最近活跃的用户
                if (users.Count == 0)
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        var command = new MySqlCommand(
                            @"SELECT DISTINCT u.UserId, u.Username, u.Avatar
                              FROM Users u
                              JOIN RoomMessages m ON u.UserId = m.SenderId
                              WHERE m.RoomId = @RoomId
                              ORDER BY m.CreateTime DESC
                              LIMIT 10",
                            connection);
                        command.Parameters.AddWithValue("@RoomId", roomId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var user = new UserDTO
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    Username = reader.GetString(reader.GetOrdinal("Username")),
                                    Status = "offline",
                                    LastActive = DateTime.Now.AddMinutes(-15),
                                    AvatarUrl = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? 
                                                null : reader.GetString(reader.GetOrdinal("Avatar"))
                                };
                                users.Add(user);
                            }
                        }
                    }
                }
                
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取聊天室在线用户失败: {ex.Message}");
                return users;
            }
        }

        /// <summary>
        /// 获取或创建默认聊天室
        /// </summary>
        public async Task<int> GetOrCreateDefaultRoomAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 查找默认聊天室
                    var selectCommand = new MySqlCommand(
                        "SELECT RoomId FROM ChatRooms WHERE RoomName = 'WHU 校园公共聊天室' LIMIT 1",
                        connection);

                    var roomId = await selectCommand.ExecuteScalarAsync();
                    
                    if (roomId != null && roomId != DBNull.Value)
                    {
                        return Convert.ToInt32(roomId);
                    }

                    // 创建默认聊天室
                    var insertCommand = new MySqlCommand(
                        @"INSERT INTO ChatRooms (RoomName, Description, CreateTime, UpdateTime) 
                          VALUES ('WHU 校园公共聊天室', '欢迎来到武汉大学校园公共聊天室，这里是交流分享的空间！', NOW(), NOW());
                          SELECT LAST_INSERT_ID();",
                        connection);

                    var newRoomId = await insertCommand.ExecuteScalarAsync();
                    return Convert.ToInt32(newRoomId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取或创建默认聊天室失败: {ex.Message}");
                return 1; // 默认返回1，通常是第一个聊天室
            }
        }

        /// <summary>
        /// 添加用户到在线列表
        /// </summary>
        public void AddUserToRoom(int roomId, UserDTO user)
        {
            var roomUsers = _onlineUsers.GetOrAdd(roomId, new ConcurrentDictionary<int, UserDTO>());
            roomUsers[user.Id] = user;
        }

        /// <summary>
        /// 从在线列表移除用户
        /// </summary>
        public void RemoveUserFromRoom(int roomId, int userId)
        {
            if (_onlineUsers.TryGetValue(roomId, out var roomUsers))
            {
                roomUsers.TryRemove(userId, out _);
                
                // 如果聊天室没有用户了，移除聊天室记录
                if (roomUsers.IsEmpty)
                {
                    _onlineUsers.TryRemove(roomId, out _);
                }
            }
        }
    }
} 