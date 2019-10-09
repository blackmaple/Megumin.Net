﻿using Megumin.Message;
using Net.Remote;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Megumin.Remote
{
    /// <summary>
    /// 
    /// </summary>
    public class TcpRemoteListener
    {
        /// <summary>
        /// 
        /// </summary>
        private TcpListener tcpListener;
        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint ConnectIPEndPoint { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public EndPoint RemappedEndPoint { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public TcpRemoteListener(int port)
        {
            this.ConnectIPEndPoint = new IPEndPoint(IPAddress.None,port);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task<Socket> Accept()
        {
            if (tcpListener == null)
            {
                //同时支持IPv4和IPv6
                tcpListener = TcpListener.Create(ConnectIPEndPoint.Port);

                tcpListener.AllowNatTraversal(true);
            }

            tcpListener.Start();
          //  Socket remoteSocket = null;
            try
            {
                //此处有远程连接拒绝异常
                return tcpListener.AcceptSocketAsync();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e);
                //出现异常重新开始监听
                tcpListener = null;
                return Accept();
            }
        }

        /// <summary>
        ///创建TCPRemote并ReceiveStart
        /// </summary>
        /// <returns></returns>
        public async Task<TcpRemote> ListenAsync(ReceiveCallback receiveHandle)
        {
            var remoteSocket = await Accept();
            var remote = new TcpRemote(remoteSocket);
            remote.MessagePipeline = MessagePipeline.Default;
            remote.OnReceiveCallback += receiveHandle;
            remote.ReceiveStart();
            return remote;
        }

        /// <summary>
        /// 创建TCPRemote并ReceiveStart.在ReceiveStart调用之前设置pipline,以免设置不及时漏掉消息.
        /// </summary>
        /// <param name="receiveHandle"></param>
        /// <param name="pipline"></param>
        /// <returns></returns>
        public async Task<TcpRemote> ListenAsync(ReceiveCallback receiveHandle, IMessagePipeline pipline)
        {
            var remoteSocket = await Accept();
            var remote = new TcpRemote(remoteSocket);
            remote.MessagePipeline = pipline;
            remote.OnReceiveCallback += receiveHandle;
            remote.ReceiveStart();
            return remote;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop() => tcpListener?.Stop();
    }
}
