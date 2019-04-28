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

        public async Task StartAsync(string remoteAddress, int remotePort)
        {
            _remoteAddress = remoteAddress;
            _remotePort = remotePort;
            try
            {
                _server.Bind(new IPEndPoint(IPAddress.Any, 1080));
                _server.Listen(100);
                Console.WriteLine("客户端正在监听1080端口");
                while (true)
                {
                    var browser = await _server.AcceptAsync();
                    TcpHandler(browser);
                }
            }
            catch (Exception ex)
            {
                _server.Dispose();
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

            socketBrowser.UserData = socketRemote;
            socketRemote.UserData = socketBrowser;

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
            socketBrowser.OnRecv += SocketBrowser_OnRecv;

            //浏览器普通接收
            socketBrowser.Receive();
        }

        private bool SocketBrowser_OnRecv(byte[] data, SocketUnit socketBrowser)
        {
            SocketUnit socketRemote = socketBrowser.UserData as SocketUnit;

            //接收到浏览器数据
            if (data.Length == 3 && string.Join(",", data) == "5,1,0")
            {
                if (socketRemote.Connect(_remoteAddress, _remotePort))
                {
                    socketRemote.OnRecv += SocketRemote_OnRecv;
                    socketRemote.ReceiveAndHandleFrame();
                    return socketBrowser.Send(new byte[] { 5, 0 });
                }
                else
                {
                    return false;
                }
            }
            else
            {
                //加密并添加头部长度发送到远程服务器
                return socketRemote.SendWithLenth(Crypto.EncryptAES(data));
            }
        }

        private bool SocketRemote_OnRecv(byte[] data, SocketUnit socketRemote)
        {
            SocketUnit socketBrowser = socketRemote.UserData as SocketUnit;
            //接收到远程数据解密发往浏览器
            return socketBrowser.Send(Crypto.DecryptAES(data));
        }
    }
}
