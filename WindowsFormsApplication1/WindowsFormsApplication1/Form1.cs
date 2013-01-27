using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public static volatile int percentOfCoping = 0;
        public static volatile int percentOfDeleting = 0;
        private FileScanner fs;
        private Thread scanner = null;
        private Thread copyrighter = null;
        private Thread deleter = null;
        private List<string[]> foundedFiles = null;

        public Form1()
        {
            InitializeComponent();
            progressBar1.MarqueeAnimationSpeed = 0;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            label2.Text = folderBrowserDialog1.SelectedPath;
        }

        private void button2_Click(object sender, EventArgs e)
        {

            folderBrowserDialog2.ShowDialog();
            label3.Text = folderBrowserDialog2.SelectedPath;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            percentOfCoping = 0;
            percentOfDeleting = 0;
            button3.Enabled = false;
            Scan_files();
        }
        private void Scan_files()
        {
            progressBar1.MarqueeAnimationSpeed = 25;
            progressBar1.Style = ProgressBarStyle.Marquee;
            button3.Enabled = false;
            fs = new FileScanner(label2.Text);
            scanner = new Thread(fs.scanFiles);
            scanner.Start();
        }
        private void Copy_files()
        {
            int i = 1;
            int g = 1;
            for (int j = 0; j < foundedFiles.Count; j++)
            {
                try
                {
                    File.Copy(foundedFiles[j][1], label3.Text + "\\" + foundedFiles[j][0]);
                }
                catch (Exception exc)
                {
                    File.Copy(foundedFiles[j][1], label3.Text + "\\(" + i + ")" + g +")" +foundedFiles[j][0]);
                    i++;
                }
                Form1.percentOfCoping = (j*100) / foundedFiles.Count;
            }
        } 

        private void Deduplicate_Files()
        {
            percentOfCoping = 100;
            string[] fs1 = Directory.GetFiles(label3.Text);
            for (int i = 0; i < fs1.Length; i++)
            {
                string file1 = fs1[i];
                for (int j = 0; j < fs1.Length; j++)
                {
                    string file2 = fs1[j];
                    if (File.Exists(file1) && File.Exists(file2))
                    {
                            if (!file1.Equals(file2))
                            {
                                string md1 = getMd5(file1);
                                string md2 = getMd5(file2);
                                if (md1.Equals(md2))
                                {
                                    File.Delete(file2);

                                }
                            }
                    }
                }
                Form1.percentOfDeleting = (i * 100) / fs1.Length;
            }
            percentOfDeleting = 100;
        }
        private string getMd5(string path)
        {
            long miliseconds = DateTime.Now.Millisecond;
            FileStream fs = new FileStream(path, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = md5.ComputeHash(fs);
            fs.Close();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            Debug.WriteLine(DateTime.Now.Millisecond - miliseconds);
            return sb.ToString();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            progressBar2.Value = percentOfCoping;
            progressBar3.Value = percentOfDeleting;
            if (scanner != null && !scanner.IsAlive)
            {

                progressBar1.Style = ProgressBarStyle.Blocks;
                progressBar1.Value = 100;
                foundedFiles = fs.getFiles();
                scanner = null;
                fs = null;
                copyrighter = new Thread(this.Copy_files);
                copyrighter.Start();
            }
            if (copyrighter != null && !copyrighter.IsAlive)
            {
                copyrighter = null;
                deleter = new Thread(this.Deduplicate_Files);
                deleter.Start();
            }
            if (deleter != null && !deleter.IsAlive)
            {
                deleter = null;
                button3.Enabled = true;
            }
        }
    }

    public class FileScanner
    {
        //Место, где будут храниться файлы.
        //Это могила.
        private List<string[]> scannedFiles;
        private string pathToScan;

        public FileScanner(string pathToScan)
        {
            scannedFiles = new List<string[]>();
            this.pathToScan = pathToScan;
        }

        public void scanFiles()
        {
            string[] allFilesS = Directory.GetFiles(pathToScan);
            if (allFilesS.Length != 0)
            {
                foreach (string file in allFilesS)
                {
                    FileInfo fi = new FileInfo(file);
                    string name = fi.Name;
                    string fullPath = fi.FullName;
                    string[] fileInfo = new string[] { name, fullPath};
                    scannedFiles.Add(fileInfo);
                }
            }
            string[] st = Directory.GetDirectories(pathToScan);
            for (int i = 0; i < st.Length; i++)
            {
                string[] DirectoriesInCurrentFolder = Directory.GetDirectories(st[i]);
                string[] allFiles = Directory.GetFiles(st[i]);
                if (allFiles.Length != 0)
                {
                    foreach (string file in allFiles)
                    {
                        FileInfo fi = new FileInfo(file);
                        string name = fi.Name;
                        string fullPath = fi.FullName;
                        string[] fileInfo = new string[] { name, fullPath};
                        scannedFiles.Add(fileInfo);
                    }
                }
                if (DirectoriesInCurrentFolder.Length != 0)
                {
                    pathToScan = st[i];
                    scanFiles();
                }
            }
        }
        public List<string[]> getFiles()
        {
            return scannedFiles;
        }
    }
}
