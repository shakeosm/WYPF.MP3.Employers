namespace MCPhase3.Models
{
    public interface IAPICalls
    {
        public string CallAPI(string fileName, string url);
        public string CallAPI(int remId);
        public string CallAPI(int remID, string url);
        public string CallAPISummary(int remID, string url);
        public string CallAPIGetEmployerName(int remID, string url);

        //public string CallAPIToShowWithPersonRecord(ErrorAndWarningToShowListViewModel errorAndWarningTo, string url);

    }
}
