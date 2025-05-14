namespace backend.Models
{
    /// <summary>
    /// 通知模型
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// 通知ID
        /// </summary>
        public int NotificationId { get; set; }

        /// <summary>
        /// 用户ID（接收者）
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 通知标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 通知内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 通知类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 是否已读
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool IsHandled { get; set; }

        /// <summary>
        /// 相关ID（可用于存储如好友请求发起者ID等）
        /// </summary>
        public int? RelatedId { get; set; }

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