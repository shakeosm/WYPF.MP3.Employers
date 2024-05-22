using MCPhase3.CodeRepository;
using MCPhase3.Common;
using MCPhase3.Models;
using MCPhase3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
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

            string loginErrorMessage = TempData["Msg1"]?.ToString();
            ClearTempData();
            return View(new LoginViewModel() { LoginErrorMessage  = loginErrorMessage });
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel loginVM)
        {
            //## lets store the UserId  in the Browser session, as we will be using this at very early stage. if Login isn't successful- they will be overridden later..
            ContextSetValue(Constants.LoginNameKey, loginVM.UserId);    //## this 'loginVM.UserId' this is actually 'Upm2.LoginName'. eg: we have userId: 'BlackburnD' and LoginName: 'BlackburnD1'

            //check username and password
            var loginResult = await LoginCheckMethod(loginVM.UserId, loginVM.Password);

            //## Yes, the user is valid, but we haven't Logged the user in yet. need to see if there is another session running anywhere
            if (loginResult == (int)LoginStatus.Valid)
            {
                //## Get the User Details..
                var currentUser = await base.GetUserDetails(loginVM.UserId);    //## we actually passing the LoginName (UPM.LoginName) to get the User Details.. 
                await AddPayrollProviderInfo(currentUser);

                //## now change the 'UserId' VALUE IN THE sESSION.. we have a tricky situation- where UserID not always same as LoginName.
                //## so- LoginName is only for Login process.. once the user is logged in- use UserId everywhere- in all Procedure calls..
                ContextSetValue(Constants.UserIdKey, currentUser.UserId);

                //## store the current UserId BrowserId and WindowsId in the session to be used later..                
                ContextSetValue(Constants.LoggedInUserEmailKeyName, currentUser.Email);
                ContextSetValue(Constants.BrowserId, loginVM.BrowserId);
                ContextSetValue(Constants.WindowsId, loginVM.WindowsId);

                //## Super User Scenario
                await LoggedInAs_SuperUser_SetSessionValue(loginVM.UserId);

                //## Check in the Config- whether we should do MFA verification  or not.. we can sometimes disable it- for various reasons..
                if (await Is_MfaEnabled())
                {
                    //### check whether this user needs a Multi-Factor Vreification today again... if yes, then send email with MFA Code and then ask to verify it
                    string mfa_Requirement_Check_Url = GetApiUrl(_configuration["ApiEndpoints:MFA_IsRequiredForUser"]);

                    var apiResult = await ApiGet(mfa_Requirement_Check_Url + loginVM.UserId);

                    Boolean.TryParse(apiResult, out bool isMFA_Required);

                    if (isMFA_Required)
                    {                        
                        var mailData = new MailDataVM()
                        {
                            UserId = loginVM.UserId,
                            EmailTo = currentUser.Email,
                            FullName = currentUser.FullName,
                            /* EmailBody, Subject- will be generated in the API,*/
                        };

                        //## store this MailData object in Redis..  we will need this to Resend MFA code to user.. again and again..
                        _cache.Set(GetKeyName(Constants.MFA_MailData), mailData);                        
                        apiResult = await SendMFA_VerificationCode();

                        if (IsEmpty(apiResult))
                        {
                            LogInfo("API error: Failed to send verification code. apiResult  =  NULL");

                            loginVM.LoginErrorMessage = "Server error: Failed to send verification code. Please try again.";
                            return View(loginVM);
                        }

                        //## its all good.. MFA code has been sent out.. now prompt the user to enter the verification code within next 2 mins..
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
                //TempData["Msg1"] = AccountLockedMessage;
                loginVM.LoginErrorMessage = AccountLockedMessage;
                return View(loginVM);
            }
            else if (loginResult == (int)LoginStatus.Failed)
            {
                loginVM.LoginErrorMessage = AccountFailedLoginMessage;
                return View(loginVM);
            }
            else if (loginResult == (int)LoginStatus.InactiveInUpm)
            {
                loginVM.LoginErrorMessage = AccountInactiveInUpmMessage;
                return View(loginVM);
            }

            //## if none of the above were true- which is not possible- then take back to login screen again.. where else!?
            return View(loginVM);

        }


        /// <summary>This will go to the API and see if the current user is listed as a SuperUser</summary>
        /// <param name="userId">UserId- the one they have used for Login</param>
        /// <returns>It sets value in the Session, does not return any value</returns>
        private async Task LoggedInAs_SuperUser_SetSessionValue(string userId)
        {
            string apiUrl = GetApiUrl(_apiEndpoints.SuperUserCheck);
            var result = await ApiGet(apiUrl + userId);
            if (IsEmpty(result))
            {
                ContextSetValue(Constants.LoggedInAs_SuperUser, "false");   //## if any error happenned- then just say 'NO, Not a SuperUser'
            }
            else {
                ContextSetValue(Constants.LoggedInAs_SuperUser, result.ToString());
            }
            
        }

        /// <summary>This will send MFA Verification code to the User.. Send or Resend...</summary>
        /// <returns></returns>
        private async Task<string> SendMFA_VerificationCode()
        {
            string mfa_SendVerificationCodeUrl = GetApiUrl(_configuration["ApiEndpoints:MFA_SendToEmployer"]);
            var mailData = _cache.Get<MailDataVM>(GetKeyName(Constants.MFA_MailData));  //## we did set this value after Log in - Success.. it must be there..
            if (mailData is null) {
                LogInfo("SendMFA_VerificationCode() => _cache.mailData is null");
                return null;
            }
            string apiResult = await ApiPost(mfa_SendVerificationCodeUrl, mailData);
            ContextSetValue(Constants.MFA_TokenExpiryTime, DateTime.Now.AddMinutes(2).ToLongTimeString());

            return apiResult;
        }

        public async Task<ActionResult> VerifyTokenResend()
        {
            string apiError = "";
            var result = await SendMFA_VerificationCode();
            if (IsEmpty(result))
            {
                LogInfo("API error: Failed to send verification code. apiResult  =  NULL");

                apiError = "Server error: Failed to send verification code. Please try again.";
            }

            string expiryTime = ContextGetValue(Constants.MFA_TokenExpiryTime);

            var mailData = _cache.Get<MailDataVM>(GetKeyName(Constants.MFA_MailData));  //## we did set this value after Log in - Success.. it must be there..
            var tokenDetails = new TokenDataVerifyVM()
            {
                UserId = mailData.UserId,
                Email = MaskedEmail(mailData.EmailTo),
                ExpiryTime = expiryTime,
                VerificationMessage = apiError,
            };

            return View("VerifyToken", tokenDetails);
        }



        private async Task<bool> Is_MfaEnabled()
        {
            string mfa_Requirement_Check_Url = GetApiUrl(_configuration["ApiEndpoints:Is_MfaEnabled"]);
            var apiResult = await ApiGet(mfa_Requirement_Check_Url);
            bool mfaEnabled = bool.Parse(apiResult);
            LogInfo($"is_MfaEnabled() >> calling: {mfa_Requirement_Check_Url}, mfaEnabled: {mfaEnabled}");
            return mfaEnabled;
        }

        /// <summary>This will get Payroll Provider info for this user</summary>
        /// <param name="currentUser">Current user object- w2userId will be user here</param>
        /// <returns></returns>
        private async Task AddPayrollProviderInfo(UserDetailsVM currentUser)
        {
            payrollBO = await GetPayrollProviderInfo(currentUser.UserId);

            currentUser.Pay_Location_Ref = payrollBO.paylocation_ref;
            currentUser.Pay_Location_ID = payrollBO.pay_location_ID;
            currentUser.Pay_Location_Name = payrollBO.pay_location_name;
            currentUser.Client_Id = payrollBO.client_Id;

            //## now set this newly built object in the cache- so we can re-use it faster..
            string cacheKey = $"{currentUser.LoginName}_{Constants.AppUserDetails}";            
            _cache.Set(cacheKey, currentUser);
        }


        [HttpGet]
        public IActionResult VerifyToken()
        {
            string userId = base.CurrentUserId();            
            string emailId = ContextGetValue(Constants.LoggedInUserEmailKeyName);

            if (IsEmpty(emailId)) {
                throw new Exception("EmailId not found in the Redis Session");
            }
            var tokenDetails = new TokenDataVerifyVM()
            {
                UserId = userId,
                Email = MaskedEmail(emailId),
                VerificationMessage = TempData["ErrorMessage"]?.ToString(),
                ExpiryTime = ContextGetValue(Constants.MFA_TokenExpiryTime)
            };

            TempData["ErrorMessage"] = "";

            return View(tokenDetails);

        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyToken(TokenDataVerifyVM tokenDetails)
        {
            LogInfo($"VerifyToken() [HttpPost] => {tokenDetails.UserId}: {tokenDetails.SessionToken}");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Select(x => x.Value.Errors)
                           .Where(y => y.Count > 0)
                           .ToList();

                LogInfo("VerifyToken() => Invalid Model state. Sending back to VerifyToken(), " + string.Concat(errors));
                TempData["ErrorMessage"] = "You must enter a valid Token";
                return RedirectToAction("VerifyToken");
            }

            //## Check the VerificationToken is Valid
            var tokenVerificationUrl = GetApiUrl(_configuration["ApiEndpoints:MFA_Verify"]);
            var apiResult = await ApiPost(tokenVerificationUrl, tokenDetails);
            var verification = JsonConvert.DeserializeObject<TaskResults>(apiResult);

            if (IsEmpty(apiResult)) {
                LogInfo($"api: {tokenVerificationUrl}, returned NULL");
                var loginVM = new LoginViewModel() { LoginErrorMessage = "Server Error: Token verificaton failed. Please contact support." };
                return RedirectToAction("Index", loginVM);
            }

            if (!verification.IsSuccess)
            {
                TempData["ErrorMessage"] = "This verification code is invalid or expired.";
                LogInfo("verification.IsSuccess= FALSE");
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


        private async Task<IActionResult> ProceedToLogIn(string loginName)
        {
            var currentUser = await base.GetUserDetails(loginName);
            if (string.IsNullOrEmpty(currentUser.Client_Id))
            {
                await AddPayrollProviderInfo(currentUser);
            }

            LogInfo($"ProceedToLogIn() => {loginName} -> {currentUser.Email}");

            var fireSchemeId = _configuration.GetValue<string>("ValidSchemesId")
                                               .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);



            //check user login details            
            HttpContext.Session.SetString(Constants.SessionKeyClientId, currentUser.Client_Id);
            HttpContext.Session.SetString(Constants.LoginNameKey, loginName);      //## used in the 'UserSessionCheckActionFilter' for Authentication  
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

            LogInfo("######################################################################");
            LogInfo("######################### Employers Portal ###########################");
            LogInfo("######################################################################");
            LogInfo("ProceedToLogIn()");

            return RedirectToAction("Index", "Admin");

        }

        /// <summary>
        /// This will be used to Register new users to MP3 portal... they will be given a chance to change the password- 
        /// which will look like a new User registration..
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string id1, string id2)
        {
            if (IsEmpty(id1) || IsEmpty(id2)) {
                return View("Register", new UserRegistrationVM());
            }

            string verifyUserRegistrationCodeApi = GetApiUrl(_apiEndpoints.VerifyUserRegistrationCode); //##: api/VerifyUserRegistrationCode
            var userToken = new UserRegistrationTokenVM() { 
                SessionToken = id2,
                UserId = id1
            };

            var apiResult = await ApiPost(verifyUserRegistrationCodeApi, userToken);
            var isVerified = JsonConvert.DeserializeObject<bool>(apiResult);

            ContextSetValue(Constants.LoginNameKey, id1);

            string cacheKey = $"{id1}{Constants.UserRegistrationTokenDetails}";
            _cache.Set(cacheKey, userToken);

            if (isVerified)
            { //## the user exist and Token is valid.. now pull the User details and allow the user to change their password

                var currentUser = await GetUserDetails(id1);
                await AddPayrollProviderInfo(currentUser);
                var registerUser = new UserRegistrationVM()
                {
                    UserId = id1,
                    UserDetails = currentUser,
                };
                ContextSetValue(Constants.NewUserRegistrationVerification, "true");

                return View("Register", registerUser);
            }
            else {
                return View("Register", new UserRegistrationVM());
            }
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterUserWithNewPassword(RegisterUserWithNewPasswordVM vm)
        {
            var result = new TaskResults();

            if (!ModelState.IsValid) {

                result.IsSuccess = false;
                result.Message = "Error: Please enter a valid password and confirm it.";

                return Json(result);
            }

            string cacheKey = $"{vm.UserId}{Constants.UserRegistrationTokenDetails}";
            var userToken = _cache.Get<UserRegistrationTokenVM>(cacheKey);
            vm.SessionToken = userToken.SessionToken;

            string regiserUserApiUrl = GetApiUrl(_apiEndpoints.RegisterUserWithNewPassword);
            var apiResult = await ApiPost(regiserUserApiUrl, vm);
            var isRegistered = JsonConvert.DeserializeObject<bool>(apiResult);

            if (isRegistered)
            {
                DeleteAllUserSessions();    //## to make sure no orphaned session values will screw up this new User session..

                result.IsSuccess = true;
                result.Message = $"Congratulations <span class='text-danger'>{vm.UserId}</span>!<p>Your account is successfully registered.</p><p>Please go to <span class='text-danger'>Log in</span> page and enter your credentials.</p>";
            }
            else {
                result.Message = "Error while trying to register your account. We sincerely apoligize for this inconvenience. Please speak to you Finance Business Partner.";
            }

            return Json(result);
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
        /// <param name="userName">w2user Id, NOT InternetLogin Id</param>
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


        private async Task<int> LoginCheckMethod(string userId, string password)
        {
            var loginParams = new LoginPostVM()
            {
                UserName = userId,
                Password = password,                
            };

            string apiBaseUrlForLoginCheck = GetApiUrl(_apiEndpoints.LoginCheck);   ///## api/CheckLogin
            var apiResult = await ApiPost(apiBaseUrlForLoginCheck, loginParams);
            if (IsEmpty(apiResult)) {
                return (int)LoginStatus.Failed;
            }

            loginParams.Result = JsonConvert.DeserializeObject<int>(apiResult);

            return loginParams.Result;
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

            DeleteAllUserSessions();
            // await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }


        private void DeleteAllUserSessions()
        {
            //## clear the Redis cache... so the user can login next time easily            
            //## but delete if this is your Redis session.. 
            //## Scenario: user logged in from Browser 2 and wanna kick out Browser1 session.
            //##  When logged in using Browser2- they already have cleared session for Browser_1. So- don't just delete a Redis session if that session doesn't belong to current Browser session Id            

            var currentBrowserSessionId = ContextGetValue(SessionGuidKeyName);
            var sessionInfo = _cache.Get<UserSessionInfoVM>(SessionInfoKeyName());

            _cache.DeleteUserSession(CurrentUserId());    //## Deletes the User session in Redis Cache..

            //## Browser Session Id and Redis SessionId-> are they same..?
            if (sessionInfo != null)
            {
                if (currentBrowserSessionId == sessionInfo.SessionId)
                {
                    _cache.Delete(SessionInfoKeyName());
                    _cache.Delete(currentBrowserSessionId);
                }
            }

            ClearTempData();            //## Clear all the 'TempData["XXX"]' values
            ClearSessionValues();       //## Clear all the Http.Session values

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

        }


        /// <summary>This will show the Login page again with proper message for an expired session
        /// also, delete any session in Browser.</summary>
        /// <returns>Login Page with 'session expired' message</returns>
        public IActionResult SessionExpired()
        {
            ClearRedisUserSession(CurrentUserId());
            ClearTempData();
            ClearSessionValues();

            var loginVM = new LoginViewModel()
            {
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
            HttpContext.Session.Remove(LoggedInUserEmailKeyName);
            HttpContext.Session.Remove(LoginNameKey);
            HttpContext.Session.Remove(UserIdKey);
            HttpContext.Session.Remove(SessionKeyPayLocName);
            //HttpContext.Session.Remove(SessionKeyPayLocId);
            HttpContext.Session.Remove(SessionKeyClientId);
            HttpContext.Session.Remove(SessionKeyClientType);
            HttpContext.Session.Remove(SessionKeyEmployerName);
            HttpContext.Session.Remove(LoggedInAs_SuperUser);
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

        [HttpGet]
        [Obsolete("Use the one in the AdminStaffTools action controller")]
        public IActionResult ClearOlderCustomerFilesNotProcessed(string id)
        {
            string uploadFolder = _configuration["FileUploadPath"];

            if (System.IO.Path.Exists(uploadFolder)) {
                string[] files = Directory.GetFiles(uploadFolder);
                StringBuilder cleanupResult = new StringBuilder();
                cleanupResult.AppendLine("############### New hourly execution #############");
                cleanupResult.AppendLine($"############# Time: {DateTime.Now} ##########");

                if(files.Any() == false)
                {
                    cleanupResult.AppendLine("No files found...");
                    cleanupResult.AppendLine();
                    Log_ClearOlderCustomerFilesNotProcessed(cleanupResult.ToString());
                    return Json($"success, called at: {DateTime.Now}");
                }

                int oldFilesFound = 0; int newFilesFound = 0;
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    var created = fi.CreationTime;
                    string hoursElapsed = _configuration["ClearCustomerFilesOlderThan_X_Hours"].ToString();
                    int.TryParse(hoursElapsed, out int hoursThresholdToDeleteFile);

                    //cleanupResult.AppendLine($"{DateTime.Now} >> File: {file}, created: {created}");
                    if (DateTime.Now > created.AddHours(hoursThresholdToDeleteFile))
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                            cleanupResult.AppendLine($"deleting >> {file}, created: {created},");
                        }
                        catch (Exception ex)
                        {
                            cleanupResult.AppendLine($"Error: Failed to Delete >> {file}, created: {created}, Reason: {ex.ToString()}");
                        }
                        
                        oldFilesFound++;
                    }
                    else {
                        cleanupResult.AppendLine($"New File>> {file}, created: {created},");
                        newFilesFound++;
                    }
                }
                cleanupResult.AppendLine($"Total: {files.Length} files found. New file: {newFilesFound}, Old: {oldFilesFound}.");
                cleanupResult.AppendLine();
                Log_ClearOlderCustomerFilesNotProcessed(cleanupResult.ToString());

                return Json($"success, called at: {DateTime.Now}");
            }
            return Json("who are you?");
        }

        private string MaskedEmail(string userEmail)
        {
            LogInfo($"MaskedEmail() => userEmail: {userEmail}");

            var emailParts = userEmail.Split("@");
            string userId = emailParts[0];
            var maskedUserId = userId[..4] + "****";
            var maskedEmail = maskedUserId + "@" + emailParts[1];

            LogInfo($"maskedEmail: {maskedEmail}");

            return maskedEmail;
        }

        /// <summary>This will be called via ajax to check a password strength so user will know about its validity</summary>
        /// <returns>A number indicating strength, between 0-100. A score of 70 is a pass mark</returns>        
        [HttpGet, Route("/Login/CheckPasswordStrength/{passwordToCheck}"), AllowAnonymous]
        public async Task<IActionResult> CheckPasswordStrength(string passwordToCheck)
        {
            string passwordMeterResult = "Weak;0;70";    //## Format: $"{scoreText};{currentScore};{scoreToPass}";
            if (!PaswordPolicyMatched(passwordToCheck))
            {
                return PartialView("/Views/Profile/_PasswordMeter.cshtml", passwordMeterResult);
            }

            passwordMeterResult = await GetPasswordMeterValue(passwordToCheck);
            Console.WriteLine($"CheckPasswordStrength() -> apiResult: {passwordMeterResult}");

            return PartialView("/Views/Profile/_PasswordMeter.cshtml", passwordMeterResult);
        }
    }
}
