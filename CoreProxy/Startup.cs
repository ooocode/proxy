using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace CoreProxy
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
           
        }

        System.Net.Connections.SocketsConnectionFactory connectionFactory =
            new System.Net.Connections.SocketsConnectionFactory(System.Net.Sockets.AddressFamily.InterNetwork , System.Net.Sockets.SocketType.Stream,
            System.Net.Sockets.ProtocolType.Tcp);

        public void Configure(IApplicationBuilder app)
        {
            var listenerFactory = app.ApplicationServices.GetRequiredService<IConnectionListenerFactory>();
            // IConnectionFactory connectionFactory = app.ApplicationServices.GetRequiredService<IConnectionFactory>();
        
            Task.Run(async () =>
            { 
                Local local = new Local();
                await local.StartAsync(listenerFactory, connectionFactory, 1081/*, config.LocalAddress, config.RemotePort, config.LocalPort*/);
            });
         

            app.UseStaticFiles();
        }
    }
}
