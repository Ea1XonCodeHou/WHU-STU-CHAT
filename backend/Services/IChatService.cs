using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTOs;

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
        /// 添加用户到在线列表
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <param name="user">用户信息</param>
        void AddUserToRoom(int roomId, UserDTO user);

        /// <summary>
        /// 从在线列表移除用户
        /// </summary>
        /// <param name="roomId">聊天室ID</param>
        /// <param name="userId">用户ID</param>
        void RemoveUserFromRoom(int roomId, int userId);
    }
} 