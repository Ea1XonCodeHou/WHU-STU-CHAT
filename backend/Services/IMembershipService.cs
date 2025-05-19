using System;
using System.Threading.Tasks;
using backend.DTOs;

namespace backend.Services
{
    public interface IMembershipService
    {
        /// <summary>
        /// 创建会员订阅
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="level">会员等级 1-VIP，2-SVIP</param>
        /// <param name="paymentMethod">支付方式</param>
        /// <returns>订阅信息</returns>
        Task<MemberSubscriptionDTO> CreateSubscriptionAsync(int userId, int level, string paymentMethod);
        
        /// <summary>
        /// 获取用户当前订阅信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>订阅信息</returns>
        Task<MemberSubscriptionDTO> GetCurrentSubscriptionAsync(int userId);
        
        /// <summary>
        /// 取消会员订阅
        /// </summary>
        /// <param name="subscriptionId">订阅ID</param>
        /// <returns>是否成功</returns>
        Task<bool> CancelSubscriptionAsync(int subscriptionId);
        
        /// <summary>
        /// 检查会员是否过期并处理
        /// </summary>
        /// <returns>处理的记录数</returns>
        Task<int> ProcessExpiredSubscriptionsAsync();
    }
} 