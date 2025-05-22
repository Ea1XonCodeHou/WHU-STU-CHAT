using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace backend.Hubs
{
    public class GroupChatHub : Hub
    {
        private readonly ILogger<GroupChatHub> _logger;
        private readonly IGroupService _groupService;
        private static readonly ConcurrentDictionary<string, UserConnection> _connections = new ConcurrentDictionary<string, UserConnection>();

        public GroupChatHub(ILogger<GroupChatHub> logger, IGroupService groupService)
        {
            _logger = logger;
            _groupService = groupService;
        }

        // 用户加入群组
        public async Task JoinGroup(int userId, string username, int groupId)
        {
            try
            {
                _logger.LogInformation($"用户 {username}({userId}) 尝试加入群组 {groupId}");

                // 验证参数
                if (userId <= 0)
                {
                    _logger.LogWarning($"无效的用户ID: {userId}");
                    await Clients.Caller.SendAsync("Error", "无效的用户ID");
                    return;
                }

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning($"用户 {userId} 的用户名为空");
                    await Clients.Caller.SendAsync("Error", "用户名为空");
                    return;
                }

                if (groupId <= 0)
                {
                    _logger.LogWarning($"无效的群组ID: {groupId}");
                    await Clients.Caller.SendAsync("Error", "无效的群组ID");
                    return;
                }

                // 检查群组是否存在
                var group = await _groupService.GetGroupAsync(groupId);
                if (group == null)
                {
                    _logger.LogWarning($"群组 {groupId} 不存在");
                    await Clients.Caller.SendAsync("Error", "群组不存在");
                    return;
                }

                // 检查用户是否已经是群组成员
                var groupUsers = await _groupService.GetGroupUsersAsync(groupId);
                if (!groupUsers.Any(u => u.UserId == userId))
                {
                    _logger.LogWarning($"用户 {username}({userId}) 不是群组 {groupId} 的成员");
                    await Clients.Caller.SendAsync("Error", "您不是该群组的成员");
                    return;
                }

                // 保存用户信息
                _connections[Context.ConnectionId] = new UserConnection
                {
                    UserId = userId,
                    Username = username,
                    RoomId = groupId,
                    ConnectionId = Context.ConnectionId
                };

                // 将用户添加到 SignalR 组
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{groupId}");
                _logger.LogInformation($"用户 {username}({userId}) 已加入 SignalR 组 Group_{groupId}");

                // 设置用户全局在线状态
                _groupService.SetUserOnline(userId, true);

                // 获取群组历史消息
                var messages = await _groupService.GetGroupMessagesAsync(groupId, 20);
                await Clients.Caller.SendAsync("ReceiveHistoryMessages", messages);

                // 通知群组其他成员用户已加入
                var joinMessage = new GroupMessageDTO
                {
                    MessageId = 0,
                    GroupId = groupId,
                    SenderId = 0,
                    SenderName = "系统",
                    Content = $"{username} 加入了群组",
                    CreateTime = DateTime.Now,
                    MessageType = "system"
                };
                await Clients.Group($"Group_{groupId}").SendAsync("ReceiveMessage", joinMessage);

                // 更新群组用户列表
                var users = await _groupService.GetGroupUsersAsync(groupId);
                await Clients.Group($"Group_{groupId}").SendAsync("UpdateGroupUsers", users);

                _logger.LogInformation($"用户 {username}({userId}) 成功加入群组 {groupId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"用户加入群组时发生错误: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"加入群组失败: {ex.Message}");
            }
        }

        // 发送消息到群组
        public async Task SendMessageToGroup(string message)
        {
            try
            {
                if (!_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Clients.Caller.SendAsync("Error", "用户未加入群组");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                _logger.LogInformation($"收到用户 {userConnection.Username}({userConnection.UserId}) 的消息: {message}");

                // 保存消息到数据库
                int messageId = await _groupService.SaveGroupMessageAsync(userConnection.RoomId, userConnection.UserId, message);
                if (messageId <= 0)
                {
                    _logger.LogWarning("保存消息失败");
                    await Clients.Caller.SendAsync("Error", "保存消息失败，请重试");
                    return;
                }

                // 构建消息对象
                var messageDto = new GroupMessageDTO
                {
                    MessageId = messageId,
                    GroupId = userConnection.RoomId,
                    SenderId = userConnection.UserId,
                    SenderName = userConnection.Username,
                    Content = message,
                    CreateTime = DateTime.UtcNow,
                    MessageType = "text"
                };

                // 发送消息给群组所有成员
                await Clients.Group($"Group_{userConnection.RoomId}").SendAsync("ReceiveMessage", messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送消息时发生错误: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "发送消息失败: " + ex.Message);
            }
        }

        public async Task SendImageToGroup(string imageUrl, string fileName, long fileSize)
        {
            try
            {
                if (!_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Clients.Caller.SendAsync("Error", "用户未加入群组");
                    return;
                }

                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return;
                }

                _logger.LogInformation($"收到用户 {userConnection.Username}({userConnection.UserId}) 的图片消息");

                // 保存图片消息到数据库
                int messageId = await _groupService.SaveGroupImageMessageAsync(userConnection.RoomId, userConnection.UserId, imageUrl, fileName, fileSize);
                if (messageId <= 0)
                {
                    _logger.LogWarning("保存图片消息失败");
                    await Clients.Caller.SendAsync("Error", "保存图片消息失败，请重试");
                    return;
                }

                // 构建消息对象
                var messageDto = new GroupMessageDTO
                {
                    MessageId = messageId,
                    GroupId = userConnection.RoomId,
                    SenderId = userConnection.UserId,
                    SenderName = userConnection.Username,
                    Content = imageUrl,
                    CreateTime = DateTime.UtcNow,
                    MessageType = "image",
                    FileUrl = imageUrl,
                    FileName = fileName,
                    FileSize = fileSize
                };

                // 发送消息给群组所有成员
                await Clients.Group($"Group_{userConnection.RoomId}").SendAsync("ReceiveMessage", messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送图片消息时发生错误: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "发送图片消息失败: " + ex.Message);
            }
        }

        // 用户离开群组
        public async Task LeaveGroup(int groupId)
        {
            try
            {
                if (!_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Clients.Caller.SendAsync("Error", "用户未加入群组");
                    return;
                }

                if (userConnection.RoomId != groupId)
                {
                    await Clients.Caller.SendAsync("Error", "用户不在该群组中");
                    return;
                }

                _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 离开群组 {groupId}");

                // 从SignalR组中移除
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Group_{groupId}");
                _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 已从 SignalR 组 Group_{groupId} 移除");

                // 设置用户全局离线状态（检查是否还有其他连接）
                _groupService.SetUserOnline(userConnection.UserId, false);

                

                // 通知群组其他成员用户已离开
                var leaveMessage = new GroupMessageDTO
                {
                    MessageId = 0,
                    GroupId = groupId,
                    SenderId = 0,
                    SenderName = "系统",
                    Content = $"{userConnection.Username} 离开了群组",
                    CreateTime = DateTime.Now,
                };
                await Clients.Group($"Group_{groupId}").SendAsync("ReceiveMessage", leaveMessage);

                // 更新群组用户列表
                var users = await _groupService.GetGroupUsersAsync(groupId);
                await Clients.Group($"Group_{groupId}").SendAsync("UpdateGroupUsers", users);

                _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 成功离开群组 {groupId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"用户离开群组时发生错误: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "离开群组失败: " + ex.Message);
            }
        }

        // 用户断开连接
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                if (_connections.TryRemove(Context.ConnectionId, out var userConnection))
                {
                    _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 断开连接");

                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Group_{userConnection.RoomId}");
                    _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 已从 SignalR 组 Group_{userConnection.RoomId} 移除");

                    // 设置用户全局离线状态（检查是否还有其他连接）
                    _groupService.SetUserOnline(userConnection.UserId, false);

                    

                    // 通知群组其他成员用户已离开
                    var leaveMessage = new GroupMessageDTO
                    {
                        MessageId = 0,
                        GroupId = userConnection.RoomId,
                        SenderId = 0,
                        SenderName = "系统",
                        Content = $"{userConnection.Username} 离开了群组",
                        CreateTime = DateTime.Now,
                        
                    };
                    await Clients.Group($"Group_{userConnection.RoomId}").SendAsync("ReceiveMessage", leaveMessage);

                    // 更新群组用户列表
                    var users = await _groupService.GetGroupUsersAsync(userConnection.RoomId);
                    await Clients.Group($"Group_{userConnection.RoomId}").SendAsync("UpdateGroupUsers", users);

                    _logger.LogInformation($"用户 {userConnection.Username}({userConnection.UserId}) 成功离开群组 {userConnection.RoomId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"用户断开连接时发生错误: {ex.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // 获取最新的群组聊天消息
        public async Task RequestGroupLatestMessages(int groupId, int count)
        {
            try
            {
                _logger.LogInformation($"用户请求获取群组 {groupId} 的最新 {count} 条消息");
                
                // 验证连接
                if (!_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    _logger.LogWarning($"无效的连接请求获取消息: {Context.ConnectionId}");
                    await Clients.Caller.SendAsync("Error", "未加入群组，不能获取消息");
                    return;
                }
                
                // 确保用户在请求的群组内
                if (userConnection.RoomId != groupId)
                {
                    _logger.LogWarning($"用户尝试获取非当前群组的消息: 用户在 {userConnection.RoomId}，请求 {groupId}");
                    await Clients.Caller.SendAsync("Error", "只能获取当前群组的消息");
                    return;
                }
                
                // 获取最新消息
                var messages = await _groupService.GetGroupMessagesAsync(groupId, count);
                _logger.LogInformation($"已获取群组 {groupId} 的 {messages.Count} 条最新消息");
                
                // 返回消息给调用者
                await Clients.Caller.SendAsync("ReceiveGroupHistory", messages);
                _logger.LogInformation($"已向用户 {userConnection.Username}({userConnection.UserId}) 发送最新消息");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取最新群组消息时出错: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "获取最新群组消息失败: " + ex.Message);
            }
        }
    }
}
