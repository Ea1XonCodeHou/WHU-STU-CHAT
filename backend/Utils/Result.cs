using System;
using System.Collections.Generic;

namespace backend.Utils
{
    /// <summary>
    /// ͨ�ý���࣬��װAPI���ؽ��
    /// </summary>
    /// <typeparam name="T">��������</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// ״̬��
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// ��Ϣ
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// �ɹ�
        /// </summary>
        /// <param name="data">����</param>
        /// <param name="msg">��Ϣ</param>
        /// <returns>Result����</returns>
        public static Result<T> Success(T data, string msg = "�����ɹ�")
        {
            return new Result<T>
            {
                Code = 200,
                Msg = msg,
                Data = data
            };
        }

        /// <summary>
        /// ʧ��
        /// </summary>
        /// <param name="msg">������Ϣ</param>
        /// <param name="code">������</param>
        /// <returns>Result����</returns>
        public static Result<T> Error(string msg = "����ʧ��", int code = 500)
        {
            return new Result<T>
            {
                Code = code,
                Msg = msg,
                Data = default
            };
        }

        /// <summary>
        /// δ��Ȩ
        /// </summary>
        /// <param name="msg">������Ϣ</param>
        /// <returns>Result����</returns>
        public static Result<T> Unauthorized(string msg = "δ��Ȩ")
        {
            return new Result<T>
            {
                Code = 401,
                Msg = msg,
                Data = default
            };
        }
    }

    /// <summary>
    /// �޷��Ͳ�����Result�����ڲ���Ҫ�������ݵĽӿ�
    /// </summary>
    public class Result
    {
        /// <summary>
        /// ״̬��
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// ��Ϣ
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// �ɹ�
        /// </summary>
        /// <param name="msg">��Ϣ</param>
        /// <returns>Result����</returns>
        public static Result Success(string msg = "�����ɹ�")
        {
            return new Result
            {
                Code = 200,
                Msg = msg
            };
        }

        /// <summary>
        /// ʧ��
        /// </summary>
        /// <param name="msg">������Ϣ</param>
        /// <param name="code">������</param>
        /// <returns>Result����</returns>
        public static Result Error(string msg = "����ʧ��", int code = 500)
        {
            return new Result
            {
                Code = code,
                Msg = msg
            };
        }

        /// <summary>
        /// δ��Ȩ
        /// </summary>
        /// <param name="msg">������Ϣ</param>
        /// <returns>Result����</returns>
        public static Result Unauthorized(string msg = "δ��Ȩ")
        {
            return new Result
            {
                Code = 401,
                Msg = msg
            };
        }
    }
} 