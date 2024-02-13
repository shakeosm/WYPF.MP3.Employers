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
using System.IO;
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

        public void ContextRemoveValue(string keyName)
        {
            HttpContext.Session.SetString(keyName, "");
            HttpContext.Session.Remove(keyName);
        }

        //#########################
        //## Life saving methods ##
        //#########################
        //public string CurrentUserId() => HttpContext.Session.GetString(Constants.LoggedInAsKeyName);
        
        /// <summary>Returns w2User UserId- which is used in all Procedures</summary>
        /// <returns></returns>
        public string CurrentUserId() => HttpContext.Session.GetString(Constants.UserIdKey);   
        public string SessionInfoKeyName()=> $"{Constants.SessionInfoKeyName}_{CurrentUserId()}";

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
            if (IsEmpty(value)) {
                throw new ArgumentException("Parameter cannot be null", "Url parameter");
            }
            return _protector.Unprotect(value);
        }

        /// <summary>Only to be used to Encrypt a value to prepare to use in a Query string, which usually requires to be in Encoded format</summary>
        public string EncryptUrlValue(string value)
        {
            if (IsEmpty(value)){
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
            string host = HttpContext.Request.Host.ToString();
            LogInfo($"api: {apiUrl}, host: {host}");

            //HttpContext.Response.Headers.Add("client-id", host);    //## the APi will know who is the Consumer and can log the request accordingly..
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("client-id", host);
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
            string strJson = Newtonsoft.Json.JsonConvert.SerializeObject(paramList);
            using var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(paramList), Encoding.UTF8, "application/json");

            using var response = await httpClient.PostAsync(apiUrl, content);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return result;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {

                var errorDetails = new ErrorViewModel()
                {
                    RequestId = "0",
                    UserId = HttpContext.Session.GetString(Constants.LoginNameKey),
                    ApplicationId = Constants.EmployersPortal,
                    ErrorPath = $"ApiPost()-> {apiUrl}",
                    Message = $"Failed API call with status: {response.StatusCode}",
                    StackTrace = "The api call is failed with the following parameters: <br/>" + strJson
                };

                string insertErrorLogApi = GetApiUrl(_apiEndpoints.ErrorLogCreate);
                await ApiPost(insertErrorLogApi, errorDetails);

                LogInfo($"API-> {apiUrl}, failed, status: {response.StatusCode}, Parameters: {strJson}");

                return null;
            }
            else
            {
                LogInfo($"ERROR >>>>> API-> {apiUrl}, Unhandled exception- status: {response.StatusCode}, Parameters: {strJson}");                
                ContextSetValue(Constants.ApiCallParamObjectKeyName, strJson);

                throw new Exception($"Failed to connect to: {apiUrl}, Status: {response.StatusCode}");
                
            }
        }

        public async Task ErrorLog_Insert(ErrorViewModel errorViewModel)
        {
            string insertErrorLogApi = GetApiUrl(_apiEndpoints.ErrorLogCreate);
            await ApiPost(insertErrorLogApi, errorViewModel);
        }


        public async Task<UserDetailsVM> GetUserDetails(string loginName)
        {
            string cacheKey = $"{loginName}_{Constants.AppUserDetails}";
            var appUser = _cache.Get<UserDetailsVM>(cacheKey);

            if (appUser is null)
            {
                string getUserDetailsApi = GetApiUrl(_apiEndpoints.GetUserDetails);
                var apiResult = await ApiGet(getUserDetailsApi + loginName);
                appUser = JsonConvert.DeserializeObject<UserDetailsVM>(apiResult);

                _cache.Set(cacheKey, appUser);  //## set it, first time only.. subsequent calls will be able to read it from local cache            
                LogInfo($"GetUserDetails() => cacheKey: {cacheKey}");
            }

            return appUser;
        }

        /// <summary>This will create a key name Prefixed with userName() to make it conistant accross all Read/write or Get/Set- whether using Redis or Browser Session.</summary>
        /// <param name="keyName">Key name to create</param>
        /// <returns></returns>
        public string GetKeyName(string keyName)         
        {
            return $"{CurrentUserId()}_{keyName}";
        }

        public bool IsEmpty(string value) 
        {
            return string.IsNullOrEmpty(value);
        }

        public void LogInfo(string message, bool addNewLine = false)
        {
            var logMessageText = $"{DateTime.Now.ToLongTimeString()}> {CurrentUserId()} > {message}";

            #if DEBUG
                if (addNewLine) {
                    Console.WriteLine("");
                }
                Console.WriteLine(logMessageText);
            #endif
            
            if (_configuration["LogDebugInfo"].ToString().ToLower() == "yes")
            {
                var line = Environment.NewLine + Environment.NewLine;
                logMessageText = $"{DateTime.Now.ToLongTimeString()}> {message}";
                try
                {
                    string filepath = _configuration["LogDebugInfoFilePath"].ToString();

                    if (!Directory.Exists(filepath))
                    {
                        Directory.CreateDirectory(filepath);
                    }

                    filepath = filepath + CurrentUserId() + "-" + DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                    if (!System.IO.File.Exists(filepath))
                    {
                        System.IO.File.Create(filepath).Dispose();
                    }

                    using StreamWriter sw = System.IO.File.AppendText(filepath);
                    //sw.WriteLine(line);
                    sw.WriteLine(logMessageText);
                    //sw.WriteLine(line);
                    sw.Flush();
                    sw.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error at: public void LogInfo => " + e.ToString());
                }
            }
        }

        
        public void Log_ClearOlderCustomerFilesNotProcessed(string message)
        {
            //var logMessageText = $"{DateTime.Now.ToLongTimeString()}> {message}";

#if DEBUG
            Console.WriteLine(message);
#endif

            if (_configuration["Log_ClearOlderCustomerFilesNotProcessed"].ToString().ToLower() == "yes")
            {
                var line = Environment.NewLine + Environment.NewLine;
                //logMessageText = $"{DateTime.Now.ToLongTimeString()}> {message}";
                try
                {
                    string filepath = _configuration["LogDebugInfoFilePath"].ToString() + "FileCleanup\\";

                    if (!Directory.Exists(filepath))
                    {
                        Directory.CreateDirectory(filepath);
                    }

                    filepath = filepath + DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                    if (!System.IO.File.Exists(filepath))
                    {
                        System.IO.File.Create(filepath).Dispose();
                    }

                    using StreamWriter sw = System.IO.File.AppendText(filepath);
                    sw.WriteLine(message);
                    sw.Flush();
                    sw.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error at: public void Log_ClearOlderCustomerFilesNotProcessed() => " + e.ToString());
                }
            }
        }
    }

}
