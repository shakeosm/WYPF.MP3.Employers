using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MCPhase3.CodeRepository;
using System.Data;

namespace MCPhase3.Models
{
    /// <summary>
    /// following class is used for three api calls
    /// </summary>
    public class TotalRecordsInsertedAPICall
    {
        /// <summary>
        /// this api will get all the number of records that are inserted into Monthlypost database by using 
        /// bulk data insert 
        /// </summary>
        /// <param name="remId"></param>
        /// <returns></returns>
        public async Task<string> GetTotalRecordsCount(int remId, string url)
        {

            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri("http://172.22.80.130:89/api/TotalRecordsInserted/");
                client.BaseAddress = new Uri(url);
                //send parameters as input
                var responseTask = await client.GetAsync(remId.ToString());
                //responseTask.Wait();
                //var result = responseTask.Result;
                var readTask = await responseTask.Content.ReadAsStringAsync();
                //readTask.Wait();

                //string messageResult = readTask.Result;

                return readTask;
            }
        }
        /// <summary>
        /// AutoMatch api call
        /// </summary>
        /// <param name="remID">Remittance id</param>
        /// <param name="url">url of api</param>
        /// <returns></returns>
        public async Task<AutoMatchBO> GetAutoMatchResult(int remID, string url)
        {
            AutoMatchBO autoMatchBO = new AutoMatchBO();
                //using (var client = new HttpClient())
                //{
                //    client.BaseAddress = new Uri(url);
                //    //send parameters as input
                //    var responseTask = client.GetAsync(remID.ToString());
                //    responseTask.Wait();
                //    var result = responseTask.Result;
                //    var readTask = result.Content.ReadAsStringAsync().Result;
                // List<AutoMatchBO> myResult = new List<AutoMatchBO>();
                // myResult = JsonConvert.DeserializeObject<List<AutoMatchBO>>(readTask);
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(url + remID))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            autoMatchBO = JsonConvert.DeserializeObject<AutoMatchBO>(result);
                        }
                        else
                        {
                            throw new Exception($"Failed to call the GetAutoMatchResult(). Check the API is active, api: {url}/{remID}");
                        }
                }
                }
                return autoMatchBO;
        }
        /// <summary>
        /// Check file is already available in our db for the selected month and year.
        /// </summary>
        
        /// <returns></returns>
        public async Task<int> CheckFileAvailable(CheckFileUploadedBO eBO, string apiLink)
        {
            int result = 0; 
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(eBO), Encoding.UTF8, "application/json");
                string endPoint = apiLink;

                using (var response = await client.PostAsync(apiLink, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var result1 = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<int>(result1);

                    }
                    else
                    {
                        throw new Exception($"Failed to call the CheckFileAvailable(). Check the API is active, api: {apiLink}");
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// This api call will get remittance id of current file by providing file name
        /// and remittance id will be passed to 1st api call.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetRemittanceIdBy_FileName(string fileName, string url)
        {
            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri("http://172.22.80.130:89/api/GetRemittanceId/");
                client.BaseAddress = new Uri(url);
                //send parameters as input
                var responseTask = client.GetAsync(fileName);
                responseTask.Wait();
                var result = responseTask.Result;
                var readTask = result.Content.ReadAsStringAsync();
                readTask.Wait();

                string messageResult = readTask.Result;

                return messageResult;
            }
        }

        public string CallAPIGetEmployerName(int remID, string url)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                //send parameters as input
                var responseTask = client.GetAsync(remID.ToString());
                responseTask.Wait();
                var result = responseTask.Result;
                var readTask = result.Content.ReadAsStringAsync().Result;

                return readTask;
            }
        }

        /// <summary>
        /// This api is show summary
        /// </summary>
        /// <param name="remID"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<List<ErrorAndWarningViewModelWithRecords>> GetErrorAndWarningSummary(AlertSumBO alertSumBO, string url)
        {
            var model = new List<ErrorAndWarningViewModelWithRecords>();

            using (var client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(alertSumBO), Encoding.UTF8, "application/json");
                string endpoint = url;
                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        //  client.BaseAddress = new Uri(url);
                        //send parameters as input
                        //  var responseTask = client.GetAsync(remID.ToString());
                        // responseTask.Wait();
                        // var result = responseTask.Result;
                        var readTask = await Response.Content.ReadAsStringAsync();
                        model = JsonConvert.DeserializeObject<List<ErrorAndWarningViewModelWithRecords>>(readTask);
                    }
                    else
                    {
                        throw new Exception($"Failed to call the GetErrorAndWarningSummary(). Check the API is active, api: {url}");
                    }
                }
            }
            return model;
        }

        /// <summary>
        /// This api is show specific or All errors and warnings with person record
        /// </summary>
        /// <param name="remID"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<MemberUpdateRecordBO> UpdateRecordGetValueCall(int dataRow, string url)
        {
           HttpClient client = new System.Net.Http.HttpClient();

            MemberUpdateRecordBO memberUpdateRecordBO = null;
            
            //HttpResponseMessage response = await client.GetAsync(url);
            //if (response.IsSuccessStatusCode)
            //{
            //    memberUpdateRecordBO = await response.Content.reReadAsAsync<MemberUpdateRecordBO>();
            //}
            return memberUpdateRecordBO;
        }
        /// <summary>
        /// to get Event Details of a remittance id.
        /// </summary>
        /// <param name="remID"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<EventDetailsBO> GetEventDetails(int remID, string url)
        {
            EventDetailsBO eBO = new EventDetailsBO();

            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(url + remID))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        eBO = JsonConvert.DeserializeObject<EventDetailsBO>(result);
                    }
                    else
                    {
                        throw new Exception($"Failed to call the GetEventDetails(). Check the API is active, api: {url}/{remID}");
                    }
                }
            }
            return eBO;
        }

        /// <summary>
        /// to insert Event Details of a remittance id.
        /// </summary>
        /// <param name="remID"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async void InsertEventDetails(EventDetailsBO eBO, string apiLink)
        {
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(eBO), Encoding.UTF8, "application/json");
                string endPoint = apiLink;

                using (var response = await client.PostAsync(apiLink, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                    }
                    else
                    {
                        throw new Exception($"Failed to InsertEventDetails(). Check the API is active, api: {apiLink}");
                    }
                }
            }
            //return eBO;
        }
        public async Task<bool> InsertDataApi(DataTable dataTable, string apiLink)
        {
            bool result = false;
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(dataTable), Encoding.UTF8, "application/json");
                string endPoint = apiLink;

                using (var response = await client.PostAsync(apiLink, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result = true;
                    }
                    else
                    {
                        throw new Exception($"Failed to call the InsertDataApi(). Check the API is active, api: {apiLink}");
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// Check file previous uploaded file is completed or WYPF still processing on file. If file is still in process then 
        /// stop new uploaded file to auto match.
        /// </summary>

        /// <returns></returns>
        public async Task<InitialiseProcBO> InitialiseProce(InitialiseProcBO eBO, string apiLink)
        {
           
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(eBO), Encoding.UTF8, "application/json");
                string endPoint = apiLink;

                using (var response = await client.PostAsync(apiLink, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var result1 = await response.Content.ReadAsStringAsync();
                        eBO = JsonConvert.DeserializeObject<InitialiseProcBO>(result1);

                    }
                    else {
                        throw new Exception($"Failed to initialise the Remittance journey-> InitialiseProce(). Check the API is active, api: {apiLink}");
                    }
                }
            }
            return eBO;
        }

        public async Task<ReturnCheckBO> ReturnCheckAPICall(ReturnCheckBO eBO, string apiLink) 
        {
            ReturnCheckBO result = new ReturnCheckBO();
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(eBO), Encoding.UTF8, "application/json");
                string endPoint = apiLink;

                using (var response = await client.PostAsync(apiLink, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var result1 = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<ReturnCheckBO>(result1);
                    }
                    else
                    {
                        throw new Exception($"Failed to call the ReturnCheckAPICall(). Check the API is active, api: {apiLink}");
                    }
                }
            }
            return result;
        }


        public async Task<int> counterAPI(string url, RangeOfRowsModel rangeOfRowsModel)
        {
            int messageResult = 0;
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(rangeOfRowsModel), Encoding.UTF8, "application/json");
                string endPoint = url;
                
                using (var response = await client.PostAsync(url, content))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        messageResult = JsonConvert.DeserializeObject<int>(result);
                    }
                    else
                    {
                        throw new Exception($"Failed to call counterAPI(). Check the API is active, api: {url}");
                    }
                }
            }           
            return messageResult;
            
        }
    }
}
