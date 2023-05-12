using MCPhase3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository.RefectorUpdateRecord
{
    public class UpdateFolder : IUpdateRecord
    {
        private UpdateRecordModel model = new UpdateRecordModel();

        public UpdateRecordModel UpdateRecord()
        {
            model.folderMatch = "90";
            model.personMatch = "90";

            return model;
        }
    }
}
