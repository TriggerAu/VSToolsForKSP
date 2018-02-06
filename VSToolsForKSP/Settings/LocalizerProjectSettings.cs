using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSToolsForKSP.Managers;

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
        public int NextProjectID
        {
            get { return nextProjectId; }
            set { Set(ref nextProjectId, value); }
        }

        private string baseCfgFile;
        public string BaseCfgFile
        {
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
            set { Set(ref useMultiCFGFiles, value); }
        }

        private bool useMultiAndBaseCFGFiles;
        public bool UseMultiAndBaseCfgFiles
        {
            get { return useMultiAndBaseCFGFiles; }
            set { Set(ref useMultiAndBaseCFGFiles, value); }
        }

        private string multiCfgFile;
        public string MultiCfgFile
        {
            get { return multiCfgFile; }
            set { Set(ref multiCfgFile, value); }
        }

        private bool addLanguageCodePrefixToMultiFiles;
        public bool AddLanguageCodePrefixToMultiFiles
        {
            get { return addLanguageCodePrefixToMultiFiles; }
            set { Set(ref addLanguageCodePrefixToMultiFiles, value); }
        }


        public List<LocalizerTemplateSettings> Templates { get; set; }

        public LocalizerProjectSettings()
        {
            TagAutoLocPortion = "#autoLOC";
            TagProjectPortion = "";
            IDType = IDTypeEnum.ProjectBased;
            NextProjectID = 1000000;
            BaseCfgFile = "refactor.cfg";
            LanguageCodes = "en-us,es-es,ja,ru,zh-cn,de-de,fr-fr,it-it,pt-br";
            UseMultiCfgFiles = false;
            MultiCfgFile = "refactor_{LANGCODE}.cfg";
            addLanguageCodePrefixToMultiFiles = true;
            Templates = new List<LocalizerTemplateSettings>();
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

        public void SaveTemplate(string name)
        {
            LocalizerTemplateSettings t = new LocalizerTemplateSettings();

            t.name = name;

            t.BaseCfgFile = BaseCfgFile;
            t.LanguageCodes = LanguageCodes;
            t.AddLanguageCodePrefixToMultiFiles = AddLanguageCodePrefixToMultiFiles;
            t.UseMultiCfgFiles = UseMultiCfgFiles;
            t.UseMultiAndBaseCfgFiles = UseMultiAndBaseCfgFiles;
            t.MultiCfgFile = MultiCfgFile;

            //Add the new template to the array
            for (int i = Templates.Count; i-- > 0;)
            {
                if (Templates[i].name == t.name)
                {
                    Templates.RemoveAt(i);
                }
            }
            Templates.Add(t);

            Templates = Templates.OrderBy(x => x.name).ToList();
        }

        public void ApplyTemplate(string name)
        {
            foreach (LocalizerTemplateSettings t in Templates)
            {
                if(t.name == name)
                {
                    BaseCfgFile = t.BaseCfgFile;
                    LanguageCodes = t.LanguageCodes;
                    AddLanguageCodePrefixToMultiFiles = t.AddLanguageCodePrefixToMultiFiles;
                    UseMultiCfgFiles = t.UseMultiCfgFiles;
                    UseMultiAndBaseCfgFiles = t.UseMultiAndBaseCfgFiles;
                    MultiCfgFile = t.MultiCfgFile;

                    return;
                }
            }
            OutputManager.WriteLine("Template not found in settings:" + name);
        }
    }
}