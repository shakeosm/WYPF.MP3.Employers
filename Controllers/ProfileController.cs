using MCPhase3.CodeRepository;
using MCPhase3.Common;
using MCPhase3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MCPhase3.Common.Constants;

namespace MCPhase3.Controllers
{
    public class ProfileController : BaseController
    {
        private readonly IConfiguration _configuration;
        //public readonly IRedisCache _cache;
        //public readonly IDataProtectionProvider provider;
        //public readonly ApiEndpoints _apiEndpoints;
        private readonly IDataProtector _protector;

        public ProfileController(IConfiguration configuration, IRedisCache cache, IDataProtectionProvider Provider, IOptions<ApiEndpoints> ApiEndpoints) : base(configuration, cache, Provider, ApiEndpoints)
        {
            _configuration = configuration;
        }


        public async Task<IActionResult> Index()
        {
            var loginName = ContextGetValue(Constants.LoginNameKey);
            var currentUser = await GetUserDetails(loginName);

            LogInfo("Loading -> /Profile/Index() ");
            return View(currentUser);
        }



        /// <summary>
        /// Update password for Staff and Employers
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> UpdatePassword()
        {
            var loginName = ContextGetValue(Constants.LoginNameKey);

            var apiResult = await ApiGet(GetApiUrl(_apiEndpoints.GeneratePassword));
            var suggestedPasswordList = JsonConvert.DeserializeObject<List<string>>(apiResult);
            LogInfo("suggestedPasswordList: " + suggestedPasswordList);

            var login = new LoginBO()
            {
                UserName = CurrentUserId(),
                SuggestedPasswords = suggestedPasswordList
            };

            LogInfo("Loading UpdatePassword() page..");
            return View(login);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(PasswordUpdateVM passwordParams)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Password", "Invalid password. Please use a strong password.");
                return View(new LoginBO());
            }

            if (!PaswordPolicyMatched(passwordParams.Password))
            {
                ModelState.AddModelError("Password", "Invalid password. Please follow the Password policy.");
                return View(new LoginBO());
            }

            
            //                    < li > at least 12 characters,</ li >
            //                    < li > one or more capital letters,</ li >
            //                    < li > one or more lower case letters,</ li >
            //                    < li > one or more numbers,</ li >
            //                    < li > one or more special characters, for example!,”.#</li>
            //                    < li > should not contain the user name </ li >
            //                    < li > not have 3 or more of the same characters together, e.g.aaa or 111 </ li >
            
            LogInfo("User posting UpdatePassword() page.");

            ViewBag.isStaff = 1;
            passwordParams.UserName = CurrentUserId();

            var result = (Password)await UpdatePasswordMethod(passwordParams);
            if (result == Password.Updated)
            {
                TempData["Msg1"] = "Password updated successfully, please login using the new password.";
                LogInfo("Password updated successfully, please login using the new password.");
                return RedirectToAction("Logout", "Login");
            }
            if (result == Password.Invalid)     //## this will not happen.. as we already have verified against RegEx.Match()
            {
                TempData["UpdateMessage"] = "Password does not meet our complexity requirements: ";
                TempData["PasswordRequ"] = 1;
                return RedirectToAction("UpdatePassword", "Admin");
            }
            else if (result == Password.IncorrectOldPassword)
            {
                TempData["UpdateMessage"] = "Old Password is not correct, please try again.";
                return RedirectToAction("UpdatePassword", "Admin");
            }
            else
            {
                TempData["UpdateMessage"] = "Failed to updated password, please try again.";
                return RedirectToAction("UpdatePassword", "Admin");
            }
        }

       
        /// <summary>This will POST the Password and User name to the API and get the Update result</summary>
        /// <param name="passwordParams">Login parameters with UserId and Password</param>
        /// <returns>Update result in Numeric value</returns>
        private async Task<int> UpdatePasswordMethod(PasswordUpdateVM passwordParams)
        {
            string apiBaseUrlForLoginCheck = GetApiUrl(_apiEndpoints.PasswordChange);   //## api/ChangePassword
            string apiResponse = await ApiPost(apiBaseUrlForLoginCheck, passwordParams);
            passwordParams.Result = JsonConvert.DeserializeObject<int>(apiResponse);

            return passwordParams.Result;

        }

        /// <summary>This will be called via ajax to load more Password suggestions by the user</summary>
        /// <returns>List of suggested passwords</returns>
        [HttpGet, Route("/Profile/LoadSuggestedPassword")]
        public async Task<IActionResult> LoadSuggestedPassword()
        {
            var apiResult = await ApiGet(GetApiUrl(_apiEndpoints.GeneratePassword));
            var suggestedPasswordList = JsonConvert.DeserializeObject<List<string>>(apiResult);

            return PartialView("_SuggestedPasswordList", suggestedPasswordList);
        }


        /// <summary>This will be called via ajax to check a password strength so user will know about its validity</summary>
        /// <returns>A number indicating strength, between 0-100. A score of 70 is a pass mark</returns>
        [HttpGet, Route("/Profile/CheckPasswordStrength/{passwordToCheck}"), AllowAnonymous]
        public async Task<IActionResult> CheckPasswordStrength(string passwordToCheck)
        {            
            string passwordMeterResult = "Weak;0;70";    //## Format: $"{scoreText};{currentScore};{scoreToPass}";
            if (!PaswordPolicyMatched(passwordToCheck))
            {
                
                return PartialView("_PasswordMeter", passwordMeterResult);
            }

            passwordMeterResult = await GetPasswordMeterValue(passwordToCheck);
            Console.WriteLine($"CheckPasswordStrength() -> apiResult: {passwordMeterResult}");

            return PartialView("_PasswordMeter", passwordMeterResult);
        }


    }
}
