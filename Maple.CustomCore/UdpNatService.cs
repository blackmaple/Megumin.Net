using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Maple.CustomCore
{
    public class UdpNatService : UdpNatBase
    {
        public UdpNatService(int port) : base(port, AddressFamily.InterNetwork)
        { 
        
        }
    }
}
