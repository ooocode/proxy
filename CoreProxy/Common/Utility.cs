using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CoreProxy.Common
{
    public class Utility
    {
        /// <summary>
        /// 产生随机字节数组
        /// </summary>
        /// <param name="n">位数</param>
        /// <returns></returns>
        public static byte[] MakeRandomBytes(uint n)
        {
            byte[] vs = new byte[n];
            using (RandomNumberGenerator random = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                random.GetNonZeroBytes(vs);
            }
            return vs;
        }
    }
}
