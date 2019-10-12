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
            this.ID = messageId;
        }

        /// <summary>
        /// 消息类唯一编号
        /// </summary>
        public int ID { get; }


    }

}
