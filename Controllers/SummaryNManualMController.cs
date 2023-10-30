using MCPhase3.CodeRepository;
using MCPhase3.CodeRepository.RefectorUpdateRecord;
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



    public class SummaryNManualMController : BaseController
    {
        private readonly ILogger<SummaryNManualMController> _logger;
        private readonly IWebHostEnvironment _host;
        private readonly IConfiguration _Configure;
        ErrorAndWarningViewModelWithRecords _errorAndWarningViewModel = new ErrorAndWarningViewModelWithRecords();
        List<ErrorAndWarningViewModelWithRecords> model = new List<ErrorAndWarningViewModelWithRecords>();

        //following class I am using to consume api's
        EventDetailsBO eventDetails = new EventDetailsBO();

        public SummaryNManualMController(ILogger<SummaryNManualMController> logger, IWebHostEnvironment host, IConfiguration configuration, IRedisCache cache, IDataProtectionProvider Provider, IOptions<ApiEndpoints> ApiEndpoints) : base(configuration, cache, Provider, ApiEndpoints)
        {
            _Configure = configuration;
            _host = host;
            _logger = logger;
        }


        /// <summary>
        /// Error and warnings summary is displayed in Index 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index(AlertSumBO alertSumBO, int? pageNumber)
        {
            //## coming here for the first time- save in the Redis cache- and be happy .. now u can always find this record in the Cache
            if (alertSumBO.remittanceId != null)
            {
                _cache.SetString(RemittanceIdKeyName(), alertSumBO.remittanceId);
            }
            else
            {
                alertSumBO.remittanceId = GetRemittanceId();
            }

            //When select to work with error and warnings on payloction level then I have to keep PaylocFile in sesssion so 
            //when come back to this page after 1st process/during process I can get that from session.
            if (alertSumBO.L_PAYLOC_FILE_ID != null)
            {
                HttpContext.Session.SetString(Constants.SessionKeyPaylocFileID, alertSumBO.L_PAYLOC_FILE_ID.ToString());
                ViewData["paylocID"] = alertSumBO.L_PAYLOC_FILE_ID;
            }

            if (HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID) != null)
            {
                alertSumBO.L_PAYLOC_FILE_ID = Convert.ToInt32(HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID));
            }

            alertSumBO.L_USERID = CurrentUserId();
            var model = new List<ErrorAndWarningViewModelWithRecords>();

            int remID = Convert.ToInt32(DecryptUrlValue(alertSumBO.remittanceId));

            ViewBag.remID = EncryptUrlValue(remID.ToString());

            ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);

            //add remittance id into session for future use.

            //work on remittance level if null else Paylocation level
            string apiBaseUrlForErrorAndWarnings = GetApiUrl(_apiEndpoints.ErrorAndWarnings);   //## api: /AlertSummaryRecordsPayLoc

            alertSumBO.remittanceId = remID.ToString();
            var apiResult = await ApiPost(apiBaseUrlForErrorAndWarnings, alertSumBO);
            model = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(apiResult);

            if (model != null && model.Count > 0)
            {

                var newModel1 = model.Where(x => x.ACTION_BY.Equals("ALL")).ToList();

                foreach (var item in newModel1)
                {
                    item.EncryptedRowRecordID = alertSumBO.remittanceId;
                }
            }

            var newModel = model.AsQueryable();
            newModel = newModel.OrderByDescending(x => x.remittanceID);

            int pageSize = Convert.ToInt32(ConfigGetValue("PaginationSize"));
            ViewBag.MaximumPaginationItemsPerPage = pageSize;       //## to show/hide the pagination control            
            return View(PaginatedList<ErrorAndWarningViewModelWithRecords>.CreateAsync(newModel, pageNumber ?? 1, pageSize));
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
                TempData["Msg1"] = "No Alert data found to acknowledge. Please try again.";
                return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM");
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

            //## All done.. now send the user back to the 'WarningsListforBulkApproval' with success status
            return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM");
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
                return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM");
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

            return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM");
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

                HelpTextBO helpTextBO = new HelpTextBO();
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
        public async Task<IActionResult> UpdateRecordToDataBase(MemberUpdateRecordBO updateRecordBO)
        {
            updateRecordBO.modUser = HttpContext.Session.GetString(Constants.SessionKeyUserID);

            string remittanceID_Encrypted = GetRemittanceId();

            string apiLink = GetApiUrl(_apiEndpoints.UpdateRecord);

            string apiResponse = await ApiPost(apiLink, updateRecordBO);

            return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM", remittanceID_Encrypted);
        }


        /// <summary>
        /// This is new action which will handle all the loose matching and 
        /// new folder create or new starter process.
        /// GETs file record and shows matches from UPM and updates UPM by selected record.
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> NewMatchingCritera(string id)
        {
            GetMatchesBO getMatchesBO = new GetMatchesBO();
            GetMatchesViewModel listOfMatches = new GetMatchesViewModel();
            HelpTextBO helpTextBO = new HelpTextBO();

            int.TryParse(DecryptUrlValue(id), out int dataRowID);

            //show employer name
            ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
            //shows error and warnings data
            string apiBaseUrlForUpdateRecordGetValues = GetApiUrl(_apiEndpoints.UpdateRecordGetValues);
            //shows recent data that is inserted by using posting file 
            string apiBaseUrlForMatchingRecordsManual = GetApiUrl(_apiEndpoints.MatchingRecordsManual);
            //shows members matched data that was already inside UPM for matching
            string apiBaseUrlForErrorAndWarningsList = GetApiUrl(_apiEndpoints.UpdateRecordGetErrorWarningList);

            helpTextBO.L_DATAROWID_RECD = dataRowID;
            helpTextBO.L_USERID = HttpContext.Session.GetString(Constants.SessionKeyUserID);
            string result = string.Empty;

            string apiResponse = await ApiPost(apiBaseUrlForUpdateRecordGetValues, helpTextBO);
            MemberUpdateRecordBO memberUpdateRecordBO = JsonConvert.DeserializeObject<MemberUpdateRecordBO>(apiResponse);

            //To cal all values/data of member record. that is already in UPM.
            getMatchesBO.userId = HttpContext.Session.GetString(Constants.SessionKeyUserID);
            getMatchesBO.dataRowId = dataRowID;
            //method to get matching records from UPM
            listOfMatches.Matches = await GetRecords(getMatchesBO);

            if (listOfMatches.Matches.Count >= 1)
            {
                memberUpdateRecordBO.dataRowID = dataRowID;
                ViewBag.fileData = memberUpdateRecordBO;

                foreach (var item in listOfMatches.Matches)
                {
                    item.dataRowId = dataRowID;
                }

                listOfMatches.DataRowEncryptedId = id;  //## we are sending the Encrypted Id back to the UI- to allow 'Switch View'
            }

            return View(listOfMatches);

        }


        /// <summary>
        /// only datarowid and userid need to call api.
        /// </summary>
        /// <param name="getMatchesBO"></param>
        /// <returns></returns>
        private async Task<List<GetMatchesBO>> GetRecords(GetMatchesBO getMatchesBO)
        {
            string apiLink = GetApiUrl(_apiEndpoints.MatchingRecordsUPM);
            string apiResponse = await ApiPost(apiLink, getMatchesBO);
            List<GetMatchesBO> bO = JsonConvert.DeserializeObject<List<GetMatchesBO>>(apiResponse);
            return bO;
        }

        /// <summary>
        /// following method will update record in UPM
        /// </summary>
        /// <param name="getMatchesBO"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUsingManualMatch(GetMatchesViewModel getMatchesBO)
        {
            UpdateRecordModel updateRecordModel = new UpdateRecordModel();
            string remittanceID = GetRemittanceId(returnAsEncrypted: false);

            int remID = Convert.ToInt32(remittanceID);

            UpdateFolder obj1 = new UpdateFolder();
            AddNewFolder obj2 = new AddNewFolder();
            AddNewPersonAndFolder obj3 = new AddNewPersonAndFolder();


            GetMatchesBO bO = new GetMatchesBO();
            //  var classToCall = getMatchesBO.activeProcess;
            try
            {
                foreach (var item in getMatchesBO.Matches)
                {
                    bO.dataRowId = item.dataRowId;
                    bO.isActive = getMatchesBO.ActiveProcess;
                    bO.folderId = item.folderId;
                    bO.personId = item.personId;
                    bO.folderRef = item.folderRef;
                    string s1 = bO.isActive?.Substring(0, 12);
                    string s2 = bO.isActive?.Remove(0, 12);

                    //string s2 = bO.;
                    if (s1 == "UpdateFolder" && s2 == bO.folderId)
                    {
                        updateRecordModel = GetUpdatedUPMRecord(obj1);
                        break;
                    }
                    //else if (s1 == "AddNewFolder" && s2 == bO.personId.ToString())
                    else if (bO.folderRef == "NEWREC" && !string.IsNullOrEmpty(bO.personId.ToString()))
                    {
                        bO.folderId = null;
                        bO.personId = item.personId;
                        updateRecordModel = GetUpdatedUPMRecord(obj2);
                        break;

                    }
                    else if (bO.isActive.Equals("AddNewPersonAndFolder"))
                    {
                        bO.folderId = null;
                        bO.personId = null;
                        updateRecordModel = GetUpdatedUPMRecord(obj3);
                        break;
                    }
                }
            }
            catch (System.NullReferenceException ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                TempData["error"] = "Select a matching record by clicking on radio button and submit record.";
                return RedirectToAction("NewMatchingCritera", "SummaryNManualM", new { id = bO.dataRowId });
            }

            bO.personMatch = updateRecordModel.personMatch;
            bO.folderMatch = updateRecordModel.folderMatch;
            bO.userId = CurrentUserId();
            bO.note = getMatchesBO.Note;

            string matchingRecordsManualApi = GetApiUrl(_apiEndpoints.MatchingRecordsManual);
            string apiResponse = await ApiPost(matchingRecordsManualApi, bO);
            TempData["msg"] = "Record updated successfully";
            return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM", GetRemittanceId());
        }


        /// <summary>
        /// Return a specific record that user selected on Loose match page.
        /// </summary>
        /// <param name="getMatchesBO"></param>
        /// <returns></returns>
        private static UpdateRecordModel GetUpdatedUPMRecord(IUpdateRecord updateRecord)
        {
            return updateRecord.UpdateRecord();
        }


        private string ConverToString(MatchCollection alertid)
        {
            string result = string.Empty;
            foreach (var id in alertid)
            {
                result += id;
            }

            return result;
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
