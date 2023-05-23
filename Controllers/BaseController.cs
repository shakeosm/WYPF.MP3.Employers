using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using MCPhase3.CodeRepository;
using MCPhase3.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection.Metadata;
using System.Web;

namespace MCPhase3.Controllers
{
    public class BaseController : Controller
    {
        private readonly IConfiguration _configuration;

        public BaseController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ContextGetValue(string keyName)
        {
            return HttpContext.Session.GetString($"{keyName}");
        } 

        public void ContextSetValue(string keyName, string value)
        {
            HttpContext.Session.SetString($"{keyName}", value);
        }

        public string CurrentUserId() => HttpContext.Session.GetString(Constants.UserIdKeyName);


        public string ConfigGetValue(string keyName) => _configuration.GetValue<string>(keyName);

        /// <summary>Only to be used to Decrypt a Query string value, which usually comes in Encoded format</summary>
        public string DecryptUrlValue(string value)
        {
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentException("Parameter cannot be null", "Url parameter");
            }
            //var result = HttpUtility.UrlDecode(value);
            return CustomDataProtection.Decrypt(value);
        }

        /// <summary>Only to be used to Encrypt a value to prepare to use in a Query string, which usually requires to be in Encoded format</summary>
        public string EncryptUrlValue(string value)
        {
            if (string.IsNullOrEmpty(value)){
                throw new ArgumentException("Parameter cannot be null", "Url parameter");
            }
            return CustomDataProtection.Encrypt(value);
            //return HttpUtility.UrlEncode(value);
        }

    }
}
