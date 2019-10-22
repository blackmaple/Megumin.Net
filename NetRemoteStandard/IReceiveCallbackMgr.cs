using Megumin.Message;
using Net.Remote;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetRemoteStandard
{
    /// <summary>
    /// 
    /// </summary>
    public interface IReceiveCallbackMgr
    {
        IMessagePipeline MessagePipeline { get; }
        ValueTask<object> ReceiveCallback(int messageId, object message, IReceiveMessage receiver);
    }
}
