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
            domain = selectedDomain.SelectedItem.ToString();
            var t = String.IsNullOrEmpty(userName.Text);
            var p = String.IsNullOrEmpty(pw.Password);
            var b = userName.Text;
            Console.WriteLine(t);
            Console.WriteLine(b);
            Console.WriteLine(p);
            if (String.IsNullOrEmpty(userName.Text) || String.IsNullOrEmpty(pw.Password))
            {
                String message = "Password und Benutzernamen eingeben!";
                String caption = "Fehler";
                MessageBoxButton button = MessageBoxButton.OKCancel;

                var result = MessageBox.Show(message, caption, button);

            }
            else
            {

                String name = userName.Text;
                String pwd = string.Format(pw.Password);
                //MessageBox.Show("User is:"+ user, "pw is:" + pwd);
                stConnect(name, pwd);
            }


        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("net.exe", "use /del * /yes");
            List<String> domains = GetDomains();
            selectedDomain.ItemsSource = domains;
            selectedDomain.SelectedItem = domains[0];
            domain = domains[0];
            Console.WriteLine(domains.Capacity);
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


        public void stConnect(String name, String pwd)
        {
            HttpResponseMessage s =null ;
            try
            {
                s = GetToken(name, pwd);
            }
            catch {
                MessageBox.Show("Admin nicht erreichbar!", "Unerreichbar");
            }

            if (s.StatusCode.ToString() == "OK")
            {

                String script = GetLogonScript(s.Content.ReadAsStringAsync().Result);
                //   MessageBox.Show(script + "script in connect");
                /*   try
                   {
                       connectPrinterShare(s.Content.ReadAsStringAsync().Result);
                   }
                   catch {
                       MessageBox.Show("Printserver unavailable");
                   }*/
                //MessageBox.Show("connected Printer Share");
                runScript(script);
              /*    try
              {
                    disconnectPrinterShare();
                }
                catch
                {

                }*/

            }
            else if (s.StatusCode.ToString() == "Unauthorized")
            {

                pw.Clear();
                String caption = "Fehler";
                MessageBox.Show("Passwort falsch!", caption);
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

            HttpResponseMessage res = client.PostAsync("https://admin."+domain+"/api/sessions/login", content).Result;
            var token = res.Content.ReadAsStringAsync().Result;


            Console.WriteLine("just token: " + token);
            Console.WriteLine("res Status: " + res.StatusCode);
            // Console.WriteLine("CodeIs: ", res.Headers);
            return res;

            /*client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            client.Headers[HttpRequestHeader.Accept] = "text/plain";
            var data = "username=" + name + "&password=" + password;

            try
            {
                var res = client.UploadString("https://172.16.0.2/api/sessions/login", "POST", data);
                return res;

            }
            catch (WebException e){
                MessageBox.Show(e.Message);
                Console.WriteLine("Whole message:" + e.ToString());
                Console.WriteLine("Status: " + (int)e.Status);
                return e.Status.ToString();
            }*/


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
