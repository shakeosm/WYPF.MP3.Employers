using DocumentFormat.OpenXml.EMMA;
using MCPhase3.CodeRepository;
using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    public class AdminController : BaseController
    {
        //public const string SessionKeyUserID = "_UserName";
        //public const string SessionKeyEmployerName = "_employerName";
        //public const string SessionKeyPaylocFileID = "_PaylocFileID";
        //public const string SessionKeyRemittanceID = "_remittanceID";
        //public const string SessionKeyPayLocId = "_Id";
        private readonly IConfiguration _configuration;
        TotalRecordsInsertedAPICall callApi = new TotalRecordsInsertedAPICall();
        
        public AdminController(IConfiguration configuration) : base(configuration)
        {
            _configuration = configuration;        
        }
        /// <summary>
        /// Home page with all the icons
        /// </summary>
        /// <returns></returns>
        public IActionResult Home(int? id)
        {
            int remittanceID = 0;
            string apiBaseUrlForInsertEventDetails = ConfigGetValue("WebapiBaseUrlForInsertEventDetails");
            EventDetailsBO eBO = new EventDetailsBO();
            if (id == 1)
            {
                try
                {
                    remittanceID = (int)HttpContext.Session.GetInt32(Constants.SessionKeyRemittanceID);
                }
                catch (Exception ex)
                {
                    TempData["Msg1"] = "Session expired, please login again.";
                    return RedirectToAction("Index", "Login");
                }
                
                eBO.remittanceID = remittanceID;
                //eBO.P_PAYLOC_FILE_ID = Convert.ToInt32(rBO.P_PAYLOC_FILE_ID);
                eBO.remittanceStatus = 1;
                eBO.eventTypeID = 105;
                eBO.eventDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                eBO.notes = "Data quality score threshold check skipped, File passed to WYPF by Emp for processing.";
                //eventUpdate.UpdateEventDetailsTable(eBO);
                //update Event Details table File is uploaded successfully.
                //I have disabled it for staff.
                callApi.InsertEventDetails(eBO, apiBaseUrlForInsertEventDetails);
            }
            return View();
        }
        /// <summary>
        /// show list of all the remittance to employers
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index(int? pageNumber)
        {
            List<GetRemittanceStatusByUserBO> dashboardBO = new List<GetRemittanceStatusByUserBO>();
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);
            //ViewBag.EmployerName = GetContextValue(Constants.SessionKeyEmployerName);
            var userid = ContextGetValue(Constants.SessionKeyUserID);
            dashboardBO = await getDashboardValuesForEmployers(userid, "pending");
            var newBO = dashboardBO.AsQueryable<GetRemittanceStatusByUserBO>();
            newBO = newBO.OrderByDescending(x=>x.event_DateTime);
            int pageSize = 10;

            return View(PaginatedList<GetRemittanceStatusByUserBO>.CreateAsync(newBO, pageNumber ?? 1, pageSize));
           // return View(dashboardBO);
        }
        public async Task<IActionResult> CompletedFiles(int? pageNumber)
        {
            string payrolID = string.Empty;
            DashboardBO detailBO = new DashboardBO();
            List<DashboardBO> dashboardBO = new List<DashboardBO>();
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);
            var userid = ContextGetValue(Constants.SessionKeyUserID);
            payrolID = ContextGetValue(Constants.SessionKeyPayLocId);

            //List<DashboardBO> dashboardBO = new List<DashboardBO>();
            // DashboardBO dashboardBO1 = new DashboardBO();
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);
           // var userid = GetContextValue(SessionKeyUserID);
            detailBO.userId = userid;
            //detailBO.L_PAYROLL_PROVIDER = payrolID;
            detailBO.L_PAYROLL_PROVIDER = null;
            //detailBO.statusType = "COMPLETED";
            detailBO.statusType = "WYPF";

            //dashboardBO = await getDashboardValuesForEmployers(userid, "completed");
            dashboardBO = await getDashboardValues(detailBO);
            var newBO = dashboardBO.AsQueryable<DashboardBO>();
            newBO = newBO.OrderByDescending(x => x.return_Status_Code);
            int pageSize = 10;

            return View(PaginatedList<DashboardBO>.CreateAsync(newBO, pageNumber ?? 1, pageSize));
             //return View(dashboardBO);
        }

        /// <summary>
        /// show list of score history to employers
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ScoreHist(string remittanceId)
        {
            int remID = Convert.ToInt32(CustomDataProtection.Encrypt(remittanceId));
            List<DashboardHistScoreBO> dashboardScoreHistBO = new List<DashboardHistScoreBO>();
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);            
            dashboardScoreHistBO = await getDashboardScoreHist(remID);
            return View(dashboardScoreHistBO);
        }


        /// <summary>
        /// staff is not in use. 
        /// </summary>
        /// <returns></returns>
        public IActionResult Staff()
        {
            ViewBag.EmployerName = ContextGetValue(Constants.SessionKeyEmployerName);
            return View();
        }
        /// <summary>
        /// Update password for Staff and Employers
        /// </summary>
        /// <returns></returns>
        public IActionResult UpdatePassword()
        {
            LoginBO loginBO = new LoginBO() { userName = CurrentUserId() };
            //loginBO.isStaff = 0;
            return View(loginBO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(LoginBO loginBO)
        {
            if (!ModelState.IsValid) {
                ModelState.AddModelError("Password","Invalid password. Please use a strong password.");
                return View(loginBO);
            }

            string passwordPattern = @"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{9,255}$";
            var passwordStrength = Regex.Match(loginBO.password, passwordPattern);
            if (!passwordStrength.Success)
            {
                ModelState.AddModelError("Password", "Invalid password. Please use a strong password.");
                return View(loginBO);
            }

            List<string> passwordReq  = new List<string>
            {
                "at least nine characters",
                "one or more capital letters",
                "one or more lower case letters",
                "one or more numbers",
                "one or more special characters, for example !,”.#",
            };

            ViewBag.isStaff = 1;
            loginBO.userName = ContextGetValue(Constants.SessionKeyUserID);
            int result = await UpdatePasswordMethod(loginBO);
            if (result == 1)
            {
                TempData["Msg1"] = "Password updated successfully, please login using new password.";
                
                // add this new password to RedisCache.. Set a flag- that password is changed

                //return RedirectToAction("Index", "Login");
                return RedirectToAction("Logout", "Login");
            }
            if (result == 2)
            {
                TempData["UpdateMessage"] = "Password does not meet our complexity requirements: ";
                TempData["PasswordRequ"] = 1;
                return RedirectToAction("UpdatePassword", "Admin");
            }
            else
            {
                TempData["UpdateMessage"] = "Old Password is not correct, please try again.";
                return RedirectToAction("UpdatePassword", "Admin");
            }
        }

        private async Task<int> UpdatePasswordMethod(LoginBO loginBO)
        {
            string apiBaseUrlForLoginCheck = ConfigGetValue("WebapiBaseUrlForPasswordChange");
            string apiResponse = string.Empty;
            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(loginBO), Encoding.UTF8, "application/json");
                // string endPoint = apiLink;

                using (var response = await httpClient.PostAsync(apiBaseUrlForLoginCheck, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        loginBO.result = JsonConvert.DeserializeObject<int>(result);
                    }
                }
            }
            return loginBO.result;

        }
        /// <summary>
        /// call dashboard api
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        private async Task<List<DashboardBO>> getDashboardValues(DashboardBO dashboardBO)
        {
            string apiBaseUrlForDashboard = ConfigGetValue("WebapiBaseUrlForDashboard");
            List<DashboardBO> listBO = new List<DashboardBO>();
            DashboardBO bo = new DashboardBO();
            // bo.userId = userid;
            string apiResponse = string.Empty;
            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(dashboardBO), Encoding.UTF8, "application/json");
                using (var response = await httpClient.PostAsync(apiBaseUrlForDashboard, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        listBO = JsonConvert.DeserializeObject<List<DashboardBO>>(result);
                    }
                }
            }

            //if (status.Equals("completed"))
            //{
            //    return listBO.Where(x =>Convert.ToInt32(x.return_Status_Code) >= 105).ToList();
            //}

            //return listBO.Where(x => Convert.ToInt32(x.return_Status_Code) < 110).ToList();           
            return listBO;
        }

        public async Task<IActionResult> MasterDetailEmp( int? pageNumber, string remid)
        {
            try
            {
                string payrolID = string.Empty;
                DashboardViewModelNew viewModel = new DashboardViewModelNew();
                //Model class 
                DashboardBO detailBO = new DashboardBO();
                MasterDetailEmpListBO BO = new MasterDetailEmpListBO();
                string apiResponse = string.Empty;
                string WebapiBaseUrlForDetailEmpList = string.Empty;

                //check of PaylocID session has value then empty it.
                if (ContextGetValue(Constants.SessionKeyPaylocFileID) != null)
                {
                    HttpContext.Session.Clear();
                }
                payrolID = ContextGetValue(Constants.SessionKeyPayLocId);

                List<DashboardBO> dashboardBO = new List<DashboardBO>();
                // DashboardBO dashboardBO1 = new DashboardBO();
                //ViewBag.EmployerName = GetContextValue(SessionKeyEmployerName);
                var userid = ContextGetValue(Constants.SessionKeyUserID);
                detailBO.userId = userid;
                //detailBO.L_PAYROLL_PROVIDER = payrolID;
                detailBO.L_PAYROLL_PROVIDER = null;
                //detailBO.statusType = "PROCESSING";
                detailBO.statusType = "EMPLOYER";

                // DashboardBO dashboardBO1 = new DashboardBO();

                // var userid = GetContextValue(SessionKeyUserID);
                viewModel.dashboardBO = await getDashboardValues(detailBO);
                // viewModel.dashboardBO = await getDashboardValuesForEmployers(userid, "pending");


                //var newBO = dashboardBO.AsQueryable<DashboardBO>();
                //newBO = newBO.OrderByDescending(x => x.event_DateTime);
                //int pageSize = 10;

                WebapiBaseUrlForDetailEmpList = ConfigGetValue("WebapiBaseUrlForDetailEmpList");
                BO.L_USERID = ContextGetValue(Constants.SessionKeyUserID);
                int j = 0;

                if (!string.IsNullOrEmpty(remid))
                {
                    remid = CustomDataProtection.Encrypt(remid);
                    BO.L_REMITTANCE_ID = remid;
                    ViewData["selectedRow"] = remid;
                    BO.L_STATUSTYPE = "ALL";
                    viewModel.BO = viewModel.dashboardBO.Where(x => x.remittance_Id.ToString() == remid).FirstOrDefault();
                    using (var httpClient = new HttpClient())
                    {
                        StringContent content = new StringContent(JsonConvert.SerializeObject(BO), Encoding.UTF8, "application/json");
                        // string endPoint = apiLink;

                        using (var response = await httpClient.PostAsync(WebapiBaseUrlForDetailEmpList, content))
                        {
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string result = await response.Content.ReadAsStringAsync();
                                viewModel.details = JsonConvert.DeserializeObject<List<DashboardBO>>(result);
                            }
                        }
                    }

                    //select the Master table and remove the selected row from that.
                    viewModel.dashboardBO = viewModel.dashboardBO.Where(x => x.remittance_Id.ToString() != remid).ToList();
                    TempData["remID"] = remid;
                }
                //else
                //{
                //    foreach (var item in viewModel.dashboardBO)
                //    {
                //        // viewModel.BO = viewModel.dashboardBO.Where(x => x.remittance_Id.ToString() == remid).FirstOrDefault();

                //        BO.L_REMITTANCE_ID = item.remittance_Id;
                //        ViewData["selectedRow"] = remid;
                //        BO.L_STATUSTYPE = "ALL";
                //        using (var httpClient = new HttpClient())
                //        {
                //            StringContent content = new StringContent(JsonConvert.SerializeObject(BO), Encoding.UTF8, "application/json");
                //            // string endPoint = apiLink;

                //            using (var response = await httpClient.PostAsync(WebapiBaseUrlForDetailEmpList, content))
                //            {
                //                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                //                {
                //                    string result = await response.Content.ReadAsStringAsync();
                //                    viewModel.details = JsonConvert.DeserializeObject<List<DashboardBO>>(result);
                //                }
                //            }
                //            if (viewModel.details != null && viewModel.details.Count() == 1)
                //            {

                //                foreach (var obj in viewModel.details)
                //                {
                //                    viewModel.dashboardBO[j].paylocation_Name = obj.paylocation_Name;
                //                    j++;
                //                }
                //            }
                //            else
                //            {
                //               // foreach (var obj in viewModel.details)
                //               // {
                //                    viewModel.dashboardBO[j].paylocation_Name = GetContextValue(SessionKeyEmployerName);
                //                    j++;
                //                //}
                //            }
                //        }
                //    }
                //}

                //to add paylocation name in main remittance detail if it is one and if it is more than one then put the payroll provider name in 
                
                //else
                //{
                //    foreach (var obj in viewModel.details)
                //    {
                //        viewModel.dashboardBO[0].paylocation_Name = obj.paylocation_Name;
                //    }
                //    //show paylocation with the remittance if the file has only one paylocation.
                //    // ViewBag.EmployerName = GetContextValue(SessionKeyEmployerName);
                    
                //}

                int pageSize = 10;
                int i = 0;
                //viewModel.BO.remittance_Id = protector.Encode(viewModel.BO.remittance_Id);
                foreach (var model in viewModel.dashboardBO)
                {
                    viewModel.dashboardBO[i].remittance_IdEnc = CustomDataProtection.Encrypt(model.remittance_Id);
                    i++;
                }
                //viewModel.dashboardB
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["MsgError"] = "System is showing error, please try again later";
                return RedirectToAction("Index", "Login");
            }
        }

        /// <summary>
        /// Get values for Employers dashboard - Not in use, Started using new David's procedure in above action
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        private async Task<List<GetRemittanceStatusByUserBO>> getDashboardValuesForEmployers(string userid, string status)
        {
            string apiBaseUrlForDashboard = ConfigGetValue("WebapiBaseUrlForDashboardEmployers");
            List<GetRemittanceStatusByUserBO> listBO = new List<GetRemittanceStatusByUserBO>();
            string apiResponse = string.Empty;
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(apiBaseUrlForDashboard + userid))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        listBO = JsonConvert.DeserializeObject<List<GetRemittanceStatusByUserBO>>(result);
                    }
                }
            }
            if (status.Equals("completed"))
            {
                return listBO.Where(x => x.event_Type_ID > 100).ToList();
            }
           
             return listBO.Where(x => x.event_Type_ID <= 100).ToList();
            
        }
        /// <summary>
        /// Get values for Employers dashboard
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        private async Task<List<DashboardHistScoreBO>> getDashboardScoreHist(int remittanceId)
        {
            string apiBaseUrlForDashboardScoreHist = ConfigGetValue("WebapiBaseUrlForDashboardScoreHist");
            List<DashboardHistScoreBO> listBO = new List<DashboardHistScoreBO>();
            string apiResponse = string.Empty;
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(apiBaseUrlForDashboardScoreHist + remittanceId))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        listBO = JsonConvert.DeserializeObject<List<DashboardHistScoreBO>>(result);
                    }
                }
            }
            return listBO;
        }

        /// <summary>
        /// Submit each paylocation to UPM
        /// </summary>
        /// <param name="paylocID"></param>
        /// <returns></returns>
        public async Task<IActionResult> SubmitReturn(ReturnSubmitBO rBO)
        {
            string apiBaseUrlForInsertEventDetails = ConfigGetValue("WebapiBaseUrlForInsertEventDetails");
            EventDetailsBO eBO = new EventDetailsBO();
            int remID = 0;

            if (rBO.p_REMITTANCE_ID == null)
            {
                // remID = Convert.ToInt32(GetContextValue(SessionKeyRemittanceID));
                return RedirectToAction("Logout", "Home");
            }
            rBO.p_REMITTANCE_ID = CustomDataProtection.Encrypt(rBO.p_REMITTANCE_ID);
            //rBO.P_PAYLOC_FILE_ID = paylocID;
            rBO.P_USERID = ContextGetValue(Constants.SessionKeyUserID);
            //rBO.p_REMITTANCE_ID = remID;
            TempData["msg1"] = "File uploaded to WYPF database successfully.";
            //eBO.remittanceID = remID;
            //eBO.P_PAYLOC_FILE_ID = Convert.ToInt32(rBO.P_PAYLOC_FILE_ID);
            //eBO.remittanceStatus = 1;
            //eBO.eventTypeID = 330;
            //eBO.eventDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
            //eBO.notes = "All errors and warnings cleared by Staff, PayLoc is ready for loading into UPM2";
            ////eventUpdate.UpdateEventDetailsTable(eBO);
            ////update Event Details table File is uploaded successfully.
            ////I have disabled it for staff.
            //callApi.InsertEventDetails(eBO, apiBaseUrlForInsertEventDetails);

            string WebapiBaseUrlForSubmitReturn = ConfigGetValue("WebapiBaseUrlForSubmitReturn");

            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(rBO), Encoding.UTF8, "application/json");
                // string endPoint = apiLink;

                using (var response = await httpClient.PostAsync(WebapiBaseUrlForSubmitReturn, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        rBO = JsonConvert.DeserializeObject<ReturnSubmitBO>(result);
                    }
                }
            }
            TempData["submitReturnMsg"] = rBO.RETURN_STATUSTEXT;

            return RedirectToAction("MasterDetailEmp", "Admin");
        }
        /// <summary>
        /// Delete a remittance including all processes and data at Employer level.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteRemittance(string id)
        {
            int remID =Convert.ToInt32(CustomDataProtection.Encrypt(id));
            string result = string.Empty;
            string apiCallDeleteRemittance = ConfigGetValue("WebapiBaseUrlForDeleteRemittance");
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(apiCallDeleteRemittance + remID))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<string>(result);
                    }
                }
            }
            TempData["msgDelete"] = result;
            return RedirectToAction("MasterDetailEmp");
        }

    }
}
