using System.Data;

namespace MCPhase3.Models
{
    /// <summary>
    /// All the common properties that I use inside MC3 are 
    /// held here in following class.
    /// ## Update: Currently used only in Fire template...; Shawkat; 12/06/2023
    /// </summary>
    public class MyModel
    {
        public string myErrorMessageText { get; set; }
        public DataTable stringDataTable;

        public string monthsList { get; set; }
        public string yearsList { get; set; }
        public string paylocationList { get; set; }
        public string remittanceId { get; set; }
        public bool isFire { get; set; }
        public string posting { get; set; }
    }
}
