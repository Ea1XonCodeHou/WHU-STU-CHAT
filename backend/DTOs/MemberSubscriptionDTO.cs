using System;

namespace backend.DTOs
{
    public class MemberSubscriptionDTO
    {
        public int SubscriptionId { get; set; }
        public int UserId { get; set; }
        public int Level { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PaymentAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime CreateTime { get; set; }

        // 计算剩余天数
        public int RemainingDays => (EndDate - DateTime.Now).Days > 0 ? (EndDate - DateTime.Now).Days : 0;
        
        // 会员等级名称
        public string LevelName => Level == 1 ? "VIP会员" : Level == 2 ? "SVIP会员" : "普通用户";
        
        // 会员状态是否有效
        public bool IsActive => Status == "active" && DateTime.Now <= EndDate;
    }
    
    public class CreateSubscriptionRequest
    {
        public int UserId { get; set; }
        public int Level { get; set; }
        public string PaymentMethod { get; set; }
    }
} 