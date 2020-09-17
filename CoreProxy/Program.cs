using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace CoreProxy
{
    public class Program
    {
        static void ShowInfomation()
        {

            Console.WriteLine("可以设置开机启动项");
            Console.WriteLine("     C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\StartUp");


            Console.WriteLine("运行IE代理--pac代理     http://127.0.0.1:520/pac.txt");
            Console.WriteLine("          --全局代理    http://127.0.0.1:520/global.txt");
        }

        static void Main(string[] args)
        {
            ShowInfomation();

            WebHost.CreateDefaultBuilder()
                .UseUrls("http://localhost:520")
                .UseStartup<Startup>()
                .Build().Run();
        }
    }
}
