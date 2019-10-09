using System;
using MessagePack;
using Megumin.Message;
using ProtoBuf;

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
    public class Login2GateResult
    {
        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        [Key(0)]
        public bool IsSuccess { get; set; }
    }
}
