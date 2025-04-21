using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTOs;

namespace backend.Services
{
    /// <summary>
    /// �������ӿ�
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// ��ȡ����������
        /// </summary>
        /// <param name="roomId">������ID</param>
        /// <returns>����������</returns>
        Task<string> GetRoomNameAsync(int roomId);

        /// <summary>
        /// ��ȡ��������ʷ��Ϣ
        /// </summary>
        /// <param name="roomId">������ID</param>
        /// <param name="count">��Ϣ����</param>
        /// <returns>��Ϣ�б�</returns>
        Task<List<MessageDTO>> GetRoomMessagesAsync(int roomId, int count);

        /// <summary>
        /// ������������Ϣ
        /// </summary>
        /// <param name="roomId">������ID</param>
        /// <param name="userId">�û�ID</param>
        /// <param name="message">��Ϣ����</param>
        /// <returns>��ϢID</returns>
        Task<int> SaveRoomMessageAsync(int roomId, int userId, string message);

        /// <summary>
        /// ��ȡ�����������û�
        /// </summary>
        /// <param name="roomId">������ID</param>
        /// <returns>�û��б�</returns>
        Task<List<UserDTO>> GetRoomOnlineUsersAsync(int roomId);

        /// <summary>
        /// ��ȡ�򴴽�Ĭ��������
        /// </summary>
        /// <returns>������ID</returns>
        Task<int> GetOrCreateDefaultRoomAsync();

        /// <summary>
        /// ����û��������б�
        /// </summary>
        /// <param name="roomId">������ID</param>
        /// <param name="user">�û���Ϣ</param>
        void AddUserToRoom(int roomId, UserDTO user);

        /// <summary>
        /// �������б��Ƴ��û�
        /// </summary>
        /// <param name="roomId">������ID</param>
        /// <param name="userId">�û�ID</param>
        void RemoveUserFromRoom(int roomId, int userId);
    }
} 