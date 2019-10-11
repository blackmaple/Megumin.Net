using Maple.CustomExplosions;
using Net.Remote;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maple.CustomCore
{
    /// <summary>
    /// 
    /// </summary>
    public class ReceiveCallbackMgr : IReceiveCallbackMgr
    {
        private Dictionary<int, ReceiveCallback> DicCallback = new Dictionary<int, ReceiveCallback>();
        



        public virtual ValueTask<object> ReceiveCallback(int messageId,int rpcId, object message, IReceiveMessage receiver)
        {
            try
            {
                if (this.DicCallback.TryGetValue(messageId, out var callback))
                {
                    callback?.Invoke(messageId, rpcId, message, receiver);
                }
                else
                {
                   
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
              
            }
            return default;
        }

        protected virtual ValueTask<object> OnException(int messageId, int rpcId, object message, Exception ex)
        {
            return default;
        }

        protected virtual ValueTask<object> OnNotFound(int messageId, int rpcId, object message )
        {
            return default;
        }

    }
}
