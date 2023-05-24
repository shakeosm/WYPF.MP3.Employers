using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using MCPhase3.CodeRepository;
using MCPhase3.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection.Metadata;
using System.Web;

namespace MCPhase3.Controllers
{
    public class BaseController : Controller
    {
        private readonly IConfiguration _configuration;
        public readonly IRedisCache _cache;

        public BaseController(IConfiguration configuration, IRedisCache Cache)
        {
            _configuration = configuration;
            _cache = Cache;
        }

        //###################################################################
        //###############   HttpContext.Session Values    ###################
        //###################################################################
        public string ContextGetValue(string keyName)
        {
            return HttpContext.Session.GetString($"{keyName}");
        } 

        public void ContextSetValue(string keyName, string value)
        {
            HttpContext.Session.SetString($"{keyName}", value);
        }

        //#########################
        //## Life saving methods ##
        //#########################
        public string CurrentUserId() => HttpContext.Session.GetString(Constants.UserIdKeyName);

        /// <summary>This will return Remittance Id for the current session</summary>
        /// <param name="returnEncryptedIdOnly">Will return an Encrypted value if True, set false to get Number value</param>
        /// <param name="forceDecode">If the previous parameter is already in decoded format then we set this as False, no need for an extra Decode operation before Decrypttion</param>
        /// <returns></returns>
        public string RemittanceId(bool returnEncryptedIdOnly = true) {
            string sessionKeyRemittanceID = $"{CurrentUserId()}_{Constants.SessionKeyRemittanceID}";
            string encryptedValue = _cache.GetString(sessionKeyRemittanceID);

            string decryptUrlValue = DecryptUrlValue(encryptedValue, forceDecode: false);

            return returnEncryptedIdOnly ? encryptedValue : decryptUrlValue;

        }


        public string ConfigGetValue(string keyName) => _configuration.GetValue<string>(keyName);


        //###################################################################
        //#################   Encryption / Decryption    ####################
        //###################################################################
        /// <summary>Only to be used to Decrypt a Query string value, which usually comes in Encoded format</summary>
        public string DecryptUrlValue(string value, bool forceDecode = true)
        {
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentException("Parameter cannot be null", "Url parameter");
            }
            //var result = HttpUtility.UrlDecode(value);
            return CustomDataProtection.Decrypt(value, forceDecode);
        }

        /// <summary>Only to be used to Encrypt a value to prepare to use in a Query string, which usually requires to be in Encoded format</summary>
        public string EncryptUrlValue(string value)
        {
            if (string.IsNullOrEmpty(value)){
                throw new ArgumentException("Parameter cannot be null", "Url parameter");
            }
            return CustomDataProtection.Encrypt(value);
            //return HttpUtility.UrlEncode(value);
        }

    }
}
