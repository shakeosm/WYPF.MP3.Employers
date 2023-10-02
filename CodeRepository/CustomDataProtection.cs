using Effortless.Net.Encryption;
using System;
using System.Collections;
using System.Web;

namespace MCPhase3.CodeRepository
{
    public static class CustomDataProtection
    {
        static byte[] key = Bytes.GenerateKey();
        static byte[] iv = Bytes.GenerateIV();
        

        public static string Encrypt(string inputString)
        {
            string encrypted = Strings.Encrypt(inputString, key, iv);
            var value = HttpUtility.HtmlEncode(encrypted);
            return value;
        }

        public static string Decrypt(string inputString, bool forceDecode = true)
        {
            try
            {
                string decrypted = inputString;
                if (forceDecode) { 
                    decrypted = HttpUtility.UrlDecode(inputString);
                }

                decrypted = Strings.Decrypt(decrypted, key, iv);
                return decrypted;
            }
            catch (System.Exception)
            {

                return Strings.Decrypt(inputString, key, iv);
            }
        }

        /// <summary>Admin Portal may send us Encrypted IDs.. we need to use same Protector to Decrypt that value</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string DecryptUrlValueFromAdminPortal(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Parameter cannot be null", "Url parameter");
            }
            return Decrypt(value);
        }


    }
    
}