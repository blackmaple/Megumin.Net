﻿using System;
using System.Collections.Generic;
using System.Text;
using Net.Remote;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>
    /// 一个简单异步任务实现，特点是缓存任务不构造任务实例。
    /// todo 如果任务没有完成访问Result,会返回null而不是阻塞。以后会改为和Task一致
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MiniTask<T> : IMiniAwaitable<T>
    {
        enum EnumState
        {
            InPool,
            Waiting,
            Success,
            Faild,
        }

        /// <summary>
        /// 
        /// </summary>
        public static int MaxCount { get; set; } = 512;

        static ConcurrentQueue<MiniTask<T>> pool = new ConcurrentQueue<MiniTask<T>>();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static MiniTask<T> Rent()
        {
            if (pool.TryDequeue(out var task))
            {
                if (task != null)
                {
                    task.State = EnumState.Waiting;


                    return task;
                }
            }

            return new MiniTask<T>() { State = EnumState.Waiting };
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ClearPool()
        {
            lock (pool)
            {
                while (pool.Count > 0)
                {
                    pool.TryDequeue(out var task);
                    task?.Reset();
                }
            }
        }

        volatile EnumState State = EnumState.InPool;

        private Action continuation;
        /// <summary>
        /// 是否进入异步挂起阶段
        /// </summary>
        private bool alreadyEnterAsync = false;

        /// <summary>
        /// 
        /// </summary>
        public bool IsCompleted => State == EnumState.Success || State == EnumState.Faild;
        /// <summary>
        /// 请不要同步访问Result。即使同步完成也应该使用await 关键字。同步访问可能无法取得正确的值，或抛出异常。
        /// </summary>
        public T Result { get; protected set; }
        readonly object innerlock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="continuation"></param>
        public void UnsafeOnCompleted(Action continuation)
        {
            lock (innerlock)
            {
                if (State == EnumState.InPool)
                {
                    //这里被触发一定是是类库BUG。
                    throw new ArgumentException($"{nameof(MiniTask<T>)} task conflict, underlying error, please contact the framework author." +
                        $"/{nameof(MiniTask<T>)}任务冲突，底层错误，请联系框架作者。");
                }

                alreadyEnterAsync = true;
                this.continuation -= continuation;
                this.continuation += continuation;
                TryComplete();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            lock (innerlock)
            {
                if (State == EnumState.InPool)
                {
                    //这里被触发一定是是类库BUG。
                    throw new ArgumentException($"{nameof(MiniTask<T>)} task conflict, underlying error, please contact the framework author." +
                        $"/{nameof(MiniTask<T>)}任务冲突，底层错误，请联系框架作者。");
                }

                alreadyEnterAsync = true;
                this.continuation -= continuation;
                this.continuation += continuation;
                TryComplete();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public void SetResult(T result)
        {
            lock (innerlock)
            {
                if (State == EnumState.InPool)
                {
                    throw new InvalidOperationException($"Task does not exist/任务不存在");
                }
                this.Result = result;
                State = EnumState.Success;
                TryComplete();
            }
        }

        private void TryComplete()
        {
            if (alreadyEnterAsync)
            {
                if (State == EnumState.Waiting)
                {
                    return;
                }

                if (State == EnumState.Success)
                {
                    continuation?.Invoke();
                }

                //处理后续方法结束，归还到池中
                this.Return();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void CancelWithNotExceptionAndContinuation()
        {
            lock (innerlock)
            {
                if (State == EnumState.InPool)
                {
                    throw new InvalidOperationException($"Task does not exist/任务不存在");
                }

                Result = default;
                State = EnumState.Faild;
                TryComplete();
            }
        }

#if DEBUG
        int lastThreadID;
#endif

        void Return()
        {
            Reset();

            if (State != EnumState.InPool)
            {
                //state = State.InPool;必须在pool.Enqueue(this);之前。
                //因为当pool为空的时候，放入池的元素会被立刻取出。并将状态设置为Waiting。
                //如果state = State.InPool;在pool.Enqueue(this)后，那么会导致Waiting 状态被错误的设置为InPool;
                // **** 我在这里花费了4个小时（sad）。
                State = EnumState.InPool;

#if DEBUG
                lastThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif

                if (pool.Count < MaxCount)
                {
                    pool.Enqueue(this);
                }
            }
        }

        void Reset()
        {
            alreadyEnterAsync = false;
            Result = default;
            continuation = null;
        }
    }
}
