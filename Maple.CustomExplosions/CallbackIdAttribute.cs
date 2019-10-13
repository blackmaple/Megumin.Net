using Megumin.Message;
using System;

namespace Maple.CustomExplosions
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackIdAttribute : MessageIdAttribute
    {
        public CallbackIdAttribute(int id) : base(id)
        {

        }


        public CallbackIdAttribute(
            int id,
            Type argument
            , Type resultType) : this(id)
        {
            this.Argument = argument;
            this.ResultType = resultType;
        }

        public Type Argument { get; }
        public Type ResultType { get; }


    }

}
