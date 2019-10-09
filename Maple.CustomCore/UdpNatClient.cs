using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Maple.CustomCore
{
    public class UdpNatClient
    {
        UdpClient  Udp { get; }
        public UdpNatClient(AddressFamily addressFamily)
        {
            this.Udp = new UdpClient(addressFamily);
        }


    }
}
