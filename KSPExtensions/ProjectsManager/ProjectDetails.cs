using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSPExtensions
{
    internal class ProjectDetails
    {
        public string name;
        public string fileName;
        public List<string> references = new List<string>();

        public string FolderPath { get { return System.IO.Path.GetDirectoryName(fileName); } }
    }
}
