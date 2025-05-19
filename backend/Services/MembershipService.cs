using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using backend.DTOs;
using backend.Models;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace backend.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly string _connectionString;
        
        public MembershipService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        
        public async Task<MemberSubscriptionDTO> CreateSubscriptionAsync(int userId, int level, string paymentMethod)
        {
            // 检查参数有效性
            if (level != 1 && level != 2)
                throw new ArgumentException("会员等级必须为1(VIP)或2(SVIP)");
                
            if (string.IsNullOrEmpty(paymentMethod))
                throw new ArgumentException("支付方式不能为空");
                
            // 确定价格
            decimal price = level == 1 ? 9.9M : 19.9M;
            
            // 设置开始和结束日期（默认30天）
            var startDate = DateTime.Now;
            var endDate = startDate.AddDays(30);
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 开始事务
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. 更新用户的会员等级
                        string updateUserSql = "UPDATE users SET Level = @Level WHERE UserId = @UserId";
                        using (var command = new MySqlCommand(updateUserSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Level", level);
                            command.Parameters.AddWithValue("@UserId", userId);
                            await command.ExecuteNonQueryAsync();
                        }
                        
                        // 2. 插入订阅记录
                        string insertSubscriptionSql = @"
                            INSERT INTO member_subscriptions 
                            (UserId, Level, StartDate, EndDate, PaymentAmount, PaymentMethod, Status, CreateTime, UpdateTime) 
                            VALUES 
                            (@UserId, @Level, @StartDate, @EndDate, @PaymentAmount, @PaymentMethod, 'active', NOW(), NOW());
                            SELECT LAST_INSERT_ID();";
                            
                        int subscriptionId;
                        using (var command = new MySqlCommand(insertSubscriptionSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            command.Parameters.AddWithValue("@Level", level);
                            command.Parameters.AddWithValue("@StartDate", startDate);
                            command.Parameters.AddWithValue("@EndDate", endDate);
                            command.Parameters.AddWithValue("@PaymentAmount", price);
                            command.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                            subscriptionId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }
                        
                        // 提交事务
                        await transaction.CommitAsync();
                        
                        // 返回创建的订阅信息
                        return new MemberSubscriptionDTO
                        {
                            SubscriptionId = subscriptionId,
                            UserId = userId,
                            Level = level,
                            StartDate = startDate,
                            EndDate = endDate,
                            PaymentAmount = price,
                            PaymentMethod = paymentMethod,
                            Status = "active",
                            CreateTime = DateTime.Now
                        };
                    }
                    catch
                    {
                        // 出错回滚事务
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
        
        public async Task<MemberSubscriptionDTO> GetCurrentSubscriptionAsync(int userId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string sql = @"
                    SELECT * FROM member_subscriptions 
                    WHERE UserId = @UserId AND Status = 'active' AND EndDate >= NOW()
                    ORDER BY EndDate DESC
                    LIMIT 1";
                    
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new MemberSubscriptionDTO
                            {
                                SubscriptionId = reader.GetInt32(reader.GetOrdinal("SubscriptionId")),
                                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                Level = reader.GetInt32(reader.GetOrdinal("Level")),
                                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                                EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                                PaymentAmount = reader.GetDecimal(reader.GetOrdinal("PaymentAmount")),
                                PaymentMethod = reader.GetString(reader.GetOrdinal("PaymentMethod")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime"))
                            };
                        }
                    }
                }
                
                return null; // 没有找到有效的订阅
            }
        }
        
        public async Task<bool> CancelSubscriptionAsync(int subscriptionId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string sql = "UPDATE member_subscriptions SET Status = 'cancelled', UpdateTime = NOW() WHERE SubscriptionId = @SubscriptionId AND Status = 'active'";
                
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@SubscriptionId", subscriptionId);
                    
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        
        public async Task<int> ProcessExpiredSubscriptionsAsync()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 开始事务
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. 查找过期的订阅
                        string findExpiredSql = @"
                            SELECT UserId FROM member_subscriptions 
                            WHERE Status = 'active' AND EndDate < NOW()";
                            
                        var expiredUserIds = new List<int>();
                        using (var command = new MySqlCommand(findExpiredSql, connection, transaction))
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    expiredUserIds.Add(reader.GetInt32(reader.GetOrdinal("UserId")));
                                }
                            }
                        }
                        
                        // 2. 更新订阅状态为过期
                        string updateSubscriptionSql = @"
                            UPDATE member_subscriptions 
                            SET Status = 'expired', UpdateTime = NOW() 
                            WHERE Status = 'active' AND EndDate < NOW()";
                            
                        int updatedSubscriptions;
                        using (var command = new MySqlCommand(updateSubscriptionSql, connection, transaction))
                        {
                            updatedSubscriptions = await command.ExecuteNonQueryAsync();
                        }
                        
                        // 3. 如果没有其他有效订阅，将用户等级重置为0
                        foreach (var userId in expiredUserIds)
                        {
                            // 检查用户是否还有其他有效订阅
                            string checkActiveSql = @"
                                SELECT COUNT(*) FROM member_subscriptions 
                                WHERE UserId = @UserId AND Status = 'active' AND EndDate >= NOW()";
                                
                            bool hasActiveSubscription;
                            using (var command = new MySqlCommand(checkActiveSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                hasActiveSubscription = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                            }
                            
                            // 如果没有其他有效订阅，将用户等级重置为0
                            if (!hasActiveSubscription)
                            {
                                string resetUserLevelSql = "UPDATE users SET Level = 0 WHERE UserId = @UserId";
                                using (var command = new MySqlCommand(resetUserLevelSql, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@UserId", userId);
                                    await command.ExecuteNonQueryAsync();
                                }
                            }
                        }
                        
                        // 提交事务
                        await transaction.CommitAsync();
                        
                        return updatedSubscriptions;
                    }
                    catch
                    {
                        // 出错回滚事务
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
} 