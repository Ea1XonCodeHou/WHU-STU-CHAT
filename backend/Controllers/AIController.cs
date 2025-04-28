using System;
using System.Threading.Tasks;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly ILogger<AIController> _logger;
        private readonly IAIService _aiService;

        public AIController(ILogger<AIController> logger, IAIService aiService)
        {
            _logger = logger;
            _aiService = aiService;
        }

        /// <summary>
        /// 发送消息给AI
        /// </summary>
        /// <param name="request">聊天请求</param>
        /// <returns>AI回复</returns>
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AIChatRequestDTO request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest("消息内容不能为空");
                }

                _logger.LogInformation($"接收到AI聊天请求: {request.Message}");
                var response = await _aiService.SendMessageAsync(request);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning($"AI响应失败: {response.Error}");
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI控制器异常");
                return StatusCode(500, new AIChatResponseDTO
                {
                    Success = false,
                    Error = $"服务器错误: {ex.Message}",
                    Timestamp = DateTime.Now
                });
            }
        }
        
        /// <summary>
        /// 总结聊天记录
        /// </summary>
        /// <param name="request">聊天总结请求</param>
        /// <returns>AI生成的总结</returns>
        [HttpPost("summarize")]
        public async Task<IActionResult> SummarizeChat([FromBody] ChatSummaryRequestDTO request)
        {
            try
            {
                if (request.RoomId <= 0)
                {
                    return BadRequest("无效的聊天室ID");
                }

                _logger.LogInformation($"接收到聊天总结请求: 聊天室ID {request.RoomId}，用户 {request.Username}({request.UserId})");
                var response = await _aiService.SummarizeChatAsync(request);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning($"生成聊天总结失败: {response.Error}");
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "总结聊天记录异常");
                return StatusCode(500, new AIChatResponseDTO
                {
                    Success = false,
                    Error = $"服务器错误: {ex.Message}",
                    Timestamp = DateTime.Now
                });
            }
        }
    }
} 