using System;

namespace Maple.CustomExplosions
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackIdAttribute  :Attribute 
    {
        public CallbackIdAttribute(int messageId) : base()
        { 
        
        }

    }
 
}
