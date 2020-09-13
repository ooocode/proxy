using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CoreProxy.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServerWebApplication
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


        public static IPEndPoint GetIpEndPoint(string address, int port)
        {
            DnsEndPoint dns = new DnsEndPoint(address, port);
            IPAddress ipadress = Dns.GetHostEntry(address)?.AddressList.ElementAtOrDefault(0);
            if (ipadress != null)
            {
                return new IPEndPoint(ipadress, port);
            }
            return null;
        }



        public IConfiguration Configuration { get; }



        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionFactory, SocketConnectionFactory>();
        }


        /// <summary>
        /// 处理浏览器
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static void ProcessBrowserAsync(ConnectionContext browser)
        {
          Task.Factory.StartNew(async () =>
          {
                //最终网站
                ConnectionContext target = null;
              try
              {
                  while (true)
                  {
                      System.IO.Pipelines.ReadResult result = await browser.Transport.Input.ReadAsync();
                      ReadOnlySequence<byte> buffer = result.Buffer;
                      var cc = BuffersExtensions.PositionOf<byte>(buffer, 0x05);

                      if (result.Buffer.Length > 0)
                      {

                          var message = Message.ParsePack(result.Buffer.ToArray());
                          if (message.Item1 != null)
                          {
                              browser.Transport.Input.AdvanceTo(result.Buffer.GetPosition(message.Item2));
                              var data = Crypto.DecryptAES(message.Item1.Content);
                              Socket5Info socket5Info = new Socket5Info();
                              if (socket5Info.TryParse(data))
                              {
                                    //连接到服务器
                                    var ipEndPoint = GetIpEndPoint(System.Text.Encoding.UTF8.GetString(socket5Info.Address), socket5Info.Port);
                                  target = await ConnectFactory.ConnectAsync(ipEndPoint);

                                  byte[] sendData = new byte[] { 0x05, 0x00, 0x00, 0x01, 0x7f, 0x00, 0x00, 0x01, 0x1f, 0x40 };
                                    //发送确认到浏览器

                                    await browser.Transport.Output.WriteAsync(sendData);

                                  ProcessTargetServer(browser, target);
                              }
                              else
                              {
                                    //Console.WriteLine($"收到浏览器{result.Buffer.Length}字节");

                                    //发送数据到目标服务器
                                    await target.Transport.Output.WriteAsync(data);
                              }
                          }
                          else
                          {
                              browser.Transport.Input.AdvanceTo(result.Buffer.GetPosition(0));
                          }
                      }
                      else
                      {
                          browser.Transport.Input.AdvanceTo(result.Buffer.GetPosition(0));
                      }



                      if (result.IsCompleted)
                      {
                          break;
                      }
                  }
                  await browser.Transport.Input.CompleteAsync();

              }
              catch (Exception ex)
              {
                  Console.WriteLine(ex.Message);
              }

              if (target != null)
              {
                    //await target.DisposeAsync();
                }
              if (browser != null)
              {
                    //await browser.DisposeAsync();
              }
          }).Start();
        }


        /// <summary>
        /// 监听网站目标服务器
        /// </summary>
        public static void ProcessTargetServer(ConnectionContext browser, ConnectionContext target)
        {
            new Task(async () =>
            {
                try
                {
                    while (true)
                    {
                        System.IO.Pipelines.ReadResult result = await target.Transport.Input.ReadAsync();

                        if (result.Buffer.Length > 0)
                        {
                            //发往浏览器
                            await browser.Transport.Output.WriteAsync(result.Buffer.ToArray());
                        }


                        target.Transport.Input.AdvanceTo(result.Buffer.GetPosition(result.Buffer.Length));
                        if (result.IsCompleted)
                        {
                            break;
                        }
                    }

                    await target.Transport.Input.CompleteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                if (target != null)
                {
                    //await target.DisposeAsync();
                }
                if (browser != null)
                {
                    //await browser.DisposeAsync();
                }

            }).Start();
        }

        private static IConnectionFactory ConnectFactory;

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ConnectFactory = app.ApplicationServices.GetRequiredService<IConnectionFactory>();
            IConnectionListenerFactory listenerFactory = app.ApplicationServices.GetRequiredService<IConnectionListenerFactory>();

            new Task(async () =>
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

                }

            }).Start();
        }
    }
}
