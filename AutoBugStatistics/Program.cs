using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Text;
namespace AutoBugStatistics
{
    public struct Config
    {
        public List<string> ProjectNameList;
        public string key;
        public string host;
    }
    class Program
    {
        public static RedmineManager manager;
        //private static string host = "http://172.20.0.60/redmines/wise-paas";
        private static Config MyConfig = new Config();
        private static RedmineFun redmine = new RedmineFun();        
        private static Dictionary<string, int> BugNumber = new Dictionary<string, int>();
        private static Dictionary<string, string> LogList = new Dictionary<string, string>();
        private static Dictionary<string, int> StatusList = new Dictionary<string, int>();
        private static Dictionary<string, int> PriorityList = new Dictionary<string, int>();
        
        static void Main(string[] args)
        {
            string filePath = System.Environment.CurrentDirectory + "\\BugStatistic_" +
                    DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".html";
            try
            {
                ReadIni();                
                //connet to redmine           
                try
                {
                    manager = new RedmineManager(MyConfig.host, MyConfig.key);//apiKey                   
                }
                catch (Exception ex)
                {
                    WriteMyLog(filePath, "ErrorMessage:" + ex.Message);                   
                    return;
                }

                foreach (IssueStatus itemStatus in redmine.GetAllObjectList<IssueStatus>())
                {
                    if (!StatusList.ContainsKey(itemStatus.Name) && itemStatus.Name.ToLower() != "closed")
                        StatusList.Add(itemStatus.Name, 0);
                }
                foreach (IssuePriority itemPriority in redmine.GetAllObjectList<IssuePriority>())
                {
                    if (!PriorityList.ContainsKey(itemPriority.Name))
                        PriorityList.Add(itemPriority.Name, 0);
                }

                foreach (Project item in redmine.GetAllObjectList<Project>())
                {
                    try
                    {
                        BugNumber.Clear();                       
                        if (IsProjectExistInIni(item))
                        {
                            Console.WriteLine("Statistic " + item.Name + "...");     
                            NameValueCollection mem = new NameValueCollection { { "project_id", item.Id.ToString() }, { "status_id", "*" } };
                            int num = 0;   
                            foreach (Issue itemissue in redmine.GetAllObjectList<Issue>(mem, out num))
                            {
                                if (itemissue.Tracker.Name.ToLower() == "bug")
                                {
                                    string status = itemissue.Status.Name;
                                    string priority = itemissue.Priority.Name;
                                    if (BugNumber.ContainsKey(status + "," + priority))
                                    {
                                        BugNumber[status + "," + priority] += 1;
                                    }
                                    else
                                    {
                                        BugNumber.Add(status + "," + priority, 1);
                                    }                                                                  
                                }
                            }
                            string project_name = Regex.Replace(item.Name, @"\s", "");
                            if (!LogList.ContainsKey(project_name))
                                LogList.Add(project_name, GetBugLog(item));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error Message:" + e.Message);
                        continue;
                    }

                }
                WriteLog(filePath);
            }
            catch(Exception e)
            {
                WriteMyLog(filePath, "ErrorMessage:" + e.Message);                
            }
            
        }
        private static void WriteLog(string filePath)
        {
            for (int i = 0; i < MyConfig.ProjectNameList.Count; i++)
            {
                string name = Regex.Replace(MyConfig.ProjectNameList[i], @"\s", "");
                if (LogList.ContainsKey(name))
                    WriteMyLog(filePath, LogList[name]);
            }
        }
        public static Project GetParentProject(Project project)
        {            
             if (project.Parent != null)
            {
                IList<Project> list = redmine.GetProjectsByName(project.Parent.Name);
                return list[0];
            }
            else            
                return null;
        }
        public static string GetParentList(Project project)
        {
            List<string> projectList = new List<string>();            
            Project parent = GetParentProject(project);
            projectList.Add(project.Name);
            while(parent != null)
            {
                projectList.Add(parent.Name);
                parent = GetParentProject(parent);                
            }
            string str = string.Empty;
            for (int i = projectList.Count - 1; i > 0; i--)
            {
                str += projectList[i] + ">";
            }
            str += projectList[0];
            return str;
            
        }
        public static string AddHTMLoStr(string source, bool IsHead)
        {                
            if(IsHead)
                return "<th width=" + '"' + "80" + '"' + "align=" + '"' + "center" + '"' + 
                    ">" + source  + "</th>";
            else
                return "<td width=" + '"' + "80" + '"' + "align=" + '"' + "center" + '"' + 
                    ">" + source + "</td>";                       
        }       
        public static string GetBugLog(Project project)
        {
            string result = string.Empty;
            string str = string.Empty;   
            result +="<br>" + "Time:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "<br>";
            result += GetParentList(project) + "<br>";
            result += "<table border=" + '"' + "1" + '"' +">";

            int openBugs = 0;
            int resolvedBugs = 0;
            int verifiedBugs = 0;
            foreach (var item in StatusList)
            {                
                if (item.Key == "Resolved" || item.Key == "Reviewing")
                    resolvedBugs += GetTotalNumber(item.Key);
                else if (item.Key == "Verified")
                    verifiedBugs += GetTotalNumber(item.Key);
                else 
                    openBugs += GetTotalNumber(item.Key);
            }            
            str = string.Format("OpenBug:{0}; ResolvedBug:{1}; VerifiedBug:{2}",
                 openBugs, resolvedBugs, verifiedBugs);

            result += "<th colspan=" + '"' + (StatusList.Count + 1) + '"' + ">" + str + "</th>";

            result += "<tr>" + AddHTMLoStr("", false);
            foreach (var item in StatusList)
            {
                result += AddHTMLoStr(item.Key, true);
            }
            result += "</tr>";

            
            foreach (var itemCol in PriorityList)
            {
                result += "<tr>" + AddHTMLoStr(itemCol.Key, true);
                foreach (var itemRow in StatusList)
                {
                    result += AddHTMLoStr(GetBugNumber(itemRow.Key + "," + itemCol.Key).ToString(), false);

                }
                result += "</tr>";
            }

            result += "<tr>" + AddHTMLoStr("Total", true);
            foreach (var item in StatusList)
            {
                result += AddHTMLoStr(GetTotalNumber(item.Key).ToString(), false);
            }
            result += "</tr>";  

            result += "</table>";
            return result;
        }      
       
        public static int GetTotalNumber(string status)
        {
            int num = 0;
            foreach (var item in BugNumber)
            {
                if (item.Key.Contains(status))
                    num += item.Value;
            }
            return num;
        }
        public static int GetBugNumber(string key)
        {
            int num = 0;
            if (BugNumber.ContainsKey(key))
                num = BugNumber[key];
            return num;
            
        }
        public static void WriteMyLog(string fileName, string str)
        {
            FileStream file = null;
            StreamWriter sw = null;
            try
            {
                file = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(file, Encoding.GetEncoding("ASCII"));               
                sw.WriteLine(str);
            }
            catch
            {
            }

            if (sw != null)
                sw.Close();

            if (file != null)
                file.Close();
        }
        public static bool IsProjectExistInIni(Project project)
        {
            for(int i = 0; i < MyConfig.ProjectNameList.Count; i++)
            {
                string name1 = Regex.Replace(MyConfig.ProjectNameList[i], @"\s", "");
                string name2 = Regex.Replace(project.Name, @"\s", "");
                if (name1 == name2)
                    return true;
            }
            return false;
        }

        public static void ReadIni()
        {
            MyConfig.ProjectNameList = new List<string>();
            IniFile ini = new IniFile(System.Environment.CurrentDirectory + "\\AutoBugStatistics.ini");           
            for (int i = 1; i < 1000; i++)
            {
                string str = ini.IniReadValue("Project", "ProjectName" + i.ToString());
                if(str == "")
                    break;
                MyConfig.ProjectNameList.Add(str);
            }
            MyConfig.key = ini.IniReadValue("Key", "RedmineKey");
            MyConfig.host = ini.IniReadValue("Key", "RedmineHost");            
        }
       
    }
}
