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
        



        public virtual ValueTask<object> ReceiveCallback(int messageId, object message, IReceiveMessage receiver)
        {
            try
            {
                if (this.DicCallback.TryGetValue(messageId, out var callback))
                {
                    callback?.Invoke(messageId, message, receiver);
                }
                else
                {
                    this.OnNotFound(messageId, message);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return this.OnException(ex);
            }
            return default;
        }

        protected virtual ValueTask<object> OnException(Exception ex)
        {
            return default;
        }

        protected virtual ValueTask<object> OnNotFound(int messageId, object message)
        {
            return default;
        }

    }
}
