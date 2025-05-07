using System;

namespace backend.DTOs
{
    /// <summary>
    /// 用户状态DTO
    /// </summary>
    public class UserStatusDTO
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// 是否在线
        /// </summary>
        public bool IsOnline { get; set; }
        
        /// <summary>
        /// 是否可见（是否显示在线状态）
        /// </summary>
        public bool IsVisible { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
} 