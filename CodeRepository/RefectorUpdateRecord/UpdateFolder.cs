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
