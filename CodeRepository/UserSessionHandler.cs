using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace MCPhase3.CodeRepository
{
    public class UserSessionHandler
    {
        private readonly RequestDelegate _next;
        public readonly string _relm;

        public UserSessionHandler(RequestDelegate next, string relm)
        {
            _next = next;
            _relm = relm;            
        }

        public async Task InvokeAsync(HttpContext context)
        {
            bool passwordChanged = false;
            var currentUser = context.User.FindFirst(ClaimTypes.NameIdentifier); ;            
            if (currentUser == null)
            {
                //## means nobody logged in yet.. don't do any check.. let it continue
            }
            else {
                Console.WriteLine($"User: {currentUser.Value}");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Uanauthorised");
                return;
            }



            if (passwordChanged){
                if(context.Session.GetString("HasPasswordChanged").Equals("true")) { 
                    /// nothing.. will think later..

                }
            }

            await _next(context);
        }

        
    }
}
