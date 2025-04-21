using System;

namespace backend.Models
{
    /// <summary>
    /// �û�ģ��
    /// </summary>
    public class User
    {
        /// <summary>
        /// �û�ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// �û���
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        public string Password { get; set; }

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

        /// <summary>
        /// ����¼ʱ��
        /// </summary>
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
} 