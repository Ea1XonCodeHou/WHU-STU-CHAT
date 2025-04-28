using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using backend.DTOs;
using backend.Models;
using backend.Utils;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace backend.Services
{
    /// <summary>
    /// 用户服务实现
    /// </summary>
    public class UserService : IUserService
    {
        private readonly string _connectionString;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置</param>
        public UserService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="loginDto">登录信息</param>
        /// <returns>登录结果</returns>
        public async Task<Result<LoginResultVO>> LoginAsync(LoginDTO loginDto)
        {
            try
            {
                // 用于存储用户信息的变量
                int userId = 0;
                string username = string.Empty;
                string email = null;
                string phone = null;
                string avatar = null;
                bool passwordIsValid = false;

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 根据用户名查询用户
                    var command = new MySqlCommand(
                        "SELECT UserId, Username, Password, Email, Phone, Avatar FROM Users WHERE Username = @Username",
                        connection);
                    command.Parameters.AddWithValue("@Username", loginDto.Username);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // 获取用户信息
                            userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                            username = reader.GetString(reader.GetOrdinal("Username"));
                            
                            // 验证密码
                            var storedPassword = reader.GetString(reader.GetOrdinal("Password"));
                            passwordIsValid = VerifyPassword(loginDto.Password, storedPassword);

                            if (!passwordIsValid)
                            {
                                return Result<LoginResultVO>.Error("用户名或密码错误", 400);
                            }

                            email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email"));
                            phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone"));
                            avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader.GetString(reader.GetOrdinal("Avatar"));
                        }
                        else
                        {
                            return Result<LoginResultVO>.Error("用户名或密码错误", 400);
                        }
                    }
                    
                    // DataReader已关闭，现在可以安全地执行下一个命令
                    if (passwordIsValid)
                    {
                        // 更新最后登录时间
                        var updateCommand = new MySqlCommand(
                            "UPDATE Users SET LastLoginTime = @LastLoginTime WHERE UserId = @UserId",
                            connection);
                        updateCommand.Parameters.AddWithValue("@LastLoginTime", DateTime.Now);
                        updateCommand.Parameters.AddWithValue("@UserId", userId);
                        await updateCommand.ExecuteNonQueryAsync();
                        
                        // 生成令牌
                        var token = GenerateToken(userId, username);
                        var expireTime = DateTime.Now.AddDays(loginDto.RememberMe ? 7 : 1);

                        var result = new LoginResultVO
                        {
                            UserInfo = new UserVO
                            {
                                Id = userId,
                                Username = username,
                                Email = email,
                                Phone = phone,
                                Avatar = avatar
                            },
                            Token = token,
                            ExpireTime = expireTime
                        };

                        return Result<LoginResultVO>.Success(result, "登录成功");
                    }
                }
                
                // 下面代码实际上不会执行到，因为上面已经做了密码验证，但为了代码完备性保留
                return Result<LoginResultVO>.Error("用户名或密码错误", 400);
            }
            catch (Exception ex)
            {
                return Result<LoginResultVO>.Error($"登录失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="registerDto">注册信息</param>
        /// <returns>注册结果</returns>
        public async Task<Result<UserVO>> RegisterAsync(RegisterDTO registerDto)
        {
            try
            {
                // 检查用户名是否已存在
                var usernameExists = await CheckUsernameExistsInternalAsync(registerDto.Username);
                if (usernameExists)
                {
                    return Result<UserVO>.Error("用户名已存在", 400);
                }

                // 加密密码
                var encryptedPassword = EncryptPassword(registerDto.Password);

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 创建新用户记录
                    var command = new MySqlCommand(
                        @"INSERT INTO Users (Username, Password, Email, Phone, CreateTime, UpdateTime) 
                          VALUES (@Username, @Password, @Email, @Phone, @CreateTime, @UpdateTime);
                          SELECT LAST_INSERT_ID();",
                        connection);

                    command.Parameters.AddWithValue("@Username", registerDto.Username);
                    command.Parameters.AddWithValue("@Password", encryptedPassword);
                    command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(registerDto.Email) ? DBNull.Value : (object)registerDto.Email);
                    command.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(registerDto.Phone) ? DBNull.Value : (object)registerDto.Phone);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    // 执行SQL并获取新插入的用户ID
                    var userId = Convert.ToInt32(await command.ExecuteScalarAsync());

                    // 创建用户信息
                    var userVo = new UserVO
                    {
                        Id = userId,
                        Username = registerDto.Username,
                        Email = registerDto.Email,
                        Phone = registerDto.Phone,
                        Avatar = null // 默认为null，可设置默认头像
                    };

                    return Result<UserVO>.Success(userVo, "注册成功");
                }
            }
            catch (Exception ex)
            {
                return Result<UserVO>.Error($"注册失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 验证用户名是否存在
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>验证结果</returns>
        public async Task<Result<bool>> CheckUsernameExistsAsync(string username)
        {
            try
            {
                var exists = await CheckUsernameExistsInternalAsync(username);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return Result<bool>.Error($"检查用户名失败：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户信息</returns>
        public async Task<Result<UserVO>> GetUserInfoAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand(
                        "SELECT UserId, Username, Email, Phone, Avatar FROM Users WHERE UserId = @UserId",
                        connection);
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var userVo = new UserVO
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("UserId")),
                                Username = reader.GetString(reader.GetOrdinal("Username")),
                                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                                Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader.GetString(reader.GetOrdinal("Avatar"))
                            };

                            return Result<UserVO>.Success(userVo);
                        }
                        else
                        {
                            return Result<UserVO>.Error("用户不存在", 404);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<UserVO>.Error($"获取用户信息失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 内部方法：检查用户名是否存在
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户名是否存在</returns>
        private async Task<bool> CheckUsernameExistsInternalAsync(string username)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new MySqlCommand(
                    "SELECT COUNT(1) FROM Users WHERE Username = @Username",
                    connection);
                command.Parameters.AddWithValue("@Username", username);

                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
        }

        /// <summary>
        /// 密码加密
        /// </summary>
        /// <param name="password">原始密码</param>
        /// <returns>加密后的密码</returns>
        private string EncryptPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// 验证密码
        /// </summary>
        /// <param name="inputPassword">输入的密码</param>
        /// <param name="storedPassword">存储的加密密码</param>
        /// <returns>密码是否正确</returns>
        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            var encryptedInput = EncryptPassword(inputPassword);
            return encryptedInput == storedPassword;
        }

        /// <summary>
        /// 生成令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="username">用户名</param>
        /// <returns>令牌字符串</returns>
        private string GenerateToken(int userId, string username)
        {
            // 简单实现，实际项目应使用JWT等安全的令牌生成方式
            var tokenData = $"{userId}:{username}:{DateTime.Now.Ticks}";
            using (var sha256 = SHA256.Create())
            {
                var tokenBytes = Encoding.UTF8.GetBytes(tokenData);
                var hash = sha256.ComputeHash(tokenBytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
} 