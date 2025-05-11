using System.Threading.Tasks;
using backend.DTOs;
using backend.Services;
using backend.Utils;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// 用户控制器
    /// </summary>
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="userService">用户服务</param>
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="loginDto">登录信息</param>
        /// <returns>登录结果</returns>
        [HttpPost("login")]
        public async Task<Result<LoginResultVO>> Login([FromBody] LoginDTO loginDto)
        {
            return await _userService.LoginAsync(loginDto);
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="registerDto">注册信息</param>
        /// <returns>注册结果</returns>
        [HttpPost("register")]
        public async Task<Result<UserVO>> Register([FromBody] RegisterDTO registerDto)
        {
            return await _userService.RegisterAsync(registerDto);
        }

        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>检查结果</returns>
        [HttpGet("check-username")]
        public async Task<Result<bool>> CheckUsername([FromQuery] string username)
        {
            return await _userService.CheckUsernameExistsAsync(username);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户信息</returns>
        [HttpGet("{userId}")]
        public async Task<Result<UserVO>> GetUserInfo([FromRoute] int userId)
        {
            return await _userService.GetUserInfoAsync(userId);
        }

        /// <summary>
        /// 获取用户在线状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户在线状态</returns>
        [HttpGet("{userId}/status")]
        public Result<bool> GetUserStatus([FromRoute] int userId, [FromServices] IChatService chatService)
        {
            bool isOnline = chatService.IsUserOnline(userId);
            return Result<bool>.Success(isOnline);
        }
        
        /// <summary>
        /// 获取所有在线用户
        /// </summary>
        /// <returns>在线用户ID列表</returns>
        [HttpGet("online")]
        public Result<List<int>> GetOnlineUsers([FromServices] IChatService chatService)
        {
            var onlineUsers = chatService.GetOnlineUsers();
            return Result<List<int>>.Success(onlineUsers);
        }

        /// <summary>
        /// 检查用户是否为好友
        /// </summary>
        /// <param name="userId">当前用户ID</param>
        /// <param name="targetUserId">目标用户ID</param>
        /// <returns>是否为好友</returns>
        [HttpGet("{userId}/is-friend/{targetUserId}")]
        public async Task<Result<bool>> CheckIsFriend([FromRoute] int userId, [FromRoute] int targetUserId, [FromServices] IGroupService groupService)
        {
            var privateGroups = await groupService.GetPrivateGroupBetweenUsersAsync(userId, targetUserId);
            bool isFriend = privateGroups != null && privateGroups.Count > 0;
            
            return Result<bool>.Success(isFriend);
        }

        /// <summary>
        /// 获取用户的好友列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>好友列表</returns>
        [HttpGet("{userId}/friends")]
        public async Task<Result<List<UserDTO>>> GetUserFriends([FromRoute] int userId, [FromServices] IFriendshipService friendshipService)
        {
            try
            {
                var friends = await friendshipService.GetUserFriendsAsync(userId);
                return Result<List<UserDTO>>.Success(friends);
            }
            catch (Exception ex)
            {
                return Result<List<UserDTO>>.Error($"获取好友列表失败: {ex.Message}");
            }
        }
    }
} 