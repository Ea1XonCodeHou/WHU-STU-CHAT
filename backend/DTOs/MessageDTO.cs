using System;

namespace backend.DTOs
{
    /// <summary>
    /// ��Ϣ���ݴ������
    /// </summary>
    public class MessageDTO
    {
        /// <summary>
        /// ��ϢID
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// ��Ϣ����
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public DateTime SendTime { get; set; }

        /// <summary>
        /// ������ID
        /// </summary>
        public int SenderId { get; set; }

        /// <summary>
        /// �������û���
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// ������ID
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// �Ƿ��Ѷ�
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// ��Ϣ���ͣ�text��system��image�ȣ�
        /// </summary>
        public string MessageType { get; set; } = "text";
    }

    public class GroupMessageDTO
    {
        /// <summary>
        /// ��ϢID
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// ��Ϣ����
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// ������ID
        /// </summary>
        public int SenderId { get; set; }

        /// <summary>
        /// �������û���
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// ������ID
        /// </summary>
        public int GroupId { get; set; }
        
    }
} 