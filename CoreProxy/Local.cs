using CoreProxy.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreProxy
{
    //local端处理
    public class Local
    {
        Socket _server = null;
        string _remoteAddress;
        int _remotePort;

        public Local()
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
        }


        public async Task StartAsync(string remoteAddress, int remotePort,int localListenPort)
        {
            _remoteAddress = remoteAddress;
            _remotePort = remotePort;
            try
            {
                _server.Bind(new IPEndPoint(IPAddress.Any, localListenPort));
                _server.Listen(100);
                Console.WriteLine($"客户端正在监听{localListenPort}端口");
                while (true)
                {
                    var browser = await _server.AcceptAsync();
                    TcpHandler(browser);
                }
            }
            catch (Exception ex)
            {
                _server.Close();
                Console.WriteLine("local StartAsync " + ex.Message);
            }
        }


        /// <summary>
        /// tcp转发
        /// </summary>
        /// <param name="client"></param>
        void TcpHandler(Socket browser)
        {
            SocketUnit socketBrowser = new SocketUnit(browser);
            SocketUnit socketRemote = new SocketUnit(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));

            //浏览器普通接收
            new Task(async () => 
            {
                try
                {
                    while (true)
                    {
                        var browserData = await socketBrowser.ReceiveAsync();
                        //接收到浏览器数据
                        if (browserData.Length == 3 && string.Join(",", browserData) == "5,1,0")
                        {
                            await socketRemote.ConnectAsync(_remoteAddress, _remotePort);
                            await socketBrowser.SendAsync(new byte[] { 5, 0 });

                            new Task(async () =>
                            {
                                try
                                {
                                    while (true)
                                    {
                                        var remoteData = await socketRemote.ReceiveFrameAsync();
                                        foreach (var frame in remoteData)
                                        {
                                            //接收到远程数据解密发往浏览器
                                            await socketBrowser.SendAsync(Crypto.DecryptAES(frame));
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    socketBrowser.Socket.Close();
                                    socketRemote.Socket.Close();
                                }

                            }).Start();
                        }
                        else
                        {
                            //加密并添加头部长度发送到远程服务器
                            await socketRemote.SendWithLenthAsync(Crypto.EncryptAES(browserData));
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
