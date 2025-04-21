using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace backend.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IChatService _chatService;
        private static readonly ConcurrentDictionary<string, UserConnection> _connections = new ConcurrentDictionary<string, UserConnection>();

        public ChatHub(ILogger<ChatHub> logger, IChatService chatService)
        {
            _logger = logger;
            _chatService = chatService;
        }

        // �û�����������
        public async Task JoinChatRoom(int userId, string username, int roomId)
        {
            try
            {
                _logger.LogInformation($"�û� {username}({userId}) ���Լ��������� {roomId}");
                
                string roomName = await _chatService.GetRoomNameAsync(roomId);
                
                if (string.IsNullOrEmpty(roomName))
                {
                    _logger.LogWarning($"������ {roomId} ������");
                    await Clients.Caller.SendAsync("Error", "�����Ҳ�����");
                    return;
                }

                // �����û�������Ϣ
                _connections[Context.ConnectionId] = new UserConnection
                {
                    UserId = userId,
                    Username = username,
                    RoomId = roomId,
                    ConnectionId = Context.ConnectionId
                };

                // ���û���ӵ�SignalR��
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomId}");
                
                _logger.LogInformation($"�û� {username}({userId}) �Ѽ���SignalR�� Room_{roomId}");

                // ���û���ӵ������û��б�
                var userDto = new UserDTO
                {
                    Id = userId,
                    Username = username,
                    Status = "online",
                    LastActive = DateTime.Now,
                    AvatarUrl = null // ���Դ����ݿ��ȡ
                };
                _chatService.AddUserToRoom(roomId, userDto);
                
                _logger.LogInformation($"�û� {username}({userId}) ����ӵ������� {roomId} �������û��б�");

                // ��ȡ��ʷ��Ϣ
                var messages = await _chatService.GetRoomMessagesAsync(roomId, 20);
                await Clients.Caller.SendAsync("ReceiveHistoryMessages", messages);
                
                _logger.LogInformation($"�����û� {username}({userId}) ���� {messages.Count} ����ʷ��Ϣ");

                // ����ϵͳ��Ϣ��֪ͨ�������û��Ѽ���
                var joinMessage = new MessageDTO
                {
                    MessageId = 0,
                    RoomId = roomId,
                    SenderId = 0, // ϵͳ��Ϣ������IDΪ0
                    SenderName = "ϵͳ",
                    Content = $"{username} ������������",
                    SendTime = DateTime.Now,
                    MessageType = "system",
                    IsRead = true
                };
                
                await Clients.Group($"Room_{roomId}").SendAsync("ReceiveMessage", joinMessage);
                
                _logger.LogInformation($"���������� {roomId} �����û� {username}({userId}) �����ϵͳ��Ϣ");
                
                // ��ȡ�����û��б�
                var onlineUsers = await _chatService.GetRoomOnlineUsersAsync(roomId);
                await Clients.Group($"Room_{roomId}").SendAsync("UpdateOnlineUsers", onlineUsers);
                
                _logger.LogInformation($"���������� {roomId} ���͸��º�������û��б��� {onlineUsers.Count} ��");

                _logger.LogInformation($"�û� {username}({userId}) ������������ {roomName}({roomId}) ��ȫ���������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"�û�����������ʱ��������: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "����������ʧ��: " + ex.Message);
            }
        }

        // ������Ϣ
        public async Task SendMessageToRoom(string message)
        {
            try
            {
                if (!_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Clients.Caller.SendAsync("Error", "����δ����������");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                _logger.LogInformation($"�յ������û� {userConnection.Username}({userConnection.UserId}) ����Ϣ: {message}");

                // ������Ϣ�����ݿ�
                int messageId = await _chatService.SaveRoomMessageAsync(
                    userConnection.RoomId, 
                    userConnection.UserId, 
                    message
                );
                
                _logger.LogInformation($"��Ϣ�ѱ��棬ID: {messageId}");

                if (messageId <= 0)
                {
                    _logger.LogWarning("��Ϣ����ʧ�ܣ����ص�ID��Ч");
                    await Clients.Caller.SendAsync("Error", "��Ϣ����ʧ�ܣ�������");
                    return;
                }

                // ������Ϣ����
                var messageDto = new MessageDTO
                {
                    MessageId = messageId,
                    SenderId = userConnection.UserId,
                    SenderName = userConnection.Username,
                    Content = message,
                    RoomId = userConnection.RoomId,
                    SendTime = DateTime.Now,
                    MessageType = "text",
                    IsRead = false
                };

                // ������Ϣ�����������г�Ա
                await Clients.Group($"Room_{userConnection.RoomId}").SendAsync("ReceiveMessage", messageDto);
                
                _logger.LogInformation($"��Ϣ�����͵������� {userConnection.RoomId} �����г�Ա");
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
                    
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{userConnection.RoomId}");
                    _logger.LogInformation($"�û� {userConnection.Username}({userConnection.UserId}) �Ѵ�SignalR�� Room_{userConnection.RoomId} �Ƴ�");
                    
                    // �������û��б����Ƴ��û�
                    _chatService.RemoveUserFromRoom(userConnection.RoomId, userConnection.UserId);
                    _logger.LogInformation($"�û� {userConnection.Username}({userConnection.UserId}) �Ѵ������� {userConnection.RoomId} �������û��б��Ƴ�");
                    
                    // ����ϵͳ��Ϣ��֪ͨ�������û����뿪
                    var leaveMessage = new MessageDTO
                    {
                        MessageId = 0,
                        RoomId = userConnection.RoomId,
                        SenderId = 0,
                        SenderName = "ϵͳ",
                        Content = $"{userConnection.Username} �뿪��������",
                        SendTime = DateTime.Now,
                        MessageType = "system",
                        IsRead = true
                    };
                    
                    await Clients.Group($"Room_{userConnection.RoomId}").SendAsync("ReceiveMessage", leaveMessage);
                    _logger.LogInformation($"���������� {userConnection.RoomId} �����û� {userConnection.Username}({userConnection.UserId}) �뿪��ϵͳ��Ϣ");
                    
                    // ���������û��б�
                    var onlineUsers = await _chatService.GetRoomOnlineUsersAsync(userConnection.RoomId);
                    await Clients.Group($"Room_{userConnection.RoomId}").SendAsync("UpdateOnlineUsers", onlineUsers);
                    _logger.LogInformation($"���������� {userConnection.RoomId} ���͸��º�������û��б��� {onlineUsers.Count} ��");
                    
                    _logger.LogInformation($"�û� {userConnection.Username}({userConnection.UserId}) �뿪������ {userConnection.RoomId} ��ȫ���������");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"�Ͽ����Ӵ���ʱ��������: {ex.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    // �����û�������Ϣ����
    public class UserConnection
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int RoomId { get; set; }
        public string ConnectionId { get; set; }
    }
} 