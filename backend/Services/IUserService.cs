using System.Threading.Tasks;
using backend.DTOs;
using backend.Models;
using backend.Utils;

namespace backend.Services
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="loginDto">登录信息</param>
        /// <returns>登录结果</returns>
        Task<Result<LoginResultVO>> LoginAsync(LoginDTO loginDto);

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="registerDto">注册信息</param>
        /// <returns>注册结果</returns>
        Task<Result<UserVO>> RegisterAsync(RegisterDTO registerDto);

        /// <summary>
        /// 验证用户名是否存在
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>验证结果</returns>
        Task<Result<bool>> CheckUsernameExistsAsync(string username);

        /// <summary>
        /// 根据ID获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户信息</returns>
        Task<Result<UserVO>> GetUserInfoAsync(int userId);
    }
} 