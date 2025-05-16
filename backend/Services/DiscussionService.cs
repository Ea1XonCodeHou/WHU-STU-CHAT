using backend.Models;
using MySql.Data.MySqlClient;
using System.Data;
using Dapper;

namespace backend.Services
{
    /// <summary>
    /// 讨论区服务实现
    /// </summary>
    public class DiscussionService : IDiscussionService
    {
        private readonly IConfiguration _configuration;

        public DiscussionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 获取数据库连接
        /// </summary>
        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        /// <summary>
        /// 获取所有讨论区
        /// </summary>
        public async Task<List<Discussion>> GetAllDiscussionsAsync()
        {
            var discussions = new List<Discussion>();
            try
            {
                Console.WriteLine("正在获取所有讨论区...");
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    Console.WriteLine("数据库连接已打开");
                    
                    using (var command = new MySqlCommand("SELECT * FROM discussions ORDER BY UpdateTime DESC", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // 检查是否有结果
                            if (!reader.HasRows)
                            {
                                Console.WriteLine("数据库中没有讨论区数据，初始化默认讨论区");
                                // 如果没有结果，尝试创建默认讨论区
                                await reader.CloseAsync(); // 关闭reader后才能执行其他命令
                                
                                try
                                {
                                    // 检查表是否存在
                                    var checkTableCommand = new MySqlCommand(
                                        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'discussions'",
                                        connection);
                                    int tableExists = Convert.ToInt32(await checkTableCommand.ExecuteScalarAsync());
                                    
                                    if (tableExists == 0)
                                    {
                                        Console.WriteLine("discussions表不存在");
                                        throw new Exception("discussions表不存在");
                                    }
                                    
                                    // 创建默认讨论区
                                    var createDefaultDiscussionCommand = new MySqlCommand(
                                        @"INSERT INTO discussions (Title, Description, CreatorId, IsHot, CreateTime, UpdateTime) 
                                          VALUES ('校园生活', '讨论校园日常生活、活动和经验分享', 1, 1, NOW(), NOW()),
                                                 ('学习交流', '学业问题、考试经验、学习方法分享', 1, 1, NOW(), NOW()),
                                                 ('校园公告', '重要通知与公告', 1, 0, NOW(), NOW()),
                                                 ('失物招领', '丢失和拾获物品信息发布', 1, 0, NOW(), NOW())",
                                        connection);
                                        
                                    await createDefaultDiscussionCommand.ExecuteNonQueryAsync();
                                    Console.WriteLine("已创建默认讨论区");
                                    
                                    // 重新获取数据
                                    var reloadCommand = new MySqlCommand("SELECT * FROM discussions ORDER BY UpdateTime DESC", connection);
                                    using (var reloadReader = await reloadCommand.ExecuteReaderAsync())
                                    {
                                        while (await reloadReader.ReadAsync())
                                        {
                                            discussions.Add(new Discussion
                                            {
                                                DiscussionId = reloadReader.GetInt32("DiscussionId"),
                                                Title = reloadReader.GetString("Title"),
                                                Description = reloadReader.GetString("Description"),
                                                CreatorId = reloadReader.GetInt32("CreatorId"),
                                                IsHot = reloadReader.GetBoolean("IsHot"),
                                                CreateTime = reloadReader.GetDateTime("CreateTime"),
                                                UpdateTime = reloadReader.GetDateTime("UpdateTime")
                                            });
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"创建默认讨论区失败: {ex.Message}");
                                    throw;
                                }
                            }
                            else
                            {
                                Console.WriteLine("开始读取讨论区数据");
                                while (await reader.ReadAsync())
                                {
                                    try
                                    {
                                        discussions.Add(new Discussion
                                        {
                                            DiscussionId = reader.GetInt32("DiscussionId"),
                                            Title = reader.GetString("Title"),
                                            Description = reader.GetString("Description"),
                                            CreatorId = reader.GetInt32("CreatorId"),
                                            IsHot = reader.GetBoolean("IsHot"),
                                            CreateTime = reader.GetDateTime("CreateTime"),
                                            UpdateTime = reader.GetDateTime("UpdateTime")
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"读取讨论区记录时出错: {ex.Message}");
                                        // 跳过错误的记录，继续处理其他记录
                                    }
                                }
                            }
                        }
                    }
                }
                Console.WriteLine($"成功获取 {discussions.Count} 个讨论区");
                return discussions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取讨论区失败: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取讨论区
        /// </summary>
        public async Task<Discussion> GetDiscussionByIdAsync(int discussionId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand("SELECT * FROM discussions WHERE DiscussionId = @DiscussionId", connection))
                {
                    command.Parameters.AddWithValue("@DiscussionId", discussionId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Discussion
                            {
                                DiscussionId = reader.GetInt32("DiscussionId"),
                                Title = reader.GetString("Title"),
                                Description = reader.GetString("Description"),
                                CreatorId = reader.GetInt32("CreatorId"),
                                IsHot = reader.GetBoolean("IsHot"),
                                CreateTime = reader.GetDateTime("CreateTime"),
                                UpdateTime = reader.GetDateTime("UpdateTime")
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 创建讨论区
        /// </summary>
        public async Task<int> CreateDiscussionAsync(Discussion discussion)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(
                    "INSERT INTO discussions (Title, Description, CreatorId, IsHot, CreateTime, UpdateTime) " +
                    "VALUES (@Title, @Description, @CreatorId, @IsHot, @CreateTime, @UpdateTime); " +
                    "SELECT LAST_INSERT_ID();", connection))
                {
                    command.Parameters.AddWithValue("@Title", discussion.Title);
                    command.Parameters.AddWithValue("@Description", discussion.Description);
                    command.Parameters.AddWithValue("@CreatorId", discussion.CreatorId);
                    command.Parameters.AddWithValue("@IsHot", discussion.IsHot);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var id = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return id;
                }
            }
        }

        /// <summary>
        /// 更新讨论区
        /// </summary>
        public async Task<bool> UpdateDiscussionAsync(Discussion discussion)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(
                    "UPDATE discussions SET Title = @Title, Description = @Description, " +
                    "IsHot = @IsHot, UpdateTime = @UpdateTime " +
                    "WHERE DiscussionId = @DiscussionId", connection))
                {
                    command.Parameters.AddWithValue("@DiscussionId", discussion.DiscussionId);
                    command.Parameters.AddWithValue("@Title", discussion.Title);
                    command.Parameters.AddWithValue("@Description", discussion.Description);
                    command.Parameters.AddWithValue("@IsHot", discussion.IsHot);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        /// <summary>
        /// 删除讨论区
        /// </summary>
        public async Task<bool> DeleteDiscussionAsync(int discussionId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 先删除该讨论区下的所有评论
                        using (var cmd1 = new MySqlCommand(
                            "DELETE FROM comment WHERE PostId IN (SELECT PostId FROM posts WHERE DiscussionId = @DiscussionId)", connection))
                        {
                            cmd1.Transaction = transaction;
                            cmd1.Parameters.AddWithValue("@DiscussionId", discussionId);
                            await cmd1.ExecuteNonQueryAsync();
                        }

                        // 删除该讨论区下的所有帖子
                        using (var cmd2 = new MySqlCommand(
                            "DELETE FROM posts WHERE DiscussionId = @DiscussionId", connection))
                        {
                            cmd2.Transaction = transaction;
                            cmd2.Parameters.AddWithValue("@DiscussionId", discussionId);
                            await cmd2.ExecuteNonQueryAsync();
                        }

                        // 删除讨论区
                        using (var cmd3 = new MySqlCommand(
                            "DELETE FROM discussions WHERE DiscussionId = @DiscussionId", connection))
                        {
                            cmd3.Transaction = transaction;
                            cmd3.Parameters.AddWithValue("@DiscussionId", discussionId);
                            var result = await cmd3.ExecuteNonQueryAsync() > 0;
                            await transaction.CommitAsync();
                            return result;
                        }
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 获取热门讨论区
        /// </summary>
        public async Task<List<Discussion>> GetHotDiscussionsAsync()
        {
            var discussions = new List<Discussion>();
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand("SELECT * FROM discussions WHERE IsHot = 1 ORDER BY UpdateTime DESC", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            discussions.Add(new Discussion
                            {
                                DiscussionId = reader.GetInt32("DiscussionId"),
                                Title = reader.GetString("Title"),
                                Description = reader.GetString("Description"),
                                CreatorId = reader.GetInt32("CreatorId"),
                                IsHot = reader.GetBoolean("IsHot"),
                                CreateTime = reader.GetDateTime("CreateTime"),
                                UpdateTime = reader.GetDateTime("UpdateTime")
                            });
                        }
                    }
                }
            }
            return discussions;
        }

        /// <summary>
        /// 获取讨论区内所有帖子
        /// </summary>
        public async Task<List<Post>> GetPostsByDiscussionIdAsync(int discussionId)
        {
            var posts = new List<Post>();
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(
                    "SELECT * FROM posts WHERE DiscussionId = @DiscussionId ORDER BY PostType DESC, UpdateTime DESC", connection))
                {
                    command.Parameters.AddWithValue("@DiscussionId", discussionId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            posts.Add(new Post
                            {
                                PostId = reader.GetInt32("PostId"),
                                DiscussionId = reader.GetInt32("DiscussionId"),
                                Title = reader.GetString("Title"),
                                Content = reader.GetString("Content"),
                                AuthorId = reader.GetInt32("AuthorId"),
                                LikeCount = reader.GetInt32("LikeCount"),
                                CommentCount = reader.GetInt32("CommentCount"),
                                PostType = reader.GetString("PostType"),
                                IsAnonymous = reader.GetBoolean("IsAnonymous"),
                                CreateTime = reader.GetDateTime("CreateTime"),
                                UpdateTime = reader.GetDateTime("UpdateTime")
                            });
                        }
                    }
                }
            }
            return posts;
        }

        /// <summary>
        /// 创建帖子
        /// </summary>
        public async Task<int> CreatePostAsync(Post post)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(
                    "INSERT INTO posts (DiscussionId, Title, Content, AuthorId, LikeCount, CommentCount, PostType, IsAnonymous, CreateTime, UpdateTime) " +
                    "VALUES (@DiscussionId, @Title, @Content, @AuthorId, @LikeCount, @CommentCount, @PostType, @IsAnonymous, @CreateTime, @UpdateTime); " +
                    "SELECT LAST_INSERT_ID();", connection))
                {
                    command.Parameters.AddWithValue("@DiscussionId", post.DiscussionId);
                    command.Parameters.AddWithValue("@Title", post.Title);
                    command.Parameters.AddWithValue("@Content", post.Content);
                    command.Parameters.AddWithValue("@AuthorId", post.AuthorId);
                    command.Parameters.AddWithValue("@LikeCount", 0);
                    command.Parameters.AddWithValue("@CommentCount", 0);
                    command.Parameters.AddWithValue("@PostType", post.PostType ?? "normal");
                    command.Parameters.AddWithValue("@IsAnonymous", post.IsAnonymous);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var id = Convert.ToInt32(await command.ExecuteScalarAsync());
                    
                    // 更新讨论区的更新时间
                    using (var updateCmd = new MySqlCommand(
                        "UPDATE discussions SET UpdateTime = @UpdateTime WHERE DiscussionId = @DiscussionId", connection))
                    {
                        updateCmd.Parameters.AddWithValue("@DiscussionId", post.DiscussionId);
                        updateCmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    return id;
                }
            }
        }

        /// <summary>
        /// 获取帖子详情
        /// </summary>
        public async Task<Post> GetPostByIdAsync(int postId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand("SELECT * FROM posts WHERE PostId = @PostId", connection))
                {
                    command.Parameters.AddWithValue("@PostId", postId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Post
                            {
                                PostId = reader.GetInt32("PostId"),
                                DiscussionId = reader.GetInt32("DiscussionId"),
                                Title = reader.GetString("Title"),
                                Content = reader.GetString("Content"),
                                AuthorId = reader.GetInt32("AuthorId"),
                                LikeCount = reader.GetInt32("LikeCount"),
                                CommentCount = reader.GetInt32("CommentCount"),
                                PostType = reader.GetString("PostType"),
                                IsAnonymous = reader.GetBoolean("IsAnonymous"),
                                CreateTime = reader.GetDateTime("CreateTime"),
                                UpdateTime = reader.GetDateTime("UpdateTime")
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 更新帖子
        /// </summary>
        public async Task<bool> UpdatePostAsync(Post post)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(
                    "UPDATE posts SET Title = @Title, Content = @Content, PostType = @PostType, IsAnonymous = @IsAnonymous, UpdateTime = @UpdateTime " +
                    "WHERE PostId = @PostId", connection))
                {
                    command.Parameters.AddWithValue("@PostId", post.PostId);
                    command.Parameters.AddWithValue("@Title", post.Title);
                    command.Parameters.AddWithValue("@Content", post.Content);
                    command.Parameters.AddWithValue("@PostType", post.PostType);
                    command.Parameters.AddWithValue("@IsAnonymous", post.IsAnonymous);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var result = await command.ExecuteNonQueryAsync() > 0;
                    
                    if (result)
                    {
                        // 更新讨论区的更新时间
                        using (var updateCmd = new MySqlCommand(
                            "UPDATE discussions SET UpdateTime = @UpdateTime WHERE DiscussionId = @DiscussionId", connection))
                        {
                            updateCmd.Parameters.AddWithValue("@DiscussionId", post.DiscussionId);
                            updateCmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }
                    
                    return result;
                }
            }
        }

        /// <summary>
        /// 删除帖子
        /// </summary>
        public async Task<bool> DeletePostAsync(int postId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        int discussionId = 0;
                        
                        // 获取帖子所属的讨论区ID
                        using (var getCmd = new MySqlCommand("SELECT DiscussionId FROM posts WHERE PostId = @PostId", connection))
                        {
                            getCmd.Transaction = transaction;
                            getCmd.Parameters.AddWithValue("@PostId", postId);
                            var result = await getCmd.ExecuteScalarAsync();
                            if (result != null)
                            {
                                discussionId = Convert.ToInt32(result);
                            }
                        }
                        
                        // 删除帖子的所有评论
                        using (var cmd1 = new MySqlCommand("DELETE FROM comment WHERE PostId = @PostId", connection))
                        {
                            cmd1.Transaction = transaction;
                            cmd1.Parameters.AddWithValue("@PostId", postId);
                            await cmd1.ExecuteNonQueryAsync();
                        }

                        // 删除帖子
                        using (var cmd2 = new MySqlCommand("DELETE FROM posts WHERE PostId = @PostId", connection))
                        {
                            cmd2.Transaction = transaction;
                            cmd2.Parameters.AddWithValue("@PostId", postId);
                            var result = await cmd2.ExecuteNonQueryAsync() > 0;
                            
                            if (result && discussionId > 0)
                            {
                                // 更新讨论区的更新时间
                                using (var updateCmd = new MySqlCommand(
                                    "UPDATE discussions SET UpdateTime = @UpdateTime WHERE DiscussionId = @DiscussionId", connection))
                                {
                                    updateCmd.Transaction = transaction;
                                    updateCmd.Parameters.AddWithValue("@DiscussionId", discussionId);
                                    updateCmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                                    await updateCmd.ExecuteNonQueryAsync();
                                }
                            }
                            
                            await transaction.CommitAsync();
                            return result;
                        }
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 获取帖子评论
        /// </summary>
        public async Task<List<Comment>> GetCommentsByPostIdAsync(int postId)
        {
            var comments = new List<Comment>();
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(
                    "SELECT * FROM comment WHERE PostId = @PostId ORDER BY CreateTime ASC", connection))
                {
                    command.Parameters.AddWithValue("@PostId", postId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            comments.Add(new Comment
                            {
                                CommentId = reader.GetInt32("CommentId"),
                                PostId = reader.GetInt32("PostId"),
                                Content = reader.GetString("Content"),
                                UserId = reader.GetInt32("UserId"),
                                ParentId = reader.GetInt32("ParentId"),
                                LikeCount = reader.GetInt32("LikeCount"),
                                IsAnonymous = reader.GetBoolean("IsAnonymous"),
                                CreateTime = reader.GetDateTime("CreateTime"),
                                UpdateTime = reader.GetDateTime("UpdateTime")
                            });
                        }
                    }
                }
            }
            return comments;
        }

        /// <summary>
        /// 添加评论
        /// </summary>
        public async Task<int> AddCommentAsync(Comment comment)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 确保数据有效
                        if (comment.ParentId < 0)
                        {
                            comment.ParentId = 0;
                        }
                        
                        if (comment.LikeCount < 0)
                        {
                            comment.LikeCount = 0;
                        }
                        
                        if (comment.CreateTime == default)
                        {
                            comment.CreateTime = DateTime.Now;
                        }
                        
                        if (comment.UpdateTime == default)
                        {
                            comment.UpdateTime = DateTime.Now;
                        }
                        
                        // 添加评论
                        using (var command = new MySqlCommand(
                            "INSERT INTO comment (PostId, Content, UserId, ParentId, LikeCount, IsAnonymous, CreateTime, UpdateTime) " +
                            "VALUES (@PostId, @Content, @UserId, @ParentId, @LikeCount, @IsAnonymous, @CreateTime, @UpdateTime); " +
                            "SELECT LAST_INSERT_ID();", connection))
                        {
                            command.Transaction = transaction;
                            command.Parameters.AddWithValue("@PostId", comment.PostId);
                            command.Parameters.AddWithValue("@Content", comment.Content);
                            command.Parameters.AddWithValue("@UserId", comment.UserId);
                            command.Parameters.AddWithValue("@ParentId", comment.ParentId);
                            command.Parameters.AddWithValue("@LikeCount", comment.LikeCount);
                            command.Parameters.AddWithValue("@IsAnonymous", comment.IsAnonymous);
                            command.Parameters.AddWithValue("@CreateTime", comment.CreateTime);
                            command.Parameters.AddWithValue("@UpdateTime", comment.UpdateTime);

                            var id = Convert.ToInt32(await command.ExecuteScalarAsync());

                            // 更新帖子的评论数和更新时间
                            using (var updateCmd = new MySqlCommand(
                                "UPDATE posts SET CommentCount = CommentCount + 1, UpdateTime = @UpdateTime " +
                                "WHERE PostId = @PostId", connection))
                            {
                                updateCmd.Transaction = transaction;
                                updateCmd.Parameters.AddWithValue("@PostId", comment.PostId);
                                updateCmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                                await updateCmd.ExecuteNonQueryAsync();
                            }

                            // 获取帖子所属的讨论区ID并更新讨论区的更新时间
                            using (var getCmd = new MySqlCommand("SELECT DiscussionId FROM posts WHERE PostId = @PostId", connection))
                            {
                                getCmd.Transaction = transaction;
                                getCmd.Parameters.AddWithValue("@PostId", comment.PostId);
                                var discussionId = await getCmd.ExecuteScalarAsync();
                                
                                if (discussionId != null)
                                {
                                    using (var updateDiscussionCmd = new MySqlCommand(
                                        "UPDATE discussions SET UpdateTime = @UpdateTime WHERE DiscussionId = @DiscussionId", connection))
                                    {
                                        updateDiscussionCmd.Transaction = transaction;
                                        updateDiscussionCmd.Parameters.AddWithValue("@DiscussionId", discussionId);
                                        updateDiscussionCmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                                        await updateDiscussionCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            await transaction.CommitAsync();
                            return id;
                        }
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 删除评论
        /// </summary>
        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        int postId = 0;
                        
                        // 获取评论所属的帖子ID
                        using (var getCmd = new MySqlCommand("SELECT PostId FROM comment WHERE CommentId = @CommentId", connection))
                        {
                            getCmd.Transaction = transaction;
                            getCmd.Parameters.AddWithValue("@CommentId", commentId);
                            var result = await getCmd.ExecuteScalarAsync();
                            if (result != null)
                            {
                                postId = Convert.ToInt32(result);
                            }
                        }
                        
                        // 删除评论
                        using (var command = new MySqlCommand("DELETE FROM comment WHERE CommentId = @CommentId", connection))
                        {
                            command.Transaction = transaction;
                            command.Parameters.AddWithValue("@CommentId", commentId);
                            var result = await command.ExecuteNonQueryAsync() > 0;
                            
                            if (result && postId > 0)
                            {
                                // 更新帖子的评论数
                                using (var updateCmd = new MySqlCommand(
                                    "UPDATE posts SET CommentCount = GREATEST(CommentCount - 1, 0), UpdateTime = @UpdateTime " +
                                    "WHERE PostId = @PostId", connection))
                                {
                                    updateCmd.Transaction = transaction;
                                    updateCmd.Parameters.AddWithValue("@PostId", postId);
                                    updateCmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                                    await updateCmd.ExecuteNonQueryAsync();
                                }
                                
                                // 获取帖子所属的讨论区ID并更新讨论区的更新时间
                                using (var getCmd = new MySqlCommand("SELECT DiscussionId FROM posts WHERE PostId = @PostId", connection))
                                {
                                    getCmd.Transaction = transaction;
                                    getCmd.Parameters.AddWithValue("@PostId", postId);
                                    var discussionId = await getCmd.ExecuteScalarAsync();
                                    
                                    if (discussionId != null)
                                    {
                                        using (var updateDiscussionCmd = new MySqlCommand(
                                            "UPDATE discussions SET UpdateTime = @UpdateTime WHERE DiscussionId = @DiscussionId", connection))
                                        {
                                            updateDiscussionCmd.Transaction = transaction;
                                            updateDiscussionCmd.Parameters.AddWithValue("@DiscussionId", discussionId);
                                            updateDiscussionCmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                                            await updateDiscussionCmd.ExecuteNonQueryAsync();
                                        }
                                    }
                                }
                            }
                            
                            await transaction.CommitAsync();
                            return result;
                        }
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 通过ID获取评论
        /// </summary>
        public async Task<Comment> GetCommentByIdAsync(int commentId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(
                    "SELECT * FROM comment WHERE CommentId = @CommentId", connection))
                {
                    command.Parameters.AddWithValue("@CommentId", commentId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Comment
                            {
                                CommentId = reader.GetInt32("CommentId"),
                                PostId = reader.GetInt32("PostId"),
                                Content = reader.GetString("Content"),
                                UserId = reader.GetInt32("UserId"),
                                ParentId = reader.GetInt32("ParentId"),
                                LikeCount = reader.GetInt32("LikeCount"),
                                IsAnonymous = reader.GetBoolean("IsAnonymous"),
                                CreateTime = reader.GetDateTime("CreateTime"),
                                UpdateTime = reader.GetDateTime("UpdateTime")
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 点赞帖子
        /// </summary>
        public async Task<bool> LikePostAsync(int postId, int userId)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    // 打开数据库连接
                    await connection.OpenAsync();

                    // 检查帖子是否存在
                    string checkPostSql = "SELECT * FROM Posts WHERE PostId = @PostId";
                    var post = await connection.QueryFirstOrDefaultAsync<Post>(checkPostSql, new { PostId = postId });
                    if (post == null)
                    {
                        return false;
                    }

                    // 检查用户是否已经点赞
                    string checkLikeSql = "SELECT * FROM PostLike WHERE PostId = @PostId AND UserId = @UserId";
                    var existingLike = await connection.QueryFirstOrDefaultAsync<dynamic>(checkLikeSql, new { PostId = postId, UserId = userId });
                    if (existingLike != null)
                    {
                        // 用户已经点赞过了，返回成功
                        return true;
                    }

                    // 开始事务
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 插入点赞记录
                            string insertLikeSql = @"
                                INSERT INTO PostLike (PostId, UserId, CreateTime) 
                                VALUES (@PostId, @UserId, @CreateTime)";
                            await connection.ExecuteAsync(insertLikeSql, new 
                            { 
                                PostId = postId, 
                                UserId = userId, 
                                CreateTime = DateTime.Now 
                            }, transaction);

                            // 更新帖子点赞数
                            string updatePostSql = @"
                                UPDATE Posts 
                                SET LikeCount = LikeCount + 1 
                                WHERE PostId = @PostId";
                            await connection.ExecuteAsync(updatePostSql, new { PostId = postId }, transaction);

                            // 提交事务
                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            // 回滚事务
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"点赞帖子失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 取消点赞帖子
        /// </summary>
        public async Task<bool> UnlikePostAsync(int postId, int userId)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    // 打开数据库连接
                    await connection.OpenAsync();

                    // 检查帖子是否存在
                    string checkPostSql = "SELECT * FROM Posts WHERE PostId = @PostId";
                    var post = await connection.QueryFirstOrDefaultAsync<Post>(checkPostSql, new { PostId = postId });
                    if (post == null)
                    {
                        return false;
                    }

                    // 检查用户是否已经点赞
                    string checkLikeSql = "SELECT * FROM PostLike WHERE PostId = @PostId AND UserId = @UserId";
                    var existingLike = await connection.QueryFirstOrDefaultAsync<dynamic>(checkLikeSql, new { PostId = postId, UserId = userId });
                    if (existingLike == null)
                    {
                        // 用户没有点赞过，返回成功
                        return true;
                    }

                    // 开始事务
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 删除点赞记录
                            string deleteLikeSql = @"
                                DELETE FROM PostLike 
                                WHERE PostId = @PostId AND UserId = @UserId";
                            await connection.ExecuteAsync(deleteLikeSql, new { PostId = postId, UserId = userId }, transaction);

                            // 更新帖子点赞数
                            string updatePostSql = @"
                                UPDATE Posts 
                                SET LikeCount = GREATEST(LikeCount - 1, 0) 
                                WHERE PostId = @PostId";
                            await connection.ExecuteAsync(updatePostSql, new { PostId = postId }, transaction);

                            // 提交事务
                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            // 回滚事务
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"取消点赞帖子失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查用户是否已点赞帖子
        /// </summary>
        public async Task<bool> HasUserLikedPostAsync(int postId, int userId)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    // 打开数据库连接
                    await connection.OpenAsync();

                    // 检查用户是否已经点赞
                    string checkLikeSql = "SELECT COUNT(1) FROM PostLike WHERE PostId = @PostId AND UserId = @UserId";
                    int count = await connection.ExecuteScalarAsync<int>(checkLikeSql, new { PostId = postId, UserId = userId });

                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查用户点赞状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户信息</returns>
        public async Task<dynamic> GetAuthorInfoAsync(int userId)
        {
            try {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    
                    // 直接从Users表中获取基本信息
                    var command = new MySqlCommand(
                        "SELECT UserId, Username, Avatar FROM Users WHERE UserId = @UserId",
                        connection);
                    command.Parameters.AddWithValue("@UserId", userId);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new 
                            {
                                Id = reader.GetInt32("UserId"),
                                Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? $"用户{userId}" : reader.GetString("Username"),
                                Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader.GetString("Avatar")
                            };
                        }
                    }
                    
                    // 如果没有找到用户，返回带ID的默认信息
                    return new { Id = userId, Username = $"用户{userId}" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取用户信息失败: {ex.Message}");
                // 出错时也返回带ID的默认信息
                return new { Id = userId, Username = $"用户{userId}" };
            }
        }
    }
} 