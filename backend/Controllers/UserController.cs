using System.Threading.Tasks;
using backend.DTOs;
using backend.Services;
using backend.Utils;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// �û�������
    /// </summary>
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="userService">�û�����</param>
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// �û���¼
        /// </summary>
        /// <param name="loginDto">��¼��Ϣ</param>
        /// <returns>��¼���</returns>
        [HttpPost("login")]
        public async Task<Result<LoginResultVO>> Login([FromBody] LoginDTO loginDto)
        {
            return await _userService.LoginAsync(loginDto);
        }

        /// <summary>
        /// �û�ע��
        /// </summary>
        /// <param name="registerDto">ע����Ϣ</param>
        /// <returns>ע����</returns>
        [HttpPost("register")]
        public async Task<Result<UserVO>> Register([FromBody] RegisterDTO registerDto)
        {
            return await _userService.RegisterAsync(registerDto);
        }

        /// <summary>
        /// ����û����Ƿ����
        /// </summary>
        /// <param name="username">�û���</param>
        /// <returns>�����</returns>
        [HttpGet("check-username")]
        public async Task<Result<bool>> CheckUsername([FromQuery] string username)
        {
            return await _userService.CheckUsernameExistsAsync(username);
        }

        /// <summary>
        /// ��ȡ�û���Ϣ
        /// </summary>
        /// <param name="userId">�û�ID</param>
        /// <returns>�û���Ϣ</returns>
        [HttpGet("{userId}")]
        public async Task<Result<UserVO>> GetUserInfo([FromRoute] int userId)
        {
            return await _userService.GetUserInfoAsync(userId);
        }
    }
} 