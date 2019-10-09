﻿using Megumin.Message;
using Net.Remote;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Megumin.Remote
{
    /// <summary>
    /// 不支持多播地址 每包大小最好不要大于 537（548 - 框架报头11）
    /// </summary>
    public partial class UdpRemote : RemoteBase, IRemote
    {
        /// <summary>
        /// 
        /// </summary>
        public Socket Client => udpClient?.Client;

        /// <summary>
        /// 
        /// </summary>
        public UdpClient udpClient;

        /// <summary>
        /// 
        /// </summary>
        public EndPoint RemappedEndPoint => udpClient?.Client.RemoteEndPoint;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addressFamily"></param>
        public UdpRemote(AddressFamily addressFamily = AddressFamily.InterNetworkV6) :
            this(new UdpClient(0, addressFamily))
        {

        }

        internal UdpRemote(UdpClient udp)
        {
            udpClient = udp;
            IsVaild = true;
        }

        void OnSocketException(SocketError error)
        {
            udpClient?.Close();
            OnDisConnect?.Invoke(error);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                    udpClient.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。
                IsVaild = false;
                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~UDPRemote2() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion

    }

    ///连接
    partial class UdpRemote
    {
        /// <summary>
        /// 
        /// </summary>
        public event Action<SocketError> OnDisConnect;

        bool isConnecting = false;

        /// <summary>
        /// IPv4 和 IPv6不能共用
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="retryCount"></param>
        /// <returns></returns>
        public async Task<Exception> ConnectAsync(IPEndPoint endPoint, int retryCount = 0)
        {
            if (isConnecting)
            {
                return new Exception("连接正在进行中");
            }
            isConnecting = true;

            if (this.Client.AddressFamily != endPoint.AddressFamily)
            {
                //IP版本转换
                this.ConnectIPEndPoint = new IPEndPoint(
                    this.Client.AddressFamily == AddressFamily.InterNetworkV6 ? endPoint.Address.MapToIPv6() :
                    endPoint.Address.MapToIPv4(), endPoint.Port);
            }
            else
            {
                this.ConnectIPEndPoint = endPoint;
            }

            while (retryCount >= 0)
            {
                try
                {
                    var res = await this.ConnectAsync();
                    if (res)
                    {
                        isConnecting = false;
                        ReceiveStart();
                        return null;
                    }
                }
                catch (Exception e)
                {
                    if (retryCount <= 0)
                    {
                        isConnecting = false;
                        return e;
                    }
                }
                finally
                {
                    retryCount--;
                }
            }

            isConnecting = false;
            return new SocketException((int)SocketError.TimedOut);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Disconnect()
        {
            IsVaild = false;
            manualDisconnecting = true;
            udpClient?.Close();
        }

        int lastseq;
        int lastack;
        async Task<bool> ConnectAsync()
        {
            lastseq = new Random().Next(0, 10000);
            var buffer = MakeUDPConnectMessage(1, 0, lastseq, lastack);
            CancellationTokenSource source = new CancellationTokenSource();
            TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();

#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法

            Task.Run(async () =>
            {
                while (true)
                {
                    var recv = await udpClient.ReceiveAsync();
                    var (Size, MessageID) = Message.MessagePipeline.Default.ParsePacketHeader(recv.Buffer);
                    if (MessageID == EnumMessgaeId.UdpConnectMessageID)
                    {
                        var (SYN, ACK, seq, ack) = ReadConnectMessage(recv.Buffer);
                        if (SYN == 1 && ACK == 1 && lastseq + 1 == ack)
                        {
                            //ESTABLISHED

                            udpClient.Connect(recv.RemoteEndPoint);
                            break;
                        }
                    }
                }
                source.Cancel();
                taskCompletion.SetResult(true);

            }, source.Token);


            Task.Run(async () =>
            {
                while (true)
                {
                    await udpClient.SendAsync(buffer,buffer.Length, ConnectIPEndPoint);
                    await Task.Delay(1000);
                }
            }, source.Token);

            Task.Run(async () =>
            {
                await Task.Delay(5000, source.Token);
                //一段时间没有反应，默认失败。
                source.Cancel();
                taskCompletion.TrySetException(new TimeoutException());

            }, source.Token);


#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法

            return await taskCompletion.Task;
        }


        internal async Task<bool> TryAccept(UdpReceiveResult udpReceive)
        {
            if (Client.Connected && this.Client.RemoteEndPoint.Equals(udpReceive.RemoteEndPoint))
            {
                //已经成功连接，忽略连接请求
                return true;
            }

            //LISTEN;
            var (SYN, ACK, seq, ack) = ReadConnectMessage(udpReceive.Buffer);

            if (SYN == 1 && ACK == 0)
            {
                //SYN_RCVD;
                lastack = new Random().Next(0, 10000);
                lastseq = seq;

                ConnectIPEndPoint = udpReceive.RemoteEndPoint;

                //绑定远端
                udpClient.Connect(udpReceive.RemoteEndPoint);
                var buffer = MakeUDPConnectMessage(1, 1, lastack, seq + 1);

#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                Task.Run(async () =>
                {
                    for (int i = 0; i < 3; i++)
                    {
                        udpClient.Send(buffer,buffer.Length);
                        await Task.Delay(800);
                    }
                });
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法

                return true;
            }
            else
            {
                //INVALID;
                return false;
            }
        }

        static (int SYN, int ACK, int seq, int ack) ReadConnectMessage(byte[] buffer)
        {
            ReadOnlySpan<byte> bf = buffer.AsSpan(11);

            int SYN = bf.ReadInt();
            int ACK = bf.Slice(4).ReadInt();
            int seq = bf.Slice(8).ReadInt();
            int ack = bf.Slice(12).ReadInt();
            return (SYN, ACK, seq, ack);
        }

        static byte[] MakeUDPConnectMessage(int SYN, int ACT, int seq, int ack)
        {
            var bf = new byte[27];
            ((ushort)27).WriteTo(bf);
            ((int)EnumMessgaeId.UdpConnectMessageID).WriteTo(bf.AsSpan(2));
            bf[6] = 1;
            bf[7] = 0;
            bf[8] = 0;
            bf[9] = 0;
            bf[10] = 0;
            int tempOffset = 11;
            SYN.WriteTo(bf.AsSpan(tempOffset));
            ACT.WriteTo(bf.AsSpan(tempOffset + 4));
            seq.WriteTo(bf.AsSpan(tempOffset + 8));
            ack.WriteTo(bf.AsSpan(tempOffset + 12));
            return bf;
        }
    }

    /// 发送
    partial class UdpRemote
    {
        /// <summary>
        /// 注意，发送完成时内部回收了buffer。
        /// ((框架约定1)发送字节数组发送完成后由发送逻辑回收)
        /// </summary>
        /// <param name="bufferMsg"></param>
        public override void SendAsync(IMemoryOwner<byte> bufferMsg)
        {
            try
            {
                Task.Run(() =>
                        {
                            try
                            {
                                if (MemoryMarshal.TryGetArray<byte>(bufferMsg.Memory, out var sbuffer))
                                {
                                    udpClient.Send(sbuffer.Array, sbuffer.Count);
                                }
                            }
                            finally
                            {
                                bufferMsg.Dispose();
                            }
                        });
            }
            catch (SocketException e)
            {
                if (!manualDisconnecting)
                {
                    OnSocketException(e.SocketErrorCode);
                }
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgBuffer"></param>
        /// <returns></returns>
        public Task BroadCastSendAsync(ArraySegment<byte> msgBuffer)
        {
            if (msgBuffer.Offset == 0)
            {
                return udpClient.SendAsync(msgBuffer.Array, msgBuffer.Count);
            }

            //此处几乎用不到，省掉一个async。
            var buffer = new byte[msgBuffer.Count];
            Buffer.BlockCopy(msgBuffer.Array, msgBuffer.Offset, buffer, 0, msgBuffer.Offset);
            return udpClient.SendAsync(buffer, msgBuffer.Count);
        }
    }

    /// 接收
    partial class UdpRemote
    {
        /// <summary>
        /// 
        /// </summary>
        public bool isReceiving = false;

        /// <summary>
        /// 
        /// </summary>
        public override void ReceiveStart()
        {
            if (!Client.Connected || isReceiving)
            {
                return;
            }
            ReceiveAsync(BufferPool.Rent(MaxBufferLength));
        }

        async void ReceiveAsync(IMemoryOwner<byte> buffer)
        {
            if (!Client.Connected || disposedValue)
            {
                return;
            }

            try
            {
                isReceiving = true;
                if (MemoryMarshal.TryGetArray<byte>(buffer.Memory,out var receiveBuffer) )
                {
                    var res = await udpClient.ReceiveAsync(receiveBuffer);
                    LastReceiveTime = DateTime.Now;
                    if (IsVaild)
                    {
                        //递归，继续接收
                        ReceiveAsync(BufferPool.Rent(MaxBufferLength));
                    }

                    DealMessageAsync(buffer);
                }
                else
                {
                    throw new ArgumentException();
                }
                
            }
            catch (SocketException e)
            {
                if (!manualDisconnecting)
                {
                    OnSocketException(e.SocketErrorCode);
                }
                isReceiving = false;
            }
        }

        private void DealMessageAsync(IMemoryOwner<byte> byteOwner)
        {
            Task.Run(() =>
            {
                ReceiveByteMessage(byteOwner);
            });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class UDPClientEx_102F7D01C985465EB23822F83FDE9C75
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="udp"></param>
        /// <param name="receiveBuffer"></param>
        /// <returns></returns>
        public static Task<UdpReceiveResult_E74D> ReceiveAsync(this UdpClient udp, ArraySegment<byte> receiveBuffer)
        {
            return Task<UdpReceiveResult_E74D>.Factory.FromAsync(
                (callback, state) => ((UdpClient)state).BeginReceive(callback, state, receiveBuffer),
                asyncResult =>
                {
                    var client = (UdpClient)asyncResult.AsyncState;
                    IPEndPoint remoteEP = null;
                    int length = client.EndReceive2(asyncResult, ref remoteEP);
                    var resbuffer = new ArraySegment<byte>(receiveBuffer.Array, receiveBuffer.Offset, length);
                    return new UdpReceiveResult_E74D(resbuffer, remoteEP);
                },
                state: udp);
        }

        /// <summary>
        /// 
        /// </summary>
        internal static class IPEndPointStatics_9931EFCAB48741B998C533DF851CB575
        {
            internal const int AnyPort = IPEndPoint.MinPort;
            internal static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
            internal static readonly IPEndPoint IPv6Any = new IPEndPoint(IPAddress.IPv6Any, AnyPort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="udp"></param>
        /// <param name="requestCallback"></param>
        /// <param name="state"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static IAsyncResult BeginReceive(this UdpClient udp, AsyncCallback requestCallback, object state, ArraySegment<byte> buffer)
        {
            // Due to the nature of the ReceiveFrom() call and the ref parameter convention,
            // we need to cast an IPEndPoint to its base class EndPoint and cast it back down
            // to IPEndPoint.
            EndPoint tempRemoteEP;
            if (udp.Client.AddressFamily == AddressFamily.InterNetwork)
            {
                tempRemoteEP = IPEndPointStatics_9931EFCAB48741B998C533DF851CB575.Any;
            }
            else
            {
                tempRemoteEP = IPEndPointStatics_9931EFCAB48741B998C533DF851CB575.IPv6Any;
            }

            return udp.Client.BeginReceiveFrom(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, ref tempRemoteEP, requestCallback, state);
        }

        static int EndReceive2(this UdpClient udp, IAsyncResult asyncResult, ref IPEndPoint remoteEP)
        {
            EndPoint tempRemoteEP;
            if (udp.Client.AddressFamily == AddressFamily.InterNetwork)
            {
                tempRemoteEP = IPEndPointStatics_9931EFCAB48741B998C533DF851CB575.Any;
            }
            else
            {
                tempRemoteEP = IPEndPointStatics_9931EFCAB48741B998C533DF851CB575.IPv6Any;
            }

            int received = udp.Client.EndReceiveFrom(asyncResult, ref tempRemoteEP);
            remoteEP = (IPEndPoint)tempRemoteEP;

            return received;
        }
    }

    /// <summary>
    /// https://source.dot.net/#System.Net.Sockets/System/Net/Sockets/UdpReceiveResult.cs,3adcfc441b5a5fd9
    /// Presents UDP receive result information from a call to the <see cref="UDPClientEx_102F7D01C985465EB23822F83FDE9C75.ReceiveAsync(UdpClient, ArraySegment{byte})"/> method
    /// </summary>
    public struct UdpReceiveResult_E74D : IEquatable<UdpReceiveResult_E74D>
    {
        private ArraySegment<byte> _buffer;
        private IPEndPoint _remoteEndPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpReceiveResult"/> class
        /// </summary>
        /// <param name="buffer">A buffer for data to receive in the UDP packet</param>
        /// <param name="remoteEndPoint">The remote endpoint of the UDP packet</param>
        public UdpReceiveResult_E74D(ArraySegment<byte> buffer, IPEndPoint remoteEndPoint)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (remoteEndPoint == null)
            {
                throw new ArgumentNullException(nameof(remoteEndPoint));
            }

            _buffer = buffer;
            _remoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        /// Gets a buffer with the data received in the UDP packet
        /// </summary>
        public ArraySegment<byte> Buffer
        {
            get
            {
                return _buffer;
            }
        }

        /// <summary>
        /// Gets the remote endpoint from which the UDP packet was received
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return _remoteEndPoint;
            }
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return (_buffer != null) ? (_buffer.GetHashCode() ^ _remoteEndPoint.GetHashCode()) : 0;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object
        /// </summary>
        /// <param name="obj">The object to compare with this instance</param>
        /// <returns>true if obj is an instance of <see cref="UdpReceiveResult"/> and equals the value of the instance; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is UdpReceiveResult))
            {
                return false;
            }

            return Equals((UdpReceiveResult)obj);
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object
        /// </summary>
        /// <param name="other">The object to compare with this instance</param>
        /// <returns>true if other is an instance of <see cref="UdpReceiveResult"/> and equals the value of the instance; otherwise, false</returns>
        public bool Equals(UdpReceiveResult_E74D other)
        {
            return object.Equals(_buffer, other._buffer) && object.Equals(_remoteEndPoint, other._remoteEndPoint);
        }

        /// <summary>
        /// Tests whether two specified <see cref="UdpReceiveResult"/> instances are equivalent
        /// </summary>
        /// <param name="left">The <see cref="UdpReceiveResult"/> instance that is to the left of the equality operator</param>
        /// <param name="right">The <see cref="UdpReceiveResult"/> instance that is to the right of the equality operator</param>
        /// <returns>true if left and right are equal; otherwise, false</returns>
        public static bool operator ==(UdpReceiveResult_E74D left, UdpReceiveResult_E74D right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests whether two specified <see cref="UdpReceiveResult"/> instances are not equal
        /// </summary>
        /// <param name="left">The <see cref="UdpReceiveResult"/> instance that is to the left of the not equal operator</param>
        /// <param name="right">The <see cref="UdpReceiveResult"/> instance that is to the right of the not equal operator</param>
        /// <returns>true if left and right are unequal; otherwise, false</returns>
        public static bool operator !=(UdpReceiveResult_E74D left, UdpReceiveResult_E74D right)
        {
            return !left.Equals(right);
        }
    }
}