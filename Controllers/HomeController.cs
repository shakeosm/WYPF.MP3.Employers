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
using Microsoft.AspNetCore.Mvc.Rendering;
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
using System.Net.Http;
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
        string remittanceInsertApi = string.Empty;
        string apiBaseUrlForAutoMatch = string.Empty;
        EventDetailsBO eBO = new EventDetailsBO();
        InsertDataTable dataTableToDB = new InsertDataTable();
        DataTable excelDt;
        DataTable excelDt1;
        MyModel modelDT = new MyModel();
        CheckSpreadsheetValuesSample repo = new CheckSpreadsheetValuesSample();

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment host, IConfiguration configuration, IRedisCache Cache, IDataProtectionProvider Provider, IOptions<ApiEndpoints> ApiEndpoints) : base(configuration, Cache, Provider, ApiEndpoints)
        {
            _logger = logger;
            _host = host;
            _Configure = configuration;
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
            //check if year, month and type of posting is already selected from session
            string monthSelected = ContextGetValue(Constants.SessionKeyMonth) ?? string.Empty;
            string yearSelected = ContextGetValue(Constants.SessionKeyYears) ?? string.Empty;
            string postSelected = ContextGetValue(Constants.SessionKeyPosting) ?? string.Empty;
            string userName = ContextGetValue(Constants.SessionKeyUserID);

            //client type is null or empty goto login page again
            if (string.IsNullOrEmpty(clientType))
            {
                return RedirectToAction("Index", "Login");
            }

            //if logged in user is Fire then goto FireIndex
            if (clientType.Equals("FIRE"))
            {
                return RedirectToAction("IndexFire", "Home");
            }

            ViewBag.Months = GetMonths(monthSelected).Select(x => new SelectListItem()
            {
                Text = x.text,
                Value = x.value
            });

            ViewBag.Years = GetYears(yearSelected).Select(x => new SelectListItem()
            {
                Text = x.years,
                Value = x.years
            });

            ViewBag.Posting = GetOption(postSelected).Select(x => new SelectListItem()
            {
                Text = x.text,
                Value = x.value
            });

            List<PayrollProvidersBO> subPayList = await CallPayrollProviderService(userName);
            //staff login for frontend to upload a file for Employers causing an issue where it is getting a "-" in list which 
            //throws an exception.

            subPayList = subPayList.Where(x => x.pay_location_ID != null).ToList();

            ViewBag.Paylocations = subPayList.Select(x => new SelectListItem()
            {
                Text = x.pay_location_name,
                Value = x.pay_location_ID.ToString()
            });

            string fileUploadErrorMessage = _cache.GetString(Constants.FileUploadErrorMessage);
            //## Delete the message once Read.. otherwise- this will keep coming on every page request...
            _cache.Delete(Constants.FileUploadErrorMessage);

            var viewModel = new HomeFileUploadVM()
            {
                MonthList = GetMonths(monthSelected),
                YearList = GetYears(yearSelected),
                OptionList = GetOption(postSelected),
                PayLocationList = subPayList,
                ErrorMessage = fileUploadErrorMessage,
            };

            return View(viewModel);
        }

        /// <summary>
        /// To Fire - Remittance page
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> IndexFire()
        {
            string userName = string.Empty;
            userName = ContextGetValue(Constants.SessionKeyUserID);
            string monthSelected = ContextGetValue(Constants.SessionKeyMonth) ?? string.Empty;
            string yearSelected = ContextGetValue(Constants.SessionKeyYears) ?? string.Empty;
            string postSelected = ContextGetValue(Constants.SessionKeyPosting) ?? string.Empty;

            List<PayrollProvidersBO> subPayList = await CallPayrollProviderService(userName);

            ViewBag.Paylocations = subPayList.Select(x => new SelectListItem()
            {
                Text = x.pay_location_name,
                Value = x.pay_location_ID.ToString()
            });
            ViewBag.Months = GetMonths(monthSelected).Select(x => new SelectListItem()
            {
                Text = x.text,
                Value = x.value
            });

            ViewBag.Years = GetYears(yearSelected).Select(x => new SelectListItem()
            {
                Text = x.years,
                Value = x.years
            });

            ViewBag.Posting = GetOption(postSelected).Select(x => new SelectListItem()
            {
                Text = x.text,
                Value = x.value
            });

            return View();
        }


        [AllowAnonymous]
        public async Task<IActionResult> Error()
        {
            var exDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            var errorDetails = new ErrorViewModel()
            {

                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                UserId = HttpContext.Session.GetString(Constants.UserIdKeyName),
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
            if (!ModelState.IsValid)
            {
                _cache.SetString(Constants.FileUploadErrorMessage, "Error: You must select 'Year', 'Month' and 'PostType' to continue");
                return RedirectToAction("Index", "Home");
            }

            var fileCheck = IsFileValid(vm.PaymentFile);

            if (!fileCheck.IsSuccess)
            {
                _cache.SetString(Constants.FileUploadErrorMessage, fileCheck.Message);
                return RedirectToAction("Index", "Home");   //, null, "uploadFile");
            }

            //Add selected name of month into Session, filename and total records in file.
            HttpContext.Session.SetString(Constants.SessionKeyMonth, vm.SelectedMonth);
            HttpContext.Session.SetString(Constants.SessionKeyYears, vm.SelectedYear);
            HttpContext.Session.SetString(Constants.SessionKeyPosting, vm.SelectedPostType.ToString());
            HttpContext.Session.SetString(Constants.SessionKeySchemeName, SessionSchemeNameValue);

            //TotalRecordsInsertedAPICall apiCall = new TotalRecordsInsertedAPICall();

            string userName = ContextGetValue(SessionKeyUserID);
            string empName = ContextGetValue(Constants.SessionKeyEmployerName);
            string empID = ContextGetValue(Constants.SessionKeyPayrollProvider);

            var fileCheckBO = new CheckFileUploadedBO
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
            string apiBaseUrlForCheckFileAvailable = GetApiUrl(_apiEndpoints.CheckFileAvailable);

            //check if records were uploaded previously for the selected month and year.
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

            if (vm.SelectedPostType == (int)PostingType.First)
            {
                if (fileAvailableCheck == 1)
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
                _logger.LogInformation("File is copied to local folder.");
            }


            //## malicious script check
            //TODO: Use NugetPackage for Excel operation: 'https://www.nuget.org/packages/CsvHelper.Excel.Core/'
            var result = CheckMaliciousScripts_And_EmptyRows(filePath);
            if (result.IsSuccess == false)
            {
                System.IO.File.Delete(filePath);
                _cache.SetString(Constants.FileUploadErrorMessage, result.Message);
                return RedirectToAction("Index", "Home");
            }

            try
            {
                //## This will Convert the Excel sheet to a System.Data.DataTable to make it convenient to validate fields
                excelDt = CommonRepo.ConvertExcelToDataTable(filePath, out errorMessage);

                //## all good.. now store this filePath in the Redis cache for further processing, if required
                _cache.SetString($"{CurrentUserId()}_{UploadedFilePathKey}", filePath);
            }
            catch (Exception ex)
            {
                _cache.SetString(Constants.FileUploadErrorMessage, "File has an error or missing something " + ex.Message);
                return RedirectToAction("Index", "Home");
            }


            if (excelDt == null)
            {
                _cache.SetString(Constants.FileUploadErrorMessage, errorMessage);
                return RedirectToAction("Index", "Home");
            }

            // change column heading Name
            if (!CommonRepo.ChangeColumnHeadings(excelDt, out errorMessage))
            {
                _cache.SetString(Constants.FileUploadErrorMessage, errorMessage);
                return RedirectToAction("Index", "Home");
                //return RedirectToAction("Index", "Home");
            }

            // convert all fields in data table to string
            modelDT.stringDT = CommonRepo.ConvertAllFieldsToString(excelDt);


            int numberOfRows = modelDT.stringDT.Rows.Count;

            //Add selected name of month into Session, filename and total records in file.
            HttpContext.Session.SetString(Constants.SessionKeyMonth, vm.SelectedMonth.ToString());
            HttpContext.Session.SetString(Constants.SessionKeyYears, vm.SelectedYear.ToString());

            var fileNameForUpload = Path.GetFileName(filePath);
            HttpContext.Session.SetString(Constants.SessionKeyFileName, fileNameForUpload);
            HttpContext.Session.SetString(Constants.SessionKeyTotalRecords, (numberOfRows - 1).ToString());//HttpContext.Session.SetString(Constants.SessionKeyTotalRecords, (numberOfRows - 1).ToString());

            // var validYears = GetConfigValue("ValidPayrollYears").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            //Seperated LG and Fire functions
            //user selects a year from dropdown list so no need to provide seperate list of years. posting will ignore same month validation.
            //## This is the actual Field / Data validation on the Excel file - which is now in a DataSet. This will generate respective error message based on the defined validation rules on each field.
            string CheckSpreadSheetErrorMsg = repo.CheckSpreadsheetValues(modelDT.stringDT, vm.SelectedMonth, vm.SelectedPostType.ToString(), vm.SelectedYear, subPayList, validTitles, invalidSigns, ref errorMessage);

            if (!errorMessage.Equals(""))
            {
                //following tempdata is showing list of errors in file.            
                _cache.SetString(Constants.FileUploadErrorMessage, "<h2> Please remove following errors from file and upload again </h2><br />" + CheckSpreadSheetErrorMsg);
                return RedirectToAction("Index", "Home");
            }
            else
            {

                //string designDocPath = GetConfigValue("DesginXMLFilePath");
                //read design XML from wwwroot folder
                string designDocPath = string.Concat(this._host.WebRootPath, "\\DesignXML\\DataIntegration_Design.xml");
                try
                {
                    //save datatable for future use
                    dataTableToDB.PassDt(0, "", "", "", "", excelDt, designDocPath);

                    _cache.SetString(Constants.FileUploadErrorMessage, "File is uploaded successfully");
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

            result.IsSuccess = string.IsNullOrEmpty(result.Message);


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
            string userName = ContextGetValue(Constants.SessionKeyUserID);
            string empName = ContextGetValue(Constants.SessionKeyEmployerName);
            string empID = ContextGetValue(Constants.SessionKeyPayLocId);
            fileCheckBO.P_EMPID = empID;
            HttpContext.Session.SetString(Constants.SessionKeySchemeName, "FIRE");

            string[] validTitles = ConfigGetValue("ValidMemberTitles").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //Check all invalid signs from file and show error to employer
            string[] invalidSigns = ConfigGetValue("SignToCheck").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string apiBaseUrlForCheckFileAvailable = GetApiUrl(_apiEndpoints.CheckFileAvailable);
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
                    excelDt = CommonRepo.ConvertExcelToDataTable(filePath, out errorMessage);
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
                if (!CommonRepo.ChangeColumnHeadingsFire(excelDt, out errorMessage))
                {
                    TempData["MsgM"] = errorMessage;
                    // model.myErrorMessageText += errorMessage;
                    return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                }


                // convert all fields in data table to string
                // modelDT.stringDT = CommonRepo.ConvertAllFieldsToString(excelDt, monthsList, fileNameWithoutExt);
                modelDT.stringDT = CommonRepo.ConvertAllFieldsToString(excelDt);

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
                string CheckSpreadSheetErrorMsg = repo.CheckSpreadsheetValues(modelDT.stringDT, monthsList, posting, yearsList, subPayList, validTitles, invalidSigns, ref errorMessage);
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
                        dataTableToDB.PassDt(0, "", "", "", "", excelDt, designDocPath);
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
        public IActionResult Remad()
        {

            DataTable dt = CommonRepo.PassDataTableToViews();
            //modelDT.stringDT = CommonRepo.ConvertAllFieldsToString(excelDt);
            RemadShowTotalsValues formTotals = new RemadShowTotalsValues();
            MonthlyContributionBO contributionBO = new MonthlyContributionBO();

            contributionBO = formTotals.GetSpreadsheetValues(dt);
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
            DataTable dt = CommonRepo.PassDataTableToViews();
            //modelDT.stringDT = CommonRepo.ConvertAllFieldsToString(excelDt);
            RemadShowTotalsValues formTotals = new RemadShowTotalsValues();
            MonthlyContributionBO contributionBO = new MonthlyContributionBO();

            //remove the browser response issue of pen testing
            if (string.IsNullOrEmpty(ContextGetValue("_UserName")))
            {
                contributionBO = null;

                RedirectToAction("Index", "Login");
            }
            return View(contributionBO);
        }


        // [HttpPost]
        //[ValidateAntiForgeryToken] 
        /// <summary> New remitance submitted and remitance ID created for the uploaded file/// </summary>
        /// <param name="contributionBO"></param>
        /// <returns></returns>
        public async Task<IActionResult> Remad(MonthlyContributionBO contributionBO)
        {
            //RemadShowTotalsValues formTotals = new RemadShowTotalsValues();

            string remittanceID = string.Empty;
            contributionBO.UserLoginID = ContextGetValue(Constants.SessionKeyUserID);
            contributionBO.UserName = ContextGetValue(Constants.SessionKeyUserID);
            contributionBO.employerID = Convert.ToDouble(ContextGetValue(Constants.SessionKeyPayLocId));
            contributionBO.employerName = ContextGetValue(Constants.SessionKeyEmployerName);
            contributionBO.payrollProviderID = ContextGetValue(Constants.SessionKeyPayrollProvider);
            string path = string.Empty;

            contributionBO.PaymentMonth = ContextGetValue(Constants.SessionKeyMonth);//monthAndPayroll.Columns[0].ColumnName;
            contributionBO.UploadedFileName = ContextGetValue(Constants.SessionKeyFileName);//monthAndPayroll.Columns[1].ColumnName;
            contributionBO.ClientID = ContextGetValue(Constants.SessionKeyClientId);
            contributionBO.payRollYear = ContextGetValue(Constants.SessionKeyYears);

            remittanceInsertApi = GetApiUrl(_apiEndpoints.TotalRecordsInserted);
            string getRemittanceIdByFileNameApi = GetApiUrl(_apiEndpoints.GetRemittanceId);

            ///API URI is getting from Apsetting.json file.
            //string apiBaseUrlForInsertEventDetails = GetApiUrl(_apiEndpoints.InsertEventDetails);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(JsonConvert.SerializeObject(contributionBO), Encoding.UTF8, "application/json");
                    //string endpoint = remittanceInsertApi;

                    using (var Response = await client.PostAsync(remittanceInsertApi, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                            _logger.LogInformation("Remittance API Call successfull");
                            TempData["Msg"] = "Remittance successfully inserted into database.";
                            try
                            {
                                //## by now the RemittanceId is created, when called 'var Response = await client.PostAsync(endpoint, content)'
                                var apiResult = await ApiGet($"{getRemittanceIdByFileNameApi}{contributionBO.UploadedFileName}");
                                remittanceID = JsonConvert.DeserializeObject<string>(apiResult);

                                _logger.LogInformation("API to get Remittance id is successfull");

                                //Update Event Details table and remittance inserted.
                                eBO.remittanceID = Convert.ToInt32(remittanceID);
                                eBO.remittanceStatus = 1;
                                eBO.eventTypeID = 2;
                                //eBO.eventDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                                eBO.notes = "New remitance submitted and remittance ID created for the uploaded file.";

                                InsertEventDetails(eBO);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("API to insert Remittance shows error.");
                                TempData["Msg"] = "System is showing some error to send Remittance totals, Please try again";
                                //monthAndPayroll.Clear();
                                return View(contributionBO);
                            }

                            remittanceID = EncryptUrlValue(remittanceID);

                            return Redirect($"/Home/MoveFileForFTP?id={remittanceID}");

                        }
                        else
                        {
                            ModelState.Clear();
                            _logger.LogError(string.Empty, "Error occured while calling Remittance Insert API");
                            TempData["Msg"] = "System is showing some error to send Remittance totals, Please try again";
                            //monthAndPayroll.Clear();

                            return View(contributionBO);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error while calling insert data into Remittance table");
                }
            }
            TempData["Msg"] = "System is showing some error to send Remittance totals, Please try again";
            return View(contributionBO);
        }

        /// <summary>
        /// following action will move file from uploadedfiles to transferto folder on Web Server.
        /// 1st step after totals page.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> MoveFileForFTP(string id)
        {

            RangeOfRowsModel rangeOfRowsModel = new RangeOfRowsModel();
            //only remittance id is provided in following model class for view
            var errorAndWarningViewModelWithRecords = new ErrorAndWarningViewModelWithRecords();

            //decode remittance id in url
            errorAndWarningViewModelWithRecords.remittanceID = id;
            id = DecryptUrlValue(id);

            string filePathName = _cache.GetString($"{CurrentUserId()}_{UploadedFilePathKey}");

            string destPath = ConfigGetValue("FileUploadPath") + ConfigGetValue("FileUploadDonePath");

            if (!Path.Exists(Path.GetFullPath(filePathName)))
            {
                TempData["MsgError"] = $"User File not found/accessible";
                return View(errorAndWarningViewModelWithRecords);
            }

            if (!Path.Exists(destPath))
            {
                TempData["MsgError"] = $"Destination file path not found/accessible";
                return View(errorAndWarningViewModelWithRecords);
            }

            //string apiBaseUrlForInsertEventDetails = GetApiUrl(_apiEndpoints.InsertEventDetails);

            //copy file to local folder
            try
            {
                string clientID = ContextGetValue(Constants.SessionKeyClientId);
                string schemeName = ContextGetValue(Constants.SessionKeySchemeName);
                string userName = ContextGetValue(Constants.SessionKeyUserID);

                //get total records from in file from datatable.
                int totalRecordsInF = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));
                totalRecordsInF++;
                rangeOfRowsModel.P_USERID = userName;
                rangeOfRowsModel.P_REMITTANCE_ID = Convert.ToInt32(id);
                rangeOfRowsModel.P_NUMBER_OF_VALUES_REQUIRED = totalRecordsInF;


                string dataCounterInsertApi = GetApiUrl(_apiEndpoints.InsertDataCounter);   // will return- [P_FIRST_ROWID_VALUE] from 'RangeRowsReturn' api

                //Get the max Datarow id from MC_CONTRIBUTIONS_RECD to insert bulk data.
                //## we need to get the first DataRow_RecordId from the Database.. and then create new Primary Key as we insert new records.
                //int dataRowRecordId = await callApi.counterAPI(dataCounterInsertApi, rangeOfRowsModel);
                var apiResult = await ApiPost(dataCounterInsertApi, rangeOfRowsModel);
                int dataRowRecordId = JsonConvert.DeserializeObject<int>(apiResult);

                //following datatable will change column names of datatable to DB column names and insert data from excelDT.
                DataTable newDT = dataTableToDB.KeepDataTable(dataRowRecordId + 1, userName, schemeName, clientID, id.ToString());

                //Insert all the records to the database using api
                string bulkDataInsertApi = GetApiUrl(_apiEndpoints.InsertData);
                newDT.AcceptChanges();
                //bool InsertToDbSuccess = await callApi.InsertDataApi(newDT, bulkDataInsertApi);
                apiResult = await ApiPost(bulkDataInsertApi, newDT);
                bool InsertToDbSuccess = JsonConvert.DeserializeObject<bool>(apiResult);

                newDT.Dispose();


                if (InsertToDbSuccess)
                {
                    WriteToDBEventLog(Convert.ToInt32(id), "Bulk data insert into database.", 1, 4);
                }
                else
                {
                    TempData["MsgError"] = "System has failed uploading records to the database, Please contact MP3 support.";
                    WriteToDBEventLog(Convert.ToInt32(id), "FAILED to execute Bulk data insert into database.", 1, 4);
                    RedirectToAction("Admin", "Home");
                }

                //## Once BULK-INSERT to DB is success- we can move our User data file to the DONE folder...
                bool fileMovedToDone = CommonRepo.CopyFileToFolder(Path.GetFullPath(filePathName), destPath, Path.GetFileName(filePathName));
                if (fileMovedToDone)
                {
                    string logInfoText = $"Moved user uploaded file to: {destPath}";
                    _logger.LogInformation(logInfoText);
                    WriteToDBEventLog(Convert.ToInt32(id), logInfoText);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error at MoveFileForFTP(). Details:: {ex}");
                TempData["MsgError"] = "System has failed uploading records to the database, Please contact MP3 support.";
                WriteToDBEventLog(Convert.ToInt32(id), $"Error at MoveFileForFTP(). Details: {ex.Message.Substring(0, 200)}");
            }


            return View(errorAndWarningViewModelWithRecords);
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
                notes = eventNotes
            };

            //update Event Details table File is uploaded successfully.                               
            InsertEventDetails(eBO);
        }

        /// <summary>
        /// Here to start encode remittance.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> RemittanceInsertDB(string id)
        {
            int remttanceId = Convert.ToInt32(DecryptUrlValue(id));

            ReturnCheckBO result = new();

            string userID = ContextGetValue(Constants.SessionKeyUserID);

            var errorAndWarningViewModelWithRecords = new ErrorAndWarningViewModelWithRecords
            {
                ALERT_TYPE_REF = "ALL",
                ALERT_CLASS = "Error and Warnings",
                remittanceID = id,
                ALERT_DESC = "All the Errors and Warnings in file"
            };

            //List<AutoMatchBO> BO = new List<AutoMatchBO>();
            apiBaseUrlForAutoMatch = GetApiUrl(_apiEndpoints.AutoMatch);
            //url to get total number of records in database
            string apiBaseUrlForTotalRecords = GetApiUrl(_apiEndpoints.TotalRecordsInserted);
            string apiBaseUrlForInitialiseProc = GetApiUrl(_apiEndpoints.InitialiseProc);

            var initialiseProcBO = new InitialiseProcBO
            {
                P_REMITTANCE_ID = remttanceId,
                P_USERID = userID
            };

            try
            {
                var apiResult = await ApiGet($"{apiBaseUrlForTotalRecords}{remttanceId}");

                int total = JsonConvert.DeserializeObject<int>(apiResult);
                string totalRecordsInF = ContextGetValue(Constants.SessionKeyTotalRecords);
                //following loop will keep on until it finds a record in database.//Windows bulk insertion service submits only 10000 records at time so I Need to keep check until all the records inserted.
                while (total == 0 || Convert.ToInt32(totalRecordsInF) > total)
                {
                    apiResult = await ApiGet($"{apiBaseUrlForTotalRecords}{remttanceId}");
                    total = JsonConvert.DeserializeObject<int>(apiResult);
                }

                //## Following is a big piece of Task- initialising the entire journey, setting values, fixing issues and many things... 
                apiResult = await ApiPost(apiBaseUrlForInitialiseProc, initialiseProcBO);
                initialiseProcBO = JsonConvert.DeserializeObject<InitialiseProcBO>(apiResult);

                //## need to add a check to see what is the outcome of 'apiBaseUrlForInitialiseProc' API call..
                if (initialiseProcBO.P_STATUSCODE == 9)
                {
                    //## abort mission... corrupted data found..
                    var errorViewModel = new ErrorViewModel()
                    {
                        ApplicationId = Constants.EmployersPortal,
                        ErrorPath = $"/Home/RemittanceInsertDB/{remttanceId}",
                        Message = "Return Initialise failed with StatusCode 9.",
                        RemittanceInfo = remttanceId.ToString(),
                        RequestId = "0",
                        Source = apiBaseUrlForInitialiseProc,
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

                    return RedirectToAction("ErrorCustom", "Home");
                }


                eBO.remittanceID = remttanceId;
                eBO.remittanceStatus = 1;
                eBO.eventTypeID = 50;
                eBO.notes = "Initial Processing Completed.";
                InsertEventDetails(eBO);

                //result = await callApi.ReturnCheckAPICall(result, apiBaseUrlForCheckReturn);
                //Return Check API to call to check if the previous month file is completed ppse
                result.p_REMITTANCE_ID = remttanceId;
                result.P_USERID = userID;
                result.P_PAYLOC_FILE_ID = 0;

                string apiBaseUrlForCheckReturn = GetApiUrl(_apiEndpoints.ReturnCheckProc);      //## Get 'status of a file'
                apiResult = await ApiPost(apiBaseUrlForCheckReturn, result);
                result = JsonConvert.DeserializeObject<ReturnCheckBO>(apiResult);   //## we get StatusCode and StatusText back, ie: 1 = Record Not Found.. 

                //Add functionality here to restrict file if the previous month file is still pending.

                AutoMatchBO autoMatchBO = new();
                //following is call to Automatch api
                //autoMatchBO = await callApi.GetAutoMatchResult(remttanceId, apiBaseUrlForAutoMatch);
                apiResult = await ApiGet($"{apiBaseUrlForAutoMatch}{remttanceId}");
                autoMatchBO = JsonConvert.DeserializeObject<AutoMatchBO>(apiResult);

                if (Convert.ToInt32(totalRecordsInF) < total || Convert.ToInt32(totalRecordsInF) > 10000)
                {
                    totalRecordsInF = total.ToString();
                }

                TempData["showOnlyTotals"] = "Total records in uploaded file are <b>: " + totalRecordsInF + "</b><br />"
                                  + " Total number of records inserted successfully into database are: <b>" + total + "</b><br />"
                                  + " Employers processed:  <b>" + initialiseProcBO.P_EMPLOYERS_PROCESSED + "</b><br />";

                TempData["msgExtra"] = "Total records in uploaded file are <b>: " + totalRecordsInF + "</b><br />"
                                    + " Total number of records inserted successfully into database are: <b>" + total + "</b><br />"
                                    + "Persons Matched : " + autoMatchBO.personMatchCount + "<br />"
                                    + "Folders Matched : " + autoMatchBO.folderMatchCount + "<br />"
                                   //+ "Records ready to post : " + GetIntValueFromString(totalMatched) + "<br />"
                                   ;

                //check if AutoMatch successfull then proceed otherwise skip and take file to Dashboard.
                if (autoMatchBO.L_STATUS_CODE == 3)
                {
                    TempData["MsgError"] = "Previous month file is still in process by WYPF";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("Bulk data insert is successfull and Auto Matching successfull");

                //Update Event Details table and add Auto Match successfull.
                eBO.remittanceID = remttanceId; //Convert.ToInt32(remittanceID);
                eBO.remittanceStatus = 1;
                eBO.eventTypeID = 80;
                eBO.notes = "Auto matching done.";

                InsertEventDetails(eBO);
            }

            catch (Exception ex)
            {

                _logger.LogError($"Bulk Data or AutoMatch is failed, it is implemented in Home controller. Detail: {ex.StackTrace}");
                TempData["MsgError"] = "Please refresh your page in couple of minutes.";

                var vm = new ErrorViewModel()
                {
                    ApplicationId = Constants.EmployersPortal,
                    Message = ex.Message,
                    Source = apiBaseUrlForTotalRecords,
                    ErrorPath = $"Home->RemittanceInsertDB(int {remttanceId})",
                    RemittanceInfo = remttanceId.ToString(),
                    StackTrace = ex.StackTrace,
                    UserId = CurrentUserId()
                };

                await ErrorLog_Insert(vm);
            }

            return View(errorAndWarningViewModelWithRecords);
        }
        /// <summary>
        /// following action is not in use
        /// </summary>
        /// <param name="remID"></param>
        /// <returns></returns>
        public IActionResult LooseMatchRecords(int remID)
        {
            return RedirectToAction("RemittanceInsertDB", new { id = remID });
        }

        public IActionResult Privacy()
        {
            return View();
        }



        /// <summary>
        /// List to show months in dropdown menu
        /// </summary>
        /// <returns></returns>
        public List<NameOfMonths> GetMonths(string valueItem)
        {
            List<NameOfMonths> nameOfMonths = new List<NameOfMonths>(){
                //new NameOfMonths(){text = "Select Month",value = "month"},
                new NameOfMonths(){text = "JANUARY",value = "JANUARY"},
                new NameOfMonths(){text = "FEBRUARY",value = "FEBRUARY"},
                new NameOfMonths(){text = "MARCH",value = "MARCH"},
                new NameOfMonths(){text = "APRIL",value = "APRIL"},
                new NameOfMonths(){text = "MAY",value = "MAY"},
                new NameOfMonths(){text = "JUNE",value = "JUNE"},
                new NameOfMonths(){text = "JULY",value = "JULY"},
                new NameOfMonths(){text = "AUGUST",value = "AUGUST"},
                new NameOfMonths(){text = "SEPTEMBER",value = "SEPTEMBER"},
                new NameOfMonths(){text = "OCTOBER",value = "OCTOBER"},
                new NameOfMonths(){text = "NOVEMBER",value = "NOVEMBER"},
                new NameOfMonths(){text = "DECEMBER",value = "DECEMBER"}
            }.ToList();

            return nameOfMonths;
        }

        public List<NameOfMonths> GetOption(string valueItem)
        {
            List<NameOfMonths> postings = new List<NameOfMonths>()
            {
               new NameOfMonths{ text = "1st posting", value = "1" },
               new NameOfMonths{ text = "2nd posting for same month", value = "2" },
               new NameOfMonths{ text = "File has previous month data", value = "3" }

            }.ToList();

            return postings;


        }
        /// <summary>
        /// List to show months in dropdown menu
        /// </summary>
        /// <returns></returns>
        public List<YearsBO> GetYears(string valueItem)
        {


            var validYears = ConfigGetValue("ValidPayrollYears").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var yearList = validYears.Select(a => new YearsBO()
            {
                years = a
            }).ToList();

            return yearList;

        }


        /// <summary>
        /// following method will show list of sub payroll provider with login name and id
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        private async Task<List<PayrollProvidersBO>> CallPayrollProviderService(string userName)
        {
            var subPayrollList = new List<PayrollProvidersBO>();
            string apiBaseUrlForSubPayrollProvider = GetApiUrl(_apiEndpoints.SubPayrollProvider);

            var apiResult = await ApiGet(apiBaseUrlForSubPayrollProvider + userName);
            subPayrollList = JsonConvert.DeserializeObject<List<PayrollProvidersBO>>(apiResult);

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

            result.IsSuccess = string.IsNullOrEmpty(result.Message);
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
            if (formFile == null)
            {
                return Json("No file added. Please add a valid file and try again.");
            }

            var fileTypeCheck = IsFileValid(formFile);

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
