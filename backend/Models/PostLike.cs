namespace backend.Models
{
    /// <summary>
    /// 帖子点赞模型
    /// </summary>
    public class PostLike
    {
        /// <summary>
        /// 点赞ID
        /// </summary>
        public int LikeId { get; set; }

        /// <summary>
        /// 帖子ID
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
} 