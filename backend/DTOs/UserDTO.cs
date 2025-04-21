using System.ComponentModel.DataAnnotations;
using System;

namespace backend.DTOs
{
    /// <summary>
    /// �û���¼����DTO
    /// </summary>
    public class LoginDTO
    {
        /// <summary>
        /// �û���
        /// </summary>
        [Required(ErrorMessage = "�û�������Ϊ��")]
        public string Username { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        [Required(ErrorMessage = "���벻��Ϊ��")]
        public string Password { get; set; }

        /// <summary>
        /// ��ס��
        /// </summary>
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// �û�ע������DTO
    /// </summary>
    public class RegisterDTO
    {
        /// <summary>
        /// �û���
        /// </summary>
        [Required(ErrorMessage = "�û�������Ϊ��")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "�û������ȱ�����3-50���ַ�֮��")]
        public string Username { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        [Required(ErrorMessage = "���벻��Ϊ��")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "���볤�ȱ�����6-100���ַ�֮��")]
        public string Password { get; set; }

        /// <summary>
        /// ȷ������
        /// </summary>
        [Required(ErrorMessage = "ȷ�����벻��Ϊ��")]
        [Compare("Password", ErrorMessage = "�����������벻һ��")]
        public string ConfirmPassword { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        [EmailAddress(ErrorMessage = "�����ʽ����ȷ")]
        public string Email { get; set; }

        /// <summary>
        /// �ֻ���
        /// </summary>
        [Phone(ErrorMessage = "�ֻ��Ÿ�ʽ����ȷ")]
        public string Phone { get; set; }
    }

    /// <summary>
    /// �û����ݴ������
    /// </summary>
    public class UserDTO
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
        /// �ǳ�
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// ͷ��URL
        /// </summary>
        public string AvatarUrl { get; set; }

        /// <summary>
        /// ״̬������/���ߣ�
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// ����Ծʱ��
        /// </summary>
        public DateTime LastActive { get; set; }
    }
} 