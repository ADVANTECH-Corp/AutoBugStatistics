using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

using System.Net;


namespace AutoBugStatistics
{
    class RedmineFun
    {       
        #region GetFun        

        public Dictionary<string, int> GetRedmineProjectMember(int project_id)
        {
            //get redmine member list
            Dictionary<string, int> redmine_member = new Dictionary<string, int>();
            NameValueCollection mem = new NameValueCollection { { "project_id", project_id.ToString() } };
            int num = 0;
            foreach (var item in GetAllObjectList<ProjectMembership>(mem, out num))
            {
                redmine_member.Add(item.User.Name, item.User.Id);
            }
            return redmine_member;
        }

        public IList<Project> GetProjectsByName(string name, bool beUpper)
        {
            IList<Project> items = new List<Project>();           
            foreach (Project project in GetAllObjectList<Project>())
            {
                if (beUpper == true)
                {
                    if (project.Name.ToUpper() == name.ToUpper())
                        items.Add(project);
                }
                else
                {
                    if (project.Name == name)
                        items.Add(project);
                }
            }
            return items;
        }

        public IList<T> GetAllObjectList<T>() where T : class, new()
        {
            int totalCount;
            return GetAllObjectList<T>(out totalCount);
        }

        public IList<T> GetAllObjectList<T>(out int totalCount) where T : class, new()
        {
            NameValueCollection parameters = new NameValueCollection { { "status_id", "*" } };

            return GetAllObjectList<T>(parameters, out totalCount);
        }

        public IList<T> GetAllObjectList<T>(NameValueCollection newParameters, out int totalCount) where T : class, new()
        {
            NameValueCollection parameters = new NameValueCollection { { "limit", "100" } };
            parameters.Add(newParameters);
            List<T> objects;

            objects = Program.manager.GetObjectList<T>(parameters, out totalCount).ToList();

            if (totalCount > 100)
            {
                int steps = (totalCount % 100 > 0 ? (totalCount / 100) + 1 : totalCount / 100);
                for (int step = 1; step < steps; step++)
                {
                    parameters = new NameValueCollection { { "limit", "100" }, { "offset", (step * 100).ToString() } };
                    parameters.Add(newParameters);
                    objects.AddRange(Program.manager.GetObjectList<T>(parameters).ToList());
                }
            }

            return objects;
        }
     
        public T GetFirstObject<T>() where T : class, new()
        {
            NameValueCollection parameters = new NameValueCollection { { "status_id", "*" }, { "sort", "created_on" }, { "limit", "1" } };
            List<T> objects;

            objects = Program.manager.GetObjectList<T>(parameters).ToList();
            if (objects.Count <= 0)
            {
                return null;
            }

            return objects[0];
        }

        public IList<Project> GetProjectsByName(string name)
        {
            return GetProjectsByName(name, false);
        }      

        #endregion


    }
}
