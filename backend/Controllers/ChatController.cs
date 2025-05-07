using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using backend.Services;
using backend.DTOs;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }
        
        [HttpGet("history/private/{friendId}")]
        public async Task<IActionResult> GetPrivateChatHistory(int friendId)
        {
            try
            {
                // 从token或Session中获取当前用户ID
                int userId = int.Parse(User.Identity?.Name ?? HttpContext.Session.GetString("UserId") ?? "0");
                if (userId <= 0)
                {
                    // 如果无法从认证上下文获取，则尝试从请求头获取
                    if (Request.Headers.TryGetValue("UserId", out var userIdHeader))
                    {
                        userId = int.Parse(userIdHeader);
                    }
                    else
                    {
                        return Unauthorized(new { message = "无法识别当前用户" });
                    }
                }
                
                if (friendId <= 0)
                {
                    return BadRequest(new { message = "好友ID无效" });
                }
                
                // 获取私聊历史消息
                var messages = await _chatService.GetPrivateChatHistoryAsync(userId, friendId, 50);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"获取私聊历史消息失败: {ex.Message}" });
            }
        }
        
        [HttpPost("private/message")]
        public async Task<IActionResult> SendPrivateMessage([FromBody] MessageDTO message)
        {
            try
            {
                if (message == null)
                {
                    return BadRequest(new { message = "消息内容不能为空" });
                }
                
                // 从token或Session中获取当前用户ID
                int userId = int.Parse(User.Identity?.Name ?? HttpContext.Session.GetString("UserId") ?? "0");
                if (userId <= 0)
                {
                    // 如果无法从认证上下文获取，则尝试从请求头获取
                    if (Request.Headers.TryGetValue("UserId", out var userIdHeader))
                    {
                        userId = int.Parse(userIdHeader);
                    }
                    else
                    {
                        return Unauthorized(new { message = "无法识别当前用户" });
                    }
                }
                
                // 设置发送者ID
                message.SenderId = userId;
                
                // 保存消息
                int messageId = await _chatService.SavePrivateMessageAsync(message);
                if (messageId <= 0)
                {
                    return StatusCode(500, new { message = "保存消息失败" });
                }
                
                // 返回带有ID的消息对象
                message.MessageId = messageId;
                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"发送私聊消息失败: {ex.Message}" });
            }
        }
    }
} 