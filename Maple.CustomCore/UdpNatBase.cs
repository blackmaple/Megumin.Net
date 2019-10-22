using Maple.CustomStandard;
using Megumin.Message;
using Net.Remote;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Maple.CustomCore
{
    public class UdpNatBase : IDisposable
    {
        UdpClient Udp { get; }

        public IMessagePipeline MessagePipeline { get; set; } = Megumin.Message.MessagePipeline.Default;

        public bool IsListening { protected set; get; }

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

        public void Start()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                AcceptAsync();
            });
        }

        public void Close()
        {
            this.IsListening = false;
            this.Udp.Dispose();
        }

        async void AcceptAsync()
        {
            while (IsListening)
            {
                var data = await this.Udp.ReceiveAsync();
                
            }
        }

        public void Dispose()
        {
            this.Close();
        }

        public async ValueTask<int> SendAsync(object message, params IPEndPoint[] ips)
        {
            if (ips == null)
            {
                throw new ArgumentNullException(nameof(ips));
            }
            var length = ips.Length;
            if (length == 0)
            {
                throw new ArgumentNullException(nameof(ips));
            }

            using (var owner = MessagePipeline.Pack(0, message))
            {
                if (MemoryMarshal.TryGetArray<byte>(owner.Memory, out var buffer) == false)
                {
                    return 0;
                }
                var tasks = new Task<int>[ips.Length];
                var data = buffer.Array;
                var count = buffer.Count;
                for (int i = 0; i < length; ++i)
                {
                    tasks[i] = this.Udp.SendAsync(data, count, ips[i]);
                }
                var retData = await Task.WhenAll(tasks);
                return retData.Sum();
            }
        }


        public virtual void OnSocketException(SocketError error)
        {

        }
    }
}
