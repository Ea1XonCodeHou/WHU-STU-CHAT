using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using backend.DTOs;
using backend.Models;
using backend.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Services
{
    /// <summary>
    /// 用户服务实现
    /// </summary>
    public class UserService : IUserService
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <param name="webHostEnvironment">Web环境</param>
        /// <param name="serviceScopeFactory">服务范围工厂</param>
        public UserService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment, IServiceScopeFactory serviceScopeFactory)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _webHostEnvironment = webHostEnvironment;
            _serviceScopeFactory = serviceScopeFactory;
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

                        return Result<LoginResultVO>.SuccessResult(result, "登录成功");
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

                    return Result<UserVO>.SuccessResult(userVo, "注册成功");
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
                return Result<bool>.SuccessResult(exists);
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

                    // 检查Status字段是否存在
                    bool statusFieldExists = false;
                    try
                    {
                        var schemaCommand = new MySqlCommand(
                            "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                            "WHERE TABLE_SCHEMA = 'whu-chat' " +
                            "AND TABLE_NAME = 'Users' " +
                            "AND COLUMN_NAME = 'Status'",
                            connection);
                        var count = Convert.ToInt32(await schemaCommand.ExecuteScalarAsync());
                        statusFieldExists = count > 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"检查Status字段出错: {ex.Message}");
                    }

                    // 根据是否存在Status字段构建不同的SQL查询
                    string sql;
                    if (statusFieldExists)
                    {
                        sql = "SELECT UserId, Username, Email, Phone, Avatar, Signature, Status FROM Users WHERE UserId = @UserId";
                    }
                    else
                    {
                        sql = "SELECT UserId, Username, Email, Phone, Avatar, Signature FROM Users WHERE UserId = @UserId";
                    }

                    var command = new MySqlCommand(sql, connection);
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
                                Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader.GetString(reader.GetOrdinal("Avatar")),
                                Signature = reader.IsDBNull(reader.GetOrdinal("Signature")) ? null : reader.GetString(reader.GetOrdinal("Signature")),
                                Status = "offline" // 默认状态为离线
                            };

                            // 如果存在Status字段，则读取实际状态
                            if (statusFieldExists)
                            {
                                userVo.Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "offline" : reader.GetString(reader.GetOrdinal("Status"));
                            }

                            return Result<UserVO>.SuccessResult(userVo);
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
        public async Task<UserDTO> GetUserByUsernameAsync(string username)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "SELECT UserId, Username, Status, Avatar, Signature FROM Users WHERE Username = @Username",
                    connection);
                command.Parameters.AddWithValue("@Username", username);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new UserDTO
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Status = reader.IsDBNull(2) ? "offline" : reader.GetString(2),
                            AvatarUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Signature = reader.IsDBNull(4) ? null : reader.GetString(4)
                        };
                    }
                }
            }
            return null;
        }
         public async Task<UserDTO> GetUserByIdAsync(int userId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "SELECT UserId, Username, Status, Avatar, Signature FROM Users WHERE UserId = @UserId",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new UserDTO
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Status = reader.IsDBNull(2) ? "offline" : reader.GetString(2),
                            AvatarUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Signature = reader.IsDBNull(4) ? null : reader.GetString(4)
                        };
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="updateDto">更新信息</param>
        /// <returns>更新结果</returns>
        public async Task<Result<UserVO>> UpdateUserInfoAsync(UpdateUserDTO updateDto)
        {
            try
            {
                if (updateDto == null)
                {
                    return Result<UserVO>.Error("更新信息不能为空");
                }

                // 检查用户是否存在
                var userExists = await CheckUserExistsAsync(updateDto.UserId);
                if (!userExists)
                {
                    return Result<UserVO>.Error("用户不存在", 404);
                }

                // 如果更新用户名，需要检查用户名是否被占用
                if (!string.IsNullOrEmpty(updateDto.Username))
                {
                    var usernameExists = await CheckUsernameExistsInternalAsync(updateDto.Username);
                    // 如果用户名被占用且不是当前用户，则返回错误
                    if (usernameExists)
                    {
                        var existingUserId = await GetUserIdByUsernameAsync(updateDto.Username);
                        if (existingUserId != updateDto.UserId)
                        {
                            return Result<UserVO>.Error("用户名已被占用");
                        }
                    }
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 拼接SQL语句，只更新有值的字段
                    var sql = "UPDATE Users SET UpdateTime = @UpdateTime";
                    if (!string.IsNullOrEmpty(updateDto.Username))
                    {
                        sql += ", Username = @Username";
                    }
                    if (!string.IsNullOrEmpty(updateDto.Email))
                    {
                        sql += ", Email = @Email";
                    }
                    if (!string.IsNullOrEmpty(updateDto.Phone))
                    {
                        sql += ", Phone = @Phone";
                    }
                    // 如果需要添加个人签名字段，需要先在Users表中添加Signature字段
                    if (!string.IsNullOrEmpty(updateDto.Signature))
                    {
                        sql += ", Signature = @Signature";
                    }
                    sql += " WHERE UserId = @UserId";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", updateDto.UserId);
                        command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        if (!string.IsNullOrEmpty(updateDto.Username))
                        {
                            command.Parameters.AddWithValue("@Username", updateDto.Username);
                        }
                        if (!string.IsNullOrEmpty(updateDto.Email))
                        {
                            command.Parameters.AddWithValue("@Email", updateDto.Email);
                        }
                        if (!string.IsNullOrEmpty(updateDto.Phone))
                        {
                            command.Parameters.AddWithValue("@Phone", updateDto.Phone);
                        }
                        if (!string.IsNullOrEmpty(updateDto.Signature))
                        {
                            command.Parameters.AddWithValue("@Signature", updateDto.Signature);
                        }

                        // 执行更新
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected <= 0)
                        {
                            return Result<UserVO>.Error("更新用户信息失败");
                        }
                    }

                    // 获取更新后的用户信息
                    return await GetUserInfoAsync(updateDto.UserId);
                }
            }
            catch (Exception ex)
            {
                return Result<UserVO>.Error($"更新用户信息失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新用户头像
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="file">头像文件</param>
        /// <returns>更新结果</returns>
        public async Task<Result<string>> UpdateUserAvatarAsync(int userId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Result<string>.Error("请上传有效的头像文件");
                }

                // 检查用户是否存在
                var userExists = await CheckUserExistsAsync(userId);
                if (!userExists)
                {
                    return Result<string>.Error("用户不存在", 404);
                }

                // 检查文件类型
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                bool isValidExtension = Array.Exists(allowedExtensions, ext => ext == extension);
                if (!isValidExtension)
                {
                    return Result<string>.Error("不支持的文件类型，请上传jpg、jpeg、png或gif格式的图片");
                }

                // 检查文件大小限制（5MB）
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Result<string>.Error("头像图片大小不能超过5MB");
                }

                // 使用阿里云OSS上传头像
                using var serviceScope = _serviceScopeFactory.CreateScope();
                var ossHelper = serviceScope.ServiceProvider.GetRequiredService<AliOSSHelper>();

                // 生成唯一的对象名称
                var uniqueFileName = $"avatars/{userId}_{Guid.NewGuid()}{extension}";
                
                // 上传到阿里云OSS
                var avatarUrl = await ossHelper.UploadFileAsync(file, uniqueFileName);

                // 更新数据库中的头像路径
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new MySqlCommand(
                        "UPDATE Users SET Avatar = @Avatar, UpdateTime = @UpdateTime WHERE UserId = @UserId",
                        connection);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Avatar", avatarUrl);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected <= 0)
                    {
                        return Result<string>.Error("更新头像失败");
                    }
                }

                return Result<string>.SuccessResult(avatarUrl, "头像上传成功");
            }
            catch (Exception ex)
            {
                return Result<string>.Error($"更新头像失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 内部方法：检查用户是否存在
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户是否存在</returns>
        private async Task<bool> CheckUserExistsAsync(int userId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "SELECT COUNT(1) FROM Users WHERE UserId = @UserId",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
        }

        /// <summary>
        /// 内部方法：根据用户名获取用户ID
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户ID，如不存在则返回0</returns>
        private async Task<int> GetUserIdByUsernameAsync(string username)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(
                    "SELECT UserId FROM Users WHERE Username = @Username",
                    connection);
                command.Parameters.AddWithValue("@Username", username);
                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }
        
        /// <summary>
        /// 保存用户设置
        /// </summary>
        /// <param name="settingsDto">设置信息</param>
        /// <returns>保存结果</returns>
        public async Task<Result<bool>> SaveUserSettingAsync(UserSettingsDTO settingsDto)
        {
            try
            {
                if (settingsDto == null)
                {
                    return Result<bool>.Error("设置信息不能为空");
                }

                // 检查用户是否存在
                var userExists = await CheckUserExistsAsync(settingsDto.UserId);
                if (!userExists)
                {
                    return Result<bool>.Error("用户不存在", 404);
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 首先检查是否已存在该设置
                    var checkCommand = new MySqlCommand(
                        "SELECT COUNT(1) FROM UserSettings WHERE UserId = @UserId AND SettingKey = @SettingKey",
                        connection);
                    checkCommand.Parameters.AddWithValue("@UserId", settingsDto.UserId);
                    checkCommand.Parameters.AddWithValue("@SettingKey", settingsDto.SettingKey);
                    
                    var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    
                    // 根据是否存在决定执行更新还是插入
                    string sql;
                    if (count > 0)
                    {
                        sql = @"UPDATE UserSettings 
                               SET SettingValue = @SettingValue, UpdateTime = @UpdateTime 
                               WHERE UserId = @UserId AND SettingKey = @SettingKey";
                    }
                    else
                    {
                        sql = @"INSERT INTO UserSettings (UserId, SettingKey, SettingValue, CreateTime, UpdateTime) 
                               VALUES (@UserId, @SettingKey, @SettingValue, @CreateTime, @UpdateTime)";
                    }

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", settingsDto.UserId);
                        command.Parameters.AddWithValue("@SettingKey", settingsDto.SettingKey);
                        command.Parameters.AddWithValue("@SettingValue", settingsDto.SettingValue);
                        command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                        
                        if (count == 0)
                        {
                            command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                        }

                        await command.ExecuteNonQueryAsync();
                        return Result<bool>.SuccessResult(true, "设置保存成功");
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<bool>.Error($"保存设置失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取用户设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="settingKey">设置键</param>
        /// <returns>设置值</returns>
        public async Task<Result<string>> GetUserSettingAsync(int userId, string settingKey)
        {
            try
            {
                // 检查用户是否存在
                var userExists = await CheckUserExistsAsync(userId);
                if (!userExists)
                {
                    return Result<string>.Error("用户不存在", 404);
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var command = new MySqlCommand(
                        "SELECT SettingValue FROM UserSettings WHERE UserId = @UserId AND SettingKey = @SettingKey",
                        connection);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@SettingKey", settingKey);
                    
                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        return Result<string>.SuccessResult(result.ToString(), "获取设置成功");
                    }
                    else
                    {
                        return Result<string>.SuccessResult(null, "未找到指定设置");
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<string>.Error($"获取设置失败：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取用户所有设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>所有设置</returns>
        public async Task<Result<Dictionary<string, string>>> GetAllUserSettingsAsync(int userId)
        {
            try
            {
                // 检查用户是否存在
                var userExists = await CheckUserExistsAsync(userId);
                if (!userExists)
                {
                    return Result<Dictionary<string, string>>.Error("用户不存在", 404);
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var command = new MySqlCommand(
                        "SELECT SettingKey, SettingValue FROM UserSettings WHERE UserId = @UserId",
                        connection);
                    command.Parameters.AddWithValue("@UserId", userId);
                    
                    var settings = new Dictionary<string, string>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var key = reader.GetString(0);
                            var value = reader.GetString(1);
                            settings[key] = value;
                        }
                    }
                    
                    return Result<Dictionary<string, string>>.SuccessResult(settings, "获取设置成功");
                }
            }
            catch (Exception ex)
            {
                return Result<Dictionary<string, string>>.Error($"获取设置失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新用户在线状态
        /// </summary>
        /// <param name="statusDto">状态DTO</param>
        /// <returns>更新结果</returns>
        public async Task<Result<bool>> UpdateUserStatusAsync(UserStatusDTO statusDto)
        {
            if (statusDto == null)
            {
                return Result<bool>.Error("状态数据不能为空");
            }
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // 更新用户状态
                    string sql = @"UPDATE users SET 
                                Status = @Status, 
                                IsStatusVisible = @IsVisible,
                                UpdateTime = @UpdateTime
                                WHERE UserId = @UserId";
                    
                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", statusDto.UserId);
                        command.Parameters.AddWithValue("@Status", statusDto.IsOnline ? "online" : "offline");
                        command.Parameters.AddWithValue("@IsVisible", statusDto.IsVisible);
                        command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                        
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        if (rowsAffected > 0)
                        {
                            return Result<bool>.Success(true);
                        }
                        else
                        {
                            return Result<bool>.Error("用户状态更新失败");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新用户状态时出错: {ex.Message}");
                return Result<bool>.Error($"更新用户状态时出错: {ex.Message}");
            }
        }
    }
} 