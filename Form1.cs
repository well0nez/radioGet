using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace RadioGet
{
    public partial class Form1 : Form
    {
        public bool seen = false;
        public Process p;
        public int last_message = 0;
        public int last_message2 = 0;
        public int sent_mic_data = 0;
        public int running = 1;
        public ProcessStartInfo psi;
        public bool isOnline = false;
        public Task task;
        public bool ready = false;
        public int ffplay_pid = 0;
        public System.Threading.Timer t;
        public System.Threading.Timer t2;
        private System.Timers.Timer aTimer;
        private System.Timers.Timer aTimer2;

        public Form1()
        {
            InitializeComponent();
            Shown += Form1_Shown;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            PTT.MouseDown += new MouseEventHandler(button1_MouseDown);
            PTT.MouseUp += new MouseEventHandler(button1_MouseUp);

            aTimer = new System.Timers.Timer();
            aTimer2 = new System.Timers.Timer();
            aTimer.Interval = 2000;
            aTimer2.Interval = 2000;
            aTimer.Elapsed += Ping_process;
            aTimer2.Elapsed += Get_voltage;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            aTimer2.AutoReset = true;
            aTimer2.Enabled = true;

            task = Task.Run((Action)Recieve_speaker);
           if (String.IsNullOrEmpty(Properties.Settings.Default.Audio))
           {
               MessageBox.Show("Please set mic device in settings!");
           }
            else
            {
                ready = true;
            }
            Thread.Sleep(1000);
            Lautstaerke();
            try
            {
                WebRequest request = WebRequest.Create("http://192.168.219.5/get_channel"); // call GPIO PORT to CLOSE
                request.Credentials = CredentialCache.DefaultCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string strResponse = reader.ReadToEnd();
                response.Close();
                MethodInvoker inv = delegate
                {
                    label8.Text = strResponse;
                };
                Invoke(inv);
            } catch(Exception)
            {
                MessageBox.Show("Cant fetch channel informations, is the api running?");
            }
            Application.DoEvents();
        }

        private void Get_voltage(Object source, System.Timers.ElapsedEventArgs e)
        {
            aTimer2.Interval = 60000;
            try
            {
                WebRequest request = WebRequest.Create("http://192.168.219.5/voltage");
                request.Credentials = CredentialCache.DefaultCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string strResponse = reader.ReadToEnd();
                response.Close();
                MethodInvoker inv = delegate
                {
                    label9.Text = "V: " + strResponse;
                };
                Invoke(inv);
                Application.DoEvents();
            }
            catch (Exception)
            {
                MessageBox.Show("Cant fetch voltage information, is the api running?");
            }
        }

        public void Recieve_speaker()
        {
            Process p2 = new Process();
            ProcessStartInfo info2 = p2.StartInfo;
            info2.FileName = "taskkill";
            info2.Arguments = " /F /IM ffplay.exe";
            info2.CreateNoWindow = true;
            info2.UseShellExecute = false;
            p2.Start();
            Thread.Sleep(1000);


            Process p = new Process();
            ProcessStartInfo info = p.StartInfo;
            info.FileName = @AppDomain.CurrentDomain.BaseDirectory + @"Resources\ffplay.exe";
            //"highpass = f = 350, lowpass = f = 18000\"";
            info.Arguments = " -probesize 32 -acodec libfdk_aac -nodisp -autoexit -protocol_whitelist file,udp,rtp -fflags nobuffer -flags low_delay -reorder_queue_size 0  -i " + AppDomain.CurrentDomain.BaseDirectory + "\\Resources\\x.sdp";
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardError = true;
            p.ErrorDataReceived += new DataReceivedEventHandler(Ffmpeg_data2);
            p.Start();
            p.BeginErrorReadLine();
            p.WaitForExit();

        }

        public void Ping_process(Object source, System.Timers.ElapsedEventArgs e)
        {
            aTimer.Interval = 5000;
            Ping pinger = new Ping();
            PingReply reply = pinger.Send("192.168.219.5");
            bool pingable = reply.Status == IPStatus.Success;
            if(pingable == false)
            {
                Thread.Sleep(2000);
                reply = pinger.Send("192.168.219.5");
                pingable = reply.Status == IPStatus.Success;
            }
            try
            {
                MethodInvoker inv = delegate
                    {
                        if (pingable && ready)
                        {
                            isOnline = true;
                            PTT.Enabled = true;
                            button2.Enabled = true;
                            button3.Enabled = true;
                            label1.BackColor = Color.Green;
                            label1.Text = "ON";
                        }
                        else
                        {
                            isOnline = false;
                            PTT.Enabled = false;
                            button2.Enabled = false;
                            button3.Enabled = false;
                            label1.BackColor = Color.Red;
                            label1.Text = "OFF";
                        }
                    };
                Invoke(inv);
                Application.DoEvents();
            } catch(Exception)
            {

            }
        }


        private void button1_MouseDown(object sender, MouseEventArgs e)
        { 
            if (seen == false)
            {
                seen = true;

                p = new Process();
                ProcessStartInfo info = p.StartInfo;
                info.FileName = @AppDomain.CurrentDomain.BaseDirectory + @"Resources\ffmpeg.exe";
                info.Arguments = " -audio_buffer_size 50 -f dshow -acodec pcm_s16le -i audio=" + Properties.Settings.Default.Audio + " -hide_banner -v error -stats -xerror -analyzeduration 0 -probesize 32 -c:a libfdk_aac -profile:a aac_eld -b:a 64k -flags +global_header -f nut tcp://192.168.219.5:1337/out.aac";
                info.RedirectStandardError = true;
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                p.ErrorDataReceived += new DataReceivedEventHandler(Ffmpeg_data);
                p.Start();
                p.BeginErrorReadLine();

                Thread.Sleep(500);
                PTT.BackColor = Color.Red;
                Application.DoEvents();
            }
        }


        void Ffmpeg_data(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                string pattern = @"size=\s*(\d+)(kB|mB|b)";
                // Create a Regex  
                Regex r = new Regex(pattern);
                Match m = r.Match(e.Data);
                if (m.Success)
                {
                    last_message = Int32.Parse(m.Groups[1].Value);
                }
                try
                {
                    MethodInvoker inv = delegate
                    {
                        label6.Text = "Outgoing: " + e.Data;
                    };
                    Invoke(inv);
                    Application.DoEvents();
                } catch(Exception)
                {

                }
            }
        }

        void Ffmpeg_data2(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                string pattern = @"size=\s*(\d+)(kB|mB|b)";
                // Create a Regex  
                Regex r = new Regex(pattern);
                Match m = r.Match(e.Data);
                if (m.Success)
                {
                    last_message2 = Int32.Parse(m.Groups[1].Value);
                }
                try
                {
                    MethodInvoker inv = delegate
                {
                    label2.Text = "Incoming: " + e.Data;
                };
                    Invoke(inv);
                    Application.DoEvents();
                }
                catch (Exception) { 
                }
            }
        }

        private void button1_MouseUp(object sender, MouseEventArgs e)
        {
            if (seen == true)
            {
                Thread.Sleep(500);
                PTT.BackColor = default(Color);
                PTT.Enabled = true;
                running = 0;
                seen = false;
                try
                {
                    p.Kill();
                } catch(InvalidOperationException)
                {
                    MessageBox.Show("FFmpeg was killed unexpected");
                }
                sent_mic_data += last_message;
                Application.DoEvents();
                Update_total_label();
            }
        }

        private void Update_total_label()
        {
            try
            {
                MethodInvoker inv = delegate
                {
                    label4.Text = "Bandwidth: " + sent_mic_data.ToString() + "kB";
                    label6.Text = "Outgoing:";
                };
                Invoke(inv);
                Application.DoEvents();
            } catch(Exception)
            {

            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                WebRequest request = WebRequest.Create("http://192.168.219.5/up"); // call GPIO PORT to CLOSE
                request.Credentials = CredentialCache.DefaultCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string strResponse = reader.ReadToEnd();
                response.Close();
                MethodInvoker inv = delegate
                {
                    label8.Text = strResponse;
                };
                Invoke(inv);
                Application.DoEvents();
            }catch(Exception)
            {
                MessageBox.Show("Cant fetch voltage information, is the api running?");
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();  
            f2.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Lautstaerke();
            DialogResult dialogResult = MessageBox.Show("Sure?", "Rebooting?", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                WebRequest request = WebRequest.Create("http://192.168.219.5/reboot"); // call GPIO PORT to CLOSE
                request.Credentials = CredentialCache.DefaultCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string strResponse = reader.ReadToEnd();
                response.Close();
            }
        }

        public void Lautstaerke()
        {
            foreach (var process in Process.GetProcesses())
            {
                if (ffplay_pid == 0)
                {
                    if (process.ProcessName == "ffplay")
                    {
                        ffplay_pid = process.Id;
                        break;
                    }
                }
            }
            VolumeMixer.SetApplicationVolume(ffplay_pid, 50f);    
        }

        private void trackBar1_Scroll(object sender, EventArgs e) {

    //        MessageBox.Show(trackBar1.Value.ToString());
            VolumeMixer.SetApplicationVolume(ffplay_pid, (float)(trackBar1.Value*10));

        }

        private void label8DoubleClick(object sender, EventArgs e)
        {

            System.Drawing.Size size = new System.Drawing.Size(100, 70);
            Form inputBox = new Form();
            inputBox.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            inputBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            inputBox.MaximizeBox = false;
            inputBox.MinimizeBox = false;
            inputBox.ClientSize = size;
            inputBox.Text = "Kanal";

            System.Windows.Forms.TextBox textBox = new TextBox();
            textBox.Text = this.label8.Text;
            textBox.Size = new System.Drawing.Size(size.Width - 10, 10);
            textBox.Location = new System.Drawing.Point(5, 5);
            inputBox.Controls.Add(textBox);

            Button okButton = new Button();
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.Text = "&OK";
            okButton.Location = new System.Drawing.Point(size.Width - 80 , 39);
            inputBox.Controls.Add(okButton);

            inputBox.AcceptButton = okButton;

            if (inputBox.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    WebRequest request = WebRequest.Create("http://192.168.219.5/set_channel?channel=" + textBox.Text); // call GPIO PORT to CLOSE
                    request.Credentials = CredentialCache.DefaultCredentials;
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string strResponse = reader.ReadToEnd();
                    response.Close();

                    MethodInvoker inv = delegate
                    {
                        label8.Text = textBox.Text;
                    };
                    Invoke(inv);
                    Application.DoEvents();
                }
                catch
                {

                }
            }
        }
    }
   
}
