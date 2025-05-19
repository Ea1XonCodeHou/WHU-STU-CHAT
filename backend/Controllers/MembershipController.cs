using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using backend.DTOs;
using backend.Services;
// using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]
    public class MembershipController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        
        public MembershipController(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }
        
        /// <summary>
        /// 获取当前用户的会员信息
        /// </summary>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            try
            {
                // 从查询参数获取用户ID
                if (!int.TryParse(HttpContext.Request.Query["userId"], out int userId))
                {
                    return BadRequest(new { message = "请提供有效的用户ID" });
                }
                
                var subscription = await _membershipService.GetCurrentSubscriptionAsync(userId);
                return Ok(subscription);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        /// <summary>
        /// 创建会员订阅
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            try
            {
                // 直接使用请求中的用户ID
                int userId = request.UserId;
                
                var subscription = await _membershipService.CreateSubscriptionAsync(
                    request.UserId,
                    request.Level,
                    request.PaymentMethod);
                    
                return Ok(subscription);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        /// <summary>
        /// 模拟支付过程
        /// </summary>
        [HttpPost("mock-payment")]
        public async Task<IActionResult> MockPayment([FromBody] CreateSubscriptionRequest request)
        {
            try
            {
                // 模拟支付处理延迟
                await Task.Delay(5000); // 5秒延迟
                
                // 直接使用请求中的用户ID
                int userId = request.UserId;
                
                // 默认支付成功
                var subscription = await _membershipService.CreateSubscriptionAsync(
                    request.UserId,
                    request.Level,
                    request.PaymentMethod);
                    
                return Ok(new { 
                    success = true,
                    message = "支付成功", 
                    subscription = subscription 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    success = false,
                    message = ex.Message 
                });
            }
        }
        
        /// <summary>
        /// 取消会员订阅
        /// </summary>
        [HttpPost("cancel/{subscriptionId}")]
        public async Task<IActionResult> CancelSubscription(int subscriptionId)
        {
            try
            {
                bool cancelled = await _membershipService.CancelSubscriptionAsync(subscriptionId);
                
                if (cancelled)
                {
                    return Ok(new { message = "订阅已取消" });
                }
                else
                {
                    return NotFound(new { message = "找不到有效的订阅或无法取消" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 