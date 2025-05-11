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
using System.Linq;

namespace backend.Services
{
    /// <summary>
    /// 聊天服务实现
    /// 聊天服务实现
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly string _connectionString;
        private readonly ILogger<ChatService> _logger;
        private readonly string _tempFileDirectory;
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<int, UserDTO>> _onlineUsers = 
            new ConcurrentDictionary<int, ConcurrentDictionary<int, UserDTO>>();
        // 全局在线用户列表
        private static readonly ConcurrentDictionary<int, bool> _globalOnlineUsers = new ConcurrentDictionary<int, bool>();

        public ChatService(IConfiguration configuration, ILogger<ChatService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            
            // 临时文件目录
            // 临时文件目录
            _tempFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "uploads");
            
            // 确保目录存在
            // 确保目录存在
            if (!Directory.Exists(_tempFileDirectory))
            {
                Directory.CreateDirectory(_tempFileDirectory);
            }
        }

        /// <summary>
        /// 获取房间名称
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
                _logger.LogError(ex, $"获取房间名称失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取房间消息
        /// </summary>
        public async Task<List<MessageDTO>> GetRoomMessagesAsync(int roomId, int count)
        {
            var messages = new List<MessageDTO>();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 使用自定义的SQL查询，包括MessageType和FileUrl字段
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
                            
                            // 解析文件信息并提取文件名和文件大小信息
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
                                            
                                        // 如果文件类型为文件，则显示文件名
                                        if (messageType == "file" && !string.IsNullOrEmpty(fileName))
                                            content = $"文件: {fileName}";
                                            content = $"文件: {fileName}";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "解析文件信息失败");
                                    // 如果解析失败，则保留原始内容
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
                
                // 反转消息列表，使其按时间升序排列
                messages.Reverse();
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取房间消息失败: {ex.Message}");
                return messages;
            }
        }

        /// <summary>
        /// 保存房间消息
        /// </summary>
        public async Task<int> SaveRoomMessageAsync(int roomId, int userId, string message)
        {
            // 默认消息类型为text
            return await SaveRoomMessageWithTypeAsync(roomId, userId, message, "text");
        }
        
        /// <summary>
        /// 保存房间消息，包括消息类型和文件信息
        /// </summary>
        public async Task<int> SaveRoomMessageWithTypeAsync(int roomId, int userId, string message, 
            string messageType, string fileUrl = null, string fileName = null, long? fileSize = null)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 将消息内容和文件信息序列化为JSON存储
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

                    // 使用自定义的SQL语句，包括MessageType和FileUrl字段
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
                _logger.LogError(ex, $"保存房间消息失败: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// 上传临时文件到临时目录
        /// </summary>
        public async Task<(string FileUrl, string FileName, long FileSize)> UploadTempFileAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("未选择文件或文件为空");
                    throw new ArgumentException("未选择文件或文件为空");
                }

                // 检查文件大小是否超过10MB
                if (file.Length > 10 * 1024 * 1024)
                {
                    throw new ArgumentException("文件大小超过限制");
                    throw new ArgumentException("文件大小超过限制");
                }

                // 生成唯一文件名
                // 生成唯一文件名
                string fileExtension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(_tempFileDirectory, uniqueFileName);
                
                // 保存文件
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
                throw; // 抛出异常，由调用者处理
            }
        }

        /// <summary>
        /// 获取房间在线用户
        /// </summary>
        public async Task<List<UserDTO>> GetRoomOnlineUsersAsync(int roomId)
        {
            var users = new List<UserDTO>();
            
            try
            {
                // 等待从缓存中获取房间用户信息
                if (_onlineUsers.TryGetValue(roomId, out var roomUsers))
                {
                    users.AddRange(roomUsers.Values);
                }
                
                // 如果房间没有用户，则从数据库中获取最近活跃的10个用户
                if (users.Count == 0)
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        // 自定义SQL查询，确保ORDER BY子句中包含SELECT子句
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
                _logger.LogError(ex, $"获取房间在线用户失败: {ex.Message}");
                return users;
            }
        }

        /// <summary>
        /// 获取或创建默认房间
        /// </summary>
        public async Task<int> GetOrCreateDefaultRoomAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 查询默认房间
                    var selectCommand = new MySqlCommand(
                        "SELECT RoomId FROM ChatRooms WHERE RoomName = 'WHU 院系交流群' LIMIT 1",
                        connection);

                    var roomId = await selectCommand.ExecuteScalarAsync();
                    
                    if (roomId != null && roomId != DBNull.Value)
                    {
                        return Convert.ToInt32(roomId);
                    }

                    // 创建默认房间
                    var insertCommand = new MySqlCommand(
                        @"INSERT INTO ChatRooms (RoomName, Description, CreateTime, UpdateTime) 
                          VALUES ('WHU 院系交流群', '欢迎加入武汉大学院系交流群，这里是大家交流学习和生活的地方。', NOW(), NOW());
                          SELECT LAST_INSERT_ID();",
                        connection);

                    var newRoomId = await insertCommand.ExecuteScalarAsync();
                    return Convert.ToInt32(newRoomId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取或创建默认房间失败: {ex.Message}");
                return 1; // 默认返回1，通常是第一个房间
            }
        }

        /// <summary>
        /// 添加用户到房间
        /// </summary>
        public void AddUserToRoom(int roomId, UserDTO user)
        {
            var roomUsers = _onlineUsers.GetOrAdd(roomId, new ConcurrentDictionary<int, UserDTO>());
            roomUsers[user.Id] = user;
        }

        /// <summary>
        /// 从聊天室列表中移除用户
        /// </summary>
        public void RemoveUserFromRoom(int roomId, int userId)
        {
            if (_onlineUsers.TryGetValue(roomId, out var roomUsers))
            {
                roomUsers.TryRemove(userId, out _);
                
                // 如果房间没有用户了，可以选择移除整个房间记录
                if (roomUsers.IsEmpty)
                {
                    _onlineUsers.TryRemove(roomId, out _);
                }
            }
        }
        
        /// <summary>
        /// 获取私聊历史消息
        /// </summary>
        public async Task<List<MessageDTO>> GetPrivateChatHistoryAsync(int userId, int friendId, int count)
        {
            List<MessageDTO> messages = new List<MessageDTO>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 获取双向私聊记录
                string sql = @"
                    SELECT pm.MessageId, pm.SenderId, pm.ReceiverId, pm.Content, pm.MessageType, 
                        pm.FileUrl, pm.FileName, pm.FileSize, pm.CreateTime as SendTime, pm.IsRead,
                        u1.Username as sender_name, u2.Username as receiver_name
                    FROM PrivateMessages pm
                    JOIN Users u1 ON pm.SenderId = u1.UserId
                    JOIN Users u2 ON pm.ReceiverId = u2.UserId
                    WHERE (pm.SenderId = @userId AND pm.ReceiverId = @friendId)
                       OR (pm.SenderId = @friendId AND pm.ReceiverId = @userId)
                    ORDER BY pm.CreateTime DESC
                    LIMIT @count";
                
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@friendId", friendId);
                    command.Parameters.AddWithValue("@count", count);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            messages.Add(new MessageDTO
                            {
                                MessageId = reader.GetInt32(reader.GetOrdinal("MessageId")),
                                SenderId = reader.GetInt32(reader.GetOrdinal("SenderId")),
                                SenderName = reader.GetString(reader.GetOrdinal("sender_name")),
                                ReceiverId = reader.GetInt32(reader.GetOrdinal("ReceiverId")),
                                ReceiverName = reader.GetString(reader.GetOrdinal("receiver_name")),
                                Content = reader.GetString(reader.GetOrdinal("Content")),
                                MessageType = reader.GetString(reader.GetOrdinal("MessageType")),
                                FileUrl = !reader.IsDBNull(reader.GetOrdinal("FileUrl")) ? reader.GetString(reader.GetOrdinal("FileUrl")) : null,
                                FileName = !reader.IsDBNull(reader.GetOrdinal("FileName")) ? reader.GetString(reader.GetOrdinal("FileName")) : null,
                                FileSize = !reader.IsDBNull(reader.GetOrdinal("FileSize")) ? reader.GetInt32(reader.GetOrdinal("FileSize")) : 0,
                                SendTime = reader.GetDateTime(reader.GetOrdinal("SendTime")),
                                IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead"))
                            });
                        }
                    }
                }
                
                // 将好友发送的消息标记为已读
                await UpdateMessagesAsReadAsync(connection, userId, friendId);
            }
            
            // 反转消息列表，使其按时间升序排列
            messages.Reverse();
            return messages;
        }
        
        private async Task UpdateMessagesAsReadAsync(MySqlConnection connection, int userId, int friendId)
        {
            string sql = @"
                UPDATE PrivateMessages
                SET IsRead = TRUE
                WHERE SenderId = @friendId AND ReceiverId = @userId AND IsRead = FALSE";
            
            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@friendId", friendId);
                await command.ExecuteNonQueryAsync();
            }
        }
        
        /// <summary>
        /// 保存私聊消息
        /// </summary>
        public async Task<int> SavePrivateMessageAsync(MessageDTO message)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string sql = @"
                    INSERT INTO private_messages (sender_id, receiver_id, content, message_type, file_url, file_name, file_size, send_time, is_read)
                    VALUES (@senderId, @receiverId, @content, @messageType, @fileUrl, @fileName, @fileSize, @sendTime, FALSE);
                    SELECT LAST_INSERT_ID();";
                
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@senderId", message.SenderId);
                    command.Parameters.AddWithValue("@receiverId", message.ReceiverId);
                    command.Parameters.AddWithValue("@content", message.Content ?? "");
                    command.Parameters.AddWithValue("@messageType", message.MessageType ?? "text");
                    command.Parameters.AddWithValue("@fileUrl", message.FileUrl as object ?? DBNull.Value);
                    command.Parameters.AddWithValue("@fileName", message.FileName as object ?? DBNull.Value);
                    command.Parameters.AddWithValue("@fileSize", message.FileSize as object ?? DBNull.Value);
                    command.Parameters.AddWithValue("@sendTime", message.SendTime != default ? message.SendTime : DateTime.Now);
                    
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        /// <summary>
        /// 获取用户在线状态
        /// </summary>
        public bool IsUserOnline(int userId)
        {
            return _globalOnlineUsers.ContainsKey(userId) && _globalOnlineUsers[userId];
        }

        /// <summary>
        /// 设置用户在线状态
        /// </summary>
        public void SetUserOnline(int userId, bool isOnline)
        {
            if (isOnline)
            {
                _globalOnlineUsers.AddOrUpdate(userId, true, (_, __) => true);
            }
            else
            {
                _globalOnlineUsers.TryRemove(userId, out _);
            }
        }
        
        /// <summary>
        /// 获取在线用户列表
        /// </summary>
        public List<int> GetOnlineUsers()
        {
            return _globalOnlineUsers.Where(u => u.Value).Select(u => u.Key).ToList();
        }
    }
} 