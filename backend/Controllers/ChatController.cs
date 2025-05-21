using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using backend.Services;
using backend.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ILogger<ChatController> _logger;
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly string _connectionString;
        
        public ChatController(ILogger<ChatController> logger, IChatService chatService, IUserService userService, IConfiguration configuration)
        {
            _logger = logger;
            _chatService = chatService;
            _userService = userService;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        
        [HttpGet("history/private/{friendId}")]
        public async Task<IActionResult> GetPrivateChatHistory(int friendId, [FromQuery] int count = 50)
        {
            try
            {
                // 从token或Session中获取当前用户ID
                int userId = int.Parse(User.Identity?.Name ?? HttpContext.Session.GetString("UserId") ?? "0");
                if (userId <= 0)
                {
                    // 如果无法从认证上下文获取，则尝试从请求头获取
                    if (Request.Headers.TryGetValue("UserId", out var userIdHeader))
                    {
                        userId = int.Parse(userIdHeader);
                    }
                    else
                    {
                        return Unauthorized(new { message = "无法识别当前用户" });
                    }
                }
                
                if (friendId <= 0)
                {
                    return BadRequest(new { message = "好友ID无效" });
                }
                
                // 获取私聊历史消息
                var messages = await _chatService.GetPrivateChatHistoryAsync(userId, friendId, count);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"获取私聊历史消息失败: {ex.Message}" });
            }
        }
        
        [HttpPost("private/message")]
        public async Task<IActionResult> SendPrivateMessage([FromBody] MessageDTO message)
        {
            try
            {
                if (message == null)
                {
                    return BadRequest(new { message = "消息内容不能为空" });
                }
                
                // 从token或Session中获取当前用户ID
                int userId = int.Parse(User.Identity?.Name ?? HttpContext.Session.GetString("UserId") ?? "0");
                if (userId <= 0)
                {
                    // 如果无法从认证上下文获取，则尝试从请求头获取
                    if (Request.Headers.TryGetValue("UserId", out var userIdHeader))
                    {
                        userId = int.Parse(userIdHeader);
                    }
                    else
                    {
                        return Unauthorized(new { message = "无法识别当前用户" });
                    }
                }
                
                // 设置发送者ID
                message.SenderId = userId;
                
                // 保存消息
                int messageId = await _chatService.SavePrivateMessageAsync(message);
                if (messageId <= 0)
                {
                    return StatusCode(500, new { message = "保存消息失败" });
                }
                
                // 返回带有ID的消息对象
                message.MessageId = messageId;
                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"发送私聊消息失败: {ex.Message}" });
            }
        }
        
        /// <summary>
        /// 获取聊天室信息
        /// </summary>
        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetRoomInfo(int roomId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand(
                        "SELECT RoomId, RoomName, Description, ActiveUserCount, OnlineUserIds FROM chatrooms WHERE RoomId = @RoomId",
                        connection);
                    command.Parameters.AddWithValue("@RoomId", roomId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var roomInfo = new
                            {
                                RoomId = reader.GetInt32(reader.GetOrdinal("RoomId")),
                                RoomName = reader.GetString(reader.GetOrdinal("RoomName")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                ActiveUserCount = reader.GetInt32(reader.GetOrdinal("ActiveUserCount")),
                                OnlineUserIds = reader.IsDBNull(reader.GetOrdinal("OnlineUserIds")) ? null : reader.GetString(reader.GetOrdinal("OnlineUserIds"))
                            };

                            return Ok(new { code = 200, data = roomInfo });
                        }
                        else
                        {
                            return NotFound(new { code = 404, msg = "聊天室不存在" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取聊天室信息失败: {ex.Message}");
                return StatusCode(500, new { code = 500, msg = "获取聊天室信息失败" });
            }
        }
        
        /// <summary>
        /// 获取聊天室在线用户列表
        /// </summary>
        [HttpGet("room/{roomId}/users")]
        public async Task<IActionResult> GetRoomOnlineUsers(int roomId)
        {
            try
            {
                // 从数据库中获取在线用户ID列表
                List<UserDTO> onlineUsers = new List<UserDTO>();
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 获取在线用户ID列表
                    var commandIds = new MySqlCommand(
                        "SELECT OnlineUserIds FROM chatrooms WHERE RoomId = @RoomId",
                        connection);
                    commandIds.Parameters.AddWithValue("@RoomId", roomId);

                    string onlineUserIdsStr = await commandIds.ExecuteScalarAsync() as string;
                    if (!string.IsNullOrEmpty(onlineUserIdsStr))
                    {
                        // 解析用户ID列表
                        var userIds = onlineUserIdsStr
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(id => Convert.ToInt32(id))
                            .ToList();

                        if (userIds.Count > 0)
                        {
                            // 查询用户信息
                            string userIdParams = string.Join(",", userIds);
                            var commandUsers = new MySqlCommand(
                                $@"SELECT u.UserId, u.Username, u.Avatar, u.Level 
                                   FROM Users u 
                                   WHERE u.UserId IN ({userIdParams})",
                                connection);

                            using (var reader = await commandUsers.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var user = new UserDTO
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("UserId")),
                                        Username = reader.GetString(reader.GetOrdinal("Username")),
                                        Status = "online",
                                        LastActive = DateTime.Now,
                                        AvatarUrl = reader.IsDBNull(reader.GetOrdinal("Avatar")) ?
                                                  null : reader.GetString(reader.GetOrdinal("Avatar")),
                                        MemberLevel = reader.IsDBNull(reader.GetOrdinal("Level")) ?
                                                  0 : reader.GetInt32(reader.GetOrdinal("Level"))
                                    };
                                    onlineUsers.Add(user);
                                }
                            }
                        }
                    }
                }

                // 如果没有在线用户，则从内存中获取
                if (onlineUsers.Count == 0)
                {
                    onlineUsers = await _chatService.GetRoomOnlineUsersAsync(roomId);
                }

                return Ok(new { code = 200, data = onlineUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取聊天室在线用户失败: {ex.Message}");
                return StatusCode(500, new { code = 500, msg = "获取聊天室在线用户失败" });
            }
        }
    }
} 