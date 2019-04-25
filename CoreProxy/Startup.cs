using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CoreProxy
{
    public class Startup
    {
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
  
        }

        public void Configure(IApplicationBuilder app)
        {
            
            app.UseOwin(pipeline =>
            {
                pipeline(next => OwinHello);
            });
        }

        public Task OwinHello(IDictionary<string, object> environment)
        {
            string responseText = "please scanf http://127.0.0.1:5000/pac  or http://127.0.0.1:5000/global";

            try
            {
                string reqPath = environment["owin.RequestPath"].ToString();
                if (reqPath == "/pac")
                {
                    responseText = File.ReadAllText("pac");
                }
                else if (reqPath == "/global")
                {
                    responseText = File.ReadAllText("global");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseText);

            // OWIN Environment Keys: http://owin.org/spec/spec/owin-1.0.0.html
            var responseStream = (Stream)environment["owin.ResponseBody"];
            var responseHeaders = (IDictionary<string, string[]>)environment["owin.ResponseHeaders"];

            responseHeaders["Content-Length"] = new string[] { responseBytes.Length.ToString(CultureInfo.InvariantCulture) };
            responseHeaders["Content-Type"] = new string[] { "text/plain" };

            return responseStream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
    }
}
