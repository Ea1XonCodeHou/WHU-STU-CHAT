using backend.Models;

namespace backend.Services
{
    /// <summary>
    /// 讨论区服务接口
    /// </summary>
    public interface IDiscussionService
    {
        /// <summary>
        /// 获取所有讨论区
        /// </summary>
        Task<List<Discussion>> GetAllDiscussionsAsync();

        /// <summary>
        /// 根据ID获取讨论区
        /// </summary>
        Task<Discussion> GetDiscussionByIdAsync(int discussionId);

        /// <summary>
        /// 创建讨论区
        /// </summary>
        Task<int> CreateDiscussionAsync(Discussion discussion);

        /// <summary>
        /// 更新讨论区
        /// </summary>
        Task<bool> UpdateDiscussionAsync(Discussion discussion);

        /// <summary>
        /// 删除讨论区
        /// </summary>
        Task<bool> DeleteDiscussionAsync(int discussionId);

        /// <summary>
        /// 获取热门讨论区
        /// </summary>
        Task<List<Discussion>> GetHotDiscussionsAsync();

        /// <summary>
        /// 获取讨论区内所有帖子
        /// </summary>
        Task<List<Post>> GetPostsByDiscussionIdAsync(int discussionId);

        /// <summary>
        /// 创建帖子
        /// </summary>
        Task<int> CreatePostAsync(Post post);

        /// <summary>
        /// 获取帖子详情
        /// </summary>
        Task<Post> GetPostByIdAsync(int postId);

        /// <summary>
        /// 更新帖子
        /// </summary>
        Task<bool> UpdatePostAsync(Post post);

        /// <summary>
        /// 删除帖子
        /// </summary>
        Task<bool> DeletePostAsync(int postId);

        /// <summary>
        /// 获取帖子评论
        /// </summary>
        Task<List<Comment>> GetCommentsByPostIdAsync(int postId);

        /// <summary>
        /// 添加评论
        /// </summary>
        Task<int> AddCommentAsync(Comment comment);

        /// <summary>
        /// 删除评论
        /// </summary>
        Task<bool> DeleteCommentAsync(int commentId);

        /// <summary>
        /// 通过ID获取评论
        /// </summary>
        Task<Comment> GetCommentByIdAsync(int commentId);

        /// <summary>
        /// 点赞帖子
        /// </summary>
        /// <param name="postId">帖子ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否点赞成功</returns>
        Task<bool> LikePostAsync(int postId, int userId);

        /// <summary>
        /// 取消点赞帖子
        /// </summary>
        /// <param name="postId">帖子ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否取消点赞成功</returns>
        Task<bool> UnlikePostAsync(int postId, int userId);

        /// <summary>
        /// 检查用户是否已点赞帖子
        /// </summary>
        /// <param name="postId">帖子ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否已点赞</returns>
        Task<bool> HasUserLikedPostAsync(int postId, int userId);

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户信息</returns>
        Task<dynamic> GetAuthorInfoAsync(int userId);
    }
} 