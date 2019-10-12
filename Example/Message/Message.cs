using System;
using MessagePack;
using Megumin.Message;
using ProtoBuf;
using Maple.CustomExplosions;

namespace Message
{

    /// <summary>
    /// 
    /// </summary>
    [MessageId(1000)]
    [ProtoContract]
    [MessagePackObject]
    public class Message
    {
    }

    /// <summary>
    /// 
    /// </summary>
    [MessageId(1001)]
    [ProtoContract]
    [MessagePackObject]
    public class Login
    {
        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        [Key(0)]
        public string IP { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessageId(1002)]
    [ProtoContract]
    [MessagePackObject]
    public class LoginResult
    {
        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        [Key(0)]
        public string TempKey { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessageId(1003)]
    [ProtoContract]
    [MessagePackObject]
    public class Login2Gate
    {
        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        [Key(0)]
        public string Account { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(2)]
        [Key(1)]
        public string Password { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessageId(1004)]
    [ProtoContract]
    [MessagePackObject]
    public class Login2GateResult: DefRpcCallbackResult
    {

        [Key(3)]
        [ProtoMember(1)]
        public string M { set; get; }

    }

    /// <summary>
    /// 所有的RPC调用应该继承这个
    /// </summary>
    
    [ProtoContract]
    [ProtoInclude(3,typeof(Login2GateResult))]
    [MessagePackObject]
    public class DefRpcCallbackResult : IRpcCallbackResult
    {
        [ProtoMember(1)]
        [Key(0)]
        public EnumRpcCallbackResultStatus Code { set; get; }
        [ProtoMember(2)]
        [Key(1)]
        public int MessgeId { set; get; }

    }




}
