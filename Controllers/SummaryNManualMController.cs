using MCPhase3.CodeRepository;
using MCPhase3.CodeRepository.RefectorUpdateRecord;
using MCPhase3.Common;
using MCPhase3.Models;
using MCPhase3.Services;
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
        private readonly IApiService _apiCallService;
        ErrorAndWarningViewModelWithRecords _errorAndWarningViewModel = new ErrorAndWarningViewModelWithRecords();
        List<ErrorAndWarningViewModelWithRecords> model = new List<ErrorAndWarningViewModelWithRecords>();

        //following class I am using to consume api's
        TotalRecordsInsertedAPICall apiClient = new TotalRecordsInsertedAPICall();
        var eventDetails = new EventDetailsBO();
        EventsTableUpdates eventUpdate;


        public SummaryNManualMController(ILogger<SummaryNManualMController> logger, IWebHostEnvironment host, IConfiguration configuration, IRedisCache cache, IDataProtectionProvider Provider, IApiService ApiService, IOptions<ApiEndpoints> ApiEndpoints) : base(configuration, cache, Provider, ApiEndpoints)
        {
            _Configure = configuration;
            _apiCallService = ApiService;
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

            //inc will keep check manual matching stage and it will not be successfully untill 
            //Process successfully completed. 

            try
            {

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

                //List<ErrorAndWarningViewModelWithRecords> model = new List<ErrorAndWarningViewModelWithRecords>();
                var model = new List<ErrorAndWarningViewModelWithRecords>();

                string apiBaseUrlForInsertEventDetails = GetApiUrl(_apiEndpoints.InsertEventDetails);


                //pass remittance id to next action
                string encryptedRemID = alertSumBO.remittanceId;    //## this is coming in Decrypted format.. good boy!
                var remitIDStr = DecryptUrlValue(encryptedRemID);
                int remID = Convert.ToInt32(DecryptUrlValue(encryptedRemID));

                ViewBag.remID = EncryptUrlValue(remID.ToString());

                ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);

                //add remittance id into session for future use.


                //var keyValuePairs = new Dictionary<string, string>();
                //work on remittance level if null else Paylocation level
                string apiBaseUrlForErrorAndWarnings = GetApiUrl(_apiEndpoints.ErrorAndWarnings);   //## api: /AlertSummaryRecordsPayLoc

                alertSumBO.remittanceId = remitIDStr;
                model = await apiClient.GetErrorAndWarningSummary(alertSumBO, apiBaseUrlForErrorAndWarnings);
                var newModel1 = model.Where(x => x.ACTION_BY.Equals("ALL")).ToList();

                if (newModel1.Count < 1)
                {
                    return RedirectToAction("Home", "Admin");
                }

                foreach (var item in newModel1)
                {
                    item.EncryptedRowRecordID = encryptedRemID;
                }

                var newModel = model.AsQueryable();
                newModel = newModel.OrderByDescending(x => x.remittanceID);


                int pageSize = Convert.ToInt32(ConfigGetValue("PaginationSize"));
                ViewBag.MaximumPaginationItemsPerPage = pageSize;       //## to show/hide the pagination control
                // vLSContext = vLSContext.OrderByDescending(x => x.CreatedDate);
                return View(PaginatedList<ErrorAndWarningViewModelWithRecords>.CreateAsync(newModel, pageNumber ?? 1, pageSize));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                TempData["MsgError"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }
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
            try
            {
                string errorWarningSummaryKeyName = $"{CurrentUserId()}_{Constants.ErrorWarningSummaryKeyName}";
                //following functionality is added to keep user on same warning and error page until all sorted.
                //## if we come back here from Acknowledgement page- then the ViewModel 'summaryVM' is empty- so need to read from the Cache.. save life..

                if (summaryVM.ALERT_COUNT is null) //## means empty... came here from another page not Dashboard
                {
                    //## if Alert.count is NULL then its an Empty, but not null// (pain in the back)
                    summaryVM = _cache.Get<ErrorAndWarningViewModelWithRecords>(errorWarningSummaryKeyName);
                    
                    if(summaryVM is null){
                        return RedirectToAction("Index", "Admin");  //## should not happen
                    }
                }
                else
                {
                    _cache.Set(errorWarningSummaryKeyName, summaryVM);
                }


                var recordsList = new List<ErrorAndWarningViewModelWithRecords>();
                var records = new ErrorAndWarningViewModelWithRecords();
                
                string userName = CurrentUserId();
               
                AlertSumBO alertSumBO = new AlertSumBO()
                {
                    remittanceId = summaryVM.remittanceID,
                    L_USERID = userName
                };

                //show Employer name 
                ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
                //show error and worning on Remittance or paylocation level.
                string apiBaseUrlForErrorAndWarnings = GetApiUrl(_apiEndpoints.AlertDetailsPLNextSteps);
                if (HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID) != null)
                {
                    alertSumBO.L_PAYLOC_FILE_ID = Convert.ToInt32(HttpContext.Session.GetString(Constants.SessionKeyPaylocFileID));
                    recordsList = await apiClient.GetErrorAndWarningSummary(alertSumBO, apiBaseUrlForErrorAndWarnings);
                }
                else
                {
                    summaryVM.L_PAYLOC_FILE_ID = 0;

                    var errorAndWarningTo = new ErrorAndWarningToShowListViewModel() { 
                        remittanceID = Convert.ToDouble(DecryptUrlValue(summaryVM.remittanceID)),
                        L_USERID = userName,
                        alertType = summaryVM.ALERT_TYPE_REF,                    
                    };

                    using HttpClient client = new HttpClient();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(errorAndWarningTo), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrlForErrorAndWarnings;

                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            _logger.LogInformation($"BulkApproval API Call successfull. StringContent: {content}");
                            //call following api to get this uploaded remittance id of file.
                            string result = await Response.Content.ReadAsStringAsync();
                            recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(result);     //TODO: cache this result .. and next time cheeck in Redis for this Object, if not found- then only call this API..

                            if (recordsList.Count < 1)
                            {
                                return View(Constants.Error403_Page);
                            }
                        }
                    }
                }

                foreach (var record in recordsList)
                {
                    record.EncryptedRowRecordID = EncryptUrlValue(record.DATAROWID_RECD);
                    record.EncryptedAlertid = EncryptUrlValue(record.MC_ALERT_ID);

                }
                // model = ChangeAPIDataToReadAble(result);
                if (summaryVM.ALERT_CLASS.Equals("W"))
                {
                    ViewBag.alertClass = "Warning";
                }
                else if (summaryVM.ALERT_CLASS.Equals("E"))
                {
                    ViewBag.alertClass = "Error";
                }

                ViewBag.status = summaryVM.ALERT_DESC + "";
                //ViewBag.total = errorModel.COUNT == 0 ? model.Count : errorModel.COUNT;
                ViewBag.total = summaryVM.ALERT_COUNT.Replace(".0", "");

                return View(recordsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }

        }

        public async Task<IActionResult> GoToSummaryPage()
        {
            //var remitID = HttpContext.Session.GetString(SessionKeyRemittanceID);
            //AlertSumBO alertSumBO = new AlertSumBO() { remittanceId = EncryptUrlValue(remitID)};

            return RedirectToAction("Index", new AlertSumBO());


        }

        /// <summary>
        /// BulkApprove all records and update in database.
        /// this action is used for both bulck approves and to approve 
        /// warnings on update record page.
        /// </summary>
        /// <returns></returns>
        //[HttpPost]      
        public async Task<IActionResult> WarningApproval(Dictionary<string, string> alertId, string id)
         {
            //## do we really need this following line????
            //if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
            //{
            //    RedirectToAction("Index", "Login");
            //}


            try
            {
                int decryptedID = 0;
                if (string.IsNullOrEmpty(id) == false) {
                    _ = int.TryParse(DecryptUrlValue(id, forceDecode: false), out decryptedID);
                }

                string result = string.Empty;
                string apiBaseUrlForErrorAndWarningsApproval = GetApiUrl(_apiEndpoints.ErrorAndWarningsApproval); 
                string apiBaseUrlForInsertEventDetails = GetApiUrl(_apiEndpoints.InsertEventDetails);

                ErrorAndWarningApprovalOB errorAndWarningTo = new ErrorAndWarningApprovalOB();
                errorAndWarningTo.userID = CurrentUserId();
                int remittanceID = Convert.ToInt16(GetRemittanceId(returnEncryptedIdOnly: false));

                //int remittanceID = (int)HttpContext.Session.GetInt32(SessionKeyRemittanceID);
                //if we call this action from update record page then alertId will be null so I assign id that comes from update record.
                if (alertId.Count == 0)
                {
                    alertId.Add("id", decryptedID.ToString());
                }


                using (HttpClient client = new HttpClient())
                {

                    string endpoint = apiBaseUrlForErrorAndWarningsApproval;

                    foreach (var ids in alertId.Values)
                    {                                                
                        string decryptedAlertId = DecryptUrlValue(ids, forceDecode: false);
                        errorAndWarningTo.alertID = Convert.ToInt32( decryptedAlertId);

                        StringContent content = new StringContent(JsonConvert.SerializeObject(errorAndWarningTo), Encoding.UTF8, "application/json");

                        using var Response = await client.PostAsync(endpoint, content);
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                            _logger.LogInformation($"BulkApproval API Call successfull-> {endpoint}");
                            //call following api to get this uploaded remittance id of file.
                            result = await Response.Content.ReadAsStringAsync();
                            errorAndWarningTo = JsonConvert.DeserializeObject<ErrorAndWarningApprovalOB>(result);

                            TempData["msg"] = errorAndWarningTo.returnStatusTxt;

                            if (errorAndWarningTo.returnStatusTxt.Contains("not found"))
                            {
                                Console.WriteLine(errorAndWarningTo.returnStatusTxt);
                                //TODO-> need to inform the user about the failure
                            }
                            else {
                                //## Update the 'ErrorAndWarningSummaryVM'- in the cache.. now the Count will be one less... for the current Record Id.. 1 fault is 'Cleared'
                                //var cachedModel = _cache.Get<ErrorAndWarningViewModelWithRecords>(CurrentUserId() + Constants.ErrorWarningSummaryKeyName);
                                //if (cachedModel != null) { 
                                    
                                //}
                            }
                        }
                    }
                }
                
                return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM", new { remittanceID = id });
                //return RedirectToAction("Index", remittanceID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }
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
                List<ErrorAndWarningViewModelWithRecords> recordsList = new List<ErrorAndWarningViewModelWithRecords>();
                ErrorAndWarningViewModelWithRecords records = new ErrorAndWarningViewModelWithRecords();

                int.TryParse(DecryptUrlValue(id, forceDecode: false), out int dataRowID);

                IEnumerable<HelpForEAndAUpdateRecord> helpText = null;
                //show employer name
                ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
                string userName = CurrentUserId();
                MemberUpdateRecordBO memberUpdateRecordBO = new MemberUpdateRecordBO();

                string apiBaseUrlForErrorAndWarningsApproval = GetApiUrl(_apiEndpoints.UpdateRecordGetValues);

                string apiBaseUrlForErrorAndWarningsList = GetApiUrl(_apiEndpoints.UpdateRecordGetErrorWarningList);
                string apiResponse = String.Empty;

                HelpTextBO helpTextBO = new HelpTextBO();
                helpTextBO.L_USERID = userName;
                helpTextBO.L_DATAROWID_RECD = dataRowID;

                using (HttpClient client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(helpTextBO), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrlForErrorAndWarningsList;

                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                            _logger.LogInformation("BulkApproval API Call successfull");
                            //call following api to get this uploaded remittance id of file.
                            apiResponse = await Response.Content.ReadAsStringAsync();
                            recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(apiResponse);


                            if (recordsList.Count < 1)
                            {
                                return View(Constants.Error403_Page);
                            }

                            // TempData["msg"] = errorAndWarningTo.returnStatusTxt;

                        }
                    }
                }

                foreach (var item in recordsList)
                {
                    item.EncryptedRowRecordID = EncryptUrlValue(item.MC_ALERT_ID);   //## Encrypt them to be used in QueryString
                }

                ViewBag.HelpText = recordsList;
                string result = string.Empty;
                using (HttpClient client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(helpTextBO), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrlForErrorAndWarningsApproval;

                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                            _logger.LogInformation("BulkApproval API Call successfull");
                            //call following api to get this uploaded remittance id of file.
                            result = await Response.Content.ReadAsStringAsync();
                            memberUpdateRecordBO = JsonConvert.DeserializeObject<MemberUpdateRecordBO>(result);

                        }
                    }
                }
                memberUpdateRecordBO.dataRowID = dataRowID;
                //ViewBag.remittanceID = remID;

                return View(memberUpdateRecordBO);
                //return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }
        }
        /// <summary>
        /// follown action will update new changes into database 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> UpdateRecordToDataBase(MemberUpdateRecordBO updateRecordBO)
        {
            try
            {
                updateRecordBO.modUser = HttpContext.Session.GetString(Constants.SessionKeyUserID);

                string remittanceID = GetRemittanceId(returnEncryptedIdOnly: false);

                int remID = Convert.ToInt32(remittanceID);

                string apiLink = GetApiUrl(_apiEndpoints.UpdateRecord);
                using (HttpClient client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(updateRecordBO), Encoding.UTF8, "application/json");
                    string endPoint = apiLink;

                    using (var response = await client.PostAsync(apiLink, content))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                            _logger.LogInformation("Record updated successfully Call successfull");
                            TempData["msg"] = "Record updated successfully";
                            //call following api to get this uploaded remittance id of file.
                            var result = await response.Content.ReadAsStringAsync();

                        }
                    }
                }
                string rem = EncryptUrlValue(remID.ToString());

                return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM", rem);
                // return RedirectToAction("Index", "SummaryNManualM", rem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }
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

            try
            {
                // IEnumerable<HelpForEAndAUpdateRecord> helpText = null;
                //show employer name
                ViewBag.empName = HttpContext.Session.GetString(Constants.SessionKeyEmployerName);
                MemberUpdateRecordBO memberUpdateRecordBO = new MemberUpdateRecordBO();
                //shows error and warnings data
                string apiBaseUrlForUpdateRecordGetValues = GetApiUrl(_apiEndpoints.UpdateRecordGetValues);
                //shows recent data that is inserted by using posting file 
                string apiBaseUrlForMatchingRecordsManual = GetApiUrl(_apiEndpoints.MatchingRecordsManual);
                //shows members matched data that was already inside UPM for matching
                string apiBaseUrlForErrorAndWarningsList = GetApiUrl(_apiEndpoints.UpdateRecordGetErrorWarningList);

                helpTextBO.L_DATAROWID_RECD = dataRowID;
                helpTextBO.L_USERID = HttpContext.Session.GetString(Constants.SessionKeyUserID);
                string result = string.Empty;
                using (HttpClient client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(helpTextBO), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrlForUpdateRecordGetValues;

                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                            _logger.LogInformation("BulkApproval API Call successfull");
                            //call following api to get this uploaded remittance id of file.
                            result = await Response.Content.ReadAsStringAsync();
                            memberUpdateRecordBO = JsonConvert.DeserializeObject<MemberUpdateRecordBO>(result);

                            // TempData["msg"] = errorAndWarningTo.returnStatusTxt;

                        }
                    }
                }

                //To cal all values/data of member record. that is already in UPM.
                getMatchesBO.userId = HttpContext.Session.GetString(Constants.SessionKeyUserID);
                //int remID = (int)HttpContext.Session.GetInt32(SessionKeyRemittanceID);
                getMatchesBO.dataRowId = dataRowID;
                //method to get matching records from UPM
                listOfMatches.Matches = await GetRecords(getMatchesBO);

                if (listOfMatches.Matches.Count < 1)
                {
                    return View(Constants.Error403_Page);
                }

                memberUpdateRecordBO.dataRowID = dataRowID;
                ViewBag.fileData = memberUpdateRecordBO;

                foreach (var item in listOfMatches.Matches)
                {
                    item.dataRowId = dataRowID;
                }

                return View(listOfMatches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                TempData["Msg1"] = "System is showing error, please try again later";
                return View(Constants.Error403_Page);
            }
        }
        /// <summary>
        /// only datarowid and userid need to call api.
        /// </summary>
        /// <param name="getMatchesBO"></param>
        /// <returns></returns>
        private async Task<List<GetMatchesBO>> GetRecords(GetMatchesBO getMatchesBO)
        {
            List<GetMatchesBO> bO = new List<GetMatchesBO>();
            string apiLink = GetApiUrl(_apiEndpoints.MatchingRecordsUPM);

            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(getMatchesBO), Encoding.UTF8, "application/json");
                string endPoint = apiLink;

                using (var response = await client.PostAsync(apiLink, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                        _logger.LogInformation("Record updated successfully Call successfull");
                        //TempData["msg"] = "Record updated successfully";
                        //call following api to get this uploaded remittance id of file.
                        var result = await response.Content.ReadAsStringAsync();
                        bO = JsonConvert.DeserializeObject<List<GetMatchesBO>>(result);
                    }
                }
            }
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

            //int remID = (int)HttpContext.Session.GetInt32(SessionKeyRemittanceID);
            string remittanceID = GetRemittanceId(returnEncryptedIdOnly: false);
            // DecryptUrlValue(_cache.GetString($"{CurrentUserId()}_{Constants.SessionKeyRemittanceID}"));

            int remID = Convert.ToInt32(remittanceID);

            string apiLink = GetApiUrl(_apiEndpoints.MatchingRecordsManual);
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
                    bO.isActive = getMatchesBO.activeProcess;
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
            bO.note = getMatchesBO.note;
            try
            {


                using (HttpClient client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(bO), Encoding.UTF8, "application/json");
                    string endPoint = apiLink;

                    using (var response = await client.PostAsync(apiLink, content))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                            _logger.LogInformation("Record updated successfully Call successfull");
                            TempData["msg"] = "Record updated successfully";
                            //call following api to get this uploaded remittance id of file.
                            var result = await response.Content.ReadAsStringAsync();
                        }
                    }
                }

                //## So far all good- now we can update the Score and avoid clicking 'CheckReturn' button
                //https://localhost:57132/Admin/SubmitReturn?P_PAYLOC_FILE_ID=0&P_STATUSCODE=80&p_REMITTANCE_ID=123
                var remitInfo = new ReturnSubmitBO()
                {
                    p_REMITTANCE_ID = remittanceID,
                    P_USERID = ContextGetValue(Constants.SessionKeyUserID)
                    //rBO.p_REMITTANCE_ID = DecryptUrlValue(rBO.p_REMITTANCE_ID);
                };
                //TODO: we need to add the current Status value (ie: 80, 90) to update and get new status code
                //var apiResult = await _apiCallService.UpdateScore(remitInfo);

                return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM", remID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex.StackTrace);
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }
        }


        /// <summary>
        /// Return a specific record that user selected on Loose match page.
        /// </summary>
        /// <param name="getMatchesBO"></param>
        /// <returns></returns>
        private static UpdateRecordModel GetUpdatedUPMRecord(IUpdateRecord updateRecord)
        {
            // GetMatchesBO.isActive myClass = new bO.isActive();
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
                int remID = Convert.ToInt16(GetRemittanceId(returnEncryptedIdOnly: false));

                bo.P_DATAROWID_RECD = dataRowID;
                bo.P_USERID = CurrentUserId(); ;

                using (HttpClient client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(bo), Encoding.UTF8, "application/json");
                    
                    string apiBaseUrlForRecordReset = GetApiUrl(_apiEndpoints.RecordReset);

                    using (var Response = await client.PostAsync(apiBaseUrlForRecordReset, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                            _logger.LogInformation("RecordReset API Call successfull");
                            //call following api to get this uploaded remittance id of file.
                            string result = await Response.Content.ReadAsStringAsync();
                            result = JsonConvert.DeserializeObject<string>(result);
                            //errorAndWarningTo = JsonConvert.DeserializeObject<ErrorAndWarningToShowListViewModel>(result);
                        }
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
