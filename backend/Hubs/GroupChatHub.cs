using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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

                //var group = await _groupService.GetGroupAsync(groupId);//要注意的是
                var group = _groupService.GetGroupAsync(groupId);
                if (group == null)
                {
                    _logger.LogWarning($"群组 {groupId} 不存在");
                    await Clients.Caller.SendAsync("Error", "群组不存在");
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

                // 将用户添加到群组
                var success = await _groupService.AddUserToGroupAsync(groupId, userId);
                if (!success)
                {
                    _logger.LogWarning($"无法将用户 {username}({userId}) 添加到群组 {groupId}");
                    await Clients.Caller.SendAsync("Error", "添加用户到群组失败");
                    return;
                }

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
                await Clients.Caller.SendAsync("Error", "加入群组失败: " + ex.Message);
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
                    SenderId = userConnection.UserId,
                    SenderName = userConnection.Username,
                    Content = message,
                    GroupId = userConnection.RoomId,
                    CreateTime = DateTime.Now,
                    
                };

                // 将消息发送给群组所有成员
                await Clients.Group($"Group_{userConnection.RoomId}").SendAsync("ReceiveMessage", messageDto);
                _logger.LogInformation($"消息已发送给群组 {userConnection.RoomId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送消息时发生错误: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "发送消息失败: " + ex.Message);
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

                // 从群组中移除用户
                await _groupService.RemoveUserFromGroupAsync(groupId, userConnection.UserId);

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

                    // 从群组中移除用户
                    //await _groupService.RemoveUserFromGroupAsync(userConnection.RoomId, userConnection.UserId);

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
    }
}
