using System.Threading.Tasks;
using backend.DTOs;
using backend.Models;
using backend.Utils;

namespace backend.Services
{
    /// <summary>
    /// �û�����ӿ�
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// �û���¼
        /// </summary>
        /// <param name="loginDto">��¼��Ϣ</param>
        /// <returns>��¼���</returns>
        Task<Result<LoginResultVO>> LoginAsync(LoginDTO loginDto);

        /// <summary>
        /// �û�ע��
        /// </summary>
        /// <param name="registerDto">ע����Ϣ</param>
        /// <returns>ע����</returns>
        Task<Result<UserVO>> RegisterAsync(RegisterDTO registerDto);

        /// <summary>
        /// ��֤�û����Ƿ����
        /// </summary>
        /// <param name="username">�û���</param>
        /// <returns>��֤���</returns>
        Task<Result<bool>> CheckUsernameExistsAsync(string username);

        /// <summary>
        /// ����ID��ȡ�û���Ϣ
        /// </summary>
        /// <param name="userId">�û�ID</param>
        /// <returns>�û���Ϣ</returns>
        Task<Result<UserVO>> GetUserInfoAsync(int userId);
        Task<UserDTO> GetUserByUsernameAsync(string username);
        Task<UserDTO> GetUserByIdAsync(int userId);
    }
} 