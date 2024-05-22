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
using System.IO;
using System.Linq;
using System.Text;
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
                eventTypeID = (int)RemittanceStatus.PassedToWypf,
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
            
            LogInfo($"Loading SubmitForProcessingAjax(), remittanceId: {currentRemittance}", true);

            EventDetailsBO eBO = new()
            {

                remittanceID = currentRemittance,
                remittanceStatus = 1,
                eventTypeID = (int)RemittanceStatus.PassedToWypf,
                notes = "Data quality score threshold check skipped, File passed to WYPF by Emp for processing."
            };

            InsertEventDetails(eBO);

            taskResult.IsSuccess = true;
            taskResult.Message = $"The remittance: {currentRemittance} is successfully passed to WYPF for further processing.";

            LogInfo(eBO.notes + "; " + taskResult.Message);

            //## Now try to read the current status- if new Status is '110: Submitted to WYPF'- The Finance Business Partner needs to know a new Submission being pushed for further processing
            var remitInfo = GetRemittanceInfo(currentRemittance);
            if (remitInfo.StatusCode == (int)RemittanceStatus.SubmittedToWypf) {
                _ = InsertNotification_For_SubmittedToWYPF(currentRemittance);  //## we don't need the result.. let this API continue in background and we go back to the Ajax call
            }

            LogInfo($"Finished SubmitForProcessingAjax()");

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

            var userid = CurrentUserId();
            
            var dashboardFilter = new RemittanceSelectParamBO
            {
                UserId = userid,
                StatusType = Constants.StatusType_COMPLETE
            };

            List<RemittanceItemVM> dashboardBO = await GetRemittanceListByStatus(dashboardFilter);

            return View(dashboardBO);
        }

        public async Task<IActionResult> SubmittedToWypf()
        {
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);

            var userid = CurrentUserId();

            var dashboardFilter = new RemittanceSelectParamBO
            {
                UserId = userid,
                StatusType = Constants.StatusType_WYPF
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
            LogInfo($"Loading GetScoreHistoryPartialView(), remittanceId: {remittanceId}", true);

            int remID = Convert.ToInt32(DecryptUrlValue(remittanceId));
            List<DashboardHistScoreBO> dashboardScoreHistBO = await getDashboardScoreHist(remID);
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);

            LogInfo($"Finished GetScoreHistoryPartialView()");

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
        /// call dashboard api
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        private async Task<List<RemittanceItemVM>> GetRemittanceListByStatus(RemittanceSelectParamBO dashboardFilter)
        {
            string apiBaseUrlForDashboard = GetApiUrl(_apiEndpoints.DashboardRecentSubmission); //# api/DashboardRecentSubmission
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

            //var currrentUser = ContextGetValue(Constants.LoginNameKey);

            var paramList = new RemittanceSelectParamBO
            {
                UserId = CurrentUserId(),
                StatusType = Constants.StatusType_EMPLOYER
            };

            var viewModel = new DashboardWrapperVM
            {
                RemittanceList = await GetRemittanceListByStatus(paramList)
            };

            LogInfo($"Admin/Home Loaded.. UserId: {paramList.UserId}, StatusType: {Constants.StatusType_EMPLOYER}");

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
            
            LogInfo($"getDashboardValuesForEmployers()-> api: {apiBaseUrlForDashboard}{userid}");

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
            LogInfo($"Loading GetDashboardScoreHist(), remittanceId: {remittanceId}", true);
            string apiBaseUrlForDashboardScoreHist = GetApiUrl(_apiEndpoints.DashboardScoreHist);

            LogInfo($"apiBaseUrlForDashboardScoreHist()-> api: {apiBaseUrlForDashboardScoreHist}");

            string apiResponse = await ApiGet(apiBaseUrlForDashboardScoreHist + remittanceId);
            var listBO = JsonConvert.DeserializeObject<List<DashboardHistScoreBO>>(apiResponse);


            if (listBO != null && listBO.Count > 0)
            {
                foreach (var item in listBO)
                {
                    item.remittanceId_Encrypted = EncryptUrlValue(item.remittance_Id.ToString());
                }
            }

            LogInfo($"Finished GetDashboardScoreHist()");

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
                TempData["msg1"] = "Invalid Remittance Id.";
                return RedirectToAction("Home", "Admin");
            }

            rBO.p_REMITTANCE_ID = DecryptUrlValue(rBO.p_REMITTANCE_ID);
            rBO.P_USERID = CurrentUserId();

            _ = Int32.TryParse(rBO.p_REMITTANCE_ID, out int remittanceId);

            LogInfo($"Initiating SubmitReturn(), RemittanceID: {rBO.p_REMITTANCE_ID}", true);

            var apiResult = await SubmitReturn_UpdateScore(rBO);

            if (apiResult.IsSuccess)
            {
                TempData["msg1"] = $"Score updated, Remittance: {rBO.p_REMITTANCE_ID}. Status: {apiResult.Message}";
                TempData["submitReturnMsg"] = apiResult.Message;
                
                //## Now try to read the current status- if new Status is '110: Submitted to WYPF'- The Finance Business Partner needs to know a new Submission being pushed for further processing
                var remitInfo = GetRemittanceInfo(remittanceId);
                if (remitInfo.StatusCode == (int)RemittanceStatus.SubmittedToWypf)
                {
                    _ = InsertNotification_For_SubmittedToWYPF(remittanceId);  //## we don't need the result.. let this API continue in background and we go back to the Ajax call
                }

            }
            else
            {
                TempData["MsgError"] = $"Failed to update WYPF database. Reason: {apiResult.Message}";
            }

            LogInfo($"Finished SubmitReturn()");
            return RedirectToAction("Home", "Admin");
        }


        /// <summary>This will update Score for a Remittance</summary>
        /// <param name="rBO">ReturnSubmitBO Model</param>
        /// <returns>ApiCallResult View Model</returns>
        async Task<ApiCallResultVM> SubmitReturn_UpdateScore(ReturnSubmitBO rBO)
        {
            LogInfo($"Initiating SubmitReturn_UpdateScore(), RemittanceID: {rBO.p_REMITTANCE_ID}", true);

            string WebapiBaseUrlForSubmitReturn = GetApiUrl(_apiEndpoints.SubmitReturn);    //## api/SubmitReturn
            var apiResult = new ApiCallResultVM() { IsSuccess = false };
            try
            {
                var contents = await ApiPost(WebapiBaseUrlForSubmitReturn, rBO);
                rBO = JsonConvert.DeserializeObject<ReturnSubmitBO>(contents);

                var statusCode = (int)rBO.L_STATUSCODE.GetValueOrDefault(0);

                apiResult.IsSuccess = rBO.Success;
                apiResult.Message = rBO.Message;

                LogInfo($"Finished SubmitReturn_UpdateScore(), RemittanceID: {rBO.p_REMITTANCE_ID}, apiResult.IsSuccess: {apiResult.IsSuccess}, apiResult.Message: {apiResult.Message}");

                return apiResult;
            }
            catch (Exception ex)
            {
                var errorDetails = new ErrorViewModel()
                {

                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    UserId = HttpContext.Session.GetString(Constants.LoginNameKey),
                    ApplicationId = Constants.EmployersPortal,
                    ErrorPath = ex.Source + ",  url: " + WebapiBaseUrlForSubmitReturn,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace

                };

                string insertErrorLogApi = GetApiUrl(_apiEndpoints.ErrorLogCreate);
                await ApiPost(insertErrorLogApi, errorDetails);
                if (ex.Message.Contains("HttpClient.Timeout"))
                {
                    apiResult.Message = "Server timed out due to large volume of data. Please wait and refresh your page after a few minutes to see the latest score for this Remittance: " + rBO.p_REMITTANCE_ID;
                }
                else {
                    apiResult.Message = "DB Server execution error!";
                }

                LogInfo($"ERROR SubmitReturn_UpdateScore(), RemittanceID: {rBO.p_REMITTANCE_ID}, ErrorPath: {errorDetails.ErrorPath}, Message: {errorDetails.Message}");

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

            LogInfo($"DeleteRemittance() -> apiCallDeleteRemittance()-> api: {apiCallDeleteRemittance}");

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
            
            LogInfo($"DeleteRemittanceAjax() -> apiDeleteRemittanceUrl()-> api: {apiDeleteRemittanceUrl}");

            string apiResponse = await ApiGet($"{apiDeleteRemittanceUrl}{remID}/{userId}");
            var result = JsonConvert.DeserializeObject<TaskResults>(apiResponse);

            return Json(result);
        }


        [HttpGet]
        public IActionResult CPanel()
        {
            if (!I_Am_A_SuperUser()) {
                LogInfo($"User attempting to Load Admin/CPanel page which is restricted.");
                return RedirectToAction("Home");
            }

            string uploadFolderPath = _configuration["FileUploadPath"];
            string filesInDoneFolderPath = uploadFolderPath + _configuration["FileUploadDonePath"];
            string logFileFolderPath = _configuration["LogDebugInfoFilePath"];

            var vm = new AdminControlPanelVM
            {
                FilesNotProcessedList = GetFileList(uploadFolderPath),
                FilesInDoneList = GetFileList(filesInDoneFolderPath),
                UserActivityList = GetFileList(logFileFolderPath)
            };

            return View("ControlPanel", vm);
        }

        public List<FileDetails> GetFileList(string folderPath)
        {
            LogInfo($"GetFileList() -> folderPath: {folderPath}");
            //string uploadFolder = _configuration["FileUploadPath"];
            var fileList = new List<FileDetails>();

            if (System.IO.Path.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath);

                if (files.Any() == false)
                {
                    return fileList;
                }
               
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    var created = fi.CreationTime;

                    fileList.Add(new FileDetails() { 
                        CreatedOn = created,
                        FileName = file.Replace(folderPath, ""),    //## no need to show the FolderPath in the UI...
                    });

                }
            }

            return fileList.OrderByDescending(fl=> fl.CreatedOn).ToList();

        }

        //[HttpGet("/Admin/ShowUserActivityLogByAjax/{fileName}")]
        [HttpGet]
        public ActionResult ShowUserActivityLogByAjax(string id)
        {
            var result = new TaskResults()
            {
                IsSuccess = false,
                Message = "File not found!"
            };

            if (IsEmpty(id) || !id.EndsWith(".txt"))
            {
                return Json(result);
            }

            string logFileFolderPath = _configuration["LogDebugInfoFilePath"];
            string activityLogFilePath = logFileFolderPath + id;

            if (System.IO.Path.Exists(activityLogFilePath)) 
            {
                var contents = System.IO.File.ReadAllText(activityLogFilePath);
                result.IsSuccess = true;
                result.Message = contents.Replace("\r\n", "<br/>");
            }

            return Json(result);

        }

    }
}
