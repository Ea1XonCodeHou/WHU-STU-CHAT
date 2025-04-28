using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTOs;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace backend.Services
{
    /// <summary>
    /// 聊天服务实现
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly string _connectionString;
        private readonly ILogger<ChatService> _logger;
        private readonly string _tempFileDirectory;
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<int, UserDTO>> _onlineUsers = 
            new ConcurrentDictionary<int, ConcurrentDictionary<int, UserDTO>>();

        public ChatService(IConfiguration configuration, ILogger<ChatService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            
            // 设置临时文件目录
            _tempFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "uploads");
            
            // 确保目录存在
            if (!Directory.Exists(_tempFileDirectory))
            {
                Directory.CreateDirectory(_tempFileDirectory);
            }
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

                    // 使用修改后的SQL查询，包含MessageType和FileUrl字段
                    var command = new MySqlCommand(
                        @"SELECT m.MessageId, m.SenderId, u.Username AS SenderName, m.Content, 
                          m.CreateTime, m.RoomId, m.MessageType, m.FileUrl
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
                            string content = reader.GetString(reader.GetOrdinal("Content"));
                            string messageType = reader.IsDBNull(reader.GetOrdinal("MessageType")) 
                                ? "text" 
                                : reader.GetString(reader.GetOrdinal("MessageType"));
                            
                            string fileUrl = null;
                            if (!reader.IsDBNull(reader.GetOrdinal("FileUrl")))
                            {
                                fileUrl = reader.GetString(reader.GetOrdinal("FileUrl"));
                            }
                            
                            // 对于文件类型消息，解析JSON内容获取文件名和大小信息
                            string fileName = null;
                            long? fileSize = null;
                            
                            if ((messageType == "file" || messageType == "image") && !string.IsNullOrEmpty(fileUrl))
                            {
                                try
                                {
                                    var fileInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                                    if (fileInfo != null)
                                    {
                                        if (fileInfo.TryGetValue("fileName", out var name))
                                            fileName = name;
                                            
                                        if (fileInfo.TryGetValue("fileSize", out var size) && long.TryParse(size, out var sizeValue))
                                            fileSize = sizeValue;
                                            
                                        // 如果是文件类型，更新显示内容
                                        if (messageType == "file" && !string.IsNullOrEmpty(fileName))
                                            content = $"文件: {fileName}";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "解析文件信息失败");
                                    // 解析失败时保持原始内容
                                }
                            }
                            
                            messages.Add(new MessageDTO
                            {
                                MessageId = reader.GetInt32(reader.GetOrdinal("MessageId")),
                                SenderId = reader.GetInt32(reader.GetOrdinal("SenderId")),
                                SenderName = reader.GetString(reader.GetOrdinal("SenderName")),
                                Content = content,
                                RoomId = reader.GetInt32(reader.GetOrdinal("RoomId")),
                                SendTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                                MessageType = messageType,
                                FileUrl = fileUrl,
                                FileName = fileName,
                                FileSize = fileSize,
                                IsRead = true
                            });
                        }
                    }
                }
                
                // 按时间升序返回消息
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
            // 调用带类型的方法，默认为text类型
            return await SaveRoomMessageWithTypeAsync(roomId, userId, message, "text");
        }
        
        /// <summary>
        /// 保存聊天室消息（带消息类型）
        /// </summary>
        public async Task<int> SaveRoomMessageWithTypeAsync(int roomId, int userId, string message, 
            string messageType, string fileUrl = null, string fileName = null, long? fileSize = null)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 对于文件类型消息，将文件信息序列化为JSON存储
                    string content = message;
                    if ((messageType == "file" || messageType == "image") && !string.IsNullOrEmpty(fileName))
                    {
                        var fileInfo = new Dictionary<string, string>
                        {
                            { "fileName", fileName },
                            { "fileSize", fileSize?.ToString() ?? "0" }
                        };
                        content = JsonSerializer.Serialize(fileInfo);
                    }

                    // 使用修改后的SQL插入语句，包含MessageType和FileUrl字段
                    var command = new MySqlCommand(
                        @"INSERT INTO RoomMessages (RoomId, SenderId, Content, CreateTime, MessageType, FileUrl) 
                          VALUES (@RoomId, @SenderId, @Content, @CreateTime, @MessageType, @FileUrl);
                          SELECT LAST_INSERT_ID();",
                        connection);
                    command.Parameters.AddWithValue("@RoomId", roomId);
                    command.Parameters.AddWithValue("@SenderId", userId);
                    command.Parameters.AddWithValue("@Content", content);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@MessageType", messageType);
                    
                    if (string.IsNullOrEmpty(fileUrl))
                        command.Parameters.AddWithValue("@FileUrl", DBNull.Value);
                    else
                        command.Parameters.AddWithValue("@FileUrl", fileUrl);

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
        /// 上传文件到临时目录
        /// </summary>
        public async Task<(string FileUrl, string FileName, long FileSize)> UploadTempFileAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("没有选择文件或文件为空");
                }

                // 限制文件大小（如10MB）
                if (file.Length > 10 * 1024 * 1024)
                {
                    throw new ArgumentException("文件大小超过限制");
                }

                // 生成唯一文件名
                string fileExtension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(_tempFileDirectory, uniqueFileName);
                
                // 保存文件
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                // 返回可访问的URL和文件信息
                string fileUrl = $"/temp/uploads/{uniqueFileName}";
                return (fileUrl, file.FileName, file.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"上传文件失败: {ex.Message}");
                throw; // 重新抛出异常以便控制器处理
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
                
                // 如果没有在线用户，从数据库获取最近活跃用户
                if (users.Count == 0)
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        // 修改SQL查询，确保ORDER BY列包含在SELECT列表中
                        var command = new MySqlCommand(
                            @"SELECT DISTINCT u.UserId, u.Username, u.Avatar, MAX(m.CreateTime) as LastActivity
                              FROM Users u
                              JOIN RoomMessages m ON u.UserId = m.SenderId
                              WHERE m.RoomId = @RoomId
                              GROUP BY u.UserId, u.Username, u.Avatar
                              ORDER BY LastActivity DESC
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
                                    LastActive = reader.GetDateTime(reader.GetOrdinal("LastActivity")),
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
        /// 添加用户到聊天室列表
        /// </summary>
        public void AddUserToRoom(int roomId, UserDTO user)
        {
            var roomUsers = _onlineUsers.GetOrAdd(roomId, new ConcurrentDictionary<int, UserDTO>());
            roomUsers[user.Id] = user;
        }

        /// <summary>
        /// 从聊天室列表移除用户
        /// </summary>
        public void RemoveUserFromRoom(int roomId, int userId)
        {
            if (_onlineUsers.TryGetValue(roomId, out var roomUsers))
            {
                roomUsers.TryRemove(userId, out _);
                
                // 如果聊天室用户已没人，移除聊天室记录
                if (roomUsers.IsEmpty)
                {
                    _onlineUsers.TryRemove(roomId, out _);
                }
            }
        }
    }
} 