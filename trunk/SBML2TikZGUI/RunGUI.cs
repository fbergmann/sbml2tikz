using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace SBML2TikZ_GUI
{
    class RunGUI
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
         
        }
    }
}
