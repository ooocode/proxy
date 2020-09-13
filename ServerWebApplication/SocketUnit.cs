using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CoreProxy.Common
{
    public delegate void OnException(string msg, SocketUnit socketUnit);


    /// <summary>
    /// 数据帧
    ///【4字节payload长度】【payload】
    /// </summary>
    public class DataFrame
    {
        /// <summary>
        /// 数据
        /// </summary>
        public byte[] Payload { get; set; }



        /// <summary>
        /// 帧形成字节数组
        /// </summary>
        /// <param name="dataFrame"></param>
        /// <returns></returns>
        public static byte[] MakeBytes(DataFrame dataFrame)
        {
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(dataFrame);
            var jsonData = Encoding.UTF8.GetBytes(str);

            //返回长度的小端字节表示   4字节
            byte[] head = BitConverter.GetBytes(jsonData.Length);

            return Utility.MergeBytes(head, jsonData);
        }



        /// <summary>
        /// 解析数据 形成帧
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static (DataFrame frame, int frameLenth) ParseBytes(byte[] data)
        {
            try
            {
                //取出头部4字节的长度
                var lenth = BitConverter.ToInt32(data.Take(4).ToArray());

                var jsonBytes = data.Skip(4).Take(lenth).ToArray();
                var jsonStr = System.Text.Encoding.UTF8.GetString(jsonBytes);

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<DataFrame>(jsonStr);
                return (result, 4 + lenth);
            }
            catch(Exception ex)
            {
                return (null, 0);
            }
        }
    }


    public class SocketUnit
    {
        public Socket Socket { get; set; } = null;


        public SocketUnit(Socket s)
        {
            Socket = s ?? throw new Exception("SocketUnit socket为空");

            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
        }


        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public Task ConnectAsync(string address, int port)
        {
            return Socket.ConnectAsync(address, port);
        }


        /// <summary>
        /// 普通发送
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<int> SendAsync(byte[] data)
        {
            return Socket.SendAsync(data, SocketFlags.None);
        }

        byte[] buff = new byte[8192];

        /// <summary>
        /// 普通接收
        /// </summary>
        public async Task<byte[]> ReceiveAsync()
        {
            int lenth = await Socket.ReceiveAsync(buff, SocketFlags.None);
            //Console.WriteLine("lenth " + lenth);
            if(lenth == 0)
            {
                throw new Exception("接收==0");
            }
            byte[] result = new byte[lenth];
            Array.Copy(buff, 0, result, 0, lenth);
            return result;
        }


        /// <summary>
        /// 发送数据帧 （lenth + payload）
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public Task<int> SendFrameAsync(byte[] payload)
        {
            var sendData = DataFrame.MakeBytes(new DataFrame { Payload = payload });
            return Socket.SendAsync(sendData, SocketFlags.None);
        }

        byte[] recvBuff = null;

        //处理帧
        public async Task<List<byte[]>> ReceiveFrameAsync()
        {
            List<byte[]> frames = new List<byte[]>();

            byte[] result = await this.ReceiveAsync();

            //解密
            //result = Crypto.DecryptAES(result);

            if (recvBuff == null)
            {
                recvBuff = result;
            }
            else
            {
                //将接收到的数据放入缓冲区
                recvBuff = Utility.MergeBytes(recvBuff, result);
            }


            while (recvBuff.Length > 0)
            {
                var frame = DataFrame.ParseBytes(recvBuff);
                if (frame.frame == null)
                {
                    break;
                }
                frames.Add(frame.frame.Payload);
                recvBuff = Utility.RemoveNBytes(recvBuff, frame.frameLenth);
            }

            return frames;
        }


        public void Close()
        {
            try
            {
                if (Socket != null)
                {
                    Socket.Close();
                    //Socket.Dispose();
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(DateTime.Now + " [Close] " + ex.Message);
            }
        }
    }
}
