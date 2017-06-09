using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSToolsForKSP.Settings
{
    public class LocalizerProjectSettings : BaseSettings
    {
        private string tagAutoLocPortion;
        public string TagAutoLocPortion
        {
            get { return tagAutoLocPortion; }
            set { Set(ref tagAutoLocPortion, value); }
        }
        private string tagProjectPortion;
        public string TagProjectPortion
        {
            get { return tagProjectPortion; }
            set { Set(ref tagProjectPortion, value); }
        }
        private IDTypeEnum idType;
        public IDTypeEnum IDType
        {
            get { return idType; }
            set { Set(ref idType, value); }
        }

        private int nextProjectId;
        public int NextProjectID {
            get { return nextProjectId; }
            set { Set(ref nextProjectId, value); }
        }

        private string baseCfgFile;
        public string BaseCfgFile {
            get { return baseCfgFile; }
            set { Set(ref baseCfgFile, value); }
        }

        private string languageCodes;
        public string LanguageCodes
        {
            get { return languageCodes; }
            set { Set(ref languageCodes, value); }
        }

        private bool useMultiCFGFiles;

        public bool UseMultiCfgFiles
        {
            get { return useMultiCFGFiles; }
            set { Set(ref useMultiCFGFiles , value); }
        }

        private string multiCfgFile;
        public string MultiCfgFile
        {
            get { return multiCfgFile; }
            set { Set(ref multiCfgFile, value); }
        }


        public LocalizerProjectSettings()
        {
            TagAutoLocPortion = "#autoLOC";
            TagProjectPortion = "";
            IDType = IDTypeEnum.ProjectBased;
            NextProjectID = 1000000;
            BaseCfgFile = "refactor.cfg";
            LanguageCodes = "en-us,es-es,ja,ru,zh-cn";
            UseMultiCfgFiles = false;
            MultiCfgFile = "refactor_{LANGCODE}.cfg";
        }
        public LocalizerProjectSettings(string projectName) : this()
        {
            TagProjectPortion = projectName;
        }

        public enum IDTypeEnum
        {
            ProjectBased,
            UserBased
        }
        public IList<IDTypeEnum> IDTypes
        {
            get
            {
                return Enum.GetValues(typeof(IDTypeEnum)).Cast<IDTypeEnum>().ToList<IDTypeEnum>();
            }
        }
    }
    
}