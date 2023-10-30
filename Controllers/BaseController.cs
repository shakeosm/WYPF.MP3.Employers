using MCPhase3.CodeRepository;
using MCPhase3.Common;
using MCPhase3.Models;
using MCPhase3.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
        /// <param name="returnAsEncrypted">Will return an Encrypted value if True, set false to get Number value</param>        
        /// <returns>Remittance Id in Encrypted format</returns>
        public string GetRemittanceId(bool returnAsEncrypted = true) {            
            string encryptedValue = _cache.GetString(RemittanceIdKeyName());

            string decryptUrlValue = DecryptUrlValue(encryptedValue, forceDecode: false);

            return returnAsEncrypted ? encryptedValue : decryptUrlValue;

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


        /// <summary>
        /// to insert Event Details of a remittance id.
        /// </summary>
        /// <param name="remID"></param>
        /// <returns></returns>
        public async void InsertEventDetails(EventDetailsBO eBO)
        {
            string apiLink = GetApiUrl(_apiEndpoints.InsertEventDetails);
            var apiResult = await ApiPost(apiLink, eBO);           
        }


        public async Task<string> ApiGet(string apiUrl)
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(apiUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return "";  //## will return empty ... don't fight plz...!
            }
            else
            {
                throw new Exception($"Failed to connect to: {apiUrl}, Status: {response.StatusCode}");
            }
        }


        public async Task<string> ApiPost(string apiUrl, object paramList)
        {
            using var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(paramList), Encoding.UTF8, "application/json");

            using var response = await httpClient.PostAsync(apiUrl, content);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return result;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                throw new Exception($"Failed to connect to: {apiUrl}, Status: {response.StatusCode}");
                
            }
        }

        public async Task ErrorLog_Insert(ErrorViewModel errorViewModel)
        {
            string insertErrorLogApi = GetApiUrl(_apiEndpoints.ErrorLogCreate);
            await ApiPost(insertErrorLogApi, errorViewModel);
        }

    }
}
