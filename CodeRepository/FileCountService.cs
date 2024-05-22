using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MCPhase3.CodeRepository
{
    /// <summary>
    /// following class will show totals value on form from uploaded excel file.
    /// </summary>
    public class FileCountService : IFileCountService
    {
        private readonly IConfiguration _configuration;

        public FileCountService(IConfiguration Configuration)
        {
            _configuration = Configuration;
        }

        public string Get_FileList_DMZ(string hostName)
        {
            //string doneList_DMZ = "DMZ (mp.wypf.org);Done;5";
            string uploadFolder = _configuration["FileUploadPath"];
            string FileUploadDonePath = uploadFolder + "Done\\";

            StringBuilder fileList = new StringBuilder();

            //## Done Folder
            if (System.IO.Path.Exists(FileUploadDonePath))
            {
                string[] files = Directory.GetFiles(FileUploadDonePath);
                fileList.Append($"{hostName},Done,{files.Length};");                
            }

            //## Temp Folder
            if (System.IO.Path.Exists(path: uploadFolder))
            {
                string[] files = Directory.GetFiles(uploadFolder);

                fileList.Append($"{hostName},Temp,{files.Length};");

            }

            return fileList.ToString();

        }

        public string GetUserList_DMZ()
        {
            //
            string logDebugInfoFilePath = _configuration["LogDebugInfoFilePath"];

            StringBuilder userList = new StringBuilder();

            //## User Activity Log files
            if (System.IO.Path.Exists(logDebugInfoFilePath))
            {
                string[] files = Directory.GetFiles(logDebugInfoFilePath);
                if (files.Any()) {
                    foreach (string file in files)
                    {
                        FileInfo fi = new FileInfo(file);
                        var lastAccessed = fi.LastWriteTime;

                        if (DateTime.Now < lastAccessed.AddHours(10))
                        {
                            var fileName = file.Replace(logDebugInfoFilePath, "");    //## no need to show the FolderPath in the UI...
                            userList.Append($"{fileName},{lastAccessed};");
                        }

                    }
                
                }
            }           

            return userList.ToString();

        }

        public string ClearOlderCustomerFilesNotProcessed(string id)
        {
            string uploadFolder = _configuration["FileUploadPath"];

            if (System.IO.Path.Exists(uploadFolder))
            {
                string[] files = Directory.GetFiles(uploadFolder);
                StringBuilder cleanupResult = new StringBuilder();
                cleanupResult.AppendLine("############### New hourly execution #############");
                cleanupResult.AppendLine($"############# Time: {DateTime.Now} ##########");

                if (files.Any() == false)
                {
                    cleanupResult.AppendLine("No files found...");
                    cleanupResult.AppendLine();
                    Log_ClearOlderCustomerFilesNotProcessed(cleanupResult.ToString());
                    return $"success, called at: {DateTime.Now}";
                }

                int oldFilesFound = 0; int newFilesFound = 0;
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    var created = fi.LastAccessTime;
                    string hoursElapsed = _configuration["ClearCustomerFilesOlderThan_X_Hours"].ToString();
                    int.TryParse(hoursElapsed, out int hoursThresholdToDeleteFile);

                    //cleanupResult.AppendLine($"{DateTime.Now} >> File: {file}, created: {created}");
                    if (DateTime.Now > created.AddHours(hoursThresholdToDeleteFile))
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                            cleanupResult.AppendLine($"deleting >> {file}, created: {created},");
                        }
                        catch (Exception ex)
                        {
                            cleanupResult.AppendLine($"Error: Failed to Delete >> {file}, created: {created}, Reason: {ex.ToString()}");
                        }

                        oldFilesFound++;
                    }
                    else
                    {
                        cleanupResult.AppendLine($"New File>> {file}, created: {created},");
                        newFilesFound++;
                    }
                }
                cleanupResult.AppendLine($"Total: {files.Length} files found. New file: {newFilesFound}, Old: {oldFilesFound}.");
                cleanupResult.AppendLine();
                Log_ClearOlderCustomerFilesNotProcessed(cleanupResult.ToString());

                return $"success, called at: {DateTime.Now}";
            }
            return "who are you?";
        }


        public void Log_ClearOlderCustomerFilesNotProcessed(string message)
        {
            //var logMessageText = $"{DateTime.Now.ToLongTimeString()}> {message}";
            if (_configuration["Log_ClearOlderCustomerFilesNotProcessed"].ToString().ToLower() == "yes")
            {
                //logMessageText = $"{DateTime.Now.ToLongTimeString()}> {message}";
                try
                {
                    string filepath = _configuration["LogDebugInfoFilePath"].ToString() + "FileCleanup\\";

                    if (!Directory.Exists(filepath))
                    {
                        Directory.CreateDirectory(filepath);
                    }

                    filepath = filepath + DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                    if (!System.IO.File.Exists(filepath))
                    {
                        System.IO.File.Create(filepath).Dispose();
                    }

                    using StreamWriter sw = System.IO.File.AppendText(filepath);
                    sw.WriteLine(message);
                    sw.Flush();
                    sw.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error at: public void Log_ClearOlderCustomerFilesNotProcessed() => " + e.ToString());
                }
            }
        }

    }

    public interface IFileCountService
    {
        public string Get_FileList_DMZ(string hostName);
        public string GetUserList_DMZ();
        public string ClearOlderCustomerFilesNotProcessed(string id);
    }
}
