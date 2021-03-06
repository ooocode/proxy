﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CoreProxy.Common
{
    /// <summary>
    /// AES-256-CBC加密
    /// </summary>
    static public class Crypto
    {
        /// <summary>
        /// 密码
        /// </summary>
        public static string Password { get; set; } = "gfhhsjl;itgdrfh";


        /// <summary>
        /// 产生256位密码
        /// </summary>
        /// <returns></returns>
        private static byte[] MakeKey()
        {
            while (Password.Length < 32)
            {
                Password += "0";
            }
            return Encoding.UTF8.GetBytes(Password.Substring(0, 32));
        }


        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="text">需要加密的文本</param>
        /// <returns>返回[iv][payload]</returns>
        public static byte[] EncryptAES(byte[] data)
        {
            if(data == null || data.Length <= 0)
            {
                throw new Exception("EncryptAES长度<=0");
            }

            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(iv);
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = MakeKey();
                aes.IV = iv;
                using (var transform = aes.CreateEncryptor())
                {
                    byte[] en = transform.TransformFinalBlock(data, 0, data.Length);
                    byte[] final = new byte[16 + en.Length];
                    Array.Copy(iv, 0, final, 0, 16);
                    Array.Copy(en, 0, final, 16, en.Length);
                    return final;
                }
            }
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="text">密文</param>
        /// <returns>返回明文</returns>
        public static byte[] DecryptAES(byte[] data)
        {
            if (data == null || data.Length <= 0)
            {
                throw new Exception("DecryptAES长度<=0");
            }

            byte[] iv = new byte[16];
            Array.Copy(data, 0, iv, 0, 16);

            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = MakeKey();
                aes.IV = iv;
                using (var transform = aes.CreateDecryptor())
                {
                    return transform.TransformFinalBlock(data, 16, data.Length - 16);
                }
            }
        }
    }
}
