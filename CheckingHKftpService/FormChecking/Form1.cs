using HYESOFT.EDI.Common.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FormChecking
{
    public partial class Form1 : Form
    {
        Thread threadInconnect = null;
        public Form1()
        {
            InitializeComponent();
            //this.Load += new EventHandler(Form1_Load);
            threadInconnect = new Thread(new ThreadStart(ThreadInconnectProcess));
            threadInconnect.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        { }
        //{
        //    threadInconnect = new Thread(new ThreadStart(ThreadInconnectProcess));
        //    threadInconnect.Start();
        //}

        private void Form1_FormClosing(object sender,FormClosingEventArgs e)
        {
            threadInconnect.Abort();
            while (threadInconnect.ThreadState != System.Threading.ThreadState.Aborted)
            {
                threadInconnect.Abort();
                Thread.Sleep(1000);
            }

        }

        private void ThreadInconnectProcess()
        {
            while (true)
            {
                CheckingHKFTP();
                Thread.Sleep(1000 * 5);
            }

        }

        public void CheckingHKFTP()
        {
            //bool isAppRunning = false;
            //System.Threading.Mutex mutex = new System.Threading.Mutex(true, "Quick Easy FTP Server V4.0.0", out isAppRunning);
            //if (!isAppRunning)
            //{
            //    return;
            //}
            string name = ConfigurationManager.AppSettings["name"];
            if (GetPidByProcessName(name) == 0)
            {
                LogHelper.Logger.Info("重启程序" + Environment.NewLine);
                string path = ConfigurationManager.AppSettings["url"];

                //string path = @"C:\HK\Quick Easy FTP Server V4.0.0.exe";
                Process proc = new Process();
                proc.StartInfo.FileName = @path + name + ".exe";
                proc.StartInfo.WorkingDirectory = @path;
                proc.StartInfo.Arguments = "";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = false;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                //Process.Start(path);
            }
        }

        public static int GetPidByProcessName(string processName)
        {
            Process[] arrayProcess = Process.GetProcessesByName(processName);
            foreach (Process p in arrayProcess)
            {
                return p.Id;
            }
            return 0;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }
    }
}
