using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MCPhase3.Controllers
{
    public class ErrorHandlerMiddlewareController : Controller
    {
        // If there is 404 status code, the route path will become Error/404
        [Route("ErrorHandlerMiddleware/HttpStatusCodeHandler/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            //var currentUser = HttpContext.Session.GetString(Constants.LoggedInAsKeyName);
            //using StreamWriter sw = System.IO.File.AppendText("C:\\MP3\\CustomerUploads\\ErrorHandlerMiddlewareController.txt");
            //sw.WriteLine($"{DateTime.Now.ToLongTimeString()} {currentUser} > ErrorHandlerMiddlewareController({statusCode})");
            //sw.Flush();
            //sw.Close();

            switch (statusCode)
            {
                case 400:
                    ViewBag.ErrorMessage = "The server can't process the request due to clientside errors";
                    ViewBag.RouteOfException = statusCode;
                    break;
                case 401:
                    ViewBag.ErrorMessage = "Lacks of valid authentication credentials for the target resource";
                    ViewBag.RouteOfException = statusCode;
                    break;
                case 403:
                    ViewBag.ErrorMessage = "You do not have permission to view this page. Please contact WYPF";
                    ViewBag.RouteOfException = statusCode;
                    break;
                case 404:
                    ViewBag.ErrorMessage = "Sorry the page you requested could not be found";
                    ViewBag.RouteOfException = statusCode;
                    break;

                case 408:
                    ViewBag.ErrorMessage = "Request Timeout. The server took longer than it's allocated timeout window.";
                    ViewBag.RouteOfException = statusCode;
                    break;
                case 500:
                    ViewBag.ErrorMessage = "Sorry something went wrong on the server";
                    ViewBag.RouteOfException = statusCode;
                    break;
            }

            ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            ViewBag.ShowRequestId = !string.IsNullOrEmpty(ViewBag.RequestId);
            ViewBag.ErrorStatusCode = statusCode;

            return View();
        }
    }
}