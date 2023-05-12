using MCPhase3.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.Controllers
{
    public class SubmitToDBController : Controller
    {
        [HttpGet]
        public IActionResult Index(string remittanceId)
        {
        //    TotalRecordsInsertedAPICall callApi = new TotalRecordsInsertedAPICall();
        //    string totalRecords = callApi.CallAPI(remittanceId);
        //    string num = totalRecords.Substring(totalRecords.IndexOf(":") + 1);
        //   // string num = totalRecords.Substring(totalRecords.IndexOf(":") + 1);
        //    num = num.Trim(new char[] { '"', '}', ']' });
        //    TempData["msg"] = "Total number of records inserted successfully into database are: " + num;
           return View();
        }
    }
}
