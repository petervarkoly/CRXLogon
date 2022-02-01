using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CRXLogon
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            Thread.Sleep(2000);
            this.WindowState = WindowState.Minimized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            System.Diagnostics.Process.Start("net.exe", "use /del * /yes");

            unmapPrinter();
            Application.Current.Shutdown();
            string path = ConfigurationManager.AppSettings["tmp"];
            string fullPath = System.Environment.ExpandEnvironmentVariables(path); 


            string logon = fullPath + "\\Logon.bat";

            File.Delete(logon);

        }

        public void unmapPrinter()
        {
            ConnectionOptions options = new ConnectionOptions();
            options.EnablePrivileges = true;
            ManagementScope scope = new ManagementScope(ManagementPath.DefaultPath, options);
            scope.Connect();
            ManagementClass win32Printer = new ManagementClass("Win32_Printer");
            ManagementObjectCollection printers = win32Printer.GetInstances();
           
            foreach (ManagementObject printer in printers)
            {
                if (printer != null)
                {
                    printer.Delete();
                }
            }
        }
    }
}
