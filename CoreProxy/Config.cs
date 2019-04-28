using System;
using System.Collections.Generic;
using System.Text;

namespace CoreProxy
{
    public class Config
    {
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 本地地址
        /// </summary>
        public string LocalAddress { get; set; }

        /// <summary>
        /// 本地端口
        /// </summary>
        public int LocalPort { get; set; }

        /// <summary>
        /// 远程地址
        /// </summary>
        public string RemoteAddress  { get; set; }

        /// <summary>
        /// 远程端口
        /// </summary>
        public int RemotePort  { get; set; }
    }
}
