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
                _logger.LogInformation($"尝试获取聊天室 {roomId} 的名称");
                
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation($"数据库连接已打开，准备查询聊天室 {roomId}");

                    var command = new MySqlCommand(
                        "SELECT RoomName FROM chatrooms WHERE RoomId = @RoomId",
                        connection);
                    command.Parameters.AddWithValue("@RoomId", roomId);

                    var result = await command.ExecuteScalarAsync();
                    
                    if (result == null || result == DBNull.Value)
                    {
                        _logger.LogWarning($"未找到ID为 {roomId} 的聊天室");
                        
                        // 检查数据库中是否有任何聊天室
                        var checkCommand = new MySqlCommand("SELECT COUNT(*) FROM chatrooms", connection);
                        var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                        _logger.LogInformation($"数据库中共有 {count} 个聊天室");
                        
                        // 如果没有聊天室，自动创建一个
                        if (count == 0)
                        {
                            _logger.LogInformation("尝试创建默认聊天室");
                            var defaultRoomId = await GetOrCreateDefaultRoomAsync();
                            _logger.LogInformation($"已创建默认聊天室，ID: {defaultRoomId}");
                            
                            // 如果创建的默认聊天室ID与请求的ID相同，返回默认名称
                            if (defaultRoomId == roomId)
                            {
                                return "WHU 校园公共聊天室";
                            }
                        }
                        
                        return null;
                    }
                    
                    _logger.LogInformation($"成功找到聊天室 {roomId}，名称: {result}");
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
                          FROM roommessages m
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
                        @"INSERT INTO roommessages (RoomId, SenderId, Content, CreateTime, MessageType, FileUrl) 
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
                }

                // 检查文件大小是否超过10MB
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
                _logger.LogInformation($"获取聊天室 {roomId} 的在线用户");
                
                // 首先从内存缓存中获取用户
                if (_onlineUsers.TryGetValue(roomId, out var roomUsers) && roomUsers.Count > 0)
                {
                    _logger.LogInformation($"从内存缓存中找到 {roomUsers.Count} 个用户");
                    users.AddRange(roomUsers.Values);
                }
                else
                {
                    _logger.LogInformation($"内存缓存中没有找到聊天室 {roomId} 的用户或为空");
                }
                
                // 从数据库中获取在线用户列表
                // 即使内存缓存有用户，我们也从数据库获取，保证数据一致性
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // 先获取聊天室的OnlineUserIds
                    var commandRoom = new MySqlCommand(
                        "SELECT OnlineUserIds, ActiveUserCount FROM chatrooms WHERE RoomId = @RoomId",
                        connection);
                    commandRoom.Parameters.AddWithValue("@RoomId", roomId);
                    
                    using (var reader = await commandRoom.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string onlineUserIdsStr = reader.IsDBNull(reader.GetOrdinal("OnlineUserIds")) ? 
                                                     null : reader.GetString(reader.GetOrdinal("OnlineUserIds"));
                            int activeUserCount = reader.GetInt32(reader.GetOrdinal("ActiveUserCount"));
                            
                            _logger.LogInformation($"数据库中聊天室 {roomId} 的ActiveUserCount={activeUserCount}, OnlineUserIds={onlineUserIdsStr ?? "null"}");
                            
                            // 如果有在线用户ID
                            if (!string.IsNullOrEmpty(onlineUserIdsStr))
                            {
                                var onlineUserIds = onlineUserIdsStr
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(id => Convert.ToInt32(id))
                                    .ToList();
                                
                                if (onlineUserIds.Count > 0)
                                {
                                    // 如果数据库中的用户数与内存缓存不匹配，则重新从数据库获取
                                    if (onlineUserIds.Count != users.Count)
                                    {
                                        _logger.LogInformation($"数据库中有 {onlineUserIds.Count} 个用户，而缓存中有 {users.Count} 个用户，需要重新查询");
                                        users.Clear(); // 清空已有用户列表，重新获取
                                        
                                        // 重新查询这些用户的信息
                                        reader.Close(); // 关闭当前reader
                                        
                                        string userIdParams = string.Join(",", onlineUserIds);
                                        var commandUsers = new MySqlCommand(
                                            $@"SELECT u.UserId, u.Username, u.Avatar, u.Level 
                                               FROM Users u 
                                               WHERE u.UserId IN ({userIdParams})",
                                            connection);
                                        
                                        using (var userReader = await commandUsers.ExecuteReaderAsync())
                                        {
                                            while (await userReader.ReadAsync())
                                            {
                                                int userId = userReader.GetInt32(userReader.GetOrdinal("UserId"));
                                                var user = new UserDTO
                                                {
                                                    Id = userId,
                                                    Username = userReader.GetString(userReader.GetOrdinal("Username")),
                                                    Status = "online",
                                                    LastActive = DateTime.Now,
                                                    AvatarUrl = userReader.IsDBNull(userReader.GetOrdinal("Avatar")) ? 
                                                               null : userReader.GetString(userReader.GetOrdinal("Avatar")),
                                                    MemberLevel = userReader.IsDBNull(userReader.GetOrdinal("Level")) ? 
                                                               0 : userReader.GetInt32(userReader.GetOrdinal("Level"))
                                                };
                                                users.Add(user);
                                                
                                                // 更新内存缓存
                                                if (!_onlineUsers.TryGetValue(roomId, out var _))
                                                {
                                                    _onlineUsers[roomId] = new ConcurrentDictionary<int, UserDTO>();
                                                }
                                                _onlineUsers[roomId][userId] = user;
                                            }
                                        }
                                        
                                        _logger.LogInformation($"从数据库中获取到 {users.Count} 个用户，并更新了内存缓存");
                                        return users;
                                    }
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"数据库中没有找到聊天室 {roomId}");
                        }
                    }
                }
                
                // 如果仍然没有用户，则从数据库中获取最近活跃的10个用户
                if (users.Count == 0)
                {
                    _logger.LogInformation($"没有找到在线用户，尝试获取最近活跃的用户");
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        // 自定义SQL查询，确保ORDER BY子句中包含SELECT子句
                        var command = new MySqlCommand(
                            @"SELECT DISTINCT u.UserId, u.Username, u.Avatar, u.Level, MAX(m.CreateTime) as LastActivity
                              FROM Users u
                              JOIN roommessages m ON u.UserId = m.SenderId
                              WHERE m.RoomId = @RoomId
                              GROUP BY u.UserId, u.Username, u.Avatar, u.Level
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
                                                null : reader.GetString(reader.GetOrdinal("Avatar")),
                                    MemberLevel = reader.IsDBNull(reader.GetOrdinal("Level")) ? 
                                                0 : reader.GetInt32(reader.GetOrdinal("Level"))
                                };
                                users.Add(user);
                            }
                        }
                        
                        _logger.LogInformation($"从数据库中获取到 {users.Count} 个最近活跃的用户");
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

                    // 检查默认房间是否存在
                    var checkCommand = new MySqlCommand(
                        "SELECT RoomId FROM chatrooms WHERE RoomName = 'WHU 院系交流群' LIMIT 1",
                        connection);

                    var existingRoomId = await checkCommand.ExecuteScalarAsync();
                    if (existingRoomId != null && Convert.ToInt32(existingRoomId) > 0)
                    {
                        return Convert.ToInt32(existingRoomId);
                    }

                    // 创建默认房间
                    var createCommand = new MySqlCommand(
                        @"INSERT INTO chatrooms (RoomName, Description, CreateTime, UpdateTime)
                          VALUES ('WHU 院系交流群', '欢迎来到武汉大学院系交流群！', NOW(), NOW());
                          SELECT LAST_INSERT_ID();",
                        connection);

                    var newRoomId = Convert.ToInt32(await createCommand.ExecuteScalarAsync());
                    return newRoomId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取或创建默认房间失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 添加用户到房间
        /// </summary>
        public void AddUserToRoom(int roomId, UserDTO user)
        {
            try
            {
                var roomUsers = _onlineUsers.GetOrAdd(roomId, new ConcurrentDictionary<int, UserDTO>());
                roomUsers[user.Id] = user;
                
                // 更新数据库中的ActiveUserCount和OnlineUserIds
                Task.Run(async () => {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        
                        // 获取当前的在线用户ID列表
                        var command = new MySqlCommand(
                            "SELECT OnlineUserIds FROM chatrooms WHERE RoomId = @RoomId", 
                            connection);
                        command.Parameters.AddWithValue("@RoomId", roomId);
                        
                        var onlineUserIdsStr = await command.ExecuteScalarAsync() as string;
                        List<int> onlineUserIds = new List<int>();
                        
                        if (!string.IsNullOrEmpty(onlineUserIdsStr))
                        {
                            onlineUserIds = onlineUserIdsStr
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(id => Convert.ToInt32(id))
                                .ToList();
                        }
                        
                        // 如果用户ID不在列表中，则添加
                        if (!onlineUserIds.Contains(user.Id))
                        {
                            onlineUserIds.Add(user.Id);
                            
                            // 更新数据库
                            var updateCommand = new MySqlCommand(
                                "UPDATE chatrooms SET ActiveUserCount = @ActiveUserCount, OnlineUserIds = @OnlineUserIds, UpdateTime = @UpdateTime WHERE RoomId = @RoomId", 
                                connection);
                            updateCommand.Parameters.AddWithValue("@ActiveUserCount", onlineUserIds.Count);
                            updateCommand.Parameters.AddWithValue("@OnlineUserIds", string.Join(",", onlineUserIds));
                            updateCommand.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                            updateCommand.Parameters.AddWithValue("@RoomId", roomId);
                            
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"添加用户到聊天室失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从聊天室列表中移除用户
        /// </summary>
        public void RemoveUserFromRoom(int roomId, int userId)
        {
            try
            {
                if (_onlineUsers.TryGetValue(roomId, out var roomUsers))
                {
                    roomUsers.TryRemove(userId, out _);
                    
                    // 如果房间没有用户了，可以选择移除整个房间记录
                    if (roomUsers.IsEmpty)
                    {
                        _onlineUsers.TryRemove(roomId, out _);
                    }
                    
                    // 更新数据库中的ActiveUserCount和OnlineUserIds
                    Task.Run(async () => {
                        using (var connection = new MySqlConnection(_connectionString))
                        {
                            await connection.OpenAsync();
                            
                            // 获取当前的在线用户ID列表
                            var command = new MySqlCommand(
                                "SELECT OnlineUserIds FROM chatrooms WHERE RoomId = @RoomId", 
                                connection);
                            command.Parameters.AddWithValue("@RoomId", roomId);
                            
                            var onlineUserIdsStr = await command.ExecuteScalarAsync() as string;
                            List<int> onlineUserIds = new List<int>();
                            
                            if (!string.IsNullOrEmpty(onlineUserIdsStr))
                            {
                                onlineUserIds = onlineUserIdsStr
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(id => Convert.ToInt32(id))
                                    .ToList();
                            }
                            
                            // 如果用户ID在列表中，则移除
                            if (onlineUserIds.Contains(userId))
                            {
                                onlineUserIds.Remove(userId);
                                
                                // 更新数据库
                                var updateCommand = new MySqlCommand(
                                    "UPDATE chatrooms SET ActiveUserCount = @ActiveUserCount, OnlineUserIds = @OnlineUserIds, UpdateTime = @UpdateTime WHERE RoomId = @RoomId", 
                                    connection);
                                updateCommand.Parameters.AddWithValue("@ActiveUserCount", onlineUserIds.Count);
                                updateCommand.Parameters.AddWithValue("@OnlineUserIds", string.Join(",", onlineUserIds));
                                updateCommand.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                                updateCommand.Parameters.AddWithValue("@RoomId", roomId);
                                
                                await updateCommand.ExecuteNonQueryAsync();
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"从聊天室移除用户失败: {ex.Message}");
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
                    FROM privatemessages pm
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
                UPDATE privatemessages
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
                    INSERT INTO privatemessages (SenderId, ReceiverId, Content, MessageType, FileUrl, FileName, FileSize, IsRead, CreateTime)
                    VALUES (@senderId, @receiverId, @content, @messageType, @fileUrl, @fileName, @fileSize, FALSE, @createTime);
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
                    command.Parameters.AddWithValue("@createTime", DateTime.UtcNow);
                    
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