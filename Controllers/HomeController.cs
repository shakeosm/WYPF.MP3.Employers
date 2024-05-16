using CsvHelper;
using MCPhase3.CodeRepository;
using MCPhase3.CodeRepository.InsertDataProcess;
using MCPhase3.Common;
using MCPhase3.Models;
using MCPhase3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MCPhase3.Common.Constants;

namespace MCPhase3.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _host;
        private readonly IConfiguration _Configure;
        private readonly string _customerUploadsLocalFolder;
        string apiBaseUrlForAutoMatch = string.Empty;
        EventDetailsBO eBO = new EventDetailsBO();

        public ICommonRepo _commonRepo;
        public IValidateExcelFile _validateExcel;
        private readonly IExcelData _excelData;
        private readonly ICheckTotalsService _checkTotalsService;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment host, IConfiguration configuration, IRedisCache Cache, IDataProtectionProvider Provider, IOptions<ApiEndpoints> ApiEndpoints, ICommonRepo CommonRepo, IValidateExcelFile ValidateExcelFile, IExcelData InsertDataTable, ICheckTotalsService CheckTotalsService) : base(configuration, Cache, Provider, ApiEndpoints)
        {
            _logger = logger;
            _host = host;
            _Configure = configuration;
            _commonRepo = CommonRepo;
            _validateExcel = ValidateExcelFile;
            _excelData = InsertDataTable;
            _checkTotalsService = CheckTotalsService;
            _customerUploadsLocalFolder = ConfigGetValue("FileUploadPath");

        }

        /// <summary>
        /// To LG - Remittance page
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            string clientType = ContextGetValue(Constants.SessionKeyClientType);
            //Session can set here to check if logged in user is Fire or LG.            
            string loginId = CurrentUserId();

            //client type is null or empty goto login page again
            if (IsEmpty(clientType))
            {
                LogInfo("Index() => IsEmpty(clientType) .. redirecting to Login() page");
                return RedirectToAction("Index", "Login");
            }

            //if logged in user is Fire then goto FireIndex
            if (clientType.Equals("FIRE"))
            {
                return RedirectToAction("IndexFire", "Home");
            }

            List<PayrollProvidersBO> subPayList = await GetPayrollProviderListByUser(loginId);

            subPayList = subPayList.Where(x => x.pay_location_ID != null).ToList();

            //## do we have an error message from previous FileUpload attempt? This page maybe loading after a Index_POST() call failed and redirected back here..
            string fileUploadErrorMessage = _cache.GetString(GetKeyName(Constants.FileUploadErrorMessage));
            //## Delete the message once Read.. otherwise- this will keep coming on every page request...
            _cache.Delete(GetKeyName(Constants.FileUploadErrorMessage));

            var viewModel = new HomeFileUploadVM()
            {
                MonthList = GetMonths(),
                YearList = GetYears(),                
                PayLocationList = subPayList,
                ErrorMessage = fileUploadErrorMessage,
            };

            //## to make better UI/UX- bring the user selection what they have made before submitting.. and maybe due to an error - we are showing them this page again...
            viewModel.SelectedYear = TempData["SelectedYear"]?.ToString();
            viewModel.SelectedMonth = TempData["SelectedMonth"]?.ToString();
            if (viewModel.SelectedYear != null) { 
                viewModel.SelectedPostType = int.Parse(TempData["SelectedPostType"]?.ToString());            
            }
            
            return View(viewModel);
        }

        /// <summary>
        /// To Fire - Remittance page
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> IndexFire()
        {
            string userId = string.Empty;
            userId = CurrentUserId();
            string monthSelected = ContextGetValue(Constants.SessionKeyMonth) ?? string.Empty;
            string yearSelected = ContextGetValue(Constants.SessionKeyYears) ?? string.Empty;
            string postSelected = ContextGetValue(Constants.SessionKeyPosting) ?? string.Empty;

            List<PayrollProvidersBO> subPayList = await GetPayrollProviderListByUser(userId);

            return View();
        }


        [AllowAnonymous]
        public async Task<IActionResult> Error()
        {
            var exDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var paramList = ContextGetValue(Constants.ApiCallParamObjectKeyName);

            var errorDetails = new ErrorViewModel()
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                UserId = HttpContext.Session.GetString(Constants.LoginNameKey),
                ApplicationId = Constants.EmployersPortal,
                ErrorPath = exDetails?.Path,
                Message = exDetails?.Error.Message,
                StackTrace = exDetails?.Error.StackTrace
            };

            string insertErrorLogApi = GetApiUrl(_apiEndpoints.ErrorLogCreate);
            await ApiPost(insertErrorLogApi, errorDetails);

            return View(errorDetails);
        }


        [AllowAnonymous]
        public async Task<IActionResult> ErrorCustom()
        {
            //### we keep the Error view model in cache.. not to pass it via URL- query string...
            //string cacheKey = $"{CurrentUserId()}_{Constants.CustomErrorDetails}";
            string cacheKey = $"{CurrentUserId()}_{Constants.CustomErrorDetails}";
            var errorDetails = _cache.Get<ErrorViewModel>(cacheKey);

            if (errorDetails == null)
            {
                return RedirectToAction("Index", "Home");
            }

            string insertErrorLogApi = GetApiUrl(_apiEndpoints.ErrorLogCreate);
            await ApiPost(insertErrorLogApi, errorDetails);

            _cache.Delete(cacheKey);    //## once Error Details are read and displayed- delete this.. Job done!
            return View(errorDetails);

        }

        /// <summary>
        /// following Create is to work with LG pages
        /// </summary>
        /// <param name="paymentFile"></param>
        /// <param name="monthsList"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HomeFileUploadVM vm)
        {
            LogInfo($"Create Remittance process started");
            if (!ModelState.IsValid)
            {
                _cache.SetString(GetKeyName(Constants.FileUploadErrorMessage), "Error: You must select 'Year', 'Month' and 'PostType' to continue");
                return RedirectToAction("Index", "Home");
            }

            if (!Path.Exists(_customerUploadsLocalFolder))
            {
                _cache.SetString(GetKeyName(Constants.FileUploadErrorMessage), "Error: File upload area not defined. Please contact support.");
                LogInfo($"Error: File upload area not defined. Please contact support");
                return RedirectToAction("Index", "Home");
            }

            var fileCheck = IsFileValid(vm.PaymentFile);

            if (!fileCheck.IsSuccess)
            {
                _cache.SetString(GetKeyName(Constants.FileUploadErrorMessage), fileCheck.Message);
                LogInfo($"Error: fileCheck.IsSuccess = False. Message: {fileCheck.Message}");
                return RedirectToAction("Index", "Home");
            }

            string userId = CurrentUserId();
            string loginName = ContextGetValue(Constants.LoginNameKey);
            var currentUser = await GetUserDetails(loginName);
            string empName = currentUser.Pay_Location_Name;
            
            //Add selected name of month into Session, filename and total records in file.
            HttpContext.Session.SetString(Constants.SessionKeyMonth, vm.SelectedMonth);
            HttpContext.Session.SetString(Constants.SessionKeyYears, vm.SelectedYear);
            HttpContext.Session.SetString(Constants.SessionKeyPosting, vm.SelectedPostType.ToString());
            HttpContext.Session.SetString(Constants.SessionKeySchemeName, SessionSchemeNameValue);

            //## store the user selection in the cache- so we can set the as Seleted once the user goes back to the page
            TempData["SelectedYear"] = vm.SelectedYear;  //## TempData[] is to transfer data between Actions in a controller- while on the same call..
            TempData["SelectedMonth"] = vm.SelectedMonth;
            TempData["SelectedPostType"] = vm.SelectedPostType;

            //##### Convert the Excel file into CSV and then into Class. Will be required by 'IsA_Valid2ndMonthPosting()'
            var excelSheetData = ConvertExcel_To_CSV_To_ClassObject();
            int numberOfRows = excelSheetData.Count;
            HttpContext.Session.SetString(Constants.SessionKeyTotalRecords, numberOfRows.ToString());

            var submissionInfo = new CheckFileUploadedBO
            {
                P_Month = vm.SelectedMonth,
                P_Year = vm.SelectedYear,
                P_EMPID = vm.SelectedPayLocationId  //## actually refering to 'payroll_provider_id' in table. EmployerId: '1003701' and payroll_provider_id: 'BAR0122'
            };

            if (vm.SelectedPostType == (int)PostingType.First)
            {
                //check if records were uploaded previously for the SelectedPayLocationId, month and year.
                string apiCheckFileIsUploadedUrl = GetApiUrl(_apiEndpoints.CheckFileIsUploaded);   //## api/CheckFileUploaded
                var apiResult = await ApiPost(apiCheckFileIsUploadedUrl, submissionInfo);
                int fileAlreadyUploaded = Convert.ToInt16(apiResult);

                if (fileAlreadyUploaded == 1)
                {
                    _cache.SetString(GetKeyName(Constants.FileUploadErrorMessage), $"<h5>File is already uploaded for the month: {vm.SelectedMonth} and payrol period: {vm.SelectedYear} <br/> You can goto Dashboard and start process on file from there.</h5>");
                    return RedirectToAction("Index", "Home");
                }
            }

            /// If “2nd posting for same month“ is selected- then Validate process should check the uploaded Excel sheet - there should be records only for the selected Month/Year
            if (vm.SelectedPostType == (int)PostingType.Second)
            {
                if (!IsA_Valid2ndMonthPosting(vm.SelectedYear, vm.SelectedMonth[..3], vm.SelectedPayLocationId))
                {
                    //## Respective error message is already set from function 'IsA_Valid2ndMonthPosting()'
                    return RedirectToAction("Index", "Home");
                }
            }
                                    
            

            List<PayrollProvidersBO> subPayList = await GetPayrollProviderListByUser(userId);

            //user selects a year from dropdown list so no need to provide seperate list of years. posting will ignore same month validation.
            //## This is the actual Field / Data validation on the Excel file - which is now in a 'List<ExcelsheetDataVM>'. This will generate respective error message based on the defined validation rules on each field.
            string validPayrollYearList = vm.SelectedYear;  //## for 1st and 2nd Posting- restrict the ValidPayrollYear to the selected one.
            if (vm.SelectedPostType == (int)PostingType.PreviousMonth)
            {
                //## if the file has previous months' record- then PayrollYear can be anything.. send the full ValidPayroll Year list.
                validPayrollYearList = String.Join(',', GetYears());
            }

            string spreadsheetValidationErrors = _validateExcel.Validate(excelSheetData, vm.SelectedMonth, vm.SelectedPostType.ToString(), validPayrollYearList, subPayList);
            LogInfo($"_validateExcel.Validate({vm.SelectedYear}, {vm.SelectedMonth}, {vm.SelectedPostType}): Finished.");

            if (IsEmpty(spreadsheetValidationErrors))
            {
                _cache.SetString(GetKeyName(Constants.FileUploadErrorMessage), $"Success: File contents are validated successfully and ready to upload. Total <b>{numberOfRows}</b> records found.");
                return RedirectToAction("Index", "Home");
            }
            else
            {
                
                _cache.SetString(GetKeyName(Constants.FileUploadErrorMessage), "<h3> Please remove the following errors from file and upload again</h3><br />" + spreadsheetValidationErrors);
                LogInfo("Error while doing _validateExcel.Validate(). Returning back to /Home/Index to reupload the file.");
                return RedirectToAction("Index", "Home");
            }

        }

        /// <summary>We already have converted that Excel file into a CSV file while checking for malacious tags. We can get the path from Session cookie and start using it for further processing
        /// </summary>
        private List<ExcelsheetDataVM> ConvertExcel_To_CSV_To_ClassObject()
        {
            //## we already have converted that Excel file into a CSV file while checking for malacious tags. We can get the path from Session cookie and start using it for further processing
            string csvFilePath = ContextGetValue(Constants.Staging_CSV_FilePathKey);
            var remittanceRecords = new List<ExcelsheetDataVM>();

            using (var reader = new StreamReader(csvFilePath))
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                remittanceRecords = csv.GetRecords<ExcelsheetDataVM>().ToList();
            }

            _cache.Set(GetKeyName(ExcelData_ToInsert), remittanceRecords);
            System.IO.File.Delete(csvFilePath); //## this is a staging file for invalid contents check.. now all need is finished

            return remittanceRecords;
        }

        private TaskResults IsFileValid(IFormFile file)
        {
            var result = new TaskResults { IsSuccess = false, Message = string.Empty };
            if (file is null)
            {
                result.Message += "No file selected.";
                return result;
            }
            _ = int.TryParse(_Configure["FileSizeLimit"], out int fileSizeLimit);

            if (file.Length > fileSizeLimit)
            {
                result.Message = "File size must be less than :" + (fileSizeLimit / 1024) / 1024 + " MB";
            }

            if (!(file.ContentType.Contains("vnd.ms-excel") || file.ContentType.Contains("vnd.openxmlformats")))
            {
                result.Message += "<br/> Invalid file contents, ";
            }

            if (!Path.GetExtension(file.FileName).Contains("xls"))
            {
                result.Message += "<br/> Invalid file type";
            }

            result.IsSuccess = IsEmpty(result.Message);
            LogInfo($"IsFileValid() > result: {result}, Message: {result.Message}, File: {file.Name}, Length: {file.Length/1024} KB, type: {file.ContentType}, extension: {Path.GetExtension(file.FileName)}");


            return result;
        }


        /// <summary>
        /// following action will replace Remad.aspx page from old web portal.
        /// Remad is for LG
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CheckTotals()
        {
            var excelData = _cache.Get<List<ExcelsheetDataVM>>(GetKeyName(Constants.ExcelData_ToInsert));

            var contributionBO = _checkTotalsService.GetSpreadsheetValues(excelData);

            return View(contributionBO);
        }


        /// <summary>
        /// following action will replace Remad.aspx page from old web portal.
        /// Remad is for LG
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult RemadF()
        {
            CheckTotalsService formTotals = new CheckTotalsService();
            MonthlyContributionBO contributionBO = new MonthlyContributionBO();

            //remove the browser response issue of pen testing
            if (IsEmpty(ContextGetValue("_UserName")))
            {
                contributionBO = null;

                RedirectToAction("Index", "Login");
            }
            return View(contributionBO);
        }


        /// <summary> New remitance submitted and remitance ID created for the uploaded file/// </summary>
        /// <param name="contributionPost"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> CheckTotals(MonthlyContributionBO contributionPost)
        {
            //var userLoginName = ContextGetValue(Constants.LoginNameKey);
            var currentUser = await GetUserDetails(ContextGetValue(Constants.LoginNameKey));            
            contributionPost.UserLoginID = currentUser.LoginName;
            contributionPost.UserName = currentUser.UserId;
            contributionPost.employerID = currentUser.Pay_Location_ID;
            contributionPost.employerName = ContextGetValue(Constants.SessionKeyEmployerName);
            contributionPost.payrollProviderID = currentUser.Pay_Location_Ref;
            
            //contributionSummaryInfo.ClientID = ContextGetValue(Constants.SessionKeyClientId);
            contributionPost.ClientID = currentUser.Client_Id;
            contributionPost.payrollYear = ContextGetValue(Constants.SessionKeyYears);
            contributionPost.PaymentMonth = ContextGetValue(Constants.SessionKeyMonth);

            string fileNamePrefix = $"{contributionPost.employerID}-{contributionPost.employerName.Replace(" ", "-")}-{contributionPost.payrollYear.Replace("/", "-")}-{contributionPost.PaymentMonth}-{DateTime.Now.ToString("dd-MM-yyyy")}-{DateTime.Now.ToString("hh-mm-ss")}";            

            //## Rename the file name.. Replace GUID with 'fileNamePrefix'            
            contributionPost.UploadedFileName = RenameFileName(fileNamePrefix);

            string remittanceInsertApi = GetApiUrl(_apiEndpoints.InsertRemitanceDetails);

            //## First Create the Remittance with its Details.. insert-into 'UPMWEBEMPLOYERCONTRIBADVICE'
            LogInfo($"Create the Remittance with its Details.. insert-into 'UPMWEBEMPLOYERCONTRIBADVICE. {currentUser.Pay_Location_Ref}-{contributionPost.employerName}, {contributionPost.PaymentMonth}-{contributionPost.payrollYear}");

            var apiResult = await ApiPost(remittanceInsertApi, contributionPost);
            string remittanceID = JsonConvert.DeserializeObject<string>(apiResult);            

            if (IsEmpty(apiResult)) {
                //## somehow crashed... 
                string errorMessage = $"Failed to insert Remittance information into database. User: {currentUser.LoginName}, employer: {contributionPost.employerName}, Provider: {contributionPost.payrollProviderID}, Period: {contributionPost.payrollYear}/{contributionPost.PaymentMonth}";
                TempData["Msg"] = "Failed to insert Remittance information into database. Please contact MP3 support team.";
                LogInfo(errorMessage, true);
                return View(contributionPost);

            }
            //## Insert EventDetails: RemitanceSubmitted =>	New remitance submitted and remitance ID created for the uploaded file
            EventLog_Add(Convert.ToInt32(remittanceID), $"New remitance submitted and remitance ID created for the uploaded file. employerID: {contributionPost.employerID}, Payroll Period: {contributionPost.PaymentMonth}-{contributionPost.payrollYear}, ", (int)EventType.RemitanceSubmitted, (int)EventType.RemitanceSubmitted);

            LogInfo($"Remittance Summary successfully inserted into database, Id: {remittanceID}.");

            //## Now insert the Bulk Data in the Database..            
            LogInfo($"calling- InsertBulkData({remittanceID}).");
            
            var isInserted = await InsertBulkData(Convert.ToInt32(remittanceID));

            LogInfo($"InsertBulkData() finished. Success= {isInserted}");

            if (isInserted)
            {
                int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));
                EventLog_Add(Convert.ToInt32(remittanceID), $"Bulk data is inserted into database. Total records: {totalRecordsInFile}. EmployersEmployeeTotalValue: {contributionPost.EmployersEmployeeTotalValue().ToString("C2")}", (int)EventType.BulkDataInsert, (int)EventType.BulkDataInsert);
                                
                _ = MoveFileToDone(remittanceID);   //## This will also insert EventLog for the 'FileMovedToDone' EventType
                //## we can still work as usual even if the file wasn't moved to 'Done' folder

            }
            else {
                TempData["MsgError"] = $"Remittance Id: {remittanceID}. System has failed inserting records in to the database, Please contact MP3 support.";
                EventLog_Add(Convert.ToInt32(remittanceID), "FAILED to execute Bulk data insert into database.", (int)EventType.BulkDataInsert, (int)EventType.BulkDataInsert);
                LogInfo("FAILED to execute Bulk data insert into database.", true);
                //## Delete this Temp file.. not needed anymore..
                DeleteTheExcelFile();
                return View(contributionPost);
            }

            remittanceID = EncryptUrlValue(remittanceID);

            //## all good- we have checked the Totals and used it from the cache.. now better remove that DataTable variable from cache.. work done
            _cache.Delete(GetKeyName(Constants.ExcelDataAsString));
                            
            //## Pass the RemittanceId via Session cache- to make the Url less Ugly with the Encrypted RemittanceId
            ContextSetValue(Constants.SessionKeyRemittanceID, remittanceID);

            LogInfo("Exiting Home/CheckTotals page..");
            return RedirectToAction("InitialiseProcessWithSteps");

        }

        private void DeleteTheExcelFile()
        {
            string filePathName = _cache.GetString($"{CurrentUserId()}_{Constants.UploadedExcelFilePathKey}");

            if (!Path.Exists(Path.GetFullPath(filePathName)))
            {
                LogInfo($"Error: DeleteTheExcelFile() => filePathName: {filePathName}, is missing!");
            }
            else { 
                System.IO.File.Delete(filePathName);
            }

        }


        /// <summary>This will move our User data file (excel, csv) to the DONE folder...</summary>
        /// <returns></returns>
        private bool MoveFileToDone(string remittanceID)
        {

            string customerFileName = _cache.GetString(GetKeyName(UploadedExcelFilePathKey)); 
            string sourceFilePath = Path.Combine(_customerUploadsLocalFolder, customerFileName);

            string destinationFolder = _customerUploadsLocalFolder + ConfigGetValue("FileUploadDonePath");

            //if (!Path.Exists(Path.GetFullPath(filePathName)))
            if (!Path.Exists(sourceFilePath))
            {
                TempData["MsgError"] = $"User File not found/accessible. Please contact MP3 support.";
                LogInfo($"User File not found/accessible. Please contact MP3 support. sourceFilePath: {sourceFilePath}");
                return false;
            }

            if (!Path.Exists(destinationFolder)){
                Directory.CreateDirectory(destinationFolder);             
            }

            try
            {

                string destinationFile = Path.Combine(destinationFolder, customerFileName);
                System.IO.File.Move(sourceFilePath, destinationFile, true);

                EventLog_Add(Convert.ToInt32(remittanceID), "File is processed and now moved to 'Done' folder.", (int)EventType.FileMovedToDone, (int)EventType.FileMovedToDone);
                string logInfoText = $"Moved user uploaded file to: 'DONE' folder. destinationFile: {destinationFile}";
                LogInfo(logInfoText);                                    

                return true;

            }
            catch (Exception ex)
            {
                EventLog_Add(Convert.ToInt32(remittanceID), $"Failed to move the customer upload file in 'Done' folder. Details: {ex.Message}", (int)EventType.FileMovedToDone, (int)EventType.FileMovedToDone);
                LogInfo($"Error at MoveFileToDone(). Source: '{sourceFilePath}', Destination: '{destinationFolder}', Details: {ex}");
                TempData["MsgError"] = "System failed to process the file, Please contact MP3 support.";                
            }

            return false;   //## shouldn't be  coming here.. something wrong.. return 'false'..
            
        }

        /// <summary>
        /// Now insert the Bulk Data in the Database
        /// </summary>
        /// <returns>Yes/No on Success/failure</returns>
        private async Task<bool> InsertBulkData(int remittanceId)
        {
            string userName = CurrentUserId();
            string clientID = ContextGetValue(Constants.SessionKeyClientId);
            string schemeName = ContextGetValue(Constants.SessionKeySchemeName);
            int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));

            var rangeOfRowsModel = new RangeOfRowsModel
            {
                P_USERID = userName,
                P_REMITTANCE_ID = remittanceId,
                P_NUMBER_OF_VALUES_REQUIRED = totalRecordsInFile
            };

            LogInfo($"Executing InsertBulkData(). remittanceId: {remittanceId}, totalRecordsInFile: {totalRecordsInFile}");

            //Get the max Datarow id from MC_CONTRIBUTIONS_RECD to insert bulk data.
            //## we need to get the first DataRow_RecordId from the Database.. and then create new Primary Key as we insert new records.
            string getFirstRowIdApi = GetApiUrl(_apiEndpoints.InsertDataCounter);   // will return- [P_FIRST_ROWID_VALUE] from 'RangeRowsReturn' api
            var apiResult = await ApiPost(getFirstRowIdApi, rangeOfRowsModel);
            int dataRowRecordId = JsonConvert.DeserializeObject<int>(apiResult);

            LogInfo($"New recordId will start from, dataRowRecordId: {dataRowRecordId}");
            
            //### INSERT 'Summary' info in the Class, before Bulk Insert, in all rows, ie: UserName, ClientId, RemittanceId, MODDATE, PostDate
            LogInfo($"_insertDataTable.AddMetaData(). excelData: {totalRecordsInFile}");
            _excelData.AddRemittanceInfo(remittanceId, dataRowRecordId++, clientID, schemeName, userName);
            LogInfo($"_insertDataTable.AddMetaData() Finished!");
            
            var excelDataTransformedInClass = _excelData.Get(userName);

            //#### INSERT BULK DATA
            string bulkDataInsertApi = GetApiUrl(_apiEndpoints.InsertData);
            LogInfo($"calling-> {bulkDataInsertApi}. Total records in excelData: {excelDataTransformedInClass.Count}");
            apiResult = await ApiPost(bulkDataInsertApi, excelDataTransformedInClass);

            if (IsEmpty(apiResult)) {
                LogInfo($"ERROR: Failed to insert Bulk data: remittanceId: {remittanceId}");
                LogInfo("delete the Remittance Summary info from the Table.. to make sure no incomplete info left to rot.");

                string apiCallDeleteRemittance = GetApiUrl(_apiEndpoints.DeleteRemittance);
                _ = await ApiGet($"{apiCallDeleteRemittance}{remittanceId}/{CurrentUserId()}");

                ErrorViewModel errorViewModel = new() {
                    ApplicationId = Constants.EmployersPortal,
                    DisplayMessage = "Failed to insert Bulk data",
                    ErrorPath = bulkDataInsertApi,
                    Message = "Api returned NULL result..",
                    RemittanceInfo = remittanceId.ToString(),
                    Source = "Home/InsertBulkData()",
                    StackTrace = "n/a",
                    UserId = userName,
                };
                await ErrorLog_Insert(errorViewModel);

                return false;
            }

            //## store the value in Sesion for InitialistProcess() to pick it up later
            ContextSetValue(Constants.SessionKeyTotalRecordsInDB, totalRecordsInFile.ToString());
            
            bool InsertToDbSuccess = JsonConvert.DeserializeObject<bool>(apiResult);

            return InsertToDbSuccess;            
        }

        public void EventLog_Add(int remitID, string eventNotes, int remittanceStatus, int eventTypeID)
        {
            //string apiBaseUrlForInsertEventDetails = GetApiUrl(_apiEndpoints.InsertEventDetails);
            var eBO = new EventDetailsBO
            {
                //Update Event Details table and add File Uploaded and ready to FTP
                remittanceID = remitID,
                remittanceStatus = remittanceStatus,
                eventTypeID = eventTypeID,
                notes = eventNotes.Length > 200 ? eventNotes[..200] : eventNotes,
            };

            //update Event Details table File is uploaded successfully.                               
            InsertEventDetails(eBO);
        }

        /// <summary>
        /// This section calls the big workload- ReturnInitialise Procedure in the DB- to do all initial work- 
        /// eg: Auto-matching, Error/warning generation and other tasks to set the Status, Scores, etc..
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> InitialiseProcess()
        {           
            var encryptedRemittanceId = ContextGetValue(Constants.SessionKeyRemittanceID);
            int remttanceId = Convert.ToInt32(DecryptUrlValue(encryptedRemittanceId));
            string userID = CurrentUserId();

            var initialiseProcessResultVM = new InitialiseProcessResultVM()
            {
                EncryptedRemittanceId = encryptedRemittanceId,
                EmployeeName = ContextGetValue(Constants.SessionKeyEmployerName)
            };

            
            LogInfo($"\r\nLoading Home/InitialiseProcess page.. RemittanceId: {remttanceId}");

            //## Make sure all records are inserted in the DB.Table- before trying to run the Initialise Process.
            _ = await CheckAllRecordsAreInsertedInDB(remttanceId);

            int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));

            //## Execute_ReturnInitialiseProcess() call.. a big piece of Task- initialising the entire journey, setting values, generating error/warnings, fixing issues and many things... 
            var initialiseProcessResult = await Execute_ReturnInitialiseProcess(userID, remttanceId);

            if(initialiseProcessResult.P_STATUSCODE != 0 ) {
                //## value '0' means all good ('Records updated').. returned by PackageProcedure in DB
                LogInfo($"ReturnInitialise returned P_STATUSCODE={initialiseProcessResult.P_STATUSCODE}, will be showing error to the User.");
                return RedirectToAction("ErrorCustom", "Home");
            }

            //## Return Check API to call to check if the previous month file is completed.
            _ = await CheckPreviousMonthFileIsSubmitted(remttanceId);


            //## Automatch api call
            var autoMatchBO = await Execute_AutoMatchProcess(remttanceId);
            if (autoMatchBO.L_STATUS_CODE == 3)
            {
                TempData["MsgError"] = "Previous month file is still in process by WYPF";
                return RedirectToAction("Index", "Home");
            }

            var totalRecordsInDatabase = int.Parse(TempData["totalRecordsInDatabase"].ToString());
            LogInfo($"totalRecordsInFile: {totalRecordsInFile}, totalRecordsInDatabase: {totalRecordsInDatabase}");
            if (Convert.ToInt32(totalRecordsInFile) < totalRecordsInDatabase || Convert.ToInt32(totalRecordsInFile) > 10000)
            {
                totalRecordsInFile = totalRecordsInDatabase;
            }

            initialiseProcessResultVM.ShowProcessedInfo = "Total records in uploaded file are <b>: " + totalRecordsInFile + "</b><br />"
                              + " Total number of records inserted successfully into database are: <b>" + totalRecordsInDatabase + "</b><br />"
                              + " Employers processed:  <b>" + initialiseProcessResult.P_EMPLOYERS_PROCESSED + "</b><br />";

            initialiseProcessResultVM.ShowMatchingResult = "Total records in uploaded file are <b>: " + totalRecordsInFile + "</b><br />"
                                + " Total number of records inserted successfully into database are: <b>" + totalRecordsInDatabase + "</b><br />"
                                + "Persons Matched : " + autoMatchBO.personMatchCount + "<br />"
                                + "Folders Matched : " + autoMatchBO.folderMatchCount + "<br />";


            LogInfo("Exiting Home/InitialiseProcess() Method.");
            LogInfo("#####################################################\r\n");           

            return View(initialiseProcessResultVM);
        }

        /// <summary>
        /// This new page will prompt the user to initiate the tasks one by one, therefore will have a smooth journey end-to-end.
        /// user will be able to see the progress of the each step- and will not be timed-out- as we can see previously API call timesoout after 100 seconds.
        /// </summary>
        /// <returns></returns>
        //[HttpGet]
        //public IActionResult InitialiseProcessWithProgress()
        //{
        //    var encryptedRemittanceId = ContextGetValue(Constants.SessionKeyRemittanceID);
        //    int remittanceId = Convert.ToInt32(DecryptUrlValue(encryptedRemittanceId));
        //    int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));
        //    var totalRecordsInDatabase = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecordsInDB));
        //    //string errorMessage = TempData["InitialiseProcessError"]?.ToString();

        //    LogInfo($"Loading Home/InitialiseProcessWithProgress() .. RemittanceId: {remittanceId}");
        //    LogInfo($"TotalRecordsInFile: {totalRecordsInFile}, totalRecordsInDatabase: {totalRecordsInDatabase}");

        //    var initialiseProcessResultVM = new InitialiseProcessResultVM()
        //    {
        //        EncryptedRemittanceId = encryptedRemittanceId,
        //        EmployeeName = ContextGetValue(Constants.SessionKeyEmployerName),
        //        ErrorMessage = "",
        //    };

        //    //EventLog_Add(remittanceId, "Waiting for employer to initiate 'ReturnInitialise' Process.", (int)EventType.AwaitingInitialiseProcess, (int)EventType.AwaitingInitialiseProcess);
            
        //    initialiseProcessResultVM.TotalRecordsInFile = totalRecordsInFile.ToString();
        //    initialiseProcessResultVM.TotalRecordsInDatabase = totalRecordsInDatabase.ToString();
        //    initialiseProcessResultVM.EmployersProcessedRecords = "PENDING";

        //    initialiseProcessResultVM.CurrentStep = "ReturnInitialise";

        //    return View(initialiseProcessResultVM);
        //}

        /// <summary>
        /// This is an alternative page for InitialiseProcess()- which has a big workload- all at once..
        /// This new page will prompt the user to initiate the tasks one by one, therefore will have a smooth journey end-to-end
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult InitialiseProcessWithSteps()
        {
            #region Only For TEST
            string encRmitId = EncryptUrlValue("260230");
            ContextSetValue(Constants.SessionKeyTotalRecords, "8517");
            ContextSetValue(SessionKeyTotalRecordsInDB, "8517");
            ContextSetValue(Constants.SessionKeyRemittanceID, encRmitId);
            #endregion

            var encryptedRemittanceId = ContextGetValue(Constants.SessionKeyRemittanceID);
            int remittanceId = Convert.ToInt32(DecryptUrlValue(encryptedRemittanceId));
            int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));
            var totalRecordsInDatabase = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecordsInDB));
            
            string errorMessage = TempData["InitialiseProcessError"]?.ToString();

            LogInfo($"Loading Home/InitialiseProcessWithSteps() .. RemittanceId: {remittanceId}");
            LogInfo($"TotalRecordsInFile: {totalRecordsInFile}, totalRecordsInDatabase: {totalRecordsInDatabase}");

            var initialiseProcessResultVM = new InitialiseProcessResultVM()
            {
                EncryptedRemittanceId = encryptedRemittanceId,
                EmployeeName = ContextGetValue(Constants.SessionKeyEmployerName),
                ErrorMessage = errorMessage,
            };

            var employerProcessedCount = ContextGetValue(Constants.EmployerProcessedCount);

            if (IsEmpty(employerProcessedCount))
            {
                //TODO: Enable is for deployment
                //EventLog_Add(remittanceId, "Waiting for employer to initiate 'ReturnInitialise' Process.", (int)EventType.AwaitingInitialiseProcess, (int)EventType.AwaitingInitialiseProcess);
                employerProcessedCount = "PENDING";
            }

            var returnInitialiseCurrentStep = ContextGetValue(Constants.ReturnInitialiseCurrentStep);
            if (IsEmpty(returnInitialiseCurrentStep))
            {
                returnInitialiseCurrentStep = "ReturnInitialise";
            }

            initialiseProcessResultVM.TotalRecordsInFile = totalRecordsInFile.ToString();
            initialiseProcessResultVM.TotalRecordsInDatabase = totalRecordsInDatabase.ToString();
            initialiseProcessResultVM.EmployersProcessedRecords = employerProcessedCount;

            initialiseProcessResultVM.CurrentStep = returnInitialiseCurrentStep;

            //## if there are less than XXX records- then dont show the UI with progress bar.. Return initialise will be done in 5 seconds..
            _ = int.TryParse(_Configure["RecordCountToSkipProgressiveDisplay"], out int recordCountToSkipProgressiveDisplay);
            if (totalRecordsInFile <= recordCountToSkipProgressiveDisplay)
            {
                return View(initialiseProcessResultVM);
            }
            return View("~/Views/Home/InitialiseProcessWithProgress.cshtml", initialiseProcessResultVM);
        }


        /// <summary>
        /// This will make the call and come back from API.. will not wait for the result..
        /// we will soon send another request after on every 10 seconds to see the proogress.
        /// We will get the RemittanceId from Session Cache
        /// </summary>
        /// <returns></returns>
        [HttpPost]        
        public IActionResult InitialiseProcessCallOnly_Ajax(string id)
        {
            //string userID = CurrentUserId();            
            int remittanceId = Convert.ToInt32(DecryptUrlValue(id));
            int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));

            var initialiseProcBO = new InitialiseProcBO
            {
                P_REMITTANCE_ID = remittanceId,
                P_USERID = CurrentUserId()
            };

            LogInfo($"InitialiseProcessCallOnly_Ajax()-> api: " + _apiEndpoints.InitialiseProc_v2);

            //## ReturnInitialise() call.. a big piece of Task- initialising the entire journey, setting values, generating error/warnings, fixing issues and many things...            
            _ = ApiPost(GetApiUrl(_apiEndpoints.InitialiseProc_v2), initialiseProcBO);        //## api/InitialiseProc_v2

            int recordProcessedInitialValue = 10;  //## this is a initial value for the progress bar.. assuming we have processed already 10 records since it has started.. minimum value to keep the Employers happy..
            double percentProcessed = (recordProcessedInitialValue * 1.00 / totalRecordsInFile) * 100;  

            LogInfo($"InitialiseProcessCallOnly_Ajax() call finished.. working in background! totalRecordsInFile: {totalRecordsInFile}, recordsProcessed: {recordProcessedInitialValue}, Processed %: {percentProcessed}");
            var processingProgress = new RemittanceProcessingProgressVM() {
                Name = Constants.Step1_ReturnInitialise,
                TotalRecords = totalRecordsInFile,
                ProcessedRecords = recordProcessedInitialValue, //## just an initial value                              
            };

            return PartialView("_progressbar", processingProgress);

        }

        /// <summary>This will be called when there are 2000+ records and via ajax,
        /// on every 10 seconds to get the latest progress on record processing- for both 'ReturnInitialise' and 'AutoMatch' processes
        /// </summary>
        /// <returns>A partial view with progress bar and related progression values</returns>
        [HttpGet, Route("Home/CheckProgress_Periodically_ReturnInitialise_Ajax/{stepName}")]
        public IActionResult CheckProgress_Periodically_ReturnInitialise_Ajax(string stepName)
        {
            string encryptedRemittanceId = ContextGetValue(Constants.SessionKeyRemittanceID);
            int remittanceId = Convert.ToInt32(DecryptUrlValue(encryptedRemittanceId));            

            LogInfo($"Home/CheckProgress_Periodically_ReturnInitialise_Ajax(), processName: {stepName}");
            
            string progressCheckApiUrl = stepName.ToLower().Equals("step1") ? _apiEndpoints.GetProgressForReturnInitialise: _apiEndpoints.GetProgressForAutoMatch;
            
            var apiResult = ApiGet_NonAsync(GetApiUrl(progressCheckApiUrl + remittanceId));        //## api/GetProgressForReturnInitialise
            LogInfo($"stepName: {stepName}, apiResult: {apiResult}");

            var processingProgress = JsonConvert.DeserializeObject<List<RemittanceProcessingProgressVM>>(apiResult);
            
            if(processingProgress is null || !processingProgress.Any() )
            {
                //## ONLY FOR TEST / DEMO- WHILE WE HAVE SET SLOW/DELAY IN HE SQL - WE WILL NOT GET ANY RESULT FOR THE FIRST CALL FOR Auto_Match.. Known case..
                if (stepName == Constants.Step2_AutoMatch) {

                    var initialProgress = GetInitialProgress();
                    return PartialView("_progressbar", initialProgress);
                }

                LogInfo($"error while fetching process status for: " + stepName);
                return Json("error while fetching process status for: " + stepName);
            }

            var currentProgress = processingProgress.First();

            LogInfo($"{stepName} => totalRecordsInFile: {currentProgress.TotalRecords}, recordsProcessed: {currentProgress.ProcessedRecords}");

            currentProgress.Name = stepName;
            //## Return_Check Process- once Return_Initialise process is completed successfully
            if (stepName == Constants.Step1_ReturnInitialise) {
                if (currentProgress.IsCompleted()){                   
                    LogInfo($"Step1-> Return_Initialise is complete. {currentProgress.ProcessedRecords} / {currentProgress.TotalRecords}. Start 'Return_Check()' process now.");
                    _ = Execute_Return_Check_Process(remittanceId, CurrentUserId());    //## intentionally calling as Non-async.. so- it will stay in ASP.net and not go back to UI to AJAX...
                }
            }

            if(stepName == Constants.Step2_AutoMatch)
            {
                ClearRemittance_SessionCookies();
                if (currentProgress.IsCompleted()) {
                    LogInfo($"Step2-> Auto_Match is complete. {currentProgress.ProcessedRecords} / {currentProgress.TotalRecords}, Member matched: {currentProgress.Members_Matched}, Folder Matched: {currentProgress.Folders_Matched}");
                }
            }

            return PartialView("_progressbar", currentProgress);

        }


        /// <summary>This POST method will only be called when there are less than 2000 records.. otherwise another process will be called by Ajax</summary>
        /// <param name="remittanceId"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> InitialiseProcessWithSteps(string remittanceId)
        {
            string userID = CurrentUserId();
            var encryptedRemittanceId = ContextGetValue(Constants.SessionKeyRemittanceID);
            int remttanceId = Convert.ToInt32(DecryptUrlValue(encryptedRemittanceId));

            LogInfo($"Post -> Home/InitialiseProcessWithSteps()");

            //## ReturnInitialise() call.. a big piece of Task- initialising the entire journey, setting values, generating error/warnings, fixing issues and many things... 
            var initialiseProcessResult = await Execute_ReturnInitialiseProcess(userID, remttanceId);

            if (initialiseProcessResult.P_STATUSCODE == 9)
            {
                //## value '0' means all good ('Records updated').. returned by PackageProcedure in DB
                LogInfo($"ReturnInitialise returned P_STATUSCODE={initialiseProcessResult.P_STATUSCODE}, File maybe corrupted! Terminate the process now.");
                return RedirectToAction("ErrorCustom", "Home");

            }
            else if (initialiseProcessResult.P_STATUSCODE != 0)
            {
                //## value '0' means all good ('Records updated').. returned by PackageProcedure in DB
                LogInfo($"ReturnInitialise returned P_STATUSCODE={initialiseProcessResult.P_STATUSCODE}, will be showing error to the User.");
                TempData["InitialiseProcessError"] = $"Error: Status = {initialiseProcessResult.P_STATUSCODE}-{initialiseProcessResult.P_STATUSTEXT}. Remttance: {remttanceId}. Please contact IT Support with a screenshot of this page.";
                return RedirectToAction("InitialiseProcessWithSteps");

            }

            //## this will update the score- from 50->60 and then 60->70. AUTO_MATCH will update the score later to 80.
            var isReturnChecked = await Execute_Return_Check_Process(remttanceId, userID);  

            ContextSetValue(Constants.EmployerProcessedCount, initialiseProcessResult.P_RECORDS_PROCESSED.ToString());
            ContextSetValue(Constants.ReturnInitialiseCurrentStep, "Auto_Match");

            return RedirectToAction("InitialiseProcessWithSteps");
        }


        /// <summary>The following will be used/called from the "InitialiseProcessWithProgress" page.. where Total records are > 2000. Will show a progress bar while processing a large file..</summary>
        /// <returns></returns>
        [HttpGet,HttpPost]
        public ActionResult InitialiseAutoMatchProcess_CallOnly_Ajax()
        {
            var encryptedRemittanceId = ContextGetValue(Constants.SessionKeyRemittanceID);
            int remittanceId = Convert.ToInt32(DecryptUrlValue(encryptedRemittanceId));

            apiBaseUrlForAutoMatch = GetApiUrl(_apiEndpoints.AutoMatch_V2);    //## api/AutoMatchRecords_V2
            LogInfo($"InitialiseAutoMatchProcessByAjax() -> Calling Automatch api: {apiBaseUrlForAutoMatch}{remittanceId}");

            //## lets not make a async/await call. we make the call and move forward without waiting for the result...
            _ = ApiGet($"{apiBaseUrlForAutoMatch}{remittanceId}");  //## we are not waiting here for the result.. let it continue in the background

            int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));

            int recordProcessedInitialValue = 10;  //## this is a initial value for the progress bar.. assuming we have processed already 10 records since it has started.. minimum value to keep the Employers happy..
            double percentProcessed = (recordProcessedInitialValue * 1.00 / totalRecordsInFile) * 100;

            LogInfo($"AutoMatch_V2 call finished.. working in background! totalRecordsInFile: {totalRecordsInFile}, recordsProcessed: {recordProcessedInitialValue}, Processed %: {percentProcessed}");
            var processingProgress = new RemittanceProcessingProgressVM()
            {
                Name = Constants.Step2_AutoMatch,                
                TotalRecords = totalRecordsInFile,
                ProcessedRecords = recordProcessedInitialValue, //## just an initial value                              
            };

            return PartialView("_progressbar", processingProgress);

        }


        /// <summary>The following will be used/called from the "InitialiseProcessWithSteps" page.. where Total records are < 2000</summary>
        [HttpGet, HttpPost]
        public async Task<ActionResult> InitialiseAutoMatchProcessByAjax()
        {
            var encryptedRemittanceId = ContextGetValue(Constants.SessionKeyRemittanceID);
            int remttanceId = Convert.ToInt32(DecryptUrlValue(encryptedRemittanceId));

            var autoMatchResult = await Execute_AutoMatchProcess(remttanceId);
            var taskResult = new TaskResults()
            {
                IsSuccess = autoMatchResult.L_STATUS_CODE == 0,
                Message = $"<i class='fas fa-users mx-2 mx-2'></i> Persons Matched: {autoMatchResult.personMatchCount}<br />"
                    + $"<i class='fas fa-folder-open mx-2'></i> Folders Matched: {autoMatchResult.folderMatchCount}<br />"
            };

            if (autoMatchResult.L_STATUS_CODE != 0)
            {
                taskResult.Message = $"Error: {autoMatchResult.L_STATUS_CODE} - {autoMatchResult.L_STATUS_TEXT}";
            }

            return Json(taskResult);
        }

        [Obsolete("We will not need this anymore.. UI has changed..")]
        private async Task<AutoMatchBO> Execute_AutoMatchProcess(int remittanceId)
        {
            apiBaseUrlForAutoMatch = GetApiUrl(_apiEndpoints.AutoMatch);    //## api/AutoMatchRecords

            ContextRemoveValue(Constants.ReturnInitialiseCurrentStep); //## Remove the value.. so system will reset to original value- whatever it was before..
            ContextRemoveValue(Constants.EmployerProcessedCount);
            ContextRemoveValue(Constants.SessionKeyTotalRecords);
            ContextRemoveValue(Constants.SessionKeyTotalRecordsInDB);

            LogInfo($"Calling Automatch api: {apiBaseUrlForAutoMatch}. / {remittanceId}");
                        
            string apiResult = await ApiGet($"{apiBaseUrlForAutoMatch}{remittanceId}");
            AutoMatchBO autoMatchResult = JsonConvert.DeserializeObject<AutoMatchBO>(apiResult);
                
            LogInfo($"Automatch executed. autoMatchBO.L_STATUS_CODE: {autoMatchResult.L_STATUS_CODE} - {autoMatchResult.L_STATUS_TEXT}.");

            return autoMatchResult;

        }

        /// <summary>This updates the Remittance score respectively and checks if we have a submission of the previous month.
        /// For the Statuses- 50-70 -> it runs 'RETURN_CHECK_INITIAL_STAGE' and updates the score to a higher one..
        /// It may not go to Status:70 (Ready to AutoMatch)- if a prev month file is missing. Which will require to run "Manual_Match" from Dashboard
        /// </summary>
        /// <param name="remttanceId"></param>
        /// <returns></returns>
        private async Task<bool> Execute_Return_Check_Process(int remttanceId, string userID)
        {
            //Return Check API to call to check if the previous month file is completed ppse
            ReturnCheckBO result = new()
            {
                p_REMITTANCE_ID = remttanceId,
                P_USERID = userID,
                P_PAYLOC_FILE_ID = 0
            };

            string apiBaseUrlForCheckReturn = GetApiUrl(_apiEndpoints.ReturnCheckProc);      //## Get 'status of a file'
            LogInfo($"Execute_Return_Check_Process()-> apiBaseUrlForCheckReturn - {apiBaseUrlForCheckReturn} -> remttanceId {remttanceId}", true);


            var apiResult = await ApiPost(apiBaseUrlForCheckReturn, result);
            result = JsonConvert.DeserializeObject<ReturnCheckBO>(apiResult);   //## we get StatusCode and StatusText back, ie: 1 = Record Not Found.. 

            LogInfo($"Execute_Return_Check_Process() Finished, status: {result.L_STATUSTEXT}");
            return true;
        }


        /// <summary>Return Check API to call to check if the previous month file is completed.</summary>
        /// <param name="remttanceId"></param>
        /// <returns></returns>
        private async Task<bool> CheckPreviousMonthFileIsSubmitted(int remttanceId)
        {
            LogInfo($"Skipping  'Task<bool> CheckPreviousMonthFileIsSubmitted', as this have no followup action, means- this check is useless. remttanceId {remttanceId}");
            return true;

            ReturnCheckBO result = new()
            {
                p_REMITTANCE_ID = remttanceId,
                P_USERID = CurrentUserId(),
                P_PAYLOC_FILE_ID = 0
            };

            LogInfo($"apiBaseUrlForCheckReturn - remttanceId {remttanceId}");

            string apiBaseUrlForCheckReturn = GetApiUrl(_apiEndpoints.ReturnCheckProc);      //## Get 'status of a file'
            string apiResult = await ApiPost(apiBaseUrlForCheckReturn, result);
            result = JsonConvert.DeserializeObject<ReturnCheckBO>(apiResult);   //## we get StatusCode and StatusText back, ie: 1 = Record Not Found.. 

            LogInfo($"apiBaseUrlForCheckReturn Finished, status: {result.L_STATUSTEXT}");

            return true;
            //Add functionality here to restrict file if the previous month file is still pending.
        }


        /// <summary>Initialising the entire journey, setting values, fixing issues and many things
        /// </summary>
        /// <param name="userID">User Id</param>
        /// <param name="remttanceId">Remittance ID</param>
        /// <returns>True/False on success</returns>
        private async Task<InitialiseProcBO> Execute_ReturnInitialiseProcess(string userID, int remttanceId)
        {
            //######################################
            //## Actual ReturnInitialise() Process
            //######################################
            var initialiseProcBO = new InitialiseProcBO
            {
                P_REMITTANCE_ID = remttanceId,
                P_USERID = userID
            };

            LogInfo($"Execute_ReturnInitialiseProcess(): initialising the journey, Error/warning generation and other tasks to set the Status, Scores, etc");

            //## Following is a big piece of Task- initialising the journey, setting values, fixing issues and many things... 
            string initialiseProcApi = GetApiUrl(_apiEndpoints.InitialiseProc);     //## api/InitialiseProc
            string apiResult = await ApiPost(initialiseProcApi, initialiseProcBO);
            initialiseProcBO = JsonConvert.DeserializeObject<InitialiseProcBO>(apiResult);
            LogInfo($"ReturnInitialise: Finished. initialiseProcBO.P_STATUSCODE: {initialiseProcBO.P_STATUSCODE}.");

            //## need to add a check to see what is the outcome of 'apiBaseUrlForInitialiseProc' API call..
            if (initialiseProcBO.P_STATUSCODE == 9)
            {
                //## abort mission... corrupted data found..
                var errorViewModel = new ErrorViewModel()
                {
                    ApplicationId = Constants.EmployersPortal,
                    ErrorPath = $"/Home/InitialiseProcessWithSteps/{remttanceId}",
                    Message = "Return Initialise failed with StatusCode 9.",
                    RemittanceInfo = remttanceId.ToString(),
                    RequestId = "0",
                    Source = initialiseProcApi,
                    UserId = CurrentUserId(),
                    DisplayMessage = "The supplied file seems to have corrupt data therefore Insert operation failed. Please try to upload a file with simplified data."
                };

                string cacheKey = $"{CurrentUserId()}_{Constants.CustomErrorDetails}";
                _cache.Set(cacheKey, errorViewModel);

                eBO.remittanceID = remttanceId;
                eBO.remittanceStatus = 1;
                eBO.eventTypeID = 4;
                eBO.notes = errorViewModel.Message;
                InsertEventDetails(eBO);                
            }

            //return initialiseProcBO.P_STATUSCODE == 0;  //## value '0' means all good ('Records updated').. returned by Packagerocedure in DB
            return initialiseProcBO;
        }
      

        /// <summary>Make sure all records are inserted in the DB.Table- before trying to run the Initialise Process.
        /// </summary>
        /// <param name="remttanceId"></param>
        /// <returns></returns>
        private async Task<bool> CheckAllRecordsAreInsertedInDB(int remttanceId)
        {
            string getTotalRecordsApi = GetApiUrl(_apiEndpoints.TotalRecordsInserted);
            var apiResult = await ApiGet($"{getTotalRecordsApi}{remttanceId}");

            int total = JsonConvert.DeserializeObject<int>(apiResult);
            string totalRecordsInF = ContextGetValue(Constants.SessionKeyTotalRecords);
            LogInfo($"TotalRecordsInserted: {total}, and totalRecordsInFile: {totalRecordsInF}");

            //### following loop will attempt 5 times to find records in the table. //Windows bulk insertion service submits only 10000 records at time so I Need to keep check until all the records inserted.
            int maxRetry = 5; int attempCount = 1;
            while (total == 0 || Convert.ToInt32(totalRecordsInF) > total)
            {
                if (attempCount >= maxRetry)
                { //## need to give up after 5 attempts.. means 15 seconds.. enough to assume system was crashed while doing BulkInsert()
                    attempCount = maxRetry;
                }
                LogInfo("\r\nInside ReturnInitialise().. but Database hasn't finished inserting all records yet. adding a 3 Seconds. Ateempt: " + attempCount);
                System.Threading.Thread.Sleep(3000);
                apiResult = await ApiGet($"{getTotalRecordsApi}{remttanceId}");
                total = JsonConvert.DeserializeObject<int>(apiResult);

                attempCount++;
            }
            //## these are the temporary data transfer variables between Action/Controllers...
            //TempData["totalRecordsInFile"] = totalRecordsInF;
            TempData["totalRecordsInDatabase"] = total;

            //## by now all 5 attempts are made and we should have all records in the DB... same as Excel-DB Table should have same number of rows
            return total >= int.Parse(totalRecordsInF);
        }

        /// <summary>
        /// following action is not in use
        /// </summary>
        /// <param name="remID"></param>
        /// <returns></returns>
        public IActionResult LooseMatchRecords(int remID)
        {
            return RedirectToAction("InitialiseProcess", new { id = remID });
        }

        public IActionResult Privacy()
        {
            return View();
        }



        /// <summary>
        /// List to show months in dropdown menu
        /// </summary>
        /// <returns></returns>
        public List<string> GetMonths()
        {
            var monthList = _Configure["ValidPayrollMonths"];
            var nameOfMonths = monthList.Split(",").ToList();
            return nameOfMonths;
        }


        
        /// <summary>
        /// Populate the Financial Year list and return in a Descending sorted List<String>
        /// </summary>
        /// <returns>List of strings</returns>
        public List<string> GetYears()
        {
            string cacheKey = Constants.ValidPayrollYears;            
            var payrollYears = _cache.Get<List<string>>(cacheKey);

            if (payrollYears is null || payrollYears.Count < 1) {
                payrollYears = new List<string>();
                int payrollYearsStartFrom = Convert.ToInt16(ConfigGetValue("PayrollYearsStartFrom"));

                for (int i = payrollYearsStartFrom; i <= DateTime.Today.Year; i++)
                {
                    var newFinancialYear = $"{i}/{(i - 2000) + 1}";
                    payrollYears.Add(newFinancialYear);   //## ie: 2023/24
                }
                //## this will add the current year as well.. but if we are in Jan/Feb/March 2024 - then we still don't need 2024/25, until current month is >= 4.
                if (DateTime.Today.Month < 4)
                {
                    payrollYears.RemoveAt(payrollYears.Count - 1);
                }
                var sortedList = payrollYears.ToArray().Reverse().ToList();
                _cache.Set(cacheKey, sortedList);

                return sortedList;
            }

            return payrollYears;




        }


        /// <summary>
        /// following method will show list of sub payroll provider available for a User to Upload a file
        /// </summary>
        /// <param name="w2UserId">w2User id, not UPM.LoginName</param>
        /// <returns></returns>
        private async Task<List<PayrollProvidersBO>> GetPayrollProviderListByUser(string w2UserId)
        {
            string apiBaseUrlForSubPayrollProvider = GetApiUrl(_apiEndpoints.SubPayrollProvider);

            var apiResult = await ApiGet(apiBaseUrlForSubPayrollProvider + w2UserId);
            var subPayrollList = JsonConvert.DeserializeObject<List<PayrollProvidersBO>>(apiResult);

            return subPayrollList;
        }


        private TaskResults ConvertExcelFileToCsv(string filePathName)
        {
            //## Convert the Excel file to CSV first
            var csvPath = Path.GetDirectoryName(filePathName) + "/csv/";
            var csvFileName = Path.GetFileNameWithoutExtension(filePathName) + ".csv";
            var csvFilePath = csvPath + csvFileName;
            var isConverted = ConvertExcelToCsv.Convert(filePathName, csvFilePath);

            var result = new TaskResults { IsSuccess = false, Message = string.Empty };

            if (isConverted == false)
            {
                result.Message = "File format error. Please try another file.";
                return result;
            }

            ContextSetValue(Constants.Staging_CSV_FilePathKey, csvFilePath);
            
            result.IsSuccess = true;
            return result;

        }

        /// <summary>
        /// This will convert the Excel to CSV file, then look for any malicious scripts in the file.
        /// Also checks for Empty Rows in the Excel sheet
        /// If found- return with error. Otherwise- returns empty text
        /// </summary>
        /// <param name="filePathName">Excel file name to check</param>
        /// <returns>File processing error</returns>
        private TaskResults CheckMaliciousScripts_And_EmptyRows()
        {
            var result = new TaskResults { IsSuccess = false, Message = string.Empty };

            string filePathName = ContextGetValue(Constants.Staging_CSV_FilePathKey);

            var maliciousTags = ConfigGetValue("MaliciousTags").Split(",");

            var contents = System.IO.File.ReadAllText(filePathName).Split('\n');
            int rowCounter = 1;
            List<int> rowNumberList = new();
            List<string> newCsv = new();

            foreach (var item in contents)  //## read line by line Rows in the CSV/ExcelSheet
            {
                foreach (var tag in maliciousTags)  //## Read each invalid characters listed in the Config file
                {
                    if (item.ToLower().Contains(tag))
                    {
                        result.Message = "Error: Invalid characters found in the file. Please remove the invalid symbols from the file and try again. <br/>Please avoid symbols, ie: <h3>" + string.Join(" , ", maliciousTags) + "</h3>";
                        return result;
                    }
                }

                //## Check for empty Excel rows- which are simply some commas without values in a CSV file, ie: ",,,,,,,," You can view this opening the file with Notepad                    
                if (item.ToLower().StartsWith(",,,") || item.ToLower()=="\r") //## at least 3 commas means- there are and will be more empty cells.. which makes empty rows
                {
                    //result.Message = "<i class='fas fa-exclamation-triangle mr-4 fa-lg'></i><div class='h4'>Error: There are empty rows found in the Excel file. Please delete the empty rows and try again.</div>";
                    
                    rowNumberList.Add(rowCounter); //## This is the Text holding all Empty Row numbers, to display the user.. ie: "2,5,8"                    
                }

                rowCounter++;

                newCsv.Add(item);
            }
            

            //## if Empty rows found and we have generated a string with the list of Row numbers- then finish the end Tag for <div>
            if (rowNumberList.Any()) {
                string warningMessage = "<i class='fas fa-exclamation-triangle mr-4 fa-lg'></i><div class='h4'>Error: Empty rows found in the Excel file. Rows: " + string.Join(", ", rowNumberList) + "</div>";

                result.Message = $"{warningMessage}<div class='h5 text-primary'>Please delete the empty rows and try again.</div>";
                return result;
            }


            //## now check the header row has all the correct Field name. If any field name has been modified- we will not be able to map and insert in the DB
            string headerRow = contents[0];
            TaskResults isTemplateValid = InputFile_ColumnNamesAreValid(headerRow);
            if (isTemplateValid.IsSuccess == false)
            {
                return isTemplateValid;
            }

            //################# WE MAY NOT NEED THIS.. DELETE THIS LATER IF NOT BENEFICIARY #############
            //Rewrite the CSV file replacing all the '£' symbol- and then use it to cast to Class Object
            using (StreamWriter swOutputFile = new StreamWriter(new FileStream(filePathName, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                foreach (var line in newCsv)
                {
                    swOutputFile.WriteLine(line.Replace("£", "").Replace("  ", " ").Replace("\r", ""));
                }
            }

            //# don't delete the file after contents check. We need this file on several ocassions later.
            //## one is- to check PayrollYear and Month values. for a '2nd Month posting'- we need to verify that all records in Excelsheet are for the same Year/Month selected in the UI

            result.IsSuccess = IsEmpty(result.Message);
            return result;  //## success! All good!
        }

        private TaskResults InputFile_ColumnNamesAreValid(string headerRow)
        {
            var inputFileFieldNames = headerRow.Replace("\r", "").Split(",");
            var templateFieldNames = _Configure["TemplateFieldNames"];

            var result = new TaskResults { IsSuccess = true, Message = string.Empty };

            LogInfo("Checking InputFile_ColumnNamesAreValid()");

            foreach (var templateField in templateFieldNames.Split(","))
            {
                if(!inputFileFieldNames.Contains(templateField))
                {
                    result.Message = _Configure["FileUploadInvalidTemplate_ErrorMessage"] + _Configure["FileUploadTemplate_DownloadLink"];
                    result.IsSuccess = false;
                    LogInfo($"Invalid column name in the Excel file. Missing field: {templateField}");
                    break;
                }
            }
            
            return result;
        }


        /// <summary>
        /// This will check the File contents, size and Type. The it will look for any malicious/invalid characters in the file. If found- return the error message
        /// </summary>
        /// <param name="formFile">CustomerFile</param>
        /// <returns>Test result</returns>
        [HttpPost]
        public async Task<IActionResult> ValidateFile(IFormFile formFile)
        {
            LogInfo($"Task<IActionResult> ValidateFile, formFile: {formFile.Name}");

            if (formFile == null)
            {
                return Json("No file added. Please add a valid file and try again.");
            }

            var fileTypeCheck = IsFileValid(formFile);
            LogInfo($"File is valid type");

            //## check whether the file is valid- if NOT- then don't proceed with any other checks.. just exit now...
            if (fileTypeCheck.IsSuccess == false)
            {
                return Json(fileTypeCheck.Message);
            }

            string fileExt = Path.GetExtension(formFile.FileName);
            string fileNameForUpload = $"{Guid.NewGuid().ToString()}{fileExt}";

            string filePathName = Path.Combine(_customerUploadsLocalFolder, fileNameForUpload);
            using (FileStream fileStream = new(filePathName, FileMode.Create))
            {
                await formFile.CopyToAsync(fileStream);
                _logger.LogInformation("File is copied to local folder.");
            }
            
            var convertResult = ConvertExcelFileToCsv(filePathName);
            if (convertResult.IsSuccess) { 
                var scriptsCheck = CheckMaliciousScripts_And_EmptyRows();

                //## Now check results for malicious script Validations..
                if (scriptsCheck.IsSuccess)
                {
                    _cache.Set(GetKeyName(UploadedExcelFilePathKey), fileNameForUpload); //## saving the staging Excel file name in Redis.
                    return Json("success");
                }

                string errorMessage = convertResult.Message + " " + scriptsCheck.Message;
                return Json(errorMessage);
            }
            //## the following shouldn't happen.. anyway- not bad to have an error message
            return Json("Failed to transform the Excel file. Please use another file and try again.");
        }


        private string RenameFileName(string newFileName)
        {
            string currentFileName = _customerUploadsLocalFolder + _cache.Get<string>(GetKeyName(Constants.UploadedExcelFilePathKey));
            string fileExt = Path.GetExtension(currentFileName);

            string fileNameForUpload = $"{_customerUploadsLocalFolder}{newFileName}{fileExt}";
            System.IO.File.Move(currentFileName, fileNameForUpload);

            LogInfo($"GenerateFileName.fileNameForUpload: '{fileNameForUpload}'");
            fileNameForUpload = $"{newFileName}{fileExt}";
            _cache.SetString(GetKeyName(Constants.UploadedExcelFilePathKey), fileNameForUpload);

            return fileNameForUpload;
        }

        //private string GenerateFilePathNameForUpload(IFormFile customerFile)
        //{
        //    //var fileNameForUpload = Path.GetFileNameWithoutExtension(customerFile.FileName);
        //    string fileExt = Path.GetExtension(customerFile.FileName);

        //    string fileNameForUpload = $"{Guid.NewGuid().ToString()}{fileExt}";

        //    string filePath = Path.Combine(_customerUploadsLocalFolder, fileNameForUpload);

        //    LogInfo($"GenerateFilePathNameForUpload.filePath: '{filePath}'");

        //    return filePath;
        //}

        /// <summary>
        /// If “2nd posting for same month“ is selected- then Validate process should check the uploaded excel sheet - there should be records only for the selected Month/Year
        /// </summary>
        /// <param name="payrollYear">Selected Payroll Year</param>
        /// <param name="payrollMonth">Selected Payroll Month</param>
        /// <param name="selectedPayLocationId">Selected PayLocationId</param>
        /// <returns>True/False if valid</returns>
        private bool IsA_Valid2ndMonthPosting(string payrollYear, string payrollMonth, string selectedPayLocationId)
        {
            var remittanceRecords = _cache.Get<List<ExcelsheetDataVM>>(GetKeyName(Constants.ExcelData_ToInsert));

            if (remittanceRecords.Any(r => r.PAYROLL_PD.ToUpper().Contains(payrollMonth) == false) || remittanceRecords.Any(r => r.PAYROLL_YR != payrollYear) || remittanceRecords.Any(r => r.EMPLOYER_LOC_CODE != selectedPayLocationId))
            {
                string fileUploadErrorMessage2ndPosting = _Configure["FileUploadErrorMessage2ndPosting"];
                _cache.SetString(GetKeyName(Constants.FileUploadErrorMessage), fileUploadErrorMessage2ndPosting);
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Once we are at Step2->Auto_Match process-> Remove the session values.. so no data conflict with next submission. 
        /// </summary>
        private void ClearRemittance_SessionCookies()
        {
            ContextRemoveValue(Constants.ReturnInitialiseCurrentStep);
            ContextRemoveValue(Constants.EmployerProcessedCount);
            ContextRemoveValue(Constants.SessionKeyTotalRecords);
            ContextRemoveValue(Constants.SessionKeyTotalRecordsInDB);
        }


        private RemittanceProcessingProgressVM GetInitialProgress()
        {
            var totalRecordsInDatabase = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecordsInDB));

            var initialProgress = new RemittanceProcessingProgressVM()
            {
                TotalRecords = totalRecordsInDatabase,
                ProcessedRecords = 1,
                Folders_Matched = 0,
                Members_Matched = 0,   
                Name = "Step2",               
            };

            return initialProgress;
        }
    }
}
