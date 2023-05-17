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
            return encrypted;
        }

        public static string Decrypt(string inputString)
        {
            var encrypedIDfromURL = HttpUtility.HtmlEncode(inputString);

            string decrypted = Strings.Decrypt(encrypedIDfromURL, key, iv);
            return decrypted;
        }
       

    }
}