﻿using Maple.CustomCore;
using Megumin.DCS;
using Megumin.Remote;
using Message;
using Net.Remote;
using System;
using System.Threading.Tasks;

namespace ServerApp
{
    internal class GateService : IService
    {
        public int GUID { get; set; }

        TcpRemoteListener listener = new TcpRemoteListener(Config.MainPort);

        public void Start()
        {
            StartListenAsync();
        }

        ReceiveCallbackMgr ReceiveCallbackMgr = new ReceiveCallbackMgr();

        public async void StartListenAsync()
        {
            var remote = await listener.ListenAsync(new ReceiveCallbackMgr());
            Console.WriteLine($"建立连接");
            StartListenAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="message"></param>
        /// <param name="receiver"></param>
        /// <returns></returns>
        //public static async ValueTask<object> DealMessage(int messageId , object message, IReceiveMessage receiver)
        //{
        //    switch (message)
        //    {
        //        case string str:
        //            Console.WriteLine(str);
        //            break;
        //        case Login2Gate login:
        //            Console.WriteLine($"客户端登陆请求：{login.Account}-----{login.Password}");

        //            Login2GateResult resp = new Login2GateResult();
        //            resp.Code =  EnumRpcCallbackResultStatus.Success;
        //            return resp;
        //        default:
        //            break;
        //    }
        //    return null;
        //}

        public void Update(double deltaTime)
        {

        }



    }
}