using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSPExtensions.Settings
{
    public class LocalizerProjectSettings : BaseSettings
    {
        public string TagAutoLocPortion { get; set; }
        public string TagProjectPortion { get; set; }
        public IDTypeEnum IDType { get; set; }

        public int NextProjectID { get; set; }

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