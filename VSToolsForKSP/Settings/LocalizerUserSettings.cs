using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSToolsForKSP.Settings
{
    public class LocalizerUserSettings : BaseSettings
    {
        private int nextUserID;
        public int NextUserID
        {
            get { return nextUserID; }
            set { Set(ref nextUserID, value); }
        }

        public LocalizerUserSettings()
        {
            NextUserID = 600000;
        }
    }
    
}