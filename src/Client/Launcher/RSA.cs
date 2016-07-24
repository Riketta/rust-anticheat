using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RGuard
{
    internal class Rijndael
    {
        public static string pubKeyString = "";
        public static RSAParameters pubKey;

        static Rijndael()
        {
            pubKeyString = "<?xml version=\"1.0\" encoding=\"utf-16\"?><RSAParameters xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">  <Exponent>AQAB</Exponent>  <Modulus>sMwIDg3wLijKvFV4KNJ5jooCu7KwQCqDnZrGvJgsLejDMx3f/y4salfWvQyJBaiVIeQdeeXEjDWuE7rZ/KuazjJZxUdCmbkzfulbKAPCdanxcdFd3V4QG5d56sks/CdrtOPfl1kwmGXZZ6m2YdI42RcPjgmAqn0PgGHoahuV1CBd9QgL/jS/ZIeYU17GqeIkCz2v5WuppV54U+a6P/IeuhFMupgPentzMdP6JBWI0qEOfq++1Qf9b/Q/lWAxRInYIBGc0dNouUp3OSlv6fYJXaGKnPuVD/LO2JIwj5Nixtcpwn6oSopoiK5gxzLSiMpe2QiIPKNvb/pruHNKireHDw==</Modulus></RSAParameters>";
            pubKey = ReadKeyFromString(pubKeyString);
        }

        public static RSAParameters ReadKeyFromString(string key)
        {
            StringReader sr = new System.IO.StringReader(key);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            RSAParameters k = (RSAParameters)xs.Deserialize(sr);
            return k;
        }

        public static string Encrypt(string data, RSAParameters pKey)
        {
            try
            {
                RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
                csp.ImportParameters(pubKey);
                byte[] bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(data);
                byte[] bytesCypherText = csp.Encrypt(bytesPlainTextData, false);
                string cypherText = Convert.ToBase64String(bytesCypherText);
                return cypherText;
            }
            catch (Exception ex) { RGuard.Log("RSA ex: " + ex.ToString()); }
            return string.Empty;
        }
    }
}