using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaEncryption
    {
        private static byte[] _key = null;


        private static byte[] hashKey
        {
            get
            {
                return _key ?? (_key = Encoding.ASCII.GetBytes(Properties.Settings.Default.InstaHashKey));
            }

        }
        public static string getSignature(string strData)
        {
            HMACSHA256 hmac = new HMACSHA256(hashKey);

            byte[] data = System.Text.Encoding.UTF8.GetBytes(strData);

            byte[] hashBytes = hmac.ComputeHash(data);

            string strRet = "";
            for (int i = 0; i < hashBytes.Length; i++)
            {
                strRet = strRet + hashBytes[i].ToString("x2");
            }
            return strRet;
        }
    }
}
