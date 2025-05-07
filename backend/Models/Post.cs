using System;

namespace backend.Models
{
    /// <summary>
    /// 讨论区帖子模型
    /// </summary>
    public class Post
    {
        /// <summary>
        /// 帖子ID
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// 所属讨论区ID
        /// </summary>
        public int DiscussionId { get; set; }

        /// <summary>
        /// 帖子标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 帖子内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 作者ID
        /// </summary>
        public int AuthorId { get; set; }

        /// <summary>
        /// 点赞数
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// 评论数
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// 帖子类型（普通、置顶、精华等）
        /// </summary>
        public string PostType { get; set; }

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