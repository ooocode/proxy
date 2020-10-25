using CoreProxy.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CoreProxy
{

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
        public Local()
        {

        }

        private string remoteAddress;
        private int remotePort;
        private ILogger<Local> logger;

        public async Task StartAsync(ILogger<Local> logger,
            IConnectionListenerFactory listenerFactory,
                                     string localListenAddress,
                                     int localListenPort,
                                     string remoteAddress,
                                     int remotePort)
        {

            this.logger = logger;
            this.remoteAddress = remoteAddress;
            this.remotePort = remotePort;

            try
            {
                var bind = await listenerFactory.BindAsync(new IPEndPoint(IPAddress.Parse(localListenAddress), localListenPort));
                logger.LogInformation($"客户端正在监听{localListenPort}端口");

                while (true)
                {
                    ConnectionContext browser = await bind.AcceptAsync();
                    TcpHandlerAsync(browser);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex.Message);
            }
        }


        /// <summary>
        /// 浏览器tcp转发
        /// </summary>
        /// <param name="client"></param>
        private void TcpHandlerAsync(ConnectionContext browser)
        {
            Task.Run(async () =>
            {
                TcpClient target = new TcpClient();
                target.NoDelay = false;
                try
                {
                    await target.ConnectAsync(IPAddress.Parse(remoteAddress), remotePort);

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
                            await target.Client.SendAsync(pack,SocketFlags.None);
                            //await target.Pipe.Output.WriteAsync(pack);
                        }

                        browser.Transport.Input.AdvanceTo(buff.GetPosition(buff.Length));
                    }

                    await browser.Transport.Input.CompleteAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            });
        }

        //处理目标服务器
        private Task ProcessTaget(ConnectionContext browser, TcpClient target)
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        byte[] buff = new byte[8192];
                        var realLenth = await target.Client.ReceiveAsync(buff,SocketFlags.None);

                        if (realLenth == 0)
                        {
                            break;
                        }
                        else
                        {
                            var result = new byte[realLenth];
                            Array.Copy(buff, result, realLenth);
                            //发往浏览器
                            await browser.Transport.Output.WriteAsync(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }

                target.Dispose();
            });
        }
    }
}
