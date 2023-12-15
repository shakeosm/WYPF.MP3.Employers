namespace MC3.API.Models
{
    public class MailDataVM
    {        
        public string EmailToId { get; set; }
        public string EmailToName { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }        
        public string CommunicationMethod { get; set; }        
    }

    public class MailDataVerifyVM
    {
        public string LoginId { get; set; }
        public string SessionToken { get; set; }
               
    }
}
