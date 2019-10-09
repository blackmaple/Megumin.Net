using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Maple.CustomCore
{
    public class UdpNatService
    {
        UdpClient  Udp { get; }
        public UdpNatService(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            this.Udp = new UdpClient(addressFamily);
        }
    }
}
