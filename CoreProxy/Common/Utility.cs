using System;
using System.Collections.Generic;
using System.Linq;
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


        /// <summary>
        /// 合并字节数组
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static byte[] MergeBytes(byte[] b1,byte[] b2)
        {
            byte[] dest = new byte[b1.Length + b2.Length];

            Array.Copy(b1, 0, dest, 0, b1.Length);
            Array.Copy(b2, 0, dest, b1.Length, b2.Length);
            return dest;
        }


        /// <summary>
        /// 移除字节数组前面的N个字节
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static byte[] RemoveNBytes(byte[] b,int n)
        {
            return b.Skip(n).ToArray();
        }
    }
}
