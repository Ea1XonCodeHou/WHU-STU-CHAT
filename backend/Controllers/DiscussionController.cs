using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using backend.DTOs;

namespace backend.Controllers
{
    /// <summary>
    /// 讨论区控制器
    /// </summary>
    [ApiController]
    [Route("api/discussion")]
    public class DiscussionController : ControllerBase
    {
        private readonly IDiscussionService _discussionService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DiscussionController(IDiscussionService discussionService)
        {
            _discussionService = discussionService;
        }

        /// <summary>
        /// 获取所有讨论区
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllDiscussions()
        {
            try
            {
                // 获取所有讨论区
                var discussions = await _discussionService.GetAllDiscussionsAsync();

                // 如果数据库中没有讨论区数据，返回空列表但状态码为200表示成功
                if (discussions == null || discussions.Count == 0)
                {
                    return Ok(new List<DiscussionDTO>());
                }

                return Ok(discussions);
            }
            catch (Exception ex)
            {
                // 记录错误日志
                Console.WriteLine($"获取讨论区失败: {ex.Message}");
                return StatusCode(500, "获取讨论区失败");
            }
        }
        
        // 初始化默认讨论区数据
        private async Task InitializeDefaultDiscussionsAsync()
        {
            try
            {
                // 创建默认讨论区
                var discussions = new List<Discussion>
                {
                    new Discussion
                    {
                        Title = "校园生活",
                        Description = "讨论校园日常生活、活动和经验分享",
                        CreatorId = 1, // 使用ID为1的用户作为创建者
                        IsHot = true,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    },
                    new Discussion
                    {
                        Title = "学习交流",
                        Description = "学业问题、考试经验、学习方法分享",
                        CreatorId = 1,
                        IsHot = true,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    },
                    new Discussion
                    {
                        Title = "校园公告",
                        Description = "重要通知与公告",
                        CreatorId = 1,
                        IsHot = false,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    },
                    new Discussion
                    {
                        Title = "失物招领",
                        Description = "丢失和拾获物品信息发布",
                        CreatorId = 1,
                        IsHot = false,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    }
                };

                // 依次创建讨论区
                foreach (var discussion in discussions)
                {
                    await _discussionService.CreateDiscussionAsync(discussion);
                }
                
                Console.WriteLine("成功初始化默认讨论区数据");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化讨论区数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取热门讨论区
        /// </summary>
        [HttpGet("hot")]
        public async Task<IActionResult> GetHotDiscussions()
        {
            try
            {
                var discussions = await _discussionService.GetHotDiscussionsAsync();
                return Ok(discussions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取热门讨论区失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据ID获取讨论区
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDiscussionById(int id)
        {
            try
            {
                var discussion = await _discussionService.GetDiscussionByIdAsync(id);
                if (discussion == null)
                {
                    return NotFound($"未找到ID为{id}的讨论区");
                }
                return Ok(discussion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取讨论区详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建讨论区
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDiscussion([FromBody] Discussion discussion)
        {
            try
            {
                if (discussion == null)
                {
                    return BadRequest("讨论区数据不能为空");
                }

                var id = await _discussionService.CreateDiscussionAsync(discussion);
                return CreatedAtAction(nameof(GetDiscussionById), new { id }, id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"创建讨论区失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新讨论区
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiscussion(int id, [FromBody] Discussion discussion)
        {
            try
            {
                if (discussion == null)
                {
                    return BadRequest("讨论区数据不能为空");
                }

                discussion.DiscussionId = id;
                var success = await _discussionService.UpdateDiscussionAsync(discussion);
                if (!success)
                {
                    return NotFound($"未找到ID为{id}的讨论区");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"更新讨论区失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除讨论区
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscussion(int id)
        {
            try
            {
                var success = await _discussionService.DeleteDiscussionAsync(id);
                if (!success)
                {
                    return NotFound($"未找到ID为{id}的讨论区");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"删除讨论区失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取讨论区内的所有帖子
        /// </summary>
        [HttpGet("{id}/posts")]
        public async Task<IActionResult> GetPosts(int id)
        {
            try
            {
                var discussion = await _discussionService.GetDiscussionByIdAsync(id);
                if (discussion == null)
                {
                    return NotFound($"未找到ID为{id}的讨论区");
                }

                var posts = await _discussionService.GetPostsByDiscussionIdAsync(id);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取帖子列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建帖子
        /// </summary>
        [HttpPost("{id}/posts")]
        public async Task<IActionResult> CreatePost(int id, [FromBody] Post post)
        {
            try
            {
                if (post == null)
                {
                    return BadRequest("帖子数据不能为空");
                }

                var discussion = await _discussionService.GetDiscussionByIdAsync(id);
                if (discussion == null)
                {
                    return NotFound($"未找到ID为{id}的讨论区");
                }

                post.DiscussionId = id;
                var postId = await _discussionService.CreatePostAsync(post);
                return CreatedAtAction(nameof(GetPost), new { id = postId }, postId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"创建帖子失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取帖子详情
        /// </summary>
        [HttpGet("posts/{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            try
            {
                var post = await _discussionService.GetPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound($"未找到ID为{id}的帖子");
                }
                
                return Ok(post);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取帖子详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新帖子
        /// </summary>
        [HttpPut("posts/{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] Post post)
        {
            try
            {
                if (post == null)
                {
                    return BadRequest("帖子数据不能为空");
                }

                var existingPost = await _discussionService.GetPostByIdAsync(id);
                if (existingPost == null)
                {
                    return NotFound($"未找到ID为{id}的帖子");
                }
                
                // 设置帖子ID和讨论区ID（不允许修改所属讨论区）
                post.PostId = id;
                post.DiscussionId = existingPost.DiscussionId;
                
                var success = await _discussionService.UpdatePostAsync(post);
                if (!success)
                {
                    return StatusCode(500, "更新帖子失败");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"更新帖子失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除帖子
        /// </summary>
        [HttpDelete("posts/{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                var post = await _discussionService.GetPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound($"未找到ID为{id}的帖子");
                }
                
                var success = await _discussionService.DeletePostAsync(id);
                if (!success)
                {
                    return StatusCode(500, "删除帖子失败");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"删除帖子失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取帖子的评论
        /// </summary>
        [HttpGet("posts/{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            try
            {
                var post = await _discussionService.GetPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound($"未找到ID为{id}的帖子");
                }
                
                var comments = await _discussionService.GetCommentsByPostIdAsync(id);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取评论列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加评论
        /// </summary>
        [HttpPost("posts/{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] Comment comment)
        {
            try
            {
                if (comment == null)
                {
                    return BadRequest("评论数据不能为空");
                }

                // 设置帖子ID
                comment.PostId = id;
                
                // 确保评论有创建时间和更新时间
                if (comment.CreateTime == default)
                {
                    comment.CreateTime = DateTime.Now;
                }
                
                if (comment.UpdateTime == default)
                {
                    comment.UpdateTime = DateTime.Now;
                }

                // 尝试获取当前用户ID，如果无法获取则使用前端传来的值
                // var userId = User.Identity.IsAuthenticated ? int.Parse(User.Identity.Name) : comment.UserId;
                // comment.UserId = userId;

                var commentId = await _discussionService.AddCommentAsync(comment);
                return Ok(new { commentId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"添加评论失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除评论
        /// </summary>
        [HttpDelete("comments/{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var comment = await _discussionService.GetCommentByIdAsync(id);
                if (comment == null)
                {
                    return NotFound($"未找到ID为{id}的评论");
                }
                
                var success = await _discussionService.DeleteCommentAsync(id);
                if (!success)
                {
                    return StatusCode(500, "删除评论失败");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"删除评论失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 点赞帖子
        /// </summary>
        [HttpPost("posts/{id}/like")]
        public async Task<IActionResult> LikePost(int id)
        {
            try
            {
                var post = await _discussionService.GetPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound($"未找到ID为{id}的帖子");
                }

                // 从请求中获取用户ID
                if (!int.TryParse(HttpContext.Request.Query["userId"], out int userId))
                {
                    return BadRequest("请提供有效的用户ID");
                }

                var success = await _discussionService.LikePostAsync(id, userId);
                if (!success)
                {
                    return StatusCode(500, "点赞失败");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"点赞失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消点赞帖子
        /// </summary>
        [HttpDelete("posts/{id}/like")]
        public async Task<IActionResult> UnlikePost(int id)
        {
            try
            {
                var post = await _discussionService.GetPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound($"未找到ID为{id}的帖子");
                }

                // 从请求中获取用户ID
                if (!int.TryParse(HttpContext.Request.Query["userId"], out int userId))
                {
                    return BadRequest("请提供有效的用户ID");
                }

                var success = await _discussionService.UnlikePostAsync(id, userId);
                if (!success)
                {
                    return StatusCode(500, "取消点赞失败");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"取消点赞失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查用户是否已点赞帖子
        /// </summary>
        [HttpGet("posts/{id}/like")]
        public async Task<IActionResult> CheckLikeStatus(int id)
        {
            try
            {
                var post = await _discussionService.GetPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound($"未找到ID为{id}的帖子");
                }

                // 从请求中获取用户ID
                if (!int.TryParse(HttpContext.Request.Query["userId"], out int userId))
                {
                    return BadRequest("请提供有效的用户ID");
                }

                var hasLiked = await _discussionService.HasUserLikedPostAsync(id, userId);
                return Ok(new { hasLiked });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"检查点赞状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取帖子作者信息
        /// </summary>
        /// <param name="postId">帖子ID</param>
        /// <returns>作者信息</returns>
        [HttpGet("posts/{postId}/author")]
        public async Task<IActionResult> GetPostAuthor(int postId)
        {
            try
            {
                var post = await _discussionService.GetPostByIdAsync(postId);
                if (post == null)
                {
                    return NotFound("帖子不存在");
                }

                // 如果是匿名帖子，直接返回匿名用户信息
                if (post.IsAnonymous)
                {
                    return Ok(new { Username = "匿名用户" });
                }

                // 获取作者信息
                try 
                {
                    var author = await _discussionService.GetAuthorInfoAsync(post.AuthorId);
                    if (author != null)
                    {
                        return Ok(author);
                    }
                    
                    // 如果获取不到详细信息，返回简单信息
                    return Ok(new { Username = $"用户{post.AuthorId}", Id = post.AuthorId });
                }
                catch
                {
                    // 出错时返回简单信息
                    return Ok(new { Username = $"用户{post.AuthorId}", Id = post.AuthorId });
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"获取作者信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取评论作者信息
        /// </summary>
        /// <param name="commentId">评论ID</param>
        /// <returns>作者信息</returns>
        [HttpGet("comments/{commentId}/author")]
        public async Task<IActionResult> GetCommentAuthor(int commentId)
        {
            try
            {
                var comment = await _discussionService.GetCommentByIdAsync(commentId);
                if (comment == null)
                {
                    return NotFound("评论不存在");
                }

                // 如果是匿名评论，直接返回匿名用户信息
                if (comment.IsAnonymous)
                {
                    return Ok(new { Username = "匿名用户" });
                }

                // 获取作者信息
                try 
                {
                    var author = await _discussionService.GetAuthorInfoAsync(comment.UserId);
                    if (author != null)
                    {
                        return Ok(author);
                    }
                    
                    // 如果获取不到详细信息，返回简单信息
                    return Ok(new { Username = $"用户{comment.UserId}", Id = comment.UserId });
                }
                catch
                {
                    // 出错时返回简单信息
                    return Ok(new { Username = $"用户{comment.UserId}", Id = comment.UserId });
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"获取评论者信息失败: {ex.Message}");
            }
        }
    }
} 