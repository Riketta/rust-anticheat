using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RowAC
{
    class AES
    {
        public static string Decrypt(string base64Encrypted, string base64Key, string base64IV)
        {
            byte[] cipherText = Convert.FromBase64String(base64Encrypted);
            string plaintext = null;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(base64Key);
                aes.IV = Convert.FromBase64String(base64IV);

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    plaintext = srDecrypt.ReadToEnd();
            }
            return plaintext;
        }
    }
}
