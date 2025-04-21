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
    /// �û�����ʵ��
    /// </summary>
    public class UserService : IUserService
    {
        private readonly string _connectionString;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="configuration">����</param>
        public UserService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// �û���¼
        /// </summary>
        /// <param name="loginDto">��¼��Ϣ</param>
        /// <returns>��¼���</returns>
        public async Task<Result<LoginResultVO>> LoginAsync(LoginDTO loginDto)
        {
            try
            {
                // ���ڴ洢�û���Ϣ�ı���
                int userId = 0;
                string username = string.Empty;
                string email = null;
                string phone = null;
                string avatar = null;
                bool passwordIsValid = false;

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // �����û�����ѯ�û�
                    var command = new MySqlCommand(
                        "SELECT UserId, Username, Password, Email, Phone, Avatar FROM Users WHERE Username = @Username",
                        connection);
                    command.Parameters.AddWithValue("@Username", loginDto.Username);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // ��ȡ�û���Ϣ
                            userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                            username = reader.GetString(reader.GetOrdinal("Username"));
                            
                            // ��֤����
                            var storedPassword = reader.GetString(reader.GetOrdinal("Password"));
                            passwordIsValid = VerifyPassword(loginDto.Password, storedPassword);

                            if (!passwordIsValid)
                            {
                                return Result<LoginResultVO>.Error("�û������������", 400);
                            }

                            email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email"));
                            phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone"));
                            avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader.GetString(reader.GetOrdinal("Avatar"));
                        }
                        else
                        {
                            return Result<LoginResultVO>.Error("�û������������", 400);
                        }
                    }
                    
                    // DataReader�ѹرգ����ڿ��԰�ȫ��ִ����һ������
                    if (passwordIsValid)
                    {
                        // ��������¼ʱ��
                        var updateCommand = new MySqlCommand(
                            "UPDATE Users SET LastLoginTime = @LastLoginTime WHERE UserId = @UserId",
                            connection);
                        updateCommand.Parameters.AddWithValue("@LastLoginTime", DateTime.Now);
                        updateCommand.Parameters.AddWithValue("@UserId", userId);
                        await updateCommand.ExecuteNonQueryAsync();
                        
                        // ��������
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

                        return Result<LoginResultVO>.Success(result, "��¼�ɹ�");
                    }
                }
                
                // ���д���ʵ���ϲ���ִ�е�����Ϊ�����Ѿ������������������Ϊ�˴��������Ա���
                return Result<LoginResultVO>.Error("�û������������", 400);
            }
            catch (Exception ex)
            {
                return Result<LoginResultVO>.Error($"��¼ʧ�ܣ�{ex.Message}");
            }
        }

        /// <summary>
        /// �û�ע��
        /// </summary>
        /// <param name="registerDto">ע����Ϣ</param>
        /// <returns>ע����</returns>
        public async Task<Result<UserVO>> RegisterAsync(RegisterDTO registerDto)
        {
            try
            {
                // ����û����Ƿ��Ѵ���
                var usernameExists = await CheckUsernameExistsInternalAsync(registerDto.Username);
                if (usernameExists)
                {
                    return Result<UserVO>.Error("�û����Ѵ���", 400);
                }

                // ��������
                var encryptedPassword = EncryptPassword(registerDto.Password);

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // �������û���¼
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

                    // ִ��SQL����ȡ�²�����û�ID
                    var userId = Convert.ToInt32(await command.ExecuteScalarAsync());

                    // �����û���Ϣ
                    var userVo = new UserVO
                    {
                        Id = userId,
                        Username = registerDto.Username,
                        Email = registerDto.Email,
                        Phone = registerDto.Phone,
                        Avatar = null // Ĭ��Ϊnull����������Ĭ��ͷ��
                    };

                    return Result<UserVO>.Success(userVo, "ע��ɹ�");
                }
            }
            catch (Exception ex)
            {
                return Result<UserVO>.Error($"ע��ʧ�ܣ�{ex.Message}");
            }
        }

        /// <summary>
        /// ��֤�û����Ƿ����
        /// </summary>
        /// <param name="username">�û���</param>
        /// <returns>��֤���</returns>
        public async Task<Result<bool>> CheckUsernameExistsAsync(string username)
        {
            try
            {
                var exists = await CheckUsernameExistsInternalAsync(username);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return Result<bool>.Error($"����û���ʧ�ܣ�{ex.Message}");
            }
        }

        /// <summary>
        /// ����ID��ȡ�û���Ϣ
        /// </summary>
        /// <param name="userId">�û�ID</param>
        /// <returns>�û���Ϣ</returns>
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
                            return Result<UserVO>.Error("�û�������", 404);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<UserVO>.Error($"��ȡ�û���Ϣʧ�ܣ�{ex.Message}");
            }
        }

        #region ˽�з���

        /// <summary>
        /// ����û����Ƿ��Ѵ��ڣ��ڲ�ʹ�ã�
        /// </summary>
        /// <param name="username">�û���</param>
        /// <returns>�Ƿ����</returns>
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
        /// ��������
        /// </summary>
        /// <param name="password">ԭʼ����</param>
        /// <returns>���ܺ������</returns>
        private string EncryptPassword(string password)
        {
            // ��ʵ�֣�ʵ����Ŀ��Ӧʹ�ø���ȫ���㷨��BCrypt
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// ��֤����
        /// </summary>
        /// <param name="inputPassword">���������</param>
        /// <param name="storedPassword">�洢������</param>
        /// <returns>�Ƿ���Ч</returns>
        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            // ������������룬�����ݿ��д洢������Ƚ�
            var encryptedInput = EncryptPassword(inputPassword);
            return encryptedInput == storedPassword;
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="userId">�û�ID</param>
        /// <param name="username">�û���</param>
        /// <returns>�����ַ���</returns>
        private string GenerateToken(int userId, string username)
        {
            // ��ʵ�֣�ʵ����Ŀ��Ӧʹ��JWT
            var tokenData = $"{userId}:{username}:{Guid.NewGuid()}:{DateTime.Now.Ticks}";
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(tokenData);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        #endregion
    }
} 