using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MCPhase3.CodeRepository.HelperClasses
{
    public class XmlReader
    {
    string configPath = string.Empty;
    DataSet ds = new DataSet();
    ColumnDefinations valuesXML = new ColumnDefinations();
    public List<ColumnDefinations> readXMLFile(string path)
    {
        XmlDocument xmlDocument = new XmlDocument();
        XmlNodeList nodeList;
        DataTable myDT = new DataTable();
            

        List<ColumnDefinations> columnDefinations = new List<ColumnDefinations>();
       // this.configPath = System.Configuration.ConfigurationManager.AppSettings["designPath"];
        try
        {
                
               xmlDocument.Load(path);
                //calc server
                //xmlDocument.Load("C:\\MC3\\DesignXML\\DataIntegration_Design.xml");

                //my pc
                //xmlDocument.Load("D:\\SchedularCodeFromH\\SVNScheduler\\Configurations\\DataIntegration_Design.xml");
                // xmlDocument.Load(this.configPath);
                nodeList = xmlDocument.SelectNodes("/DataIntegration/DataTable[@SourceFileType ='Excel']");
            int inc = 0;

            if (nodeList != null)
                //ds.Load(nodeList.InnerText,);
                foreach (XmlNode node in nodeList)
                {
                    foreach (XmlNode item in node.ChildNodes)
                    {
                        foreach (XmlNode newNode in item)
                        {
                            columnDefinations.Add(
                                                    new ColumnDefinations()
                                                    {
                                                        fileColumnName = newNode.Attributes[0].Value
                                                      ,
                                                        dBColumnName = newNode.Attributes[1].Value
                                                      ,
                                                        columnDataType = newNode.Attributes[2].Value
                                                      ,
                                                        length = Double.Parse(string.IsNullOrEmpty(newNode.Attributes[3].Value) ? "0" : newNode.Attributes[3].Value)
                                                      ,
                                                        allowNull = newNode.Attributes[4].Value
                                                    });
                        }
                    }
                }

            return columnDefinations;
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {

        }
    }
}
}
