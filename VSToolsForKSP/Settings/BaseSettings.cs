using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VSToolsForKSP.Settings
{
    public class BaseSettings : BindableBase
    {
        public void WriteXML(string filePath)
        {
            XmlSerializer x = new XmlSerializer(this.GetType());
            TextWriter tw = File.CreateText(filePath);
            x.Serialize(tw, this);
            tw.Close();
        }

        public static T CreateFromXML<T>(string filePath) where T : BaseSettings
        {
            if (File.Exists(filePath))
            {
                XmlSerializer x = new XmlSerializer(typeof(T));
                TextReader tr = File.OpenText(filePath);
                var r = (T)x.Deserialize(tr);
                tr.Close();
                return r;
            }
            else
            {
                //ExtensionsGlobal.WriteOutputPane("File doesn't exist:{0}", filePath);
                return null;
            }
        }
    }
}
