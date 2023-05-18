//using HtmlAgilityPack;
using MCPhase3.CodeRepository;
using MCPhase3.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using MCPhase3.CodeRepository.InsertDataProcess;
using MCPhase3.Common;
using static MCPhase3.Common.Constants;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MCPhase3.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _host;
        private readonly IConfiguration _Configure;
        private readonly IHostingEnvironment _Environment;
        //public const string SessionKeyClientId = "_clientId";
        //public const string SessionKeyUserID = "_UserName";
        //public const string SessionKeyMonth = "_month";
        //public const string SessionKeyFileName = "_fileName";
        //public const string SessionKeyTotalRecords = "_totalRecords";
        //public const string SessionKeyRemittanceID = "_remittanceID";
        //public const string SessionKeyEmployerName = "_employerName";
        //public const string SessionKeyClientType = "_clientType";
        //public const string SessionKeyPayLocId = "_Id";
        //public const string SessionKeyPayrollProvider = "_payrollProvider";
        //public const string SessionKeyYears = "_years";
        //public const string SessionKeyReturnInit = "_returnInit";
        //public const string SessionKeyPosting = "_posting";
        //public const string SessionKeySchemeName = "_scheme";


        string apiBaseUrlForRemittanceInsert = string.Empty;
        string apiBaseUrlForAutoMatch = string.Empty;
        //following class I am using to consume api's
        TotalRecordsInsertedAPICall callApi = new TotalRecordsInsertedAPICall();
        EventDetailsBO eBO = new EventDetailsBO();
        EventsTableUpdates eventUpdate;
        InsertDataTable dataTableToDB = new InsertDataTable();
        DataTable excelDt;
        DataTable excelDt1;
        MyModel modelDT = new MyModel();        
        CheckSpreadsheetValuesSample repo = new CheckSpreadsheetValuesSample();               

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment host,IHostingEnvironment environment, IConfiguration configuration) : base (configuration)
        {
            _logger = logger;
            _host = host;
            _Configure = configuration;
            _Environment = environment;
        
        }
        
        /// <summary>
        /// To LG - Remittance page
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            string clientType = string.Empty;           
            //Session can set here to check if logged in user is Fire or LG.
            clientType = ContextGetValue(Constants.SessionKeyClientType);
            //check if year, month and type of posting is already selected from session
            string monthSelected = ContextGetValue(Constants.SessionKeyMonth)?? string.Empty;
            string yearSelected = ContextGetValue(Constants.SessionKeyYears) ?? string.Empty;
            string postSelected = ContextGetValue(Constants.SessionKeyPosting) ?? string.Empty;
            string userName = ContextGetValue(Constants.SessionKeyUserID);

            //client type is null or empty goto login page again
            if (string.IsNullOrEmpty(clientType))
            {
                return RedirectToAction("Index","Login");
            }

            //if logged in user is Fire then goto FireIndex
            if (clientType.Equals("FIRE"))
            {
                return RedirectToAction("IndexFire","Home");
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

            subPayList = subPayList.Where(x=>x.pay_location_ID != null).ToList();             

                ViewBag.Paylocations = subPayList.Select(x => new SelectListItem()
                {
                    Text = x.pay_location_name,
                    Value = x.pay_location_ID.ToString()
                });           
           
            return View();
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

        /// <summary>
        /// following Create is to work with LG pages
        /// </summary>
        /// <param name="paymentFile"></param>
        /// <param name="monthsList"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile paymentFile, string monthsList, string yearsList, string posting)
        {
            var fileUplaodStatus = IsFileValid(paymentFile);

            if (!fileUplaodStatus.IsSuccess)
            {
                TempData["MsgM"] = fileUplaodStatus.Message;
                return RedirectToAction("Index", "Home", null, "uploadFile");
            }

            if (monthsList.Equals("month") || yearsList.Equals("Select Year")) {
                TempData["MsgM"] = "Error: You must select Year and Month to continue";
                return RedirectToAction("Index", "Home", null, "uploadFile");
            }

            //Add selected name of month into Session, filename and total records in file.
            HttpContext.Session.SetString(Constants.SessionKeyMonth, monthsList);
            HttpContext.Session.SetString(Constants.SessionKeyYears, yearsList);
            HttpContext.Session.SetString(Constants.SessionKeyPosting, posting);
            HttpContext.Session.SetString(Constants.SessionKeySchemeName, "LGPS");

            TotalRecordsInsertedAPICall apiCall = new TotalRecordsInsertedAPICall();
            var fileCheckBO = new CheckFileUploadedBO
            {
                P_Month = monthsList,
                P_Year = yearsList
            };
            string userName = ContextGetValue(SessionKeyUserID);
            string empName = ContextGetValue(Constants.SessionKeyEmployerName);
            string empID = ContextGetValue(Constants.SessionKeyPayrollProvider);
            
            fileCheckBO.P_EMPID = empID;

            //Member titles coming from config - 
            string[] validTitles = ConfigGetValue("ValidMemberTitles").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //Check all invalid signs from file and show error to employer
            string[] invalidSigns = ConfigGetValue("SignToCheck").Split(",".ToCharArray(),StringSplitOptions.RemoveEmptyEntries);

            //update Event Details table File is uploaded successfully.
            string apiBaseUrlForInsertEventDetails = ConfigGetValue("WebapiBaseUrlForInsertEventDetails");
            string apiBaseUrlForCheckFileAvailable = ConfigGetValue("WebapiBaseUrlForCheckFileAvailable");
            
            //check if records were uploaded previously for the selected month and year.
            int result1 = await apiCall.CheckFileAvailable(fileCheckBO, apiBaseUrlForCheckFileAvailable);

            string fileExt = string.Empty;
            string path = string.Empty;
            string spreadSheetName = string.Empty;
            string errorMessage = string.Empty;
            bool answer = true;
            // string fileNameWithoutExt = string.Empty;
            string webRootPath = _host.WebRootPath;
            MyModel model = new MyModel();
            //get list of paylocations
            //List<NameOfMonths> payLocations = GetPaylocations();
            List<PayrollProvidersBO> subPayList = await CallPayrollProviderService(userName);
            //bypass year and month check 
            
            //if (postingNumber == (int)PostingType.First)
            if (posting == "1") // ## PostingType.First
            {
                if (result1 == 1)
                {
                    TempData["MsgM"] = $"File is already uploaded for the month: {monthsList} and payrol period: {yearsList} <br/> You can goto Dashboard and start process on file from there. ";
                    return RedirectToAction("Index", "Home", null, "uploadFile");
                }
            }
          
           

            if (excelDt != null)
            {
                excelDt.Clear();
            }

            //clear Datatable when upload file button is clicked.
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(paymentFile.FileName);
            fileExt = Path.GetExtension(paymentFile.FileName);

            fileNameWithoutExt  = $"{empName.Replace(" ", "-")}_{yearsList}_{monthsList}_{posting}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.{fileExt}";

            path = Path.Combine(webRootPath + "/UploadedFiles/", fileNameWithoutExt);
            //copy file to local folder
            string _customerUploadsLocalPath = ConfigGetValue("FileUploadPath");
            if (!Path.Exists(_customerUploadsLocalPath)) {
                TempData["MsgM"] = "Error: File upload area not defined. Please contact support.";
                return RedirectToAction("Index", "Home", null, "uploadFile");
            }

            using (System.IO.FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                await paymentFile.CopyToAsync(fileStream);
                _logger.LogInformation("File is copied to local folder.");
            }

            //CopyFileToFolderAsync(fileNameWithoutExt, _host.WebRootPath, "/UploadedFiles/");

            //saved file
            //string fullPathWithFileName = Path.Combine(path,fileExt);
            try
            {
                excelDt = CommonRepo.ExcelToDT(path, out errorMessage);
            }
            catch (Exception ex)
            {
                TempData["MsgM"] = "File has an error or missing something " + ex.Message;
                return RedirectToAction("Index", "Home", null, "uploadFile");
            }
              

            if (excelDt == null)
            {
                TempData["MsgM"] = errorMessage;
                //model.myErrorMessageText += errorMessage;
                return RedirectToAction("Index", "Home", null, "uploadFile");                    
            }
            //allow a sign in a column
            // excelDt = AllowedSigns(excelDt);
                

            // change column heading Name
            if (!CommonRepo.ChangeColumnHeadings(excelDt, out errorMessage))
            {
                TempData["MsgM"] = errorMessage;
                // model.myErrorMessageText += errorMessage;
                return RedirectToAction("Index", "Home", null, "uploadFile");
                //return RedirectToAction("Index", "Home");
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
            HttpContext.Session.SetString(Constants.SessionKeyTotalRecords, (numberOfRows-1).ToString());//HttpContext.Session.SetString(Constants.SessionKeyTotalRecords, (numberOfRows - 1).ToString());

            // var validYears = GetConfigValue("ValidPayrollYears").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            //Seperated LG and Fire functions
            //user selects a year from dropdown list so no need to provide seperate list of years. posting will ignore same month validation.
            string CheckSpreadSheetErrorMsg = repo.CheckSpreadsheetValues(modelDT.stringDT, monthsList, posting, yearsList, subPayList,validTitles, invalidSigns, ref errorMessage);

            if (!errorMessage.Equals(""))
            {
                TempData["MsgM"] = null;
                //following tempdata is showing list of errors in file.
                TempData["MsgM"] = "<h2> Please remove following errors from file and upload again </h2><br />" + CheckSpreadSheetErrorMsg;
                //HtmlString str = new HtmlString( "<h2> Please remove following errors from file and upload again </h2>" + CheckSpreadSheetErrorMsg);
                return RedirectToAction("Index", "Home",null, "uploadFile");
            }
            else
            {
                    
                //string designDocPath = GetConfigValue("DesginXMLFilePath");
                //read design XML from wwwroot folder
                string designDocPath = string.Concat(this._Environment.WebRootPath, "\\DesignXML\\DataIntegration_Design.xml");
                try
                {
                    //save datatable for future use
                    dataTableToDB.PassDt(0, "", "", "", "", excelDt, designDocPath);
                    TempData["MsgM"] = "File is uploaded successfully";
                      
                    return RedirectToAction("Index", "Home", null, "uploadFile");
                }
                catch (Exception ex)
                {
                    TempData["MsgM"] = "File is not uploaded, please check error in qoutes : '" + " < b > "+ ex.Message + " </ b > '";
                    return RedirectToAction("Index", "Home", null, "uploadFile");
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
            if (file is null) {
                result.Message += "No file selected.";
                return result;
            }
            int fileSizeLimit = 5100000;

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
            TotalRecordsInsertedAPICall apiCall = new TotalRecordsInsertedAPICall();
            CheckFileUploadedBO fileCheckBO = new CheckFileUploadedBO();
            fileCheckBO.P_Month = monthsList;
            fileCheckBO.P_Year = yearsList;
            string userName = ContextGetValue(Constants.SessionKeyUserID);
            string empName = ContextGetValue(Constants.SessionKeyEmployerName);
            string empID = ContextGetValue(Constants.SessionKeyPayLocId);
            HttpContext.Session.SetString(Constants.SessionKeySchemeName, "FIRE");
            fileCheckBO.P_EMPID = empID;

            string[] validTitles = ConfigGetValue("ValidMemberTitles").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //Check all invalid signs from file and show error to employer
            string[] invalidSigns = ConfigGetValue("SignToCheck").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string apiBaseUrlForCheckFileAvailable = ConfigGetValue("WebapiBaseUrlForCheckFileAvailable");
            //check if file is uploaded for the selected month and year.
            int result1 = await apiCall.CheckFileAvailable(fileCheckBO, apiBaseUrlForCheckFileAvailable);
            string fileExt = string.Empty;
            string path = string.Empty;
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
                if (result1 == 1)
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
                //Only excel files allowed
                
                fileNameWithoutExt = fileNameWithoutExt+"_" +empName+ DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + fileExt;
                path = Path.Combine(webRootPath + "/UploadedFiles/", fileNameWithoutExt);
                
                //copy file to local folder
                using (System.IO.FileStream fileStream = new FileStream(path, FileMode.Create))
                {
                    await files.CopyToAsync(fileStream);
                    _logger.LogInformation("File is copied to local folder.");
                }
              
                //saved file
                //string fullPathWithFileName = Path.Combine(path,fileExt);
                try
                {
                    excelDt = CommonRepo.ExcelToDT(path, out errorMessage);
                }
                catch (Exception ex)
                {
                    TempData["MsgM"] = "File is not uploaded, please check error in qoutes : '" + " < b > " + ex.Message + " </ b > '";
                    return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                }

                if (excelDt == null)
                {
                    TempData["MsgM"] = errorMessage;
                    //model.myErrorMessageText += errorMessage;
                    return RedirectToAction("IndexFire", "Home", null, "uploadFile");
                }
                //allow a sign in a column
                //excelDt = AllowedSigns(excelDt);
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
                string CheckSpreadSheetErrorMsg = repo.CheckSpreadsheetValues(modelDT.stringDT, monthsList, posting, yearsList, subPayList,validTitles, invalidSigns, ref errorMessage);
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
                    //DataTable dtable = dataTableToDB.KeepDataTable(excelDt);
                    //dtable.Clear();
                    //read design XML from wwwroot folder
                    string designDocPath = string.Concat(_Environment.WebRootPath, "\\DesignXML\\DataIntegration_Design.xml");
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

            //remove the browser response issue of pen testing
            if (string.IsNullOrEmpty(ContextGetValue("_UserName")))
            {
                contributionBO = null;
               
                RedirectToAction("Index", "Login");
            }
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
        public async Task<IActionResult> Remad(MonthlyContributionBO contributionBO)
        {
            RemadShowTotalsValues formTotals = new RemadShowTotalsValues();
            
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
          
            //Get api url from appsetting.json
            apiBaseUrlForRemittanceInsert = ConfigGetValue("WebAPIBaseUrlForRemittanceInsert");
            //Get api url from appsetting.json
            string apiBaseUrlForRemittanceGet = ConfigGetValue("WebAPIBaseUrlForRemittanceGet");
            ///API URI is getting from Apsetting.json file.
            string apiBaseUrlForInsertEventDetails = ConfigGetValue("WebapiBaseUrlForInsertEventDetails");
          
                    using (HttpClient client = new HttpClient())
                    {
                        try
                        {
                            StringContent content = new StringContent(JsonConvert.SerializeObject(contributionBO), Encoding.UTF8, "application/json");
                            string endpoint = apiBaseUrlForRemittanceInsert;

                            using (var Response = await client.PostAsync(endpoint, content))
                            {
                                if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    // var contributionBONew = JsonConvert.SerializeObject(contributionBO);
                                    _logger.LogInformation("Remittance API Call successfull");
                                    TempData["Msg"] = "Remittance successfully inserted into database.";
                                    try
                                    {
                                        TotalRecordsInsertedAPICall callApi = new TotalRecordsInsertedAPICall();
                                        var totalRecords = callApi.CallAPI(contributionBO.UploadedFileName.ToUpper(), apiBaseUrlForRemittanceGet);
                                        string num = totalRecords.Substring(totalRecords.IndexOf(":") + 1);
                                        remittanceID = num.Trim(new char[] { '"', '}', ']' });
                                        _logger.LogInformation("API to get Remittance id is successfull");

                                        //Update Event Details table and remittance inserted.
                                        eBO.remittanceID = Convert.ToInt32(remittanceID);
                                        eBO.remittanceStatus = 1;
                                        eBO.eventTypeID = 2;
                                        eBO.eventDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                                        eBO.notes = "New remitance submitted and remitance ID created for the uploaded file.";

                                        callApi.InsertEventDetails(eBO, apiBaseUrlForInsertEventDetails);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError("API to insert Remittance shows error.");
                                        TempData["Msg"] = "System is showing some error to send Remittance totals, Please try again";
                                        //monthAndPayroll.Clear();
                                        return View(contributionBO);
                                    }

                                    if (!string.IsNullOrEmpty(remittanceID))
                                    {
                                        remittanceID = CustomDataProtection.Decrypt(remittanceID);
                                    }

                            //return RedirectToAction("RemittanceInsertDB", "Home", new { id = remittanceID });
                            return RedirectToAction("MoveFileForFTP", "Home", new { id = remittanceID });

                                }
                                else
                                    {
                                        ModelState.Clear();
                                        _logger.LogError(string.Empty, "Error occured while calling Remittance Insert API");
                                        TempData["Msg"] = "System is showing some error to send Remittance totals, Please try again";
                            //monthAndPayroll.Clear();

                            //remove the browser response issue of pen testing
                            if (string.IsNullOrEmpty(ContextGetValue("_UserName")))
                            {
                                contributionBO = null;

                                RedirectToAction("Index", "Login");
                            }
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
            ErrorAndWarningViewModelWithRecords errorAndWarningViewModelWithRecords = new ErrorAndWarningViewModelWithRecords();

            errorAndWarningViewModelWithRecords.remittanceID = id;
            
            //decode remittance id in url
            if (!string.IsNullOrEmpty(id))
            {
                id = CustomDataProtection.Encrypt(id);
            }
            string path = string.Empty;
            string uploadedFileName = ContextGetValue(Constants.SessionKeyFileName);
            //Get api url from appsetting.json           
            string apiBaseUrlForInsertEventDetails = ConfigGetValue("WebapiBaseUrlForInsertEventDetails");
            

            path = Path.Combine(_host.WebRootPath + "/UploadedFiles/", uploadedFileName);
            //copied to folder 
            //string destPathWithFileName = Path.Combine(_host.WebRootPath + "/TransferTo/", contributionBO.UploadedFileName);
            string destPath = Path.Combine(_host.WebRootPath + "/TransferTo/");
            //copy file to local folder

            try
            {
                bool result = CommonRepo.CopyFileToFolder(path, destPath, uploadedFileName);
                //Update Event Details table and add File Uploaded and ready to FTP
                eBO.remittanceID = Convert.ToInt32(id);
                eBO.remittanceStatus = 1;
                eBO.eventTypeID = 1;
                eBO.eventDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                eBO.notes = "Monthly Posting file uploaded onto web portal.";

                //update Event Details table File is uploaded successfully.                               
                callApi.InsertEventDetails(eBO, apiBaseUrlForInsertEventDetails);

                string clientID = ContextGetValue(Constants.SessionKeyClientId);
                string schemeName = ContextGetValue(Constants.SessionKeySchemeName);
                string userName = ContextGetValue(Constants.SessionKeyUserID);
                //get total records from in file from datatable.
                int totalRecordsInF = Convert.ToInt32(ContextGetValue(Constants.SessionKeyTotalRecords));
                totalRecordsInF += 1;
                rangeOfRowsModel.P_USERID = userName;
                rangeOfRowsModel.P_REMITTANCE_ID = Convert.ToInt32(id);
                rangeOfRowsModel.P_NUMBER_OF_VALUES_REQUIRED = totalRecordsInF;

                
                //Get the max Datarow id from MC_CONTRIBUTIONS_RECD to insert bulk data.
                int row = 0;
                string insertDataCounter = ConfigGetValue("WebapiBaseUrlForInsertDataCounter");
                row = await callApi.counterAPI(insertDataCounter, rangeOfRowsModel);//row = await callApi.counterAPI(insertDataCounter);
                
                //following datatable will change column names of datatable to DB column names and insert data from excelDT.
                DataTable newDT = dataTableToDB.KeepDataTable(row + 1, userName, schemeName, clientID, id.ToString()) ;
                //upload file to database using api
                string insertDataConn = ConfigGetValue("WebapiBaseUrlForInsertData");
                newDT.AcceptChanges();
                bool result1 = await callApi.InsertDataApi(newDT, insertDataConn);
                newDT.Dispose();


                if (result1)
                {
                    eBO.remittanceID = Convert.ToInt32(id);
                    eBO.remittanceStatus = 1;
                    eBO.eventTypeID = 4;
                    eBO.eventDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                    eBO.notes = "Bulk data insert into database.";
                    //update Event Details table File is uploaded successfully.                               
                    callApi.InsertEventDetails(eBO, apiBaseUrlForInsertEventDetails);
                }
                else
                {
                    TempData["MsgError"] = "System is showing some error while uploading file to database, Please try again";
                    RedirectToAction("Admin","Home");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error where file moves to FTP folder on WebServer.");
                TempData["MsgError"] = "System is showing some error, Please try again";
            }
            //return RedirectToAction("RemittanceInsertDB", "Home", new { id = remittanceID });

            
            return View(errorAndWarningViewModelWithRecords);
        }
        /// <summary>
        /// Here to start encode remittance
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> RemittanceInsertDB(string id)
        {
            int id1 = 0;
            if (!string.IsNullOrEmpty(id))
            {
                id1 = Convert.ToInt32(CustomDataProtection.Encrypt(id));
            }
            HelpTextBO helpTextBO = new HelpTextBO();
           
            ReturnCheckBO result = new ReturnCheckBO();
            //AutoMatchBO autoMatchBO = new AutoMatchBO();
            ErrorAndWarningViewModelWithRecords errorAndWarningViewModelWithRecords = new ErrorAndWarningViewModelWithRecords();
            InitialiseProcBO initialiseProcBO = new InitialiseProcBO();
            string userID = ContextGetValue(Constants.SessionKeyUserID);
            string empURL = ConfigGetValue("WebapiBaseUrlForEmployerName");            
            //string insertDataConn = GetConfigValue("WebapiBaseUrlForInsertData");

            //callApi.InsertDataApi(excelDt, insertDataConn);

            //following newID needs to replace with id(remittance id)
            int newID = id1;
            errorAndWarningViewModelWithRecords.ALERT_TYPE_REF = "ALL";
            errorAndWarningViewModelWithRecords.ALERT_CLASS = "Error and Warnings";
            //pass decoded id to view model class
            errorAndWarningViewModelWithRecords.remittanceID = id; //errorAndWarningViewModelWithRecords.remittanceID = newID.ToString();
            errorAndWarningViewModelWithRecords.ALERT_DESC = "All the Errors and Warnings in file";

            List<AutoMatchBO> BO = new List<AutoMatchBO>();
            //call Automatch api url from appsettings.json
            apiBaseUrlForAutoMatch = ConfigGetValue("WebapiBaseUrlForAutoMatch");
            //url to get total number of records in database
            string apiBaseUrlForTotalRecords = ConfigGetValue("WebAPIBaseUrlForRemittanceInsert");
            string apiBaseUrlForInitialiseProc = ConfigGetValue("WebAPIBaseUrlForInitialiseProc");

            initialiseProcBO.P_REMITTANCE_ID = newID;
            initialiseProcBO.P_USERID = userID;
            //initailise procedure api is added at 25/07/22
           

            try
            {
                string totalRecords = await callApi.CallAPIRem(id1, apiBaseUrlForTotalRecords);
                string num = totalRecords.Substring(totalRecords.IndexOf(":") + 2);
                int total = Convert.ToInt32(num.Remove(num.IndexOf('"')));
                string totalRecordsInF = ContextGetValue(Constants.SessionKeyTotalRecords);
                //following loop will keep on until it finds a record in database.//Windows bulk insertion service submits only 10000 records at time so I Need to keep check until all the records inserted.
                while (total == 0 || Convert.ToInt32(totalRecordsInF) > total)
                    {
                        totalRecords = await callApi.CallAPIRem(id1, apiBaseUrlForTotalRecords);
                        num = totalRecords.Substring(totalRecords.IndexOf(":") + 2);
                        total = Convert.ToInt32(num.Remove(num.IndexOf('"')));
                    }
                //if (GetContextValue(Constants.SessionKeyReturnInit) == null)
                //{
                    initialiseProcBO = await callApi.InitialiseProce(initialiseProcBO, apiBaseUrlForInitialiseProc);
                   // HttpContext.Session.SetString(Constants.SessionKeyReturnInit, initialiseProcBO.P_RECORDS_PROCESSED.ToString());
               // }
                
                //HttpContext.Session.SetString(Constants.SessionKeyReturnInit, initialiseProcBO.P_RECORDS_PROCESSED.ToString());
                //Return Check API to call to check if the previous month file is completed 
                result.p_REMITTANCE_ID = id1;
                result.P_USERID = userID;
                result.P_PAYLOC_FILE_ID = 0;

                

                string apiBaseUrlForCheckReturn = ConfigGetValue("WebAPIBaseUrlForReturnCheckProc");
                string apiBaseUrlForInsertEventDetails = ConfigGetValue("WebapiBaseUrlForInsertEventDetails");
                
                eBO.remittanceID = id1;
                eBO.remittanceStatus = 1;
                eBO.eventTypeID = 50;
                eBO.eventDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                eBO.notes = "Initial Processing Completed.";
                callApi.InsertEventDetails(eBO, apiBaseUrlForInsertEventDetails);

                result = await callApi.ReturnCheckAPICall(result, apiBaseUrlForCheckReturn);
                //Add functionality here to restrict file if the previous month file is still pending.

                AutoMatchBO autoMatchBO = new AutoMatchBO();
                //following is call to Automatch api
                autoMatchBO = await callApi.CallAPI(id1, apiBaseUrlForAutoMatch);
                                
                if (Convert.ToInt32(totalRecordsInF) < total || Convert.ToInt32(totalRecordsInF) > 10000)
                    {
                        totalRecordsInF = total.ToString();
                    }

                TempData["showOnlyTotals"] = "Total records in uploaded file are <b>: " + totalRecordsInF + "</b><br />"
                                  + " Total number of records inserted successfully into database are: <b>" + total + "</b><br />"
                                  +" Employers processed:  <b>" + initialiseProcBO.P_EMPLOYERS_PROCESSED + "</b><br />";

                TempData["msgExtra"] = "Total records in uploaded file are <b>: " + totalRecordsInF + "</b><br />"
                                    + " Total number of records inserted successfully into database are: <b>" + total + "</b><br />"
                                    + "Persons Matched : " + autoMatchBO.personMatchCount + "<br />"
                                    + "Folders Matched : " + autoMatchBO.folderMatchCount + "<br />"
                                    //+ "Records ready to post : " + GetIntValueFromString(totalMatched) + "<br />"
                                   ;

                //check if AutoMatch successfull then proceed otherwise skip and take file to Dashboard.
                if (autoMatchBO.L_STATUS_CODE == 3)
                {
                    TempData["Msg1"] = "Previous month file is still in process by WYPF";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("Bulk data insert is successfull and Auto Matching successfull");

                //Update Event Details table and add Auto Match successfull.
                eBO.remittanceID = id1; //Convert.ToInt32(remittanceID);
                eBO.remittanceStatus = 1;
                eBO.eventTypeID = 80;
                eBO.eventDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                eBO.notes = "Auto matching done.";
                //update Event Details table File is uploaded successfully.                               
                callApi.InsertEventDetails(eBO, apiBaseUrlForInsertEventDetails);
                //eventUpdate.UpdateEventDetailsTable(eBO);
                // BO = JsonConvert.DeserializeObject<List<AutoMatchBO>>(result);
            }
            
            catch (Exception ex)
            {

                _logger.LogError("Bulk Data or AutoMatch is failed, it is implemented in Home controller.");
                TempData["MsgError"] = "Please refresh your page in couple of minutes.";
                //RedirectToAction("RemittanceInsertDB","Home", id);
            }

            //remove the browser response issue of pen testing
            if (string.IsNullOrEmpty(ContextGetValue("_UserName")))
            {
                errorAndWarningViewModelWithRecords = null;

                RedirectToAction("Index", "Login");
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
            return RedirectToAction("RemittanceInsertDB", new { id = remID});
        }
        /// <summary>
        /// get only number from a string. Not in use
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>        
        public string GetIntValueFromString(string source)
        {
            if (String.IsNullOrWhiteSpace(source))
                return string.Empty;
            var number = Regex.Match(source, @"\d+");
            if (number != null)
                return number.Value;
            else
                return string.Empty;
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        /// <summary>
        /// List to show months in dropdown menu
        /// </summary>
        /// <returns></returns>
        public List<NameOfMonths> GetMonths(string valueItem)
        {
            List<NameOfMonths> nameOfMonths = new List<NameOfMonths>()
                {
                    new NameOfMonths(){text = "Select Month",value = "month"},
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

            if (!string.IsNullOrEmpty(valueItem))
            {
                return nameOfMonths.OrderByDescending(x => x.value == valueItem).ThenBy(x=>x.value).ToList();
            }
            else
            {
                return nameOfMonths;
            }
        }

        public List<NameOfMonths> GetOption(string valueItem)
        {
            List<NameOfMonths> postings = new List<NameOfMonths>()
            {
               new NameOfMonths{ text = "1st posting", value = "1" },
               new NameOfMonths{ text = "2nd posting for same month", value = "2" },
               new NameOfMonths{ text = "File has previous month data", value = "3" }

            }.ToList();
            if (!string.IsNullOrEmpty(valueItem))
            {
                return postings.OrderByDescending(x => x.value == valueItem).ThenBy(x => x.value).ToList();
            }
            else
            {
                return postings;
            }

        }
        /// <summary>
        /// List to show months in dropdown menu
        /// </summary>
        /// <returns></returns>
        public List<YearsBO> GetYears(string valueItem)
        {
            
           
            var validYears = ConfigGetValue("ValidPayrollYears").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            // string validYears = GetConfigValue("ValidPayrollYears").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToString();

            var yearList = new List<YearsBO>();

            //YearsBO[] bO = new YearsBO[validYears.Length];
            foreach (var item in validYears)
            {
                yearList.Add(new YearsBO
                {
                    years = item
                });
            }
            if (!string.IsNullOrEmpty(valueItem))
            {
                return yearList.OrderByDescending(x => x.years == valueItem).ThenBy(x => x.years).ToList();
            }
            else
            {
                return yearList;
            }

            return yearList;
        }
        

        /// <summary>
        /// following method will show list of sub payroll provider with login name and id
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        private async Task<List<PayrollProvidersBO>> CallPayrollProviderService(string userName)
        {
            List<PayrollProvidersBO> subPayrollList = new List<PayrollProvidersBO>();
            string apiBaseUrlForSubPayrollProvider = ConfigGetValue("WebapiBaseUrlForSubPayrollProvider");
            string apiResponse = String.Empty;

            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(apiBaseUrlForSubPayrollProvider + userName))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        subPayrollList = JsonConvert.DeserializeObject<List<PayrollProvidersBO>>(result);
                    }
                }
            }

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
            for (int i=0; i < countRows; i++)
            {
                foreach (var item in allowedColumns)
                {
                    foreach (var sign in allwedSigns)
                    {
                        dataTable.Rows[i][item] = dataTable.Rows[i][item].ToString().Replace(sign,"");
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
                throw ex;
            }
            finally
            {
                dataTable.Dispose();
            }           
            
        }
    }
}
