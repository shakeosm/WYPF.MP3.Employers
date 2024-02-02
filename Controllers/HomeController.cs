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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        //string remittanceInsertApi = string.Empty;
        string apiBaseUrlForAutoMatch = string.Empty;
        EventDetailsBO eBO = new EventDetailsBO();

        DataTable excelDt;
        MyModel modelDT = new MyModel();

        public ICommonRepo _commonRepo;
        public IValidateExcelFile _validateExcel;
        private readonly IInsertDataTable _insertDataTable;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment host, IConfiguration configuration, IRedisCache Cache, IDataProtectionProvider Provider, IOptions<ApiEndpoints> ApiEndpoints, ICommonRepo CommonRepo, IValidateExcelFile ValidateExcelFile, IInsertDataTable InsertDataTable) : base(configuration, Cache, Provider, ApiEndpoints)
        {
            _logger = logger;
            _host = host;
            _Configure = configuration;
            _commonRepo = CommonRepo;
            _validateExcel = ValidateExcelFile;
            _insertDataTable = InsertDataTable;
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
            string userName = CurrentUserId();

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

            List<PayrollProvidersBO> subPayList = await CallPayrollProviderService(userName);
            //staff login for frontend to upload a file for Employers causing an issue where it is getting a "-" in list which 
            //throws an exception.

            subPayList = subPayList.Where(x => x.pay_location_ID != null).ToList();


            //## do we have an error message from previous FileUpload attempt? This page maybe loading after a Index_POST() call failed and redirected back here..
            string fileUploadErrorMessage = _cache.GetString(Constants.FileUploadErrorMessage);
            //## Delete the message once Read.. otherwise- this will keep coming on every page request...
            _cache.Delete(Constants.FileUploadErrorMessage);

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
            string userName = string.Empty;
            userName = CurrentUserId();
            string monthSelected = ContextGetValue(Constants.SessionKeyMonth) ?? string.Empty;
            string yearSelected = ContextGetValue(Constants.SessionKeyYears) ?? string.Empty;
            string postSelected = ContextGetValue(Constants.SessionKeyPosting) ?? string.Empty;

            List<PayrollProvidersBO> subPayList = await CallPayrollProviderService(userName);

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
                UserId = HttpContext.Session.GetString(Constants.LoggedInAsKeyName),
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
                _cache.SetString(Constants.FileUploadErrorMessage, "Error: You must select 'Year', 'Month' and 'PostType' to continue");
                return RedirectToAction("Index", "Home");
            }

            var fileCheck = IsFileValid(vm.PaymentFile);

            if (!fileCheck.IsSuccess)
            {
                _cache.SetString(Constants.FileUploadErrorMessage, fileCheck.Message);
                return RedirectToAction("Index", "Home");
            }

            //Add selected name of month into Session, filename and total records in file.
            HttpContext.Session.SetString(Constants.SessionKeyMonth, vm.SelectedMonth);
            HttpContext.Session.SetString(Constants.SessionKeyYears, vm.SelectedYear);
            HttpContext.Session.SetString(Constants.SessionKeyPosting, vm.SelectedPostType.ToString());
            HttpContext.Session.SetString(Constants.SessionKeySchemeName, SessionSchemeNameValue);

            //TotalRecordsInsertedAPICall apiCall = new TotalRecordsInsertedAPICall();

            string userId = CurrentUserId();// ContextGetValue(SessionKeyUserID);
            var currentUser = await GetUserDetails(userId);
            string empName = currentUser.Pay_Location_Name;
            string empID = currentUser.Pay_Location_Ref;// ContextGetValue(Constants.SessionKeyPayrollProvider);

            var submissionPeriod = new CheckFileUploadedBO
            {
                P_Month = vm.SelectedMonth,
                P_Year = vm.SelectedYear,
                P_EMPID = empID
            };

            //Member titles coming from config - 
            string[] validTitles = ConfigGetValue("ValidMemberTitles").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //Check all invalid signs from file and show error to employer
            string[] invalidSigns = ConfigGetValue("SignToCheck").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            //update Event Details table File is uploaded successfully.
            string apiCheckFileIsUploadedUrl = GetApiUrl(_apiEndpoints.CheckFileIsUploaded);   //## api/CheckFileUploaded

            //check if records were uploaded previously for the selected month and year.
            //int fileAvailableCheck = await apiCall.CheckFileAvailable(fileCheckBO, apiBaseUrlForCheckFileAvailable);
            var apiResult = await ApiPost(apiCheckFileIsUploadedUrl, submissionPeriod);
            int fileAlreadyUploaded = JsonConvert.DeserializeObject<int>(apiResult);

            string fileExt = string.Empty;
            string filePath = string.Empty;
            string spreadSheetName = string.Empty;
            string errorMessage = string.Empty;
            //bool answer = true;
            // string fileNameWithoutExt = string.Empty;
            //string webRootPath = _host.WebRootPath;
            //MyModel model = new MyModel();
            //get list of paylocations
            List<PayrollProvidersBO> subPayList = await CallPayrollProviderService(userId);
            //bypass year and month check 

            if (vm.SelectedPostType == (int)PostingType.First)
            {
                if (fileAlreadyUploaded == 1)
                {
                    //TempData["MsgM"] 
                    _cache.SetString(Constants.FileUploadErrorMessage, $"File is already uploaded for the month: {vm.SelectedMonth} and payrol period: {vm.YearList} <br/> You can goto Dashboard and start process on file from there. ");
                    return RedirectToAction("Index", "Home");
                }
            }

            if (excelDt != null)
            {
                excelDt.Clear();
            }

            if (!Path.Exists(_customerUploadsLocalFolder))
            {
                _cache.SetString(Constants.FileUploadErrorMessage, "Error: File upload area not defined. Please contact support.");
                return RedirectToAction("Index", "Home");
            }

            filePath = GenerateFilePathNameForUpload(vm.PaymentFile, fileNamePrefix: $"{empName.Replace(" ", "-")}_{vm.SelectedYear.Replace("/", "-")}_{vm.SelectedMonth}_{vm.SelectedPostType}_");
            using (FileStream fileStream = new(filePath, FileMode.Create))
            {
                await vm.PaymentFile.CopyToAsync(fileStream);
            }
            LogInfo($"File copied to local path: {filePath}");


            //## malicious script check
            //TODO: Use NugetPackage for Excel operation: 'https://www.nuget.org/packages/CsvHelper.Excel.Core/'
            var result = CheckMaliciousScripts_And_EmptyRows(filePath);
            LogInfo($"CheckMaliciousScripts_And_EmptyRows complete. result: {result}");

            if (result.IsSuccess == false)
            {
                System.IO.File.Delete(filePath);
                _cache.SetString(Constants.FileUploadErrorMessage, result.Message);
                return RedirectToAction("Index", "Home");
            }

            try
            {
                //## This will Convert the Excel sheet to a System.Data.DataTable to make it convenient to validate fields
                LogInfo($"ConvertExcelToDataTable");
                excelDt = _commonRepo.ConvertExcelToDataTable(filePath, out errorMessage);
                LogInfo($"ConvertExcelToDataTable finished");

                //## all good.. now store this filePath in the Redis cache for further processing, if required
                _cache.SetString($"{CurrentUserId()}_{UploadedFilePathKey}", filePath);
            }
            catch (Exception ex)
            {
                _cache.SetString(Constants.FileUploadErrorMessage, "Error transforming the excel file. Please check all column 'Names' are correct and NO empty rows in the sheet." + ex.Message);
                return RedirectToAction("Index", "Home");
            }


            if (excelDt is null)
            {
                _cache.SetString(Constants.FileUploadErrorMessage, errorMessage);
                return RedirectToAction("Index", "Home");
            }

            LogInfo($"change column heading Name");
            // change column heading Name
            if (!_commonRepo.ChangeColumnHeadings(excelDt, out errorMessage))
            {
                _cache.SetString(Constants.FileUploadErrorMessage, errorMessage);
                return RedirectToAction("Index", "Home");
            }
            LogInfo($"change column heading Name: Finished");

            // convert all fields in data table to string
            modelDT.stringDT = _commonRepo.ConvertAllFieldsToString(excelDt, userId);

            int numberOfRows = modelDT.stringDT.Rows.Count;

            LogInfo($"ConvertAllFieldsToString: Finished. numberOfRows: {numberOfRows}");

            //Add selected name of month into Session, filename and total records in file.
            //HttpContext.Session.SetString(Constants.SessionKeyMonth, vm.SelectedMonth.ToString());
            //HttpContext.Session.SetString(Constants.SessionKeyYears, vm.SelectedYear.ToString());

            var fileNameForUpload = Path.GetFileName(filePath);
            HttpContext.Session.SetString(Constants.SessionKeyFileName, fileNameForUpload);
            HttpContext.Session.SetString(Constants.SessionKeyTotalRecords, numberOfRows.ToString());

            //## store the user selection in the cache- so we can set the as Seleted once the user goes back to the page
            TempData["SelectedYear"] = vm.SelectedYear;  //## TempData[] is to transfer data between Actions in a controller- while on the same call..
            TempData["SelectedMonth"] = vm.SelectedMonth;
            TempData["SelectedPostType"] = vm.SelectedPostType;

            //Seperated LG and Fire functions
            //user selects a year from dropdown list so no need to provide seperate list of years. posting will ignore same month validation.
            //## This is the actual Field / Data validation on the Excel file - which is now in a DataSet. This will generate respective error message based on the defined validation rules on each field.
            string CheckSpreadSheetErrorMsg = _validateExcel.ValidateValues(modelDT.stringDT, vm.SelectedMonth, vm.SelectedPostType.ToString(), vm.SelectedYear, subPayList, validTitles, invalidSigns, ref errorMessage);

            LogInfo($"_validateExcel.ValidateValues: Finished.");

            if (!errorMessage.Equals(""))
            {
                //following tempdata is showing list of errors in file.            
                _cache.SetString(Constants.FileUploadErrorMessage, "<h3> Please remove the following errors from file and upload again</h3><br />" + CheckSpreadSheetErrorMsg);
                return RedirectToAction("Index", "Home");
            }
            else
            {

                //read design XML from wwwroot folder
                string designDocPath = string.Concat(this._host.WebRootPath, "\\DesignXML\\DataIntegration_Design.xml");
                try
                {
                    //save datatable for future use
                    //### This will insert new column in the DataTable, and add values in all rows, ie: UserName, ClientId, RemittanceId, MODDATE, PostDate
                    LogInfo($"_insertDataTable.PassDt. excelDt: {excelDt.Rows.Count} designDocPath: {designDocPath}");
                    _insertDataTable.ProcessDataTable(0, userId, "", "", "", excelDt, designDocPath);
                    LogInfo($"_insertDataTable.PassDt Finished!");

                    _cache.SetString(Constants.FileUploadErrorMessage, $"Success: File contents are validated successfully and ready to upload. Total <b>{numberOfRows}</b> records found.");
                    return RedirectToAction("Index", "Home");

                }
                catch (Exception ex)
                {
                    _cache.SetString(Constants.FileUploadErrorMessage, $"File upload failed! Please check error in qoutes : <b> {ex.Message}</b >'");
                    return RedirectToAction("Index", "Home");
                }
                finally
                {
                    excelDt.Dispose();
                    modelDT.stringDT.Dispose();
                }

            }

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


            return result;
        }

        /// <summary>
        /// following Create is to work with Fire pages
        /// </summary>
        /// <param name="files"></param>
        /// <param name="monthsList"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFire(IFormFile files, string monthsList, string yearsList, string posting)
        {
            var fileUplaodStatus = IsFileValid(files);
            if (!fileUplaodStatus.IsSuccess)
            {
                TempData["MsgM"] = "Only excel files can be uploaded";
                return RedirectToAction("IndexFire", "Home", null, "uploadFile");

            }

            CheckFileUploadedBO fileCheckBO = new()
            {
                P_Month = monthsList,
                P_Year = yearsList
            };
            string userName = CurrentUserId();
            string empName = ContextGetValue(Constants.SessionKeyEmployerName);
            string empID = "XXXXXX";// ContextGetValue(Constants.SessionKeyPayLocId);
            fileCheckBO.P_EMPID = empID;
            HttpContext.Session.SetString(Constants.SessionKeySchemeName, "FIRE");

            string[] validTitles = ConfigGetValue("ValidMemberTitles").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //Check all invalid signs from file and show error to employer
            string[] invalidSigns = ConfigGetValue("SignToCheck").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string apiBaseUrlForCheckFileAvailable = GetApiUrl(_apiEndpoints.CheckFileIsUploaded);
            //check if file is uploaded for the selected month and year.
            //int fileAvailableCheck = await apiCall.CheckFileAvailable(fileCheckBO, apiBaseUrlForCheckFileAvailable);
            var apiResult = await ApiPost(apiBaseUrlForCheckFileAvailable, fileCheckBO);
            int fileAvailableCheck = JsonConvert.DeserializeObject<int>(apiResult);

            string fileExt = string.Empty;
            string filePath = string.Empty;
            string spreadSheetName = string.Empty;
            string errorMessage = string.Empty;
            bool answer = true;
            // string fileNameWithoutExt = string.Empty;
            string webRootPath = _host.WebRootPath;
            MyModel model = new MyModel();
            //get list of paylocations
            List<PayrollProvidersBO> subPayList = await CallPayrollProviderService(userName);

            //bypass year and month check 
            if (posting.Equals(1))
            {
                if (fileAvailableCheck == 1)
                {
                    TempData["MsgM"] = "File is already uploaded for the month: " + monthsList + " and " + " payrol period: " + yearsList + " <br> You can goto Dashboard and start process on file from there. ";
                    return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                }
            }
            if (files == null)
            {
                TempData["MsgM"] = "Please choose a file first";
                return RedirectToAction("IndexFire", "Home", null, "uploadFile");
            }
            else if (monthsList.Equals("month") || yearsList.Equals("Select Year"))
            {
                TempData["MsgM"] = "Please select a payroll month and payroll year from drop down list.";
                return RedirectToAction("IndexFire", "Home", null, "uploadFile");
            }
            else
            {
                if (excelDt != null)
                {
                    excelDt.Clear();
                }
                //clear Datatable when upload file button is clicked.

                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(files.FileName);
                fileExt = Path.GetExtension(files.FileName);

                fileNameWithoutExt = $"{empName.Replace(" ", "-")}_{yearsList.Replace("/", "-")}_{monthsList}_{posting}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}{fileExt}";

                //copy file to local folder
                string _customerUploadsLocalFolder = ConfigGetValue("FileUploadPath");
                filePath = Path.Combine(_customerUploadsLocalFolder, fileNameWithoutExt);

                if (!Path.Exists(_customerUploadsLocalFolder))
                {
                    TempData["MsgM"] = "Error: File upload area not defined. Please contact support.";
                    return RedirectToAction("Index", "Home", null, "uploadFile");
                }

                //copy file to local folder
                using (FileStream fileStream = new(filePath, FileMode.Create))
                {
                    await files.CopyToAsync(fileStream);
                    _logger.LogInformation("File is copied to local folder.");
                }

                //saved file
                //string fullPathWithFileName = Path.Combine(path,fileExt);
                try
                {
                    excelDt = _commonRepo.ConvertExcelToDataTable(filePath, out errorMessage);
                }
                catch (Exception ex)
                {
                    TempData["MsgM"] = $"File is not uploaded, please check error in qoutes : <b>{ex.Message}</b>";
                    return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                }

                if (excelDt == null)
                {
                    TempData["MsgM"] = errorMessage;
                    //model.myErrorMessageText += errorMessage;
                    return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                }
                //allow a sign in a column
                // change column heading Name
                if (!_commonRepo.ChangeColumnHeadingsFire(excelDt, out errorMessage))
                {
                    TempData["MsgM"] = errorMessage;
                    // model.myErrorMessageText += errorMessage;
                    return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                }


                // convert all fields in data table to string
                // modelDT.stringDT = CommonRepo.ConvertAllFieldsToString(excelDt, monthsList, fileNameWithoutExt);
                modelDT.stringDT = _commonRepo.ConvertAllFieldsToString(excelDt, CurrentUserId());

                int numberOfRows = modelDT.stringDT.Rows.Count;

                //Add selected name of month into Session, filename and total records in file.
                HttpContext.Session.SetString(Constants.SessionKeyMonth, monthsList.ToString());
                HttpContext.Session.SetString(Constants.SessionKeyYears, yearsList.ToString());
                //HttpContext.Session.SetString(Constants.SessionKeyClientId,3.ToString());

                HttpContext.Session.SetString(Constants.SessionKeyFileName, fileNameWithoutExt.ToString());
                //HttpContext.Session.SetString(Constants.SessionKeyTotalRecords, (numberOfRows - 1).ToString());
                HttpContext.Session.SetString(Constants.SessionKeyTotalRecords, (numberOfRows).ToString());

                //var validYears = GetConfigValue("ValidPayrollYears").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                //Seperated LG and Fire functions
                string CheckSpreadSheetErrorMsg = _validateExcel.ValidateValues(modelDT.stringDT, monthsList, posting, yearsList, subPayList, validTitles, invalidSigns, ref errorMessage);
                //Seperated LG and Fire functions
                //string CheckSpreadSheetErrorMsg = repo.CheckSpreadsheetValuesFire(modelDT.stringDT, ref errorMessage);               

                if (!errorMessage.Equals(""))
                {
                    //following tempdata is showing list of errors in file.
                    TempData["MsgM"] += "<h2> Please remove following errors from file and upload again </h2><br />" + CheckSpreadSheetErrorMsg;
                    return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                }
                else
                {
                    TempData["MsgM"] += "File is uploaded successfully";

                    //save datatable for future use
                    //read design XML from wwwroot folder
                    string designDocPath = string.Concat(_host.WebRootPath, "\\DesignXML\\DataIntegration_Design.xml");
                    //save datatable for future use
                    try
                    {
                        //save datatable for future use
                        _insertDataTable.ProcessDataTable(0, "", "", "", "", excelDt, designDocPath);
                        TempData["MsgM"] = "File is uploaded successfully";

                        return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                    }
                    catch (Exception ex)
                    {
                        TempData["MsgM"] = "File has some data format or value that is causing issue " + ex.Message;
                        return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                    }
                    finally
                    {
                        excelDt.Dispose();
                        modelDT.stringDT.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// following action will replace Remad.aspx page from old web portal.
        /// Remad is for LG
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CheckTotals()
        {

            DataTable dt = _commonRepo.GetExcelDataAsString(CurrentUserId());
            
            var formTotals = new RemadShowTotalsValues();
            var contributionBO = formTotals.GetSpreadsheetValues(dt);

            double employeeTotal = contributionBO.MemberContribSS + contributionBO.MemberContrib5050SS + contributionBO.MemberContribAPCSS + contributionBO.MemberContribPOESSS + contributionBO.MemberContribARCSS;
            ViewBag.EmployeeTotal = employeeTotal;

            double empTotal = contributionBO.EmployerContribAPCSS + contributionBO.EmployerContribSS;
            ViewBag.empTotal = empTotal;

            double EmployersEmployeeTotalValue = employeeTotal + contributionBO.EmployersTotalSS;
            ViewBag.EmployersEmployeeTotalValue = EmployersEmployeeTotalValue.ToString("0.00");

            double DedifitTotalLblValue = contributionBO.DeficitRec +
                                 contributionBO.YearEndBalanceRec +
                                 contributionBO.FundedBenefitsRec +
                                 contributionBO.Miscellaneous_Rec;

            ViewBag.DedifitTotalLblValue = DedifitTotalLblValue;
            double GrandTotalValue = EmployersEmployeeTotalValue + DedifitTotalLblValue;
            ViewBag.GrandTotalValue = GrandTotalValue.ToString("0.00");

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
            DataTable dt = _commonRepo.GetExcelDataAsString(CurrentUserId());
            //modelDT.stringDT = CommonRepo.ConvertAllFieldsToString(excelDt);
            RemadShowTotalsValues formTotals = new RemadShowTotalsValues();
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
        /// <param name="contributionSummaryInfo"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> CheckTotals(MonthlyContributionBO contributionSummaryInfo)
        {
            //## if there are 7k+ records- add 2 seconds delay in each DB calls... so things will go in block by block- not all at once and clog the App and DB Server
            int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));

            //RemadShowTotalsValues formTotals = new RemadShowTotalsValues();
            var currentUser = await GetUserDetails(CurrentUserId());

            string remittanceID = string.Empty;
            contributionSummaryInfo.UserLoginID = currentUser.LoginId;
            contributionSummaryInfo.UserName = currentUser.UserId;
            contributionSummaryInfo.employerID = Convert.ToDouble(currentUser.Pay_Location_ID);
            contributionSummaryInfo.employerName = ContextGetValue(Constants.SessionKeyEmployerName);
            contributionSummaryInfo.payrollProviderID = currentUser.Pay_Location_Ref;
            
            contributionSummaryInfo.UploadedFileName = ContextGetValue(Constants.SessionKeyFileName);
            //contributionSummaryInfo.ClientID = ContextGetValue(Constants.SessionKeyClientId);
            contributionSummaryInfo.ClientID = currentUser.Client_Id;
            contributionSummaryInfo.payrollYear = ContextGetValue(Constants.SessionKeyYears);
            contributionSummaryInfo.PaymentMonth = ContextGetValue(Constants.SessionKeyMonth);

            //var content = new StringContent(JsonConvert.SerializeObject(contributionSummaryInfo), Encoding.UTF8, "application/json");
            string remittanceInsertApi = GetApiUrl(_apiEndpoints.InsertRemitanceDetails);

            //## First Create the Remittance with its Details.. insert-into 'UPMWEBEMPLOYERCONTRIBADVICE'
            LogInfo($"Create the Remittance with its Details.. insert-into 'UPMWEBEMPLOYERCONTRIBADVICE. {currentUser.Pay_Location_Ref}-{contributionSummaryInfo.employerName}, {contributionSummaryInfo.PaymentMonth}{contributionSummaryInfo.payrollYear}");

            if (totalRecordsInFile >= 7000)
            {
                LogInfo($"Large Excel file.. {totalRecordsInFile} records.. adding a 2-seconds delay before RemittanceInsertApi().. to let it flow smoothly");
                System.Threading.Thread.Sleep(2000);
                LogInfo("Sleep(2000) --> Finished");
            }

            var apiResult = await ApiPost(remittanceInsertApi, contributionSummaryInfo);
            remittanceID = JsonConvert.DeserializeObject<string>(apiResult);            

            if (apiResult == "") {
                //## somehow crashed... 
                TempData["Msg"] = "Failed to insert Remittance information into database. Please contact MP3 support team.";
                WriteToDBEventLog(-1, $"Failed to insert Remittance information into database. User: {currentUser.LoginId}, employer: {contributionSummaryInfo.employerName}, Provider: {contributionSummaryInfo.payrollProviderID}, Perdio: {contributionSummaryInfo.payrollYear}/{contributionSummaryInfo.PaymentMonth}", 1, 1);

                return View(contributionSummaryInfo);

            }
            LogInfo($"Remittance successfully inserted into database, Id: {remittanceID}.");

            TempData["Msg"] = "Remittance successfully inserted into database.";


            //## Now insert the Bulk Data in the Database..            
            LogInfo($"calling- await InsertBulkData({remittanceID}).");
            var isInserted = await InsertBulkData(Convert.ToInt32(remittanceID));
            LogInfo("InsertBulkData finished.");
            if (isInserted)
            {
                //## Once BULK-INSERT to DB is success- we can move our User data file (excel, csv) to the DONE folder...
                var isMoved = MoveFileToDone(remittanceID);                                

            }
            else {
                TempData["MsgError"] = $"Remittance Id: {remittanceID}. System has failed inserting records in to the database, Please contact MP3 support.";
                WriteToDBEventLog(Convert.ToInt32(remittanceID), "FAILED to execute Bulk data insert into database.", 1, 4);
                //## Delete this Temp file.. not needed anymore..
                DeleteTheExcelFile();
                return View(contributionSummaryInfo);
            }

            remittanceID = EncryptUrlValue(remittanceID);

            //## all good- we have checked the Totals and used it from the cache.. now better remove that DataTable variable from cache.. work done
            string cacheKey = $"{CurrentUserId()}_{Constants.ExcelDataAsString}";
            _cache.Delete(cacheKey);
                            
            //## Pass the RemittanceId via Session cache- to make the Url less Ugly with the Encrypted RemittanceId
            ContextSetValue(Constants.SessionKeyRemittanceID, remittanceID);

            LogInfo("Exiting Home/CheckTotals page..");
            return RedirectToAction("InitialiseProcessWithSteps");

        }

        private void DeleteTheExcelFile()
        {
            string filePathName = _cache.GetString($"{CurrentUserId()}_{Constants.UploadedFilePathKey}");

            if (!Path.Exists(Path.GetFullPath(filePathName)))
            {
                LogInfo($"Error: DeleteTheExcelFile() => filePathName: {filePathName}, is missing!");
            }
            else { 
                System.IO.File.Delete(filePathName);
            }

        }


        /// <summary>
        /// following action will move file from uploadedfiles to transferto folder on Web Server.
        /// 1st step after totals page.
        /// </summary>
        /// <returns></returns>
        private bool MoveFileToDone(string remittanceId)
        {

            string filePathName = _cache.GetString($"{CurrentUserId()}_{Constants.UploadedFilePathKey}");

            string destPath = ConfigGetValue("FileUploadPath") + ConfigGetValue("FileUploadDonePath");

            if (!Path.Exists(Path.GetFullPath(filePathName)))
            {
                TempData["MsgError"] = $"User File not found/accessible. Please try to upload the file again.";
                return false;
            }

            if (!Path.Exists(destPath))
            {
                TempData["MsgError"] = $"Destination file path not found/accessible. Please send an email to support with this screenshot.";
                return false;
            }

            try
            {                               
                bool fileMovedToDone = _commonRepo.CopyFileToFolder(Path.GetFullPath(filePathName), destPath, Path.GetFileName(filePathName));
                if (fileMovedToDone)
                {
                    string logInfoText = $"Moved user uploaded file to: \\DONE folder";
                    _logger.LogInformation(logInfoText);
                    WriteToDBEventLog(Convert.ToInt32(remittanceId), logInfoText);
                }

                return fileMovedToDone;

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error at MoveFileForFTP(). Details: {ex}");
                TempData["MsgError"] = "System failed to process the file, Please contact MP3 support.";
                WriteToDBEventLog(Convert.ToInt32(remittanceId), $"System failed to process the file. at MoveFileToDone(). Details: {ex.Message}");
            }

            return false;   //## shouldn't be  coming here.. something wrong.. return 'false'..
            //return View(errorAndWarningViewModelWithRecords);
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

            //get total records from in file from datatable.                
            int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));
            totalRecordsInFile++;

            var rangeOfRowsModel = new RangeOfRowsModel
            {
                P_USERID = userName,
                P_REMITTANCE_ID = remittanceId,
                P_NUMBER_OF_VALUES_REQUIRED = totalRecordsInFile
            };

            LogInfo($"Inside InsertBulkData(). remittanceId: {remittanceId}, totalRecordsInFile: {totalRecordsInFile}");

            string dataCounterInsertApi = GetApiUrl(_apiEndpoints.InsertDataCounter);   // will return- [P_FIRST_ROWID_VALUE] from 'RangeRowsReturn' api

            //Get the max Datarow id from MC_CONTRIBUTIONS_RECD to insert bulk data.
            //## we need to get the first DataRow_RecordId from the Database.. and then create new Primary Key as we insert new records.
            //int dataRowRecordId = await callApi.counterAPI(dataCounterInsertApi, rangeOfRowsModel);
            var apiResult = await ApiPost(dataCounterInsertApi, rangeOfRowsModel);
            int dataRowRecordId = JsonConvert.DeserializeObject<int>(apiResult);

            LogInfo($"New recordId will start from, dataRowRecordId: {dataRowRecordId}");

            //following datatable will change column names of datatable to DB column names and insert data from excelDT.                        
            DataTable excelData = _insertDataTable.Get(dataRowRecordId + 1, userName, schemeName, clientID, remittanceId.ToString());
            excelData.AcceptChanges();

            //Insert all the records to the database using api
            string bulkDataInsertApi = GetApiUrl(_apiEndpoints.InsertData);
            LogInfo($"calling-> {bulkDataInsertApi}. Total records in excelData: {excelData.Rows.Count}");
            apiResult = await ApiPost(bulkDataInsertApi, excelData);

            if (IsEmpty(apiResult)) {
                LogInfo($"ERROR: Failed to insert Bulk data: remittanceId: {remittanceId}");
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
            ContextSetValue(Constants.SessionKeyTotalRecordsInDB, excelData.Rows.Count.ToString());
            
            bool InsertToDbSuccess = JsonConvert.DeserializeObject<bool>(apiResult);

            excelData.Dispose();

            return InsertToDbSuccess;            
        }

        public void WriteToDBEventLog(int remitID, string eventNotes, int remittanceStatus = 1, int eventTypeID = 1)
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
            bool isSuccess = await CheckAllRecordsAreInsertedInDB(remttanceId);

            //## if there are 7k+ records- add 2 seconds delay in each DB calls... so things will go in block by block- not all at once and clog the App and DB Server
            int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));
            bool shouldAddDelay = totalRecordsInFile >= 7000;
            
            //## ReturnInitialise() call.. a big piece of Task- initialising the entire journey, setting values, generating error/warnings, fixing issues and many things... 
            var initialiseProcessResult = await Execute_ReturnInitialiseProcess(userID, remttanceId);

            if(initialiseProcessResult.P_STATUSCODE != 0 ) {
                //## value '0' means all good ('Records updated').. returned by PackageProcedure in DB
                LogInfo($"ReturnInitialise returned P_STATUSCODE={initialiseProcessResult.P_STATUSCODE}, will be showing error to the User.");
                return RedirectToAction("ErrorCustom", "Home");
            }

            //## Return Check API to call to check if the previous month file is completed.
            isSuccess = await CheckPreviousMonthFileIsSubmitted(remttanceId);


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
        /// This is an alternative page for InitialiseProcess()- which has a big workload- all at once..
        /// This new page will prompt the user to initiate the tasks one by one, therefore will have a smooth journey end-to-end
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult InitialiseProcessWithSteps()
        {
            var encryptedRemittanceId = ContextGetValue(Constants.SessionKeyRemittanceID);
            int remttanceId = Convert.ToInt32(DecryptUrlValue(encryptedRemittanceId));
            int totalRecordsInFile = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));
            var totalRecordsInDatabase = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecordsInDB));
            string errorMessage = TempData["InitialiseProcessError"]?.ToString();

            LogInfo($"Loading Home/InitialiseProcessWithSteps() .. RemittanceId: {remttanceId}");
            LogInfo($"TotalRecordsInFile: {totalRecordsInFile}, totalRecordsInDatabase: {totalRecordsInDatabase}");

            var initialiseProcessResultVM = new InitialiseProcessResultVM()
            {
                EncryptedRemittanceId = encryptedRemittanceId,
                EmployeeName = ContextGetValue(Constants.SessionKeyEmployerName),
                ErrorMessage = errorMessage,
            };

            var employerProcessedCount = ContextGetValue(Constants.EmployerProcessedCount);
            if (IsEmpty(employerProcessedCount)) {
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

            return View(initialiseProcessResultVM);
        }


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

        [HttpGet,HttpPost]
        public async Task<ActionResult> InitialiseAutoMatchProcessByAjax()
        {
            var encryptedRemittanceId = ContextGetValue(Constants.SessionKeyRemittanceID);
            int remttanceId = Convert.ToInt32(DecryptUrlValue(encryptedRemittanceId));

            var autoMatchResult = await Execute_AutoMatchProcess(remttanceId);
            var taskResult = new TaskResults() { 
                IsSuccess = autoMatchResult.L_STATUS_CODE == 0,
                Message =  $"<i class='fas fa-users mx-2 mx-2'></i> Persons Matched: {autoMatchResult.personMatchCount}<br />" 
                    + $"<i class='fas fa-folder-open mx-2'></i> Folders Matched: {autoMatchResult.folderMatchCount}<br />"
            };

            if (autoMatchResult.L_STATUS_CODE != 0) {
                taskResult.Message = $"Error: {autoMatchResult.L_STATUS_CODE} - {autoMatchResult.L_STATUS_TEXT}";
            }

            return Json(taskResult);
        }

        private async Task<AutoMatchBO> Execute_AutoMatchProcess(int remittanceId)
        {
            apiBaseUrlForAutoMatch = GetApiUrl(_apiEndpoints.AutoMatch);    //## api/AutoMatchRecords

            ContextRemoveValue(Constants.ReturnInitialiseCurrentStep); //## Remove the value.. so system will reset to original value- whatever it was before..
            ContextRemoveValue(Constants.EmployerProcessedCount);
            ContextRemoveValue(Constants.SessionKeyTotalRecords);
            ContextRemoveValue(Constants.SessionKeyTotalRecordsInDB);

            LogInfo($"Calling Automatch api: {apiBaseUrlForAutoMatch}.");
            AutoMatchBO autoMatchResult = new();
                        
            //## even though 'initialiseProcessResult' is successful- we have seen on various ocassions that a Remittance still have status < 70, due to an on-going process.
            //## we need to wait for at least 5 seconds before allowing the user to initiate Auto_Match process.. otherwise- that will fail for a Remittance status < 70.
            for (int i = 0; i < 2; i++)
            {
                string apiResult = await ApiGet($"{apiBaseUrlForAutoMatch}{remittanceId}");
                autoMatchResult = JsonConvert.DeserializeObject<AutoMatchBO>(apiResult);
                
                LogInfo($"Automatch executed. autoMatchBO.L_STATUS_CODE: {autoMatchResult.L_STATUS_CODE} - {autoMatchResult.L_STATUS_TEXT}.");

                if (autoMatchResult.L_STATUS_CODE != 0)
                {
                    LogInfo($"Attempt {i + 1}: Automatch failed. autoMatchBO.L_STATUS_CODE: {autoMatchResult.L_STATUS_CODE}-{autoMatchResult.L_STATUS_TEXT}. Try again after 5 seconds.");
                    Thread.Sleep(5000);
                }
                else {
                    LogInfo($"Attempt {i + 1}: Automatch Success");
                    break;
                }
            }
            
            LogInfo($"Automatch finished. autoMatchBO.L_STATUS_CODE: {autoMatchResult.L_STATUS_CODE} - {autoMatchResult.L_STATUS_TEXT}.");

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
            //result = await callApi.ReturnCheckAPICall(result, apiBaseUrlForCheckReturn);
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
        /// List to show months in dropdown menu
        /// </summary>
        /// <returns></returns>
        public List<string> GetYears()
        {
            var validYears = ConfigGetValue("ValidPayrollYears").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            
            return validYears.ToList();

        }


        /// <summary>
        /// following method will show list of sub payroll provider with login name and id
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        private async Task<List<PayrollProvidersBO>> CallPayrollProviderService(string userName)
        {
            //var subPayrollList = new List<PayrollProvidersBO>();
            string apiBaseUrlForSubPayrollProvider = GetApiUrl(_apiEndpoints.SubPayrollProvider);

            var apiResult = await ApiGet(apiBaseUrlForSubPayrollProvider + userName);
            var subPayrollList = JsonConvert.DeserializeObject<List<PayrollProvidersBO>>(apiResult);

            return subPayrollList;
        }
        /// <summary>
        /// This functionality is added for Olla's request to allow . sign in title field
        /// </summary>
        private DataTable AllowedSigns(DataTable dataTable)
        {
            string[] allwedSigns = ConfigGetValue("AllowedSigns").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] allowedColumns = ConfigGetValue("AllowedColumns").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int countRows = dataTable.Rows.Count;
            for (int i = 0; i < countRows; i++)
            {
                foreach (var item in allowedColumns)
                {
                    foreach (var sign in allwedSigns)
                    {
                        dataTable.Rows[i][item] = dataTable.Rows[i][item].ToString().Replace(sign, "");
                    }
                }
            }
            try
            {
                dataTable.AcceptChanges();
                return dataTable;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Error at 'sign' in the title field. Description: {ex.ToString()}");
            }
            finally
            {
                dataTable.Dispose();
            }
        }

        /// <summary>
        /// This will convert the Excel to CSV file, then look for any malicious scripts in the file.
        /// Also checks for Empty Rows in the Excel sheet
        /// If found- return with error. Otherwise- returns empty text
        /// </summary>
        /// <param name="filePathName">Excel file name to check</param>
        /// <returns>File processing error</returns>
        private TaskResults CheckMaliciousScripts_And_EmptyRows(string filePathName)
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

            var maliciousTags = ConfigGetValue("MaliciousTags").Split(",");

            var contents = System.IO.File.ReadAllText(csvFilePath).Split('\n');
            
            int rowCounter = 1;
            string rowNumberList = "";

            foreach (var item in contents)  //## read line by line Rows in the CSV/ExcelSheet
            {
                foreach (var tag in maliciousTags)  //## Read each invalid characters listed in the Config file
                {
                    if (item.ToLower().Contains(tag))
                    {
                        result.Message = "Error: Invalid characters found in the file. Please remove the invalid symbols from the file and try again. <br/>Please avoid symbols, ie: <h3>" + string.Join(" , ", maliciousTags) + "</h3>";
                    }
                }

                //## Check for empty Excel rows- which are simply some commas without values in a CSV file, ie: ",,,,,,,," You can view this opening the file with Notepad                    
                if (item.ToLower().StartsWith(",,,") || item.ToLower()=="\r") //## at least 3 commas means- there are and will be more empty cells.. which makes empty rows
                {
                    //result.Message = "<i class='fas fa-exclamation-triangle mr-4 fa-lg'></i><div class='h4'>Error: There are empty rows found in the Excel file. Please delete the empty rows and try again.</div>";
                    
                    rowNumberList += rowCounter + ", "; //## This is the Text holding all Empty Row numbers, to display the user.. ie: "2,5,8"                    
                }

                rowCounter++;
            }

            //## if Empty rows found and we have generated a string with the list of Row numbers- then finish the end Tag for <div>
            if (rowNumberList != "") {
                string warningMessage = "<i class='fas fa-exclamation-triangle mr-4 fa-lg'></i><div class='h4'>Error: Empty rows found in the Excel file. Rows: " + rowNumberList + "</div>";

                result.Message = $"{warningMessage}<div class='h5 text-primary'>Please delete the empty rows and try again.</div>";
            }
            

            System.IO.File.Delete(csvFilePath); //## this is a staging file for invalid contents check.. delete it once processed

            result.IsSuccess = IsEmpty(result.Message);
            return result;  //## success! All good!
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

            string filePathName = GenerateFilePathNameForUpload(formFile, "staging-file-");

            using (FileStream fileStream = new(filePathName, FileMode.Create))
            {
                await formFile.CopyToAsync(fileStream);
                _logger.LogInformation("File is copied to local folder.");
            }

            var scriptsCheck = CheckMaliciousScripts_And_EmptyRows(filePathName);

            //## now delete the both staging files.. not required anymore
            System.IO.File.Delete(filePathName);

            //## Now check results for malicious script Validations..
            if (scriptsCheck.IsSuccess)
            {
                return Json("success");
            }

            string errorMessage = fileTypeCheck.Message + " " + scriptsCheck.Message;
            return Json(errorMessage);

        }

        private string GenerateFilePathNameForUpload(IFormFile customerFile, string fileNamePrefix)
        {
            var fileNameForUpload = Path.GetFileNameWithoutExtension(customerFile.FileName);
            string fileExt = Path.GetExtension(customerFile.FileName);

            fileNameForUpload = $"{fileNamePrefix}{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}{fileExt}";

            //copy file to local folder
            string filePath = Path.Combine(_customerUploadsLocalFolder, fileNameForUpload);

            return filePath;
        }
    }
}
