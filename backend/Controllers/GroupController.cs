using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateGroup([FromBody] GroupRegDTO groupRegDto)
        {
            try
            {
                var groupId = await _groupService.CreateGroupAsync(groupRegDto);
                return Ok(new { code = 200, data = groupId, msg = "群组创建成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpDelete("{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            try
            {
                var result = await _groupService.DeleteGroupAsync(groupId);
                if (result)
                    return Ok(new { code = 200, msg = "群组删除成功" });
                return NotFound(new { code = 404, msg = "群组不存在" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserGroups(int userId)
        {
            try
            {
                var groups = await _groupService.GetAllGroupsAsync(userId);
                return Ok(new { code = 200, data = groups, msg = "获取群组列表成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroupDetails(int groupId)
        {
            try
            {
                var group = await _groupService.GetGroupDetailsAsync(groupId);
                if (group != null)
                    return Ok(new { code = 200, data = group, msg = "获取群组详情成功" });
                return NotFound(new { code = 404, msg = "群组不存在" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpPost("{groupId}/add-user/{userId}")]
        public async Task<IActionResult> AddUserToGroup(int groupId, int userId)
        {
            try
            {
                var result = await _groupService.AddUserToGroupAsync(groupId, userId);
                if (result)
                    return Ok(new { code = 200, msg = "添加用户到群组成功" });
                return BadRequest(new { code = 400, msg = "添加用户到群组失败" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpPost("{groupId}/add-user-by-username")]
        public async Task<IActionResult> AddUserToGroupByUserName(int groupId, [FromBody] AddUserByUsernameDTO request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserName))
                {
                    return BadRequest(new { code = 400, msg = "用户名不能为空" });
                }

                var result = await _groupService.AddUserToGroupByUserNameAsync(groupId, request.UserName);
                if (result)
                {
                    return Ok(new { code = 200, msg = "用户已成功添加到群组" });
                }
                return BadRequest(new { code = 400, msg = "添加用户到群组失败" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpDelete("{groupId}/remove-user/{userId}")]
        public async Task<IActionResult> RemoveUserFromGroup(int groupId, int userId)
        {
            try
            {
                var result = await _groupService.RemoveUserFromGroupAsync(groupId, userId);
                if (result)
                    return Ok(new { code = 200, msg = "从群组移除用户成功" });
                return BadRequest(new { code = 400, msg = "从群组移除用户失败" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpGet("{groupId}/users")]
        public async Task<IActionResult> GetGroupUsers(int groupId)
        {
            try
            {
                var users = await _groupService.GetGroupUsersAsync(groupId);
                return Ok(new { code = 200, data = users, msg = "获取群组用户列表成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpGet("{groupId}/messages")]
        public async Task<IActionResult> GetGroupMessages(int groupId, [FromQuery] int count = 20)
        {
            try
            {
                var messages = await _groupService.GetGroupMessagesAsync(groupId, count);
                return Ok(new { code = 200, data = messages, msg = "获取群组消息成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpGet("user/{userId}/private")]
        public async Task<IActionResult> GetUserPrivateChats(int userId)
        {
            try
            {
                // 调用 GetFriendsAsync 获取好友列表
                var friends = await _groupService.GetFriendsAsync(userId);

                // 构造私聊信息
                var privateChats = friends.Select(friend => new
                {
                    friendId = friend.FriendId,
                    username = friend.Username,
                    groupId = friend.GroupId,
                    friendshipCreatedTime = friend.FriendshipCreatedTime
                }).ToList();

                return Ok(new { code = 200, data = privateChats, msg = "获取私聊列表成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchGroups([FromQuery] string groupName, [FromQuery] int userId)
        {
            try
            {
                var groups = await _groupService.SearchGroupsByNameAsync(groupName, userId);
                return Ok(new { code = 200, data = groups, msg = "搜索群组成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, msg = ex.Message });
            }
        }
    }
} 