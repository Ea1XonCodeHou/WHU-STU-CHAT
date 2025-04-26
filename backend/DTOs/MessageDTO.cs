using System;

namespace backend.DTOs
{
    /// <summary>
    /// 消息数据传输对象
    /// </summary>
    public class MessageDTO
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime SendTime { get; set; }

        /// <summary>
        /// 发送者ID
        /// </summary>
        public int SenderId { get; set; }

        /// <summary>
        /// 发送者用户名
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// 聊天室ID
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// 是否已读
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// 消息类型（text、system、image等）
        /// </summary>
        public string MessageType { get; set; } = "text";
    }

    public class GroupMessageDTO
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 发送者ID
        /// </summary>
        public int SenderId { get; set; }

        /// <summary>
        /// 发送者用户名
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// 聊天室ID
        /// </summary>
        public int GroupId { get; set; }
        
    }
} 