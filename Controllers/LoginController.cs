using MCPhase3.CodeRepository;
using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace MCPhase3.Controllers
{    
    public class LoginController : Controller
    {
        public const string SessionKeyUserID = "_UserName";
        //Paylocation and Employer both are same.
        public const string SessionKeyPayLocName = "_PayLocName";
        public const string SessionKeyPayLocId = "_Id";
        public const string SessionKeyPassword = "_Password";   
        public const string SessionKeyClientId = "_clientId";
        public const string SessionKeyClientType = "_clientType";
        public const string SessionKeyEmployerName = "_employerName";
        public const string SessionKeyPayrollProvider = "_payrollProvider";

       
        public string SessionKeyMonth = "_month";
        public string SessionKeyFileName = "_fileName";
        public string SessionKeyTotalRecords = "_totalRecords";
        public string SessionKeyRemittanceID = "_remittanceID";
        

        //My list item to add all  the errors from the spreadsheet
        List<string> AllSpreadsheetErrors = new List<string>();       
        PayrollProvidersBO payrollBO = new PayrollProvidersBO();       
        DummyLoginViewModel loginDetails = new DummyLoginViewModel();
        string uploadedFileName = string.Empty;     
        private readonly IConfiguration _configuration;
        

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;            
        }
       
        public IActionResult Index()
        {
            bool sessionResult = false;
            ///following function will check if session has value then login user with out showing login page again.
            sessionResult = SessionHasValue();
            if (sessionResult)
            {
                return RedirectToAction("Home", "Admin");
            }

            //encode decode test

            //int remID = 20;
            //string enRemid = protector.Decode(remID.ToString());
            //ViewData["remid"] = enRemid;
            //ViewData["enRemid"] = protector.Encode(enRemid);


            // Page_Load();
            return View(loginDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
       
        public async Task<IActionResult> Index(DummyLoginViewModel loginDetails)
        {
            LoginBO loginBO = new LoginBO();
            CodeRepository.UPM2LoginSR login = new CodeRepository.UPM2LoginSR();
            int result = 0;

            loginBO.userName = loginDetails.userName;
            loginBO.password = loginDetails.password;
           

            //this model class has variables defined that I am using to validate.
            MyModel isFileFire = new MyModel();
            //get valid client id's from config file.
             var fireSchemeId = _configuration.GetValue<string>("ValidSchemesId").Split(",".ToCharArray(),StringSplitOptions.RemoveEmptyEntries);

            //call new payroll provider REST API
            try
            {
                //Once usernames copied to new table then uncomment following line of code
                 payrollBO = await CallPayrollProviderService(loginDetails.userName);
                
                
                //check username and password
                result = await LoginCheckMethod(loginBO);

                ///following function will check if session has value then login user 
                /// it will mainly to empty session if user clicked on logout button.
                //sessionResult = SessionHasValue();
                //if (sessionResult)
                //{
                //    return RedirectToAction("Home", "Admin");
                //}
            }
            catch (Exception ex)
            {
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }

            // loginDetails.clientId = payrollBO.pay_location_ID.ToString();
            loginDetails.clientId = payrollBO.client_Id;
            

            //check user login details
            // if(login.Login(loginDetails.userName, loginDetails.password))
            // if (loginDetails.userName.ToUpper() == "BROWNA" && loginDetails.password == "1234567")
            if (result == 1)
            {
               // var result1 = await signInManager.PasswordSignInAsync(loginDetails.userName, loginDetails.password, false, false);
                //loginDetailsService = loginDetails.userName;//services.GetLoginDetails(loginDetails.userName);
                HttpContext.Session.SetString(SessionKeyClientId, loginDetails.clientId);
                HttpContext.Session.SetString(SessionKeyUserID, loginDetails.userName);
                HttpContext.Session.SetString(SessionKeyPayLocName, payrollBO.pay_location_name);
                HttpContext.Session.SetString(SessionKeyPayLocId, payrollBO.pay_location_ID.ToString());
                HttpContext.Session.SetString(SessionKeyEmployerName, payrollBO.pay_location_name);
                //following is a payrollprovider
                HttpContext.Session.SetString(SessionKeyPayrollProvider, payrollBO.paylocation_ref);

                TempData["ps"] = loginDetails.userName;

                if (fireSchemeId.Contains(loginDetails.clientId.Trim()))
                {
                    TempData["MainHeading"] = "Fire - Contribution Advice";
                    TempData["isFire"] = true;
                    isFileFire.isFire = true;
                    HttpContext.Session.SetString(SessionKeyClientType, "FIRE");
                    return RedirectToAction("Home", "Admin");
                }
                else
                {
                    TempData["isFire"] = false;
                    isFileFire.isFire = false;
                    HttpContext.Session.SetString(SessionKeyClientType, "LG");
                    return RedirectToAction("Home", "Admin");
                }
            }
            else if (result == 2)
            {
                TempData["Msg1"] = "Your account is temporarily locked to prevent unauthorized use. Try again later in 30 minutes, and if you still have trouble, contact WYPF.";
                return RedirectToAction("Index", "Login");
            }
            else
            {
                TempData["Msg1"] = "Username or password not correct, please try again";
                return RedirectToAction("Index", "Login");
            }          
        }
          
            /// <summary>
            /// following method will show main payroll provider with login name and id
            /// </summary>
            /// <param name="userName"></param>
            /// <returns></returns>
        private async Task<PayrollProvidersBO> CallPayrollProviderService(string userName)
        {
            string apiBaseUrlForPayrollProvider = _configuration.GetValue<string>("WebapiBaseUrlForPayrollProvider");
            string apiResponse = String.Empty;
           
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(apiBaseUrlForPayrollProvider + userName))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        payrollBO = JsonConvert.DeserializeObject<PayrollProvidersBO>(result);
                    }
                }
            }

            return payrollBO;
        }
        /// <summary>
        /// Check if username and password correct
        /// </summary>
        /// <param name="loginBO"></param>
        /// <returns></returns>

        private async Task<int> LoginCheckMethod(LoginBO loginBO)
        {
            string apiBaseUrlForLoginCheck = _configuration.GetValue<string>("WebapiBaseUrlForLoginCheck");
            string apiResponse = string.Empty;
            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(loginBO), Encoding.UTF8, "application/json");
               // string endPoint = apiLink;

                using (var response = await httpClient.PostAsync(apiBaseUrlForLoginCheck, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        loginBO.result = JsonConvert.DeserializeObject<int>(result);
                    }
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
           
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeyUserID)))
            {  
                result = true;               
            }
            return result;
        }

        public async Task<IActionResult> Logout()
        {
            TempData["ps"] = null;          
            TempData["Msg"] = null;
            TempData["Msg1"] = null;
            TempData["MsgM"] = null;
            HttpContext.Session.Clear();
            
            HttpContext.Session.Remove(SessionKeyUserID);
            HttpContext.Session.Remove(SessionKeyPayLocName);
            HttpContext.Session.Remove(SessionKeyPayLocId);
            HttpContext.Session.Remove(SessionKeyClientId);
            HttpContext.Session.Remove(SessionKeyClientType);
            HttpContext.Session.Remove(SessionKeyEmployerName);
            HttpContext.Session.Remove(SessionKeyPayrollProvider);

            

            
            var opts = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.Now.AddHours(12),
                SameSite = SameSiteMode.Lax,
                Secure = true
            };

            HttpContext.Response.Cookies.Delete(SessionKeyUserID);
            HttpContext.Response.Cookies.Delete(".AspNetCore.Session");
            HttpContext.Response.Cookies.Append(".AspNetCore.Session", "test");

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }


            // await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Login");           
        }       
    }
}
