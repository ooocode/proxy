using CoreProxy.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Connections;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CoreProxy
{
    public class Options : IOptions<SocketTransportOptions>
    {
        public SocketTransportOptions Value => new SocketTransportOptions { NoDelay = true };
    }


    //local端处理
    public class Message
    {
        public byte[] Content { get; set; }


        public static byte[] MakePack(Message message)
        {
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            var jsonData = System.Text.Encoding.UTF8.GetBytes(str);

            //返回长度的小端字节表示   4字节
            byte[] head = BitConverter.GetBytes(jsonData.Length);

            return Utility.MergeBytes(head, jsonData);
        }

        public static Message ParsePack(byte[] data)
        {
            try
            {
                //取出头部4字节的长度
                var lenth = BitConverter.ToInt32(data.Take(4).ToArray());

                var jsonBytes = data.Skip(4).Take(lenth).ToArray();
                var jsonStr = System.Text.Encoding.UTF8.GetString(jsonBytes);

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(jsonStr);
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }


    public class Local
    {
        public static IPEndPoint GetIpEndPoint(string address, int port)
        {
            DnsEndPoint dns = new DnsEndPoint(address, port);
            var ipadress = Dns.GetHostEntry(address)?.AddressList.ElementAtOrDefault(0);
            if (ipadress != null)
            {
                return new IPEndPoint(ipadress, port);
            }
            return null;
        }


        public Local()
        {

        }

        SocketsConnectionFactory connectionFactory;

        public async Task StartAsync(IConnectionListenerFactory listenerFactory,
                                    SocketsConnectionFactory connectionFactory,
                                     //string remoteAddress,
                                     // int remotePort,
                                     int localListenPort)
        {
            var bind = await listenerFactory.BindAsync(new IPEndPoint(IPAddress.Loopback, localListenPort));
            Console.WriteLine($"客户端正在监听{localListenPort}端口");

            this.connectionFactory = connectionFactory;

            while (true)
            {
                ConnectionContext browser = await bind.AcceptAsync();
                TcpHandlerAsync(browser);
            }
        }


        /// <summary>
        /// 浏览器tcp转发
        /// </summary>
        /// <param name="client"></param>
        private void TcpHandlerAsync(ConnectionContext browser)
        {
            Task.Factory.StartNew(async () =>
            {
                Connection target = null;
                try
                {
                    target = await connectionFactory.ConnectAsync(new IPEndPoint(IPAddress.Parse("23.95.20.144"), 2019));
#if DEBUG
                    // target = await connectionFactory.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2019));

#else
                    target = await connectionFactory.ConnectAsync(new IPEndPoint(IPAddress.Parse("23.95.20.144"), 2019));
#endif

                    ProcessTaget(browser, target);
                    while (true)
                    {
                        //浏览器普通接收
                        var result = await browser.Transport.Input.ReadAsync();

                        if (result.IsCompleted || result.IsCanceled)
                        {
                            break;
                        }

                        var buff = result.Buffer;


                        // 接收到浏览器数据
                        if (buff.Length == 3 && string.Join(",", buff.ToArray()) == "5,1,0")
                        {
                            //发5 0 回到浏览器
                            await browser.Transport.Output.WriteAsync(new byte[] { 5, 0 });
                        }
                        else
                        {
                            var pack = Message.MakePack(new Message { Content = Crypto.EncryptAES(buff.ToArray()) });


                            //发送数据到服务器
                            await target.Pipe.Output.WriteAsync(pack);
                        }

                        browser.Transport.Input.AdvanceTo(buff.GetPosition(buff.Length));
                    }

                    await browser.Transport.Input.CompleteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        //处理目标服务器
        void ProcessTaget(ConnectionContext browser, Connection target)
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (true)
                    {
                       
                        ReadResult result = await target.Pipe.Input.ReadAsync();
                        if (result.IsCompleted || result.IsCanceled)
                        {
                            break;
                        }
                     
                        if (result.Buffer.Length > 0)
                        {
                            //Console.WriteLine($"sslocal 收到服务器{result.Buffer.Length}字节");
                            //发往浏览器
                            await browser.Transport.Output.WriteAsync(result.Buffer.ToArray());

                            target.Pipe.Input.AdvanceTo(result.Buffer.GetPosition(result.Buffer.Length));
                        }
                    }

                    await target.Pipe.Input.CompleteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }
    }
}
