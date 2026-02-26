using System;
using System.Windows.Forms;
using HardwareHook.Main.Forms;

namespace HardwareHook.Main
{
    static class Program
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
