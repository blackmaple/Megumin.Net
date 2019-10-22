using Maple.CustomCore;
using Maple.CustomStandard;
using Megumin.Message;
using Megumin.Remote;
using Message;
using Net.Remote;
using NetRemoteStandard;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //decimal.TryParse("               1.03", out var a);
            //HttpClient c = new HttpClient();
            //var str = "1212";
            //var data = new StringContent(str, System.Text.Encoding.UTF8);
            //var t = c.PostAsync("http://localhost:56727/api/boc/Save", data);
            //var s = t.Result.Content.ReadAsStringAsync().Result;


            //将协议类的程序集注册进查找表中
            Protobuf_netLUT.Regist(typeof(Login).Assembly);
            //    Protobuf_netLUT.Regist(typeof(Login).Assembly);

            //建立主线程 或指定的任何线程 轮询。（确保在unity中使用主线程轮询）
            //ThreadScheduler保证网络底层的各种回调函数切换到主线程执行以保证执行顺序。
            ThreadPool.QueueUserWorkItem((A) =>
            {
                while (true)
                {
                    MessageThreadTransducer.Update(0);
                    Thread.Yield();
                }

            });

            ConnectAsync();
            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) break;
                remote.SendAsync(line);
            }
            Console.ReadLine();
        }


        static IRemote remote = new TcpRemote();
        /// <summary>
        /// 连接服务器
        /// </summary>
        private static async void ConnectAsync()
        {

            var ex = await remote.ConnectAsync(new IPEndPoint(IPAddress.IPv6Loopback, 54321));
            remote.ReceiveCallbackMgr = new ReceiveCallbackMgr();
            if (ex == null)
            {
                //没有异常，连接成功
                Console.WriteLine("连接成功");

                //创建一个登陆消息
                var login = new Login2Gate()
                {
                    Account = $"TestClient",
                    Password = "123456"
                };

                //有返回值，这个是一个RPC过程，Exception在网络中传递
                var resp = await remote.SendAsyncSafeAwait<Login2GateResult>(login);
                if (resp.Code == EnumRpcCallbackResultStatus.Success)
                {
                    Console.WriteLine("登录成功");
                }
                else
                {
                    Console.WriteLine("登录失败");
                }
                //没有返回值，不是RPC过程
            }
            else
            {
                //连接失败
                Console.WriteLine(ex.ToString());
            }
        }

        private static System.Threading.Tasks.ValueTask<object> Remote_OnReceiveCallback(int messgaeId , object message, IReceiveMessage receiver)
        {
            return default;
        }
    }
}
