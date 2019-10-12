using ProtoBuf;

namespace Maple.CustomExplosions
{
    /// <summary>
    /// RPC的基类应该继承这个接口
    /// </summary>
    public interface IRpcCallbackResult
    {
        EnumRpcCallbackResultStatus Code { set; get; }
        int MessgeId { set; get; }

    }
}
