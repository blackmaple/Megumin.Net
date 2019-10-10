using System;

namespace Maple.CustomExplosions
{
    /// <summary>
    /// 所有的RPC调用应该继承这个
    /// </summary>
    public class CRpcCallbackDTO
    {
        public int Code { set; get; }
        public string Messge { set; get; }
    }
}
