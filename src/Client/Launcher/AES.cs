using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace RGuard
{
    public class AES
    {
        public static string Encrypt(string plainText, byte[] key, byte[] iv)
        {
            byte[] encrypted;
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        swEncrypt.Write(plainText);
                    encrypted = msEncrypt.ToArray();
                }
            }
            return Convert.ToBase64String(encrypted);
        }
    }
}
