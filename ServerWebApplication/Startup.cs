using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CoreProxy.Common;
using DnsClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerWebApplication;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
//namespace ServerWebApplication
{
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

        public static (Message, int) ParsePack(byte[] data)
        {
            try
            {
                //取出头部4字节的长度
                var lenth = BitConverter.ToInt32(data.Take(4).ToArray());

                var jsonBytes = data.Skip(4).Take(lenth).ToArray();
                var jsonStr = System.Text.Encoding.UTF8.GetString(jsonBytes);

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(jsonStr);
                return (result, 4 + lenth);
            }
            catch (Exception ex)
            {
                return (null, 0);
            }
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        public static async Task<IPEndPoint> GetIpEndPointAsync(string address, int port)
        {
            /* DnsEndPoint dns = new DnsEndPoint(address, port);
             IPAddress ipadress = Dns.GetHostEntry(address)?.AddressList.ElementAtOrDefault(0);
             if (ipadress != null)
             {
                 return new IPEndPoint(ipadress, port);
             }
             return null;*/

            return new IPEndPoint(IPAddress.Parse(address), port);

            var client = new LookupClient(new LookupClientOptions { UseCache = true });
            var result = await client.QueryAsync(address, QueryType.A);
            var parserAddr = result.Answers?.ARecords()?.FirstOrDefault()?.Address;
            if (parserAddr == null)
            {
                return null;
            }
            return new IPEndPoint(parserAddr, port);
        }



        public IConfiguration Configuration { get; }



        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            IConnectionListenerFactory listenerFactory = app.ApplicationServices.GetRequiredService<IConnectionListenerFactory>();

            Task.Run(async () =>
            {
                try
                {
                    var listener = await listenerFactory.BindAsync(new IPEndPoint(IPAddress.Any, 2019));

                    while (true)
                    {
                        ConnectionContext browser = await listener.AcceptAsync();
                        //处理浏览器
                        ProcessBrowserAsync(browser);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        /// <summary>
        /// 处理浏览器
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static void ProcessBrowserAsync(ConnectionContext browser)
        {
            Task.Run(async () =>
            {
                //最终网站
                SocketConnect target = new SocketConnect();
                try
                {
                    while (true)
                    {
                        System.IO.Pipelines.ReadResult result = await browser.Transport.Input.ReadAsync();
                        if (!result.Buffer.IsEmpty)
                        {
                            var message = Message.ParsePack(result.Buffer.ToArray());
                            if (message.Item1 != null)
                            {
                                var data = message.Item1.Content;
                                //var data = Crypto.DecryptAES(message.Item1.Content);
                                Socket5Info socket5Info = new Socket5Info();
                                if (socket5Info.TryParse(data))
                                {
                                    await target.ConnectAsync(System.Text.Encoding.UTF8.GetString(socket5Info.Address), socket5Info.Port);
                                    //连接到服务器
                                    //var ipEndPoint = await GetIpEndPointAsync(System.Text.Encoding.UTF8.GetString(socket5Info.Address), socket5Info.Port);
                                    //if(ipEndPoint == null)
                                    //{
                                    //    break;
                                    //}

                                    //await target.ConnectAsync(ipEndPoint.Address,ipEndPoint.Port);

                                    byte[] sendData = new byte[] { 0x05, 0x00, 0x00, 0x01, 0x7f, 0x00, 0x00, 0x01, 0x1f, 0x40 };
                                    //发送确认到浏览器

                                    await browser.Transport.Output.WriteAsync(sendData);

                                    ProcessTargetServer(browser, target);
                                }
                                else
                                {
                                    //发送数据到目标服务器
                                    await target.TcpClient.Client.SendAsync(data, SocketFlags.None);
                                }

                                browser.Transport.Input.AdvanceTo(result.Buffer.GetPosition(message.Item2));
                            }
                            else
                            {
                                browser.Transport.Input.AdvanceTo(result.Buffer.GetPosition(0));
                            }

                        }
                        else
                        {
                            if (result.IsCompleted || result.IsCanceled)
                            {
                                break;
                            }
                        }
                    }
                    await browser.Transport.Input.CompleteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }


        /// <summary>
        /// 监听网站目标服务器
        /// </summary>
        public static void ProcessTargetServer(ConnectionContext browser, SocketConnect target)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var readResult = await target.PipeReader.ReadAsync();
                        if (!readResult.Buffer.IsEmpty)
                        {
                            SequencePosition position = readResult.Buffer.Start;
                            if (readResult.Buffer.TryGet(ref position, out var memory))
                            {

                                //发往浏览器
                                await browser.Transport.Output.WriteAsync(memory);

                                target.PipeReader.AdvanceTo(readResult.Buffer.GetPosition(memory.Length));
                            }
                        }
                        else
                        {
                            if (readResult.IsCanceled || readResult.IsCompleted)
                            {
                                break;
                            }
                        }
                    }

                    await target.PipeReader.CompleteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }
    }
}
