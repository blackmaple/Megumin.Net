using Megumin.Message;
using Message;
using Net.Remote;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Megumin.Remote
{
    /// <summary>
    /// 
    /// </summary>
    public class ReceiveCallbackMgr : IReceiveCallbackMgr
    {
        private Dictionary<EnumMessgaeId, ReceiveCallback> DicCallback = new Dictionary<EnumMessgaeId, ReceiveCallback>();
        
        public ValueTask<object> ReceiveCallback(EnumMessgaeId messgaeId, object message, IReceiveMessage receiver)
        {
           
            return default;
        }

        [MsgId(EnumMessgaeId.LongID)]
        public ValueTask<Login2Gate> Login(EnumMessgaeId messgaeId, Login message, IReceiveMessage receiver)
        {
            return default;
        }


        public enum EnumCustomMessageId : EnumMessgaeId
        { 
           
        }
        public class Enumxxx :Enum
    }
}
