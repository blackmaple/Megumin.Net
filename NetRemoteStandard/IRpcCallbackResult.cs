namespace NetRemoteStandard
{
    /// <summary>
    /// RPC的基类应该继承这个接口
    /// </summary>
    public interface IRpcCallbackResult: ICustomMessageData
    {
        EnumRpcCallbackResultStatus Code { set; get; }
        int MessgeId { set; get; }

    }
}
