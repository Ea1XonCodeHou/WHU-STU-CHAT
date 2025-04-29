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

        public NotificationController(
            IUserService userService,
            INotificationService notificationService,
            IGroupService groupService)
        {
            _userService = userService;
            _notificationService = notificationService;
            _groupService = groupService;
        }

        // 1. 发送好友请求
        [HttpPost("friend-request")]
        public async Task<IActionResult> SendFriendRequest([FromBody] FriendRequestDTO dto)
        {
            var targetUser = await _userService.GetUserByUsernameAsync(dto.TargetUsername);
            if (targetUser == null)
                return NotFound(new { msg = "用户不存在" });

            var content = $"{dto.RequesterUsername} 请求加你为好友";
            await _notificationService.CreateNotificationAsync(targetUser.Id, content);

            return Ok(new { msg = "好友请求已发送" });
        }

        // 2. 获取用户所有通知
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId);
            return Ok(notifications);
        }

        // 3. 同意好友请求（创建私聊群）
        [HttpPost("accept-friend")]
        public async Task<IActionResult> AcceptFriend([FromBody] AcceptFriendDTO dto)
        {
            var notification = await _notificationService.GetNotificationByIdAsync(dto.NotificationId);
            if (notification == null) return NotFound();

            // 假设格式为"xxx 请求加你为好友"
            var requesterUsername = notification.Content.Split(' ')[0];
            var requester = await _userService.GetUserByUsernameAsync(requesterUsername);
            var receiver = await _userService.GetUserByIdAsync(notification.UserId);

            if (requester == null || receiver == null)
                return BadRequest(new { msg = "用户信息有误" });

            // 创建群聊（只有两人）
            var groupRegDto = new GroupRegDTO
            {
                GroupName = $"{requester.Username}和{receiver.Username}的私聊",
                CreatorId = requester.Id,
                MemberCount = 1, // 只包含创建者
                Description = "私聊"
            };
            var groupId = await _groupService.CreateGroupAsync(groupRegDto);
            await _groupService.AddUserToGroupAsync(groupId, receiver.Id);

            // 标记通知为已处理（你可以实现为删除或加状态字段）
            await _notificationService.MarkAsHandled(dto.NotificationId);

            return Ok(new { msg = "已同意好友请求，已创建私聊" });
        }
    }


    
}
