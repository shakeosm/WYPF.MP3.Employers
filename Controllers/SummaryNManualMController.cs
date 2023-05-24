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
        //private readonly IRedisCache _cache;
        public const string SessionKeyPaylocFileID = "_PaylocFileID";
        public const string SessionKeyClientId = "_clientId";

        string SessionKeyRemittanceID = "_remittanceID";
        public const string SessionKeyEmployerName = "_employerName";
        public const string SessionKeyUserID = "_UserName";
        ErrorAndWarningViewModelWithRecords _errorAndWarningViewModel = new ErrorAndWarningViewModelWithRecords();
        List<ErrorAndWarningViewModelWithRecords> model = new List<ErrorAndWarningViewModelWithRecords>();

        //following class I am using to consume api's
        TotalRecordsInsertedAPICall callApi = new TotalRecordsInsertedAPICall();
        EventDetailsBO eBO = new EventDetailsBO();
        EventsTableUpdates eventUpdate;


        public SummaryNManualMController(ILogger<SummaryNManualMController> logger, IWebHostEnvironment host, IConfiguration configuration, IRedisCache cache, IDataProtectionProvider Provider) : base(configuration, cache, Provider)
        {
            _Configure = configuration;
            //this._cache = cache;
            _host = host;
            _logger = logger;
            //CustomDataProtection = customIDataProtection;
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
                _cache.SetString($"{CurrentUserId()}_{SessionKeyRemittanceID}", alertSumBO.remittanceId);
            }
            else
            {
                alertSumBO.remittanceId = GetRemittanceId();    //## but why are you null?! how can that be?
            }

            //inc will keep check manual matching stage and it will not be successfully untill 
            //Process successfully completed. 

            string apiBaseUrlForErrorAndWarnings = string.Empty;

            try
            {

                //When select to work with error and warnings on payloction level then I have to keep PaylocFile in sesssion so 
                //when come back to this page after 1st process/during process I can get that from session.
                if (alertSumBO.L_PAYLOC_FILE_ID != null)
                {
                    HttpContext.Session.SetString(SessionKeyPaylocFileID, alertSumBO.L_PAYLOC_FILE_ID.ToString());
                }

                if (HttpContext.Session.GetString(SessionKeyPaylocFileID) != null)
                {
                    alertSumBO.L_PAYLOC_FILE_ID = Convert.ToInt32(HttpContext.Session.GetString(SessionKeyPaylocFileID));
                }

                alertSumBO.L_USERID = CurrentUserId();

                ViewData["paylocID"] = alertSumBO.L_PAYLOC_FILE_ID;

                //List<ErrorAndWarningViewModelWithRecords> model = new List<ErrorAndWarningViewModelWithRecords>();
                List<ErrorAndWarningViewModelWithRecords> model = new List<ErrorAndWarningViewModelWithRecords>();

                string apiBaseUrlForInsertEventDetails = _Configure.GetValue<string>("WebapiBaseUrlForInsertEventDetails");


                //pass remittance id to next action
                string encryptedRemID = alertSumBO.remittanceId;    //## this is coming in Decrypted format.. good boy!
                var remitIDStr = DecryptUrlValue(encryptedRemID, forceDecode: false);
                int remID = Convert.ToInt32(remitIDStr);

                ViewBag.remID = EncryptUrlValue(remID.ToString());

                ViewBag.empName = HttpContext.Session.GetString(SessionKeyEmployerName);

                //add remittance id into session for future use.


                Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                //work on remittance level if null else Paylocation level
                apiBaseUrlForErrorAndWarnings = _Configure.GetValue<string>("WebapiBaseUrlForErrorAndWarnings");

                alertSumBO.remittanceId = remitIDStr;
                model = await callApi.CallAPISummary(alertSumBO, apiBaseUrlForErrorAndWarnings);
                var newModel1 = model.Where(x => x.ACTION_BY.Equals("ALL")).ToList();

                foreach (var item in newModel1)
                {
                    item.EncryptedID = encryptedRemID;
                }


                // ViewBag.remID = remID;

                //remove the browser response issue of pen testing
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
                {
                    model.Clear();// = null;
                    RedirectToAction("Index", "Login");
                }

                var newModel = model.AsQueryable();
                newModel = newModel.OrderByDescending(x => x.remittanceID);


                int pageSize = 7;
                // vLSContext = vLSContext.OrderByDescending(x => x.CreatedDate);
                return View(PaginatedList<ErrorAndWarningViewModelWithRecords>.CreateAsync(newModel, pageNumber ?? 1, pageSize));
            }
            catch (Exception ex)
            {
                TempData["MsgError"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }
        }
        /// <summary>
        /// checks remittance id and login if session expired then returns to login page.
        /// </summary>
        /// <param name="remittanceID"></param>
        /// <returns></returns>
        private int CheckRem(int remittanceID)
        {
            try
            {
                if (remittanceID == 0)
                {
                    remittanceID = (int)HttpContext.Session.GetInt32(SessionKeyRemittanceID);
                }
                else
                {
                    HttpContext.Session.SetInt32(SessionKeyRemittanceID, remittanceID);
                }
            }
            catch (Exception ex)
            {
                TempData["MsgSession"] = "Session Expired, please login again";
                RedirectToAction("Index", "Login");
            }
            return remittanceID;
        }

        /// <summary>
        /// Used to show all records with a warning that is suitable for bulk approval.
        /// If we pass following action a remittance id then it will show all warnings and errors 
        /// and if we pass remittance id and alert type then specific error or warning will show
        /// </summary>
        /// <returns></returns>
        // public async Task<IActionResult> WarningsListforBulkApproval(ErrorAndWarningToShowListViewModel errorAndWarningTo)

        public async Task<IActionResult> WarningsListforBulkApproval(ErrorAndWarningViewModelWithRecords errorModel)
        {
            try
            {
                //following functionality is added to keep user on same warning and error page until all sorted.
                //## if we come back here from Acknowledgement page- then the ViewModel 'errorModel' is empty- so need to read from the Cache.. save life..

                //if (errorCount < 1) //## means empty... came here from another page not Dashboard
                if (errorModel.ALERT_COUNT is null) //## means empty... came here from another page not Dashboard
                {
                    //## if Alert.count<1 then its an Empty, but not null// (pain in the back)
                    //_ = Int32.TryParse(errorModel.ALERT_COUNT.Replace(".0", ""), out int errorCount);

                    errorModel = _cache.Get<ErrorAndWarningViewModelWithRecords>(CurrentUserId() + Constants.ErrorWarningSummaryKeyName);
                    
                    if(errorModel is null)
                    {
                        return RedirectToAction("Index", "Admin");  //## should not happen
                    }
                }
                else
                {
                    _cache.Set(CurrentUserId() + Constants.ErrorWarningSummaryKeyName, errorModel);
                }


                var recordsList = new List<ErrorAndWarningViewModelWithRecords>();
                var records = new ErrorAndWarningViewModelWithRecords();
                AlertSumBO alertSumBO = new AlertSumBO();
                string apiBaseUrlForErrorAndWarnings = string.Empty;

                string userName = CurrentUserId();

                ErrorAndWarningToShowListViewModel errorAndWarningTo = new ErrorAndWarningToShowListViewModel();
                //to check if remittance is null then session expire and return to login page.
                //if (string.IsNullOrEmpty(errorModel.remittanceID))
                //{
                //    errorModel.remittanceID = RemittanceId(); //## go - read from cache.. save ur time...
                //}
                //else
                //{
                //    //errorModel.remittanceID = CheckRem(Convert.ToInt32(CustomDataProtection.Decrypt(errorModel.remittanceID))).ToString();                    
                //    errorModel.remittanceID = errorModel.remittanceID;
                //}

                //alertSumBO.remittanceId = (int)errorModel.remittanceID;
                alertSumBO.remittanceId = errorModel.remittanceID;
                alertSumBO.L_USERID = userName;

                //show Employer name 
                ViewBag.empName = HttpContext.Session.GetString(SessionKeyEmployerName);
                //show error and worning on Remittance or paylocation level.
                apiBaseUrlForErrorAndWarnings = _Configure.GetValue<string>("WebapiBaseUrlForAlertDetailsPLNextSteps");
                if (HttpContext.Session.GetString(SessionKeyPaylocFileID) != null)
                {
                    alertSumBO.L_PAYLOC_FILE_ID = Convert.ToInt32(HttpContext.Session.GetString(SessionKeyPaylocFileID));
                    recordsList = await callApi.CallAPISummary(alertSumBO, apiBaseUrlForErrorAndWarnings);
                }
                else
                {
                    errorModel.L_PAYLOC_FILE_ID = 0;
                    // apiBaseUrlForErrorAndWarnings = _Configure.GetValue<string>("WebapiBaseUrlForErrorAndWarningsSelection");
                    errorAndWarningTo.remittanceID = Convert.ToDouble(DecryptUrlValue(errorModel.remittanceID));
                    errorAndWarningTo.L_USERID = userName;

                    errorAndWarningTo.alertType = errorModel.ALERT_TYPE_REF;                    
                    using (HttpClient client = new HttpClient())
                    {
                        StringContent content = new StringContent(JsonConvert.SerializeObject(errorAndWarningTo), Encoding.UTF8, "application/json");
                        string endpoint = apiBaseUrlForErrorAndWarnings;

                        using (var Response = await client.PostAsync(endpoint, content))
                        {
                            if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                _logger.LogInformation($"BulkApproval API Call successfull. StringContent: {content}");
                                //call following api to get this uploaded remittance id of file.
                                string result = await Response.Content.ReadAsStringAsync();
                                recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(result);
                            }
                        }
                    }
                }

                foreach (var record in recordsList)
                {
                    record.EncryptedID = EncryptUrlValue(record.DATAROWID_RECD);
                }
                // model = ChangeAPIDataToReadAble(result);
                if (errorModel.ALERT_CLASS.Equals("W"))
                {
                    ViewBag.alertClass = "Warning";
                }
                else if (errorModel.ALERT_CLASS.Equals("E"))
                {
                    ViewBag.alertClass = "Error";
                }

                ViewBag.status = errorModel.ALERT_DESC + "";
                //ViewBag.total = errorModel.COUNT == 0 ? model.Count : errorModel.COUNT;
                ViewBag.total = errorModel.ALERT_COUNT.Replace(".0", "");

                return View(recordsList);
            }
            catch (Exception ex)
            {
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
            {
                RedirectToAction("Index", "Login");
            }


            try
            {
                _ = int.TryParse(DecryptUrlValue(id, forceDecode: false), out int decryptedID);

                string result = string.Empty;
                string apiBaseUrlForErrorAndWarningsApproval = _Configure.GetValue<string>("WebapiBaseUrlForErrorAndWarningsApproval");
                string apiBaseUrlForInsertEventDetails = _Configure.GetValue<string>("WebapiBaseUrlForInsertEventDetails");

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
                        var alertid = Regex.Matches(ids, @"\d+");
                        if (alertId.Count == 1)
                        {
                            errorAndWarningTo.alertID = decryptedID;
                        }
                        else {
                            errorAndWarningTo.alertID = 1324;   // TODO
                        }

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
                                var cachedModel = _cache.Get<ErrorAndWarningViewModelWithRecords>(CurrentUserId() + Constants.ErrorWarningSummaryKeyName);
                                if (cachedModel != null) { 
                                    
                                }
                            }
                        }
                    }
                }
                
                return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM", new { remittanceID = id });
                //return RedirectToAction("Index", remittanceID);
            }
            catch (Exception ex)
            {
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
            //remove the browser response issue of pen testing
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
            {
                RedirectToAction("Index", "Login");
            }

            try
            {
                List<ErrorAndWarningViewModelWithRecords> recordsList = new List<ErrorAndWarningViewModelWithRecords>();
                ErrorAndWarningViewModelWithRecords records = new ErrorAndWarningViewModelWithRecords();

                int.TryParse(DecryptUrlValue(id, forceDecode: false), out int dataRowID);

                IEnumerable<HelpForEAndAUpdateRecord> helpText = null;
                //show employer name
                ViewBag.empName = HttpContext.Session.GetString(SessionKeyEmployerName);
                string userName = CurrentUserId();
                MemberUpdateRecordBO memberUpdateRecordBO = new MemberUpdateRecordBO();

                string apiBaseUrlForErrorAndWarningsApproval = _Configure.GetValue<string>("WebapiBaseUrlForUpdateRecordGetValues");

                string apiBaseUrlForErrorAndWarningsList = _Configure.GetValue<string>("WebapiBaseUrlForUpdateRecordGetErrorWarningList");
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

                            // TempData["msg"] = errorAndWarningTo.returnStatusTxt;

                        }
                    }
                }

                foreach (var item in recordsList)
                {
                    item.EncryptedID = EncryptUrlValue(item.MC_ALERT_ID);   //## Encrypt them to be used in QueryString
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
                updateRecordBO.modUser = HttpContext.Session.GetString(SessionKeyUserID);

                string encryptedRemID = DecryptUrlValue(_cache.GetString($"{CurrentUserId()}_{SessionKeyRemittanceID}"));

                int remID = Convert.ToInt32(encryptedRemID);

                string apiLink = _Configure.GetValue<string>("WebapiBaseUrlForUpdateRecord");
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
            List<ErrorAndWarningViewModelWithRecords> recordsList = new List<ErrorAndWarningViewModelWithRecords>();
            ErrorAndWarningViewModelWithRecords records = new ErrorAndWarningViewModelWithRecords();

            GetMatchesBO getMatchesBO = new GetMatchesBO();
            //List<GetMatchesBO> listOfMatches = new List<GetMatchesBO>();
            GetMatchesViewModel listOfMatches = new GetMatchesViewModel();
            HelpTextBO helpTextBO = new HelpTextBO();

            int.TryParse(DecryptUrlValue(id), out int dataRowID);

            try
            {
                // IEnumerable<HelpForEAndAUpdateRecord> helpText = null;
                //show employer name
                ViewBag.empName = HttpContext.Session.GetString(SessionKeyEmployerName);
                MemberUpdateRecordBO memberUpdateRecordBO = new MemberUpdateRecordBO();
                //shows error and warnings data
                string apiBaseUrlForUpdateRecordGetValues = _Configure.GetValue<string>("WebapiBaseUrlForUpdateRecordGetValues");
                //shows recent data that is inserted by using posting file 
                string apiBaseUrlForMatchingRecordsManual = _Configure.GetValue<string>("WebapiBaseUrlForMatchingRecordsManual");
                //shows members matched data that was already inside UPM for matching
                string apiBaseUrlForErrorAndWarningsList = _Configure.GetValue<string>("WebapiBaseUrlForUpdateRecordGetErrorWarningList");

                helpTextBO.L_DATAROWID_RECD = dataRowID;
                helpTextBO.L_USERID = HttpContext.Session.GetString(SessionKeyUserID);
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
                getMatchesBO.userId = HttpContext.Session.GetString(SessionKeyUserID);
                //int remID = (int)HttpContext.Session.GetInt32(SessionKeyRemittanceID);
                getMatchesBO.dataRowId = dataRowID;
                //method to get matching records from UPM
                listOfMatches.Matches = await GetRecords(getMatchesBO);

                memberUpdateRecordBO.dataRowID = dataRowID;
                ViewBag.fileData = memberUpdateRecordBO;

                foreach (var item in listOfMatches.Matches)
                {
                    item.dataRowId = dataRowID;
                }

                //ViewBag.listOfFolders = listOfMatches.Matches.FirstOrDefault(a => a.folderId == Model.Matches[j].folderId);

                //remove the browser response issue of pen testing
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
                {
                    listOfMatches.Matches.Clear();// = null;
                    listOfMatches.activeProcess = null;
                    listOfMatches.note = null;
                    RedirectToAction("Index", "Login");
                }

                return View(listOfMatches);
                //return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
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
            string apiLink = _Configure.GetValue<string>("WebapiBaseUrlForMatchingRecordsUPM");
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
            string encryptedRemID = DecryptUrlValue(_cache.GetString($"{CurrentUserId()}_{SessionKeyRemittanceID}"));

            int remID = Convert.ToInt32(encryptedRemID);

            string apiLink = _Configure.GetValue<string>("WebapiBaseUrlForMatchingRecordsManual");
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
                    string s1 = bO.isActive.Substring(0, 12);
                    string s2 = bO.isActive.Remove(0, 12);

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
                TempData["error"] = "Select a matching record by clicking on radio button and submit record.";
                return RedirectToAction("NewMatchingCritera", "SummaryNManualM", new { id = bO.dataRowId });
            }

            bO.personMatch = updateRecordModel.personMatch;
            bO.folderMatch = updateRecordModel.folderMatch;
            bO.userId = HttpContext.Session.GetString(SessionKeyUserID);
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

                //remove the browser response issue of pen testing
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
                {
                    remID = -1;
                    RedirectToAction("Index", "Login");
                }

                return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM", remID);
                // return RedirectToAction("Index", remID);
            }
            catch (Exception ex)
            {
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
        public async Task<IActionResult> ResetRecord(string Id)
        {
            string result = string.Empty;
            string apiBaseUrlForRecordReset = _Configure.GetValue<string>("WebapiBaseUrlForRecordReset");
            RecordResetBO bo = new RecordResetBO();
            try
            {

                int.TryParse(DecryptUrlValue(Id, forceDecode: false), out int dataRowID);
                string userID = HttpContext.Session.GetString(SessionKeyUserID);                
                int remID = Convert.ToInt16(GetRemittanceId(returnEncryptedIdOnly: false));

                bo.P_DATAROWID_RECD = dataRowID;
                bo.P_USERID = userID;

                using (HttpClient client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(bo), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrlForRecordReset;

                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                            _logger.LogInformation("BulkApproval API Call successfull");
                            //call following api to get this uploaded remittance id of file.
                            result = await Response.Content.ReadAsStringAsync();
                            result = JsonConvert.DeserializeObject<string>(result);
                            //errorAndWarningTo = JsonConvert.DeserializeObject<ErrorAndWarningToShowListViewModel>(result);
                        }
                    }
                }
                TempData["msg"] = result;

                //remove the browser response issue of pen testing
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
                {
                    remID = -1;
                    RedirectToAction("Index", "Login");
                }
                //return RedirectToAction("Index", remID);
                return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM", remID);
            }
            catch (Exception ex)
            {
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
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
            //remove all decimals
            //int input = newVal.IndexOf(".");
            //if(input > 0)
            //    newVal = newVal.Substring(0, input);
            return newVal;
        }
    }
}
