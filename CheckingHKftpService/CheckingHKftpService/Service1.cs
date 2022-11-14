﻿using HYESOFT.EDI.Common.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace CheckingHKftpService
{
    class UserProcess
    {
        #region Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public String lpReserved;
            public String lpDesktop;
            public String lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }
        #endregion
        #region Enumerations
        enum TOKEN_TYPE : int
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }
        enum SECURITY_IMPERSONATION_LEVEL : int
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3,
        }
        enum WTSInfoClass
        {
            InitialProgram,
            ApplicationName,
            WorkingDirectory,
            OEMId,
            SessionId,
            UserName,
            WinStationName,
            DomainName,
            ConnectState,
            ClientBuildNumber,
            ClientName,
            ClientDirectory,
            ClientProductId,
            ClientHardwareId,
            ClientAddress,
            ClientDisplay,
            ClientProtocolType
        }
        #endregion
        #region Constants
        public const int TOKEN_DUPLICATE = 0x0002;
        public const uint MAXIMUM_ALLOWED = 0x2000000;
        public const int CREATE_NEW_CONSOLE = 0x00000010;
        public const int IDLE_PRIORITY_CLASS = 0x40;
        public const int NORMAL_PRIORITY_CLASS = 0x20;
        public const int HIGH_PRIORITY_CLASS = 0x80;
        public const int REALTIME_PRIORITY_CLASS = 0x100;
        #endregion
        #region Win32 API Imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);
        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();
        [DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        static extern bool WTSQuerySessionInformation(System.IntPtr hServer, int sessionId, WTSInfoClass wtsInfoClass, out System.IntPtr ppBuffer, out uint pBytesReturned);
        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public extern static bool CreateProcessAsUser(IntPtr hToken, String lpApplicationName, String lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment,
            String lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
        [DllImport("kernel32.dll")]
        static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);
        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        public extern static bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess,
            ref SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType,
            int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
        [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);
        #endregion
        public static string GetCurrentActiveUser()
        {
            IntPtr hServer = IntPtr.Zero, state = IntPtr.Zero;
            uint bCount = 0;
            // obtain the currently active session id; every logged on user in the system has a unique session id  
            uint dwSessionId = WTSGetActiveConsoleSessionId();
            string domain = string.Empty, userName = string.Empty;
            if (WTSQuerySessionInformation(hServer, (int)dwSessionId, WTSInfoClass.DomainName, out state, out bCount))
            {
                domain = Marshal.PtrToStringAuto(state);
            }
            if (WTSQuerySessionInformation(hServer, (int)dwSessionId, WTSInfoClass.UserName, out state, out bCount))
            {
                userName = Marshal.PtrToStringAuto(state);
            }
            return string.Format("{0}\\{1}", domain, userName);
        }
        /// <summary>  
        /// Launches the given application with full admin rights, and in addition bypasses the Vista UAC prompt  
        /// </summary>  
        /// <param name="applicationName">The name of the application to launch</param>  
        /// <param name="procInfo">Process information regarding the launched application that gets returned to the caller</param>  
        /// <returns></returns>  
        public static bool StartProcessAndBypassUAC(String applicationName, String command, out PROCESS_INFORMATION procInfo)
        {
            uint winlogonPid = 0;
            IntPtr hUserTokenDup = IntPtr.Zero, hPToken = IntPtr.Zero, hProcess = IntPtr.Zero;
            procInfo = new PROCESS_INFORMATION();
            // obtain the currently active session id; every logged on user in the system has a unique session id  
            uint dwSessionId = WTSGetActiveConsoleSessionId();
            // obtain the process id of the winlogon process that is running within the currently active session  
            Process[] processes = Process.GetProcessesByName("winlogon");
            foreach (Process p in processes)
            {
                if ((uint)p.SessionId == dwSessionId)
                {
                    winlogonPid = (uint)p.Id;
                }
            }
            // obtain a handle to the winlogon process  
            hProcess = OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);
            // obtain a handle to the access token of the winlogon process  
            if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE, ref hPToken))
            {
                CloseHandle(hProcess);
                return false;
            }
            // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser  
            // I would prefer to not have to use a security attribute variable and to just   
            // simply pass null and inherit (by default) the security attributes  
            // of the existing token. However, in C# structures are value types and therefore  
            // cannot be assigned the null value.  
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);
            // copy the access token of the winlogon process; the newly created token will be a primary token  
            if (!DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
            {
                CloseHandle(hProcess);
                CloseHandle(hPToken);
                return false;
            }
            // By default CreateProcessAsUser creates a process on a non-interactive window station, meaning  
            // the window station has a desktop that is invisible and the process is incapable of receiving  
            // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user   
            // interaction with the new process.  
            STARTUPINFO si = new STARTUPINFO();
            si.cb = (int)Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default"; // interactive window station parameter; basically this indicates that the process created can display a GUI on the desktop  
            // flags that specify the priority and creation method of the process  
            int dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;
            // create a new process in the current user's logon session  
            bool result = CreateProcessAsUser(hUserTokenDup,        // client's access token  
                                            applicationName,        // file to execute  
                                            command,                // command line  
                                            ref sa,                 // pointer to process SECURITY_ATTRIBUTES  
                                            ref sa,                 // pointer to thread SECURITY_ATTRIBUTES  
                                            false,                  // handles are not inheritable  
                                            dwCreationFlags,        // creation flags  
                                            IntPtr.Zero,            // pointer to new environment block   
                                            null,                   // name of current directory   
                                            ref si,                 // pointer to STARTUPINFO structure  
                                            out procInfo            // receives information about new process  
                                            );
            // invalidate the handles  
            CloseHandle(hProcess);
            CloseHandle(hPToken);
            CloseHandle(hUserTokenDup);
            return result; // return the result  
        }
    }
    public partial class Service1 : ServiceBase
    {

        Thread thread1 = null;
        int time = int.Parse(ConfigurationManager.AppSettings["sleepTime"]);
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            thread1 = new Thread(new ThreadStart(Thread1Process));
            thread1.Start();
        }

        private void Thread1Process()
        {
            while (true)
            {
                try
                {
                    CheckingHKFTP();
                }catch(Exception e)
                {
                    LogHelper.Logger.Info("错误：" + e.Message);
                }
                
                Thread.Sleep(1000 * time);
            }
        }


            public void CheckingHKFTP()
        {
            
            string name = ConfigurationManager.AppSettings["name"];
            if (GetPidByProcessName(name) == 0)
            {
                LogHelper.Logger.Info("监控程序重启" + Environment.NewLine);
                //string path = System.Environment.CurrentDirectory;
                //path += @"\" + name + ".exe";
                //Process.Start(path);

                string path = ConfigurationManager.AppSettings["url"];
                //path += @"\" + name + ".exe";
                //Process proc = new Process();
                //proc.StartInfo.FileName = @path+ "\\" + name + ".exe";
                //proc.StartInfo.WorkingDirectory = @path;
                //proc.StartInfo.Arguments = "";
                //proc.StartInfo.UseShellExecute = false;
                //proc.StartInfo.RedirectStandardInput = true;
                //proc.StartInfo.RedirectStandardOutput = false;
                //proc.StartInfo.RedirectStandardError = true;
                //proc.StartInfo.CreateNoWindow = true;
                //proc.Start();

                UserProcess.PROCESS_INFORMATION pInfo = new UserProcess.PROCESS_INFORMATION();
                UserProcess.StartProcessAndBypassUAC(@path + "\\" + name + ".exe", string.Empty, out pInfo);
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

        protected override void OnStop()
        {
            
            thread1.Abort();
            while (thread1.ThreadState != System.Threading.ThreadState.Aborted)
            {
                thread1.Abort();
                Thread.Sleep(1000);
            }
            LogHelper.Logger.Info("服务停止：" + Environment.NewLine);
        }
    }
}
