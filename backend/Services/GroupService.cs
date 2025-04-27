using backend.DTOs;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;

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
                            // 插入 ChatGroups 表
                            var insertGroupCommand = new MySqlCommand(
                                @"INSERT INTO ChatGroups (GroupId, GroupName, Description, CreatorId, MemberCount, CreateTime) 
                                  VALUES (@GroupId, @GroupName, @Description, @CreatorId, @MemberCount, @CreateTime); 
                                  SELECT LAST_INSERT_ID();",
                                connection, transaction);

                            insertGroupCommand.Parameters.AddWithValue("@GroupId", groupRegDto.GroupId);
                            insertGroupCommand.Parameters.AddWithValue("@GroupName", groupRegDto.GroupName);
                            insertGroupCommand.Parameters.AddWithValue("@Description", groupRegDto.Description);
                            insertGroupCommand.Parameters.AddWithValue("@CreatorId", groupRegDto.CreatorId);
                            insertGroupCommand.Parameters.AddWithValue("@MemberCount", groupRegDto.MemberCount);
                            insertGroupCommand.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);

                            var groupId = Convert.ToInt32(await insertGroupCommand.ExecuteScalarAsync());

                            // 插入 GroupMembers 表
                            var insertMemberCommand = new MySqlCommand(
                                @"INSERT INTO GroupMembers (MemberId, GroupId, UserId, JoinTime) 
                                  VALUES (@MemberId, @GroupId, @UserId, @JoinTime)",
                                connection, transaction);

                            insertMemberCommand.Parameters.AddWithValue("@MemberId", $"{groupRegDto.GroupId}+{groupRegDto.CreatorId}");
                            insertMemberCommand.Parameters.AddWithValue("@GroupId", groupRegDto.GroupId);
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

                    // Step 2: 从 ChatGroups 表中查询这些 GroupId 对应的 GroupDTO
                    var getGroupsCommand = new MySqlCommand(
                        $"SELECT GroupId, GroupName, UpdateTime FROM ChatGroups WHERE GroupId IN ({string.Join(",", groupIds)})",
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
                                UpdateTime = reader.GetDateTime(2)
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
                        "SELECT GroupId, GroupName, UpdateTime FROM ChatGroups WHERE GroupId = @GroupId",
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
                        "INSERT INTO GroupMembers (MemberId, GroupId, UserId, JoinTime) VALUES (@MemberId, @GroupId, @UserId, @JoinTime)",
                        connection);
                    addMemberCommand.Parameters.AddWithValue("@GroupId", groupId);
                    addMemberCommand.Parameters.AddWithValue("@UserId", userId);
                    addMemberCommand.Parameters.AddWithValue("@MemberId", groupId.ToString() + "+" + userId.ToString());
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
                        "SELECT UserId, Username,LastLoginTime FROM Users WHERE UserId IN (SELECT UserId FROM GroupMembers WHERE GroupId = @GroupId)",
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


        private async Task<bool> CheckGroupExistsInternalAsync(string groupName)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new MySqlCommand(
                    "SELECT COUNT(1) FROM ChatGroups WHERE GroupName = @GroupName",
                    connection);
                command.Parameters.AddWithValue("@Username", groupName);

                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
        }
    }
}
