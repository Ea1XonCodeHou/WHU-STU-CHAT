using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IChatService _chatService;
        private static readonly ConcurrentDictionary<string, UserConnection> _connections = new ConcurrentDictionary<string, UserConnection>();
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ChatHub(ILogger<ChatHub> logger, IChatService chatService, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _chatService = chatService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        // 用户加入聊天室
        public async Task JoinChatRoom(int userId, string username, int roomId)
        {
            try
            {
                _logger.LogInformation($"用户 {username}({userId}) 正在加入聊天室 {roomId}");
                
                string roomName = await _chatService.GetRoomNameAsync(roomId);
                
                if (string.IsNullOrEmpty(roomName))
                {
                    _logger.LogWarning($"聊天室 {roomId} 不存在");
                    await Clients.Caller.SendAsync("Error", "聊天室不存在");
                    return;
                }

                // 保存用户连接信息
                _connections[Context.ConnectionId] = new UserConnection
                {
                    UserId = userId,
                    Username = username,
                    RoomId = roomId,
                    ConnectionId = Context.ConnectionId
                };

                // 将用户加入到SignalR组
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomId}");
                
                _logger.LogInformation($"用户 {username}({userId}) 已加入SignalR组 Room_{roomId}");

                // 设置用户在线状态
                _chatService.SetUserOnline(userId, true);
                
                // 更新用户状态到数据库
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    var statusDto = new UserStatusDTO
                    {
                        UserId = userId,
                        IsOnline = true,
                        IsVisible = true // 默认设置为可见
                    };
                    await userService.UpdateUserStatusAsync(statusDto);
                }
                
                // 将用户加入到在线用户列表
                var userDto = new UserDTO
                {
                    Id = userId,
                    Username = username,
                    Status = "online",
                    LastActive = DateTime.Now,
                    AvatarUrl = null // 可以从数据库获取
                };
                _chatService.AddUserToRoom(roomId, userDto);
                
                _logger.LogInformation($"用户 {username}({userId}) 已被加入到聊天室 {roomId} 的在线用户列表");

                // 获取历史消息
                var messages = await _chatService.GetRoomMessagesAsync(roomId, 20);
                await Clients.Caller.SendAsync("ReceiveHistoryMessages", messages);
                
                _logger.LogInformation($"已向用户 {username}({userId}) 发送 {messages.Count} 条历史消息");

                // 获取并发送在线用户列表
                var onlineUsers = await _chatService.GetRoomOnlineUsersAsync(roomId);
                await Clients.Group($"Room_{roomId}").SendAsync("UpdateOnlineUsers", onlineUsers);
                
                _logger.LogInformation($"已向聊天室 {roomId} 发送在线用户列表，共 {onlineUsers.Count} 人");

                // 发送系统消息，通知其他用户已加入
                var joinMessage = new MessageDTO
                {
                    MessageId = 0,
                    RoomId = roomId,
                    SenderId = 0, // 系统消息，发送者ID为0
                    SenderName = "系统",
                    Content = $"{username} 加入了聊天室",
                    SendTime = DateTime.Now,
                    MessageType = "system",
                    IsRead = true
                };
                
                await Clients.Group($"Room_{roomId}").SendAsync("ReceiveMessage", joinMessage);
                
                _logger.LogInformation($"已向聊天室 {roomId} 发送用户 {username}({userId}) 加入的系统消息");
                
                _logger.LogInformation($"用户 {username}({userId}) 加入聊天室 {roomName}({roomId}) 的全部流程已完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"用户加入聊天室时出现异常: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "加入聊天室失败: " + ex.Message);
            }
        }

        // 发送文本消息（保持向后兼容）
        public async Task SendMessageToRoom(string message)
        {
            await SendMessageWithTypeToRoom(message, "text");
        }
        
        // 发送消息（带类型）
        public async Task SendMessageWithTypeToRoom(string message, string messageType, string fileUrl = null, string fileName = null, long? fileSize = null)
        {
            try
            {
                if (!_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Clients.Caller.SendAsync("Error", "你还未加入聊天室");
                    return;
                }

                // 文本消息不能为空，其他类型可以（因为内容可能在fileUrl中）
                if (messageType == "text" && string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                _logger.LogInformation($"收到来自用户 {userConnection.Username}({userConnection.UserId}) 的{messageType}消息");

                // 保存消息到数据库
                int messageId = await _chatService.SaveRoomMessageWithTypeAsync(
                    userConnection.RoomId, 
                    userConnection.UserId, 
                    message,
                    messageType,
                    fileUrl,
                    fileName,
                    fileSize
                );
                
                _logger.LogInformation($"消息已保存，ID: {messageId}");

                if (messageId <= 0)
                {
                    _logger.LogWarning("消息保存失败，返回的ID无效");
                    await Clients.Caller.SendAsync("Error", "消息保存失败，请重试");
                    return;
                }

                // 准备消息对象
                var messageDto = new MessageDTO
                {
                    MessageId = messageId,
                    SenderId = userConnection.UserId,
                    SenderName = userConnection.Username,
                    Content = message,
                    RoomId = userConnection.RoomId,
                    SendTime = DateTime.Now,
                    MessageType = messageType,
                    FileUrl = fileUrl,
                    FileName = fileName,
                    FileSize = fileSize,
                    IsRead = false
                };

                // 发送消息给聊天室所有成员
                await Clients.Group($"Room_{userConnection.RoomId}").SendAsync("ReceiveMessage", messageDto);
                
                _logger.LogInformation($"消息已发送到聊天室 {userConnection.RoomId} 的所有成员");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送消息时出现异常: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "发送消息失败: " + ex.Message);
            }
        }
        
        // 发送图片消息
        public async Task SendImageToRoom(string imageUrl, string fileName, long fileSize)
        {
            await SendMessageWithTypeToRoom("", "image", imageUrl, fileName, fileSize);
        }
        
        // 发送文件消息
        public async Task SendFileToRoom(string fileUrl, string fileName, long fileSize)
        {
            await SendMessageWithTypeToRoom("", "file", fileUrl, fileName, fileSize);
        }
        
        // 发送Emoji表情
        public async Task SendEmojiToRoom(string emojiCode)
        {
            await SendMessageWithTypeToRoom(emojiCode, "emoji");
        }

        // 用户主动离开聊天室
        public async Task LeaveRoom(int roomId)
        {
            try
            {
                if (!_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Clients.Caller.SendAsync("Error", "你还未加入聊天室");
                    return;
                }

                if (userConnection.RoomId != roomId)
                {
                    await Clients.Caller.SendAsync("Error", "你不在该聊天室中");
                    return;
                }

                _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 主动离开聊天室 {roomId}");

                // 从SignalR组中移除
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{roomId}");
                _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 已从SignalR组 Room_{roomId} 移除");

                // 从在线用户列表中移除用户
                _chatService.RemoveUserFromRoom(roomId, userConnection.UserId);
                _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 已从聊天室 {roomId} 的在线用户列表移除");

                // 发送系统消息，通知其他用户已离开
                var leaveMessage = new MessageDTO
                {
                    MessageId = 0,
                    RoomId = roomId,
                    SenderId = 0,
                    SenderName = "系统",
                    Content = $"{userConnection.Username} 离开了聊天室",
                    SendTime = DateTime.Now,
                    MessageType = "system",
                    IsRead = true
                };

                await Clients.Group($"Room_{roomId}").SendAsync("ReceiveMessage", leaveMessage);
                _logger.LogInformation($"已向聊天室 {roomId} 发送用户 {userConnection.Username}({userConnection.UserId}) 离开的系统消息");

                // 更新在线用户列表
                var onlineUsers = await _chatService.GetRoomOnlineUsersAsync(roomId);
                await Clients.Group($"Room_{roomId}").SendAsync("UpdateOnlineUsers", onlineUsers);
                _logger.LogInformation($"已向聊天室 {roomId} 发送更新后的在线用户列表，共有 {onlineUsers.Count} 人");

                // 从连接字典中移除
                _connections.TryRemove(Context.ConnectionId, out _);

                // 设置用户为离线状态
                _chatService.SetUserOnline(userConnection.UserId, false);
                
                _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 离开聊天室 {roomId} 的全部流程已完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"用户离开聊天室时发生错误: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "离开聊天室失败: " + ex.Message);
            }
        }

        // 断开连接处理
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                if (_connections.TryRemove(Context.ConnectionId, out var userConnection))
                {
                    _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 断开连接");
                    
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{userConnection.RoomId}");
                    _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 已从SignalR组 Room_{userConnection.RoomId} 移除");
                    
                    // 从在线用户列表中移除用户
                    _chatService.RemoveUserFromRoom(userConnection.RoomId, userConnection.UserId);
                    _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 已从聊天室 {userConnection.RoomId} 的在线用户列表移除");
                    
                    // 发送系统消息，通知其他用户已离开
                    var leaveMessage = new MessageDTO
                    {
                        MessageId = 0,
                        RoomId = userConnection.RoomId,
                        SenderId = 0,
                        SenderName = "系统",
                        Content = $"{userConnection.Username} 离开了聊天室",
                        SendTime = DateTime.Now,
                        MessageType = "system",
                        IsRead = true
                    };
                    
                    await Clients.Group($"Room_{userConnection.RoomId}").SendAsync("ReceiveMessage", leaveMessage);
                    _logger.LogInformation($"已向聊天室 {userConnection.RoomId} 发送用户 {userConnection.Username}({userConnection.UserId}) 离开的系统消息");
                    
                    // 更新在线用户列表
                    var onlineUsers = await _chatService.GetRoomOnlineUsersAsync(userConnection.RoomId);
                    await Clients.Group($"Room_{userConnection.RoomId}").SendAsync("UpdateOnlineUsers", onlineUsers);
                    _logger.LogInformation($"已向聊天室 {userConnection.RoomId} 发送更新后的在线用户列表，共有 {onlineUsers.Count} 人");
                    
                    // 从连接字典中移除
                    _connections.TryRemove(Context.ConnectionId, out _);

                    // 设置用户为离线状态
                    _chatService.SetUserOnline(userConnection.UserId, false);
                    
                    _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 离开聊天室 {userConnection.RoomId} 的全部流程已完成");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"断开连接处理时出现异常: {ex.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // 获取最新的聊天消息
        public async Task RequestLatestMessages(int roomId, int count)
        {
            try
            {
                _logger.LogInformation($"用户请求获取聊天室 {roomId} 的最新 {count} 条消息");
                
                // 验证连接
                if (!_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    _logger.LogWarning($"无效的连接请求获取消息: {Context.ConnectionId}");
                    await Clients.Caller.SendAsync("Error", "未加入聊天室，不能获取消息");
                    return;
                }
                
                // 确保用户在请求的聊天室内
                if (userConnection.RoomId != roomId)
                {
                    _logger.LogWarning($"用户尝试获取非当前聊天室的消息: 用户在 {userConnection.RoomId}，请求 {roomId}");
                    await Clients.Caller.SendAsync("Error", "只能获取当前聊天室的消息");
                    return;
                }
                
                // 获取最新消息
                var messages = await _chatService.GetRoomMessagesAsync(roomId, count);
                _logger.LogInformation($"已获取聊天室 {roomId} 的 {messages.Count} 条最新消息");
                
                // 返回消息给调用者
                await Clients.Caller.SendAsync("ReceiveHistoryMessages", messages);
                _logger.LogInformation($"已向用户 {userConnection.Username}({userConnection.UserId}) 发送最新消息");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取最新消息时出错: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "获取最新消息失败: " + ex.Message);
            }
        }
    }

    // 用于存储用户连接信息的类
    public class UserConnection
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int RoomId { get; set; }
        public string ConnectionId { get; set; }
    }
} 