using System;
using System.Collections.Generic;

namespace backend.DTOs
{
    /// <summary>
    /// 聊天室数据传输对象
    /// </summary>
    public class RoomDTO
    {
        /// <summary>
        /// 聊天室ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 聊天室名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 聊天室描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 聊天室成员
        /// </summary>
        public List<UserDTO> Members { get; set; }

        /// <summary>
        /// 最新消息
        /// </summary>
        public MessageDTO LastMessage { get; set; }
    }
} 