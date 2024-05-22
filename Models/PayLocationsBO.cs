using System;

namespace MCPhase3.Models
{
    public class PayLocationsBO
    {
        public string pay_location_name { get; set; }
        public string pay_location_ID { get; set; }
        public string contact_name { get; set; }
        public string paylocation_ref { get; set; }
        public string client_id { get; set; }

        //public string to_char(trim(paylocation_id)) { Get; Set; }

    }

    public class PayLocationWithFinBusPartnerVM
    {
        public string PayLocationId { get; set; }
        public string PayLocationRef { get; set; }
        public string FBP_UserId { get; set; }
        public string PayLocationName { get; set; }
        public DateTime? DateClosed { get; set; }
    }
}
