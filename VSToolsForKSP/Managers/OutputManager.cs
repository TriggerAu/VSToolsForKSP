using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSToolsForKSP.Managers
{
    internal static class OutputManager
    {
        private static IVsOutputWindow outWindow;
        private static IVsOutputWindowPane debugPane;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            outWindow = serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            Guid customGuid = new Guid("0F44E2D1-F5FA-4d2d-AB30-22BE8ECD9789");
            string customTitle = "VS Tools for KSP";
            outWindow.CreatePane(ref customGuid, customTitle, 1,0);

            //Guid generalPaneGuid = VSConstants.GUID_OutWindowDebugPane;
            outWindow.GetPane(ref customGuid, out debugPane);
        }

        public static void ShowPane()
        {
            debugPane.Activate();
        }

        public static void WriteError(string message, params object[] args)
        {
            WriteLine("ERROR: " + message, args);
            ShowPane();
        }
        public static void WriteErrorEx(Exception ex, string message, params object[] args)
        {
            WriteError(message, args);
            WriteLine("Details: " + ex.Message);
        }
        public static void Write(string message)
        {
            debugPane.OutputString(message);
        }
        public static void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }

        public static void WriteLine(string message)
        {
            Write(message);
            Write("\n");
        }

        public static void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }
    }
}
