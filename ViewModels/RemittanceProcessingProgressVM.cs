using Newtonsoft.Json;
using System;

namespace MCPhase3.ViewModels
{
    public class RemittanceProcessingProgressVM
    {
        [JsonIgnore]
        public string Name { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public double Members_Matched { get; set; }
        public double Folders_Matched { get; set; }
//        public double ProcessedPercent() => ProcessedRecords / TotalRecords * 100;
        public string Status { get; set; } = "Processing";
        [JsonIgnore]
        public string LastUpdated { get; set; } = DateTime.Now.ToString("HH:mm:ss");
        public string Message { get; set; }

        /// <summary>This will return TRUE when Processed Records are equal to Total Records or 'Status' is 'COMPELTE'</summary>
        /// <returns></returns>
        public bool IsCompleted() => TotalRecords == ProcessedRecords || Status.ToLower().Equals("complete");

    }
}