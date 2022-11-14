using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;

namespace HYESOFT.EDI.WebApi.Common
{
    public class WriteLog
    {
        private static object _lock = new object();//类调用  加锁
        public static WriteLog _WriteLog;

        /// <summary>
        /// 写日志到文本文件
        /// </summary>
        /// <param name="actionName">方法名称</param>
        /// <param name="strMessage">日志主体内容</param>
        /// <param name="fileName">写入日志的文件夹的名称</param>
        /// <param name="strStackTrace">堆栈信息内容</param>
        private static void LogResult(string actionName, string strMessage, string logFileName = "", string strStackTrace = "")
        {
            try
            {
                string path = "";
                if (string.IsNullOrEmpty(path))
                {
                    path = System.AppDomain.CurrentDomain.BaseDirectory + ("log");
                }
                else
                {
                    path = System.AppDomain.CurrentDomain.BaseDirectory + ("log") + "\\" + logFileName;
                }
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                path = path + "\\" + DateTime.Now.ToString("yyyy-MM-dd").Replace(":", "") + ".txt";
                //组装打印文本
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("Time(时间):    " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffff") + "\r\n");
                stringBuilder.Append("Action(方法名称):    " + actionName + "\r\n");
                stringBuilder.Append("Message(消息主体)：   " + strMessage + "\r\n");
                stringBuilder.Append("StackTrace(堆栈信息):    " + strStackTrace + "\r\n");
                stringBuilder.Append("-----------------------------------------------------------\r\n\r\n");

                FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter fs = new StreamWriter(stream, Encoding.Default);
                fs.WriteLine(stringBuilder.ToString());
                fs.WriteLine();
                fs.Close();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        /// <summary>
        /// 写日志到文本文件
        /// </summary>
        /// <param name="actionName">方法名称</param>
        /// <param name="strMessage">日志主体内容</param>
        /// <param name="fileName">写入日志的文件夹的名称</param>
        /// <param name="strStackTrace">堆栈信息内容</param>
        public static void WriteLogMessage(string actionName, string strMessage, string logFileName = "", string strStackTrace = "")
        {
            lock (_lock)
            {
                LogResult(actionName, strMessage, logFileName, strStackTrace);
            }
        }
    }
}
