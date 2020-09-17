using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreProxy
{
    public class Startup
    {

        System.Net.Connections.SocketsConnectionFactory connectionFactory =
            new System.Net.Connections.SocketsConnectionFactory(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream,
            System.Net.Sockets.ProtocolType.Tcp);

        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        
        }



        public void Configure(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Local>>();

            var listenerFactory = app.ApplicationServices.GetRequiredService<IConnectionListenerFactory>();
            Task.Run(async () =>
            {
                var localListenAddress = Configuration["LocalListenAddress"];
                if (!int.TryParse(Configuration["LocalListenPort"],out int localPort))
                {
                    localPort = 1081;
                }

                var remoteAddress = Configuration["RemoteConnectAddress"];
                if (!int.TryParse(Configuration["RemoteConnectPort"], out int remotePort))
                {
                    remotePort = 2019;
                }

                Local local = new Local();
                await local.StartAsync(logger,listenerFactory, connectionFactory,localListenAddress, localPort,remoteAddress,remotePort);
            });

            app.UseStaticFiles();
        }
    }
}
