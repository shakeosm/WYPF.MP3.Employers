using MC3StaffAdmin.Models;
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
using System.Text.RegularExpressions;
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
        public string CurrentUserLoginId() => HttpContext.Session.GetString(Constants.LoginNameKey);   
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
            LogInfo($"api: {apiUrl}");

            //HttpContext.Response.Headers.Add("client-id", host);    //## the APi will know who is the Consumer and can log the request accordingly..
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("client-id", host);

            using var response = await httpClient.GetAsync(apiUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                string outputString = responseContent.Length > 200 ? responseContent[..200] : responseContent;
                LogInfo($"ApiGet() -> responseContent: {outputString}");
                return responseContent;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                LogInfo($"ApiGet Call '{apiUrl}' -> status: NotFound(). Message: {response.ToString} ");
                return "";  //## will return empty ... don't fight plz...!
            }
            else
            {
                throw new Exception($"Failed to connect to: {apiUrl}, Status: {response.StatusCode}");
            }
        }

        public string ApiGet_NonAsync(string apiUrl)
        {
            string host = HttpContext.Request.Host.ToString();
            LogInfo($"ApiGet_NonAsync(): {apiUrl}");


            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("client-id", host);

            var webRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            //{
            //    Content = new StringContent("{ 'some': 'value' }", Encoding.UTF8, "application/json")
            //};

            var response = client.Send(webRequest);

            using var reader = new StreamReader(response.Content.ReadAsStream());
            string outputString = reader.ReadToEnd();

            LogInfo("ApiGet_NonAsync() -> StreamReader: " + outputString);

            return outputString;
        }

  
        public async Task<string> ApiPost(string apiUrl, object paramList)
        {
            string strJson = Newtonsoft.Json.JsonConvert.SerializeObject(paramList);
            var content = new StringContent(JsonConvert.SerializeObject(paramList), Encoding.UTF8, "application/json");
            if (strJson.Length > 150) {
                strJson = strJson[..150];
            }
            string host = HttpContext.Request.Host.ToString();
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(Constants.ClientIdHttpRequestKey, host);    //## the APi will know who is the Consumer and can log the request accordingly..            
            httpClient.DefaultRequestHeaders.Add(Constants.PortalNameHttpRequestKey, Constants.ThisPortalName);    //## the APi will know who is the Consumer and can log the request accordingly..            
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

                LogInfo($"ApiPost()-> {apiUrl}, failed, status: {response.StatusCode}, Parameters: {strJson}");

                return null;
            }
            else
            {
                LogInfo($"ERROR >>>>> API-> {apiUrl}, Unhandled exception- status: {response.StatusCode}, Parameters: {strJson}");                
                ContextSetValue(Constants.ApiCallParamObjectKeyName, strJson);

                throw new Exception($"Failed to connect to: {apiUrl}, Status: {response.StatusCode}");
                
            }
        }

        public async Task<bool> ApiPost_NonAsync(string apiUrl, object paramList)
        {
            string strJson = Newtonsoft.Json.JsonConvert.SerializeObject(paramList);
            var content = new StringContent(JsonConvert.SerializeObject(paramList), Encoding.UTF8, "application/json");
            if (strJson.Length > 150) {
                strJson = strJson[..150];
            }
            string host = HttpContext.Request.Host.ToString();
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("client-id", host);    //## the APi will know who is the Consumer and can log the request accordingly..            
            
            //HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            //using var response = httpClient.Send(req);

            using var response = await httpClient.PostAsync(apiUrl, content);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                LogInfo("Got result from api: " + apiUrl + ", >> Json Object: " + strJson);
            }           
            else
            {
                LogInfo($"ERROR >>>>> ApiPost_NonAsync() API-> {apiUrl}, Unhandled exception- status: {response.StatusCode}, Parameters: {strJson}");                               
            }

            return true;
        }

        public async Task ErrorLog_Insert(ErrorViewModel errorViewModel)
        {
            string insertErrorLogApi = GetApiUrl(_apiEndpoints.ErrorLogCreate);
            await ApiPost(insertErrorLogApi, errorViewModel);
        }

        /// <summary>This will take UPM LoginName and match it with w2User and return User details, ie: FullName, Email, Job Title</summary>
        /// <param name="loginName">UPM LoginName</param>
        /// <returns>UserDetailsVM object</returns>
        public async Task<UserDetailsVM> GetUserDetails(string loginName)
        {
            string cacheKey = $"{loginName.ToUpper()}_{Constants.AppUserDetails}";
            var appUser = _cache.Get<UserDetailsVM>(cacheKey);

            if (appUser is null)
            {
                string getUserDetailsApi = GetApiUrl(_apiEndpoints.GetUserDetails);
                var apiResult = await ApiGet(getUserDetailsApi + loginName);
                appUser = JsonConvert.DeserializeObject<UserDetailsVM>(apiResult);

                appUser.IsSuperUser = I_Am_A_SuperUser();

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

        public bool I_Am_A_SuperUser()
        {
            var value = ContextGetValue(Constants.LoggedInAs_SuperUser);
            return Convert.ToBoolean(value) == true;
        }
        
        public void Log_ClearOlderCustomerFilesNotProcessed(string message)
        {
            //var logMessageText = $"{DateTime.Now.ToLongTimeString()}> {message}";

            LogInfo("Log_ClearOlderCustomerFilesNotProcessed()" + message);

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

        public async Task InsertNotification_For_SubmittedToWYPF(int currentRemittance)
        {
            LogInfo($"InsertNotification_For_SubmittedToWYPF()-> Remittance ID: {currentRemittance}. Current Status: 110.");
            //## Now insert a notification for this Submission- If the Status = '110: Submitted to WYPF'.. The Finance Business Partner needs to know a new Submission being pushed for further processing
            var notifToFBP = new EventDetailsBO() { remittanceID = currentRemittance };
            string apirUrl = GetApiUrl(_apiEndpoints.SubmissionNotification_CreateNew);
            _ = await ApiPost(apirUrl, notifToFBP); //## no need to wait/know the result.. just go back to the UI and take the json result..
        }



        
        public RemittanceStatusAndScoreVM GetRemittanceInfo(int currentRemittance)
        {            
            //## Now insert a notification for this Submission- If the Status = '110: Submitted to WYPF'.. The Finance Business Partner needs to know a new Submission being pushed for further processing
            string apirUrl = GetApiUrl(_apiEndpoints.GetRemittanceInfo);
            var apiResult = ApiGet_NonAsync($"{apirUrl}{currentRemittance}");
            var remittanceInfoVM = JsonConvert.DeserializeObject<RemittanceStatusAndScoreVM>(apiResult);

            return remittanceInfoVM;            
        }

        public async Task<string> Get_Finance_Business_Partner_By_PayLocation(string payLocationRef)
        {
            string apiUrl = GetApiUrl(_apiEndpoints.GetFinanceBusinessPartnerByPayLocation);    //## api/Get_Finance_Business_Partner_By_PayLocation
            var apiResult = await ApiGet($"{apiUrl}{payLocationRef}");
            var fbpDetails = JsonConvert.DeserializeObject<PayLocationWithFinBusPartnerVM>(apiResult);

            return fbpDetails.FBP_UserId;
        }

        public bool PaswordPolicyMatched(string userPassword)
        {
            string loginName = ContextGetValue(Constants.LoginNameKey);
            if (IsEmpty(userPassword) || userPassword.Contains(loginName))
            {
                return false;
            }

            string passwordPattern = @"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[`!@$%^&*(){}[\];'#:@~<>?/|\-\=\+]).{12,}$";
            var passwordStrength = Regex.Match(userPassword, passwordPattern);

            return passwordStrength.Success;
        }


        public async Task<string> GetPasswordMeterValue(string passwordToCheck)
        {
            var apiResult = await ApiGet(GetApiUrl(_apiEndpoints.ValidatePassword) + passwordToCheck);
            var passwordMeterResult = JsonConvert.DeserializeObject<string>(apiResult);

            return passwordMeterResult;
        }

    }

}
