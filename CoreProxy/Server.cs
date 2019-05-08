using CoreProxy.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreProxy
{
    /// <summary>
    /// 服务端
    /// </summary>
    public class Server
    {
        Socket _server = null;
        public Server()
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
        }


        public async Task StartAsync(int listenPort)
        {
            try
            {
                _server.Bind(new IPEndPoint(IPAddress.Any, listenPort));
                _server.Listen(100);
                Console.WriteLine($"服务器  正在监听{listenPort}端口......");
                while (true)
                {
                    var browser = await _server.AcceptAsync();
                    TcpHandler(browser);
                }
            }
            catch (Exception ex)
            {
                _server.Close();
                Console.WriteLine("server StartAsync " + ex.Message);
            }
        }


        /// <summary>
        /// tcp转发
        /// </summary>
        /// <param name="client"></param>
        void TcpHandler(Socket browser)
        {
            SocketUnit socketBrowser = new SocketUnit(browser);
            SocketUnit socketRemote = null;
            new Task(async () => 
            {
                try
                {
                    while (true)
                    {
                        var browserFrames = await socketBrowser.ReceiveFrameAsync();
                        foreach(var frame in browserFrames)
                        {
                            var browserData = Crypto.DecryptAES(frame);
                            Socket5Info socket5Info = new Socket5Info();
                            if (socket5Info.TryParse(browserData))
                            {
                                var remoteResult = socket5Info.ConnectThisSocket();
                                if (remoteResult.sucess)
                                {
                                    socketRemote = new SocketUnit(remoteResult.remote);
                                    new Task(async () => {
                                        try
                                        {
                                            while (true)
                                            {
                                                var remoteData = await socketRemote.ReceiveAsync();
                                                await socketBrowser.SendWithLenthAsync(Crypto.EncryptAES(remoteData));
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            socketBrowser.Socket.Close();
                                            socketRemote.Socket.Close();
                                        }
                                    }).Start();


                                    byte[] sendData = Crypto.EncryptAES(new byte[] { 0x05, 0x00, 0x00, 0x01, 0x7f, 0x00, 0x00, 0x01, 0x1f, 0x40 });
                                    await socketBrowser.SendWithLenthAsync(sendData);
                                }
                            }
                            else
                            {
                                //发送到远程服务器
                                await socketRemote.SendAsync(browserData);
                            }
                        }
                       
                    }
                }
                catch(Exception ex)
                {
                    socketBrowser.Socket.Close();
                    socketRemote.Socket.Close();
                }
            }).Start();
        }
    }
}
