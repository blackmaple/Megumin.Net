﻿using Megumin.Message.TestMessage;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Megumin.Message
{
    /// <summary>
    /// Key冲突改怎么做
    /// </summary>
    public enum KeyAlreadyHave
    {
        /// <summary>
        /// 替换
        /// </summary>
        Replace,
        /// <summary>
        /// 跳过
        /// </summary>
        Skip,
        /// <summary>
        /// 抛出异常
        /// </summary>
        ThrowException,
    }

    /// <summary>
    /// 等待所有框架支持完毕ReadOnlyMemory 切换为ReadOnlySpan，现在需要将ReadOnlyMemory包装成流
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public delegate object Deserialize(ReadOnlyMemory<byte> buffer);
    /// <summary>
    /// 将消息从0位置开始 序列化 到 指定buffer中,返回序列化长度
    /// </summary>
    /// <param name="message">消息实例</param>
    /// <param name="buffer">给定的buffer,长度为16384</param>
    /// <returns>序列化消息的长度</returns>
    public delegate ushort RegistSerialize<in T>(T message, Span<byte> buffer);

    /// <summary>
    /// 值类型使用这个委托注册，相比值类型使用RegistSerialize注册，可以省一点点性能，但是仍然不建议用值类型消息。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public delegate ushort ValueRegistSerialize<T>(in T message, Span<byte> buffer);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public delegate ushort Serialize(object message, Span<byte> buffer);

    /// <summary>
    /// 消息查找表
    /// <seealso cref="Message.Serialize"/>  <seealso cref="Message.Deserialize"/>
    /// </summary>
    public class MessageLUT
    {
        static MessageLUT()
        {
            //注册测试消息和内置消息
            Regist<TestPacket1>(MessageIdAttribute.TestPacket1ID, TestPacket1.S, TestPacket1.D);
            Regist<TestPacket2>(MessageIdAttribute.TestPacket2ID, TestPacket2.S, TestPacket2.D);
            //5个基础类型
            Regist<string>(MessageIdAttribute.StringID, BaseType.Serialize, BaseType.StringDeserialize);
            Regist<int>(MessageIdAttribute.IntID, BaseType.Serialize, BaseType.IntDeserialize);
            Regist<long>(MessageIdAttribute.LongID, BaseType.Serialize, BaseType.LongDeserialize);
            Regist<float>(MessageIdAttribute.FloatID, BaseType.Serialize, BaseType.FloatDeserialize);
            Regist<double>(MessageIdAttribute.DoubleID, BaseType.Serialize, BaseType.DoubleDeserialize);


            //框架用类型
            Regist<HeartBeatsMessage>(MessageIdAttribute.HeartbeatsMessageID,
                HeartBeatsMessage.Seiralizer, HeartBeatsMessage.Deserilizer, KeyAlreadyHave.ThrowException);


            Regist<UdpConnectMessage>(MessageIdAttribute.UdpConnectMessageID,
                UdpConnectMessage.Serialize, UdpConnectMessage.Deserialize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="registSerialize"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Serialize Convert<T>(RegistSerialize<T> registSerialize)
        {
            return (obj, buffer) =>
            {
                if (obj is T message)
                {
                    return registSerialize(message, buffer);
                }
                throw new InvalidCastException(typeof(T).FullName);
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="registSerialize"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Serialize Convert<T>(ValueRegistSerialize<T> registSerialize)
        {
            return (obj, buffer) =>
            {
                if (obj is T message)
                {
                    return registSerialize(in message, buffer);
                }
                throw new InvalidCastException(typeof(T).FullName);
            };
        }

        static readonly Dictionary<int, (Type type, Deserialize deserialize)> dFormatter = new Dictionary<int, (Type type, Deserialize deserialize)>();
        static readonly Dictionary<Type, (int MessageID, Serialize serialize)> sFormatter = new Dictionary<Type, (int MessageID, Serialize serialize)>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="messageID"></param>
        /// <param name="seiralize"></param>
        /// <param name="key"></param>
        protected static void AddSFormatter(Type type, int messageID, Serialize seiralize, KeyAlreadyHave key = KeyAlreadyHave.Skip)
        {
            if (type == null || seiralize == null)
            {
                throw new ArgumentNullException();
            }

            switch (key)
            {
                case KeyAlreadyHave.Replace:
                    sFormatter[type] = (messageID, seiralize);
                    return;
                case KeyAlreadyHave.Skip:
                    if (sFormatter.ContainsKey(type))
                    {
                        return;
                    }
                    else
                    {
                        sFormatter.Add(type, (messageID, seiralize));
                    }
                    break;
                case KeyAlreadyHave.ThrowException:
                default:
                    sFormatter.Add(type, (messageID, seiralize));
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageID"></param>
        /// <param name="type"></param>
        /// <param name="deserilize"></param>
        /// <param name="key"></param>
        protected static void AddDFormatter(int messageID, Type type, Deserialize deserilize, KeyAlreadyHave key = KeyAlreadyHave.Skip)
        {
            if (deserilize == null)
            {
                throw new ArgumentNullException();
            }

            switch (key)
            {
                case KeyAlreadyHave.Replace:
                    dFormatter[messageID] = (type, deserilize);
                    return;
                case KeyAlreadyHave.Skip:
                    if (dFormatter.ContainsKey(messageID))
                    {
                        Debug.LogWarning($"[{type.FullName}]和[{dFormatter[messageID].type.FullName}]的消息ID[{messageID}]冲突。");
                        return;
                    }
                    else
                    {
                        dFormatter.Add(messageID, (type, deserilize));
                    }
                    break;
                case KeyAlreadyHave.ThrowException:
                default:
                    dFormatter.Add(messageID, (type, deserilize));
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="messageID"></param>
        /// <param name="seiralize"></param>
        /// <param name="deserilize"></param>
        /// <param name="key"></param>
        public static void Regist(Type type, int messageID, Serialize seiralize, Deserialize deserilize, KeyAlreadyHave key = KeyAlreadyHave.Skip)
        {
            AddSFormatter(type, messageID, seiralize, key);
            AddDFormatter(messageID, type, deserilize, key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageID"></param>
        /// <param name="seiralize"></param>
        /// <param name="deserilize"></param>
        /// <param name="key"></param>
        public static void Regist<T>(int messageID, RegistSerialize<T> seiralize, Deserialize deserilize, KeyAlreadyHave key = KeyAlreadyHave.Skip)
        {
            AddSFormatter(typeof(T), messageID, Convert(seiralize), key);
            AddDFormatter(messageID, typeof(T), deserilize, key);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageID"></param>
        /// <param name="seiralize"></param>
        /// <param name="deserilize"></param>
        /// <param name="key"></param>
        public static void Regist<T>(int messageID, ValueRegistSerialize<T> seiralize, Deserialize deserilize, KeyAlreadyHave key = KeyAlreadyHave.Skip)
        {
            AddSFormatter(typeof(T), messageID, Convert(seiralize), key);
            AddDFormatter(messageID, typeof(T), deserilize, key);
        }



        /// <summary>
        /// </summary>
        /// <param name="buffer16384"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"> 消息长度大于8192 - 25(框架用长度),请拆分发送。"</exception>
        /// <remarks>框架中TCP接收最大支持8192，所以发送也不能大于8192，为了安全起见，框架提供的字节数组长度是16384的。</remarks>
        public static (int messageID, ushort length)
            Serialize(object message, Span<byte> buffer16384)
        {
            var type = message.GetType();
            if (sFormatter.TryGetValue(type, out var sf))
            {
                //序列化消息
                var (MessageID, Seiralize) = sf;

                if (Seiralize == null)
                {
                    Debug.LogError($"消息[{type.Name}]的序列化函数没有找到。");
                    return (MessageIdAttribute.ErrorType, default);
                }

                ushort length = Seiralize(message, buffer16384);

                //if (length > 8192 - 25)
                //{
                //    //BufferPool.Push16384(buffer16384);
                //    ///消息过长
                //    throw new ArgumentOutOfRangeException(
                //        $"The message length is greater than {8192 - 25}," +
                //        $" Please split to send./" +
                //        $"消息长度大于{8192 - 25}," +
                //        $"请拆分发送。");
                //}

                return (MessageID, length);
            }
            else
            {
                Debug.LogError($"消息[{type.Name}]的序列化函数没有找到。");
                return (MessageIdAttribute.ErrorType, default);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageID"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Deserialize(int messageID, in ReadOnlyMemory<byte> body)
        {
            if (dFormatter.ContainsKey(messageID))
            {
                return dFormatter[messageID].deserialize(body);
            }
            else
            {
                Debug.LogError($"消息ID为[{messageID}]的反序列化函数没有找到。");
                return null;
            }
        }

        /// <summary>
        /// 查找消息类型
        /// </summary>
        /// <param name="messageID"></param>
        /// <returns></returns>
        public static Type GetType(int messageID)
        {
            if (dFormatter.TryGetValue(messageID, out var res))
            {
                return res.type;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool TryGetType(int messageID, out Type type)
        {
            if (dFormatter.TryGetValue(messageID, out var res))
            {
                type = res.type;
                return true;
            }
            else
            {
                type = null;
                return false;
            }
        }

        /// <summary>
        /// 查找消息ID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static int? GetID<T>()
        {
            if (sFormatter.TryGetValue(typeof(T), out var res))
            {
                return res.MessageID;
            }
            return null;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <typeparam name="T"></typeparam>
       /// <param name="ID"></param>
       /// <returns></returns>
        public static bool TryGetID<T>(out int ID)
        {
            if (sFormatter.TryGetValue(typeof(T), out var res))
            {
                ID = res.MessageID;
                return true;
            }

            ID = MessageIdAttribute.ErrorType;
            return false;
        }
    }


    internal static class Debug
    {
        const string moduleName = "Megumin.MessageLUT";
        public static void Log(object message)
            => MeguminDebug.Log(message, moduleName);

        public static void LogError(object message)
            => MeguminDebug.LogError(message, moduleName);

        public static void LogWarning(object message)
            => MeguminDebug.LogWarning(message, moduleName);
    }
}
