
using MCPhase3.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
    public class EventsTableUpdates
    {
        TotalRecordsInsertedAPICall callApi = new TotalRecordsInsertedAPICall();
        EventDetailsBO eBO = new EventDetailsBO();
       
        public void UpdateEventDetailsTable(EventDetailsBO eBO)
        {
            //update Event Details table File is uploaded successfully.
           // string apiBaseUrlForInsertEventDetails = _Configure.GetValue<string>("WebapiBaseUrlForInsertEventDetails"); 
           // callApi.InsertEventDetails(eBO, apiBaseUrlForInsertEventDetails);
        }
    }
}
