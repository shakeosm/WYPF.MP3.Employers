using MCPhase3.CodeRepository;
using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static MCPhase3.Common.Constants;

namespace MCPhase3.Controllers
{
    public class LoginController : BaseController
    {
        PayrollProvidersBO payrollBO = new PayrollProvidersBO();
        LoginViewModel loginDetails = new LoginViewModel();

        string uploadedFileName = string.Empty;
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration, IRedisCache cache, IDataProtectionProvider Provider, IOptions<ApiEndpoints> ApiEndpoints) : base(configuration, cache, Provider, ApiEndpoints)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            //## following function will check if session has value then login user with out showing login page again.
            bool sessionResult = SessionHasValue();

            if (sessionResult)
            {
                return RedirectToAction("Index", "Admin");
            }

            ClearTempData();
            return View(loginDetails);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel loginDetails)
        {
            //check username and password
            var loginResult = await LoginCheckMethod(loginDetails.UserName, loginDetails.Password);

            //## Yes, the user is valid, but we haven't Logged the user in yet. need to see if there is another session running anywhere
            if (loginResult == (int)LoginStatus.Valid)
            {

                var sessionInfo = GetUserSessionInfo(loginDetails);

                //## we need to confirm their is only one login session for this user.. 
                if (sessionInfo.HasExistingSession)
                {
                    //## Notify the user - whether they wanna kill the existing one and continue here...?
                    sessionInfo.Password = ""; //## don't take the password back to the UI
                    return View("MultipleSessionPrompt", sessionInfo);

                }
                //## if no 'HasExistingSession' - then proceed to login and take the user to Admin/Home page
                return await ProceedToLogIn(loginDetails);

            }
            else if (loginResult == (int)LoginStatus.Locked)
            {
                TempData["Msg1"] = AccountLockedMessage;
                loginDetails.LoginErrorMessage = AccountLockedMessage;
                return View(loginDetails);
            }
            else if (loginResult == (int)LoginStatus.Failed)
            {
                TempData["Msg1"] = AccountFailedLoginMessage;
                loginDetails.LoginErrorMessage = AccountFailedLoginMessage;
                return View(loginDetails);
            }

            //## if none of the above were true- which is not possible- then take back to login screen again.. where else!?
            return View(loginDetails);

        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedCurrentSession(LoginViewModel vm)
        {
            //## The user wasn't logged in, rather shown a warning message to take action- coz they have another session opened somewhere... 
            //## Now the user has decided to stay/continue this current login.. and kills the other session...
            var currentBrowserSessionId = Guid.NewGuid().ToString();
            var sessionInfo = GetUserSessionInfo(vm);

            _cache.Delete(SessionInfoKeyName());

            //## Create a new one
            sessionInfo.HasExistingSession = false;
            sessionInfo.LastLoggedIn = DateTime.Now.ToString();
            sessionInfo.SessionId = currentBrowserSessionId;
            _cache.Set(SessionInfoKeyName(), sessionInfo);

            //## create entries in Session Cookies, too..
            ContextSetValue(Constants.SessionGuidKeyName, currentBrowserSessionId); //## will use this on page navigation- to see whether user has started another session and requested to kill this session

            //## The user was authenticated first, then shown a screen - either to continue on this browser or close this browser.            
            return await ProceedToLogIn(vm);
        }


        private async Task<IActionResult> ProceedToLogIn(LoginViewModel vm)
        {
            var fireSchemeId = _configuration.GetValue<string>("ValidSchemesId")
                                               .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            try
            {
                //Once usernames copied to new table then uncomment following line of code
                payrollBO = await CallPayrollProviderService(vm.UserName);
            }
            catch (Exception ex)
            {
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }

            // loginDetails.clientId = payrollBO.pay_location_ID.ToString();
            vm.ClientId = payrollBO.client_Id;


            //check user login details            
            HttpContext.Session.SetString(Constants.SessionKeyClientId, vm.ClientId);
            HttpContext.Session.SetString(Constants.SessionKeyUserID, vm.UserName);
            HttpContext.Session.SetString(Constants.UserIdKeyName, vm.UserName);
            HttpContext.Session.SetString(Constants.SessionKeyPayLocName, payrollBO.pay_location_name);
            HttpContext.Session.SetString(Constants.SessionKeyPayLocId, payrollBO.pay_location_ID.ToString());
            HttpContext.Session.SetString(Constants.SessionKeyEmployerName, payrollBO.pay_location_name);
            //following is a payrollprovider
            HttpContext.Session.SetString(Constants.SessionKeyPayrollProvider, payrollBO.paylocation_ref);

            TempData["ps"] = vm.UserName;

            if (fireSchemeId.Contains(vm.ClientId.Trim()))
            {
                TempData["MainHeading"] = "Fire - Contribution Advice";
                TempData["isFire"] = true;
                HttpContext.Session.SetString(SessionKeyClientType, "FIRE");
            }
            else
            {
                TempData["isFire"] = false;
                HttpContext.Session.SetString(SessionKeyClientType, "LG");
            }

            return RedirectToAction("Index", "Admin");

        }


        /// <summary>This will read Redis cache and find if there is any entry for this user session</summary>
        /// <param name="userId">User Id</param>
        /// <returns>UserSessionInfoVM object</returns>
        private UserSessionInfoVM GetUserSessionInfo(LoginViewModel vm)
        {
            //## Get the session info from Redis cache            
            HttpContext.Session.SetString(Constants.UserIdKeyName, vm.UserName);
            var sessionInfo = _cache.Get<UserSessionInfoVM>(SessionInfoKeyName());   //## this KeyName should be used in Logout- to Delete the Redis entry

            if (sessionInfo is null)
            {
                var currentBrowserSessionId = Guid.NewGuid().ToString();

                ContextSetValue(Constants.SessionGuidKeyName, currentBrowserSessionId); //## will use this on page navigation- to see whether user has started another session and requested to kill this session
                ContextSetValue(Constants.UserIdKeyName, vm.UserName);

                //## No user session info in the Cache..  so create an entry to stop any further login attempt from another browser                
                sessionInfo = new UserSessionInfoVM()
                {
                    UserName = vm.UserName,
                    Password = vm.Password,
                    BrowserId = vm.BrowserId,
                    WindowsId = vm.WindowsId,
                    HasExistingSession = false,
                    LastLoggedIn = DateTime.Now.ToString(),
                    SessionId = currentBrowserSessionId
                };
                _cache.Set(SessionInfoKeyName(), sessionInfo);

                return sessionInfo;
            }
            else
            {
                //## The following is when user will try to use 'Incongnito' on Chrome.. very clever!
                //##means there is an entry for this user in cache.. and the user is just trying to be cleaver.. don't let them Login
                sessionInfo.HasExistingSession = true;
                _cache.Set(SessionInfoKeyName(), sessionInfo);
                return sessionInfo;
            }
        }


        /// <summary>
        /// following method will show main payroll provider with login name and id
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        private async Task<PayrollProvidersBO> CallPayrollProviderService(string userName)
        {
            string apiBaseUrlForPayrollProvider = GetApiUrl(_apiEndpoints.PayrollProvider);
            using (var httpClient = new HttpClient())
            {
                using var response = await httpClient.GetAsync(apiBaseUrlForPayrollProvider + userName);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    payrollBO = JsonConvert.DeserializeObject<PayrollProvidersBO>(result);
                }
            }

            return payrollBO;
        }
        /// <summary>
        /// Check if username and password correct
        /// </summary>
        /// <param name="loginBO"></param>
        /// <returns></returns>
        [Obsolete("Do not use this one.. Instead use- LoginCheckMethod(string userId, string password)")]
        private async Task<int> LoginCheckMethod(LoginBO loginBO)
        {
            string apiBaseUrlForLoginCheck = GetApiUrl(_apiEndpoints.LoginCheck);

            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(loginBO), Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync(apiBaseUrlForLoginCheck, content);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    loginBO.result = JsonConvert.DeserializeObject<int>(result);
                }
            }
            return loginBO.result;
        }

        private async Task<int> LoginCheckMethod(string userId, string password)
        {
            var loginBO = new LoginBO()
            {
                userName = userId,
                password = password
            };

            string apiBaseUrlForLoginCheck = GetApiUrl(_apiEndpoints.LoginCheck);
            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(loginBO), Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync(apiBaseUrlForLoginCheck, content);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    loginBO.result = JsonConvert.DeserializeObject<int>(result);
                }
            }
            return loginBO.result;
        }


        /// <summary>
        /// following function will check everytime someone comes to 
        /// login page if session has values then empty it before new login.
        /// </summary>
        private bool SessionHasValue()
        {
            bool result = false;

            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(Constants.SessionKeyUserID)))
            {
                result = true;
            }

            TempData["Msg1"] = null;  //## to clear any previously stored garbage.. we should avoid using this TempDtat completely
            TempData["msg"] = null;

            return result;
        }

        public IActionResult Logout()
        {

            //## clear the Redis cache... so the user can login next time easily            
            //## but delete if this is your Redis session.. 
            //## Scenario: user logged in from Browser 2 and wanna kick out Browser1 session.
            //##  When logged in using Browser2- they already have cleared session for Browser_1. So- don't just delete a Redis session if that session doesn't belong to current Browser session Id            

            string currentUserId = CurrentUserId();

            var currentBrowserSessionId = ContextGetValue(SessionGuidKeyName);
            var sessionKeyName = $"{currentUserId}_{SessionInfoKeyName}";
            var sessionInfo = _cache.Get<UserSessionInfoVM>(sessionKeyName);

            _cache.DeleteUserSession(currentUserId);    //## Deletes the User session in Redis Cache..

            //## Browser Session Id and Redis SessionId-> are they same..?
            if (sessionInfo != null)
            {
                if (currentBrowserSessionId == sessionInfo.SessionId)
                {
                    _cache.Delete(sessionKeyName);
                    _cache.Delete(currentBrowserSessionId);
                }
            }

            ClearTempData();            //## Clear all the 'TempData["XXX"]' values
            ClearSessionValues();       //## Clear all the Http.Session values

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            // await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }


        /// <summary>This will show the Login page again with proper message for an expired session
        /// also, delete any session in Browser.</summary>
        /// <returns>Login Page with 'session expired' message</returns>
        public IActionResult SessionExpired()
        {
            ClearTempData();
            ClearSessionValues();
            loginDetails.LoginErrorMessage = SessionExpiredMessage;

            return View("Index", loginDetails);

        }

        private void ClearTempData()
        {
            TempData["ps"] = null;
            TempData["Msg"] = null;
            TempData["Msg1"] = null;
            TempData["MsgM"] = null;
        }

        /// <summary>This will delete all Context.Session for the current browser</summary>
        private void ClearSessionValues()
        {
            HttpContext.Session.Clear();

            HttpContext.Session.Remove(SessionKeyUserID);
            HttpContext.Session.Remove(SessionKeyPayLocName);
            HttpContext.Session.Remove(SessionKeyPayLocId);
            HttpContext.Session.Remove(SessionKeyClientId);
            HttpContext.Session.Remove(SessionKeyClientType);
            HttpContext.Session.Remove(SessionKeyEmployerName);
            HttpContext.Session.Remove(SessionKeyPayrollProvider);

            HttpContext.Response.Cookies.Delete(SessionKeyUserID);
            HttpContext.Response.Cookies.Delete(".AspNetCore.Session");
            HttpContext.Response.Cookies.Append(".AspNetCore.Session", "test");
        }

        /// <summary>
        /// This will only be called from the ActionFilter -> when the ServerAdmin changes password-
        /// and calls the 'ClearRedisUserSession()' method- that clears the current Redis user session,
        /// And then when the user tries to navigate to other page- we capture the request- 
        /// then bring the user here- to clean the current browser cookies/session- 
        /// and force the user to login with new password.
        /// </summary>
        /// <returns></returns>
        public IActionResult ClearSessionAndLogin()
        {
            ClearTempData();
            ClearSessionValues();
            //## But don't clear the Redis session for current User..

            return RedirectToAction("Index", "Login");
        }

        /// <summary>This EndPoint will be used only by AdminPortal. Everytime there is a 
        /// Password changed for a user- we need to log that user out from their current session.
        /// We will call Redis to Delete all the relevent session for this user.</summary>
        /// <param name="id">Encrypted User id</param>
        /// <returns>JSon Text</returns>
        public IActionResult ClearRedisUserSession(string id)
        {
            if (string.IsNullOrEmpty(id) == false)
            {
                _cache.DeleteUserSession(id);
                ClearTempData();
                ClearSessionValues();

                return Json("success");
            }
            return Json("who are you?");
        }
    }
}
