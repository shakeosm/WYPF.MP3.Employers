using System;

namespace MCPhase3.Models
{
    public class RemittanceSelectParamBO
    {
        public string UserId { get; set; }
        public string StatusType { get; set; }
        public string StatusCode { get; set; }
        public string YearFrom { get; set; }
        public string YearTo { get; set; }        
        public string PayrollProvider { get; set; }
        
        /// <summary>This is to set the type of User, ie: EMP or WYPF</summary>
        public string UserType { get; set; }

    }
}
