using System;
using System.Collections.Generic;
using System.Text;

namespace Megumin.Message
{
    /// <summary>
    /// 使用MessageID来为每一个消息指定一个唯一ID(-999~999 被框架占用)。
    /// 请查看常量。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method)]
    public sealed class MsgIdAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messgaeId"></param>
        public MsgIdAttribute(EnumMessgaeId  messgaeId)
        {
            this.ID = messgaeId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messgaeId"></param>
        public MsgIdAttribute(int messgaeId)
        {
            this.ID = (EnumMessgaeId)messgaeId;
        }

        /// <summary>
        /// 消息类唯一编号
        /// </summary>
        public EnumMessgaeId ID { get; }



        //public const int TestPacket1ID = -101;
        //public const int TestPacket2ID = -102;
        ///// <summary>
        ///// 错误的类型，表示框架未记录的类型。不是void，也不是任何异常ErrorType。
        ///// </summary>
        //public const int ErrorType = -1;
        //public const int StringID = 11;
        //public const int IntID = 12;
        //public const int FloatID = 13;
        //public const int LongID = 14;
        //public const int DoubleID = 15;
        ///// <summary>
        ///// Udp握手连接使用的消息ID编号
        ///// </summary>
        //public const int UdpConnectMessageID = 101;
        ///// <summary>
        ///// 心跳包ID，255好识别，buffer[2-5]=[255,0,0,0]
        ///// </summary>
        //public const int HeartbeatsMessageID = 255;
    }

    /// <summary>
    /// 使用MessageID来为每一个消息指定一个唯一ID(-999~999 被框架占用)。
    /// </summary>
    public enum EnumMessgaeId:int
    {
        /// <summary>
        /// 
        /// </summary>
        PlaceholderBegin = -999,
        /// <summary>
        /// 
        /// </summary>
        TestPacket1ID = -101,
        /// <summary>
        /// 
        /// </summary>
        TestPacket2ID = -102,

        /// <summary>
        /// 框架未记录的类型。不是void，也不是任
        /// </summary>
        ErrorType = -1,

        /// <summary>
        /// 
        /// </summary>
        StringID = 11,

        /// <summary>
        /// 
        /// </summary>
        IntID = 12,

        /// <summary>
        /// 
        /// </summary>
        FloatID = 13,

        /// <summary>
        /// 
        /// </summary>
        LongID = 14,

        /// <summary>
        /// 
        /// </summary>
        DoubleID = 15,

        /// <summary>
        /// Udp握手连接使用的消息ID编号
        /// </summary>
        UdpConnectMessageID = 101,

        /// <summary>
        /// 心跳包ID，255好识别，buffer[2-5]=[255,0,0,0]
        /// </summary>
        HeartbeatsMessageID = 255,

        /// <summary>
        /// 
        /// </summary>
        PlaceholderEnd = 999,

    }

}
