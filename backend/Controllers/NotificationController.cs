using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IGroupService _groupService;
        private readonly IFriendshipService _friendshipService;

        public NotificationController(
            IUserService userService,
            INotificationService notificationService,
            IGroupService groupService,
            IFriendshipService friendshipService)
        {
            _userService = userService;
            _notificationService = notificationService;
            _groupService = groupService;
            _friendshipService = friendshipService;
        }

        // 1. 发送好友请求
        [HttpPost("friend-request")]
        public async Task<IActionResult> SendFriendRequest([FromBody] FriendRequestDTO dto)
        {
            var targetUser = await _userService.GetUserByUsernameAsync(dto.TargetUsername);
            if (targetUser == null)
                return NotFound(new { msg = "用户不存在" });
                
            // 获取请求者信息
            var requester = await _userService.GetUserByUsernameAsync(dto.RequesterUsername);
            if (requester == null)
                return NotFound(new { msg = "请求者不存在" });
                
            // 检查是否已经是好友
            bool isFriend = await _friendshipService.CheckIsFriendAsync(requester.Id, targetUser.Id);
            if (isFriend)
            {
                return BadRequest(new { msg = "已经是好友关系，无需重复添加" });
            }

            // 创建好友请求
            await _friendshipService.CreateFriendRequestAsync(requester.Id, targetUser.Id);

            // 发送带验证消息的好友请求通知
            var content = $"{dto.RequesterUsername} 请求加你为好友\n验证消息: {dto.Message}";
            await _notificationService.CreateNotificationAsync(targetUser.Id, content, "friend_request");

            return Ok(new { msg = "好友请求已发送" });
        }

        // 2. 获取用户所有通知
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId);
            return Ok(notifications);
        }

        // 3. 同意好友请求（创建好友关系并创建私聊群）
        [HttpPost("accept-friend")]
        public async Task<IActionResult> AcceptFriend([FromBody] AcceptFriendDTO dto)
        {
            var notification = await _notificationService.GetNotificationByIdAsync(dto.NotificationId);
            if (notification == null) return NotFound(new { msg = "通知不存在" });

            // 假设格式为"xxx 请求加你为好友"
            var requesterUsername = notification.Content.Split(' ')[0];
            var requester = await _userService.GetUserByUsernameAsync(requesterUsername);
            var receiver = await _userService.GetUserByIdAsync(notification.UserId);

            if (requester == null || receiver == null)
                return BadRequest(new { msg = "用户信息有误" });

            // 接受好友请求（更新Friendships表）
            await _friendshipService.AcceptFriendRequestAsync(receiver.Id, requester.Id);

            // 创建群聊（只有两人）用于私聊
            var groupRegDto = new GroupRegDTO
            {
                GroupName = $"{requester.Username}和{receiver.Username}的私聊",
                CreatorId = requester.Id,
                MemberCount = 1, // 只包含创建者
                Description = "私聊"
            };
            var groupId = await _groupService.CreateGroupAsync(groupRegDto);
            await _groupService.AddUserToGroupAsync(groupId, receiver.Id);

            // 标记通知为已处理
            await _notificationService.MarkAsHandled(dto.NotificationId);

            return Ok(new { msg = "已同意好友请求，已创建私聊" });
        }
        
        // 4. 删除好友（同时删除私聊群组）
        [HttpDelete("friend/{userId}/{friendId}")]
        public async Task<IActionResult> DeleteFriend(int userId, int friendId)
        {
            try
            {
                // 先检查是否为好友
                bool isFriend = await _friendshipService.CheckIsFriendAsync(userId, friendId);
                if (!isFriend)
                {
                    return NotFound(new { msg = "未找到与该用户的好友关系" });
                }
                
                // 删除好友关系
                await _friendshipService.DeleteFriendshipAsync(userId, friendId);
                
                // 获取并删除私聊群组
                var privateGroups = await _groupService.GetPrivateGroupBetweenUsersAsync(userId, friendId);
                if (privateGroups != null && privateGroups.Count > 0)
                {
                    foreach (var group in privateGroups)
                    {
                        await _groupService.DeleteGroupAsync(group.GroupId);
                    }
                }
                
                // 创建通知告知对方
                var currentUser = await _userService.GetUserByIdAsync(userId);
                await _notificationService.CreateNotificationAsync(
                    friendId, 
                    $"{currentUser.Username} 已将你从好友列表中移除");
                
                return Ok(new { msg = "好友已删除" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { msg = $"删除好友失败: {ex.Message}" });
            }
        }

        // 5. 拒绝好友请求
        [HttpPost("reject-friend")]
        public async Task<IActionResult> RejectFriend([FromBody] AcceptFriendDTO dto)
        {
            var notification = await _notificationService.GetNotificationByIdAsync(dto.NotificationId);
            if (notification == null) return NotFound(new { msg = "通知不存在" });
            
            // 检查通知类型
            if (notification.Type != "friend_request")
                return BadRequest(new { msg = "此通知不是好友请求" });
            
            // 假设格式为"xxx 请求加你为好友"
            var requesterUsername = notification.Content.Split(' ')[0];
            var requester = await _userService.GetUserByUsernameAsync(requesterUsername);
            var receiver = await _userService.GetUserByIdAsync(notification.UserId);

            if (requester == null || receiver == null)
                return BadRequest(new { msg = "用户信息有误" });
                
            // 拒绝好友请求（更新Friendships表）
            await _friendshipService.RejectFriendRequestAsync(receiver.Id, requester.Id);
                
            // 标记通知为已处理
            await _notificationService.MarkAsHandled(dto.NotificationId);
            
            return Ok(new { msg = "已拒绝好友请求" });
        }
        
        // 6. 获取好友状态
        [HttpGet("friendship/{userId}/{targetUserId}")]
        public async Task<IActionResult> GetFriendshipStatus(int userId, int targetUserId)
        {
            string status = await _friendshipService.GetFriendshipStatusAsync(userId, targetUserId);
            return Ok(new { status = status ?? "none" });
        }
    }
}
        