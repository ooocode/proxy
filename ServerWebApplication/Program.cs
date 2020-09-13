using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using CoreProxy.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;


namespace ServerWebApplication
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }



        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
