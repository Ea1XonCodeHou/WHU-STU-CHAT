using System;
using System.Collections.Generic;

namespace backend.Utils
{
    /// <summary>
    /// 通用结果类，封装API返回结果
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 成功
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="msg">消息</param>
        /// <returns>Result对象</returns>
        public static Result<T> Success(T data, string msg = "操作成功")
        {
            return new Result<T>
            {
                Code = 200,
                Msg = msg,
                Data = data
            };
        }

        /// <summary>
        /// 成功结果 (别名方法，等同于Success)
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="msg">消息</param>
        /// <returns>Result对象</returns>
        public static Result<T> SuccessResult(T data, string msg = "操作成功")
        {
            return Success(data, msg);
        }

        /// <summary>
        /// 失败
        /// </summary>
        /// <param name="msg">错误消息</param>
        /// <param name="code">错误码</param>
        /// <returns>Result对象</returns>
        public static Result<T> Error(string msg = "操作失败", int code = 500)
        {
            return new Result<T>
            {
                Code = code,
                Msg = msg,
                Data = default
            };
        }

        /// <summary>
        /// 未授权
        /// </summary>
        /// <param name="msg">错误消息</param>
        /// <returns>Result对象</returns>
        public static Result<T> Unauthorized(string msg = "未授权")
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
    /// 无泛型参数的Result，用于不需要返回数据的接口
    /// </summary>
    public class Result
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 成功
        /// </summary>
        /// <param name="msg">消息</param>
        /// <returns>Result对象</returns>
        public static Result Success(string msg = "操作成功")
        {
            return new Result
            {
                Code = 200,
                Msg = msg
            };
        }

        /// <summary>
        /// 成功结果 (别名方法，等同于Success)
        /// </summary>
        /// <param name="msg">消息</param>
        /// <returns>Result对象</returns>
        public static Result SuccessResult(string msg = "操作成功")
        {
            return Success(msg);
        }

        /// <summary>
        /// 失败
        /// </summary>
        /// <param name="msg">错误消息</param>
        /// <param name="code">错误码</param>
        /// <returns>Result对象</returns>
        public static Result Error(string msg = "操作失败", int code = 500)
        {
            return new Result
            {
                Code = code,
                Msg = msg
            };
        }

        /// <summary>
        /// 未授权
        /// </summary>
        /// <param name="msg">错误消息</param>
        /// <returns>Result对象</returns>
        public static Result Unauthorized(string msg = "未授权")
        {
            return new Result
            {
                Code = 401,
                Msg = msg
            };
        }
    }
} 