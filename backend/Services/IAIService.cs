using System.Threading.Tasks;
using System.Collections.Generic;
using backend.DTOs;

namespace backend.Services
{
    /// <summary>
    /// AI服务接口
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// 发送消息给AI并获取回复
        /// </summary>
        /// <param name="request">AI聊天请求</param>
        /// <returns>AI回复内容</returns>
        Task<AIChatResponseDTO> SendMessageAsync(AIChatRequestDTO request);
        
        /// <summary>
        /// 获取AI聊天历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>历史消息列表</returns>
        Task<List<AIMessageDTO>> GetChatHistoryAsync(int userId);
        
        /// <summary>
        /// 清空AI聊天历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>操作结果</returns>
        Task<bool> ClearChatHistoryAsync(int userId);
        
        /// <summary>
        /// 总结聊天记录
        /// </summary>
        /// <param name="request">聊天总结请求</param>
        /// <returns>AI生成的总结</returns>
        Task<AIChatResponseDTO> SummarizeChatAsync(ChatSummaryRequestDTO request);
    }
} 