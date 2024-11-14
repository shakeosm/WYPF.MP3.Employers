using CsvHelper;
using CsvHelper.Configuration;
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
using System.IO;
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
        ErrorAndWarningVM _errorAndWarningViewModel = new ErrorAndWarningVM();
        List<ErrorAndWarningVM> model = new List<ErrorAndWarningVM>();

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
        public async Task<IActionResult> Index(AlertSumBO alertSumBO)
        {
            var alertViewWrapperVM = new ErrorAndWarningWrapperVM();
            //## coming here for the first time- save in the Redis cache- and be happy .. now u can always find this record in the Cache
            if (alertSumBO.RemittanceId != null){
                _cache.SetString(RemittanceIdKeyName(), alertSumBO.RemittanceId);
            }
            else{
                alertSumBO.RemittanceId = GetRemittanceId();
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
            var alertList = new List<ErrorAndWarningVM>();

            int remID = Convert.ToInt32(DecryptUrlValue(alertSumBO.RemittanceId));
            LogInfo($"ErrorWarning/Index, remittanceId: {remID}", true);

            alertViewWrapperVM.RemittanceId = EncryptUrlValue(remID.ToString());
            alertViewWrapperVM.EmployerName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);

            //add remittance id into session for future use.

            //work on remittance level if null else Paylocation level
            string apiBaseUrlForErrorAndWarnings = GetApiUrl(_apiEndpoints.ErrorAndWarnings);   //## api: /AlertSummaryRecordsPayLoc
            LogInfo($"ErrorAndWarningsApi: {apiBaseUrlForErrorAndWarnings}");

            alertSumBO.RemittanceId = remID.ToString();
            var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, alertSumBO);
            if (apiResult.NotEmpty()) { 
                alertList = JsonConvert.DeserializeObject<List<ErrorAndWarningVM>>(apiResult);            
            }

            if (alertList.HasItems())
            {
                var alertListFiltered = alertList.Where(x => x.ACTION_BY.Equals("ALL")).ToList();

                foreach (var item in alertListFiltered)
                {
                    item.EncryptedRowRecordID = alertSumBO.RemittanceId;
                }

                alertViewWrapperVM.ErrorsAndWarningsList = alertListFiltered;
            }

            LogInfo($"Finished ErrorWarning/Index");

            return View(alertViewWrapperVM);
        }


        /// <summary>
        /// Used to show all records with a warning that is suitable for bulk approval.
        /// If we pass following action a remittance id then it will show all warnings and errors 
        /// and if we pass remittance id and alert type then specific error or warning will show
        /// </summary>
        /// <returns></returns>
        // public async Task<IActionResult> WarningsListforBulkApproval(ErrorAndWarningToShowListViewModel errorAndWarningTo)
        public async Task<IActionResult> WarningsListforBulkApproval(AlertQueryVM summaryVM)
        {
            string errorWarningSummaryKeyName = $"{CurrentUserId()}_{Constants.ErrorWarningSummaryKeyName}";

            //following functionality is added to keep user on same warning and error page until all sorted.
            //## if we come back here from Acknowledgement page- then the ViewModel 'summaryVM' is empty- so need to read from the Cache.. save life..

            if (summaryVM.Total is null) //## means empty... came here from another page not Dashboard
            {
                //## if Alert.count is NULL then its an Empty, but not null// (pain in the back)
                summaryVM = _cache.Get<AlertQueryVM>(errorWarningSummaryKeyName);

                if (summaryVM is null)
                {
                    return RedirectToAction("Index", "Admin");  //## should not happen
                }
            }
            else
            {
                _cache.Set(errorWarningSummaryKeyName, summaryVM);
            }

            var recordsList = new List<ErrorAndWarningVM>();
            string userName = CurrentUserId();

            var alertSumBO = new AlertSumBO()
            {
                RemittanceId = summaryVM.RemittanceId,
                L_USERID = userName
            };

            LogInfo($"ErrorWarning/WarningsListforBulkApproval, remittanceId: { DecryptUrlValue(alertSumBO.RemittanceId)}", true);

            //show error and worning on Remittance or paylocation level.
            string apiBaseUrlForErrorAndWarnings = GetApiUrl(_apiEndpoints.AlertDetailsPLNextSteps);    //## api/AlertDetailsForPayLocNextStep
            if (HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID) != null)
            {
                alertSumBO.L_PAYLOC_FILE_ID = Convert.ToInt32(HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID));                
                var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, alertSumBO);
                if (apiResult.NotEmpty()) { 
                    recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningVM>>(apiResult);
                }
            }
            else
            {
                //summaryVM.L_PAYLOC_FILE_ID = 0;

                var errorAndWarningTo = new ErrorAndWarningToShowListViewModel()
                {
                    remittanceID = Convert.ToDouble(DecryptUrlValue(summaryVM.RemittanceId)),
                    L_USERID = userName,
                    alertType = summaryVM.AlertType,
                };

                var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, errorAndWarningTo);
                if (apiResult.NotEmpty())
                {
                    recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningVM>>(apiResult);
                }
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

            string alertClass = (summaryVM.AlertType.Equals("W")) ? "Warning" : "Error";
            ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
            ViewBag.status = summaryVM.Status + "";
            ViewBag.UpdateStatus = TempData["UpdateStatus"];

            ContextSetValue(Constants.CurrentAlertDescription, $"{recordsList.First().ALERT_CLASS};{recordsList.First().ALERT_DESC}");
           
            return View(recordsList);


        }


        /// <summary>This will be used via ajax</summary>
        /// <param name="remittanceID"></param>
        /// <param name="alertType"></param>
        /// <returns></returns>
        public async Task<IActionResult> AlertListByAjax(string remittanceID, string alertType)
        {            
            var recordsList = new List<ErrorAndWarningVM>();
            string userName = CurrentUserId();

            var alertSumBO = new AlertSumBO()
            {
                RemittanceId = remittanceID,
                L_USERID = userName
            };

            //show error and worning on Remittance or paylocation level.
            string apiBaseUrlForErrorAndWarnings = GetApiUrl(_apiEndpoints.AlertDetailsPLNextSteps);    //## api/AlertDetailsForPayLocNextStep
            if (HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID) != null)
            {
                alertSumBO.L_PAYLOC_FILE_ID = Convert.ToInt32(HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID));                
                var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, alertSumBO);
                if (apiResult.NotEmpty())
                {
                    recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningVM>>(apiResult);
                }
            }
            else
            {

                var alertListQueryParams = new ErrorAndWarningToShowListViewModel()
                {
                    remittanceID = Convert.ToDouble(DecryptUrlValue(remittanceID)),
                    L_USERID = userName,
                    alertType = alertType,
                };

                var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, alertListQueryParams);
                if (apiResult.NotEmpty())
                {
                    recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningVM>>(apiResult);
                }
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
            //## Set the 'CurrentAlertDescription' in the Session- so that we can show it later in the Child pages, ie: 'ErrorWarning/MemberFolderMatching'
            ContextSetValue(Constants.CurrentAlertDescription, $"{recordsList.First().ALERT_CLASS};{recordsList.First().ALERT_DESC}");

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
            var apiResult = await ApiPost(apiApproveWarningsBulkList, paramList);
            if (apiResult.NotEmpty())
            {
                paramList = JsonConvert.DeserializeObject<ApproveWarningsInBulkVM>(apiResult);  //## we get the ApiCall result in the same object and see what status we have got in return
            }

            //## All done.. now send the user back to the 'Index' with success status
            TempData["msg"] = $"All warnings ({bulkApprovalRecordIdList.Count}) are successfully Acknowledged.";
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
                        if (!IsEmpty(result))
                        { 
                            errorAndWarningTo = JsonConvert.DeserializeObject<ErrorAndWarningApprovalOB>(result);                        
                        }

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
            if (apiResult.NotEmpty())
            { 
                errorAndWarningTo = JsonConvert.DeserializeObject<ErrorAndWarningApprovalOB>(apiResult);            
            }
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
                List<ErrorAndWarningVM> recordsList = new();
                ErrorAndWarningVM records = new();

                int.TryParse(DecryptUrlValue(id, forceDecode: false), out int dataRowID);
                //show employer name
                //ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
                string userName = CurrentUserId();

                string apiBaseUrlForErrorAndWarningsApproval = GetApiUrl(_apiEndpoints.GetAlertDetailsInfo);  //## api/GetIndvidualRecords

                string apiBaseUrlForErrorAndWarningsList = GetApiUrl(_apiEndpoints.GetErrorWarningList);    //## api/UpdateIndvidualRecordErrorWarningList
                string apiResponse = String.Empty;

                QueryParamVM helpTextBO = new QueryParamVM();
                helpTextBO.L_USERID = userName;
                helpTextBO.L_DATAROWID_RECD = dataRowID;

                apiResponse = await ApiPost(apiBaseUrlForErrorAndWarningsList, helpTextBO);
                if (!IsEmpty(apiResponse))
                {
                    recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningVM>>(apiResponse);
                }

                foreach (var item in recordsList)
                {
                    item.EncryptedRowRecordID = EncryptUrlValue(item.MC_ALERT_ID);   //## Encrypt them to be used in QueryString
                }

                ViewBag.HelpText = recordsList;
                string result = string.Empty;

                MemberUpdateRecordBO memberUpdateRecordBO = new();
                apiResponse = await ApiPost(apiBaseUrlForErrorAndWarningsApproval, helpTextBO);
                if (!IsEmpty(apiResponse)){ 
                    memberUpdateRecordBO = JsonConvert.DeserializeObject<MemberUpdateRecordBO>(apiResponse);
                
                }

                memberUpdateRecordBO.dataRowID = dataRowID;
                memberUpdateRecordBO.ErrorAndWarningList = recordsList;
                memberUpdateRecordBO.employerName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
                memberUpdateRecordBO.DataRowEncryptedId = id;  //## we are sending the Encrypted Id back to the UI- to allow 'Switch View'

                memberUpdateRecordBO.AlertDescription = ContextGetValue(Constants.CurrentAlertDescription);

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
            if (!IsEmpty(apiResponse)){
                updateRecordBO = JsonConvert.DeserializeObject<MemberUpdateRecordBO> (apiResponse);

            }

            TempData["UpdateStatus"] = $"Success: The record is successfully updated. Member: {updateRecordBO.forenames} {updateRecordBO.lastName}, NI: {updateRecordBO.NI}";            

            var statusCode = updateRecordBO.statusCode.GetValueOrDefault(-99);

            if (statusCode != 0) {
                TempData["UpdateStatus"] = "Error: " + updateRecordBO.statusTxt + ". Please try again later.";
            }

            return RedirectToAction("WarningsListforBulkApproval");

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
            int.TryParse(DecryptUrlValue(id), out int dataRowID);

            GetMatchesViewModel matchingPageWrapperVM = new GetMatchesViewModel() { 
                EmployersName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName),
                CurrentAlertDescription = ContextGetValue(Constants.CurrentAlertDescription),         
            };
            
            QueryParamVM memberRecordQuery = new QueryParamVM
            {
                L_DATAROWID_RECD = dataRowID,
                L_USERID = CurrentUserId()
            };
            

            string apiBaseUrlForUpdateRecordGetValues = GetApiUrl(_apiEndpoints.GetAlertDetailsInfo);
            MemberUpdateRecordBO memberRecord = new();
            //## show Member record- from the 'Contribution data received'
            string apiResponse = await ApiPost(apiBaseUrlForUpdateRecordGetValues, memberRecordQuery);
            if (!IsEmpty(apiResponse))
            {
                memberRecord = JsonConvert.DeserializeObject<MemberUpdateRecordBO>(apiResponse);

            }

            //To cal all values/data of member record. that is already in UPM.
            MatchingRecordQueryVM getMatchesBO = new ()
            {
                userId = CurrentUserId(),
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
            if (IsEmpty(apiResponse)){
                return null;
            }

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

            if (action == "NEWREC")
            {
                selectedFolder = matchingList.FirstOrDefault(m => m.personId == Convert.ToInt32(personFolderId) && m.folderRef == "NEWREC");
            }
            else if (action == "UpdateFolder")
            {
                selectedFolder = matchingList.FirstOrDefault(m => m.folderId == personFolderId);
            }

            //## Now create a 'MatchingPersonVM' to update in the DB with the values            
            var member = selectedFolder;
            member.userId = CurrentUserId();

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
                TempData["UpdateStatus"] = "Error: Failed to update record";
            }
            else {
                //## what if the user selected "AddNewPersonAndFolder"..? then there will be no Member name to display...
                if (selectedAction.Equals("AddNewPersonAndFolder")){
                    TempData["UpdateStatus"] = $"Success: A new Folder record is created for that selected person.";
                }
                else {
                    if (action == "NEWREC"){
                        TempData["UpdateStatus"] = $"Success: A new record is created for that selected person";
                    }
                    else {
                        TempData["UpdateStatus"] = $"Success: The record is successfully updated. Member: {member.upperForeNames} {member.upperSurName}, NI: {member.NINO}";
                    }
                }

            }
           
            return RedirectToAction("WarningsListforBulkApproval");
            //return View("UpdateStatus", updateStatus);
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


        public async Task<IActionResult> RemittanceAlertsDownloadAll(string id)
        {
            if (IsEmpty(id) )
                return RedirectToAction("Index");

            _ = int.TryParse(DecryptUrlValue(id, forceDecode: false), out int remittanceId);            
            
            var apiParamList = new AlertSumBO()
            {
                RemittanceId = remittanceId.ToString(),
                AlertType = "ALL",
                L_USERID = CurrentUserId(),
                L_PAYLOC_FILE_ID = 0,
                ShowAlertsNotCleared = true
            };

            string alertDetailsApriUrl = GetApiUrl(_apiEndpoints.AlertDetailsPLNextSteps);   //## api/AlertDetailsForPayLocNextStep                       
            //var recordsList = await callApi.GetErrorAndWarningList(apiParamList, alertDetailsByRemittanceId);
            var recordsList = await ApiPost(alertDetailsApriUrl, apiParamList);
            var alertlist = JsonConvert.DeserializeObject<List<ErrorAndWarningVM>>(recordsList);

            var cc = new CsvConfiguration(new System.Globalization.CultureInfo("en-US"));
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream: ms, encoding: new UTF8Encoding(true)))
                {
                    using (var cw = new CsvWriter(sw, cc))
                    {
                        cw.WriteRecords(alertlist);
                    }// The stream gets flushed here.
                    return File(ms.ToArray(), "text/csv", $"{CurrentUserId()}-{remittanceId}-{DateTime.UtcNow.Ticks}.csv");
                }
            }

        }
    }
}
