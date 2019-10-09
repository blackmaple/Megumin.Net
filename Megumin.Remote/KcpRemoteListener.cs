using System;
using System.Collections.Generic;
using System.Text;
using Net.Remote;

namespace Megumin.Remote
{
    /// <summary>
    /// 
    /// </summary>
    public class KcpRemoteListener:IMultiplexing
    {
        /// <summary>
        /// 
        /// </summary>
        public int MultiplexingCount { get; set; } = 1;
    }
}
