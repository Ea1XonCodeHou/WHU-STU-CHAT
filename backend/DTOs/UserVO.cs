using System;

namespace backend.DTOs
{
    /// <summary>
    /// �û���Ϣ��ͼ����
    /// </summary>
    public class UserVO
    {
        /// <summary>
        /// �û�ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// �û���
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// �ֻ���
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// ͷ��
        /// </summary>
        public string Avatar { get; set; }
    }

    /// <summary>
    /// ��¼�ɹ��󷵻ص���ͼ����
    /// </summary>
    public class LoginResultVO
    {
        /// <summary>
        /// �û�������Ϣ
        /// </summary>
        public UserVO UserInfo { get; set; }

        /// <summary>
        /// �����֤����
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// ���ƹ���ʱ��
        /// </summary>
        public DateTime ExpireTime { get; set; }
    }
} 