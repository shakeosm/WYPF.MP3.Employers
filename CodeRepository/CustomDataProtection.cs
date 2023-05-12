using Effortless.Net.Encryption;



namespace MCPhase3.CodeRepository
{
    public static class CustomDataProtection
    {
        static byte[] key = Bytes.GenerateKey();
        static byte[] iv = Bytes.GenerateIV();

        public static string Encrypt(string inputString)
        {
            string encrypted = Strings.Encrypt(inputString, key, iv);
            var encodeEncrypted = System.Net.WebUtility.UrlEncode(encrypted);
            return encodeEncrypted;
        }



        public static string Decrypt(string inputString)
        {
            //var encrypedIDfromURL = HttpUtility.HtmlEncode(inputString);



            //var encrypedIDfromURL = System.Web.HttpUtility.UrlEncode(inputString);
            var decoded = System.Net.WebUtility.UrlDecode(inputString);



            string decoddedDecrypted = Strings.Decrypt(decoded, key, iv);
            return decoddedDecrypted;
        }




    }
}