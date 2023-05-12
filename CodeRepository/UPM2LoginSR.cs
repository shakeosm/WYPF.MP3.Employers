using System;

namespace MCPhase3.CodeRepository
{
    public class UPM2LoginSR
    {
        public bool Pass;
        public string Reason;

        private string[] logRefText = new string[1];
        private string securityToken;
        private string sessionIdentifier;

       // public bool Login(string userID, string password)
       // {
            //bool result = false;
            //try
            //{

            //    Pass = true;
            //    UPM2Login.SecurityBinding secBinding = new UPM2Login.SecurityBinding();

            //    // A unique log ref needs creating for tracing errors here - 
            //    // Any error logging on the w2 server will include this reference

            //    UPM2Login.LogRefType logRef = new UPM2Login.LogRefType();
            //    string[] text = new string[1];
            //    text[0] = "123456789-A";

            //    logRefText = text;
            //    logRef.Text = text;

            //    secBinding.LogRef = logRef;

            //    UPM2Login.LoginSecurityStructure logSecurity = secBinding.getSecurityKeys(string.Empty);


            //    // Populate the keys and send them back

            //    UPM2Login.SecuritySubmissionBinding submitBinding = new UPM2Login.SecuritySubmissionBinding();

            //    // Add the security token

            //    submitBinding.Subject = new UPM2Login.SubjectType();

            //    submitBinding.Subject.SecurityToken = logSecurity.Subject.SecurityToken;

            //    submitBinding.Subject.SessionIdentifier = logSecurity.Subject.SessionIdentifier;

            //    submitBinding.LogRef = new UPM2Login.LogRefType()

            //    {

            //        Text = new string[1] { "123456789-A" }

            //    };



            //    // Create the keys

            //    UPM2Login.CominoLoginSecurityKeys keys = new UPM2Login.CominoLoginSecurityKeys();

            //    keys.LoginName = userID;

            //    keys.SessionKOSecurityKey = logSecurity.CominoLoginSecurityKeys.SessionKOSecurityKey;

            //    keys.SessionKOSecurityKey[0].SessionSecurityKey[0].SecurityKey.SecurityKeyElement[0].Answer = password;

            //    submitBinding.submitSecurityKeys(ref keys);

            //    // Now you need to inspect keys and check that the have beenauthorised.  
            //    // Also you should get a contact number back if you have been successful
            //    // If it failed you will get a contactno of 2 back (anomynous) and
            //    // The keys structure will tell you which keys failed.

            //    String myContactNo = keys.ContactNo;

            //    if (myContactNo != null && !myContactNo.Equals("2"))
            //    {
            //        // You have been successful 
            //        this.Pass = true;
            //        result = this.Pass;
            //    }

            //    else
            //    {
            //        Pass = false;
            //        if (myContactNo.Equals("2"))
            //        {
            //            Reason = "Annonymous user";
            //        }
            //        if (myContactNo == null)
            //        {
            //            Reason = "No contact number";
            //        }
            //    }


            //}
            //// if an exception is thrown
            //catch (Exception theEx)
            //{
            //    // set pass to false
            //    Pass = false;
            //    // set the exception message as the reason for the failure
            //    Reason = theEx.Message;
            //}

            //return result;
       // }
    }
}

