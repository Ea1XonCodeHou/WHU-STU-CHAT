using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTOs;


namespace backend.Services
{
    public interface IGroupService
    {

        /// <summary>
        /// 创建群组
        /// </summary>
        /// <param name="groupName">群组名称</param>
        /// <param name="creatorId">创建者用户ID</param>
        /// <returns>创建的群组ID</returns>
        Task<int> CreateGroupAsync(GroupRegDTO groupRegDto);

        /// <summary>
        /// 删除群组
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteGroupAsync(int groupId);

        /// <summary>
        /// 获取所有群组
        /// </summary>
        /// <returns>群组列表</returns>
        Task<List<GroupDTO>> GetAllGroupsAsync(int userId);

        /// <summary>
        /// 根据群组名称搜索群组
        /// </summary>
        /// <param name="groupName">群组名称</param>
        /// <returns>匹配的群组列表</returns>
        Task<List<GroupDTO>> SearchGroupsByNameAsync(string groupName, int userId);

        /// <summary>
        /// 获取群组详情
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <returns>群组详情</returns>
        Task<GroupDetailDTO> GetGroupDetailsAsync(int groupId);

        /// <summary>
        /// 根据群组ID获取群组信息
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <returns>群组信息</returns>
        Task<GroupDTO> GetGroupAsync(int groupId);


        /// <summary>
        /// 添加用户到群组
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否添加成功</returns>
        Task<bool> AddUserToGroupAsync(int groupId, int userId);

        /// <summary>
        /// 从群组移除用户
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否移除成功</returns>
        Task<bool> RemoveUserFromGroupAsync(int groupId, int userId);

        /// <summary>
        /// 获取群组用户列表
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <returns>用户列表</returns>
        Task<List<UserDTO>> GetGroupUsersAsync(int groupId);

        /// <summary>
        /// 保存群组消息
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="userId">发送者用户ID</param>
        /// <param name="message">消息内容</param>
        /// <returns>消息ID</returns>
        Task<int> SaveGroupMessageAsync(int groupId, int userId, string message);

        /// <summary>
        /// 获取群组历史消息
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="count">消息数量</param>
        /// <returns>消息列表</returns>
        Task<List<GroupMessageDTO>> GetGroupMessagesAsync(int groupId, int count);
        
        /// <summary>
        /// 获取两个用户之间的私聊群组
        /// </summary>
        /// <param name="userId1">用户1的ID</param>
        /// <param name="userId2">用户2的ID</param>
        /// <returns>私聊群组列表</returns>
        Task<List<GroupDTO>> GetPrivateGroupBetweenUsersAsync(int userId1, int userId2);
    }
}
