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
    /// �������ʵ��
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
            
            // ������ʱ�ļ�Ŀ¼
            _tempFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "uploads");
            
            // ȷ��Ŀ¼����
            if (!Directory.Exists(_tempFileDirectory))
            {
                Directory.CreateDirectory(_tempFileDirectory);
            }
        }

        /// <summary>
        /// ��ȡ����������
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
                _logger.LogError(ex, $"��ȡ����������ʧ��: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ��ȡ��������ʷ��Ϣ
        /// </summary>
        public async Task<List<MessageDTO>> GetRoomMessagesAsync(int roomId, int count)
        {
            var messages = new List<MessageDTO>();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ʹ���޸ĺ��SQL��ѯ������MessageType��FileUrl�ֶ�
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
                            
                            // �����ļ�������Ϣ������JSON���ݻ�ȡ�ļ����ʹ�С��Ϣ
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
                                            
                                        // ������ļ����ͣ�������ʾ����
                                        if (messageType == "file" && !string.IsNullOrEmpty(fileName))
                                            content = $"�ļ�: {fileName}";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "�����ļ���Ϣʧ��");
                                    // ����ʧ��ʱ����ԭʼ����
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
                
                // ��ʱ�����򷵻���Ϣ
                messages.Reverse();
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"��ȡ��������ʷ��Ϣʧ��: {ex.Message}");
                return messages;
            }
        }

        /// <summary>
        /// ������������Ϣ
        /// </summary>
        public async Task<int> SaveRoomMessageAsync(int roomId, int userId, string message)
        {
            // ���ô����͵ķ�����Ĭ��Ϊtext����
            return await SaveRoomMessageWithTypeAsync(roomId, userId, message, "text");
        }
        
        /// <summary>
        /// ������������Ϣ������Ϣ���ͣ�
        /// </summary>
        public async Task<int> SaveRoomMessageWithTypeAsync(int roomId, int userId, string message, 
            string messageType, string fileUrl = null, string fileName = null, long? fileSize = null)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // �����ļ�������Ϣ�����ļ���Ϣ���л�ΪJSON�洢
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

                    // ʹ���޸ĺ��SQL������䣬����MessageType��FileUrl�ֶ�
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
                _logger.LogError(ex, $"������������Ϣʧ��: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// �ϴ��ļ�����ʱĿ¼
        /// </summary>
        public async Task<(string FileUrl, string FileName, long FileSize)> UploadTempFileAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("û��ѡ���ļ����ļ�Ϊ��");
                }

                // �����ļ���С����10MB��
                if (file.Length > 10 * 1024 * 1024)
                {
                    throw new ArgumentException("�ļ���С��������");
                }

                // ����Ψһ�ļ���
                string fileExtension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(_tempFileDirectory, uniqueFileName);
                
                // �����ļ�
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                // ���ؿɷ��ʵ�URL���ļ���Ϣ
                string fileUrl = $"/temp/uploads/{uniqueFileName}";
                return (fileUrl, file.FileName, file.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"�ϴ��ļ�ʧ��: {ex.Message}");
                throw; // �����׳��쳣�Ա����������
            }
        }

        /// <summary>
        /// ��ȡ�����������û�
        /// </summary>
        public async Task<List<UserDTO>> GetRoomOnlineUsersAsync(int roomId)
        {
            var users = new List<UserDTO>();
            
            try
            {
                // �ȴ��ڴ��л�ȡ�����û���Ϣ
                if (_onlineUsers.TryGetValue(roomId, out var roomUsers))
                {
                    users.AddRange(roomUsers.Values);
                }
                
                // ���û�������û��������ݿ��ȡ�����Ծ�û�
                if (users.Count == 0)
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        // �޸�SQL��ѯ��ȷ��ORDER BY�а�����SELECT�б���
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
                _logger.LogError(ex, $"��ȡ�����������û�ʧ��: {ex.Message}");
                return users;
            }
        }

        /// <summary>
        /// ��ȡ�򴴽�Ĭ��������
        /// </summary>
        public async Task<int> GetOrCreateDefaultRoomAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ����Ĭ��������
                    var selectCommand = new MySqlCommand(
                        "SELECT RoomId FROM ChatRooms WHERE RoomName = 'WHU У԰����������' LIMIT 1",
                        connection);

                    var roomId = await selectCommand.ExecuteScalarAsync();
                    
                    if (roomId != null && roomId != DBNull.Value)
                    {
                        return Convert.ToInt32(roomId);
                    }

                    // ����Ĭ��������
                    var insertCommand = new MySqlCommand(
                        @"INSERT INTO ChatRooms (RoomName, Description, CreateTime, UpdateTime) 
                          VALUES ('WHU У԰����������', '��ӭ�����人��ѧУ԰���������ң������ǽ�������Ŀռ䣡', NOW(), NOW());
                          SELECT LAST_INSERT_ID();",
                        connection);

                    var newRoomId = await insertCommand.ExecuteScalarAsync();
                    return Convert.ToInt32(newRoomId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"��ȡ�򴴽�Ĭ��������ʧ��: {ex.Message}");
                return 1; // Ĭ�Ϸ���1��ͨ���ǵ�һ��������
            }
        }

        /// <summary>
        /// ����û����������б�
        /// </summary>
        public void AddUserToRoom(int roomId, UserDTO user)
        {
            var roomUsers = _onlineUsers.GetOrAdd(roomId, new ConcurrentDictionary<int, UserDTO>());
            roomUsers[user.Id] = user;
        }

        /// <summary>
        /// ���������б��Ƴ��û�
        /// </summary>
        public void RemoveUserFromRoom(int roomId, int userId)
        {
            if (_onlineUsers.TryGetValue(roomId, out var roomUsers))
            {
                roomUsers.TryRemove(userId, out _);
                
                // ����������û���û�ˣ��Ƴ������Ҽ�¼
                if (roomUsers.IsEmpty)
                {
                    _onlineUsers.TryRemove(roomId, out _);
                }
            }
        }
    }
} 