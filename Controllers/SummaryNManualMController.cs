using DocumentFormat.OpenXml.Office2010.Excel;
using MCPhase3.CodeRepository;
using MCPhase3.CodeRepository.RefectorUpdateRecord;
using MCPhase3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace MCPhase3.Controllers
{

    

    public class SummaryNManualMController : BaseController
    {
        private readonly ILogger<SummaryNManualMController> _logger;
        private readonly IWebHostEnvironment _host;
        private readonly IConfiguration _Configure;
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


        public SummaryNManualMController(ILogger<SummaryNManualMController> logger, IWebHostEnvironment host, IConfiguration configuration) : base(configuration)
        {
            _Configure = configuration;
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
           
            //inc will keep check manual matching stage and it will not be successfully untill 
            //Process successfully completed. 
            int inc = 2;
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

                alertSumBO.L_USERID = HttpContext.Session.GetString("_UserName");

            ViewData["paylocID"] = alertSumBO.L_PAYLOC_FILE_ID;

            //List<ErrorAndWarningViewModelWithRecords> model = new List<ErrorAndWarningViewModelWithRecords>();
            List<ErrorAndWarningViewModelWithRecords> model = new List<ErrorAndWarningViewModelWithRecords>();

            string apiBaseUrlForInsertEventDetails = _Configure.GetValue<string>("WebapiBaseUrlForInsertEventDetails");

                
            //pass remittance id to next action
            
            int remID = 0;

                remID = Convert.ToInt32(DecryptUrlValue(alertSumBO.remittanceId));
                ViewBag.remID = remID;
                //checks remittance id and login if session expired then returns to login page.
                alertSumBO.remittanceId = CheckRem(remID).ToString();

            ViewBag.empName = HttpContext.Session.GetString(SessionKeyEmployerName);

            //add remittance id into session for future use.


            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            //work on remittance level if null else Paylocation level
            apiBaseUrlForErrorAndWarnings = _Configure.GetValue<string>("WebapiBaseUrlForErrorAndWarnings");


            model = await callApi.CallAPISummary(alertSumBO, apiBaseUrlForErrorAndWarnings);
            var newModel1 = model.Where(x => x.ACTION_BY.Equals("ALL")).ToList();

                
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
            catch(Exception ex)
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
                if (string.IsNullOrEmpty(errorModel.remittanceID) && string.IsNullOrEmpty(errorModel.ALERT_CLASS))
                {
                 errorModel =  HttpContext.Session.GetObjectFromJson<ErrorAndWarningViewModelWithRecords>("ErrorAndWarningViewModelWithRecords");
                }
                else
                {
                    HttpContext.Session.SetObjectAsJson("ErrorAndWarningViewModelWithRecords", errorModel);
                }
                

                List<ErrorAndWarningViewModelWithRecords> recordsList = new List<ErrorAndWarningViewModelWithRecords>();
                ErrorAndWarningViewModelWithRecords records = new ErrorAndWarningViewModelWithRecords();
                AlertSumBO alertSumBO = new AlertSumBO();
                string apiBaseUrlForErrorAndWarnings = string.Empty;

                string userName = HttpContext.Session.GetString("_UserName");

                ErrorAndWarningToShowListViewModel errorAndWarningTo = new ErrorAndWarningToShowListViewModel();
                //to check if remittance is null then session expire and return to login page.
                if (string.IsNullOrEmpty(errorModel.remittanceID) || errorModel.remittanceID.Equals("0"))
                {
                    errorModel.remittanceID = CheckRem(0).ToString();
                }
                else
                {
                    errorModel.remittanceID = CheckRem(Convert.ToInt32(CustomDataProtection.Decrypt(errorModel.remittanceID))).ToString();
                }

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
                    errorAndWarningTo.remittanceID = Convert.ToDouble(errorModel.remittanceID);
                    errorAndWarningTo.L_USERID = userName;

                    errorAndWarningTo.alertType = errorModel.ALERT_TYPE_REF;
                    string result = string.Empty;
                    using (HttpClient client = new HttpClient())
                    {
                        StringContent content = new StringContent(JsonConvert.SerializeObject(errorAndWarningTo), Encoding.UTF8, "application/json");
                        string endpoint = apiBaseUrlForErrorAndWarnings;

                        using (var Response = await client.PostAsync(endpoint, content))
                        {
                            if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                                _logger.LogInformation("BulkApproval API Call successfull");
                                //call following api to get this uploaded remittance id of file.
                                result = await Response.Content.ReadAsStringAsync();
                                recordsList = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(result);
                                //errorAndWarningTo = JsonConvert.DeserializeObject<ErrorAndWarningToShowListViewModel>(result);
                            }
                        }
                    }
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

                ViewBag.status = errorModel.ALERT_DESC;
                //ViewBag.total = errorModel.COUNT == 0 ? model.Count : errorModel.COUNT;
                ViewBag.total = errorModel.ALERT_COUNT.Replace(".0", "");
               
                //remove the browser response issue of pen testing
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
                {
                    recordsList.Clear();// = null;
                    RedirectToAction("Index", "Login");
                }
                return View(recordsList);
            }
            catch(Exception ex)
            {
                TempData["Msg1"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }
           
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
            try
            {
                string result = string.Empty;
                string apiBaseUrlForErrorAndWarningsApproval = _Configure.GetValue<string>("WebapiBaseUrlForErrorAndWarningsApproval");
                string apiBaseUrlForInsertEventDetails = _Configure.GetValue<string>("WebapiBaseUrlForInsertEventDetails");

                ErrorAndWarningApprovalOB errorAndWarningTo = new ErrorAndWarningApprovalOB();
                errorAndWarningTo.userID = HttpContext.Session.GetString(SessionKeyUserID);
                int remittanceID = (int)HttpContext.Session.GetInt32(SessionKeyRemittanceID);
                //if we call this action from update record page then alertId will be null so I assign id that comes from update record.
                if (alertId.Count == 0)
                {
                    alertId.Add("id", id);
                }

                foreach (var ids in alertId.Values)
                {
                    var alertid = Regex.Matches(ids, @"\d+");
                    string newID = string.Empty;
                    newID = ConverToString(alertid);

                    errorAndWarningTo.alertID = int.Parse(newID);
                    using (HttpClient client = new HttpClient())
                    {
                        StringContent content = new StringContent(JsonConvert.SerializeObject(errorAndWarningTo), Encoding.UTF8, "application/json");
                        string endpoint = apiBaseUrlForErrorAndWarningsApproval;

                        using (var Response = await client.PostAsync(endpoint, content))
                        {
                            if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                                _logger.LogInformation("BulkApproval API Call successfull");
                                //call following api to get this uploaded remittance id of file.
                                result = await Response.Content.ReadAsStringAsync();
                                errorAndWarningTo = JsonConvert.DeserializeObject<ErrorAndWarningApprovalOB>(result);

                                TempData["msg"] = errorAndWarningTo.returnStatusTxt;

                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
                {
                    remittanceID = -1;
                    RedirectToAction("Index", "Login");
                }

                return RedirectToAction("WarningsListforBulkApproval", "SummaryNManualM", remittanceID);
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
            try
            {


                List<ErrorAndWarningViewModelWithRecords> recordsList = new List<ErrorAndWarningViewModelWithRecords>();
                ErrorAndWarningViewModelWithRecords records = new ErrorAndWarningViewModelWithRecords();
                
                int.TryParse(CustomDataProtection.Decrypt(id), out int dataRowID);

                IEnumerable<HelpForEAndAUpdateRecord> helpText = null;
                //show employer name
                ViewBag.empName = HttpContext.Session.GetString(SessionKeyEmployerName);
                string userName = HttpContext.Session.GetString("_UserName");
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

                //remove the browser response issue of pen testing
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("_UserName")))
                {
                    memberUpdateRecordBO = null;// = null;
                    RedirectToAction("Index", "Login");
                }

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
                int remID = (int)HttpContext.Session.GetInt32(SessionKeyRemittanceID);
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
                string rem = CustomDataProtection.Decrypt(remID.ToString());
                //ViewData["remid"] = enRemid;
                //ViewData["enRemid"] = CustomDataProtection.Decode(enRemid);


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
            
            int.TryParse(CustomDataProtection.Decrypt(id), out int dataRowID);

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
            catch(Exception ex)
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

            int remID = (int)HttpContext.Session.GetInt32(SessionKeyRemittanceID);
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
                return RedirectToAction("NewMatchingCritera", "SummaryNManualM", new {id = bO.dataRowId });
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
            catch(Exception ex)
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

                int.TryParse(CustomDataProtection.Decrypt(Id), out int dataRowID);
                string userID = HttpContext.Session.GetString(SessionKeyUserID);
                int remID = (int)HttpContext.Session.GetInt32(SessionKeyRemittanceID);
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
            catch(Exception ex)
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
