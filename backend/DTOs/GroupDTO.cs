using System;
using System.Data;
namespace backend.DTOs
{
    public class GroupDTO
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public DateTime UpdateTime { get; set; }
        public int MemberCount { get; set; }
        public int CreatorId { get; set; }
        public string Description { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class GroupRegDTO
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
        public int CreatorId { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreateTime { get; set; }

    }
    public class GroupDetailDTO
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
        public int CreatorId { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

    }
}
