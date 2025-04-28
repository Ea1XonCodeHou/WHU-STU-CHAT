using System;
using System.Collections.Generic;

namespace backend.DTOs
{
    /// <summary>
    /// AI聊天请求DTO
    /// </summary>
    public class AIChatRequestDTO
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 用户消息内容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 对话历史（可选）
        /// </summary>
        public List<AIMessageDTO> History { get; set; } = new List<AIMessageDTO>();
    }

    /// <summary>
    /// AI聊天响应DTO
    /// </summary>
    public class AIChatResponseDTO
    {
        /// <summary>
        /// AI响应消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 响应状态
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息（如果有）
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 响应时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// AI消息DTO（用于历史记录）
    /// </summary>
    public class AIMessageDTO
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 消息角色（用户/AI）
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 聊天记录总结请求DTO
    /// </summary>
    public class ChatSummaryRequestDTO
    {
        /// <summary>
        /// 聊天室ID
        /// </summary>
        public int RoomId { get; set; }
        
        /// <summary>
        /// 请求用户ID
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// 请求用户名
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// 要总结的消息数量，0表示全部
        /// </summary>
        public int MessageCount { get; set; } = 50;
    }
} 