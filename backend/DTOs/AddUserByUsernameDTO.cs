using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    /// <summary>
    /// 通过用户名添加用户到群组的请求DTO
    /// </summary>
    public class AddUserByUsernameDTO
    {
        /// <summary>
        /// 要添加的用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        public string UserName { get; set; }
    }
} 