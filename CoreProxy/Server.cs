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
                _server.Dispose();
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
            socketBrowser.OnException += (string msg, SocketUnit s) =>
            {
                try
                {
                    s.Socket.Close();
                    (s.UserData as SocketUnit).Socket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            };
            socketBrowser.OnRecv += SocketBrowser_OnRecv;
            socketBrowser.ReceiveAndHandleFrame();
        }

        //接收到浏览器数据
        private bool SocketBrowser_OnRecv(byte[] data, SocketUnit socketBrowser)
        {
            byte[] vs = Crypto.DecryptAES(data);
            Socket5Info socket5Info = new Socket5Info();
            if (socket5Info.TryParse(vs))
            {
                var remoteResult = socket5Info.ConnectThisSocket();
                if (remoteResult.sucess)
                {
                    SocketUnit socketRemote = new SocketUnit(remoteResult.remote);
                    socketRemote.OnException += (string msg, SocketUnit s) =>
                    {
                        try
                        {
                            s.Socket.Close();
                            (s.UserData as SocketUnit).Socket.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    };
                    socketRemote.OnRecv += SocketRemote_OnRecv;
                    socketRemote.Receive();
                    socketBrowser.UserData = socketRemote;
                    socketRemote.UserData = socketBrowser;

                    byte[] sendData = Crypto.EncryptAES(new byte[] { 0x05, 0x00, 0x00, 0x01, 0x7f, 0x00, 0x00, 0x01, 0x1f, 0x40 });
                    return socketBrowser.SendWithLenth(sendData);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                //发送到远程服务器
                return (socketBrowser.UserData as SocketUnit).Send(vs);
            }
        }

        //加密发往ss local
        private bool SocketRemote_OnRecv(byte[] data, SocketUnit remote)
        {
            SocketUnit socketBrowser = remote.UserData as SocketUnit;
            return socketBrowser.SendWithLenth(Crypto.EncryptAES(data));
        }
    }
}
