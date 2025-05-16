using backend.DTOs;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Transactions;

namespace backend.Services
{
    public class GroupService:IGroupService
    {
        private readonly string _connectionString;
        // 全局在线用户列表
        private static readonly ConcurrentDictionary<int, bool> _globalOnlineUsers = new ConcurrentDictionary<int, bool>();
        
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
                            insertGroupCommand.Parameters.AddWithValue("@MemberCount", 1);
                            insertGroupCommand.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);
                            insertGroupCommand.Parameters.AddWithValue("@IsPrivate", 0); // 设置 IsPrivate 为 0

                            var groupId = Convert.ToInt32(await insertGroupCommand.ExecuteScalarAsync());

                            // 用新 groupId 插入 GroupMembers
                            var insertMemberCommand = new MySqlCommand(
                                @"INSERT INTO GroupMembers (MemberId, GroupId, UserId, JoinTime,Role) 
                                  VALUES (@MemberId, @GroupId, @UserId, @JoinTime, @Role)",
                                connection, transaction);

                            insertMemberCommand.Parameters.AddWithValue("@MemberId", groupId * 10000 + groupRegDto.CreatorId);
                            insertMemberCommand.Parameters.AddWithValue("@GroupId", groupId);
                            insertMemberCommand.Parameters.AddWithValue("@UserId", groupRegDto.CreatorId);
                            insertMemberCommand.Parameters.AddWithValue("@JoinTime", DateTime.UtcNow);
                            insertMemberCommand.Parameters.AddWithValue("@Role","master");

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


        public async Task<bool> DeleteGroupAsync(int groupId, int operatorUserId)
        {
            // 权限校验
            if (!await IsUserAdminAsync(groupId, operatorUserId))
                throw new Exception("无权限，只有管理员或群主可以删除群组");

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

                    var deleteGroupMessageCommand = new MySqlCommand(
                        "DELETE FROM GroupMessages WHERE GroupId = @GroupId",
                        connection);
                    deleteGroupMessageCommand.Parameters.AddWithValue("@GroupId", groupId);
                    await deleteGroupMessageCommand.ExecuteNonQueryAsync();

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
                // 获取群组基本信息
                var group = await GetGroupAsync(groupId);
                if (group == null)
                    return null;

                // 获取群成员列表
                var members = await GetGroupUsersAsync(groupId);

                // 组装 GroupDetailDTO
                var detail = new GroupDetailDTO
                {
                    GroupId = group.GroupId,
                    GroupName = group.GroupName,
                    Description = group.Description,
                    CreatorId = group.CreatorId,
                    MemberCount = group.MemberCount,
                    Members = members
                };
                return detail;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取群组详情失败: {ex.Message}");
            }
        }


        public async Task<GroupDTO> GetGroupAsync(int groupId)
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
                            return new GroupDTO
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
                throw new Exception($"获取群组失败: {ex.Message}");
            }
            return null;
        }



        public async Task<bool> AddUserToGroupAsync(int groupId, int userId, int operatorUserId)
        {
            // 权限校验
            if (!await IsUserAdminAsync(groupId, operatorUserId))
                throw new Exception("无权限，只有管理员或群主可以添加成员");

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 检查用户是否已经在群组中
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

                    // 插入新成员
                    var addMemberCommand = new MySqlCommand(
                        "INSERT INTO GroupMembers (MemberId, GroupId, UserId, JoinTime) VALUES (@MemberId, @GroupId, @UserId, @JoinTime)",
                        connection);
                    addMemberCommand.Parameters.AddWithValue("@MemberId", groupId * 10000 + userId);
                    addMemberCommand.Parameters.AddWithValue("@GroupId", groupId);
                    addMemberCommand.Parameters.AddWithValue("@UserId", userId);
                    addMemberCommand.Parameters.AddWithValue("@JoinTime", DateTime.UtcNow);
                    var rowsAffected = await addMemberCommand.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        // 更新群成员数
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

        public async Task<bool> AddUserToGroupByUserNameAsync(int groupId, string userName, int operatorUserId)
        {
            // 权限校验
            if (!await IsUserAdminAsync(groupId, operatorUserId))
                throw new Exception("无权限，只有管理员或群主可以添加成员");

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 查询 userId
                    var getUserIdCommand = new MySqlCommand(
                        "SELECT UserId FROM Users WHERE Username = @UserName",
                        connection);
                    getUserIdCommand.Parameters.AddWithValue("@UserName", userName);

                    var userId = Convert.ToInt32(await getUserIdCommand.ExecuteScalarAsync());
                    if (userId == 0)
                    {
                        throw new Exception($"用户名 {userName} 不存在");
                    }

                    // 传递 operatorUserId
                    return await AddUserToGroupAsync(groupId, userId, operatorUserId);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"通过用户名添加用户到群组失败: {ex.Message}");
            }
        }


        public async Task<bool> RemoveUserFromGroupAsync(int groupId, int userId, int operatorUserId)
        {
            // 权限校验
            if (!await IsUserAdminAsync(groupId, operatorUserId))
                throw new Exception("无权限，只有管理员或群主可以移除成员");

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var removeMemberCommand = new MySqlCommand(
                        "DELETE FROM GroupMembers WHERE GroupId = @GroupId AND UserId = @UserId",
                        connection);
                    removeMemberCommand.Parameters.AddWithValue("@GroupId", groupId);
                    removeMemberCommand.Parameters.AddWithValue("@UserId", userId);
                    var rowsAffected = await removeMemberCommand.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
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

        public async Task<List<GroupMemberDTO>> GetGroupUsersAsync(int groupId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new MySqlCommand(
                        @"SELECT u.UserId, u.Username, u.Avatar, u.LastLoginTime, gm.Role, gm.JoinTime
                  FROM GroupMembers gm
                  JOIN Users u ON gm.UserId = u.UserId
                  WHERE gm.GroupId = @GroupId",
                        connection);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    var users = new List<GroupMemberDTO>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new GroupMemberDTO
                            {
                                UserId = reader.GetInt32(0),
                                Username = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Avatar = reader.IsDBNull(2) ? null : reader.GetString(2),
                                LastActive = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                                Role = reader.IsDBNull(4) ? null : reader.GetString(4),
                                JoinTime = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5),
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


        public async Task<int> SaveGroupMessageAsync(int groupId, int userId, string message)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new MySqlCommand(
                        "INSERT INTO GroupMessages (GroupId, SenderId, Content, CreateTime,MessageType) VALUES (@GroupId, @SenderId, @Content, @CreateTIme,@MessageType); SELECT LAST_INSERT_ID();",
                        connection);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    command.Parameters.AddWithValue("@SenderId", userId);
                    command.Parameters.AddWithValue("@Content", message);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@MessageType","text");
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

        public async Task<bool> IsUserAdminAsync(int groupId, int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 查询 GroupMembers 表中的 Role 列
                    var command = new MySqlCommand(
                        "SELECT Role FROM GroupMembers WHERE GroupId = @GroupId AND UserId = @UserId",
                        connection);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    command.Parameters.AddWithValue("@UserId", userId);

                    var role = await command.ExecuteScalarAsync() as string;

                    // 如果角色是 "master" 或 "admin"，返回 true
                    return role == "master" || role == "admin";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"验证用户管理员权限失败: {ex.Message}");
            }
        }
        public async Task<string> ToggleAdminRoleAsync(int groupId, int userId, int operatorUserId)
        {
            // 权限校验
            if (!await IsUserAdminAsync(groupId, operatorUserId))
                return "无权限，只有管理员或群主可以切换角色";

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var getRoleCommand = new MySqlCommand(
                        "SELECT Role FROM GroupMembers WHERE GroupId = @GroupId AND UserId = @UserId",
                        connection);
                    getRoleCommand.Parameters.AddWithValue("@GroupId", groupId);
                    getRoleCommand.Parameters.AddWithValue("@UserId", userId);

                    var role = await getRoleCommand.ExecuteScalarAsync() as string;

                    if (role == "creator")
                    {
                        return "群主不能卸任";
                    }

                    string newRole = role == "admin" ? "member" : "admin";

                    var updateRoleCommand = new MySqlCommand(
                        "UPDATE GroupMembers SET Role = @NewRole WHERE GroupId = @GroupId AND UserId = @UserId",
                        connection);
                    updateRoleCommand.Parameters.AddWithValue("@NewRole", newRole);
                    updateRoleCommand.Parameters.AddWithValue("@GroupId", groupId);
                    updateRoleCommand.Parameters.AddWithValue("@UserId", userId);

                    var rowsAffected = await updateRoleCommand.ExecuteNonQueryAsync();

                    return rowsAffected > 0 ? "已切换" : "切换失败";
                }
            }
            catch (Exception ex)
            {
                return $"切换管理员角色失败: {ex.Message}";
            }
        }




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


        //以下的代码是曾经的好友系统使用的，在确定前展示不删除





        public async Task<bool> AddFriendAsync(int user1Id, int user2Id)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var checkFriendCommand = new MySqlCommand(
                        "SELECT COUNT(1) FROM friendships WHERE (UserId = @UserId1 AND FriendId = @UserId2) OR (UserId = @UserId2 AND FriendId = @UserId1)",
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
                            var now = DateTime.UtcNow;
                            var addFriendCommand1 = new MySqlCommand(
                                "INSERT INTO friendships (UserId, FriendId, Status, CreateTime, UpdateTime) VALUES (@UserId, @FriendId, 'accepted', @CreateTime, @UpdateTime)",
                                connection, transaction);
                            addFriendCommand1.Parameters.AddWithValue("@UserId", user1Id);
                            addFriendCommand1.Parameters.AddWithValue("@FriendId", user2Id);
                            addFriendCommand1.Parameters.AddWithValue("@CreateTime", now);
                            addFriendCommand1.Parameters.AddWithValue("@UpdateTime", now);
                            await addFriendCommand1.ExecuteNonQueryAsync();

                            var addFriendCommand2 = new MySqlCommand(
                                "INSERT INTO friendships (UserId, FriendId, Status, CreateTime, UpdateTime) VALUES (@UserId, @FriendId, 'accepted', @CreateTime, @UpdateTime)",
                                connection, transaction);
                            addFriendCommand2.Parameters.AddWithValue("@UserId", user2Id);
                            addFriendCommand2.Parameters.AddWithValue("@FriendId", user1Id);
                            addFriendCommand2.Parameters.AddWithValue("@CreateTime", now);
                            addFriendCommand2.Parameters.AddWithValue("@UpdateTime", now);
                            await addFriendCommand2.ExecuteNonQueryAsync();

                            // 更新通知状态
                            var updateNotificationCommand = new MySqlCommand(
                                "UPDATE notifications SET Status = 'accepted' WHERE Type = 'friend_request' AND SenderId = @SenderId AND ReceiverId = @ReceiverId",
                                connection, transaction);
                            updateNotificationCommand.Parameters.AddWithValue("@SenderId", user1Id);
                            updateNotificationCommand.Parameters.AddWithValue("@ReceiverId", user2Id);
                            await updateNotificationCommand.ExecuteNonQueryAsync();

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
                        // 删除双向好友关系
                        var deleteFriendshipCommand = new MySqlCommand(
                            "DELETE FROM friendships WHERE (UserId = @UserId1 AND FriendId = @UserId2) OR (UserId = @UserId2 AND FriendId = @UserId1)",
                            connection, transaction);
                        deleteFriendshipCommand.Parameters.AddWithValue("@UserId1", userId1);
                        deleteFriendshipCommand.Parameters.AddWithValue("@UserId2", userId2);
                        await deleteFriendshipCommand.ExecuteNonQueryAsync();

                        await transaction.CommitAsync();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除好友失败: {ex.Message}");
            }
        }

        public async Task<List<FriendshipDTO>> GetFriendsAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand(
                        @"SELECT 
                            f.FriendshipId,
                            f.UserId,
                            f.FriendId,
                            f.Status,
                            f.CreateTime,
                            f.UpdateTime,
                            u.Username
                          FROM friendships f
                          JOIN Users u ON u.UserId = f.FriendId
                          WHERE f.UserId = @UserId AND f.Status = 'accepted'",
                        connection);
                    command.Parameters.AddWithValue("@UserId", userId);

                    var friends = new List<FriendshipDTO>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            friends.Add(new FriendshipDTO
                            {
                                FriendshipId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                FriendId = reader.GetInt32(2),
                                Status = reader.GetString(3),
                                CreateTime = reader.GetDateTime(4),
                                UpdateTime = reader.GetDateTime(5),
                                Username = reader.GetString(6)
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

        public async Task<FriendshipDTO> GetFriendByIdAsync(int userId, int friendId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand(
                        @"SELECT 
                            f.FriendshipId,
                            f.UserId,
                            f.FriendId,
                            f.Status,
                            f.CreateTime,
                            f.UpdateTime,
                            u.Username
                          FROM friendships f
                          JOIN Users u ON u.UserId = f.FriendId
                          WHERE f.UserId = @UserId AND f.FriendId = @FriendId",
                        connection);

                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@FriendId", friendId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new FriendshipDTO
                            {
                                FriendshipId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                FriendId = reader.GetInt32(2),
                                Status = reader.GetString(3),
                                CreateTime = reader.GetDateTime(4),
                                UpdateTime = reader.GetDateTime(5),
                                Username = reader.GetString(6)
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
        public async Task<List<GroupDTO>> GetPrivateGroupBetweenUsersAsync(int userId1, int userId2)
        {
            var result = new List<GroupDTO>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 查询两个用户共同所在的群组
                var command = new MySqlCommand(@"
                    SELECT g.* 
                    FROM Groups g
                    INNER JOIN GroupMembers gm1 ON g.GroupId = gm1.GroupId
                    INNER JOIN GroupMembers gm2 ON g.GroupId = gm2.GroupId
                    WHERE gm1.UserId = @UserId1 
                    AND gm2.UserId = @UserId2
                    AND g.Description = '私聊'
                    AND g.MemberCount = 2", connection);
                
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
        }


        public bool IsUserOnline(int userId)
        {
            return _globalOnlineUsers.ContainsKey(userId) && _globalOnlineUsers[userId];
        }

        public void SetUserOnline(int userId, bool isOnline)
        {
            if (isOnline)
            {
                _globalOnlineUsers.AddOrUpdate(userId, true, (_, __) => true);
            }
            else
            {
                _globalOnlineUsers.TryRemove(userId, out _);
            }
        }
        
        public List<int> GetOnlineUsers()
        {
            return _globalOnlineUsers.Where(u => u.Value).Select(u => u.Key).ToList();
        }
    }
}
