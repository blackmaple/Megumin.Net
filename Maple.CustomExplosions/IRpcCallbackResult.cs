using ProtoBuf;
using System;

namespace Maple.CustomExplosions
{
    /// <summary>
    /// RPC的基类应该继承这个接口
    /// </summary>
    public interface IRpcCallbackResult
    {
        EnumRpcCallbackResultStatus Code { set; get; }
        string Messge { set; get; }

    }

    
 


    public enum EnumRpcCallbackResultStatus
    {
        ServiceError = -2,
        ClientError = -1,
        Success = 0,
    }
}
