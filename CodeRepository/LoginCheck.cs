
using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
    public class LoginCheck : ViewComponent
    {
        private readonly IRedisCache _cache;

        public LoginCheck(IRedisCache cache)
        {
            this._cache = cache;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            //var dLogin = new LoginViewModel();
            var userDetails = new UserDetailsVM();

            if (HttpContext.Session.GetString(Constants.LoginNameKey) is not null)
            {
                userDetails.LoginName = HttpContext.Session.GetString(Constants.LoginNameKey);

                string cacheKey = $"{userDetails.LoginName.ToUpper()}_{Constants.AppUserDetails}";
                userDetails = _cache.Get<UserDetailsVM>(cacheKey);

                //dLogin.UserId = HttpContext.Session.GetString(Constants.LoginNameKey);
                //dLogin.EmployerName = HttpContext.Session.GetString(Constants.SessionKeyPayLocName);

                

            }
            else
            {
                TempData["Msg"] = "Session Expired Please login again.";
                //return RedirectToAction("Index", "Login");
            }

            //return await Task.FromResult((IViewComponentResult)View("LoginCheck", dLogin));
            return await Task.FromResult((IViewComponentResult)View("LoginCheck", userDetails));
        }
    }
}

