using backend.DTOs;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly string _connectionString;
        private readonly IUserService _userService;

        public FriendshipService(IConfiguration configuration, IUserService userService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _userService = userService;
        }

        public async Task<bool> CreateFriendRequestAsync(int userId, int friendId)
        {
            // 检查是否已经存在好友关系
            var status = await GetFriendshipStatusAsync(userId, friendId);
            if (!string.IsNullOrEmpty(status) && status != "rejected")
            {
                return false; // 已经存在好友关系或请求
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 如果之前被拒绝过，更新状态为pending
                if (status == "rejected")
                {
                    var updateCommand = new MySqlCommand(
                        "UPDATE Friendships SET Status = 'pending', UpdateTime = @UpdateTime WHERE UserId = @UserId AND FriendId = @FriendId",
                        connection);
                    updateCommand.Parameters.AddWithValue("@UserId", userId);
                    updateCommand.Parameters.AddWithValue("@FriendId", friendId);
                    updateCommand.Parameters.AddWithValue("@UpdateTime", DateTime.UtcNow);
                    
                    await updateCommand.ExecuteNonQueryAsync();
                    return true;
                }
                
                // 创建新的好友请求
                var command = new MySqlCommand(
                    "INSERT INTO Friendships (UserId, FriendId, Status, CreateTime, UpdateTime) VALUES (@UserId, @FriendId, 'pending', @CreateTime, @UpdateTime)",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@FriendId", friendId);
                command.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);
                command.Parameters.AddWithValue("@UpdateTime", DateTime.UtcNow);
                
                await command.ExecuteNonQueryAsync();
                return true;
            }
        }

        public async Task<bool> AcceptFriendRequestAsync(int userId, int friendId)
        {
            // 检查是否存在待处理的好友请求
            var status = await GetFriendshipStatusAsync(friendId, userId);
            if (string.IsNullOrEmpty(status) || status != "pending")
            {
                return false; // 不存在待处理的好友请求
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 开启事务
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 更新请求状态为accepted
                        var updateCommand = new MySqlCommand(
                            "UPDATE Friendships SET Status = 'accepted', UpdateTime = @UpdateTime WHERE UserId = @RequesterId AND FriendId = @ReceiverId",
                            connection, transaction as MySqlTransaction);
                        updateCommand.Parameters.AddWithValue("@RequesterId", friendId);
                        updateCommand.Parameters.AddWithValue("@ReceiverId", userId);
                        updateCommand.Parameters.AddWithValue("@UpdateTime", DateTime.UtcNow);
                        
                        await updateCommand.ExecuteNonQueryAsync();
                        
                        // 检查是否已经存在反向关系
                        var checkCommand = new MySqlCommand(
                            "SELECT COUNT(*) FROM Friendships WHERE UserId = @UserId AND FriendId = @FriendId",
                            connection, transaction as MySqlTransaction);
                        checkCommand.Parameters.AddWithValue("@UserId", userId);
                        checkCommand.Parameters.AddWithValue("@FriendId", friendId);
                        
                        var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                        
                        if (count == 0)
                        {
                            // 创建反向关系
                            var insertCommand = new MySqlCommand(
                                "INSERT INTO Friendships (UserId, FriendId, Status, CreateTime, UpdateTime) VALUES (@UserId, @FriendId, 'accepted', @CreateTime, @UpdateTime)",
                                connection, transaction as MySqlTransaction);
                            insertCommand.Parameters.AddWithValue("@UserId", userId);
                            insertCommand.Parameters.AddWithValue("@FriendId", friendId);
                            insertCommand.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);
                            insertCommand.Parameters.AddWithValue("@UpdateTime", DateTime.UtcNow);
                            
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                        
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> RejectFriendRequestAsync(int userId, int friendId)
        {
            // 检查是否存在待处理的好友请求
            var status = await GetFriendshipStatusAsync(friendId, userId);
            if (string.IsNullOrEmpty(status) || status != "pending")
            {
                return false; // 不存在待处理的好友请求
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 更新请求状态为rejected
                var command = new MySqlCommand(
                    "UPDATE Friendships SET Status = 'rejected', UpdateTime = @UpdateTime WHERE UserId = @RequesterId AND FriendId = @ReceiverId",
                    connection);
                command.Parameters.AddWithValue("@RequesterId", friendId);
                command.Parameters.AddWithValue("@ReceiverId", userId);
                command.Parameters.AddWithValue("@UpdateTime", DateTime.UtcNow);
                
                await command.ExecuteNonQueryAsync();
                return true;
            }
        }

        public async Task<bool> DeleteFriendshipAsync(int userId, int friendId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 开启事务
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 删除用户到好友的关系
                        var command1 = new MySqlCommand(
                            "DELETE FROM Friendships WHERE UserId = @UserId AND FriendId = @FriendId",
                            connection, transaction as MySqlTransaction);
                        command1.Parameters.AddWithValue("@UserId", userId);
                        command1.Parameters.AddWithValue("@FriendId", friendId);
                        
                        await command1.ExecuteNonQueryAsync();
                        
                        // 删除好友到用户的关系
                        var command2 = new MySqlCommand(
                            "DELETE FROM Friendships WHERE UserId = @FriendId AND FriendId = @UserId",
                            connection, transaction as MySqlTransaction);
                        command2.Parameters.AddWithValue("@UserId", userId);
                        command2.Parameters.AddWithValue("@FriendId", friendId);
                        
                        await command2.ExecuteNonQueryAsync();
                        
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<List<UserDTO>> GetUserFriendsAsync(int userId)
        {
            var friends = new List<UserDTO>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var command = new MySqlCommand(
                    @"SELECT u.UserId, u.Username, u.Avatar, u.Signature, u.Status, u.LastLoginTime 
                      FROM Friendships f 
                      JOIN Users u ON f.FriendId = u.UserId 
                      WHERE f.UserId = @UserId AND f.Status = 'accepted'",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        friends.Add(new UserDTO
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            AvatarUrl = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Signature = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Status = reader.IsDBNull(4) ? "offline" : reader.GetString(4),
                            LastActive = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5)
                        });
                    }
                }
            }
            
            return friends;
        }

        public async Task<bool> CheckIsFriendAsync(int userId, int friendId)
        {
            var status = await GetFriendshipStatusAsync(userId, friendId);
            return status == "accepted";
        }

        public async Task<string> GetFriendshipStatusAsync(int userId, int friendId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var command = new MySqlCommand(
                    "SELECT Status FROM Friendships WHERE UserId = @UserId AND FriendId = @FriendId",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@FriendId", friendId);
                
                var result = await command.ExecuteScalarAsync();
                
                return result != null ? result.ToString() : null;
            }
        }

        public async Task<bool> BlockFriendAsync(int userId, int friendId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 检查是否已经存在好友关系
                var checkCommand = new MySqlCommand(
                    "SELECT COUNT(*) FROM Friendships WHERE UserId = @UserId AND FriendId = @FriendId",
                    connection);
                checkCommand.Parameters.AddWithValue("@UserId", userId);
                checkCommand.Parameters.AddWithValue("@FriendId", friendId);
                
                var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                
                if (count > 0)
                {
                    // 更新状态为blocked
                    var updateCommand = new MySqlCommand(
                        "UPDATE Friendships SET Status = 'blocked', UpdateTime = @UpdateTime WHERE UserId = @UserId AND FriendId = @FriendId",
                        connection);
                    updateCommand.Parameters.AddWithValue("@UserId", userId);
                    updateCommand.Parameters.AddWithValue("@FriendId", friendId);
                    updateCommand.Parameters.AddWithValue("@UpdateTime", DateTime.UtcNow);
                    
                    await updateCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // 创建新的blocked记录
                    var insertCommand = new MySqlCommand(
                        "INSERT INTO Friendships (UserId, FriendId, Status, CreateTime, UpdateTime) VALUES (@UserId, @FriendId, 'blocked', @CreateTime, @UpdateTime)",
                        connection);
                    insertCommand.Parameters.AddWithValue("@UserId", userId);
                    insertCommand.Parameters.AddWithValue("@FriendId", friendId);
                    insertCommand.Parameters.AddWithValue("@CreateTime", DateTime.UtcNow);
                    insertCommand.Parameters.AddWithValue("@UpdateTime", DateTime.UtcNow);
                    
                    await insertCommand.ExecuteNonQueryAsync();
                }
                
                return true;
            }
        }
    }
} 