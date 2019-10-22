using Maple.CustomStandard;
using NetRemoteStandard;
using System;

namespace Maple.CustomExplosions
{

    public class RpcCallbackException : Exception
    {
        public IRpcCallbackResult Result { get; }

        public RpcCallbackException(IRpcCallbackResult result)
        {

        }

    }




    public class RpcCallbackException<T> : RpcCallbackException where T : IRpcCallbackResult, new()
    {


        public RpcCallbackException() : base(new T
        {
            Code = EnumRpcCallbackResultStatus.Error
        })
        {

        }

        public RpcCallbackException(int msgid) : base(new T
        {
            Code = EnumRpcCallbackResultStatus.Error,
            MessgeId = msgid
        })
        {

        }

    }
}
