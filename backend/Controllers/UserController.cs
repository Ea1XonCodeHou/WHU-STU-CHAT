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
    }
} 