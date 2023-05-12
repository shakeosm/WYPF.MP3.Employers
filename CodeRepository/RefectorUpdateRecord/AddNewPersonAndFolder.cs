using MCPhase3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository.RefectorUpdateRecord
{
    public class AddNewPersonAndFolder : IUpdateRecord
    {

       private UpdateRecordModel model = new UpdateRecordModel();
            
        public UpdateRecordModel UpdateRecord()
        {
            model.folderMatch = "95";
            model.personMatch = "95";

            return model;
        }
    }
}
