using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RadioGet
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        /// 
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1());
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Process p = new Process();
            ProcessStartInfo info = p.StartInfo;
            info.FileName = "taskkill";
            info.Arguments = " /F /IM ffmpeg.exe";
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            p.Start();

            Process p2 = new Process();
            ProcessStartInfo info2 = p2.StartInfo;
            info2.FileName = "taskkill";
            info2.Arguments = " /F /IM ffplay.exe";
            info2.CreateNoWindow = true;
            info2.UseShellExecute = false;
            p2.Start();


            try
            {
                WebRequest request = WebRequest.Create("http://192.168.219.5/end"); //make sure we are not sending again
                request.Credentials = CredentialCache.DefaultCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();

                request = WebRequest.Create("http://192.168.219.5/radio_poweroff");  // poweroff the radio
                request.Credentials = CredentialCache.DefaultCredentials;
                response = (HttpWebResponse)request.GetResponse();
                response.Close();

            } catch(Exception)
            {

            }
            // kill all threads here. 
        }
    }
}
