using System;

namespace backend.DTOs
{
    /// <summary>
    /// 用户设置DTO
    /// </summary>
    public class UserSettingsDTO
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 设置键
        /// </summary>
        public string? SettingKey { get; set; }

        /// <summary>
        /// 设置值
        /// </summary>
        public string? SettingValue { get; set; }
    }
} 