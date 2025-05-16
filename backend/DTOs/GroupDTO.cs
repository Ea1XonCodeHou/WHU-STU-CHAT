using System;
using System.Data;
namespace backend.DTOs
{
    public class GroupDTO
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
        public int CreatorId { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public class GroupMemberDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public DateTime LastActive { get; set; }
        public DateTime JoinTime { get; set; } 
        public string Role { get; set; } // 角色（管理员、成员等）
    }

    public class GroupDetailDTO : GroupDTO
    {
        public List<GroupMemberDTO> Members { get; set; }
    }

    public class GroupRegDTO
    {
        public string GroupName { get; set; }
        public string Description { get; set; }
        public int CreatorId { get; set; }
        public int MemberCount { get; set; }
    }

    public class GroupMessageDTO
    {
        public int MessageId { get; set; }
        public int GroupId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
