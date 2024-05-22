using MCPhase3.Common;

namespace MCPhase3.Models
{
    public class PasswordUpdateVM
    {

        public string UserName { get; internal set; }
        public string Password { get; set; }
        public string OldPassword { get; set; }        
        internal int Result;
        public string PortalName  { get; internal set; } = Constants.ThisPortalName;
    }

}
