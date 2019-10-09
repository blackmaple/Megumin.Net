using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Megumin.Remote
{
    /// <summary>
    /// 
    /// </summary>
    public class UdpNetClient:IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public UdpClient Udp { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="family"></param>
        public UdpNetClient(AddressFamily family = AddressFamily.InterNetwork)
        {

            Udp = new UdpClient(family);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            this.Udp.Dispose();
        }
    }
}
