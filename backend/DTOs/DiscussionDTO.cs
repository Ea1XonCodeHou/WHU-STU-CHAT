using System;

namespace backend.DTOs
{
    /// <summary>
    /// 讨论区DTO
    /// </summary>
    public class DiscussionDTO
    {
        /// <summary>
        /// 讨论区ID
        /// </summary>
        public int DiscussionId { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 创建者ID
        /// </summary>
        public int CreatorId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 帖子数量
        /// </summary>
        public int PostCount { get; set; }
    }
} 