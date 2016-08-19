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
        private static int MaxStrLenght = 13;
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
                                    string status = itemissue.Status.Name.ToLower();
                                    string priority = itemissue.Priority.Name.ToLower();
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
            int newnum = GetTotalNumber("new");
            int confirmednum = GetTotalNumber("confirmed");
            int assignednum = GetTotalNumber("assigned");
            int reopenednum = GetTotalNumber("reopened");
            int reviewnum = GetTotalNumber("reviewing");
            int resolvednum = GetTotalNumber("resolved");
            int verifiednum = GetTotalNumber("verified");            
            result +="<br>" + "Time:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "<br>";
            result += GetParentList(project) + "<br>";
            result += "<table border=" + '"' + "1" + '"' +">";
            str = string.Format("OpenBug:{0}; ResolvedBug:{1}; VerifiedBug:{2}",
                 newnum + confirmednum + assignednum + reopenednum, resolvednum + reviewnum, verifiednum);
            result += "<th colspan=" + '"' + "8" + '"' + ">" + str + "</th>";
            str = "<tr>" + AddHTMLoStr("", false) + AddHTMLoStr("New", true) +
                  AddHTMLoStr("Comfirmed", true) + AddHTMLoStr("Assigned", true) +
                  AddHTMLoStr("Reopened", true) + AddHTMLoStr("Reviewing", true) +
                  AddHTMLoStr("Resolved", true) + AddHTMLoStr("Verified", true) + "</tr>";               
            result += str;
            str = "<tr>" + AddHTMLoStr("Low", true) +
                  AddHTMLoStr(GetBugNumber("new,low").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("confirmed,low").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("assigned,low").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reopened,low").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reviewing,low").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("resolved,low").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("verified,low").ToString(), false) + "</tr>";                 
            result += str;
            str = "<tr>" + AddHTMLoStr("Normal", true) +
                  AddHTMLoStr(GetBugNumber("new,normal").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("confirmed,normal").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("assigned,normal").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reopened,normal").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reviewing,normal").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("resolved,normal").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("verified,normal").ToString(), false) + "</tr>";                 
            result += str ;
            str = "<tr>" + AddHTMLoStr("High", true) +
                  AddHTMLoStr(GetBugNumber("new,high").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("confirmed,high").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("assigned,high").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reopened,high").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reviewing,high").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("resolved,high").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("verified,high").ToString(), false) + "</tr>";                
            result += str;
            str = "<tr>" + AddHTMLoStr("Urgent", true) +
                  AddHTMLoStr(GetBugNumber("new,Urgent").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("confirmed,Urgent").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("assigned,Urgent").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reopened,Urgent").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reviewing,Urgent").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("resolved,Urgent").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("verified,Urgent").ToString(), false) + "</tr>";                
            result += str ;
            str = "<tr>" + AddHTMLoStr("Immediate", true) +
                  AddHTMLoStr(GetBugNumber("new,immediate").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("confirmed,immediate").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("assigned,immediate").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reopened,immediate").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("reviewing,immediate").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("resolved,immediate").ToString(), false) +
                  AddHTMLoStr(GetBugNumber("verified,immediate").ToString(), false) + "</tr>";                
            result += str ;
            str = "<tr>" + AddHTMLoStr("Total", true) +
                  AddHTMLoStr(newnum.ToString(), false) +
                  AddHTMLoStr(confirmednum.ToString(), false) +
                  AddHTMLoStr(assignednum.ToString(), false) +
                  AddHTMLoStr(reopenednum.ToString(), false) +
                  AddHTMLoStr(reviewnum.ToString(), false) +
                  AddHTMLoStr(resolvednum.ToString(), false) +
                  AddHTMLoStr(verifiednum.ToString(), false) + "</tr>";
            result += str;
            result += "</table>";
            return result;
        }
        //public static string GetBugLog(Project project)
        //{
        //    string result = string.Empty;
        //    string str = string.Empty;
        //    //string strFormat = "{0,10}{1,10}{2,10}{3,10}{4,10}{5,10}{6,10}{7,10}";
        //    //string numFormat = "{0,10}{1,10:D}{2,10:D}{3,10:D}{4,10:D}{5,10:D}{6,10:D}{7,10:D}"; 
        //    string strFormat = "{0}\t\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}";
        //    string numFormat = "{0}\t{1:D}\t\t{2:D}\t\t{3:D}\t\t{4:D}\t\t{5:D}\t\t{6:D}\t\t{7:D}";
        //    int newnum = GetTotalNumber("new");
        //    int confirmednum = GetTotalNumber("confirmed");
        //    int assignednum = GetTotalNumber("assigned");
        //    int reopenednum = GetTotalNumber("reopened");
        //    int reviewnum = GetTotalNumber("reviewing");
        //    int resolvednum = GetTotalNumber("resolved");
        //    int verifiednum = GetTotalNumber("verified");
        //    result += "Time:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "\r\n";
        //    result += GetParentList(project) + "\r\n";
        //    result += "*******************************************************************************************************************\r\n";
        //    str = string.Format("OpenBug:{0}; ResolvedBug:{1}; VerifiedBug:{2}",
        //         newnum + confirmednum + assignednum + reopenednum, resolvednum + reviewnum, verifiednum);
        //    result += str + "\r\n";
        //    str = string.Format(strFormat,
        //        "    ", "New", "Confirmed", "Assigned", "Reopened", "Reviewing", "Resolved", "Verified");
        //    result += str + "\r\n";
        //    str = string.Format(numFormat,
        //        "Low      ", GetBugNumber("new,low"), GetBugNumber("confirmed,low"),
        //        GetBugNumber("assigned,low"), GetBugNumber("reopened,low"),
        //        GetBugNumber("reviewing,low"), GetBugNumber("resolved,low"),
        //        GetBugNumber("verified,low"));
        //    result += str + "\r\n";
        //    str = string.Format(numFormat,
        //        "Normal   ", GetBugNumber("new,normal"), GetBugNumber("confirmed,normal"),
        //        GetBugNumber("assigned,normal"), GetBugNumber("reopened,normal"),
        //        GetBugNumber("reviewing,normal"), GetBugNumber("resolved,normal"),
        //        GetBugNumber("verified,normal"));
        //    result += str + "\r\n";
        //    str = string.Format(numFormat,
        //        "High     ", GetBugNumber("new,high"), GetBugNumber("confirmed,high"),
        //        GetBugNumber("assigned,high"), GetBugNumber("reopened,high"),
        //        GetBugNumber("reviewing,high"), GetBugNumber("resolved,high"),
        //        GetBugNumber("verified,high"));
        //    result += str + "\r\n";
        //    str = string.Format(numFormat,
        //        "Urgent   ", GetBugNumber("new,urgent"), GetBugNumber("confirmed,urgent"),
        //        GetBugNumber("assigned,urgent"), GetBugNumber("reopened,urgent"),
        //        GetBugNumber("reviewing,urgent"), GetBugNumber("resolved,urgent"),
        //        GetBugNumber("verified,urgent"));
        //    result += str + "\r\n";
        //    str = string.Format(numFormat,
        //        "Immediate", GetBugNumber("new,immediate"), GetBugNumber("confirmed,immediate"),
        //        GetBugNumber("assigned,immediate"), GetBugNumber("reopened,immediate"),
        //        GetBugNumber("reviewing,immediate"), GetBugNumber("resolved,immediate"),
        //        GetBugNumber("verified,immediate"));
        //    result += str + "\r\n";
        //    str = string.Format(numFormat,
        //        "Total    ", newnum, confirmednum, assignednum, reopenednum, reviewnum, resolvednum, verifiednum);
        //    result += str + "\r\n";
        //    result += "*******************************************************************************************************************\r\n";
        //    return result;
        //}
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
