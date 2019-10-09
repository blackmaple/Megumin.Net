﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Megumin.Message
{
    /// <summary>
    /// 心跳包消息
    /// </summary>
    public class HeartBeatsMessage
    {
        /// <summary>
        /// 发送时间，服务器收到心跳包原样返回；
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="heartBeats"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static ushort Seiralizer(HeartBeatsMessage heartBeats, Span<byte> buffer)
        {
            heartBeats.Time.ToBinary().WriteTo(buffer);
            return sizeof(long);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static HeartBeatsMessage Deserilizer(ReadOnlyMemory<byte> buffer)
        {
            long t = buffer.Span.ReadLong();
            return new HeartBeatsMessage() { Time = new DateTime(t) };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class UdpConnectMessage
    {
        /// <summary>
        /// 
        /// </summary>
        public int SYN;
        /// <summary>
        /// 
        /// </summary>
        public int ACT;
        /// <summary>
        /// 
        /// </summary>
        public int seq;
        /// <summary>
        /// 
        /// </summary>
        public int ack;
        internal static UdpConnectMessage Deserialize(ReadOnlyMemory<byte> buffer)
        {
            int SYN = buffer.Span.ReadInt();
            int ACT = buffer.Span.Slice(4).ReadInt();
            int seq = buffer.Span.Slice(8).ReadInt();
            int ack = buffer.Span.Slice(12).ReadInt();
            return new UdpConnectMessage() { SYN = SYN, ACT = ACT, seq = seq, ack = ack };
        }

        internal static ushort Serialize(UdpConnectMessage connectMessage, Span<byte> bf)
        {
            connectMessage.SYN.WriteTo(bf);
            connectMessage.ACT.WriteTo(bf.Slice(4));
            connectMessage.seq.WriteTo(bf.Slice(8));
            connectMessage.ack.WriteTo(bf.Slice(12));
            return 16;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SYN"></param>
        /// <param name="ACT"></param>
        /// <param name="seq"></param>
        /// <param name="ack"></param>
        public void Deconstruct(out int SYN, out int ACT, out int seq, out int ack)
        {
            SYN = this.SYN;
            ACT = this.ACT;
            seq = this.seq;
            ack = this.ack;
        }
    }

    internal static class BaseType
    {
        internal static ushort Serialize(string message, Span<byte> bf)
        {
            using (var mo = BufferPool.Rent(message.Length * 2))
            {
                MemoryMarshal.TryGetArray<byte>(mo.Memory, out var bs);
                var length = Encoding.UTF8.GetBytes(message,0,message.Length,bs.Array,bs.Offset);
                mo.Memory.Span.Slice(0,length).CopyTo(bf);
                return (ushort)length;
            }
        }

        internal static string StringDeserialize(ReadOnlyMemory<byte> bf)
        {
            var length = bf.Length;
            using (var mo = BufferPool.Rent(length))
            {
                MemoryMarshal.TryGetArray<byte>(mo.Memory, out var bs);
                bf.Span.CopyTo(mo.Memory.Span);
                return Encoding.UTF8.GetString(bs.Array,0, length);
            }
        }

        internal static ushort Serialize(int message, Span<byte> bf)
        {
            message.WriteTo(bf);
            return 4;
        }

        internal static object IntDeserialize(ReadOnlyMemory<byte> bf)
        {
            return bf.Span.ReadInt();
        }

        internal static ushort Serialize(long message, Span<byte> bf)
        {
            message.WriteTo(bf);
            return 8;
        }

        internal static object LongDeserialize(ReadOnlyMemory<byte> bf)
        {
            return bf.Span.ReadLong();
        }

        internal static ushort Serialize(float message, Span<byte> bf)
        {
            BitConverter.GetBytes(message).AsSpan().CopyTo(bf);
            return 4;
        }

        internal static object FloatDeserialize(ReadOnlyMemory<byte> bf)
        {
            byte[] temp = new byte[4];
            bf.Span.Slice(0,4).CopyTo(temp);
            return BitConverter.ToSingle(temp,0);
        }

        internal static ushort Serialize(double message, Span<byte> bf)
        {
            BitConverter.GetBytes(message).AsSpan().CopyTo(bf);
            return 8;
        }

        internal static object DoubleDeserialize(ReadOnlyMemory<byte> bf)
        {
            byte[] temp = new byte[8];
            bf.Span.Slice(0, 8).CopyTo(temp);
            return BitConverter.ToDouble(temp, 0);
        }
    }
}
