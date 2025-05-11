using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTOs;
using Microsoft.AspNetCore.Http;

namespace backend.Services
{
    /// <summary>
    /// 聊天服务接口
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// 获取聊天室名称
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <returns>聊天室名称</returns>
        Task<string> GetRoomNameAsync(int roomId);

        /// <summary>
        /// 获取聊天室历史消息
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <param name="count">消息数量</param>
        /// <returns>消息列表</returns>
        Task<List<MessageDTO>> GetRoomMessagesAsync(int roomId, int count);

        /// <summary>
        /// 保存聊天室消息
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="message">消息内容</param>
        /// <returns>消息ID</returns>
        Task<int> SaveRoomMessageAsync(int roomId, int userId, string message);
        
        /// <summary>
        /// 保存聊天室消息（带消息类型）
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="message">消息内容</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="fileUrl">文件URL</param>
        /// <param name="fileName">文件名</param>
        /// <param name="fileSize">文件大小</param>
        /// <returns>消息ID</returns>
        Task<int> SaveRoomMessageWithTypeAsync(int roomId, int userId, string message, 
            string messageType, string fileUrl = null, string fileName = null, long? fileSize = null);
        
        /// <summary>
        /// 上传文件到临时目录
        /// </summary>
        /// <param name="file">文件对象</param>
        /// <returns>文件URL和信息</returns>
        Task<(string FileUrl, string FileName, long FileSize)> UploadTempFileAsync(IFormFile file);

        /// <summary>
        /// 获取聊天室在线用户
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <returns>用户列表</returns>
        Task<List<UserDTO>> GetRoomOnlineUsersAsync(int roomId);

        /// <summary>
        /// 获取或创建默认聊天室
        /// </summary>
        /// <returns>聊天室ID</returns>
        Task<int> GetOrCreateDefaultRoomAsync();

        /// <summary>
        /// 添加用户到聊天室列表
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <param name="user">用户信息</param>
        void AddUserToRoom(int roomId, UserDTO user);

        /// <summary>
        /// 从聊天室列表移除用户
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <param name="userId">用户ID</param>
        void RemoveUserFromRoom(int roomId, int userId);
        
        /// <summary>
        /// 获取私聊历史消息
        /// </summary>
        /// <param name="userId">当前用户ID</param>
        /// <param name="friendId">好友用户ID</param>
        /// <param name="count">消息数量</param>
        /// <returns>消息列表</returns>
        Task<List<MessageDTO>> GetPrivateChatHistoryAsync(int userId, int friendId, int count);
        
        /// <summary>
        /// 保存私聊消息
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>消息ID</returns>
        Task<int> SavePrivateMessageAsync(MessageDTO message);

        /// <summary>
        /// 获取用户在线状态
        /// </summary>
        bool IsUserOnline(int userId);

        /// <summary>
        /// 设置用户在线状态
        /// </summary>
        void SetUserOnline(int userId, bool isOnline);
        
        /// <summary>
        /// 获取在线用户列表
        /// </summary>
        List<int> GetOnlineUsers();
    }
} 