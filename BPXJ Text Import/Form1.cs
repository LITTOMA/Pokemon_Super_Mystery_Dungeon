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

        private void ImportFiles(string[] paths)
        {
            StringBuilder promblemFiles = new StringBuilder();
            foreach (var path in paths)
            {
                try
                {
                    var rFileName = GetRawFileName(path);
                    var binPath = string.Format("{0}/{1}.bin", TB_Bin.Text, rFileName);
                    var outPath = string.Format("{0}/{1}.bin", TB_Out.Text, rFileName);
                    PlainText pt = new PlainText(path);
                    BinaryText bt = null;
                    if (File.Exists(binPath))
                    {
                        bt = new BinaryText(binPath);
                        bt.Import(pt);
                    }
                    else
                    {
                        bt = new BinaryText(pt);
                    }
                    bt.ToFile(outPath);
                }
                catch
                {
                    promblemFiles.AppendLine(path);
                    continue;
                }
            }
            MessageBox.Show(promblemFiles.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private string GetRawFileName(string path)
        {
            return Path.GetFileNameWithoutExtension(Path.GetFileName(path));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "选择原版二进制文件目录";
            var dr = dialog.ShowDialog();
            TB_Bin.Text = dialog.SelectedPath;
            if ((dr == DialogResult.Cancel) || (string.IsNullOrEmpty(TB_Bin.Text)))
            {
                button1_Click(sender, e);
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "选择输出目录";
            var dr = dialog.ShowDialog();
            TB_Out.Text = dialog.SelectedPath;
            if ((dr == DialogResult.Cancel) || (string.IsNullOrEmpty(TB_Out.Text)))
            {
                button3_Click(sender, e);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "文本文件|*.txt";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(TB_Bin.Text))
                {
                    button1_Click(sender, e);
                }
                if (string.IsNullOrEmpty(TB_Out.Text))
                {
                    button3_Click(sender, e);
                }
                ImportFiles(dialog.FileNames);
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
                TB_Bin.Text = ml.Find(m => m.Groups[1].Value == "BinPath").Groups[2].Value;
                TB_Out.Text = ml.Find(m => m.Groups[1].Value == "OutPath").Groups[2].Value;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            using(StreamWriter writer = File.CreateText("config.ini"))
            {
                writer.WriteLine("BinPath={0}", TB_Bin.Text);
                writer.WriteLine("OutPath={0}", TB_Out.Text);
            }
        }
    }
}
