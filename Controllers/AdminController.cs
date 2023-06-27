using MCPhase3.CodeRepository;
using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace MCPhase3.Controllers
{
    public class AdminController : BaseController
    {
        private readonly IConfiguration _configuration;
        TotalRecordsInsertedAPICall callApi = new TotalRecordsInsertedAPICall();
        
        public AdminController(IConfiguration configuration, IRedisCache cache, IDataProtectionProvider Provider) : base(configuration, cache, Provider)
        {
            _configuration = configuration;        
        }



        public IActionResult SubmitForProcessing()
        {
                string apiBaseUrlForInsertEventDetails = ConfigGetValue("WebapiBaseUrlForInsertEventDetails");
            EventDetailsBO eBO = new EventDetailsBO
                {
                    
                    remittanceID = Convert.ToInt32(GetRemittanceId(returnEncryptedIdOnly: false)), // we can put variable name with variable value when calling a function to make it more readable                    
                    remittanceStatus = 1,
                    eventTypeID = 105,
                    eventDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")),
                    notes = "Data quality score threshold check skipped, File passed to WYPF by Emp for processing."
                };
                //eventUpdate.UpdateEventDetailsTable(eBO);
                //update Event Details table File is uploaded successfully.
                //I have disabled it for staff.
                callApi.InsertEventDetails(eBO, apiBaseUrlForInsertEventDetails);
         
            return RedirectToAction("Index");
        }


        /// <summary>
        /// show list of all the remittance to employers
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> CompletedFiles(int? pageNumber)
        {
            string payrolID = string.Empty;
            DashboardBO detailBO = new DashboardBO();
            List<DashboardBO> dashboardBO = new List<DashboardBO>();
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);
            var userid = ContextGetValue(Constants.SessionKeyUserID);
            payrolID = ContextGetValue(Constants.SessionKeyPayLocId);

            //List<DashboardBO> dashboardBO = new List<DashboardBO>();
            // DashboardBO dashboardBO1 = new DashboardBO();
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);
           // var userid = GetContextValue(SessionKeyUserID);
            detailBO.userId = userid;
            //detailBO.L_PAYROLL_PROVIDER = payrolID;
            detailBO.L_PAYROLL_PROVIDER = null;
            //detailBO.statusType = "COMPLETED";
            detailBO.statusType = "WYPF";

            dashboardBO = await getDashboardValues(detailBO);

            return View(dashboardBO);
        }

        /// <summary>
        /// show list of score history to employers
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ScoreHist(string remittanceId)
        {
            int remID = Convert.ToInt32(DecryptUrlValue(remittanceId));
            List<DashboardHistScoreBO> dashboardScoreHistBO = await getDashboardScoreHist(remID);
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);
            
            return View(dashboardScoreHistBO);
        }

        /// <summary>
        /// show list of score history to employers- as a Partial view to be used in Ajax call..
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetScoreHistoryPartialView(string remittanceId)
        {
            int remID = Convert.ToInt32(DecryptUrlValue(remittanceId));
            List<DashboardHistScoreBO> dashboardScoreHistBO = await getDashboardScoreHist(remID);
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);
            
            return PartialView("_ScoreHistory", dashboardScoreHistBO);
        }


        /// <summary>
        /// staff is not in use. 
        /// </summary>
        /// <returns></returns>
        public IActionResult Staff()
        {
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);
            return View();
        }
        /// <summary>
        /// Update password for Staff and Employers
        /// </summary>
        /// <returns></returns>
        public IActionResult UpdatePassword()
        {
            LoginBO loginBO = new LoginBO() { userName = CurrentUserId() };
            //loginBO.isStaff = 0;
            return View(loginBO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(LoginBO loginBO)
        {
            if (!ModelState.IsValid) {
                ModelState.AddModelError("Password","Invalid password. Please use a strong password.");
                return View(loginBO);
            }

            string passwordPattern = @"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{9,255}$";
            var passwordStrength = Regex.Match(loginBO.password, passwordPattern);
            if (!passwordStrength.Success)
            {
                ModelState.AddModelError("Password", "Invalid password. Please use a strong password.");
                return View(loginBO);
            }

            List<string> passwordReq  = new List<string>
            {
                "at least nine characters",
                "one or more capital letters",
                "one or more lower case letters",
                "one or more numbers",
                "one or more special characters, for example !,”.#",
            };

            ViewBag.isStaff = 1;
            loginBO.userName = ContextGetValue(Constants.SessionKeyUserID);
            int result = await UpdatePasswordMethod(loginBO);
            if (result == 1)
            {
                TempData["Msg1"] = "Password updated successfully, please login using new password.";
                
                // add this new password to RedisCache.. Set a flag- that password is changed

                //return RedirectToAction("Index", "Login");
                return RedirectToAction("Logout", "Login");
            }
            if (result == 2)
            {
                TempData["UpdateMessage"] = "Password does not meet our complexity requirements: ";
                TempData["PasswordRequ"] = 1;
                return RedirectToAction("UpdatePassword", "Admin");
            }
            else
            {
                TempData["UpdateMessage"] = "Old Password is not correct, please try again.";
                return RedirectToAction("UpdatePassword", "Admin");
            }
        }

        private async Task<int> UpdatePasswordMethod(LoginBO loginBO)
        {
            string apiBaseUrlForLoginCheck = ConfigGetValue("WebapiBaseUrlForPasswordChange");
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
        /// call dashboard api
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        private async Task<List<DashboardBO>> getDashboardValues(DashboardBO dashboardBO)
        {
            string apiBaseUrlForDashboard = ConfigGetValue("WebapiBaseUrlForDashboard");
            var apiResult = new List<DashboardBO>();
            
            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(dashboardBO), Encoding.UTF8, "application/json");
                using (var response = await httpClient.PostAsync(apiBaseUrlForDashboard, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        apiResult = JsonConvert.DeserializeObject<List<DashboardBO>>(result);
                    }
                }
            }

            //if (status.Equals("completed"))
            //{
            //    return listBO.Where(x =>Convert.ToInt32(x.return_Status_Code) >= 105).ToList();
            //}

            //return listBO.Where(x => Convert.ToInt32(x.return_Status_Code) < 110).ToList();           
            if (apiResult != null && apiResult.Count > 0)
            {
                foreach (var item in apiResult)
                {
                    item.remittance_IdEnc = EncryptUrlValue(item.remittance_Id.ToString());
                }
                
                return apiResult.OrderByDescending(d => d.remittance_Id).ToList();
            }
            else { 
                return new List<DashboardBO>();
            }
        }


        public async Task<IActionResult> Home( )
        {
            try
            {
                //check of PaylocID session has value then empty it.
                if (ContextGetValue(Constants.SessionKeyPaylocFileID) != null)
                {
                    HttpContext.Session.Clear();
                }

                var paramList = new DashboardBO
                {
                    userId = ContextGetValue(Constants.SessionKeyUserID),
                    L_PAYROLL_PROVIDER = null,
                    statusType = Constants.StatusType_EMPLOYER
                };

                var viewModel = new DashboardViewModelNew
                {
                    dashboardBO = await getDashboardValues(paramList)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["MsgError"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Admin");
            }
        }

        /// <summary>
        /// This will be used from Admin dashboard- to show the details of a Pending submission file
        /// </summary>
        /// <param name="remittanceId">Remittance Id</param>
        /// <returns>A Partial view with list of all records in that suubmission</returns>
        [HttpGet]
        public async Task<ActionResult> GetSubmissionDetails(string remittanceId)
        {

            if (!string.IsNullOrEmpty(remittanceId) )
            {
                string WebapiBaseUrlForDetailEmpList = ConfigGetValue("WebapiBaseUrlForDetailEmpList");
                string remid = DecryptUrlValue(remittanceId);

                var paramList = new MasterDetailEmpListBO()
                {
                    L_USERID = CurrentUserId(),
                    L_REMITTANCE_ID = remid,
                    L_STATUSTYPE = "ALL"
                };

                var submissionDetails = new List<DashboardBO>();
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(paramList), Encoding.UTF8, "application/json");

                    using var response = await httpClient.PostAsync(WebapiBaseUrlForDetailEmpList, content);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        submissionDetails = JsonConvert.DeserializeObject<List<DashboardBO>>(result);
                    }
                }

                return PartialView("_SubmissionDetails", submissionDetails);
            }

            return Json("<h3>Failed ot load data.</h3>");
        }

        /// <summary>
        /// Get values for Employers dashboard - Not in use, Started using new David's procedure in above action
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        private async Task<List<GetRemittanceStatusByUserBO>> getDashboardValuesForEmployers(string userid, string status)
        {
            string apiBaseUrlForDashboard = ConfigGetValue("WebapiBaseUrlForDashboardEmployers");
            List<GetRemittanceStatusByUserBO> listBO = new List<GetRemittanceStatusByUserBO>();
            string apiResponse = string.Empty;
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(apiBaseUrlForDashboard + userid))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        listBO = JsonConvert.DeserializeObject<List<GetRemittanceStatusByUserBO>>(result);
                    }
                }
            }
            if (status.Equals("completed"))
            {
                return listBO.Where(x => x.event_Type_ID > 100).ToList();
            }
           
             return listBO.Where(x => x.event_Type_ID <= 100).ToList();
            
        }
        /// <summary>
        /// Get values for Employers dashboard
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        private async Task<List<DashboardHistScoreBO>> getDashboardScoreHist(int remittanceId)
        {
            string apiBaseUrlForDashboardScoreHist = ConfigGetValue("WebapiBaseUrlForDashboardScoreHist");
            List<DashboardHistScoreBO> listBO = new List<DashboardHistScoreBO>();
            string apiResponse = string.Empty;
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(apiBaseUrlForDashboardScoreHist + remittanceId))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        listBO = JsonConvert.DeserializeObject<List<DashboardHistScoreBO>>(result);
                    }
                }
            }

            foreach (var item in listBO)
            {
                item.remittanceId_Encrypted = EncryptUrlValue(item.remittance_Id.ToString());
            }

            return listBO;
        }

        /// <summary>
        /// Submit each paylocation to UPM
        /// </summary>
        /// <param name="paylocID"></param>
        /// <returns></returns>
        public async Task<IActionResult> SubmitReturn(ReturnSubmitBO rBO)
        {
            string apiBaseUrlForInsertEventDetails = ConfigGetValue("WebapiBaseUrlForInsertEventDetails");
            EventDetailsBO eBO = new EventDetailsBO();
            int remID = 0;

            if (rBO.p_REMITTANCE_ID == null)
            {
                // remID = Convert.ToInt32(GetContextValue(SessionKeyRemittanceID));
                return RedirectToAction("Logout", "Home");
            }
            rBO.p_REMITTANCE_ID = DecryptUrlValue(rBO.p_REMITTANCE_ID);
            //rBO.P_PAYLOC_FILE_ID = paylocID;
            rBO.P_USERID = ContextGetValue(Constants.SessionKeyUserID);
            //rBO.p_REMITTANCE_ID = remID;
            TempData["msg1"] = "File uploaded to WYPF database successfully.";

            string WebapiBaseUrlForSubmitReturn = ConfigGetValue("WebapiBaseUrlForSubmitReturn");

            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(rBO), Encoding.UTF8, "application/json");
                // string endPoint = apiLink;

                using (var response = await httpClient.PostAsync(WebapiBaseUrlForSubmitReturn, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        rBO = JsonConvert.DeserializeObject<ReturnSubmitBO>(result);
                    }
                }
            }
            TempData["submitReturnMsg"] = rBO.RETURN_STATUSTEXT;

            return RedirectToAction("Home", "Admin");
        }
        /// <summary>
        /// Delete a remittance including all processes and data at Employer level.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteRemittance(string id)
        {
            int remID =Convert.ToInt32(DecryptUrlValue(id));
            string result = string.Empty;
            string apiCallDeleteRemittance = ConfigGetValue("WebapiBaseUrlForDeleteRemittance");
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(apiCallDeleteRemittance + remID))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<string>(result);
                    }
                }
            }
            TempData["msgDelete"] = result;
            return RedirectToAction("Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRemittanceAjax(string id)
        {
            int remID = Convert.ToInt32(DecryptUrlValue(id));
            string result = string.Empty;
            string apiCallDeleteRemittance = ConfigGetValue("WebapiBaseUrlForDeleteRemittance");
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(apiCallDeleteRemittance + remID))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<string>(result);
                    }
                }
            }

            return Json(result);
        }

    }
}
