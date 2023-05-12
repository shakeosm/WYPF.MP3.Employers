using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.Models
    {
    /// <summary>
    /// All the common properties that I use inside MC3 are 
    /// held here in following class.
    /// 
    /// </summary>
    public class MyModel
    {
        public string myErrorMessageText { get; set; }
        public DataTable stringDT;

        public string monthsList { get; set; }
        public string yearsList { get; set; }
        public string paylocationList { get; set; }
        public string remittanceId { get; set; }
        public bool isFire { get; set; }
        public string posting { get; set; }
    }
}
