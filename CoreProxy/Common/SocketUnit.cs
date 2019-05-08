using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreProxy.Common
{
    public delegate void OnException(string msg, SocketUnit socketUnit);


    public class SocketUnit
    {
        public Socket Socket { get; set; } = null;


        public SocketUnit(Socket socket)
        {
            Socket = socket;
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
        }


        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task ConnectAsync(string address, int port)
        {
            await Socket.ConnectAsync(address, port);
        }


        /// <summary>
        /// 普通发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SendAsync(byte[] data)
        {
            await Socket.SendAsync(data, SocketFlags.None);
        }


        /// <summary>
        /// 发送 （lenth + payload）
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public async Task SendWithLenthAsync(byte[] data)
        {
            //返回长度的小端字节表示   4字节
            byte[] head = BitConverter.GetBytes(data.Length);
            byte[] sendData = new byte[head.Length + data.Length];

            int lenth = BitConverter.ToInt32(head);
            Array.Copy(head, 0, sendData, 0, head.Length);
            Array.Copy(data, 0, sendData, head.Length, data.Length);

            await Socket.SendAsync(sendData, SocketFlags.None);
        }

        byte[] buff = new byte[8192];

        /// <summary>
        /// 普通接收
        /// </summary>
        public async Task<byte[]> ReceiveAsync()
        {
           
            int lenth = await Socket.ReceiveAsync(buff, SocketFlags.None);
            if (lenth > 0)
            {
                byte[] result = new byte[lenth];
                Array.Copy(buff, 0, result, 0, lenth);
                return result;
            }
            return null;
        }



        /// 解析出一帧的长度
        /// </summary>
        private int FrameLenth = 0;



        private void EnQueue(byte[] vs)
        {
            foreach (var i in vs)
            {
                AllBytes.Enqueue(i);
            }
        }

        private byte[] DeQueue(int n)
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
        private Queue<byte> AllBytes = new Queue<byte>();

        //处理帧
        public async Task<List<byte[]>> ReceiveFrameAsync()
        {
            List<byte[]> frames = new List<byte[]>();
            
            int lenth = await Socket.ReceiveAsync(buff, SocketFlags.None);
            if (lenth > 0)
            {
                byte[] result = new byte[lenth];
                Array.Copy(buff, 0, result, 0, lenth);

                //进队列
                EnQueue(result);

                while (AllBytes.Count > 0)
                {
                    //取出长度
                    if (FrameLenth == 0)
                    {
                        byte[] fourBytes = DeQueue(4);
                        if (fourBytes != null)
                        {
                            FrameLenth = BitConverter.ToInt32(fourBytes);
                        }
                    }

                    //取出一帧数据
                    byte[] frame = DeQueue(FrameLenth);
                    if (frame != null)
                    {
                        FrameLenth = 0;
                        //Console.WriteLine("处理一帧 " + frame.Length + "队列大小" + socketState.AllBytes.Count);
                        //return frame;
                        frames.Add(frame);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                throw new Exception("接收缓冲区为0");
            }
            return frames;
        }
    }
}
