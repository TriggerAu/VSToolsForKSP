using VSToolsForKSP.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSToolsForKSP.Managers
{
    public class ProjectDetails
    {
        public string name;
        public string fileName;
        public List<string> references = new List<string>();

        public LocalizerSettings LocalizerSettings { get; set; }

        public ProjectDetails()
        {
            LocalizerSettings = null;
        }

        public string FolderPath { get { return System.IO.Path.GetDirectoryName(fileName); } }

        public bool HasLocalizerSettings { get { return LocalizerSettings != null; } }
    }
}
