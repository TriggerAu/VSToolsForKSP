using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSPExtensions.Settings
{
    public class LocalizerUserSettings : BaseSettings
    {
        public int NextUserID { get; set; }

        public LocalizerUserSettings()
        {
            NextUserID = 600000;
        }
    }
    
}