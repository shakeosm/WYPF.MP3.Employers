using Spire.Xls;
using System.Text;

namespace MCPhase3.Common
{
    public static class ConvertExcelToCsv
    {
        public static bool Convert(string filePathName, string csvFilePathName)
        {
            try
            {
                Workbook workbook = new Workbook();
                workbook.LoadFromFile(filePathName);
                Worksheet sheet = workbook.Worksheets[0];
                sheet.SaveToFile(csvFilePathName, ",", Encoding.UTF8);

                return true;
            }
            catch (System.Exception ex)
            {
                return false;
            }

        }

    }
}
