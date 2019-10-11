using ProtoBuf;
using System;

namespace Maple.CustomExplosions
{
    /// <summary>
    /// 所有的RPC调用应该继承这个
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
