using System;

namespace backend.DTOs
{
    /// <summary>
    /// 通知数据传输对象
    /// </summary>
    public class NotificationDTO
    {
        /// <summary>
        /// 通知唯一标识
        /// </summary>
        public int NotificationId { get; set; }

        /// <summary>
        /// 接收通知的用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 通知内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 通知创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// （可选）通知是否已处理
        /// </summary>
        public bool? IsHandled { get; set; }
    }

    public class FriendRequestDTO
    {
        public string TargetUsername { get; set; }
        public string RequesterUsername { get; set; }
    }

    public class AcceptFriendDTO
    {
        public int NotificationId { get; set; }
    }
}