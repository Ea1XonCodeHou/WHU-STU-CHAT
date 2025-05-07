namespace backend.Models
{
    /// <summary>
    /// 讨论区模型
    /// </summary>
    public class Discussion
    {
        /// <summary>
        /// 讨论区ID
        /// </summary>
        public int DiscussionId { get; set; }

        /// <summary>
        /// 讨论区名称
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 讨论区描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建者ID
        /// </summary>
        public int CreatorId { get; set; }

        /// <summary>
        /// 是否是热门讨论区
        /// </summary>
        public bool IsHot { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
} 