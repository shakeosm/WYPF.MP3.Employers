using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace MCPhase3.CodeRepository.ActionFilters
{
    public class UserSessionCheckActionFilter : ActionFilterAttribute
    {
        private readonly IRedisCache _cache;

        public UserSessionCheckActionFilter(IRedisCache cache)
        {
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //Log("OnActionExecuted", filterContext.RouteData);
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {            
            string currentUserId = filterContext.HttpContext.Session.GetString(Constants.UserIdKeyName);
            string x = filterContext.HttpContext.Session.GetString(Constants.SessionKeyUserID);
            string urlPath = filterContext.HttpContext.Request.Path.ToString().ToLower();


            //## VIP pass for this call [ClearRedisUserSession]- only the AdminPortal knows .. 
            if (urlPath.Contains("clearredisusersession") || urlPath.Contains("logout"))
            {
                //## do no check....
            }
            else if (!string.IsNullOrEmpty(currentUserId))
            {
                //## Get the session info from Redis cache
                string sessionGuid = filterContext.HttpContext.Session.GetString(Constants.SessionGuidKeyName);
                string sessionInfoKeyName = $"{currentUserId}_{Constants.SessionInfoKeyName}"; //## this must match the Keyname in the BaseController.. Don't change it here
                var sessionInfo = _cache.Get<UserSessionInfoVM>(sessionInfoKeyName);

                if (sessionInfo == null)
                {
                    //## can happen- if the RedisCache is restarted/cleared on the Server- then what to do!
                    //filterContext.HttpContext.Response.Redirect("/Login/Logout");
                    
                    filterContext.Result = new RedirectToRouteResult(
                                                new RouteValueDictionary(
                                                    new { controller = "Login", action = "Logout" }));

                }
                else if (sessionGuid != null && sessionGuid == sessionInfo.SessionId)
                {
                    //## all good
                }
                else
                {
                    filterContext.Result = new RedirectToRouteResult(
                                                new RouteValueDictionary(
                                                    new { controller = "Login", action = "Logout" }));
                }
            }
        }



    }
}
