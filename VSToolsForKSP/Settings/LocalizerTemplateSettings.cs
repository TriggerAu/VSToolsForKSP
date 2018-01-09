using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSToolsForKSP.Settings
{
    public class LocalizerTemplateSettings : BaseSettings
    {
        public string name;

        public bool UseMultiCfgFiles { get; set; }
        public bool UseMultiAndBaseCfgFiles { get; set; }
        public bool AddLanguageCodePrefixToMultiFiles { get; set; }
        public string BaseCfgFile { get; set; }
        public string MultiCfgFile { get; set; }
        public string LanguageCodes { get; set; }
    }
}
