using backend.DTOs;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using System.Transactions;

namespace backend.Services
{
    public class GroupService:IGroupService
    {
        private readonly string _connectionString;
        public GroupService(IConfiguration configuration) 
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<int> CreateGroupAsync(GroupRegDTO groupRegDto)
        {
            if (groupRegDto == null)
            {
                throw new ArgumentNullException(nameof(groupRegDto), "GroupRegDTO不能为空");
            }

            try
            {
                // 检查群组是否已存在
                var groupExists = await CheckGroupExistsInternalAsync(groupRegDto.GroupName);
                if (groupExists)
                {
                    throw new ArgumentException("群组已存在", nameof(groupRegDto.GroupName));
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 使用事务确保数据一致性
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // 插入 ChatGroups 表（不传 GroupId，让数据库自增）
                            var insertGroupCommand = new MySqlCommand(
                                @"INSERT INTO ChatGroups (GroupName, Description, CreatorId, MemberCount, CreateTime, IsPrivate) 
                                  VALUES (@GroupName, @Description, @CreatorId, @MemberCount, @CreateTime, @IsPrivate); 
                                  SELECT LAST_INSERT_ID();",
                                connection, transaction);

                            insertGroupCommand.Parameters.AddWithValue("@GroupName", groupRegDto.GroupName);
                            insertGroupCommand.Parameters.AddWithValue("@Description", groupRegDto.Description);
                            insertGroupCommand.Parameters.AddWithValue("@CreatorId", groupRegDto.CreatorId);
                            insertGroupCommand.Parameters.AddWithValue("@MemberCount", groupRegDto.MemberCount);
                            insertGroupCommand.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);
                            insertGroupCommand.Parameters.AddWithValue("@IsPrivate", 0); // 设置 IsPrivate 为 0

                            var groupId = Convert.ToInt32(await insertGroupCommand.ExecuteScalarAsync());

                            // 用新 groupId 插入 GroupMembers
                            var insertMemberCommand = new MySqlCommand(
                                @"INSERT INTO GroupMembers (MemberId, GroupId, UserId, JoinTime) 
                                  VALUES (@MemberId, @GroupId, @UserId, @JoinTime)",
                                connection, transaction);

                            insertMemberCommand.Parameters.AddWithValue("@MemberId", $"{groupId}+{groupRegDto.CreatorId}");
                            insertMemberCommand.Parameters.AddWithValue("@GroupId", groupId);
                            insertMemberCommand.Parameters.AddWithValue("@UserId", groupRegDto.CreatorId);
                            insertMemberCommand.Parameters.AddWithValue("@JoinTime", DateTime.UtcNow);

                            await insertMemberCommand.ExecuteNonQueryAsync();

                            // 提交事务
                            await transaction.CommitAsync();

                            return groupId;
                        }
                        catch
                        {
                            // 回滚事务
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录详细错误信息
                Console.WriteLine($"Error in CreateGroupAsync: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw new Exception($"创建群组失败: {ex.Message}");
            }
        }

        
        public async Task<bool> DeleteGroupAsync(int groupId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 删除 GroupMembers 表中所有 GroupId 为指定 groupId 的记录
                    var deleteGroupMembersCommand = new MySqlCommand(
                        "DELETE FROM GroupMembers WHERE GroupId = @GroupId",
                        connection);
                    deleteGroupMembersCommand.Parameters.AddWithValue("@GroupId", groupId);
                    await deleteGroupMembersCommand.ExecuteNonQueryAsync();

                    // 删除 ChatGroups 表中 GroupId 为指定 groupId 的记录
                    var deleteGroupCommand = new MySqlCommand(
                        "DELETE FROM ChatGroups WHERE GroupId = @GroupId",
                        connection);
                    deleteGroupCommand.Parameters.AddWithValue("@GroupId", groupId);
                    var rowsAffected = await deleteGroupCommand.ExecuteNonQueryAsync();

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除群组失败: {ex.Message}");
            }
        }
        
        public async Task<List<GroupDTO>> GetAllGroupsAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Step 1: 从 GroupMembers 表中查询所有 UserId 为指定 userId 的 GroupId
                    var getGroupIdsCommand = new MySqlCommand(
                        "SELECT GroupId FROM GroupMembers WHERE UserId = @UserId",
                        connection);
                    getGroupIdsCommand.Parameters.AddWithValue("@UserId", userId);

                    var groupIds = new List<int>();
                    using (var reader = await getGroupIdsCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            groupIds.Add(reader.GetInt32(0));
                        }
                    }

                    // 如果没有找到任何 GroupId，直接返回空列表
                    if (groupIds.Count == 0)
                    {
                        return new List<GroupDTO>();
                    }

                    // Step 2: 从 ChatGroups 表中查询这些 GroupId 对应的 GroupDTO，且 IsPrivate = 0
                    var getGroupsCommand = new MySqlCommand(
                        $"SELECT GroupId, GroupName, UpdateTime, MemberCount FROM ChatGroups WHERE GroupId IN ({string.Join(",", groupIds)}) AND IsPrivate = 0",
                        connection);

                    var groups = new List<GroupDTO>();
                    using (var reader = await getGroupsCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            groups.Add(new GroupDTO
                            {
                                GroupId = reader.GetInt32(0),
                                GroupName = reader.GetString(1),
                                UpdateTime = reader.GetDateTime(2),
                                MemberCount = reader.GetInt32(3)
                            });
                        }
                    }

                    return groups;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取群组列表失败: {ex.Message}");
            }
        }

        
        public async Task<GroupDetailDTO> GetGroupDetailsAsync(int groupId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new MySqlCommand(
                        "SELECT GroupId, GroupName, Description, CreatorId, MemberCount, CreateTime, UpdateTime FROM ChatGroups WHERE GroupId = @GroupId",
                        connection);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new GroupDetailDTO
                            {
                                GroupId = reader.GetInt32(0),
                                GroupName = reader.GetString(1),
                                Description = reader.GetString(2),
                                CreatorId = reader.GetInt32(3),
                                MemberCount = reader.GetInt32(4),
                                CreateTime = reader.GetDateTime(5),
                                UpdateTime = reader.GetDateTime(6)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取群组详情失败: {ex.Message}");
            }
            return null;
        }

        
        public async Task<GroupDTO> GetGroupAsync(int groupId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new MySqlCommand(
                        "SELECT GroupId, GroupName, UpdateTime FROM ChatGroups WHERE GroupId = @GroupId AND IsPrivate = 0",
                        connection);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new GroupDTO
                            {
                                GroupId = reader.GetInt32(0),
                                GroupName = reader.GetString(1),
                                UpdateTime = reader.GetDateTime(2)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取群组失败: {ex.Message}");
            }
            return null;
        }



        public async Task<bool> AddUserToGroupAsync(int groupId, int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Step 1: 检查用户是否已经在群组中
                    var checkMemberCommand = new MySqlCommand(
                        "SELECT COUNT(1) FROM GroupMembers WHERE GroupId = @GroupId AND UserId = @UserId",
                        connection);
                    checkMemberCommand.Parameters.AddWithValue("@GroupId", groupId);
                    checkMemberCommand.Parameters.AddWithValue("@UserId", userId);

                    var exists = Convert.ToInt32(await checkMemberCommand.ExecuteScalarAsync()) > 0;
                    if (exists)
                    {
                        throw new Exception("用户已在群组中");
                    }

                    // Step 2: 插入新成员到 GroupMembers 表
                    var addMemberCommand = new MySqlCommand(
                        "INSERT INTO GroupMembers (MemberId,GroupId, UserId, JoinTime) VALUES (@MemberId, @GroupId, @UserId, @JoinTime)",
                        connection);
                    addMemberCommand.Parameters.AddWithValue("@MemberId", groupId+"+"+userId);
                    addMemberCommand.Parameters.AddWithValue("@GroupId", groupId);
                    addMemberCommand.Parameters.AddWithValue("@UserId", userId);
                    addMemberCommand.Parameters.AddWithValue("@JoinTime", DateTime.UtcNow);
                    var rowsAffected = await addMemberCommand.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        // Step 3: 更新 ChatGroups 表中的 MemberCount
                        var updateMemberCountCommand = new MySqlCommand(
                            "UPDATE ChatGroups SET MemberCount = MemberCount + 1 WHERE GroupId = @GroupId",
                            connection);
                        updateMemberCountCommand.Parameters.AddWithValue("@GroupId", groupId);
                        await updateMemberCountCommand.ExecuteNonQueryAsync();

                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"添加用户到群组失败: {ex.Message}");
            }
        }

        public async Task<bool> AddUserToGroupByUserNameAsync(int groupId, string userName)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Step 1: 根据 userName 查询 userId
                    var getUserIdCommand = new MySqlCommand(
                        "SELECT UserId FROM Users WHERE Username = @UserName",
                        connection);
                    getUserIdCommand.Parameters.AddWithValue("@UserName", userName);

                    var userId = Convert.ToInt32(await getUserIdCommand.ExecuteScalarAsync());
                    if (userId == 0)
                    {
                        throw new Exception($"用户名 {userName} 不存在");
                    }

                    // Step 2: 调用现有的 AddUserToGroupAsync 方法
                    return await AddUserToGroupAsync(groupId, userId);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"通过用户名添加用户到群组失败: {ex.Message}");
            }
        }


        public async Task<bool> RemoveUserFromGroupAsync(int groupId, int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Step 1: 从 GroupMembers 表中删除成员
                    var removeMemberCommand = new MySqlCommand(
                        "DELETE FROM GroupMembers WHERE GroupId = @GroupId AND UserId = @UserId",
                        connection);
                    removeMemberCommand.Parameters.AddWithValue("@GroupId", groupId);
                    removeMemberCommand.Parameters.AddWithValue("@UserId", userId);
                    var rowsAffected = await removeMemberCommand.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        // Step 2: 更新 ChatGroups 表中的 MemberCount
                        var updateMemberCountCommand = new MySqlCommand(
                            "UPDATE ChatGroups SET MemberCount = MemberCount - 1 WHERE GroupId = @GroupId",
                            connection);
                        updateMemberCountCommand.Parameters.AddWithValue("@GroupId", groupId);
                        await updateMemberCountCommand.ExecuteNonQueryAsync();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"从群组移除用户失败: {ex.Message}");
            }
        }
        
        public async Task<List<UserDTO>> GetGroupUsersAsync(int groupId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new MySqlCommand(
                        @"SELECT u.UserId, u.Username, u.LastLoginTime
                          FROM GroupMembers gm
                          JOIN Users u ON gm.UserId = u.UserId
                          WHERE gm.GroupId = @GroupId",
                        connection);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    var users = new List<UserDTO>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new UserDTO
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                LastActive = reader.GetDateTime(2)
                            });
                        }
                    }
                    return users;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取群组用户列表失败: {ex.Message}");
            }
        }

        
        public async Task<int> SaveGroupMessageAsync(int groupId, int userId, string message)//这一个是ai推荐的，并未使用sigallr，晚点可能删除
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new MySqlCommand(
                        "INSERT INTO GroupMessages (GroupId, SenderId, Content, CreateTime) VALUES (@GroupId, @SenderId, @Content, @CreateTIme); SELECT LAST_INSERT_ID();",
                        connection);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    command.Parameters.AddWithValue("@SenderId", userId);
                    command.Parameters.AddWithValue("@Content", message);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);
                    var messageId = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return messageId;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"保存群组消息失败: {ex.Message}");
            }
        }

        
        public async Task<List<GroupMessageDTO>> GetGroupMessagesAsync(int groupId, int count)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new MySqlCommand(
                        "SELECT MessageId, GroupId, SenderId, Content, CreateTime FROM GroupMessages WHERE GroupId = @GroupId ORDER BY CreateTime DESC LIMIT @Count",
                        connection);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    command.Parameters.AddWithValue("@Count", count);
                    var messages = new List<GroupMessageDTO>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            messages.Add(new GroupMessageDTO
                            {
                                MessageId = reader.GetInt32(0),
                                GroupId = reader.GetInt32(1),
                                SenderId = reader.GetInt32(2),
                                Content = reader.GetString(3),
                                CreateTime = reader.GetDateTime(4)
                            });
                        }
                    }
                    return messages;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取群组消息失败: {ex.Message}");
            }
        }


        public async Task<bool> CheckGroupExistsInternalAsync(string groupName)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "SELECT COUNT(1) FROM ChatGroups WHERE GroupName = @GroupName",
                    connection);
                command.Parameters.AddWithValue("@GroupName", groupName);

                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
        }

        public async Task<bool> AddFriendAsync(int user1Id, int user2Id)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var checkFriendCommand = new MySqlCommand(
                        "SELECT COUNT(1) FROM friendship WHERE (UserId1 = @UserId1 AND UserId2 = @UserId2) OR (UserId1 = @UserId2 AND UserId2 = @UserId1)",
                        connection);
                    checkFriendCommand.Parameters.AddWithValue("@UserId1", user1Id);
                    checkFriendCommand.Parameters.AddWithValue("@UserId2", user2Id);

                    var isFriend = Convert.ToInt32(await checkFriendCommand.ExecuteScalarAsync()) > 0;
                    if (isFriend)
                    {
                        throw new Exception("两用户已经是好友");
                    }

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            var createGroupCommand = new MySqlCommand(
                                @"INSERT INTO ChatGroups (GroupName, Description, CreatorId, MemberCount, CreateTime, IsPrivate) 
                                  VALUES (@GroupName, @Description, @CreatorId, @MemberCount, @CreateTime, @IsPrivate); 
                                  SELECT LAST_INSERT_ID();",
                                connection, transaction);

                            createGroupCommand.Parameters.AddWithValue("@GroupName", $"PrivateChat-{user1Id}-{user2Id}");
                            createGroupCommand.Parameters.AddWithValue("@Description", "私聊群组");
                            createGroupCommand.Parameters.AddWithValue("@CreatorId", user1Id);
                            createGroupCommand.Parameters.AddWithValue("@MemberCount", 2);
                            createGroupCommand.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);
                            createGroupCommand.Parameters.AddWithValue("@IsPrivate", 1);

                            var groupId = Convert.ToInt32(await createGroupCommand.ExecuteScalarAsync());

                            var addFriendCommand = new MySqlCommand(
                                "INSERT INTO friendship (UserId1, UserId2, CreateTime, GroupId) VALUES (@UserId1, @UserId2, @CreateTime, @GroupId)",
                                connection, transaction);
                            addFriendCommand.Parameters.AddWithValue("@UserId1", user1Id);
                            addFriendCommand.Parameters.AddWithValue("@UserId2", user2Id);
                            addFriendCommand.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);
                            addFriendCommand.Parameters.AddWithValue("@GroupId", groupId);

                            await addFriendCommand.ExecuteNonQueryAsync();

                            await transaction.CommitAsync();

                            return true;
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"添加好友失败: {ex.Message}");
            }
        }

        public async Task<bool> DeleteFriendAsync(int userId1, int userId2)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 使用事务确保数据一致性
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        // Step 1: 检查是否存在好友关系并获取 GroupId
                        var checkFriendCommand = new MySqlCommand(
                            "SELECT GroupId FROM friendship WHERE (UserId1 = @UserId1 AND UserId2 = @UserId2) OR (UserId1 = @UserId2 AND UserId2 = @UserId1)",
                            connection, transaction);
                        checkFriendCommand.Parameters.AddWithValue("@UserId1", userId1);
                        checkFriendCommand.Parameters.AddWithValue("@UserId2", userId2);

                        var groupId = Convert.ToInt32(await checkFriendCommand.ExecuteScalarAsync());
                        if (groupId == 0)
                        {
                            throw new Exception("两用户之间不存在好友关系");
                        }

                        // Step 2: 删除 GroupMessages 表中引用该群组的记录
                        var deleteMessagesCommand = new MySqlCommand(
                            "DELETE FROM GroupMessages WHERE GroupId = @GroupId",
                            connection, transaction);
                        deleteMessagesCommand.Parameters.AddWithValue("@GroupId", groupId);
                        await deleteMessagesCommand.ExecuteNonQueryAsync();

                        

                        // Step 4: 删除 ChatGroups 表中的记录
                        var deleteGroupCommand = new MySqlCommand(
                            "DELETE FROM ChatGroups WHERE GroupId = @GroupId",
                            connection, transaction);
                        deleteGroupCommand.Parameters.AddWithValue("@GroupId", groupId);
                        var rowsAffected = await deleteGroupCommand.ExecuteNonQueryAsync();

                        // 提交事务
                        await transaction.CommitAsync();

                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除好友失败: {ex.Message}");
            }
        }

        public async Task<List<FriendShipDTO>> GetFriendsAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand(
                        @"SELECT 
                            CASE 
                                WHEN f.UserId1 = @UserId THEN f.UserId2 
                                ELSE f.UserId1 
                            END AS FriendId,
                            u.Username,
                            f.GroupId,
                            f.CreateTime AS FriendshipCreatedTime
                          FROM friendship f
                          JOIN Users u ON u.UserId = 
                              CASE 
                                  WHEN f.UserId1 = @UserId THEN f.UserId2 
                                  ELSE f.UserId1 
                              END
                          WHERE f.UserId1 = @UserId OR f.UserId2 = @UserId",
                        connection);
                    command.Parameters.AddWithValue("@UserId", userId);

                    var friends = new List<FriendShipDTO>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            friends.Add(new FriendShipDTO
                            {
                                FriendId = reader.GetInt32(reader.GetOrdinal("FriendId")),
                                Username = reader.GetString(reader.GetOrdinal("Username")),
                                GroupId = reader.GetInt32(reader.GetOrdinal("GroupId")),
                                FriendshipCreatedTime = reader.GetDateTime(reader.GetOrdinal("FriendshipCreatedTime"))
                            });
                        }
                    }

                    return friends;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取好友列表失败: {ex.Message}");
            }
        }

        public async Task<FriendShipDTO> GetFriendByIdAsync(int userId, int friendId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand(
                        @"SELECT 
                            CASE 
                                WHEN f.UserId1 = @UserId THEN f.UserId2 
                                ELSE f.UserId1 
                            END AS FriendId,
                            u.Username,
                            f.GroupId,
                            f.CreateTime AS FriendshipCreatedTime
                          FROM friendship f
                          JOIN Users u ON u.UserId = 
                              CASE 
                                  WHEN f.UserId1 = @UserId THEN f.UserId2 
                                  ELSE f.UserId1 
                              END
                          WHERE (f.UserId1 = @UserId AND f.UserId2 = @FriendId) 
                             OR (f.UserId1 = @FriendId AND f.UserId2 = @UserId)",
                        connection);

                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@FriendId", friendId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new FriendShipDTO
                            {
                                FriendId = reader.GetInt32(reader.GetOrdinal("FriendId")),
                                Username = reader.GetString(reader.GetOrdinal("Username")),
                                GroupId = reader.GetInt32(reader.GetOrdinal("GroupId")),
                                FriendshipCreatedTime = reader.GetDateTime(reader.GetOrdinal("FriendshipCreatedTime"))
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取好友信息失败: {ex.Message}");
            }

            return null;
        }



        /// <summary>
        /// 获取两个用户之间的私聊群组
        /// </summary>
        /// <param name="userId1">用户1的ID</param>
        /// <param name="userId2">用户2的ID</param>
        /// <returns>私聊群组列表</returns>
        /*public async Task<List<GroupDTO>> GetPrivateGroupBetweenUsersAsync(int userId1, int userId2)
        {
            var result = new List<GroupDTO>();
             
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 查询两个用户都在其中且只有这两个用户的群组
                // 使用子查询确保群组只有两个成员且包含这两个用户
                var query = @"
                    SELECT g.GroupId, g.GroupName, g.Description, g.CreatorId, g.MemberCount, g.CreateTime, g.UpdateTime 
                     FROM ChatGroups g
                WHERE g.MemberCount = 2
                AND g.GroupId IN (
                    SELECT gm1.GroupId 
                    FROM GroupMembers gm1
                    WHERE gm1.UserId = @UserId1
                    AND EXISTS (
                        SELECT 1 
                        FROM GroupMembers gm2 
                        WHERE gm2.GroupId = gm1.GroupId 
                        AND gm2.UserId = @UserId2
                    )
                    AND (
                        SELECT COUNT(*) 
                        FROM GroupMembers gm3 
                        WHERE gm3.GroupId = gm1.GroupId
                    ) = 2
                )";
                
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId1", userId1);
                command.Parameters.AddWithValue("@UserId2", userId2);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var group = new GroupDTO
                        {
                            GroupId = reader.GetInt32(reader.GetOrdinal("GroupId")),
                            GroupName = reader.GetString(reader.GetOrdinal("GroupName")),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                            CreatorId = reader.GetInt32(reader.GetOrdinal("CreatorId")),
                            MemberCount = reader.GetInt32(reader.GetOrdinal("MemberCount")),
                            CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                            UpdateTime = reader.GetDateTime(reader.GetOrdinal("UpdateTime"))
                        };
                        
                        result.Add(group);
                    }
                }
            }
            
            return result;
        }*/

        /// <summary>
        /// 根据群组名称搜索群组
        /// </summary>
        /// <param name="groupName">群组名称</param>
        /// <param name="userId">用户ID</param>
        /// <returns>匹配的群组列表</returns>
        public async Task<List<GroupDTO>> SearchGroupsByNameAsync(string groupName, int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 查询用户所在的群组，并根据群组名称模糊匹配，且 IsPrivate = 0
                    var sql = @"
                        SELECT g.GroupId, g.GroupName, g.Description, g.UpdateTime, g.MemberCount 
                        FROM ChatGroups g
                        JOIN GroupMembers m ON g.GroupId = m.GroupId
                        WHERE m.UserId = @UserId AND g.GroupName LIKE @SearchPattern AND g.IsPrivate = 0
                        ORDER BY g.UpdateTime DESC";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@SearchPattern", $"%{groupName}%");

                        var groups = new List<GroupDTO>();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                groups.Add(new GroupDTO
                                {
                                    GroupId = reader.GetInt32(0),
                                    GroupName = reader.GetString(1),
                                    Description = reader.GetString(2),
                                    UpdateTime = reader.GetDateTime(3),
                                    MemberCount = reader.GetInt32(4)
                                });
                            }
                        }
                        return groups;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"搜索群组失败: {ex.Message}");
            }
        }
    }
}
