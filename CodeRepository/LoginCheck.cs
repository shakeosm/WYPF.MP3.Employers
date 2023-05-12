﻿
using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
    public class LoginCheck : ViewComponent
    {
        public const string SessionKeyUserID = "_UserName";       
        public const string SessionKeyPayLocName = "_PayLocName";
        DummyLoginViewModel dLogin = new DummyLoginViewModel();

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (HttpContext.Session.GetString(SessionKeyUserID) != null)
            {
                dLogin.userName = HttpContext.Session.GetString(SessionKeyUserID);
                dLogin.employerName = HttpContext.Session.GetString(SessionKeyPayLocName);
            }
            else
            {
                TempData["Msg"] = "Session Expired Please login again.";
                //return RedirectToAction("Index", "Login");
            }

            return await Task.FromResult((IViewComponentResult)View("LoginCheck", dLogin));
        }
    }
}

