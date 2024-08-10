using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Windows;

namespace CRXLogon
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public object HttpUtility { get; private set; }

        public String token;
        public String domain;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
            }
        }

        private void BTN_Connect(object sender, RoutedEventArgs e)
        {

            domain = selectedDomain.Text;
            var t = String.IsNullOrEmpty(userName.Text);
            var p = String.IsNullOrEmpty(pw.Password);
            var d = String.IsNullOrEmpty(domain);
            var b = userName.Text;
            Console.WriteLine("BTN_Connect user " + p);
            Console.WriteLine("BTN_Connect domain " + domain);
            if ( t || p || d)
            {
                String message = "Password und Benutzernamen eingeben und domain wählen!";
                String caption = "Fehler";
                MessageBoxButton button = MessageBoxButton.OKCancel;
                var result = MessageBox.Show(message, caption, button);
            }
            else
            {
                String name = userName.Text;
                String pwd = string.Format(pw.Password);
                //MessageBox.Show("User is:"+ user, "pw is:" + pwd);
                enableDisable(false);
                stConnect(name, pwd);
            }
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("net.exe", "use /del * /yes");
            List<String> domains = GetDomains();
            selectedDomain.ItemsSource = domains;
            if (domains.Count > 0) {
                selectedDomain.SelectedItem = domains[0];
                domain = domains[0];
            }
        }

        private List<String> GetDomains()
        {
            List<String> domains = new List<string>();
            foreach (NetworkInterface @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                String suffix = @interface.GetIPProperties().DnsSuffix;
                if( suffix != "fritz.box" && suffix != "")
                {
                    domains.Add(suffix);
                }
            }
            return domains;
        }
        private void runScript(String script)
        {
            //MessageBox.Show(script + "runScript");

            string path = ConfigurationManager.AppSettings["tmp"];
            string fullPath = System.Environment.ExpandEnvironmentVariables(path); ProcessStartInfo processInfo;

            // MessageBox.Show(fullPath + "savepath of script");
            //string output = "";

            string logon = fullPath + "\\Logon.bat";

            //MessageBox.Show(logon);

            System.IO.File.WriteAllText(logon, script);
            Console.WriteLine("runScript script:" + script);
            Console.WriteLine("runScript logon:" + logon);

            Process process;
            processInfo = new ProcessStartInfo(logon);

            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            process = Process.Start(processInfo);
            process.WaitForExit();
            this.Visibility = Visibility.Collapsed;
           // disconnectPrinterShare();
            Window1 connect = new Window1();
            connect.ShowDialog();
        }

        private void enableDisable(Boolean ed)
        {
            pw.IsEnabled = userName.IsEnabled = selectedDomain.IsEnabled = ed;
            okButton.IsEnabled = ed;
            cancelButton.IsEnabled = ed;
        }
        public void stConnect(String name, String pwd)
        {
            HttpResponseMessage s =null ;
            try
            {
                s = GetToken(name, pwd);
                if (s.StatusCode.ToString() == "OK")
                {

                    String script = GetLogonScript(s.Content.ReadAsStringAsync().Result);
                    runScript(script);

                }
                else if (s.StatusCode.ToString() == "Unauthorized")
                {
                    pw.Clear();
                    String caption = "Fehler";
                    enableDisable(true);
                    MessageBox.Show("Passwort falsch!", caption);
                }
            }
            catch {
                enableDisable(true);
                MessageBox.Show("Admin nicht erreichbar!", "Unerreichbar");
            }
            /*Connect con = new Connect(name, pw, s);
            con.ShowDialog();
            this.Close();


            String D = getDNSName(s);
             MessageBox.Show(D);
             ConnectNetworkDrives(name,pw);
             Console.WriteLine(GetPrinters(s,name,pw));*/
        }

        private void connectPrinterShare(String token)
        {

            IWshNetwork_Class printerShare = new IWshNetwork_Class();

            String Domain = GetDomainName(token);
            String printserver = GetPrintserver(token);

            string share = "\\\\" + printserver + "." + domain + "\\print$";
            string cred = Domain + "\\" + userName.Text;
            //  MessageBox.Show(Domain + "   " + printserver + " Cred is: " + cred );

            printerShare.MapNetworkDrive("X:", share, Type.Missing, cred, pw.Password);
        }

        private void disconnectPrinterShare()
        {
            IWshNetwork_Class printerShare = new IWshNetwork_Class();

            printerShare.RemoveNetworkDrive("X:");
        }

        private HttpResponseMessage GetToken(String name, String password)
        {

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            var client = new HttpClient();

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("username", name));
            values.Add(new KeyValuePair<string, string>("password", password));
            FormUrlEncodedContent content = new FormUrlEncodedContent(values);
            //client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var url = "https://admin." + domain + "/api/sessions/login";
            Console.WriteLine("GetToken url:" + url);
            Console.WriteLine("GetToken content:" + content);
            HttpResponseMessage res = client.PostAsync(url, content).Result;
            Console.WriteLine("GetToken Status: " + res.StatusCode);
            var token = res.Content.ReadAsStringAsync().Result;
            Console.WriteLine("GetToken token: " + token);
            return res;
        }

        private String GetDomainName(String token)
        {
            var domain = " ";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            var client = new WebClient();
            client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
            client.Headers[HttpRequestHeader.Accept] = "text/plain";

            //var data = "username=" + name + "&password=" + password;

            try
            {
                var res = client.DownloadData("https://admin." +domain+ "/api/system/configuration/WORKGROUP");
                domain = System.Text.Encoding.UTF8.GetString(res);
                return domain;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return "failed";
            }
        }

        private String GetPrintserver(String token)
        {
            var printserver = " ";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            var client = new WebClient();
            client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
            client.Headers[HttpRequestHeader.Accept] = "text/plain";

            //var data = "username=" + name + "&password=" + password;

            try
            {
                var res = client.DownloadData("https://admin."+domain+"/api/system/configuration/PRINTSERVER");
                printserver = System.Text.Encoding.UTF8.GetString(res);
                // MessageBox.Show("Printserver is at: " + printserver);
                return printserver;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return "failed";
            }
        }

        private String GetLogonScript(String token)
        {
            var script = " ";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            var client = new WebClient();
            client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
            client.Headers[HttpRequestHeader.Accept] = "text/plain";

            //var data = "username=" + name + "&password=" + password;

            try
            {
                var res = client.DownloadData("https://admin."+domain+"/api/sessions/logonScript/WIN");
                script = System.Text.Encoding.UTF8.GetString(res);
                //MessageBox.Show(script);
                return script;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return "failed";
            }
        }

        private void BTN_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
            Application.Current.Shutdown();
        }

        private void OnClose(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("net.exe", "use /del * /yes");
            Application.Current.Shutdown();
            string path = ConfigurationManager.AppSettings["tmp"];
            string fullPath = System.Environment.ExpandEnvironmentVariables(path);


            string logon = fullPath + "\\Logon.bat";

            System.IO.File.Delete(logon);

        }
    }
}
