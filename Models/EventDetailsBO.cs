using System;

namespace MCPhase3.Models
{
    public class EventDetailsBO
    {
        public int eventTypeID { get; set; }
        public int remittanceID { get; set; }
        public int remittanceStatus { get; set; }
        public DateTime eventDate { get; set; }
        public string notes { get; set; }
    }
}
