using System;

namespace backend.DTOs
{
    /// <summary>
    /// 用户信息视图对象
    /// </summary>
    public class UserVO
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 个性签名
        /// </summary>
        public string? Signature { get; set; }

        /// <summary>
        /// 在线状态
        /// </summary>
        public string? Status { get; set; }
    }

    /// <summary>
    /// 登录成功后返回的视图对象
    /// </summary>
    public class LoginResultVO
    {
        /// <summary>
        /// 用户基本信息
        /// </summary>
        public UserVO? UserInfo { get; set; }

        /// <summary>
        /// 身份验证令牌
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// 令牌过期时间
        /// </summary>
        public DateTime ExpireTime { get; set; }
    }
} 