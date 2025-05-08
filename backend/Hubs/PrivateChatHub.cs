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
                
                _logger.LogInformation($"用户 {userId} 已注册私聊连接");
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
                
                _logger.LogInformation($"用户 {userId} 已加入私聊组 {chatGroupName}");
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
        
        // 断开连接处理
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // 查找并移除断开连接的用户
            foreach (var kvp in _userConnections)
            {
                if (kvp.Value == Context.ConnectionId)
                {
                    _userConnections.TryRemove(kvp.Key, out _);
                    _logger.LogInformation($"用户 {kvp.Key} 断开私聊连接");
                    break;
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
} 