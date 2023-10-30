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
