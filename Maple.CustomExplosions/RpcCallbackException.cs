using System;

namespace Maple.CustomExplosions
{

    public class RpcCallbackException : Exception
    {
        public  virtual object Result { get;}

    }




    public class RpcCallbackException<T> : RpcCallbackException where T : IRpcCallbackResult, new()
    {
        public   T Error { get; }

        public override object Result => Error;

        public RpcCallbackException()
        {
            this.Error = new T
            {
                Code = EnumRpcCallbackResultStatus.Error
            };
        }

        public RpcCallbackException(int msgid)
        {
            this.Error = new T
            {
                Code = EnumRpcCallbackResultStatus.Error,
                MessgeId = msgid
            };
        }

    }
}
