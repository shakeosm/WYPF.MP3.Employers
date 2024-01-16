using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
    [Obsolete("Do not use this Middleware. A Global ActionFilter is in operation.")]
    public class UserSessionHandlerMiddleWare
    {    
        private readonly RequestDelegate _next;
        public readonly string _relm;
        private readonly IRedisCache _cache;

        public UserSessionHandlerMiddleWare(RequestDelegate next, string relm, IRedisCache cache)
        {
            _next = next;
            _relm = relm;
            _cache = cache;
        }
        

        public async Task InvokeAsync(HttpContext context)
        {            
            string currentUserId = context.Session.GetString(Constants.LoggedInAsKeyName);
            //string x = context.Session.GetString(Constants.LoggedInAsKeyName);

            //using StreamWriter sw = System.IO.File.AppendText("C:\\MP3\\CustomerUploads\\UserSessionHandlerMiddleWare.txt");
            //sw.WriteLine($"{DateTime.Now.ToLongTimeString()} {currentUserId} > UserSessionHandlerMiddleWare()");
            //sw.Flush();
            //sw.Close();

            if (!string.IsNullOrEmpty(currentUserId)) {
                //## Get the session info from Redis cache
                string sessionGuid = context.Session.GetString(Constants.SessionGuidKeyName);
                string sessionInfoKeyName = $"{currentUserId}_{Constants.SessionInfoKeyName}"; //## this must match the Keyname in the BaseController.. Don't change it here
                var sessionInfo = _cache.Get<UserSessionInfoVM>(sessionInfoKeyName);

                if (sessionInfo == null)
                {
                    //## can happen- if the RedisCache is restarted/cleared on the Server- then what to do!
                    context.Response.Redirect("/Login/Logout");
                }

                if (sessionGuid != null && sessionGuid == sessionInfo.SessionId)
                {
                    //## all good
                }
                else {
                    context.Response.Redirect("/Login/Logout");
                }            
            }

            await _next(context);
        }

        
    }
}
