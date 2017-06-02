using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSPExtensions.Settings
{
    public class LocalizerSettings : BaseSettings
    {
        public LocalizerProjectSettings ProjectSettings { get; set; }
        public LocalizerUserSettings UserSettings { get; set; }

        public LocalizerSettings(string projectName)
        {
            ProjectSettings = new LocalizerProjectSettings(projectName);
            UserSettings = new LocalizerUserSettings();
        }

        public void WriteAllXML(string projectPath)
        {
            ProjectSettings.WriteXML(projectPath + "/.ksplocalizer.settings");
            UserSettings.WriteXML(projectPath + "/.ksplocalizer.settings.user");
        }

        public string NextTag { get {
                string returnValue = "";

                returnValue += ProjectSettings.TagAutoLocPortion + "_";
                if (!string.IsNullOrWhiteSpace( ProjectSettings.TagProjectPortion))
                    returnValue += ProjectSettings.TagProjectPortion + "_";

                if(ProjectSettings.IDType == LocalizerProjectSettings.IDTypeEnum.ProjectBased)
                { 
                    returnValue += ProjectSettings.NextProjectID;
                }
                else
                {
                    returnValue += UserSettings.NextUserID;
                }
                return returnValue;
            } }
    }
}