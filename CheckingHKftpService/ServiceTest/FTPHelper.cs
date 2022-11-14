using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ImportDataServiceTest
{
    public class FTPHelper
    {
        //private static string FTPCONSTR = "ftp://10.214.26.69:21/";//FTP的服务器地址，格式为ftp://192.168.1.234:8021/。ip地址和端口换成自己的，这些建议写在配置文件中，方便修改
        //private static string FTPUSERNAME = "whftp";//FTP服务器的用户名
        //private static string FTPPASSWORD = "Yw+ij1]mYH~.N";//FTP服务器的密码

        private static string FTPCONSTR = "ftp://localhost:21/";
        private static string FTPUSERNAME = "";//FTP服务器的用户名
        private static string FTPPASSWORD = "";//FTP服务器的密码

        #region 本地文件上传到FTP服务器
        /// <summary>
        /// 上传文件到远程ftp
        /// </summary>
        /// <param name="path">本地的文件目录</param>
        /// <param name="name">文件名称</param>
        /// <returns></returns>
        public static bool UploadFile(string path, string name)
        {
            string erroinfo = "";
            FileInfo f = new FileInfo(path);
            path = path.Replace("\\", "/");
            path = FTPCONSTR + "/data/uploadFile/photo/" + name;//这个路径是我要传到ftp目录下的这个目录下
            FtpWebRequest reqFtp = (FtpWebRequest)FtpWebRequest.Create(new Uri(path));
            reqFtp.UseBinary = true;
            reqFtp.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
            reqFtp.KeepAlive = false;
            reqFtp.Method = WebRequestMethods.Ftp.UploadFile;
            reqFtp.ContentLength = f.Length;
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;
            FileStream fs = f.OpenRead();
            try
            {
                Stream strm = reqFtp.GetRequestStream();
                contentLen = fs.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                }
                strm.Close();
                fs.Close();
                erroinfo = "完成";
                return true;
            }
            catch (Exception ex)
            {
                erroinfo = string.Format("因{0},无法完成上传", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 上传文件到远程ftp
        /// </summary>
        /// <param name="ftpPath">ftp上的文件路径</param>
        /// <param name="path">本地的文件目录</param>
        /// <param name="id">文件名</param>
        /// <returns></returns>
        public static bool UploadFile(string ftpPath, string path, string id)
        {
            string erroinfo = "";
            FileInfo f = new FileInfo(path);
            path = path.Replace("\\", "/");
            bool b = MakeDir(ftpPath);
            if (b == false)
            {
                return false;
            }
            path = FTPCONSTR + ftpPath + id;
            FtpWebRequest reqFtp = (FtpWebRequest)FtpWebRequest.Create(new Uri(path));
            reqFtp.UseBinary = true;
            reqFtp.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
            reqFtp.KeepAlive = false;
            reqFtp.Method = WebRequestMethods.Ftp.UploadFile;
            reqFtp.ContentLength = f.Length;
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;
            FileStream fs = f.OpenRead();
            try
            {
                Stream strm = reqFtp.GetRequestStream();
                contentLen = fs.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                }
                strm.Close();
                fs.Close();
                erroinfo = "完成";
                return true;
            }
            catch (Exception ex)
            {
                erroinfo = string.Format("因{0},无法完成上传", ex.Message);
                return false;
            }
        }

        ////上面的代码实现了从ftp服务器下载文件的功能
        public static Stream Download(string ftpfilepath)
        {
            Stream ftpStream = null;
            FtpWebResponse response = null;
            try
            {
                ftpfilepath = ftpfilepath.Replace("\\", "/");
                string url = FTPCONSTR + ftpfilepath;
                FtpWebRequest reqFtp = (FtpWebRequest)FtpWebRequest.Create(new Uri(url));
                reqFtp.UseBinary = true;
                reqFtp.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
                response = (FtpWebResponse)reqFtp.GetResponse();
                ftpStream = response.GetResponseStream();
            }
            catch (Exception ee)
            {
                if (response != null)
                {
                    response.Close();
                }
            }
            return ftpStream;
        }
        #endregion

        #region 从ftp服务器下载文件

        /// <summary>
        /// 从ftp服务器下载文件的功能
        /// </summary>
        /// <param name="ftpfilepath">ftp下载的地址</param>
        /// <param name="filePath">存放到本地的路径</param>
        /// <param name="fileName">保存的文件名称</param>
        /// <returns></returns>
        public static bool Download(string ftpfilepath, string filePath, string fileName, ref int isExis)
        {
            try
            {
                //filePath = filePath.Replace("我的电脑\\", "");
                String onlyFileName = Path.GetFileName(fileName);
                string newFileName = filePath + onlyFileName;
                if (File.Exists(newFileName))
                {
                    //errorinfo = string.Format("本地文件{0}已存在,无法下载", newFileName);                   
                    //File.Delete(newFileName);
                    

                    isExis = 1;
                    return false;// update by felix 2021-07-06  文件已经存在的情况下，跳过。
                }
                ftpfilepath = ftpfilepath.Replace("\\", "/") + "/" + fileName;
                string url = FTPCONSTR + ftpfilepath;
                FtpWebRequest reqFtp = (FtpWebRequest)FtpWebRequest.Create(new Uri(url));
                reqFtp.UseBinary = true;
                reqFtp.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
                FtpWebResponse response = (FtpWebResponse)reqFtp.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                long cl = response.ContentLength;
                int bufferSize = 2048;
                int readCount;
                byte[] buffer = new byte[bufferSize];
                readCount = ftpStream.Read(buffer, 0, bufferSize);
                FileStream outputStream = new FileStream(newFileName, FileMode.Create);
                while (readCount > 0)
                {
                    outputStream.Write(buffer, 0, readCount);
                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                }
                ftpStream.Close();
                outputStream.Close();
                response.Close();
                return true;
            }
            catch (Exception ex)
            {
                //errorinfo = string.Format("因{0},无法下载", ex.Message);
                return false;
            }
        }

        ///// <summary>
        ///// 读取文件目录下所有的文件名称，包括文件夹名称
        ///// </summary>
        ///// <param name="ftpAdd">传过来的文件夹路径</param>
        ///// <returns>返回的文件或文件夹名称</returns>
        //public static string[] GetFtpFileList(string ftpAdd)
        //{

        //    string url = FTPCONSTR + ftpAdd;
        //    FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(new Uri(url));
        //    ftpRequest.UseBinary = true;
        //    ftpRequest.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
        //    if (ftpRequest != null)
        //    {
        //        StringBuilder fileListBuilder = new StringBuilder();
        //        //ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;//该方法可以得到文件名称的详细资源，包括修改时间、类型等这些属性
        //        ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;//只得到文件或文件夹的名称
        //        try
        //        {

        //            WebResponse ftpResponse = ftpRequest.GetResponse();
        //            StreamReader ftpFileListReader = new StreamReader(ftpResponse.GetResponseStream(), Encoding.Default);

        //            string line = ftpFileListReader.ReadLine();
        //            while (line != null)
        //            {
        //                fileListBuilder.Append(line);
        //                fileListBuilder.Append("@");//每个文件名称之间用@符号隔开，便于前端调用的时候解析
        //                line = ftpFileListReader.ReadLine();
        //            }
        //            ftpFileListReader.Close();
        //            ftpResponse.Close();
        //            fileListBuilder.Remove(fileListBuilder.ToString().LastIndexOf("@"), 1);
        //            return fileListBuilder.ToString().Split('@');//返回得到的数组
        //        }
        //        catch (Exception ex)
        //        {
        //            return null;
        //        }
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        /// <summary>
        /// 读取文件目录下所有的文件名称，包括文件夹名称
        /// </summary>
        /// <param name="ftpAdd">传过来的文件夹路径</param>
        /// <returns>返回的文件或文件夹名称</returns>
        public static List<string> GetFtpFileList(string ftpAdd)
        {

            string url = FTPCONSTR + ftpAdd;
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(new Uri(url));
            ftpRequest.UseBinary = true;
            ftpRequest.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
            List<string> fileNameList = new List<string>();
            if (ftpRequest != null)
            {
                StringBuilder fileListBuilder = new StringBuilder();
                //ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;//该方法可以得到文件名称的详细资源，包括修改时间、类型等这些属性
                //ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;//只得到文件或文件夹的名称
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                try
                {

                    WebResponse ftpResponse = ftpRequest.GetResponse();
                    StreamReader ftpFileListReader = new StreamReader(ftpResponse.GetResponseStream(), Encoding.Default);

                    string line = ftpFileListReader.ReadLine();
                    while (line != null)
                    {
                        if (line.Contains('/'))
                        {
                            string[] lines = line.Split('/');
                            if (!lines[1].Contains('A'))
                            {
                                fileNameList.Add(lines[1]);
                            }
                        }
                        else
                        {
                            if (!line.Contains('A'))
                            {
                                fileNameList.Add(line);
                            }
                        }
                        fileListBuilder.Append(line);
                        fileListBuilder.Append("@");//每个文件名称之间用@符号隔开，便于前端调用的时候解析
                        line = ftpFileListReader.ReadLine();

                    }
                    ftpFileListReader.Close();
                    ftpResponse.Close();
                    //fileListBuilder.Remove(fileListBuilder.ToString().LastIndexOf("@"), 1);
                    //return fileListBuilder.ToString().Split('@');//返回得到的数组
                    return fileNameList;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region 获得文件的大小
        /// <summary>
        /// 获得文件大小
        /// </summary>
        /// <param name="url">FTP文件的完全路径</param>
        /// <returns></returns>
        public static long GetFileSize(string url)
        {

            long fileSize = 0;
            try
            {
                FtpWebRequest reqFtp = (FtpWebRequest)FtpWebRequest.Create(new Uri(url));
                reqFtp.UseBinary = true;
                reqFtp.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
                reqFtp.Method = WebRequestMethods.Ftp.GetFileSize;
                FtpWebResponse response = (FtpWebResponse)reqFtp.GetResponse();
                fileSize = response.ContentLength;

                response.Close();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            return fileSize;
        }
        #endregion

        #region 在ftp服务器上创建文件目录

        /// <summary>
        ///在ftp服务器上创建文件目录
        /// </summary>
        /// <param name="dirName">文件目录</param>
        /// <returns></returns>
        public static bool MakeDir(string dirName)
        {
            try
            {
                bool b = RemoteFtpDirExists(dirName);
                if (b)
                {
                    return true;
                }
                string url = FTPCONSTR + dirName;
                FtpWebRequest reqFtp = (FtpWebRequest)FtpWebRequest.Create(new Uri(url));
                reqFtp.UseBinary = true;
                // reqFtp.KeepAlive = false;
                reqFtp.Method = WebRequestMethods.Ftp.MakeDirectory;
                reqFtp.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
                FtpWebResponse response = (FtpWebResponse)reqFtp.GetResponse();
                response.Close();
                return true;
            }
            catch (Exception ex)
            {
                //errorinfo = string.Format("因{0},无法下载", ex.Message);
                return false;
            }

        }
        /// <summary>
        /// 判断ftp上的文件目录是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool RemoteFtpDirExists(string path)
        {

            path = FTPCONSTR + path;
            FtpWebRequest reqFtp = (FtpWebRequest)FtpWebRequest.Create(new Uri(path));
            reqFtp.UseBinary = true;
            reqFtp.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
            reqFtp.Method = WebRequestMethods.Ftp.ListDirectory;
            FtpWebResponse resFtp = null;
            try
            {
                resFtp = (FtpWebResponse)reqFtp.GetResponse();
                FtpStatusCode code = resFtp.StatusCode;//OpeningData
                resFtp.Close();
                return true;
            }
            catch
            {
                if (resFtp != null)
                {
                    resFtp.Close();
                }
                return false;
            }
        }
        #endregion

        #region 从ftp服务器删除文件的功能
        /// <summary>
        /// 从ftp服务器删除文件的功能
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool DeleteFile(string fileName)
        {
            try
            {
                string url = FTPCONSTR + fileName;
                FtpWebRequest reqFtp = (FtpWebRequest)FtpWebRequest.Create(new Uri(url));
                reqFtp.UseBinary = true;
                reqFtp.KeepAlive = false;
                reqFtp.Method = WebRequestMethods.Ftp.DeleteFile;
                reqFtp.Credentials = new NetworkCredential(FTPUSERNAME, FTPPASSWORD);
                FtpWebResponse response = (FtpWebResponse)reqFtp.GetResponse();
                response.Close();
                return true;
            }
            catch (Exception ex)
            {
                //errorinfo = string.Format("因{0},无法下载", ex.Message);
                return false;
            }
        }
        #endregion

        #region 读取CSV文件
        /// <summary>
        /// 读取CSV文件
        /// </summary>
        /// <param name="mycsvdt"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static bool OpenCSVFile(ref DataTable mycsvdt, string filepath)
        {
            string strpath = filepath; //csv文件的路径
            try
            {
                int intColCount = 0;
                bool blnFlag = true;

                DataColumn mydc;
                DataRow mydr;

                string strline;
                string[] aryline;
                StreamReader mysr = new StreamReader(strpath, System.Text.Encoding.Default);

                while ((strline = mysr.ReadLine()) != null)
                {
                    aryline = strline.Split(new char[] { ',' });

                    //给datatable加上列名
                    if (blnFlag)
                    {
                        blnFlag = false;
                        intColCount = aryline.Length;
                        int col = 0;
                        for (int i = 0; i < aryline.Length; i++)
                        {
                            col = i + 1;
                            mydc = new DataColumn(col.ToString());
                            mycsvdt.Columns.Add(mydc);
                        }
                    }

                    //填充数据并加入到datatable中
                    mydr = mycsvdt.NewRow();
                    for (int i = 0; i < intColCount; i++)
                    {
                        mydr[i] = aryline[i];
                    }
                    mycsvdt.Rows.Add(mydr);
                }
                return true;

            }
            catch (Exception e)
            {
                return false;
            }
        }
        #endregion
    }
}
