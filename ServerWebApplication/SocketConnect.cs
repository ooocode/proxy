using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServerWebApplication
{
    public class SocketConnect
    {
        private Pipe Pipe;

        public PipeReader PipeReader => Pipe.Reader;

        public TcpClient TcpClient { get; private set; }

        public SocketConnect()
        {
            Pipe = new Pipe();
            TcpClient = new TcpClient();
        }

        public async Task ConnectAsync(string host, int port)
        {
            try
            {
                await TcpClient.ConnectAsync(host, port);
                this.RecvAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                TcpClient.Close();
            }
        }


        private async Task RecvAsync()
        {
            while (true)
            {
                var memeory = Pipe.Writer.GetMemory(8096);
                var lenth = await TcpClient.Client.ReceiveAsync(memeory, SocketFlags.None);
                if (lenth == 0)
                {
                    break;
                }

                //写入管道
                await Pipe.Writer.WriteAsync(memeory.Slice(0,lenth));
            }
            TcpClient.Close();
            await Pipe.Writer.CompleteAsync();

            //取消读
            Pipe.Reader.CancelPendingRead();
        }
    }
}
