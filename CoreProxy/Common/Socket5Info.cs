using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CoreProxy.Common
{
    class Socket5Info
    {
        // ver 版本号 固定 0x05
        public byte Ver { get; set; }

        //SOCK的命令码，占用1个字节，表示建立连接的类型，其中，0x01表示CONNECT请求，0x02表示BIND请求，0x03表示UDP转发
        public byte Cmd { get; set; }

        //rsv 默认0x00  保留位 不使用
        public byte Rsv { get; set; }


        //DST.ADDR类型，
        //0x01表示IPv4地址，此时DST.ADDR部分4字节长度。
        //0x03则表示域名，此时DST.ADDR部分第一个字节为域名长度，DST.ADDR剩余的内容为域名，没有\0结尾。
        //0x04表示IPv6地址，此时DST.ADDR部分16个字节长度。
        public byte Atype { get; set; }

        /// <summary>
        /// 目标地址
        /// </summary>
        public byte[] Address { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        public bool TryParse(byte[] vs)
        {
            try
            {
                if (vs.Length > 3 && vs[0] == 0x05 && vs[1] == 0x01 && vs[2] == 0x00)
                {
                    Ver = vs.Skip(0).Take(1).ToArray()[0];
                    Cmd = vs.Skip(1).Take(1).ToArray()[0];
                    Rsv = vs.Skip(2).Take(1).ToArray()[0];
                    Atype = vs.Skip(3).Take(1).ToArray()[0];

                    if (Atype == 0x01)    //ip v4
                    {
                        Address = vs.Skip(4).Take(4).ToArray();
                        Port = BitConverter.ToInt16(vs, 8);
                    }
                    else if (Atype == 0x03)   //域名
                    {
                        int domainNameLenth = vs.Skip(4).Take(1).ToArray()[0];
                        Address = vs.Skip(5).Take(domainNameLenth).ToArray();

                        byte[] port = vs.Skip(5 + domainNameLenth).Take(2).ToArray();

                        Port = Convert.ToInt16((port[0].ToString("X2") + port[1].ToString("X2")), 16);
                    }
                    else if (Atype == 0x04)  //ip v6
                    {
                        Address = vs.Skip(4).Take(16).ToArray();
                        Port = BitConverter.ToInt16(vs, 20);
                    }
                    return true;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        public override string ToString()
        {
            //string str = string.Format("{0}{1}{2}{3}{4}{5}", Ver, Cmd, Rsv, Atype, Address, Port);
            //return Encoding.UTF8.GetBytes(str);

            string strAddr = System.Text.Encoding.UTF8.GetString(Address);
            string str = string.Format("ver={0} cmd={1} rsv={2} atype={3} address={4} port={5}", Ver, Cmd, Rsv, Atype, strAddr, Port);
            return str;
        }

        public (bool sucess, Socket remote) ConnectThisSocket()
        {
            foreach (var i in Dns.GetHostEntry(Encoding.UTF8.GetString(Address)).AddressList)
            {
                Socket remote = new Socket(i.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    remote.Connect(i, Port);
                    remote.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
                    //Console.WriteLine("连接 " + i.ToString());
                    return (true, remote);
                }
                catch (Exception ex)
                {
                    remote.Dispose();
                    Console.WriteLine("连接失败：" + ex.Message + " 地址：" + Encoding.UTF8.GetString(Address));
                }
            }
            return (false, null);
        }
    }
}
