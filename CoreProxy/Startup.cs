using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;


namespace CoreProxy
{
    public class Startup
    {
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
                await local.StartAsync(logger,listenerFactory,localListenAddress, localPort,remoteAddress,remotePort);
            });

            app.UseStaticFiles();
        }
    }
}
