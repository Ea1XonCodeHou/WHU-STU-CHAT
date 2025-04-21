using System;
using System.Collections.Generic;

namespace backend.DTOs
{
    /// <summary>
    /// ���������ݴ������
    /// </summary>
    public class RoomDTO
    {
        /// <summary>
        /// ������ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ����������
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ����������
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// �����ҳ�Ա
        /// </summary>
        public List<UserDTO> Members { get; set; }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        public MessageDTO LastMessage { get; set; }
    }
} 