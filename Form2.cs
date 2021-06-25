using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RadioGet
{
    public partial class Form2 : Form
    {
        private string ffmpeg_result;

        public Form2()
        {
            InitializeComponent();
            Process p = new Process();
            ProcessStartInfo info = p.StartInfo;
            info.FileName = @AppDomain.CurrentDomain.BaseDirectory + @"Resources\ffmpeg.exe";
            info.Arguments = " -list_devices true -f dshow -i dummy -hide_banner";
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardError = true;
            p.ErrorDataReceived += new DataReceivedEventHandler(ffmpeg_data);
            p.Start();
            p.BeginErrorReadLine();
            p.WaitForExit();
            string pattern = "\".*\"";
            Regex r = new Regex(pattern);
            MatchCollection m = r.Matches(ffmpeg_result);
            ArrayList ComboList = new ArrayList();
            int indexx = 0;
            if (m.Count > 0)
            {
                for (int i = 0; i < m.Count; i += 2)
                {
                    Match x = m[i];
                    Match x2 = m[i + 1];
                    KeyValuePair<string, string> keyval = new KeyValuePair<string, string>(x2.Groups[0].Value, x.Groups[0].Value);
                    if(x2.Groups[0].Value == Properties.Settings.Default.Audio)
                    {
                        indexx =  (i+1)  / 2;
                    }
                    ComboList.Add(keyval);
                }

            }
            comboBox1.DataSource = ComboList;
            comboBox1.DisplayMember = "value";
            comboBox1.ValueMember = "key";
            if(indexx != 0) 
                comboBox1.SelectedIndex = indexx;
        }

        private void ffmpeg_data(object sender, DataReceivedEventArgs e)
        {
            ffmpeg_result += (e.Data +"\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string val = String.Empty;
            try
            {
                val = comboBox1.SelectedValue.ToString();
            }
            catch (System.NullReferenceException)
            {
                    MessageBox.Show("Illegal Choice");
                return;
            }
            if (String.IsNullOrEmpty(val))
             {
                    MessageBox.Show("Illegal Choice");
                return;
            }
            else
            {
                    Properties.Settings.Default.Audio = val;
                    Properties.Settings.Default.Save();
                    this.Close();
             }
        }

    }
}
