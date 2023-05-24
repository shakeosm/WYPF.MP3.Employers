using Effortless.Net.Encryption;
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
                string decrypted = "";
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
       

    }
}