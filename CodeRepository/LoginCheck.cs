
using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
    public class LoginCheck : ViewComponent
    {
        public const string SessionKeyUserID = "_UserName";
        public const string SessionKeyPayLocName = "_PayLocName";
        LoginViewModel dLogin = new LoginViewModel();

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (HttpContext.Session.GetString(SessionKeyUserID) != null)
            {
                dLogin.UserName = HttpContext.Session.GetString(SessionKeyUserID);
                dLogin.EmployerName = HttpContext.Session.GetString(SessionKeyPayLocName);
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

