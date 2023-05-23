using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
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
            string currentUserId = context.Session.GetString(Constants.UserIdKeyName);
            string x = context.Session.GetString(Constants.SessionKeyUserID);
            

            if (!string.IsNullOrEmpty(currentUserId)) {
                //## Get the session info from Redis cache
                string sessionGuid = context.Session.GetString(Constants.SessionGuidKeyName);
                string sessionInfoKeyName = $"{Constants.SessionInfoKeyName}-{currentUserId}"; 
                var sessionInfo = _cache.Get<UserSessionInfoVM>(sessionInfoKeyName);

                if (sessionGuid != null && sessionGuid == sessionInfo.SessionId)
                {
                    //## all good
                    Console.WriteLine("sessionGuid != null && sessionGuid == sessionInfo.SessionId");
                }
                else {
                    Console.WriteLine("### Someone has deleted the session information.. means this session isn't valid anymore... password changed     ####");
                    context.Response.Redirect("/Login/Logout");
                }            
            }

            await _next(context);
        }

        
    }
}
