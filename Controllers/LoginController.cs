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
            return View(new LoginViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel loginVM)
        {
            //check username and password
            var loginResult = await LoginCheckMethod(loginVM.UserId, loginVM.Password);

            //## Yes, the user is valid, but we haven't Logged the user in yet. need to see if there is another session running anywhere
            if (loginResult == (int)LoginStatus.Valid)
            {                
                //## Get the User Details..
                var currentUser = await base.GetUserDetails(loginVM.UserId);
                await AddPayrollProviderInfo(currentUser);

                //## store the current UserId BrowserId and WindowsId in the session to be used later..
                ContextSetValue(Constants.LoggedInAsKeyName, loginVM.UserId);
                ContextSetValue(Constants.BrowserId, loginVM.BrowserId);
                ContextSetValue(Constants.WindowsId, loginVM.WindowsId);

                //## Check in the Config- whether we should do MFA verification  or not.. we can sometimes disable it- for various reasons..
                if (await is_MfaEnabled())
                {
                    //### check whether this user needs a Multi-Factor Vreification today again... if yes, then send email with MFA Code and then ask to verify it
                    string mfa_Requirement_Check_Url = GetApiUrl(_configuration["ApiEndpoints:MFA_IsRequiredForUser"]);

                    var apiResult = await ApiGet(mfa_Requirement_Check_Url + loginVM.UserId);

                    Boolean.TryParse(apiResult, out bool isMFA_Required);

                    if (isMFA_Required)
                    {
                        string mfa_SendVerificationCodeUrl = GetApiUrl(_configuration["ApiEndpoints:MFA_SendToEmployer"]);
                        var mailData = new MailDataVM() {
                            UserId = loginVM.UserId,
                            EmailTo = currentUser.Email,
                            FullName = currentUser.FullName,
                            /* EmailBody, Subject- will be generated in the API,*/
                        };
                        apiResult = await ApiPost(mfa_SendVerificationCodeUrl, mailData);
                        
                        if (string.IsNullOrEmpty(apiResult))
                        {                            
                            TempData["ErrorMessage"] = "Server error: Failed to send verification code. Please try again.";                            
                        }

                        return RedirectToAction("VerifyToken");
                    }
                }

                //## All good.. no MFA required.. so just continue .. check whether multiple session exist ...
                var sessionInfo = GetUserSessionInfo();

                //## we need to confirm their is only one login session for this user.. 
                if (sessionInfo.HasExistingSession)
                {
                    //## Notify the user - whether they wanna kill the existing one and continue here...?
                    sessionInfo.Password = ""; //## don't take the password back to the UI
                    return View("MultipleSessionPrompt", sessionInfo);

                }
                //## if no 'HasExistingSession' - then proceed to login and take the user to Admin/Home page
                return await ProceedToLogIn(loginVM.UserId);                

            }
            else if (loginResult == (int)LoginStatus.Locked)
            {
                TempData["Msg1"] = AccountLockedMessage;
                loginVM.LoginErrorMessage = AccountLockedMessage;
                return View(loginVM);
            }
            else if (loginResult == (int)LoginStatus.Failed)
            {
                TempData["Msg1"] = AccountFailedLoginMessage;
                loginVM.LoginErrorMessage = AccountFailedLoginMessage;
                return View(loginVM);
            }

            //## if none of the above were true- which is not possible- then take back to login screen again.. where else!?
            return View(loginVM);

        }

        private async Task<bool> is_MfaEnabled()
        {
            string mfa_Requirement_Check_Url = GetApiUrl(_configuration["ApiEndpoints:Is_MfaEnabled"]);
            var apiResult = await ApiGet(mfa_Requirement_Check_Url);
            bool mfaEnabled = bool.Parse(apiResult);
            return mfaEnabled;            
        }

        private async Task AddPayrollProviderInfo(UserDetailsVM currentUser)
        {
            payrollBO = await GetPayrollProviderInfo(currentUser.UserId);

            currentUser.Pay_Location_Ref = payrollBO.paylocation_ref;
            currentUser.Pay_Location_ID = payrollBO.pay_location_ID;
            currentUser.Pay_Location_Name = payrollBO.pay_location_name;
            currentUser.Client_Id = payrollBO.client_Id;

            //## now set this newly built object in the cache- so we can re-use it faster..
            string cacheKey = $"{currentUser.UserId.ToUpper()}_{Constants.AppUserDetails}";
            _cache.Set(cacheKey, currentUser);
        }


        [HttpGet]
        public async Task<IActionResult> VerifyToken()
        {
            string userId = CurrentUserId();
            var currentUser = await base.GetUserDetails(userId);
            var userEmail = currentUser.Email;

            var tokenDetails = new TokenDataVerifyVM()
            {
                UserId = userId,
                Email = MaskedEmail(userEmail),
                VerificationMessage = TempData["ErrorMessage"]?.ToString(),
            };

            return View(tokenDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyToken(TokenDataVerifyVM tokenDetails)
        {
            if (!ModelState.IsValid) {
                TempData["ErrorMessage"] = "You must enter a valid Token";                
                return RedirectToAction("VerifyToken");
            }

            //## Check the VerificationToken is Valid
            var tokenVerificationUrl = GetApiUrl(_configuration["ApiEndpoints:MFA_Verify"]);
            var apiResult = await ApiPost(tokenVerificationUrl, tokenDetails);
            var verification = JsonConvert.DeserializeObject<TaskResults>(apiResult);

            if (!verification.IsSuccess) {
                TempData["ErrorMessage"] = "This verification code is invalid or expired.";
                return RedirectToAction("VerifyToken");
            }

            //## All good.. Token is valid.. now continue .. check whether multiple session exist ...
            var sessionInfo = GetUserSessionInfo();

            //## we need to confirm their is only one login session for this user.. 
            if (sessionInfo.HasExistingSession)
            {
                //## Notify the user - whether they wanna kill the existing one and continue here...?
                sessionInfo.Password = ""; //## don't take the password back to the UI
                return View("MultipleSessionPrompt", sessionInfo);

            }
            //## if no 'HasExistingSession' - then proceed to login and take the user to Admin/Home page
            return await ProceedToLogIn(tokenDetails.UserId);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedCurrentSession(LoginViewModel vm)
        {
            //## The user wasn't logged in, rather shown a warning message to take action- coz they have another session opened somewhere... 
            //## Now the user has decided to stay/continue this current login.. and kills the other session...
            var currentBrowserSessionId = Guid.NewGuid().ToString();
            var sessionInfo = GetUserSessionInfo();

            _cache.Delete(SessionInfoKeyName());

            //## Create a new one
            sessionInfo.HasExistingSession = false;
            sessionInfo.LastLoggedIn = DateTime.Now.ToString();
            sessionInfo.SessionId = currentBrowserSessionId;
            _cache.Set(SessionInfoKeyName(), sessionInfo);

            //## create entries in Session Cookies, too..
            ContextSetValue(Constants.SessionGuidKeyName, currentBrowserSessionId); //## will use this on page navigation- to see whether user has started another session and requested to kill this session

            //## The user was authenticated first, then shown a screen - either to continue on this browser or close this browser.            
            return await ProceedToLogIn(vm.UserId);
        }


        private async Task<IActionResult> ProceedToLogIn(string userId)
        {
            var currentUser = await base.GetUserDetails(userId);

            var fireSchemeId = _configuration.GetValue<string>("ValidSchemesId")
                                               .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);



            //check user login details            
            HttpContext.Session.SetString(Constants.SessionKeyClientId, currentUser.Client_Id);
            HttpContext.Session.SetString(Constants.SessionKeyUserID, userId);      //## used in the 'UserSessionCheckActionFilter' for Authentication  
            //HttpContext.Session.SetString(Constants.LoggedInAsKeyName, vm.UserId);
            HttpContext.Session.SetString(Constants.SessionKeyPayLocName, currentUser.Pay_Location_Name);
            //HttpContext.Session.SetString(Constants.SessionKeyPayLocId, payrollBO.pay_location_ID.ToString());
            HttpContext.Session.SetString(Constants.SessionKeyEmployerName, currentUser.Pay_Location_Name);
            //following is a payrollprovider
            //HttpContext.Session.SetString(Constants.SessionKeyPayrollProvider, payrollBO.paylocation_ref);            

            if (fireSchemeId.Contains(currentUser.Client_Id.Trim()))
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
        private UserSessionInfoVM GetUserSessionInfo()
        {
            //## Get the session info from Redis cache            
            var sessionInfo = _cache.Get<UserSessionInfoVM>(SessionInfoKeyName());   //## this KeyName should be used in Logout- to Delete the Redis entry

            if (sessionInfo is null)
            {
                var currentBrowserSessionId = Guid.NewGuid().ToString();
                ContextSetValue(Constants.SessionGuidKeyName, currentBrowserSessionId); //## will use this on page navigation- to see whether user has started another session and requested to kill this session            
                
                //## No user session info in the Cache..  so create an entry to stop any further login attempt from another browser                
                sessionInfo = new UserSessionInfoVM()
                {
                    UserId = CurrentUserId(),
                    BrowserId = ContextGetValue(Constants.BrowserId),
                    WindowsId = ContextGetValue(Constants.WindowsId),
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
        private async Task<PayrollProvidersBO> GetPayrollProviderInfo(string userName)
        {
            string apiBaseUrlForPayrollProvider = GetApiUrl(_apiEndpoints.PayrollProvider);     //## api/GetPayrollProviders
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

            if (!string.IsNullOrEmpty(CurrentUserId()))
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

            var loginVM = new LoginViewModel() { 
                LoginErrorMessage = SessionExpiredMessage
            };

            return View("Index", loginVM);

        }

        private void ClearTempData()
        {            
            TempData["Msg"] = null;
            TempData["Msg1"] = null;
            TempData["MsgM"] = null;
        }

        /// <summary>This will delete all Context.Session for the current browser</summary>
        private void ClearSessionValues()
        {
            HttpContext.Session.Clear();

            //HttpContext.Session.Remove(SessionKeyUserID);
            HttpContext.Session.Remove(SessionKeyPayLocName);
            //HttpContext.Session.Remove(SessionKeyPayLocId);
            HttpContext.Session.Remove(SessionKeyClientId);
            HttpContext.Session.Remove(SessionKeyClientType);
            HttpContext.Session.Remove(SessionKeyEmployerName);
            //HttpContext.Session.Remove(SessionKeyPayrollProvider);

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

        private string MaskedEmail(string userEmail)
        {
            var emailParts = userEmail.Split("@");
            string userId = emailParts[0];
            var maskedUserId = userId[..4] + "****";
            var maskedEmail = maskedUserId + "@" + emailParts[1];
            return maskedEmail;
        }
    }
}
