using Maple.CustomExplosions;
using Message;
using Net.Remote;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maple.CustomCore
{
    /// <summary>
    /// 
    /// </summary>
    public class ReceiveCallbackMgr : IReceiveCallbackMgr
    {
        private Dictionary<int, ReceiveCallback> DicCallback { get; }

        public ReceiveCallbackMgr()
        {
            DicCallback = new Dictionary<int, ReceiveCallback>(4096);
            Regist();
        }


        public void Regist()
        {
            var methods = this.GetType().GetMethods(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.NonPublic);

            foreach (var m in methods)
            {
                var att = m.get.FirstAttribute<CallbackIdAttribute>();
                if (att == null)
                {
                    continue;
                }

                var callback = m.Invoke(null, null) as ReceiveCallback;
                DicCallback.Add(att.ID, callback);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="message"></param>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public virtual ValueTask<object> ReceiveCallback(int messageId, object message, IReceiveMessage receiver)
        {
            try
            {
                this.OnMessgeExecuting(messageId);
                return OnMessgeRunning(messageId, message, receiver);
            }
            catch (Exception ex)
            {
                return this.OnMessgeException(messageId, ex);
            }
            finally
            {
                this.OnMessgeExecuted(messageId);
            }
        }


        /// <summary>
        /// 返回错误
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual ValueTask<object> OnMessgeException(int messageId, Exception ex)
        {
            System.Console.WriteLine(ex.Message);
            //rpc返回类型
            if (ex is RpcCallbackException rpcCallbackException)
            {
                return new ValueTask<object>(rpcCallbackException.Result);
            }
            return default;
        }

        /// <summary>
        /// 查询不到
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual ValueTask<object> OnMessgeKeyNotFound(int messageId)
        {
            Console.WriteLine($@"{messageId} {nameof(OnMessgeNullReference)}");
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        protected virtual ValueTask<object> OnMessgeNullReference(int messageId)
        {
            Console.WriteLine($@"{messageId} {nameof(OnMessgeNullReference)}");
            return default;
        }

        /// <summary>
        /// 开始
        /// </summary>
        /// <param name="messageId"></param>
        protected virtual void OnMessgeExecuting(int messageId)
        {
            Console.WriteLine($@"{messageId} {nameof(OnMessgeExecuting)}");
        }

        /// <summary>
        /// 结束
        /// </summary>
        /// <param name="messageId"></param>
        protected virtual void OnMessgeExecuted(int messageId)
        {
            Console.WriteLine($@"{messageId} {nameof(OnMessgeExecuted)}");
        }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="message"></param>
        /// <param name="receiver"></param>
        /// <returns></returns>
        protected ValueTask<object> OnMessgeRunning(int messageId, object message, IReceiveMessage receiver)
        {
            if (this.DicCallback.TryGetValue(messageId, out var callback))
            {
                if (callback != null)
                {
                    return callback.Invoke(messageId, message, receiver);
                }
                else
                {
                    return this.OnMessgeNullReference(messageId);
                }
            }
            else
            {
                return this.OnMessgeKeyNotFound(messageId);
            }
        }

        [CallbackId(1003)]
        private ValueTask<object> Login(int messageId, object message, IReceiveMessage receiver)
        {
            return default;
        }

    }
}
