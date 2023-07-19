using MCPhase3.Models;
using MCPhase3.ViewModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MCPhase3.Services
{
    public class ApiService : IApiService
    {
        private readonly IConfiguration _configuration;

        public ApiService(IConfiguration Configuration)
        {
            _configuration = Configuration;
        }
        /// <summary>This will update Score for a Remittance</summary>
        /// <param name="rBO">ReturnSubmitBO Model</param>
        /// <returns>ApiCallResult View Model</returns>
        public async Task<ApiCallResultVM> UpdateScore(ReturnSubmitBO rBO)
        {
            string WebapiBaseUrlForSubmitReturn = _configuration["ApiEndpoints:WebApiBaseUrl"] + _configuration["ApiEndpoints:SubmitReturn"];
            var apiResult = new ApiCallResultVM() { IsSuccess = false };

            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(rBO), Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync(WebapiBaseUrlForSubmitReturn, content);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string contents = await response.Content.ReadAsStringAsync();
                    rBO = JsonConvert.DeserializeObject<ReturnSubmitBO>(contents);

                    apiResult.IsSuccess = true;
                    apiResult.Message = rBO.RETURN_STATUSTEXT;
                }
                else
                {
                    apiResult.IsSuccess = false;
                }
            }

            return apiResult;
        }
    }
}
