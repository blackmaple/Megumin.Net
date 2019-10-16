using Maple.CustomStandard;
using Megumin.Message;
using Net.Remote;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Maple.CustomCore
{
    public class UdpNatBase : ISendMessage, IAsyncSendMessage, IDisposable
    {
        UdpClient Udp { get; }

        public IMessagePipeline MessagePipeline { get; set; } = Megumin.Message.MessagePipeline.Default;



        public UdpNatBase(AddressFamily addressFamily)
        {
            this.Udp = new UdpClient(addressFamily);
            this.Udp.AllowNatTraversal(true);
        }
        public UdpNatBase(int port, AddressFamily addressFamily)
        {
            this.Udp = new UdpClient(port, addressFamily);
            this.Udp.AllowNatTraversal(true);
        }




        public void Dispose()
        {
            this.Udp.Dispose();
        }

        public void SendAsync(object message)
        {
            throw new NotImplementedException();
        }

        public void SendAsync(IMemoryOwner<byte> byteMessage)
        {
            throw new NotImplementedException();
        }

        public IMiniAwaitable<(RpcResult result, Exception exception)> SendAsync<RpcResult>(object message)
        {
            throw new NotImplementedException();
        }

        public IMiniAwaitable<RpcResult> SendAsyncSafeAwait<RpcResult>(object message, Action<Exception> OnException = null)
        {
            throw new NotImplementedException();
        }

        public IMiniAwaitable<RpcResult> MapleSendAsync<RpcResult>(object message) where RpcResult : IRpcCallbackResult, new()
        {
            throw new NotImplementedException();
        }

        public IMiniAwaitable<RpcResult> MapleSendAsync<RpcResult>(object message, int rpcTimeOutMilliseconds) where RpcResult : IRpcCallbackResult, new()
        {
            throw new NotImplementedException();
        }
    }
}
