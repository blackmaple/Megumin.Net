using Maple.CustomExplosions;
using Maple.CustomStandard;
using Megumin.Message;
using Message;
using Net.Remote;
using NetRemoteStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maple.CustomCore
{





    /// <summary>
    /// 
    /// </summary>
    public class ReceiveCallbackMgr : IReceiveCallbackMgr
    {

      

        public delegate ValueTask<object> MapleReceiveCallback(int messageId, object message, IReceiveMessage receiver);


        private Dictionary<int, MapleReceiveCallback> DicCallback { get; }

        public IMessagePipeline MessagePipeline => Megumin.Message.MessagePipeline.Default;

        public ReceiveCallbackMgr()
        {
            DicCallback = new Dictionary<int, MapleReceiveCallback>(4096);
            Regist();
        }

        public void Regist()
        {
            var methods = this.GetType().GetMethods(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic |
                 System.Reflection.BindingFlags.Static
               );

            var typeMethods = typeof(CallbackIdAttribute);
            var typeDelegate = typeof(MapleReceiveCallback);
            foreach (var m in methods)
            {
                if (!(m.GetCustomAttributes(typeMethods, true).FirstOrDefault() is CallbackIdAttribute att))
                {
                    continue;
                }
                var callback = m.CreateDelegate(typeDelegate, this) as MapleReceiveCallback;
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
        public virtual async  ValueTask<object> ReceiveCallback(int messageId, object message, IReceiveMessage receiver)
        {
            try
            {
                this.OnMessgeExecuting(messageId, message, receiver);
                return await  this.OnMessgeRunning(messageId, message, receiver);
            }
            catch (Exception ex)
            {
                return this.OnMessgeException(messageId, message, receiver, ex);
            }
            finally
            {
                this.OnMessgeExecuted(messageId, message, receiver);
            }
        }


        /// <summary>
        /// 返回错误
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual ValueTask<object> OnMessgeException(int messageId, object message, IReceiveMessage receiver, Exception ex)
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
        protected virtual void OnMessgeKeyNotFound(int messageId, object message, IReceiveMessage receiver)
        {
            Console.WriteLine($@"{messageId} {nameof(OnMessgeNullReference)}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        protected virtual void OnMessgeNullReference(int messageId, object message, IReceiveMessage receiver)
        {
            Console.WriteLine($@"{messageId} {nameof(OnMessgeNullReference)}");
        }

        /// <summary>
        /// 开始
        /// </summary>
        /// <param name="messageId"></param>
        protected virtual void OnMessgeExecuting(int messageId, object message, IReceiveMessage receiver)
        {
            Console.WriteLine($@"{messageId} {nameof(OnMessgeExecuting)}");
        }

        /// <summary>
        /// 结束
        /// </summary>
        /// <param name="messageId"></param>
        protected virtual void OnMessgeExecuted(int messageId, object message, IReceiveMessage receiver)
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
        protected   ValueTask<object> OnMessgeRunning(int messageId, object message, IReceiveMessage receiver)
        {
            if (this.DicCallback.TryGetValue(messageId, out var callback))
            {
                if (callback != null)
                {
                    Console.WriteLine($@"{messageId} {callback.Method.Name}");
                    return   callback.Invoke(messageId, message, receiver);
                }
                else
                {
                    this.OnMessgeNullReference(messageId, message, receiver);
                }
            }
            else
            {
                this.OnMessgeKeyNotFound(messageId, message, receiver);
            }
            return default;
        }


        private T_Message GetMessage<T_Message>(object message)
        {
            if (message is T_Message msg)
            {
                return msg;
            }
            throw new InvalidCastException();
        }


        [CallbackId(1003)]
        protected   ValueTask<object> Login(int messageId, object message, IReceiveMessage receiver)
        {
            var msg = this.GetMessage<Login2Gate>(message);
            var data = new Login2GateResult() { Code = EnumRpcCallbackResultStatus.Success };
            // return new ValueTask<object>(data);
     //       throw new RpcCallbackException<Login2GateResult>(messageId);
            return new ValueTask<object>(data);
        }

    }



}
