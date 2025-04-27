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

        // �û�����Ⱥ��
        public async Task JoinGroup(int userId, string username, int groupId)
        {
            try
            {
                _logger.LogInformation($"�û� {username}({userId}) ���Լ���Ⱥ�� {groupId}");

                //var group = await _groupService.GetGroupAsync(groupId);//��һ��Ҫע�͵���
                var group = _groupService.GetGroupAsync(groupId);
                if (group == null)
                {
                    _logger.LogWarning($"Ⱥ�� {groupId} ������");
                    await Clients.Caller.SendAsync("Error", "Ⱥ�鲻����");
                    return;
                }

                // �����û�������Ϣ
                _connections[Context.ConnectionId] = new UserConnection
                {
                    UserId = userId,
                    Username = username,
                    RoomId = groupId,
                    ConnectionId = Context.ConnectionId
                };

                // ���û���ӵ� SignalR ��
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{groupId}");
                _logger.LogInformation($"�û� {username}({userId}) �Ѽ��� SignalR �� Group_{groupId}");

                // ����û���Ⱥ��
                var success = await _groupService.AddUserToGroupAsync(groupId, userId);
                if (!success)
                {
                    _logger.LogWarning($"�޷����û� {username}({userId}) ��ӵ�Ⱥ�� {groupId}");
                    await Clients.Caller.SendAsync("Error", "����Ⱥ��ʧ��");
                    return;
                }

                // ��ȡȺ����ʷ��Ϣ
                var messages = await _groupService.GetGroupMessagesAsync(groupId, 20);
                await Clients.Caller.SendAsync("ReceiveHistoryMessages", messages);

                // ֪ͨȺ��������Ա�û��Ѽ���
                var joinMessage = new GroupMessageDTO
                {
                    MessageId = 0,
                    GroupId = groupId,
                    SenderId = 0,
                    SenderName = "ϵͳ",
                    Content = $"{username} ������Ⱥ��",
                    CreateTime = DateTime.Now,
                    
                };
                await Clients.Group($"Group_{groupId}").SendAsync("ReceiveMessage", joinMessage);

                // ����Ⱥ���û��б�
                var users = await _groupService.GetGroupUsersAsync(groupId);
                await Clients.Group($"Group_{groupId}").SendAsync("UpdateGroupUsers", users);

                _logger.LogInformation($"�û� {username}({userId}) �ɹ�����Ⱥ�� {groupId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"�û�����Ⱥ��ʱ��������: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "����Ⱥ��ʧ��: " + ex.Message);
            }
        }

        // ������Ϣ��Ⱥ��
        public async Task SendMessageToGroup(string message)
        {
            try
            {
                if (!_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Clients.Caller.SendAsync("Error", "����δ����Ⱥ��");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                _logger.LogInformation($"�յ������û� {userConnection.Username}({userConnection.UserId}) ����Ϣ: {message}");

                // ������Ϣ�����ݿ�
                int messageId = await _groupService.SaveGroupMessageAsync(userConnection.RoomId, userConnection.UserId, message);
                if (messageId <= 0)
                {
                    _logger.LogWarning("��Ϣ����ʧ��");
                    await Clients.Caller.SendAsync("Error", "��Ϣ����ʧ�ܣ�������");
                    return;
                }

                // ������Ϣ����
                var messageDto = new GroupMessageDTO
                {
                    MessageId = messageId,
                    SenderId = userConnection.UserId,
                    SenderName = userConnection.Username,
                    Content = message,
                    GroupId = userConnection.RoomId,
                    CreateTime = DateTime.Now,
                    
                };

                // ������Ϣ��Ⱥ�����г�Ա
                await Clients.Group($"Group_{userConnection.RoomId}").SendAsync("ReceiveMessage", messageDto);
                _logger.LogInformation($"��Ϣ�����͵�Ⱥ�� {userConnection.RoomId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"������Ϣʱ��������: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "������Ϣʧ��: " + ex.Message);
            }
        }

        // �Ͽ����Ӵ���
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                if (_connections.TryRemove(Context.ConnectionId, out var userConnection))
                {
                    _logger.LogInformation($"�û� {userConnection.Username}({userConnection.UserId}) �Ͽ�����");

                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Group_{userConnection.RoomId}");
                    _logger.LogInformation($"�û� {userConnection.Username}({userConnection.UserId}) �Ѵ� SignalR �� Group_{userConnection.RoomId} �Ƴ�");

                    // ��Ⱥ���Ƴ��û�
                    await _groupService.RemoveUserFromGroupAsync(userConnection.RoomId, userConnection.UserId);

                    // ֪ͨȺ��������Ա�û����뿪
                    var leaveMessage = new GroupMessageDTO
                    {
                        MessageId = 0,
                        GroupId = userConnection.RoomId,
                        SenderId = 0,
                        SenderName = "ϵͳ",
                        Content = $"{userConnection.Username} �뿪��Ⱥ��",
                        CreateTime = DateTime.Now,
                        
                    };
                    await Clients.Group($"Group_{userConnection.RoomId}").SendAsync("ReceiveMessage", leaveMessage);

                    // ����Ⱥ���û��б�
                    var users = await _groupService.GetGroupUsersAsync(userConnection.RoomId);
                    await Clients.Group($"Group_{userConnection.RoomId}").SendAsync("UpdateGroupUsers", users);

                    _logger.LogInformation($"�û� {userConnection.Username}({userConnection.UserId}) �뿪Ⱥ�� {userConnection.RoomId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"�Ͽ����Ӵ���ʱ��������: {ex.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
