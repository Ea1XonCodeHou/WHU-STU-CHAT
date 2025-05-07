namespace backend.Models
{
    /// <summary>
    /// 评论模型
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// 评论ID
        /// </summary>
        public int CommentId { get; set; }

        /// <summary>
        /// 所属帖子ID
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// 评论内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 评论者ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 父评论ID（用于回复评论，如无则为0）
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// 点赞数
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// 是否匿名发布
        /// </summary>
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
} 