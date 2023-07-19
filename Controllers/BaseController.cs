using MCPhase3.CodeRepository;
using MCPhase3.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace MCPhase3.Controllers
{
    public class BaseController : Controller
    {
        private readonly IConfiguration _configuration;
        public readonly IRedisCache _cache;
        public readonly IDataProtectionProvider provider;
        public readonly ApiEndpoints _apiEndpoints;
        private readonly IDataProtector _protector;

        public BaseController(IConfiguration configuration, IRedisCache Cache, IDataProtectionProvider Provider, IOptions<ApiEndpoints> ApiEndpoints)
        {
            _configuration = configuration;
            _cache = Cache;
            provider = Provider;
            _apiEndpoints = ApiEndpoints.Value;
            _protector = provider.CreateProtector("MCPhase3.Protector");
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
        public string SessionInfoKeyName()=> $"{CurrentUserId()}_{Constants.SessionInfoKeyName}";

        /// <summary>This will return Remittance Id for the current session. By default this will return Remittance Id in Encrypted format.</summary>
        /// <param name="returnEncryptedIdOnly">Will return an Encrypted value if True, set false to get Number value</param>        
        /// <returns>Remittance Id in Encrypted format</returns>
        public string GetRemittanceId(bool returnEncryptedIdOnly = true) {            
            string encryptedValue = _cache.GetString(RemittanceIdKeyName());

            string decryptUrlValue = DecryptUrlValue(encryptedValue, forceDecode: false);

            return returnEncryptedIdOnly ? encryptedValue : decryptUrlValue;

        }

        public string RemittanceIdKeyName() => $"{CurrentUserId()}_{Constants.SessionKeyRemittanceID}";


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
            return _protector.Unprotect(value);
        }

        /// <summary>Only to be used to Encrypt a value to prepare to use in a Query string, which usually requires to be in Encoded format</summary>
        public string EncryptUrlValue(string value)
        {
            if (string.IsNullOrEmpty(value)){
                throw new ArgumentException("Parameter cannot be null", "Url parameter");
            }
            return _protector.Protect(value);
        }

        /// <summary>This will get the complete API Endpoint, base Url + Endpoint name</summary>
        /// <param name="apiName">Endpoint name</param>
        /// <returns>Complete Url</returns>
        public string GetApiUrl(string apiName)         
        {
            return _apiEndpoints.WebApiBaseUrl + apiName;
        }

    }
}
