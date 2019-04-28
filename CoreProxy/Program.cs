using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CoreProxy
{
    public class Program
    {
        /// <summary>
        /// 开启Http服务器 pac文件
        /// </summary>
        static void StartHttpSeverice()
        {
            new Thread(() =>
            {
                Console.WriteLine("可以设置开机启动项");
                Console.WriteLine("     C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\StartUp");


                Console.WriteLine("运行IE代理--pac代理     http://127.0.0.1:520/pac");
                Console.WriteLine("          --全局代理    http://127.0.0.1:520/global");

                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseUrls("http://*:520")
                    .UseStartup<Startup>()
                    .Build();
                host.Run();
            }).Start();
        }


        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Config config = new Config
            {
                Password = "Oyk1186053605",
                LocalAddress = "127.0.0.1",
                LocalPort = 1080,
                RemoteAddress = "23.95.67.191",
                RemotePort = 2019
            };

            bool IsLocalDebug = false;
            if (IsLocalDebug)  //本机测试用
            {
                //http pac
                StartHttpSeverice();

                new Thread(new ParameterizedThreadStart(async (obj) =>
                {
                    Server server = new CoreProxy.Server();
                    await server.StartAsync(config.RemotePort);

                })).Start();

                Local local = new Local();
                await local.StartAsync(config.LocalAddress, config.RemotePort,config.LocalPort);
            }
            else  //生产环境
            {
                //windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    //http pac
                    StartHttpSeverice();

                    Local local = new Local();
                    await local.StartAsync(config.RemoteAddress, config.RemotePort,config.LocalPort);
                }
                else
                {
                    Server server = new Server();
                    await server.StartAsync(config.RemotePort);
                }
            }
        }
    }
}
