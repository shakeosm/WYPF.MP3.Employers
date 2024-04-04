using MCPhase3.CodeRepository;
using MCPhase3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MCPhase3.Controllers
{
    /// <summary>
    /// This will be working as an API and used/called by the Admin Portal- to read necessary info from the Employers Portal. For example- Customer Uploaded file count.
    /// </summary>
    [AllowAnonymous]
    public class AdminStaffTools : Controller
    {
        private readonly IFileCountService _fileCountService;

        public AdminStaffTools(IFileCountService FileCountService)
        {
            _fileCountService = FileCountService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetFileCount()
        {
            string host = HttpContext.Request.Host.ToString();
            //## 'mp.wypf.org' , 'testmp.wypf.org', 'http://172.22.80.125:92/' => Files in Done folder
            var allFileList = _fileCountService.Get_FileList_DMZ(host);

            return Ok(allFileList);
        }
        
        [HttpGet]
        public IActionResult GetActiveUserList()
        {
            //## External Live: User logged in last 10 hours..
            var userList_DMZ = _fileCountService.GetUserList_DMZ();

            return Ok(userList_DMZ);
        }


        [HttpGet]
        public IActionResult ClearOlderCustomerFilesNotProcessed(string id)
        {
            string result = _fileCountService.ClearOlderCustomerFilesNotProcessed(id);

            return Ok(result);
        }


    }
}
