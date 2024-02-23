using System;
using System.Collections.Generic;

namespace MCPhase3.ViewModels
{

    public class AdminControlPanelVM
    {
        public List<FileDetails> UserActivityList { get; set; }
        public List<FileDetails> FilesNotProcessedList { get; set; }
        public List<FileDetails> FilesInDoneList { get; set; }

        

    }

    public class FileDetails
    {
        public string FileName { get; set; }
        public DateTime CreatedOn { get; set; }
    }

}
