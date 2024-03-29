﻿
using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
    public class LoginCheck : ViewComponent
    {        
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var dLogin = new LoginViewModel();

            if (HttpContext.Session.GetString(Constants.LoginNameKey) != null)
            {
                dLogin.UserId = HttpContext.Session.GetString(Constants.LoginNameKey);
                dLogin.EmployerName = HttpContext.Session.GetString(Constants.SessionKeyPayLocName);
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

