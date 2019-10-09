﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Megumin.Message;
using Net.Remote;

namespace Megumin.Remote
{
    public abstract partial class RemoteBase : IUID<int>
    {
        /// <summary>
        /// 
        /// </summary>
        public int ID { get; } = InterlockedID<IRemote>.NewID();
        /// <summary>
        /// 这是留给用户赋值的
        /// </summary>
        public virtual int UID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsVaild { get; protected set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint ConnectIPEndPoint { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime LastReceiveTime { get; protected set; } = DateTime.Now;
        /// <summary>
        /// 
        /// </summary>
        public IRpcCallbackPool RpcCallbackPool { get; } = new RpcCallbackPool(32);
        /// <summary>
        /// 当前是否为手动关闭中
        /// </summary>
        protected bool manualDisconnecting = false;

        /// <summary>
        /// 如果没有设置消息管道，使用默认消息管道。
        /// </summary>
        public IMessagePipeline MessagePipeline { get; set; } = Message.MessagePipeline.Default;
    }

    /// 发送
    partial class RemoteBase : ISendMessage, IAsyncSendMessage
    {
        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="message"></param>
        public void SendAsync(object message)
        {
            SendAsync(0, message);
        }

        /// <summary>
        /// 正常发送入口
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="message"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal protected virtual void SendAsync(int rpcID, object message)
            => SendAsync(MessagePipeline.Pack(rpcID, message));

        /// <summary>
        /// 注意，发送完成时内部回收了buffer。
        /// ((框架约定1)发送字节数组发送完成后由发送逻辑回收)
        /// </summary>
        public abstract void SendAsync(IMemoryOwner<byte> memoryOwner);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="RpcResult"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        public IMiniAwaitable<(RpcResult result, Exception exception)> SendAsync<RpcResult>(object message)
        {
            ReceiveStart();

            var (rpcID, source) = RpcCallbackPool.Regist<RpcResult>();

            try
            {
                SendAsync(rpcID, message);
                return source;
            }
            catch (Exception e)
            {
                RpcCallbackPool.TrySetException(rpcID * -1, e);
                return source;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="RpcResult"></typeparam>
        /// <param name="message"></param>
        /// <param name="OnException"></param>
        /// <returns></returns>
        public IMiniAwaitable<RpcResult> SendAsyncSafeAwait<RpcResult>(object message, Action<Exception> OnException = null)
        {
            ReceiveStart();

            var (rpcID, source) = RpcCallbackPool.Regist<RpcResult>(OnException);

            try
            {
                SendAsync(rpcID, message);
                return source;
            }
            catch (Exception e)
            {
                source.CancelWithNotExceptionAndContinuation();
                OnException?.Invoke(e);
                return source;
            }
        }
    }

    /// <summary>
    /// 接受
    /// </summary>
    partial class RemoteBase
    {
        /// <summary>
        /// 
        /// </summary>
        protected const int MaxBufferLength = 8192;

        /// <summary>
        /// 应该为线程安全的，多次调用不应该发生错误
        /// </summary>
        public abstract void ReceiveStart();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="byteMessage"></param>
        protected virtual void ReceiveByteMessage(IMemoryOwner<byte> byteMessage)
        {
            MessagePipeline.Unpack(byteMessage, this);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    partial class RemoteBase : IObjectMessageReceiver, IReceiveMessage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messgaeId"></param>
        /// <param name="rpcID"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public ValueTask<object> Deal(EnumMessgaeId messgaeId, int rpcID, object message)
        {
            if (rpcID < 0)
            {
                //这个消息是rpc返回（回复的RpcID为负数）
                RpcCallbackPool?.TrySetResult(rpcID, message);
                return new ValueTask<object>(result: null);
            }
            else
            {
                //这个消息是非Rpc应答
                //普通响应onRely
                return DealMessage(messgaeId, message);
            }
        }

        /// <summary>
        /// 通常用户接收反序列化完毕的消息的函数
        /// </summary>
        /// <param name="messgaeId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask<object> DealMessage(EnumMessgaeId messgaeId, object message)
        {
            if (onReceive == null)
            {
                return new ValueTask<object>(result: null);
            }
            else
            {
                return onReceive.Invoke(messgaeId,message, this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected ReceiveCallback onReceive;

        /// <summary>
        /// 注意： 重写了注册函数，只能保存一个委托
        /// </summary>
        public virtual event ReceiveCallback OnReceiveCallback
        {
            add
            {
                onReceive = value;
            }
            remove
            {
                onReceive -= value;
            }
        }
    }

    ///路由
    partial class RemoteBase : IForwarder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="identifier"></param>
        public void SendAsync(object message, int identifier)
        {
            SendAsync(0, message, identifier);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rpcID"></param>
        /// <param name="message"></param>
        /// <param name="identifier"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal protected virtual void SendAsync<T>(int rpcID, T message, int identifier)
            => SendAsync(MessagePipeline.Pack(0, message, identifier));

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="RpcResult"></typeparam>
        /// <param name="message"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public IMiniAwaitable<(RpcResult result, Exception exception)> SendAsync<RpcResult>(object message, int identifier)
        {
            ReceiveStart();

            var (rpcID, source) = RpcCallbackPool.Regist<RpcResult>();

            try
            {
                SendAsync(rpcID, message, identifier);
                return source;
            }
            catch (Exception e)
            {
                RpcCallbackPool.TrySetException(rpcID * -1, e);
                return source;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="RpcResult"></typeparam>
        /// <param name="message"></param>
        /// <param name="identifier"></param>
        /// <param name="OnException"></param>
        /// <returns></returns>
        public IMiniAwaitable<RpcResult> SendAsyncSafeAwait<RpcResult>(object message, int identifier, Action<Exception> OnException = null)
        {
            ReceiveStart();

            var (rpcID, source) = RpcCallbackPool.Regist<RpcResult>(OnException);

            try
            {
                SendAsync(rpcID, message, identifier);
                return source;
            }
            catch (Exception e)
            {
                source.CancelWithNotExceptionAndContinuation();
                OnException?.Invoke(e);
                return source;
            }
        }
    }


    internal static class Debug
    {
        const string moduleName = "Megumin.Remote";
        public static void Log(object message)
            => MeguminDebug.Log(message, moduleName);

        public static void LogError(object message)
            => MeguminDebug.LogError(message, moduleName);

        public static void LogWarning(object message)
            => MeguminDebug.LogWarning(message, moduleName);
    }
}
