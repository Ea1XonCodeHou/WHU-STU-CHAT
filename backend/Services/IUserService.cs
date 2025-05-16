using System.Threading.Tasks;
using backend.DTOs;
using backend.Models;
using backend.Utils;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

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
        Task<UserDTO> GetUserByUsernameAsync(string username);
        Task<UserDTO> GetUserByIdAsync(int userId);
        
        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="updateDto">更新信息</param>
        /// <returns>更新结果</returns>
        Task<Result<UserVO>> UpdateUserInfoAsync(UpdateUserDTO updateDto);

        /// <summary>
        /// 更新用户在线状态
        /// </summary>
        /// <param name="statusDto">状态DTO</param>
        /// <returns>更新结果</returns>
        Task<Result<bool>> UpdateUserStatusAsync(UserStatusDTO statusDto);

        /// <summary>
        /// 保存用户设置
        /// </summary>
        /// <param name="settingsDto">设置信息</param>
        /// <returns>保存结果</returns>
        Task<Result<bool>> SaveUserSettingAsync(UserSettingsDTO settingsDto);

        /// <summary>
        /// 获取用户所有设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>所有设置</returns>
        Task<Result<Dictionary<string, string>>> GetAllUserSettingsAsync(int userId);

        /// <summary>
        /// 更新用户头像
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="file">头像文件</param>
        /// <returns>头像URL</returns>
        Task<Result<string>> UpdateUserAvatarAsync(int userId, IFormFile file);
    }
} 