using SecurityDemo.Win;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XafApiConverter.TestProject.Win {
    internal static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SecurityDemoWindowsFormsApplication application = new SecurityDemoWindowsFormsApplication();
        }
    }
}
