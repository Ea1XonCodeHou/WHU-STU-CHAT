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
            try
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

                // 发送带验证消息的好友请求通知，添加RelatedId为请求者ID
                var content = $"{dto.RequesterUsername} 请求加你为好友\n验证消息: {dto.Message}";
                await _notificationService.CreateNotificationAsync(
                    targetUser.Id, 
                    content, 
                    "friend_request", 
                    "好友请求", 
                    requester.Id  // 添加请求者ID作为RelatedId
                );

                return Ok(new { msg = "好友请求已发送" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { msg = $"发送好友请求失败: {ex.Message}" });
            }
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
            try 
            {
                Console.WriteLine($"Processing friend acceptance for notification: {dto.NotificationId}");
                
                var notification = await _notificationService.GetNotificationByIdAsync(dto.NotificationId);
                if (notification == null) 
                {
                    Console.WriteLine("通知不存在");
                    return NotFound(new { msg = "通知不存在" });
                }
                
                // 检查通知类型是否正确
                if (notification.Type != "friend_request") 
                {
                    Console.WriteLine($"通知类型错误: {notification.Type}");
                    return BadRequest(new { msg = "此通知不是好友请求" });
                }

                // 尝试提取用户名 - 改进提取逻辑
                // 假设格式为"xxx 请求加你为好友"或包含请求者用户名
                string requesterUsername = string.Empty;
                
                // 通知内容可能格式为 "XXX 请求加你为好友"
                if (notification.Content.Contains("请求加你为好友"))
                {
                    int endIndex = notification.Content.IndexOf(" 请求加你为好友");
                    if (endIndex > 0)
                    {
                        requesterUsername = notification.Content.Substring(0, endIndex);
                        Console.WriteLine($"从通知内容提取的用户名: {requesterUsername}");
                    }
                }
                
                // 如果上面的方式无法提取到用户名，可以尝试从RelatedId获取
                var requester = await _userService.GetUserByUsernameAsync(requesterUsername);
                
                // 如果通过用户名找不到，且notification.RelatedId有值，尝试直接通过ID查找
                if (requester == null && notification.RelatedId.HasValue && notification.RelatedId.Value > 0)
                {
                    requester = await _userService.GetUserByIdAsync(notification.RelatedId.Value);
                    Console.WriteLine($"通过RelatedId={notification.RelatedId}找到请求者");
                }
                
                if (requester == null) 
                {
                    Console.WriteLine($"未找到请求者, 用户名: {requesterUsername}, RelatedId: {notification.RelatedId}");
                    return BadRequest(new { msg = $"未找到请求者用户" });
                }
                
                var receiver = await _userService.GetUserByIdAsync(notification.UserId);
                if (receiver == null) 
                {
                    Console.WriteLine($"未找到接收者: {notification.UserId}");
                    return BadRequest(new { msg = "未找到接收用户" });
                }

                Console.WriteLine($"请求者ID: {requester.Id}, 接收者ID: {receiver.Id}");
                
                // 先检查是否已经是好友
                bool alreadyFriends = await _friendshipService.CheckIsFriendAsync(receiver.Id, requester.Id);
                if (alreadyFriends)
                {
                    Console.WriteLine("已经是好友关系");
                    
                    // 标记通知为已处理
                    await _notificationService.MarkAsHandled(dto.NotificationId);
                    
                    return Ok(new { msg = "已经是好友关系" });
                }
                
                // 接受好友请求（更新Friendships表）
                bool acceptResult = await _friendshipService.AcceptFriendRequestAsync(receiver.Id, requester.Id);
                
                if (!acceptResult) 
                {
                    // 尝试先创建好友请求再接受
                    await _friendshipService.CreateFriendRequestAsync(requester.Id, receiver.Id);
                    acceptResult = await _friendshipService.AcceptFriendRequestAsync(receiver.Id, requester.Id);
                    
                    if (!acceptResult)
                    {
                        Console.WriteLine("接受好友请求失败");
                        return StatusCode(500, new { msg = "接受好友请求失败" });
                    }
                }
                
                Console.WriteLine("好友关系已创建，现在创建私聊群组");
                
                // 创建群聊（只有两人）用于私聊
                try 
                {
                    var result = await _groupService.AddFriendAsync(requester.Id, receiver.Id);
                    if (!result) 
                    {
                        Console.WriteLine("创建私聊群组失败");
                        // 即使没有创建群聊成功，仍然保留好友关系
                    }
                }
                catch (Exception ex) 
                {
                    Console.WriteLine($"创建私聊群组异常: {ex.Message}");
                    // 继续处理，不中断流程
                }

                // 标记通知为已处理
                await _notificationService.MarkAsHandled(dto.NotificationId);
                Console.WriteLine("通知已标记为已处理");

                return Ok(new { msg = "已同意好友请求，已创建私聊" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理好友请求异常: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return StatusCode(500, new { msg = $"处理好友请求失败: {ex.Message}" });
            }
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
        