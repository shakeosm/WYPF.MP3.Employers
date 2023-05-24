using Effortless.Net.Encryption;
using Microsoft.AspNetCore.DataProtection;
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
       

    }

    public class CustomDataProtection_New
    {
        static byte[] key = Bytes.GenerateKey();
        static byte[] iv = Bytes.GenerateIV();

        private readonly IDataProtector protector;
        //public CustomDataProtection(IDataProtectionProvider dataProtectionProvider, UniqueCode uniqueCode)
        public CustomDataProtection_New(IDataProtectionProvider dataProtectionProvider)
        {
            protector = dataProtectionProvider.CreateProtector("MCPhase3.CodeRepository.CustomDataProtection");
        }
        public string Decode(string data)
        {
            return protector.Protect(data);
        }
        public string Encode(string data)
        {
            return protector.Unprotect(data);
        }




    }
}