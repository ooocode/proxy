using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreProxy.Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreProxy
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("可以设置开机启动项");
            Console.WriteLine("     C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\StartUp");


            Console.WriteLine("运行IE代理--pac代理     http://127.0.0.1:520/pac");
            Console.WriteLine("          --全局代理    http://127.0.0.1:520/global");

            WebHost.CreateDefaultBuilder()
                .UseUrls("http://localhost:520")
                .UseStartup<Startup>()
                .Build().Run();
        }
    }
}
