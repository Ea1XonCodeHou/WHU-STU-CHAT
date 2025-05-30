﻿using System;
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
        Task<bool> DeleteGroupAsync(int groupId, int operatorUserId);

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
        Task<bool> AddUserToGroupAsync(int groupId, int userId, int operatorUserId);

        /// <summary>
        /// 通过用户名添加用户到群组
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="userName">用户名</param>
        /// <returns>是否添加成功</returns>
        Task<bool> AddUserToGroupByUserNameAsync(int groupId, string userName,int operatorUserId);


        /// <summary>
        /// 从群组移除用户
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否移除成功</returns>
        Task<bool> RemoveUserFromGroupAsync(int groupId, int userId, int operatorUserId);

        /// <summary>
        /// 获取群组用户列表
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <returns>用户列表</returns>
        Task<List<GroupMemberDTO>> GetGroupUsersAsync(int groupId);

        /// <summary>
        /// 切换群成员的管理员角色（admin/member），如果为群主（creator）则返回"群主不能卸任"
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>切换结果消息，成功返回"已切换"，群主返回"群主不能卸任"，失败返回错误信息</returns>
        Task<string> ToggleAdminRoleAsync(int groupId, int userId, int operatorUserId);

        /// <summary>
        /// 保存群组消息
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="userId">发送者用户ID</param>
        /// <param name="message">消息内容</param>
        /// <returns>消息ID</returns>
        Task<int> SaveGroupMessageAsync(int groupId, int userId, string message);

        /// <summary>
        /// 保存群组图片消息
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="userId">发送者ID</param>
        /// <param name="imageUrl">图片URL</param>
        /// <param name="fileName">文件名</param>
        /// <param name="fileSize">文件大小</param>
        /// <returns>消息ID</returns>
        Task<int> SaveGroupImageMessageAsync(int groupId, int userId, string imageUrl, string fileName, long fileSize);

        /// <summary>
        /// 获取群组历史消息
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="count">消息数量</param>
        /// <returns>消息列表</returns>
        /// 
        Task<List<GroupMessageDTO>> GetGroupMessagesAsync(int groupId, int count);


        /// <summary>
        /// 添加好友
        /// </summary>
        /// <param name="user1Id">用户1的ID</param>
        /// <param name="user2Id">用户2的ID</param>
        /// <returns>是否添加成功</returns>
        Task<bool> AddFriendAsync(int user1Id, int user2Id);

        /// <summary>
        /// 删除好友关系
        /// </summary>
        /// <param name="user1Id">用户1的ID</param>
        /// <param name="user2Id">用户2的ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteFriendAsync(int user1Id, int user2Id);

        /// <summary>
        /// 获取用户的好友列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>好友列表</returns>
        Task<List<FriendshipDTO>> GetFriendsAsync(int userId);

        /// <summary>
        /// 根据用户ID和好友ID获取好友信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="friendId">好友ID</param>
        /// <returns>好友信息</returns>
        Task<FriendshipDTO> GetFriendByIdAsync(int userId, int friendId);


        /// <summary>
        /// 获取两个用户之间的私聊群组
        /// </summary>
        /// <param name="userId1">用户1的ID</param>
        /// <param name="userId2">用户2的ID</param>
        /// <returns>私聊群组列表</returns>
        Task<List<GroupDTO>> GetPrivateGroupBetweenUsersAsync(int userId1, int userId2);
        
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
