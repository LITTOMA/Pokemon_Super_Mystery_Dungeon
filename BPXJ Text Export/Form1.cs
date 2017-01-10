using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace msgtool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Export(string[] paths)
        {
            StringBuilder promblemFiles = new StringBuilder();
            foreach (string path in paths)
            {
                try
                {
                    var rFileName = GetRawFileName(path);
                    string scpPath = string.Format("{0}/{1}.lua", TB_SCP.Text, rFileName);
                    string outPath = string.Format("{0}/{1}.txt", TB_OUT.Text, rFileName);
                    BinaryText bt = new BinaryText(path);
                    PlainText pt = new PlainText(bt, scpPath);
                    pt.ToFile(outPath, checkBox1.Checked);
                }
                catch
                {
                    promblemFiles.AppendLine(path);
                    continue;
                }
            }
            MessageBox.Show(promblemFiles.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private static string GetRawFileName(string path)
        {
            return Path.GetFileNameWithoutExtension(Path.GetFileName(path));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "选择输出目录";
            var dr = dialog.ShowDialog();
            TB_OUT.Text = dialog.SelectedPath;
            if ((dr == DialogResult.Cancel) || (string.IsNullOrEmpty(TB_OUT.Text)))
            {
                button1_Click(sender, e);
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "选择脚本目录";
            var dr = dialog.ShowDialog();
            TB_SCP.Text = dialog.SelectedPath;
            if ((dr == DialogResult.Cancel) || (string.IsNullOrEmpty(TB_SCP.Text)))
            {
                button3_Click(sender, e);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "二进制文件|*.bin";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(TB_OUT.Text))
                {
                    button1_Click(sender, e);
                }
                if (string.IsNullOrEmpty(TB_SCP.Text))
                {
                    button3_Click(sender, e);
                }
                Export(dialog.FileNames);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("config.ini"))
            {
                Regex regex = new Regex("(.+)=(.+)\r\n");
                MatchCollection mc = regex.Matches(File.ReadAllText("config.ini"));
                List<Match> ml = new List<Match>();
                foreach (Match m in mc)
                {
                    ml.Add(m);
                }
                TB_SCP.Text = ml.Find(m => m.Groups[1].Value == "ScriptPath").Groups[2].Value;
                TB_OUT.Text = ml.Find(m => m.Groups[1].Value == "OutPath").Groups[2].Value;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            using (StreamWriter writer = File.CreateText("config.ini"))
            {
                writer.WriteLine("ScriptPath={0}", TB_SCP.Text);
                writer.WriteLine("OutPath={0}", TB_OUT.Text);
            }
        }
    }
}
