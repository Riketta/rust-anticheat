using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RowAC
{
    internal class RSA
    {
        public static string privKeyString = "";
        public static RSAParameters privKey;

        static RSA()
        {
            privKeyString = "<?xml version=\"1.0\" encoding=\"utf-16\"?><RSAParameters xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"> <Exponent>AQAB</Exponent> <Modulus>sMwIDg3wLijKvFV4KNJ5jooCu7KwQCqDnZrGvJgsLejDMx3f/y4salfWvQyJBaiVIeQdeeXEjDWuE7rZ/KuazjJZxUdCmbkzfulbKAPCdanxcdFd3V4QG5d56sks/CdrtOPfl1kwmGXZZ6m2YdI42RcPjgmAqn0PgGHoahuV1CBd9QgL/jS/ZIeYU17GqeIkCz2v5WuppV54U+a6P/IeuhFMupgPentzMdP6JBWI0qEOfq++1Qf9b/Q/lWAxRInYIBGc0dNouUp3OSlv6fYJXaGKnPuVD/LO2JIwj5Nixtcpwn6oSopoiK5gxzLSiMpe2QiIPKNvb/pruHNKireHDw==</Modulus> <P>8Qb2HiGRmxnQux1LGBxILSLoH21c0jyWYpfWYwVjlHzFf4mKf+Xwg/QPMMdJkXGrXM0oA6BbkdLsTNRsFkUxFV2sKoBxlQpmb/skdA/FGtRr2YBzO0kawcuSddRbxmRDik3T3Hzm6I0M76N5yXtrU2S9qPueTWj9bTb1/nVeivM=</P> <Q>u8efK/mPR67M64X6ZriCPXR7j2R8psFmJhPtvzJS+8vOjp+1TnBFQ3H7RcXvAJG9jmV+at+Iu3ERaaJ15ezG7iMXsnD7KNb7j2tc9EejauyUi3hKAxT/QdrMGem4GIqgZOYqVE8ElTiH22tdpMrUd6kWxdk4pVCsDRAvPcerYnU=</Q> <DP>AS1Hhl4jl95IZqF9/GAm+hFxkLW3/k7NbS3QnisokVEKpdTGGFnHEt3eNR7D/THQ5GMcDuh5ify9qqJe5LzxwGj0rkByTYf/eAyB4Q8ypy7iV+2IooF43/lefbTLvew/aC15G1qAxiHqLkFeFt3DaGTViD2ySC57Dk12ZgesroE=</DP> <DQ>ST9I24JxXWjWDlkon8EBLK+vMvPjm7h8/AVyC865h/asD/5EXuB0ZCal+UWIQRSYeF8mvNGNKHCmdiolCxcdUe7mY3imv/t8DSm4DKGVITQ/jVfSpvkdyLZsPv9oDEqm3jTZ9iEMjJiMhg6PbKSh1Dtk4rAk5HdfZYkWpGaqd7E=</DQ> <InverseQ>7XD9fB2sWLETBmNUFmUj5gIBg2LiPMiIb+y5o8thzYRZDU3cuaxHAssajA5sINgXGLmWr9a73S+c7goBm/2mlPOLO4jLVLC2NsRLIBO1KNHYJbS2f8UnwMC74jeo2fO2PToOSHBJ1U9L1D6i3QDfOVJFQj/iTdgLPV5tFs9hqxY=</InverseQ> <D>AfK681Nc/lfjEAXjFT691H/EfZv/oYgLu5IH4ZkjAih/9NFon/plcTs25GNoeSBSmwp/9qa9A+HYNrhxjCgPRHln5YAeWuz1r87TeyfmHdTPEnbWUXOmusnFr/wihkQgRYst8hArXxU0Ouuy/6CePBTVzjYK7cCuCeH0+oJY+XrobyjcO8EtcPxiy5LOaKnibcU3tib/aUDE2stIIJhuE+W86ZjEMv704IHVH4r0yfNvlCLAI11ez2/f3URSbQ91UoIRUgrflBHS4YaKJX0bYlT2Qg6RIamRZ6QLqsinGRtBiG4gTaFjqzMIztIW/egNWTO270lguxjo6DDjgHX1bQ==</D></RSAParameters>";
            privKey = ReadKeyFromString(privKeyString);
        }

        public static RSAParameters ReadKeyFromString(string key)
        {
            StringReader sr = new System.IO.StringReader(key);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            RSAParameters k = (RSAParameters)xs.Deserialize(sr);
            return k;
        }

        public static string Decrypt(string cryptedData, RSAParameters pKey)
        {
            try
            {
                byte[] bytesCypherText = Convert.FromBase64String(cryptedData);
                RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
                csp.ImportParameters(pKey);
                byte[] bytesPlainTextData = csp.Decrypt(bytesCypherText, false);
                string data = System.Text.Encoding.Unicode.GetString(bytesPlainTextData);
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return string.Empty;
            }
        }
    }
}
