using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreProxy.Common
{
    public delegate bool OnAction(byte[] data, SocketUnit socketUnit);

    public delegate void OnException(string msg, SocketUnit socketUnit);

    public class SocketState
    {
        public Socket Socket { get; set; }

        /// <summary>
        /// 每次接收缓冲区1024字节
        /// </summary>
        public byte[] Buff { get; set; }

        /// <summary>
        /// 回调函数  解析出一帧的时候调用
        /// </summary>
        public OnAction Action { get; set; }

        /// <summary>
        /// 解析出一帧的长度
        /// </summary>
        public int FrameLenth { get; set; } = 0;



        public void EnQueue(byte[] vs)
        {
            foreach (var i in vs)
            {
                AllBytes.Enqueue(i);
            }
        }

        public byte[] DeQueue(int n)
        {
            if (AllBytes.Count >= n)
            {
                byte[] vs = new byte[n];
                for (int i = 0; i < n; i++)
                {
                    vs[i] = AllBytes.Dequeue();
                }
                return vs;
            }
            else
            {
                return null;
            }
        }

        //每次接收的都追加到尾部
        public Queue<byte> AllBytes { get; set; } = null;
    }

    public class SocketUnit
    {
        public Socket Socket { get; set; } = null;

        /// <summary>
        /// 用户附加数据
        /// </summary>
        public object UserData { get; set; } = null;


        public event OnAction OnRecv;

        public event OnException OnException;

        public SocketUnit(Socket socket)
        {
            Socket = socket;
            try
            {
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            }
            catch(Exception ex)
            {
                OnException(ex.Message, this);
            }
        }


        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool Connect(string address, int port)
        {
            try
            {
                Socket.Connect(address, port);
                return true;
            }
            catch (Exception ex)
            {
                OnException(ex.Message, this);
            }
            return false;
        }


        /// <summary>
        /// 普通发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Send(byte[] data)
        {
            try
            {
                if (data.Length > 0)
                {
                    return Socket.Send(data) > 0;
                }
            }
            catch (Exception ex)
            {
                OnException(ex.Message, this);
            }
            return false;
        }


        /// <summary>
        /// 发送 （lenth + payload）
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public bool SendWithLenth(byte[] data)
        {
            try
            {
                if (data.Length > 0)
                {
                    //返回长度的小端字节表示   4字节
                    byte[] head = BitConverter.GetBytes(data.Length);
                    byte[] sendData = new byte[head.Length + data.Length];

                    int lenth = BitConverter.ToInt32(head);
                    Array.Copy(head, 0, sendData, 0, head.Length);
                    Array.Copy(data, 0, sendData, head.Length, data.Length);

                    return Socket.Send(sendData) > 0;
                }
            }
            catch (Exception ex)
            {
                OnException(ex.Message, this);
            }
            return false;
        }


        /// <summary>
        /// 普通接收
        /// </summary>
        public void Receive()
        {
            try
            {
                SocketState socketState = new SocketState
                {
                    Action = OnRecv,
                    Socket = this.Socket,
                    Buff = new byte[8192]
                };

                Socket.BeginReceive(socketState.Buff, 0, socketState.Buff.Length, SocketFlags.None, OnRecvCallback, socketState);
            }
            catch (Exception ex)
            {
                OnException(ex.Message, this);
            }
        }

        private void OnRecvCallback(IAsyncResult ar)
        {
            SocketState socketState = ar.AsyncState as SocketState;
            try
            {
                int lenth = socketState.Socket.EndReceive(ar);
                if (lenth <= 0)
                {
                    OnException("OnRecvCallback lenth <= 0", this);
                    return;
                }

                byte[] result = new byte[lenth];
                Array.Copy(socketState.Buff, 0, result, 0, lenth);

                if (!socketState.Action(result, this))
                {
                    OnException("OnRecvCallback socketState.Action", this);
                    return;
                }

                socketState.Socket.BeginReceive(socketState.Buff, 0, socketState.Buff.Length, SocketFlags.None, OnRecvCallback, socketState);
            }
            catch (Exception ex)
            {
                OnException(ex.Message, this);
            }
        }



        //处理帧
        public void ReceiveAndHandleFrame()
        {
            try
            {
                SocketState socketState = new SocketState
                {
                    Action = OnRecv,
                    Socket = this.Socket,
                    Buff = new byte[8192],
                    AllBytes = new Queue<byte>()
                };

                Socket.BeginReceive(socketState.Buff, 0, socketState.Buff.Length, SocketFlags.None, OnRecvCallbackHandleFrame, socketState);
            }
            catch (Exception ex)
            {
                OnException(ex.Message, this);
            }
        }

        private void OnRecvCallbackHandleFrame(IAsyncResult ar)
        {
            SocketState socketState = ar.AsyncState as SocketState;
            try
            {
                int lenth = socketState.Socket.EndReceive(ar);

                if (lenth <= 0)
                {
                    OnException("OnRecvCallbackHandleFrame ", this);
                }
                else
                {
                    byte[] result = new byte[lenth];
                    Array.Copy(socketState.Buff, 0, result, 0, lenth);

                    //进队列
                    socketState.EnQueue(result);

                    while (socketState.AllBytes.Count > 0)
                    {
                        //取出长度
                        if (socketState.FrameLenth == 0)
                        {
                            byte[] fourBytes = socketState.DeQueue(4);
                            if (fourBytes != null)
                            {
                                socketState.FrameLenth = BitConverter.ToInt32(fourBytes);
                            }
                        }

                        //取出一帧数据
                        byte[] frame = socketState.DeQueue(socketState.FrameLenth);
                        if (frame != null)
                        {
                            //Console.WriteLine("处理一帧 " + frame.Length + "队列大小" + socketState.AllBytes.Count);
                            if (!socketState.Action(frame, this))
                            {
                                OnException("OnRecvCallbackHandleFrame socketState.Action", this);
                                return;
                            }
                            socketState.FrameLenth = 0;
                        }
                        else
                        {
                            break;
                        }
                    }
                    socketState.Socket.BeginReceive(socketState.Buff, 0, socketState.Buff.Length, SocketFlags.None, OnRecvCallbackHandleFrame, socketState);
                }
            }
            catch (Exception ex)
            {
                OnException(ex.Message, this);
            }
        }
    }
}
