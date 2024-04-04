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
            string currentUserId = filterContext.HttpContext.Session.GetString(Constants.UserIdKey);
            string urlPath = filterContext.HttpContext.Request.Path.ToString().ToLower();


            if (urlPath.Contains("clearredisusersession") || urlPath.Contains("logout") || urlPath.Equals("/") || urlPath.Contains("login") || urlPath.Contains("adminstafftools"))
            {
                //## do no check....//## VIP pass for these paths 
            }            
            else if (filterContext.HttpContext.Session.GetString(Constants.UserIdKey) is null)
            {
                //## session expired
                filterContext.Result = RedirectResult("Login", "SessionExpired");

            }
            else if (!string.IsNullOrEmpty(currentUserId))
            {
                //## Get the session info from Redis cache.
                //## This current-authenticated Redis session may have been deleted by the Admin- after changing the Password..
                //## So check- whether we still have a Redis session? If not- we need to Login again
                string sessionGuid = filterContext.HttpContext.Session.GetString(Constants.SessionGuidKeyName);
                string sessionInfoKeyName = $"{currentUserId}_{Constants.SessionInfoKeyName}";     //## this must match the Keyname in the BaseController.. Don't change it here
                var redisCache = _cache.Get<UserSessionInfoVM>(sessionInfoKeyName);

                if (redisCache is null)
                {
                    //## can happen- if the RedisCache is restarted/cleared on the Server- then what to do!
                    //## or the Admin has updated password and deleted the current user Redis session
                    filterContext.Result = RedirectResult("Login", "Logout" );

                }
                else if (sessionGuid != null && sessionGuid == redisCache.SessionId)
                {
                    //## all good
                }
                else if (sessionGuid != redisCache.SessionId)
                {
                    //## From a different browser - the user has logged out my current session here, therefore 'sessionInfo.SessionId' is not anymore same as my 'Redis.SessionId' in current Browser.SessionId 
                    //## call a separate ActionController-> to clear up the Http session.. don't use Logout... that will Delete Redis cache..
                    filterContext.Result = RedirectResult("Login", "ClearSessionAndLogin");
                }
            }
        }

        private RedirectToRouteResult RedirectResult(string controllerName, string actionName)
        {
            return new RedirectToRouteResult(
                            new RouteValueDictionary(
                                new { controller = controllerName, action = actionName }));
        }

    }
}
