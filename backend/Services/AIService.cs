using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using backend.DTOs;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Linq;

namespace backend.Services
{
    /// <summary>
    /// AI服务实现
    /// </summary>
    public class AIService : IAIService
    {
        private readonly ILogger<AIService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "sk-525a19b6464b42ca8e4599933e2cf149"; // DeepSeek API Key
        private readonly string _apiEndpoint = "https://api.deepseek.com/v1/chat/completions";
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AIService(ILogger<AIService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            // 配置HTTP客户端
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// 发送消息给AI并获取回复
        /// </summary>
        public async Task<AIChatResponseDTO> SendMessageAsync(AIChatRequestDTO request)
        {
            try
            {
                _logger.LogInformation($"用户 {request.Username}({request.UserId}) 发送AI消息: {request.Message}");

                // 构建消息列表
                var messages = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>
                    {
                        { "role", "system" },
                        { "content", GetSystemPrompt(request.Username) }
                    }
                };

                // 添加历史消息
                foreach (var historyMsg in request.History)
                {
                    messages.Add(new Dictionary<string, string>
                    {
                        { "role", historyMsg.Role },
                        { "content", historyMsg.Content }
                    });
                }

                // 添加当前用户消息
                messages.Add(new Dictionary<string, string>
                {
                    { "role", "user" },
                    { "content", request.Message }
                });

                // 构建请求体
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages,
                    temperature = 0.7,
                    max_tokens = 800
                };

                // 序列化请求体
                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                // 发送请求
                var response = await _httpClient.PostAsync(_apiEndpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("AI响应成功");
                    
                    // 解析响应
                    using var jsonDoc = JsonDocument.Parse(responseBody);
                    var root = jsonDoc.RootElement;
                    
                    if (root.TryGetProperty("choices", out var choices) && 
                        choices.GetArrayLength() > 0 && 
                        choices[0].TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        var aiMessage = messageContent.GetString();
                        _logger.LogInformation($"AI回复: {aiMessage}");

                        // 保存到历史记录
                        await SaveChatHistoryAsync(request.UserId, request.Message, aiMessage);

                        return new AIChatResponseDTO
                        {
                            Message = aiMessage,
                            Success = true,
                            Timestamp = DateTime.Now
                        };
                    }
                }

                // 如果解析失败或请求不成功
                _logger.LogError($"AI响应处理失败: {responseBody}");
                return new AIChatResponseDTO
                {
                    Success = false,
                    Error = $"处理AI响应失败: {response.StatusCode}",
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI服务异常");
                return new AIChatResponseDTO
                {
                    Success = false,
                    Error = $"AI服务异常: {ex.Message}",
                    Timestamp = DateTime.Now
                };
            }
        }
        
        /// <summary>
        /// 获取AI聊天历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>历史消息列表</returns>
        public async Task<List<AIMessageDTO>> GetChatHistoryAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"获取用户 {userId} 的AI聊天历史记录");
                var result = new List<AIMessageDTO>();
                
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"SELECT ChatId, UserMessage, AIResponse, CreateTime
                                 FROM AIChatHistory
                                 WHERE UserId = @UserId
                                 ORDER BY CreateTime ASC";
                                 
                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var userMessage = reader.GetString(reader.GetOrdinal("UserMessage"));
                                var aiResponse = reader.GetString(reader.GetOrdinal("AIResponse"));
                                var createTime = reader.GetDateTime(reader.GetOrdinal("CreateTime"));
                                var chatId = reader.GetInt32(reader.GetOrdinal("ChatId"));
                                
                                // 添加用户消息
                                result.Add(new AIMessageDTO
                                {
                                    Role = "user",
                                    Content = userMessage,
                                    Id = $"u_{chatId}",
                                    Timestamp = createTime
                                });
                                
                                // 添加AI回复
                                result.Add(new AIMessageDTO
                                {
                                    Role = "assistant",
                                    Content = aiResponse,
                                    Id = $"a_{chatId}",
                                    Timestamp = createTime
                                });
                            }
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取用户 {userId} 的AI聊天历史记录失败");
                throw;
            }
        }
        
        /// <summary>
        /// 清空AI聊天历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>操作结果</returns>
        public async Task<bool> ClearChatHistoryAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"清空用户 {userId} 的AI聊天历史记录");
                
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = "DELETE FROM AIChatHistory WHERE UserId = @UserId";
                    
                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation($"已删除 {rowsAffected} 条AI聊天历史记录");
                        
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"清空用户 {userId} 的AI聊天历史记录失败");
                return false;
            }
        }
        
        /// <summary>
        /// 保存AI聊天历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="userMessage">用户消息</param>
        /// <param name="aiResponse">AI回复</param>
        /// <returns>操作结果</returns>
        private async Task<bool> SaveChatHistoryAsync(int userId, string userMessage, string aiResponse)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"INSERT INTO AIChatHistory (UserId, SessionId, UserMessage, AIResponse, CreateTime)
                                VALUES (@UserId, @SessionId, @UserMessage, @AIResponse, @CreateTime)";
                    
                    using (var command = new MySqlCommand(sql, connection))
                    {
                        string sessionId = $"session_{userId}_{DateTime.Now:yyyyMMdd}";
                        DateTime currentTime = DateTime.Now;
                        
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@SessionId", sessionId);
                        command.Parameters.AddWithValue("@UserMessage", userMessage);
                        command.Parameters.AddWithValue("@AIResponse", aiResponse);
                        command.Parameters.AddWithValue("@CreateTime", currentTime);
                        
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"保存用户 {userId} 的AI聊天历史记录失败");
                return false;
            }
        }
        
        /// <summary>
        /// 总结聊天记录
        /// </summary>
        public async Task<AIChatResponseDTO> SummarizeChatAsync(ChatSummaryRequestDTO request)
        {
            try
            {
                _logger.LogInformation($"开始生成聊天总结，群组ID: {request.GroupId}, 聊天室ID: {request.RoomId}");
                
                // 获取聊天记录
                List<ChatMessage> messages;
                if (request.RoomId.HasValue)
                {
                    messages = await GetRoomMessagesAsync(request.RoomId.Value, request.MessageCount);
                    _logger.LogInformation($"获取到聊天室 {request.RoomId} 的 {messages.Count} 条消息");
                }
                else if (request.GroupId.HasValue)
                {
                    // 实现获取群聊消息的逻辑
                    _logger.LogWarning("群组消息总结暂未实现");
                    return new AIChatResponseDTO
                    {
                        Success = false,
                        Error = "群组消息总结功能暂未实现",
                        Timestamp = DateTime.Now
                    };
                }
                else
                {
                    _logger.LogError("请求中未提供RoomId或GroupId");
                    return new AIChatResponseDTO
                    {
                        Success = false,
                        Error = "请求参数错误：需要提供RoomId或GroupId",
                        Timestamp = DateTime.Now
                    };
                }
                
                // 检查消息数量
                if (messages.Count == 0)
                {
                    _logger.LogWarning("没有找到需要总结的消息");
                    return new AIChatResponseDTO
                    {
                        Success = false,
                        Error = "没有找到聊天记录",
                        Timestamp = DateTime.Now
                    };
                }
                
                // 如果消息数量过多，可能会导致超时，进行裁剪
                if (messages.Count > 30)
                {
                    _logger.LogWarning($"消息数量过多({messages.Count})，已裁剪为最近的30条");
                    messages = messages.Skip(Math.Max(0, messages.Count - 30)).ToList();
                }
                
                // 格式化聊天记录作为提示词
                string prompt = $"请简洁总结以下聊天记录的主要内容：\n\n{FormatChatMessagesForPrompt(messages)}";
                _logger.LogInformation($"生成的提示词长度: {prompt.Length}");
                
                // 构建消息数组
                var apiMessages = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>
                    {
                        { "role", "system" },
                        { "content", GetSummarySystemPrompt() }
                    },
                    new Dictionary<string, string>
                    {
                        { "role", "user" },
                        { "content", prompt }
                    }
                };
                
                // 构建请求体
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = apiMessages,
                    temperature = 0.2, // 降低温度值使输出更加确定和精简
                    max_tokens = 600  // 减少最大token以加快响应速度
                };
                
                // 序列化请求体
                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                _logger.LogInformation("正在发送总结请求到AI API...");
                
                // 创建一个带超时的CancellationTokenSource，增加超时时间
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(180));
                
                // 发送请求
                var response = await _httpClient.PostAsync(_apiEndpoint, content, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("AI总结生成成功");
                    
                    // 解析响应
                    using var jsonDoc = JsonDocument.Parse(responseBody);
                    var root = jsonDoc.RootElement;
                    
                    if (root.TryGetProperty("choices", out var choices) && 
                        choices.GetArrayLength() > 0 && 
                        choices[0].TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        var summary = messageContent.GetString();
                        _logger.LogInformation($"聊天总结已生成，长度: {summary.Length}字符");
                        
                        return new AIChatResponseDTO
                        {
                            Message = summary,
                            Success = true,
                            Timestamp = DateTime.Now
                        };
                    }
                    else
                    {
                        _logger.LogError("无法从API响应中解析出总结内容");
                        return new AIChatResponseDTO
                        {
                            Success = false,
                            Error = "无法解析API响应",
                            Timestamp = DateTime.Now
                        };
                    }
                }
                
                // 如果解析失败或请求不成功
                _logger.LogError($"AI总结生成失败: HTTP {response.StatusCode}, {responseBody}");
                return new AIChatResponseDTO
                {
                    Success = false,
                    Error = $"处理AI总结请求失败: {response.StatusCode}",
                    Timestamp = DateTime.Now
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "AI总结请求超时");
                return new AIChatResponseDTO
                {
                    Success = false,
                    Error = $"AI总结请求超时: {ex.Message}",
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成聊天总结异常");
                return new AIChatResponseDTO
                {
                    Success = false,
                    Error = $"生成聊天总结异常: {ex.Message}",
                    Timestamp = DateTime.Now
                };
            }
        }
        
        /// <summary>
        /// 从数据库获取聊天室消息
        /// </summary>
        private async Task<List<ChatMessage>> GetRoomMessagesAsync(int roomId, int count)
        {
            var messages = new List<ChatMessage>();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string query = @"SELECT rm.MessageId, rm.SenderId, u.Username AS SenderName, 
                                     rm.Content, rm.CreateTime
                                     FROM RoomMessages rm
                                     JOIN Users u ON rm.SenderId = u.UserId
                                     WHERE rm.RoomId = @RoomId AND rm.MessageType = 'text'
                                     ORDER BY rm.CreateTime DESC";
                    
                    // 限制消息数量，防止处理过多消息导致超时
                    int messageLimit = count > 0 ? Math.Min(count, 50) : 50;
                    query += " LIMIT @Count";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RoomId", roomId);
                        command.Parameters.AddWithValue("@Count", messageLimit);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // 跳过空消息内容
                                string content = reader["Content"].ToString();
                                if (string.IsNullOrWhiteSpace(content))
                                    continue;
                                    
                                messages.Add(new ChatMessage
                                {
                                    MessageId = Convert.ToInt32(reader["MessageId"]),
                                    SenderId = Convert.ToInt32(reader["SenderId"]),
                                    SenderName = reader["SenderName"].ToString(),
                                    Content = content,
                                    SendTime = Convert.ToDateTime(reader["CreateTime"])
                                });
                            }
                        }
                    }
                }
                
                // 按时间正序排列，便于处理
                messages.Reverse();
                
                _logger.LogInformation($"获取了 {messages.Count} 条消息用于总结");
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取聊天室 {roomId} 的消息失败");
                return messages;
            }
        }
        
        /// <summary>
        /// 格式化聊天消息作为提示词
        /// </summary>
        private string FormatChatMessagesForPrompt(List<ChatMessage> messages)
        {
            var formattedMessages = new StringBuilder();
            
            foreach (var message in messages)
            {
                formattedMessages.AppendLine($"[{message.SendTime:yyyy-MM-dd HH:mm:ss}] {message.SenderName}: {message.Content}");
            }
            
            return formattedMessages.ToString();
        }
        
        /// <summary>
        /// 获取系统提示词
        /// </summary>
        private string GetSystemPrompt(string username)
        {
            return @"你是武汉大学学生互助交流平台的智能助手'WHU-AI'，你的目标是帮助学生解决学习、生活、心理等方面的问题。

            请记住以下关键信息：
            1. 你是一个友好、耐心、专业的助手，总是尽力提供有用的指导和建议
            2. 你熟悉武汉大学的各种资源、规章制度和校园文化
            3. 当被问到与武汉大学无关的问题时，你也会尽力提供准确的知识
            4. 对于不确定的问题，你会坦诚表明自己的局限性
            5. 你会尊重用户隐私，不会主动要求用户提供敏感信息
            6. 你可以使用emoji表情，但不要过度使用

            与用户" + username + @"对话时，请保持礼貌和尊重，称呼他们为'你'或使用他们的用户名。

            回复应保持简洁明了，避免过长的废话。如果需要提供分步骤的解释，请使用清晰的编号或要点列表。";
        }
        
        /// <summary>
        /// 获取总结系统提示词
        /// </summary>
        private string GetSummarySystemPrompt()
        {
            return @"你是一位专业的聊天分析师，擅长总结和提炼对话的主要内容。
            
            你需要：
            1. 准确总结聊天内容，识别主要话题和关键信息
            2. 注意保持客观，不添加未在原始聊天中出现的信息
            3. 提取有价值的观点、问题和结论
            4. 组织信息使其结构清晰、易于理解
            5. 使用简洁但全面的语言
            
            格式要求：
            - 不要使用#、##、###等Markdown标记符号
            - 不要在标题或条目前添加任何H3:、LI:、TITLE等标记前缀
            - 使用'聊天记录总结'作为标题
            - 使用固定的主要部分标题:
              * 主要话题
              * 重要观点和信息
              * 提出的问题
              * 达成的共识或结论
            - 在每个部分下使用'-'列表符号
            - 如有必要，在最后添加一个简短的'补充观察'部分

            你的总结应该帮助没有时间阅读整个对话的人快速了解主要内容。
            
            请确保不要在输出中包含类似'H3:'、'LI:'或'TITLE'这样的标记，直接使用纯文本格式输出内容。";
        }
    }
    
    // 用于内部处理的聊天消息类
    internal class ChatMessage
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime SendTime { get; set; }
    }
} 