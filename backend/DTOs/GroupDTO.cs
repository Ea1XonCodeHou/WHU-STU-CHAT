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

    public class GroupDetailDTO : GroupDTO
    {
        public List<UserDTO> Members { get; set; }
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
