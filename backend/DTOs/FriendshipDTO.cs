using System;

namespace backend.DTOs
{
    public class FriendshipDTO
    {
        public int FriendshipId { get; set; }
        public int UserId { get; set; }
        public int FriendId { get; set; }
        public string FriendName { get; set; }
        public string FriendAvatar { get; set; }
        public bool IsOnline { get; set; }
        public DateTime FriendshipCreatedTime { get; set; }
        public int GroupId { get; set; }
        public string Status { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public string Username { get; set; }  // 用于显示好友的用户名
    }
} 