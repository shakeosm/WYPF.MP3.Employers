using MCPhase3.CodeRepository;
using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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



    public class ErrorWarningController : BaseController
    {
        private readonly ILogger<ErrorWarningController> _logger;
        private readonly IWebHostEnvironment _host;
        private readonly IConfiguration _Configure;
        ErrorAndWarningViewModelWithRecords _errorAndWarningViewModel = new ErrorAndWarningViewModelWithRecords();
        List<ErrorAndWarningViewModelWithRecords> model = new List<ErrorAndWarningViewModelWithRecords>();

        //following class I am using to consume api's
        EventDetailsBO eventDetails = new EventDetailsBO();

        public ErrorWarningController(ILogger<ErrorWarningController> logger, IWebHostEnvironment host, IConfiguration configuration, IRedisCache cache, IDataProtectionProvider Provider, IOptions<ApiEndpoints> ApiEndpoints) : base(configuration, cache, Provider, ApiEndpoints)
        {
            _Configure = configuration;
            _host = host;
            _logger = logger;
        }


        /// <summary>Error and warnings List is displayed in Index page</summary>
        /// <returns></returns>
        public async Task<IActionResult> Index(AlertSumBO alertSumBO, int? pageNumber)
        {
            var alertViewWrapperVM = new ErrorAndWarningWrapperVM();
            //## coming here for the first time- save in the Redis cache- and be happy .. now u can always find this record in the Cache
            if (alertSumBO.remittanceId != null){
                _cache.SetString(RemittanceIdKeyName(), alertSumBO.remittanceId);
            }
            else{
                alertSumBO.remittanceId = GetRemittanceId();
            }

            //When select to work with error and warnings on payloction level then I have to keep PaylocFile in sesssion so 
            //when come back to this page after 1st process/during process I can get that from session.
            if (alertSumBO.L_PAYLOC_FILE_ID != null)
            {
                HttpContext.Session.SetString(Constants.SessionKeyPaylocFileID, alertSumBO.L_PAYLOC_FILE_ID.ToString());
                alertViewWrapperVM.PaylocationID = alertSumBO.L_PAYLOC_FILE_ID?.ToString();
            }

            if (HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID) != null)
            {
                alertSumBO.L_PAYLOC_FILE_ID = Convert.ToInt32(HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID));
            }

            alertSumBO.L_USERID = CurrentUserId();
            var alertList = new List<ErrorAndWarningViewModelWithRecords>();

            int remID = Convert.ToInt32(DecryptUrlValue(alertSumBO.remittanceId));

            
            alertViewWrapperVM.RemittanceId = EncryptUrlValue(remID.ToString());
            alertViewWrapperVM.EmployerName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);

            //add remittance id into session for future use.

            //work on remittance level if null else Paylocation level
            string apiBaseUrlForErrorAndWarnings = GetApiUrl(_apiEndpoints.ErrorAndWarnings);   //## api: /AlertSummaryRecordsPayLoc

            alertSumBO.remittanceId = remID.ToString();
            var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, alertSumBO);
            alertList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(apiResult);

            if (alertList != null && alertList.Count > 0)
            {
                var alertListFiltered = alertList.Where(x => x.ACTION_BY.Equals("ALL")).ToList();

                foreach (var item in alertListFiltered)
                {
                    item.EncryptedRowRecordID = alertSumBO.remittanceId;
                }

                alertViewWrapperVM.ErrorsAndWarningsList = alertListFiltered;
            }            

            return View(alertViewWrapperVM);
        }


        /// <summary>
        /// Used to show all records with a warning that is suitable for bulk approval.
        /// If we pass following action a remittance id then it will show all warnings and errors 
        /// and if we pass remittance id and alert type then specific error or warning will show
        /// </summary>
        /// <returns></returns>
        // public async Task<IActionResult> WarningsListforBulkApproval(ErrorAndWarningToShowListViewModel errorAndWarningTo)

        public async Task<IActionResult> WarningsListforBulkApproval(ErrorAndWarningViewModelWithRecords summaryVM)
        {
            string errorWarningSummaryKeyName = $"{CurrentUserId()}_{Constants.ErrorWarningSummaryKeyName}";
            //following functionality is added to keep user on same warning and error page until all sorted.
            //## if we come back here from Acknowledgement page- then the ViewModel 'summaryVM' is empty- so need to read from the Cache.. save life..

            if (summaryVM.ALERT_COUNT is null) //## means empty... came here from another page not Dashboard
            {
                //## if Alert.count is NULL then its an Empty, but not null// (pain in the back)
                summaryVM = _cache.Get<ErrorAndWarningViewModelWithRecords>(errorWarningSummaryKeyName);

                if (summaryVM is null)
                {
                    return RedirectToAction("Index", "Admin");  //## should not happen
                }
            }
            else
            {
                _cache.Set(errorWarningSummaryKeyName, summaryVM);
            }

            var recordsList = new List<ErrorAndWarningViewModelWithRecords>();
            string userName = CurrentUserId();

            var alertSumBO = new AlertSumBO()
            {
                remittanceId = summaryVM.remittanceID,
                L_USERID = userName
            };

            //show error and worning on Remittance or paylocation level.
            string apiBaseUrlForErrorAndWarnings = GetApiUrl(_apiEndpoints.AlertDetailsPLNextSteps);
            if (HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID) != null)
            {
                alertSumBO.L_PAYLOC_FILE_ID = Convert.ToInt32(HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID));
                //recordsList = await apiClient.GetErrorAndWarningSummary(alertSumBO, apiBaseUrlForErrorAndWarnings);
                var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, alertSumBO);
                recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(apiResult);
            }
            else
            {
                summaryVM.L_PAYLOC_FILE_ID = 0;

                var errorAndWarningTo = new ErrorAndWarningToShowListViewModel()
                {
                    remittanceID = Convert.ToDouble(DecryptUrlValue(summaryVM.remittanceID)),
                    L_USERID = userName,
                    alertType = summaryVM.ALERT_TYPE_REF,
                };

                var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, errorAndWarningTo);
                recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(apiResult);
            }

            foreach (var record in recordsList)
            {
                record.EncryptedRowRecordID = EncryptUrlValue(record.DATAROWID_RECD);
                record.EncryptedAlertid = EncryptUrlValue(record.MC_ALERT_ID);

            }

            //## Lets keep the list of all Warnings in the Cache.. in case the user wants to do AcknowledgeAll operation- we can retrive the Un-encrypted list and do AckAll operation
            //List<string> BulkApprovalRecordIdList = recordsList.Where(b => b.CLEARED_FG == "N").Select(r => r.MC_ALERT_ID).ToList();
            List<string> BulkApprovalRecordIdList = recordsList.Where(b => b.CLEARED_FG == "N").Select(r => r.EncryptedAlertid).ToList();
            _cache.Set($"{Constants.BulkApprovalAlertIdList}_{CurrentUserId()}", BulkApprovalRecordIdList);

            ViewBag.alertClass = (summaryVM.ALERT_CLASS.Equals("W")) ? "Warning" : "Error";
            ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
            ViewBag.status = summaryVM.ALERT_DESC + "";

            return View(recordsList);


        }


        /// <summary>This will be used via ajax</summary>
        /// <param name="remittanceID"></param>
        /// <param name="alertType"></param>
        /// <returns></returns>
        public async Task<IActionResult> AlertListByAjax(string remittanceID, string alertType)
        {
            //string errorWarningSummaryKeyName = $"{CurrentUserId()}_{Constants.ErrorWarningSummaryKeyName}";
            //following functionality is added to keep user on same warning and error page until all sorted.
            //## if we come back here from Acknowledgement page- then the ViewModel 'summaryVM' is empty- so need to read from the Cache.. save life..

            //if (summaryVM.ALERT_COUNT is null) //## means empty... came here from another page not Dashboard
            //{
            //    //## if Alert.count is NULL then its an Empty, but not null// (pain in the back)
            //    summaryVM = _cache.Get<ErrorAndWarningViewModelWithRecords>(errorWarningSummaryKeyName);

            //    if (summaryVM is null)
            //    {
            //        return RedirectToAction("Index", "Admin");  //## should not happen
            //    }
            //}
            //else
            //{
            //    _cache.Set(errorWarningSummaryKeyName, summaryVM);
            //}

            var recordsList = new List<ErrorAndWarningViewModelWithRecords>();
            string userName = CurrentUserId();

            var alertSumBO = new AlertSumBO()
            {
                remittanceId = remittanceID,
                L_USERID = userName
            };

            //show error and worning on Remittance or paylocation level.
            string apiBaseUrlForErrorAndWarnings = GetApiUrl(_apiEndpoints.AlertDetailsPLNextSteps);
            if (HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID) != null)
            {
                alertSumBO.L_PAYLOC_FILE_ID = Convert.ToInt32(HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID));
                //recordsList = await apiClient.GetErrorAndWarningSummary(alertSumBO, apiBaseUrlForErrorAndWarnings);
                var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, alertSumBO);
                recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(apiResult);
            }
            else
            {
                //summaryVM.L_PAYLOC_FILE_ID = 0;

                var errorAndWarningTo = new ErrorAndWarningToShowListViewModel()
                {
                    remittanceID = Convert.ToDouble(DecryptUrlValue(remittanceID)),
                    L_USERID = userName,
                    alertType = alertType,
                };

                var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, errorAndWarningTo);
                recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(apiResult);
            }

            foreach (var record in recordsList)
            {
                record.EncryptedRowRecordID = EncryptUrlValue(record.DATAROWID_RECD);
                record.EncryptedAlertid = EncryptUrlValue(record.MC_ALERT_ID);
            }

            //## Lets keep the list of all Warnings in the Cache.. in case the user wants to do AcknowledgeAll operation- we can retrive the Un-encrypted list and do AckAll operation            
            List<string> BulkApprovalRecordIdList = recordsList.Where(b => b.CLEARED_FG == "N").Select(r => r.EncryptedAlertid).ToList();
            _cache.Set($"{Constants.BulkApprovalAlertIdList}_{CurrentUserId()}", BulkApprovalRecordIdList);
                        
            //ViewBag.status = summaryVM.ALERT_DESC + "";

            return PartialView("_ErrorWarningList", recordsList);
        }


        /// <summary>This is a ByPass Action- when we need to AcknowledgeAll warnings- 
        /// we can simply come here- read the list from cache- which was set from 'WarningsListforBulkApproval' Action
        /// and use the list to do AcknowledgeAll operation</summary>
        /// <returns></returns>
        public async Task<IActionResult> AcknowledgeAll()
        {
            //## Get the cached list from Redis..
            var bulkApprovalRecordIdList = _cache.Get<List<string>>($"{Constants.BulkApprovalAlertIdList}_{CurrentUserId()}");

            if (bulkApprovalRecordIdList is null || bulkApprovalRecordIdList.Count < 1)
            {
                TempData["msg"] = "No Alert data found to acknowledge. Please try again.";
                return RedirectToAction("Index", "ErrorWarning");
            }

            string decryptedRecordIdList = "";
            foreach (var recordId in bulkApprovalRecordIdList)
            {
                decryptedRecordIdList += DecryptUrlValue(recordId) + ",";
            }

            var paramList = new ApproveWarningsInBulkVM()
            {
                AlertIdList = decryptedRecordIdList,
                UserID = CurrentUserId()
            };

            string apiApproveWarningsBulkList = GetApiUrl(_apiEndpoints.ApproveWarningsBulkList);
            var apiresult = await ApiPost(apiApproveWarningsBulkList, paramList);
            paramList = JsonConvert.DeserializeObject<ApproveWarningsInBulkVM>(apiresult);  //## we get the ApiCall result in the same object and see what status we have got in return

            //## All done.. now send the user back to the 'Index' with success status
            TempData["msg"] = "All warnings are successfully Acknowledged.";
            return RedirectToAction("Index", "ErrorWarning");
        }


        /// <summary>This is a short-circuit to go to Summary page without needing to create the URL from the Alert details child pages</summary>
        /// <returns></returns>
        public IActionResult GoToSummaryPage()
        {
            return RedirectToAction("Index", new AlertSumBO());
        }


        /// <summary>
        /// BulkApprove all records and update in database.
        /// this action is used for both bulck approves and to approve 
        /// warnings on update record page.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> WarningApproval(string idList, string id)
        {
            if (string.IsNullOrEmpty(idList))
            {
                TempData["Msg1"] = "No Alert data found to acknowledge. Please try again.";
                return RedirectToAction("WarningsListforBulkApproval", "ErrorWarning");
            }

            string apiBaseUrlForErrorAndWarningsApproval = GetApiUrl(_apiEndpoints.ErrorAndWarningsApproval);
            //string apiBaseUrlForInsertEventDetails = GetApiUrl(_apiEndpoints.InsertEventDetails);

            var errorAndWarningTo = new ErrorAndWarningApprovalOB();
            errorAndWarningTo.userID = CurrentUserId();


            var alertIdList = idList.Split(",", StringSplitOptions.RemoveEmptyEntries);

            using (HttpClient client = new HttpClient())
            {

                string endpoint = apiBaseUrlForErrorAndWarningsApproval;

                foreach (var ids in alertIdList)
                {
                    string decryptedAlertId = DecryptUrlValue(ids, forceDecode: false);
                    errorAndWarningTo.alertID = Convert.ToInt32(decryptedAlertId);

                    StringContent content = new StringContent(JsonConvert.SerializeObject(errorAndWarningTo), Encoding.UTF8, "application/json");

                    using var Response = await client.PostAsync(endpoint, content);
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        _logger.LogInformation($"BulkApproval API Call successfull-> {endpoint}");
                        string result = await Response.Content.ReadAsStringAsync();
                        errorAndWarningTo = JsonConvert.DeserializeObject<ErrorAndWarningApprovalOB>(result);

                        TempData["msg"] = errorAndWarningTo.returnStatusTxt;

                        if (errorAndWarningTo.returnStatusTxt.Contains("not found"))
                        {
                            Console.WriteLine(errorAndWarningTo.returnStatusTxt);
                            //TODO-> need to inform the user about the failure                           
                        }
                    }
                }
            }

            return RedirectToAction("WarningsListforBulkApproval", "ErrorWarning");
        }


        // <summary>
        /// this action is used to approve a single Warning - by Ajax calls..        
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> WarningAcknowledge(string alertId)
        {
            var result = new TaskResults();

            string ErrorAndWarningsApprovalUrl = GetApiUrl(_apiEndpoints.ErrorAndWarningsApproval);
            int.TryParse(DecryptUrlValue(alertId), out int decryptedAlertId);

            if (decryptedAlertId < 1)
            {
                result.Message = "Invalid Alert Id.";
                return Json(result);
            }

            ErrorAndWarningApprovalOB errorAndWarningTo = new()
            {
                userID = CurrentUserId(),
                alertID = decryptedAlertId
            };

            var apiResult = await ApiPost(ErrorAndWarningsApprovalUrl, errorAndWarningTo);
            errorAndWarningTo = JsonConvert.DeserializeObject<ErrorAndWarningApprovalOB>(apiResult);
            string approvalResult = errorAndWarningTo.returnStatusTxt;

            result.IsSuccess = true;
            result.Message = approvalResult;

            return Json(result);

        }


        /// <summary>
        /// This action will show all the selected data on view.
        /// </summary>
        /// <returns></returns>
        //[HttpGet]      
        public async Task<IActionResult> UpdateSingleRecord(string id)
        {
            try
            {
                List<ErrorAndWarningViewModelWithRecords> recordsList = new();
                ErrorAndWarningViewModelWithRecords records = new();

                int.TryParse(DecryptUrlValue(id, forceDecode: false), out int dataRowID);
                //show employer name
                ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
                string userName = CurrentUserId();
                MemberUpdateRecordBO memberUpdateRecordBO = new();

                string apiBaseUrlForErrorAndWarningsApproval = GetApiUrl(_apiEndpoints.UpdateRecordGetValues);

                string apiBaseUrlForErrorAndWarningsList = GetApiUrl(_apiEndpoints.UpdateRecordGetErrorWarningList);
                string apiResponse = String.Empty;

                QueryParamVM helpTextBO = new QueryParamVM();
                helpTextBO.L_USERID = userName;
                helpTextBO.L_DATAROWID_RECD = dataRowID;

                apiResponse = await ApiPost(apiBaseUrlForErrorAndWarningsList, helpTextBO);
                recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(apiResponse);

                foreach (var item in recordsList)
                {
                    item.EncryptedRowRecordID = EncryptUrlValue(item.MC_ALERT_ID);   //## Encrypt them to be used in QueryString
                }

                ViewBag.HelpText = recordsList;
                string result = string.Empty;

                apiResponse = await ApiPost(apiBaseUrlForErrorAndWarningsApproval, helpTextBO);
                memberUpdateRecordBO = JsonConvert.DeserializeObject<MemberUpdateRecordBO>(apiResponse);

                memberUpdateRecordBO.dataRowID = dataRowID;
                memberUpdateRecordBO.ErrorAndWarningList = recordsList;

                memberUpdateRecordBO.DataRowEncryptedId = id;  //## we are sending the Encrypted Id back to the UI- to allow 'Switch View'
                return View(memberUpdateRecordBO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                throw new Exception("Failed to execute UpdateSingleRecord() Process.", ex);
            }
        }


        /// <summary>
        /// follown action will update new changes into database 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UpdateSingleRecord(MemberUpdateRecordBO updateRecordBO)
        {
            updateRecordBO.modUser = CurrentUserId();

            //string remittanceID_Encrypted = GetRemittanceId();

            string apiLink = GetApiUrl(_apiEndpoints.UpdateRecord);

            string apiResponse = await ApiPost(apiLink, updateRecordBO);
            updateRecordBO = JsonConvert.DeserializeObject<MemberUpdateRecordBO> (apiResponse);

            var updateStatus = new UpdateStatusVM()
            {
                DisplayMessage = $"<h5>Member: {updateRecordBO.forenames} {updateRecordBO.lastName}</h5>"+
                                 $"<h5>Job title: {updateRecordBO.jobTitle}</h5>" +
                                 $"<h5>Date of Birth: {updateRecordBO.DOB?.ToShortDateString()}</h5>" +
                                 $"<h5>Postcode: {updateRecordBO.postCode}</h5>" +
                                 $"<h5>NI: {updateRecordBO.NI}</h5><br/><br/><hr/><br/>" +
                                 "<p class='h5 text-primary'>The record is successfully updated. Please click on the 'Back' button to go back to Error/Warning list.</p>",
                Header = "Update successful",
                IsSuccess = true
            };

            var statusCode = updateRecordBO.statusCode.GetValueOrDefault(-99);

            if (statusCode != 0) {
                updateStatus.IsSuccess = false;
                updateStatus.DisplayMessage = updateRecordBO.statusTxt + "<p class='h4 text-primary'>Please try again later</p>";
                updateStatus.Header = "Update failed.";

            }

            //return RedirectToAction("WarningsListforBulkApproval", "ErrorWarning", remittanceID_Encrypted);
            return View("UpdateStatus", updateStatus);
        }


        /// <summary>
        /// This is new action which will handle all the loose matching and 
        /// new folder create or new starter process.
        /// GETs file record and shows matches from UPM and updates UPM by selected record.
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> MemberFolderMatching(string id)
        {
            GetMatchesViewModel matchingPageWrapperVM = new GetMatchesViewModel();
            
            int.TryParse(DecryptUrlValue(id), out int dataRowID);
            
            matchingPageWrapperVM.EmployersName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
            
            QueryParamVM memberRecordQuery = new QueryParamVM
            {
                L_DATAROWID_RECD = dataRowID,
                L_USERID = HttpContext.Session.GetString(Constants.SessionKeyUserID)
            };
            

            string apiBaseUrlForUpdateRecordGetValues = GetApiUrl(_apiEndpoints.UpdateRecordGetValues);
            //## show Member record- from the 'Contribution data received'
            string apiResponse = await ApiPost(apiBaseUrlForUpdateRecordGetValues, memberRecordQuery);
            MemberUpdateRecordBO memberRecord = JsonConvert.DeserializeObject<MemberUpdateRecordBO>(apiResponse);

            //To cal all values/data of member record. that is already in UPM.
            MatchingRecordQueryVM getMatchesBO = new ()
            {
                userId = HttpContext.Session.GetString(Constants.SessionKeyUserID),
                dataRowId = dataRowID
            };

            //method to get matching records from UPM- Potential matches to compare with
            matchingPageWrapperVM.MatchingPersonList = await GetRecords(getMatchesBO);  //## api/MatchingRecords

            if (matchingPageWrapperVM.MatchingPersonList.Count >= 1)
            {
                memberRecord.dataRowID = dataRowID;
                matchingPageWrapperVM.MemberRecord = memberRecord;

                foreach (var item in matchingPageWrapperVM.MatchingPersonList)
                {
                    item.dataRowId = dataRowID;
                }

                matchingPageWrapperVM.DataRowEncryptedId = id;  //## we are sending the Encrypted Id back to the UI- to allow 'Switch View'
            }

            //## save the matching list in the Cache.. so - we can process them later- on Submit() form..
            _cache.Set($"{CurrentUserId()}_{Constants.MemberMatchingList}", matchingPageWrapperVM.MatchingPersonList);

            return View(matchingPageWrapperVM);

        }


        /// <summary>
        /// only datarowid and userid need to call api.
        /// </summary>
        /// <param name="getMatchesBO"></param>
        /// <returns></returns>
        private async Task<List<MatchingPersonVM>> GetRecords(MatchingRecordQueryVM getMatchesBO)
        {
            string apiLink = GetApiUrl(_apiEndpoints.MatchingRecordsUPM);   //## api/MatchingRecords
            string apiResponse = await ApiPost(apiLink, getMatchesBO);
            var apiResult = JsonConvert.DeserializeObject<List<MatchingPersonVM>>(apiResponse);
            return apiResult;
        }

        /// <summary>
        /// following method will update record in UPM
        /// </summary>
        /// <param name="getMatchesBO"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MemberFolderMatching(MemberFolderMatchingVM selectedMember)
        {
            string selectedAction = selectedMember.ActiveProcess;
            string action = selectedAction.Split("_")[0];
            string personFolderId = selectedAction.Split("_")[1];   //## it can be either PersonID or a FolderID. NEWREC doesn't have a FolderId.. that's why!

            var matchingList = _cache.Get<List<MatchingPersonVM >> ($"{CurrentUserId()}_{Constants.MemberMatchingList}");
            var selectedFolder = new MatchingPersonVM();

            var updateStatus = new UpdateStatusVM();

            if (action == "NEWREC")
            {
                selectedFolder = matchingList.FirstOrDefault(m => m.personId == Convert.ToInt32(personFolderId) && m.folderRef == "NEWREC");
            }
            else if (action == "UpdateFolder")
            {
                selectedFolder = matchingList.FirstOrDefault(m => m.folderId == personFolderId);
            }

            //## Now create a 'MatchingPersonVM' to update in the DB with the values
            var member = new MatchingPersonVM()
            {
                folderId = selectedFolder.folderId,
                personId = selectedFolder.personId,
                folderRef = selectedFolder.folderRef,
                dataRowId = selectedFolder.dataRowId,
                userId = CurrentUserId(),
                note = selectedMember.Note
            };

            //## now assign some conditional values- based on selected action, ie: NewRec=> 'folderMatch = "90"'
            if (action == "UpdateFolder")
            {
                member.folderMatch = "90";
                member.personMatch = "90";
            }
            else if (action == "NEWREC")
            {
                member.folderId = null;
                member.personId = selectedFolder.personId;

                member.folderMatch = "95";
                member.personMatch = "90";
            }
            else if (action == "AddNewPersonAndFolder")
            {
                member.folderId = null;
                member.personId = null;

                member.folderMatch = "95";
                member.personMatch = "95";
            }
            else
            {
                throw new Exception("Unknown user action was passed. Please reload the page and try again.");
            }

            string endPoint = GetApiUrl(_apiEndpoints.MatchingRecordsManual);
            var apiResult = await ApiPost(endPoint, member);

            if (apiResult == "")
            { //## api call failed.. Error!
                TempData["msg"] = "Error: Failed to update record";
                updateStatus.Header = "Error: Failed to update record";
                updateStatus.DisplayMessage = "Failed to update the record. Please try again later.";
            }
            else {
                //## what if the user selected "AddNewPersonAndFolder"..? then there will be no Member name to display...
                if (selectedAction.Equals("AddNewPersonAndFolder")){
                    updateStatus.DisplayMessage = $"<h5>A new Folder record is created for that selected person.</h5>";
                }
                else {
                    if (action == "NEWREC"){
                        updateStatus.DisplayMessage = $"<h5>A new record is created for that selected person.</h5>";
                    }
                    else { 
                        updateStatus.DisplayMessage = $"<h5>Member: {member.upperForeNames} {member.upperSurName}</h5>" +
                             $"<h5>Job title: {member.jobTitle}</h5>" +
                             $"<h5>Date of Birth: {member.DOB.ToShortDateString()}</h5>" +
                             $"<h5>Postcode: {member.postCode}</h5>" +
                             $"<h5>NI: {member.NINO}</h5><br/><br/><hr/><br/>" +
                             "<p class='h5 text-primary'>The record is successfully updated. Please click on the 'Back' button to go back to Error/Warning list.</p>";
                    
                    }
                }
                updateStatus.Header = "Update successful";
                updateStatus.IsSuccess = true;

                TempData["msg"] = "Record updated successfully";
            }
           
            return View("UpdateStatus", updateStatus);
        }


        /// <summary>
        /// Ammend single record and update in database.
        /// </summary>
        /// <returns></returns>
        public IActionResult AmmendWarningsOrErrors()
        {
            return View();
        }


        /// <summary>This will be used by an Ajax call from the View page</summary>
        /// <param name="Id">Record Id</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ResetRecord(string Id)
        {

            RecordResetBO bo = new RecordResetBO();
            try
            {

                _ = int.TryParse(DecryptUrlValue(Id, forceDecode: false), out int dataRowID);
                int remID = Convert.ToInt32(GetRemittanceId(returnAsEncrypted: false));

                bo.P_DATAROWID_RECD = dataRowID;
                bo.P_USERID = CurrentUserId(); ;

                using (HttpClient client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(bo), Encoding.UTF8, "application/json");

                    string apiBaseUrlForRecordReset = GetApiUrl(_apiEndpoints.RecordReset);

                    using var Response = await client.PostAsync(apiBaseUrlForRecordReset, content);
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                        _logger.LogInformation("RecordReset API Call successfull");
                        //call following api to get this uploaded remittance id of file.
                        string result = await Response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<string>(result);
                    }
                }
                return Json("success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                return Json("Details: " + ex.ToString());
            }
        }


        private Dictionary<string, string> GetListFromStringArray(string val)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            string[] newArray = val.Split("\":");

            //pass array value and remove all the extra quotes and dots
            string val1 = RemoveQuotesAndDots(newArray[0]);
            string val2 = RemoveQuotesAndDots(newArray[1]);
            data.Add(val1, val2);

            return data;
        }


        /// <summary>
        /// Api returned dots and special chars
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private string RemoveQuotesAndDots(string val)
        {
            var newVal = Regex.Replace(val, @"[^0-9a-zA-Z\.\'\s_]", "");
            return newVal;
        }
    }
}
