using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FormsFeed;

namespace FormsFeed.WinForms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var cache = new Cache())
            {
                using (var form = new MainForm(cache))
                {
                    Application.Run(form);
                }
            }
        }
    }
}
