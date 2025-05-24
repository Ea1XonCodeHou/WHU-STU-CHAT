using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace backend.Hubs
{
    public class PrivateChatHub : Hub
    {
        private readonly ILogger<PrivateChatHub> _logger;
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private static readonly ConcurrentDictionary<int, string> _userConnections = new ConcurrentDictionary<int, string>();
        
        public PrivateChatHub(ILogger<PrivateChatHub> logger, IChatService chatService, IUserService userService)
        {
            _logger = logger;
            _chatService = chatService;
            _userService = userService;
        }
        
        // 用户连接时注册
        public async Task RegisterConnection(int userId)
        {
            try
            {
                _logger.LogInformation($"用户 {userId} 正在注册私聊连接");
                
                // 保存连接信息
                _userConnections[userId] = Context.ConnectionId;
                
                // 设置用户在线状态
                _chatService.SetUserOnline(userId, true);
                
                // 更新用户状态到数据库
                var statusDto = new UserStatusDTO
                {
                    UserId = userId,
                    IsOnline = true,
                    IsVisible = true
                };
                await _userService.UpdateUserStatusAsync(statusDto);
                
                _logger.LogInformation($"用户 {userId} 已注册私聊连接并设置为在线状态");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"注册私聊连接时出错: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "注册连接失败: " + ex.Message);
            }
        }
        
        // 加入私聊
        public async Task JoinPrivateChat(int friendId)
        {
            try
            {
                // 从request中获取用户ID
                if (!Context.GetHttpContext().Request.Query.TryGetValue("userId", out var userIdValue) ||
                    !int.TryParse(userIdValue, out int userId))
                {
                    await Clients.Caller.SendAsync("Error", "无法识别用户ID");
                    return;
                }
                
                _logger.LogInformation($"用户 {userId} 正在加入与用户 {friendId} 的私聊");
                
                // 创建私聊组名称（较小ID在前，确保两个用户加入同一组）
                string chatGroupName = userId < friendId 
                    ? $"private_{userId}_{friendId}" 
                    : $"private_{friendId}_{userId}";
                
                // 将用户加入到私聊组
                await Groups.AddToGroupAsync(Context.ConnectionId, chatGroupName);
                
                // 设置用户在线状态
                _chatService.SetUserOnline(userId, true);
                
                // 更新用户状态到数据库
                var statusDto = new UserStatusDTO
                {
                    UserId = userId,
                    IsOnline = true,
                    IsVisible = true
                };
                await _userService.UpdateUserStatusAsync(statusDto);
                
                // 通知对方用户状态更新
                if (_userConnections.TryGetValue(friendId, out var friendConnectionId))
                {
                    await Clients.Client(friendConnectionId).SendAsync("UserStatusChanged", userId, "online");
                }
                
                _logger.LogInformation($"用户 {userId} 已加入私聊组 {chatGroupName} 并设置为在线状态");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"加入私聊时出错: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "加入私聊失败: " + ex.Message);
            }
        }
        
        // 发送私聊消息
        public async Task SendPrivateMessage(MessageDTO message)
        {
            try
            {
                if (message == null || string.IsNullOrEmpty(message.Content))
                {
                    await Clients.Caller.SendAsync("Error", "消息内容不能为空");
                    return;
                }
                
                int senderId = message.SenderId;
                int receiverId = message.ReceiverId;
                
                // 记录消息内容，便于调试
                _logger.LogInformation($"接收到私聊消息: SenderId={senderId}, ReceiverId={receiverId}, Content={message.Content}");
                
                // 创建私聊组名称
                string chatGroupName = senderId < receiverId 
                    ? $"private_{senderId}_{receiverId}" 
                    : $"private_{receiverId}_{senderId}";
                
                // 保存消息到数据库
                int messageId = await _chatService.SavePrivateMessageAsync(message);
                if (messageId <= 0)
                {
                    await Clients.Caller.SendAsync("Error", "保存消息失败");
                    return;
                }
                
                // 补充消息信息
                message.MessageId = messageId;
                if (string.IsNullOrEmpty(message.SenderName))
                {
                    var senderInfo = await _userService.GetUserByIdAsync(senderId);
                    message.SenderName = senderInfo?.Username ?? "未知用户";
                    message.SenderAvatar = senderInfo?.AvatarUrl;
                }
                
                // 发送消息到私聊组
                await Clients.Group(chatGroupName).SendAsync("ReceivePrivateMessage", message);
                
                _logger.LogInformation($"私聊消息已发送到组 {chatGroupName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送私聊消息时出错: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "发送消息失败: " + ex.Message);
            }
        }
        
        // 获取私聊历史消息
        public async Task GetPrivateChatHistory(int friendId, int count = 50)
        {
            try
            {
                // 从request中获取用户ID
                if (!Context.GetHttpContext().Request.Query.TryGetValue("userId", out var userIdValue) ||
                    !int.TryParse(userIdValue, out int userId))
                {
                    await Clients.Caller.SendAsync("Error", "无法识别用户ID");
                    return;
                }
                
                _logger.LogInformation($"用户 {userId} 正在获取与用户 {friendId} 的私聊历史消息");
                
                // 获取历史消息
                var messages = await _chatService.GetPrivateChatHistoryAsync(userId, friendId, count);
                
                // 发送历史消息给客户端
                await Clients.Caller.SendAsync("ReceiveHistoryMessages", messages);
                
                _logger.LogInformation($"已发送 {messages.Count} 条历史消息给用户 {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取私聊历史消息时出错: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "获取历史消息失败: " + ex.Message);
            }
        }
        
        // 发送文件消息
        public async Task SendFileToPrivate(int receiverId, string fileUrl, string fileName, long fileSize)
        {
            try
            {
                // 从request中获取用户ID
                if (!Context.GetHttpContext().Request.Query.TryGetValue("userId", out var userIdValue) ||
                    !int.TryParse(userIdValue, out int senderId))
                {
                    await Clients.Caller.SendAsync("Error", "无法识别用户ID");
                    return;
                }
                
                _logger.LogInformation($"用户 {senderId} 正在发送文件给用户 {receiverId}: {fileName}");
                
                // 创建消息对象
                var message = new MessageDTO
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = fileName,
                    MessageType = "file",
                    FileUrl = fileUrl,
                    FileName = fileName,
                    FileSize = fileSize,
                    SendTime = DateTime.Now
                };
                
                // 创建私聊组名称
                string chatGroupName = senderId < receiverId 
                    ? $"private_{senderId}_{receiverId}" 
                    : $"private_{receiverId}_{senderId}";
                
                // 保存消息到数据库
                int messageId = await _chatService.SavePrivateMessageAsync(message);
                if (messageId <= 0)
                {
                    await Clients.Caller.SendAsync("Error", "保存文件消息失败");
                    return;
                }
                
                // 补充消息信息
                message.MessageId = messageId;
                var senderInfo = await _userService.GetUserByIdAsync(senderId);
                message.SenderName = senderInfo?.Username ?? "未知用户";
                
                // 发送消息到私聊组
                await Clients.Group(chatGroupName).SendAsync("ReceivePrivateMessage", message);
                
                _logger.LogInformation($"文件消息已发送到组 {chatGroupName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送文件消息时出错: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "发送文件消息失败: " + ex.Message);
            }
        }
        
        // 发送图片消息
        public async Task SendImageToPrivate(int receiverId, string imageUrl, string fileName, long fileSize)
        {
            try
            {
                // 从request中获取用户ID
                if (!Context.GetHttpContext().Request.Query.TryGetValue("userId", out var userIdValue) ||
                    !int.TryParse(userIdValue, out int senderId))
                {
                    await Clients.Caller.SendAsync("Error", "无法识别用户ID");
                    return;
                }
                
                _logger.LogInformation($"用户 {senderId} 正在发送图片给用户 {receiverId}: {fileName}");
                
                // 创建消息对象
                var message = new MessageDTO
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = fileName,
                    MessageType = "image",
                    FileUrl = imageUrl,
                    FileName = fileName,
                    FileSize = fileSize,
                    SendTime = DateTime.Now
                };
                
                // 创建私聊组名称
                string chatGroupName = senderId < receiverId 
                    ? $"private_{senderId}_{receiverId}" 
                    : $"private_{receiverId}_{senderId}";
                
                // 保存消息到数据库
                int messageId = await _chatService.SavePrivateMessageAsync(message);
                if (messageId <= 0)
                {
                    await Clients.Caller.SendAsync("Error", "保存图片消息失败");
                    return;
                }
                
                // 补充消息信息
                message.MessageId = messageId;
                var senderInfo = await _userService.GetUserByIdAsync(senderId);
                message.SenderName = senderInfo?.Username ?? "未知用户";
                
                // 发送消息到私聊组
                await Clients.Group(chatGroupName).SendAsync("ReceivePrivateMessage", message);
                
                _logger.LogInformation($"图片消息已发送到组 {chatGroupName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送图片消息时出错: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "发送图片消息失败: " + ex.Message);
            }
        }
        
        // 断开连接处理
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                // 查找并移除断开连接的用户
                foreach (var kvp in _userConnections)
                {
                    if (kvp.Value == Context.ConnectionId)
                    {
                        int userId = kvp.Key;
                        _userConnections.TryRemove(userId, out _);
                        
                        // 设置用户离线状态
                        _chatService.SetUserOnline(userId, false);
                        
                        // 更新用户状态到数据库
                        var statusDto = new UserStatusDTO
                        {
                            UserId = userId,
                            IsOnline = false,
                            IsVisible = true
                        };
                        await _userService.UpdateUserStatusAsync(statusDto);
                        
                        // 通知所有在线用户该用户已离线
                        foreach (var connection in _userConnections)
                        {
                            await Clients.Client(connection.Value).SendAsync("UserStatusChanged", userId, "offline");
                        }
                        
                        _logger.LogInformation($"用户 {userId} 断开私聊连接并设置为离线状态");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"断开连接处理时出错: {ex.Message}");
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
} 