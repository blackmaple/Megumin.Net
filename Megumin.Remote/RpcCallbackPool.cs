using Megumin.Message;
using Net.Remote;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Megumin.Remote
{


    /// <summary>
    /// Rpc回调注册池
    /// 每个session大约每秒30个包，超时时间默认为30秒；
    /// </summary>
    public class RpcCallbackPool : System.Collections.Concurrent.ConcurrentDictionary<int, (DateTime startTime, RpcCallback rpcCallback)>, IRpcCallbackPool
    {

        /// <summary>
        /// 
        /// </summary>
        public RpcCallbackPool() : this(32)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        public RpcCallbackPool(int capacity) : base(8, capacity)
        {
        }

        /// <summary>
        /// 默认30000ms
        /// </summary>
        public int RpcTimeOutMilliseconds { get; set; } = 30000;


        static readonly object lock_BuildRpcId = new object();
        int rpcCursor = 0;

        /// <summary>
        /// 原子操作 取得RpcId,发送方的的RpcID为正数，回复的RpcID为负数，正负一一对应
        /// <para>0,int.MinValue 为无效值</para> 
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetRpcID()
        {
            lock (lock_BuildRpcId)
            {
                return (this.rpcCursor == int.MaxValue) ? (this.rpcCursor = 1) : (++this.rpcCursor);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="RpcResult"></typeparam>
        /// <returns></returns>
        public (int rpcID, IMiniAwaitable<(RpcResult result, Exception exception)> source) Regist<RpcResult>()
        {
            var rpcID = GetRpcID();
            var key = rpcID * -1;

            IMiniAwaitable<(RpcResult result, Exception exception)> source = MiniTask<(RpcResult result, Exception exception)>.Rent();
            this.AddOrUpdate(key, (DateTime.Now, DefRpcCallBack), (oldKey, oldValue) =>
            {
                oldValue.rpcCallback?.Invoke(default, new TimeoutException("RpcID overlaps and timeouts the previous callback/RpcID 重叠，对前一个回调进行超时处理"));
                return (DateTime.Now, DefRpcCallBack);
            });

            void DefRpcCallBack(object resp, Exception ex)
            {
                if (ex == null)
                {
                    if (resp is RpcResult result)
                    {
                        source.SetResult((result, null));
                    }
                    else if (resp == null)
                    {
                        source.SetResult((default, new NullReferenceException()));
                    }
                    else
                    {
                        //转换类型错误
                        source.SetResult((default,
                            new InvalidCastException($"Return {resp.GetType()} type, cannot be converted to {typeof(RpcResult)}" +
                            $"/返回{resp.GetType()}类型，无法转换为{typeof(RpcResult)}")));
                    }
                }
                else
                {
                    source.SetResult((default, ex));
                }

            }
            this.CreateCheckTimeout(key);

            return (rpcID, source);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="RpcResult"></typeparam>
        /// <param name="OnException"></param>
        /// <returns></returns>
        public (int rpcID, IMiniAwaitable<RpcResult> source) Regist<RpcResult>(Action<Exception> OnException)
        {
            var rpcID = GetRpcID();
            var key = rpcID * -1;

            IMiniAwaitable<RpcResult> source = MiniTask<RpcResult>.Rent();

            //  CheckKeyConflict(key);

            this.AddOrUpdate(key, (DateTime.Now, DefRpcCallBack), (oldKey, oldValue) =>
            {
                oldValue.rpcCallback?.Invoke(default, new TimeoutException("RpcID overlaps and timeouts the previous callback/RpcID 重叠，对前一个回调进行超时处理"));
                return (DateTime.Now, DefRpcCallBack);
            });

            void DefRpcCallBack(object resp, Exception ex)
            {
                if (ex == null)
                {
                    if (resp is RpcResult result)
                    {
                        source.SetResult(result);
                    }
                    else
                    {
                        source.CancelWithNotExceptionAndContinuation();
                        if (resp == null)
                        {
                            OnException?.Invoke(new NullReferenceException());
                        }
                        else
                        {
                            //转换类型错误
                            OnException?.Invoke(new InvalidCastException($"Return {resp.GetType()} type, cannot be converted to {typeof(RpcResult)}" +
                                $"/返回{resp.GetType()}类型，无法转换为{typeof(RpcResult)}"));
                        }
                    }
                }
                else
                {
                    source.CancelWithNotExceptionAndContinuation();
                    OnException?.Invoke(ex);
                }
            }

            this.CreateCheckTimeout(key);

            return (rpcID, source);
        }

        /// <summary>
        /// 创建超时检查
        /// </summary>
        /// <param name="rpcID"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CreateCheckTimeout(int rpcID)
        {
            //备注：即使异步发送被同步调用，此处也不会发生错误。
            //同步调用，当返回消息返回时，会从回调池移除，
            //那么计时器结束时将不会找到Task。如果调用出没有保持Task引用，
            //那么Task会成为孤岛，被GC回收。

 

            Task.Run(async () =>
            {
                if (RpcTimeOutMilliseconds >= 0)
                {
                    await Task.Delay(RpcTimeOutMilliseconds);
                    if (TryDequeue(rpcID, out var rpc))
                    {
                        MessageThreadTransducer.Invoke(() =>
                        {
                            rpc.rpcCallback?.Invoke(null, new TimeoutException($"The RPC {rpcID} callback timed out and did not get a remote response./RPC {rpcID} 回调超时，没有得到远端响应。"));
                        });
                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="rpc"></param>
        /// <returns></returns>
        public bool TryDequeue(int rpcID, out (DateTime startTime, Net.Remote.RpcCallback rpcCallback) rpc)
        {
            return this.TryRemove(rpcID, out rpc);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool TrySetResult(int rpcID, object msg)
        {
            return TryComplate(rpcID, msg, default);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public bool TrySetException(int rpcID, Exception exception)
        {
            return TryComplate(rpcID, default, exception);
        }

        bool TryComplate(int rpcID, object msg, Exception exception)
        {
            //rpc响应
            if (TryDequeue(rpcID, out var rpc))
            {
                rpc.rpcCallback?.Invoke(msg, exception);
                return true;
            }
            return false;
        }
 
    }





}
