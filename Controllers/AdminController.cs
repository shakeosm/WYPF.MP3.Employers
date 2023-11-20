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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MCPhase3.Common.Constants;

namespace MCPhase3.Controllers
{
    public class AdminController : BaseController
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration, IRedisCache cache, IDataProtectionProvider Provider, IOptions<ApiEndpoints> ApiEndpoints) : base(configuration, cache, Provider, ApiEndpoints)
        {
            _configuration = configuration;
        }


        /// <summary>This will only be used to pass to WYPF by Emp for processing</summary>
        /// <returns></returns>
        public IActionResult SubmitForProcessing()
        {

            EventDetailsBO eBO = new()
            {

                remittanceID = Convert.ToInt32(GetRemittanceId(returnAsEncrypted: false)), // we can put variable name with variable value when calling a function to make it more readable                    
                remittanceStatus = 1,
                eventTypeID = 105,
                notes = "Data quality score threshold check skipped, File passed to WYPF by Emp for processing."
            };

            InsertEventDetails(eBO);

            return RedirectToAction("Index");
        }

        /// <summary>This will only be used to pass to WYPF by Emp for processing</summary>
        /// <returns></returns>
        public IActionResult SubmitForProcessingAjax()
        {
            var taskResult = new TaskResults();
            var currentRemittance = Convert.ToInt32(GetRemittanceId(returnAsEncrypted: false)); // we can put variable name with variable value when calling a function to make it more readable                    

            EventDetailsBO eBO = new()
            {

                remittanceID = currentRemittance,
                remittanceStatus = 1,
                eventTypeID = 105,
                notes = "Data quality score threshold check skipped, File passed to WYPF by Emp for processing."
            };

            InsertEventDetails(eBO);

            taskResult.IsSuccess = true;
            taskResult.Message = $"The remittance: {currentRemittance} is successfully passed to WYPF for further processing.";
            return Json(taskResult);
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
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);

            var userid = ContextGetValue(Constants.SessionKeyUserID);
            
            var dashboardFilter = new RemittanceSelectParamBO
            {
                UserId = userid,
                StatusType = Constants.StatusType_COMPLETE
            };

            List<RemittanceItemVM> dashboardBO = await GetRemittanceListByStatus(dashboardFilter);

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
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Password", "Invalid password. Please use a strong password.");
                return View(loginBO);
            }

            string passwordPattern = @"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{9,255}$";
            var passwordStrength = Regex.Match(loginBO.password, passwordPattern);
            if (!passwordStrength.Success)
            {
                ModelState.AddModelError("Password", "Invalid password. Please use a strong password.");
                return View(loginBO);
            }

            //List<string> passwordReq  = new List<string>
            //{
            //    "at least nine characters",
            //    "one or more capital letters",
            //    "one or more lower case letters",
            //    "one or more numbers",
            //    "one or more special characters, for example !,”.#",
            //};

            ViewBag.isStaff = 1;
            loginBO.userName = ContextGetValue(Constants.SessionKeyUserID);
            var result = (Password)await UpdatePasswordMethod(loginBO);
            if (result == Password.Updated)
            {
                TempData["Msg1"] = "Password updated successfully, please login using new password.";
                return RedirectToAction("Logout", "Login");
            }
            if (result == Password.Invalid)     //## this will not happen.. as we already have verified against RegEx.Match()
            {
                TempData["UpdateMessage"] = "Password does not meet our complexity requirements: ";
                TempData["PasswordRequ"] = 1;
                return RedirectToAction("UpdatePassword", "Admin");
            }
            else if (result == Password.IncorrectOldPassword)
            {
                TempData["UpdateMessage"] = "Old Password is not correct, please try again.";
                return RedirectToAction("UpdatePassword", "Admin");
            }
            else
            {
                TempData["UpdateMessage"] = "Failed to updated password, please try again.";
                return RedirectToAction("UpdatePassword", "Admin");
            }
        }

        private async Task<int> UpdatePasswordMethod(LoginBO loginBO)
        {
            string apiBaseUrlForLoginCheck = GetApiUrl(_apiEndpoints.PasswordChange);
            string apiResponse = await ApiPost(apiBaseUrlForLoginCheck, loginBO);
            loginBO.result = JsonConvert.DeserializeObject<int>(apiResponse);

            return loginBO.result;

        }


        /// <summary>
        /// call dashboard api
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        private async Task<List<RemittanceItemVM>> GetRemittanceListByStatus(RemittanceSelectParamBO dashboardFilter)
        {
            string apiBaseUrlForDashboard = GetApiUrl(_apiEndpoints.Dashboard);
            var apiResult = new List<RemittanceItemVM>();

            string apiResponse = await ApiPost(apiBaseUrlForDashboard, dashboardFilter);
            if (string.IsNullOrEmpty(apiResponse) == false)
            {
                apiResult = JsonConvert.DeserializeObject<List<RemittanceItemVM>>(apiResponse);
            }


            if (apiResult != null && apiResult.Count > 0)
            {
                foreach (var item in apiResult)
                {
                    item.remittance_IdEnc = EncryptUrlValue(item.remittance_Id.ToString());
                }

                return apiResult.OrderByDescending(d => d.remittance_Id).ToList();
            }
            else
            {
                return new List<RemittanceItemVM>();
            }
        }


        public async Task<IActionResult> Home()
        {
            //check of PaylocID session has value then empty it.
            if (ContextGetValue(Constants.SessionKeyPaylocFileID) != null)
            {
                HttpContext.Session.Clear();
            }

            var paramList = new RemittanceSelectParamBO
            {
                UserId = ContextGetValue(Constants.SessionKeyUserID),
                StatusType = Constants.StatusType_EMPLOYER
            };

            var viewModel = new DashboardWrapperVM
            {
                RemittanceList = await GetRemittanceListByStatus(paramList)
            };

            return View(viewModel);
           
        }


        /// <summary>
        /// This will be used from Admin dashboard- to show the details of a Pending submission file
        /// </summary>
        /// <param name="remittanceId">Remittance Id</param>
        /// <returns>A Partial view with list of all records in that suubmission</returns>
        [HttpGet]
        public async Task<ActionResult> GetSubmissionDetails(string remittanceId)
        {

            if (!string.IsNullOrEmpty(remittanceId))
            {
                string WebapiBaseUrlForDetailEmpList = GetApiUrl(_apiEndpoints.DetailEmpList);
                string remid = DecryptUrlValue(remittanceId);

                var paramList = new MasterDetailEmpListBO()
                {
                    L_USERID = CurrentUserId(),
                    L_REMITTANCE_ID = remid,
                    L_STATUSTYPE = "ALL"
                };

                string apiResponse = await ApiPost(WebapiBaseUrlForDetailEmpList, paramList);
                var submissionDetails = JsonConvert.DeserializeObject<List<RemittanceItemVM>>(apiResponse);

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
            string apiBaseUrlForDashboard = GetApiUrl(_apiEndpoints.DashboardEmployers);

            string apiResponse = await ApiGet(apiBaseUrlForDashboard + userid);
            var listBO = JsonConvert.DeserializeObject<List<GetRemittanceStatusByUserBO>>(apiResponse);


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
            string apiBaseUrlForDashboardScoreHist = GetApiUrl(_apiEndpoints.DashboardScoreHist);

            string apiResponse = await ApiGet(apiBaseUrlForDashboardScoreHist + remittanceId);
            var listBO = JsonConvert.DeserializeObject<List<DashboardHistScoreBO>>(apiResponse);


            if (listBO != null && listBO.Count > 0)
            {
                foreach (var item in listBO)
                {
                    item.remittanceId_Encrypted = EncryptUrlValue(item.remittance_Id.ToString());
                }
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
            if (rBO.p_REMITTANCE_ID == null)
            {
                return RedirectToAction("Logout", "Home");
            }
            rBO.p_REMITTANCE_ID = DecryptUrlValue(rBO.p_REMITTANCE_ID);
            rBO.P_USERID = ContextGetValue(Constants.SessionKeyUserID);

            //## call the 'WebapiBaseUrlForSubmitReturn' API
            var apiResult = await SubmitReturn_UpdateScore(rBO);
            if (apiResult.IsSuccess)
            {
                TempData["msg1"] = $"Score updated, Remittance: {rBO.p_REMITTANCE_ID}. Status: {apiResult.Message}";
                TempData["submitReturnMsg"] = apiResult.Message;
            }
            else
            {
                TempData["msg1"] = "Failed to update WYPF database.";
            }

            return RedirectToAction("Home", "Admin");
        }


        /// <summary>This will update Score for a Remittance</summary>
        /// <param name="rBO">ReturnSubmitBO Model</param>
        /// <returns>ApiCallResult View Model</returns>
        async Task<ApiCallResultVM> SubmitReturn_UpdateScore(ReturnSubmitBO rBO)
        {
            string WebapiBaseUrlForSubmitReturn = GetApiUrl(_apiEndpoints.SubmitReturn);
            var apiResult = new ApiCallResultVM() { IsSuccess = false };
            try
            {
                var contents = await ApiPost(WebapiBaseUrlForSubmitReturn, rBO);
                rBO = JsonConvert.DeserializeObject<ReturnSubmitBO>(contents);

                var statusCode = (int)rBO.L_STATUSCODE.GetValueOrDefault(0);

                apiResult.IsSuccess = rBO.Success;
                apiResult.Message = rBO.Message;

                return apiResult;
            }
            catch (Exception ex)
            {
                var errorDetails = new ErrorViewModel()
                {

                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    UserId = HttpContext.Session.GetString(Constants.UserIdKeyName),
                    ApplicationId = Constants.EmployersPortal,
                    ErrorPath = ex.Source + ",  url: " + WebapiBaseUrlForSubmitReturn,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace

                };

                string insertErrorLogApi = GetApiUrl(_apiEndpoints.ErrorLogCreate);
                await ApiPost(insertErrorLogApi, errorDetails);
                return apiResult;
            }
        }


        /// <summary>
        /// Delete a remittance including all processes and data at Employer level.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteRemittance(string id)
        {
            int remID = Convert.ToInt32(DecryptUrlValue(id));
            string apiCallDeleteRemittance = GetApiUrl(_apiEndpoints.DeleteRemittance);
            string userId = CurrentUserId();

            string apiResponse = await ApiGet($"{apiCallDeleteRemittance}{remID}/{userId}");
            string result = JsonConvert.DeserializeObject<string>(apiResponse);

            TempData["msgDelete"] = result;
            return RedirectToAction("Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRemittanceAjax(string id)
        {
            int remID = Convert.ToInt32(DecryptUrlValue(id));
            string userId = CurrentUserId();
            string apiDeleteRemittanceUrl = GetApiUrl(_apiEndpoints.DeleteRemittance);

            string apiResponse = await ApiGet($"{apiDeleteRemittanceUrl}{remID}/{userId}");
            string result = JsonConvert.DeserializeObject<string>(apiResponse);

            return Json(result);
        }

    }
}
