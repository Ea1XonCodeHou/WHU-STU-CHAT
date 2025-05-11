using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTOs;

namespace backend.Services
{
    public interface IFriendshipService
    {
        /// <summary>
        /// 创建好友请求
        /// </summary>
        Task<bool> CreateFriendRequestAsync(int userId, int friendId);
        
        /// <summary>
        /// 接受好友请求
        /// </summary>
        Task<bool> AcceptFriendRequestAsync(int userId, int friendId);
        
        /// <summary>
        /// 拒绝好友请求
        /// </summary>
        Task<bool> RejectFriendRequestAsync(int userId, int friendId);
        
        /// <summary>
        /// 删除好友关系
        /// </summary>
        Task<bool> DeleteFriendshipAsync(int userId, int friendId);
        
        /// <summary>
        /// 获取用户的好友列表
        /// </summary>
        Task<List<UserDTO>> GetUserFriendsAsync(int userId);
        
        /// <summary>
        /// 检查是否为好友关系
        /// </summary>
        Task<bool> CheckIsFriendAsync(int userId, int friendId);
        
        /// <summary>
        /// 获取好友关系状态
        /// </summary>
        Task<string> GetFriendshipStatusAsync(int userId, int friendId);
        
        /// <summary>
        /// 拉黑好友
        /// </summary>
        Task<bool> BlockFriendAsync(int userId, int friendId);
    }
} 