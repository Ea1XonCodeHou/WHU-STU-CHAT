using System;

namespace backend.DTOs
{
    /// <summary>
    /// 更新用户信息DTO
    /// </summary>
    public class UpdateUserDTO
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

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
        /// 个性签名
        /// </summary>
        public string? Signature { get; set; }
        
        /// <summary>
        /// 密码（可选）
        /// </summary>
        public string? Password { get; set; }
    }
} 