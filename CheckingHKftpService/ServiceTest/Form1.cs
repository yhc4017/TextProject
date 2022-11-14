using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HYESOFT.EDI.Common.Log;
using HYESOFT.EDI.WebApi.Common;
using ImportDataServiceTest;

namespace ServiceTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string name = ConfigurationManager.AppSettings["name"];//应用名
            if (GetPidByProcessName(name) == 0)//若程序没有运行
            {
                //string path = ConfigurationManager.AppSettings["url"];//应用程序地址
                //Process proc = new Process();

                //proc.StartInfo.WorkingDirectory = @"C:\HK\";
                //proc.StartInfo.FileName = @"C:\HK\Quick Easy FTP Server V4.0.0.exe";
                //proc.StartInfo.Arguments = "";
                //proc.StartInfo.UseShellExecute = false;
                //proc.StartInfo.RedirectStandardInput = true;
                //proc.StartInfo.RedirectStandardOutput = false;
                //proc.StartInfo.RedirectStandardError = true;
                //proc.StartInfo.CreateNoWindow = false;
                //proc.Start();
                string path = System.Environment.CurrentDirectory;
                path += @"\" + name + ".exe";
                Process.Start(path);
                ////System.Diagnostics.Process p = new Process();
                ////p.StartInfo.UseShellExecute = true;
                ////p.StartInfo.FileName = @path + "\\ftp.lnk";
                ////p.Start();
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

        /// <summary>
        /// 合并三单
        /// </summary>
        public void GetDataBindThreeOrder()
        {
            try
            {
                int taskCountSingle = Convert.ToInt32(ConfigurationManager.AppSettings["taskCountSingle"]);
                //step0:先按照 部门和省份筛选订单  到店日期
                //string sql0 = "select DepartId,Province,EtaDate from FromFtp.dbo.InitialDataHye where Flag=1 group by DepartId,Province,EtaDate";
                string sql0 = "select DepartId,Province2,EtaDate from FromFtp.dbo.InitialDataHye where Flag=1 group by DepartId,Province2,EtaDate order by Province2,DepartId";// add by felix 2021-07-09
                DataSet ds0 = DbHelperSQL.Query(sql0);
                if (ds0.Tables[0].Rows.Count > 0)
                {
                    List<string> sqlList1 = new List<string>();
                    List<string> sqlList2 = new List<string>();
                    List<string> sqlList3 = new List<string>();
                    List<string> sqlList4 = new List<string>();
                    List<string> sqlList5 = new List<string>();
                    List<string> sqlAll = new List<string>();
                    for (int ss = 0; ss < ds0.Tables[0].Rows.Count; ss++)
                    {
                        string departIdFelix = ds0.Tables[0].Rows[ss]["DepartId"].ToString();
                        string provinceFelix = ds0.Tables[0].Rows[ss]["Province2"].ToString();
                        string etaDateFelix = ds0.Tables[0].Rows[ss]["EtaDate"].ToString();
                        //step1: 获取指定省份、部门、到店日期的所有订单。
                        string sqlGetOrderNo = $@"select Id,ItemCode,ItemName,Sku1,Sku2,Qty,ShopNo,ShopName,InnerPackSize,WmsOrder,DepartId,Volume,DeliveryDate,EtaDate,
                    Location,Province,Province2 from FromFtp.dbo.InitialDataHye where DepartId='{departIdFelix}' and Province2='{provinceFelix}' and EtaDate='{etaDateFelix}' and  Flag=1";
                        DataSet dsOrderNo = DbHelperSQL.Query(sqlGetOrderNo);
                        List<CompareLocation> compareLocationList = new List<CompareLocation>();//所有待分配订单的订单实体集合
                        if (dsOrderNo.Tables[0].Rows.Count <= 0)
                        {
                            return;
                        }
                        //第一个for循环 获取所有待分配订单及订单明细(组装实体)
                        for (int i = 0; i < dsOrderNo.Tables[0].Rows.Count; i++)
                        {
                            TempDataInfo tempDataInfo = new TempDataInfo();//订单明细数据
                            int Id = Convert.ToInt32(dsOrderNo.Tables[0].Rows[i]["Id"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["Id"].ToString());
                            string orderNo = dsOrderNo.Tables[0].Rows[i]["WmsOrder"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["WmsOrder"].ToString();
                            string location = dsOrderNo.Tables[0].Rows[i]["Location"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["Location"].ToString();
                            string itemCode = dsOrderNo.Tables[0].Rows[i]["ItemCode"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["ItemCode"].ToString();
                            string itemName = dsOrderNo.Tables[0].Rows[i]["ItemName"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["ItemName"].ToString();
                            string sku1 = dsOrderNo.Tables[0].Rows[i]["Sku1"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["Sku1"].ToString();
                            string sku2 = dsOrderNo.Tables[0].Rows[i]["Sku2"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["Sku2"].ToString();
                            int innerPackSize = Convert.ToInt32(dsOrderNo.Tables[0].Rows[i]["InnerPackSize"] == null ? "0" : dsOrderNo.Tables[0].Rows[i]["InnerPackSize"].ToString());
                            int qty = Convert.ToInt32(dsOrderNo.Tables[0].Rows[i]["Qty"] == null ? "0" : dsOrderNo.Tables[0].Rows[i]["Qty"].ToString());

                            string shopNo = dsOrderNo.Tables[0].Rows[i]["ShopNo"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["ShopNo"].ToString();
                            string shopName = dsOrderNo.Tables[0].Rows[i]["ShopName"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["ShopName"].ToString();
                            string deliveryDate = dsOrderNo.Tables[0].Rows[i]["DeliveryDate"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["DeliveryDate"].ToString();
                            string etaDate = dsOrderNo.Tables[0].Rows[i]["EtaDate"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["EtaDate"].ToString();
                            string departId = dsOrderNo.Tables[0].Rows[i]["DepartId"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["DepartId"].ToString();
                            string volume = dsOrderNo.Tables[0].Rows[i]["volume"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["volume"].ToString();
                            string province = dsOrderNo.Tables[0].Rows[i]["Province"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["Province"].ToString();
                            string province2 = dsOrderNo.Tables[0].Rows[i]["Province2"] == null ? "" : dsOrderNo.Tables[0].Rows[i]["Province2"].ToString();

                            tempDataInfo.InnerPackSize = innerPackSize;
                            tempDataInfo.ItemCode = itemCode;
                            tempDataInfo.ItemName = itemName;
                            tempDataInfo.Location = location;
                            tempDataInfo.OrderNo = orderNo;
                            tempDataInfo.Qty = qty;
                            tempDataInfo.Sku1 = sku1;
                            tempDataInfo.Sku2 = sku2;
                            tempDataInfo.SortNo = Id;
                            tempDataInfo.volume = volume;
                            int index = compareLocationList.FindIndex(a => a.OrderNo.Equals(orderNo));
                            /*  判断当前订单是否存在，存在则添加明细和添加路径集合。
                            如果不存在，则说明是首次添加该订单明细则创建订单。 */
                            if (index >= 0)
                            {
                                CompareLocation compareLocation = compareLocationList[index];
                                compareLocation.TempDataInfoList.Add(tempDataInfo);
                                compareLocation.LocationList.Add(location);//添加订单路径集合
                                compareLocation.SumQty += tempDataInfo.Qty;
                            }
                            else
                            {
                                CompareLocation compareLocation = new CompareLocation();
                                List<TempDataInfo> tempDataInfos = new List<TempDataInfo>();
                                tempDataInfos.Add(tempDataInfo);
                                compareLocation.TempDataInfoList = tempDataInfos;
                                compareLocation.OrderNo = orderNo;
                                compareLocation.DeliveryDate = deliveryDate;
                                compareLocation.DepartId = departId;
                                compareLocation.EtaDate = etaDate;
                                compareLocation.ShopName = shopName;
                                compareLocation.ShopNo = shopNo;
                                List<string> locationList = new List<string>();
                                locationList.Add(location);
                                compareLocation.LocationList = locationList;
                                compareLocation.SumQty = qty;
                                compareLocation.Province = province;
                                compareLocation.Province2 = province2;
                                compareLocationList.Add(compareLocation);
                            }
                        }
                        //遍历比较对象集合
                        List<string> emptyList = new List<string>();
                        /* 空比对结果集合（一个订单和所有订单的明细都匹配了一遍，没有一个匹配上的，则放入这个集合中）
                        等最后所有空比对结果两两配对成任务单  */

                        List<ABCOrder> abcOrderList = new List<ABCOrder>();//配对结果结合

                        /* 将所有要匹配的订单按照路径数的多少由高到低排序，这样可以优先将匹配数多的高的订单放在一起组成任务单 */
                        compareLocationList = compareLocationList.OrderByDescending(a => a.LocationList.Count).ToList();

                        //add by felix 2021-09-07
                        List<string> compareFinishedOrderList = new List<string>();//已经比较过的订单集合
                        List<string> taskSingleOrderList = new List<string>();//单独生成订单集合
                        for (int i = 0; i < compareLocationList.Count; i++)
                        {
                            int count = compareLocationList[i].SumQty;
                            string orderNo = compareLocationList[i].OrderNo;
                            compareFinishedOrderList.Add(orderNo);
                            if (count >= taskCountSingle)
                            {
                                //单独成任务单
                                taskSingleOrderList.Add(orderNo);
                                continue;
                            }
                            List<OrderLocationLikeCompare> orderLocationLikeCompareList = new List<OrderLocationLikeCompare>();
                            for (int s = 0; s < compareLocationList.Count; s++)
                            {
                                string orderNoCompare = compareLocationList[s].OrderNo;
                                if (compareFinishedOrderList.Contains(orderNoCompare))
                                {
                                    continue;
                                }
                                int min = (compareLocationList[i].LocationList.Intersect(compareLocationList[s].LocationList)).Count();
                                if (min > 0)
                                {
                                    OrderLocationLikeCompare model = new OrderLocationLikeCompare();
                                    model.OrderNo = orderNoCompare;
                                    model.LocationLikeCount = min;
                                }
                            }
                            orderLocationLikeCompareList = orderLocationLikeCompareList.OrderByDescending(a => a.LocationLikeCount).ToList();
                            if (orderLocationLikeCompareList.Count >= 2)
                            {
                                ABCOrder aBCOrder = new ABCOrder();
                                aBCOrder.AorderNo = orderNo;
                                aBCOrder.BorderNo = orderLocationLikeCompareList[0].OrderNo;
                                aBCOrder.CorderNo = orderLocationLikeCompareList[1].OrderNo;
                                aBCOrder.DepartmentId = departIdFelix;
                                aBCOrder.ProvinceId2 = provinceFelix;
                                abcOrderList.Add(aBCOrder);
                            }
                            else if (orderLocationLikeCompareList.Count == 1)
                            {
                                ABCOrder aBCOrder = new ABCOrder();
                                aBCOrder.AorderNo = orderNo;
                                aBCOrder.BorderNo = orderLocationLikeCompareList[0].OrderNo;
                                aBCOrder.DepartmentId = departIdFelix;
                                aBCOrder.ProvinceId2 = provinceFelix;
                                abcOrderList.Add(aBCOrder);
                            }
                            else
                            {
                                emptyList.Add(orderNo);
                            }
                        }
                        //end add

                        //计算没有匹配上的订单(注意订单总和不是 2 的倍数的情况)
                        if (emptyList.Count < 3)
                        {
                            if (emptyList.Count == 1)
                            {
                                ABCOrder aBCOrder = new ABCOrder();
                                aBCOrder.AorderNo = emptyList[0];
                                aBCOrder.DepartmentId = departIdFelix;
                                aBCOrder.ProvinceId2 = provinceFelix;
                                abcOrderList.Add(aBCOrder);
                            }
                            else if (emptyList.Count != 0)
                            {
                                ABCOrder aBCOrder = new ABCOrder();
                                aBCOrder.AorderNo = emptyList[0];
                                aBCOrder.BorderNo = emptyList[1];
                                aBCOrder.DepartmentId = departIdFelix;
                                aBCOrder.ProvinceId2 = provinceFelix;
                                abcOrderList.Add(aBCOrder);
                            }
                        }
                        else
                        {
                            //三三配对
                            if (emptyList.Count % 3 == 1)
                            {
                                ABCOrder abcOrder = new ABCOrder();
                                abcOrder.AorderNo = emptyList[0];
                                abcOrder.DepartmentId = departIdFelix;
                                abcOrder.ProvinceId2 = provinceFelix;
                                abcOrderList.Add(abcOrder);
                                emptyList.RemoveAt(0);
                            }
                            if (emptyList.Count % 3 == 2)
                            {
                                ABCOrder abcOrder = new ABCOrder();
                                abcOrder.AorderNo = emptyList[0];
                                abcOrder.BorderNo = emptyList[1];
                                abcOrder.DepartmentId = departIdFelix;
                                abcOrder.ProvinceId2 = provinceFelix;
                                abcOrderList.Add(abcOrder);
                                emptyList.RemoveAt(0);
                                emptyList.RemoveAt(0);
                            }
                            for (int i = 0; i < emptyList.Count; i = i + 3)
                            {
                                ABCOrder abcOrder = new ABCOrder();
                                abcOrder.AorderNo = emptyList[i];
                                abcOrder.BorderNo = emptyList[i + 1];
                                abcOrder.CorderNo = emptyList[i + 2];
                                abcOrder.DepartmentId = departIdFelix;
                                abcOrder.ProvinceId2 = provinceFelix;
                                abcOrderList.Add(abcOrder);
                            }
                        }

                        //超过指定数量的订单单独生成任务单
                        for (int i = 0; i < taskSingleOrderList.Count; i++)
                        {
                            ABCOrder aBCOrder = new ABCOrder();
                            aBCOrder.AorderNo = taskSingleOrderList[i];
                            aBCOrder.DepartmentId = departIdFelix;
                            aBCOrder.ProvinceId2 = provinceFelix;
                            abcOrderList.Add(aBCOrder);
                        }




                        //遍历配对结果  CreateDate   OrderCount
                        // step 1:获取编号
                        int StartSn = 0;
                        //string dt = DateTime.Now.ToString("yyyy-MM-dd");
                        string dtTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        //string sqlGetOrderCount = $"select TaskCount from TaskSNCreate where CreateDate='{dt}'";
                        //DataSet dataSet1 = DbHelperSQL.Query(sqlGetOrderCount);
                        //if (dataSet1.Tables[0].Rows.Count > 0)
                        //{
                        //    //StartSn = Convert.ToInt32(dataSet1.Tables[0].Rows[0]["OrderCount"]);
                        //    StartSn = Convert.ToInt32(dataSet1.Tables[0].Rows[0]["TaskCount"]);
                        //}
                        //string.Format("{0:D3}", 2)
                        //第三个for循环  遍历订单组合结果，创建任务单及明细
                        for (int i = 0; i < abcOrderList.Count; i++)
                        {
                            /*涉及到的表
                            * 1. TaskSNCreate  ： 更新当前任务数量
                            * 2. InitialData    :  更新已经配对的数据
                            * 3. Task          :  任务主表
                            * 4. TaskInfo      :  任务订单信息表
                            * 5. TaskLocationInfo  :  任务路径表
                            */
                            //任务主表(1)
                            StartSn = GetSortTaskCode();
                            string taskId = "H" + StartSn;
                            int totalCount = 0;
                            //任务路径表(2)                   更新已经配置的数据(3)               // 任务订单信息表(4)
                            ABCOrder abcOrder = abcOrderList[i];
                            CompareLocation compareLocationA = compareLocationList.Find(a => a.OrderNo.Equals(abcOrder.AorderNo));
                            for (int j = 0; j < compareLocationA.TempDataInfoList.Count; j++)
                            {
                                string sql2 = $@"insert into FromFtp.dbo.TaskLocationInfo(TaskId,OrderNo,ItemCode,ItemName,Volume,Location,Sku1,Sku2,InnerPackSize,Qty,CreateTime)
values('{taskId}', '{abcOrder.AorderNo}', '{compareLocationA.TempDataInfoList[j].ItemCode}', '{compareLocationA.TempDataInfoList[j].ItemName}','{compareLocationA.TempDataInfoList[j].volume}','{compareLocationA.TempDataInfoList[j].Location}',
'{compareLocationA.TempDataInfoList[j].Sku1}', '{compareLocationA.TempDataInfoList[j].Sku2}', '{compareLocationA.TempDataInfoList[j].InnerPackSize}','{compareLocationA.TempDataInfoList[j].Qty}','{dtTime}')";
                                //qutyA += Convert.ToInt32(compareLocationA.SumQty);
                                sqlList2.Add(sql2);
                                string sql3 = $"update FromFtp.dbo.InitialDataHye set Flag=2 where Id='{compareLocationA.TempDataInfoList[j].SortNo}'";
                                sqlList3.Add(sql3);
                            }
                            string sql4 = $@"insert into FromFtp.dbo.TaskInfo(TaskId,OrderNo,Total,OrderStatus,ShopNo,ShopName,CreateTime,DeliveryDate,EtaDate,DepartId,Province)
values('{taskId}','{abcOrder.AorderNo}','{compareLocationA.SumQty}','1','{compareLocationA.ShopNo}','{compareLocationA.ShopName}','{dtTime}','{compareLocationA.DeliveryDate}','{compareLocationA.EtaDate}','{compareLocationA.DepartId}','{compareLocationA.Province}')";
                            sqlList4.Add(sql4);
                            totalCount += compareLocationA.SumQty;
                            if (!string.IsNullOrEmpty(abcOrder.BorderNo))
                            {
                                CompareLocation compareLocationB = compareLocationList.Find(a => a.OrderNo.Equals(abcOrder.BorderNo));
                                for (int s = 0; s < compareLocationB.TempDataInfoList.Count; s++)
                                {
                                    string sql21 = $@"insert into FromFtp.dbo.TaskLocationInfo(TaskId,OrderNo,ItemCode,ItemName,Volume,Location,Sku1,Sku2,InnerPackSize,Qty,CreateTime)
values('{taskId}', '{abcOrder.BorderNo}', '{compareLocationB.TempDataInfoList[s].ItemCode}', '{compareLocationB.TempDataInfoList[s].ItemName}','{compareLocationB.TempDataInfoList[s].volume}','{compareLocationB.TempDataInfoList[s].Location}',
'{compareLocationB.TempDataInfoList[s].Sku1}', '{compareLocationB.TempDataInfoList[s].Sku2}', '{compareLocationB.TempDataInfoList[s].InnerPackSize}','{compareLocationB.TempDataInfoList[s].Qty}','{dtTime}')";
                                    sqlList2.Add(sql21);
                                    string sql31 = $"update FromFtp.dbo.InitialDataHye set Flag=2 where Id='{compareLocationB.TempDataInfoList[s].SortNo}'";
                                    sqlList3.Add(sql31);
                                }

                                string sql41 = $@"insert into FromFtp.dbo.TaskInfo(TaskId,OrderNo,Total,OrderStatus,ShopNo,ShopName,CreateTime,DeliveryDate,EtaDate,DepartId,Province)
values('{taskId}','{abcOrder.BorderNo}','{compareLocationB.SumQty}','1','{compareLocationB.ShopNo}','{compareLocationB.ShopName}','{dtTime}','{compareLocationB.DeliveryDate}','{compareLocationB.EtaDate}','{compareLocationB.DepartId}','{compareLocationB.Province}')";
                                sqlList4.Add(sql41);
                                totalCount += compareLocationB.SumQty;
                            }
                            ///绑定第三单
                            if (!string.IsNullOrEmpty(abcOrder.CorderNo))
                            {
                                CompareLocation compareLocationC = compareLocationList.Find(a => a.OrderNo.Equals(abcOrder.CorderNo));
                                for (int s = 0; s < compareLocationC.TempDataInfoList.Count; s++)
                                {
                                    string sql21 = $@"insert into FromFtp.dbo.TaskLocationInfo(TaskId,OrderNo,ItemCode,ItemName,Volume,Location,Sku1,Sku2,InnerPackSize,Qty,CreateTime)
values('{taskId}', '{abcOrder.CorderNo}', '{compareLocationC.TempDataInfoList[s].ItemCode}', '{compareLocationC.TempDataInfoList[s].ItemName}','{compareLocationC.TempDataInfoList[s].volume}','{compareLocationC.TempDataInfoList[s].Location}',
'{compareLocationC.TempDataInfoList[s].Sku1}', '{compareLocationC.TempDataInfoList[s].Sku2}', '{compareLocationC.TempDataInfoList[s].InnerPackSize}','{compareLocationC.TempDataInfoList[s].Qty}','{dtTime}')";
                                    sqlList2.Add(sql21);
                                    string sql31 = $"update FromFtp.dbo.InitialDataHye set Flag=2 where Id='{compareLocationC.TempDataInfoList[s].SortNo}'";
                                    sqlList3.Add(sql31);
                                }

                                string sql41 = $@"insert into FromFtp.dbo.TaskInfo(TaskId,OrderNo,Total,OrderStatus,ShopNo,ShopName,CreateTime,DeliveryDate,EtaDate,DepartId,Province)
values('{taskId}','{abcOrder.CorderNo}','{compareLocationC.SumQty}','1','{compareLocationC.ShopNo}','{compareLocationC.ShopName}','{dtTime}','{compareLocationC.DeliveryDate}','{compareLocationC.EtaDate}','{compareLocationC.DepartId}','{compareLocationC.Province}')";
                                sqlList4.Add(sql41);
                                totalCount += compareLocationC.SumQty;
                            }

                            //最后插入任务单主表
                            string sql1 = $"insert into FromFtp.dbo.Task(TaskId,CreateTime,TaskStatus,TaskBindType,Province2,DepartmentId,BackUp1)values('{taskId}','{dtTime}','1','3','{abcOrderList[i].ProvinceId2}','{abcOrderList[i].DepartmentId}','{totalCount}')";
                            sqlList1.Add(sql1);
                        }
                    }
                    sqlAll.AddRange(sqlList1);
                    sqlAll.AddRange(sqlList2);
                    sqlAll.AddRange(sqlList3);
                    sqlAll.AddRange(sqlList4);
                    sqlAll.AddRange(sqlList5);
                    int flag = DbHelperSQL.ExecuteSqlTran(sqlAll);
                    if (flag > 0)
                    {
                        LogHelper.Logger.Info("GetDataBindThreeOrder:" + "导入数据成功！");
                    }
                    else
                    {
                        LogHelper.Logger.Info("GetDataBindThreeOrder:" + "导入数据失败！");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Info("GetDataBindThreeOrder:" + ex.Message);
                LogHelper.Logger.Info("GetDataBindThreeOrder:" + ex.StackTrace);
            }
        }

        public void UpLoadFtpFile()
        {
            //string connectionString = ConfigurationManager.AppSettings["DbHelperConnectionString"];
            string filePath = ConfigurationManager.AppSettings["upLoadLocalPath"];
            //s1: 获取所有要下载的文件名  downLoadFtpFileName
            string ftpFileName = ConfigurationManager.AppSettings["downLoadFtpFileName"];
            //List<string> fileNameList = FTPHelper.GetFtpFileList(test);
            List<string> fileNameList = FTPHelper.GetFtpFileList(ftpFileName);
            List<string> csvList = new List<string>();//CSV 文件集合
            List<string> ctlList = new List<string>();//与CSV文件集合对应的数量集合
            List<string> boxList = new List<string>();//箱号文件集合
            List<string> fileExistList = new List<string>();
            for (int i = 0; i < fileNameList.Count; i++)
            {
                if (fileNameList[i].ToUpper().Contains("CSV"))
                {
                    if (fileNameList[i].ToUpper().Contains("N"))
                    {
                        boxList.Add(fileNameList[i]);
                    }
                    else if (fileNameList[i].ToUpper().Contains("Q"))
                    {
                        csvList.Add(fileNameList[i]);
                    }
                }
                if (fileNameList[i].ToUpper().Contains("CTL"))
                {
                    ctlList.Add(fileNameList[i]);
                }
            }
            //s2:下载文件
            for (int i = 0; i < fileNameList.Count; i++)
            {
                int isExis = 0;
                //bool flag = FTPHelper.Download("test", filePath, fileNameList[i], ref isExis);
                bool flag = FTPHelper.Download(ftpFileName, filePath, fileNameList[i], ref isExis);
                if (!flag)
                {
                    if (isExis == 1)
                    {
                        LogHelper.Logger.Info("下载文件:文件名[" + fileNameList[i] + "]已经存在");
                        fileExistList.Add(fileNameList[i]);
                        continue;
                    }
                    //写入日志：
                    LogHelper.Logger.Info("下载文件:文件名[" + fileNameList[i] + "]下载失败");
                    continue;
                }
            }
            //s3:比对数据
            //List<string> delCSVList = new List<string>();
            List<string> sqlAll = new List<string>();
            List<string> delList = new List<string>();
            //如果下载的是数据文件则比对数据
            for (int i = 0; i < csvList.Count; i++)
            {
                string name = csvList[i].Split('.')[0];
                DataTable dt = new DataTable();
                string path = filePath + csvList[i];
                FTPHelper.OpenCSVFile(ref dt, path);

                int index = ctlList.FindIndex(a => a.Contains(name));
                if (index < 0)
                {
                    continue;
                }
                string path2 = filePath + ctlList[index];
                DataTable dt2 = new DataTable();
                FTPHelper.OpenCSVFile(ref dt2, path2);
                //判断每一个csv文件的行和 ctl的数据是否一致。
                if (dt.Rows.Count <= 0 || dt2.Rows.Count <= 0)
                {
                    continue;
                }
                int count = dt.Rows.Count;
                int count2 = Convert.ToInt32(dt2.Rows[0][0]);
                if (count != count2)
                {
                    continue;
                }
                //写入将要删除的文件名
                delList.Add(csvList[i]);
                delList.Add(ctlList[index]);
                string dtTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                //已经存在的文件,不需要写入数据
                if (!fileExistList.Contains(csvList[i]))
                {
                    for (int j = 0; j < dt.Rows.Count; j++)
                    {
                        string s1 = dt.Rows[j][1] == DBNull.Value ? "" : dt.Rows[j][1].ToString().Trim();
                        string s2 = dt.Rows[j][2] == DBNull.Value ? "" : dt.Rows[j][2].ToString().Trim();
                        string s3 = dt.Rows[j][3] == DBNull.Value ? "" : dt.Rows[j][3].ToString().Trim();
                        string s4 = dt.Rows[j][4] == DBNull.Value ? "" : dt.Rows[j][4].ToString().Trim();
                        string s5 = dt.Rows[j][5] == DBNull.Value ? "" : dt.Rows[j][5].ToString().Trim();
                        string s6 = dt.Rows[j][6] == DBNull.Value ? "" : dt.Rows[j][6].ToString().Trim();
                        string s7 = dt.Rows[j][7] == DBNull.Value ? "" : dt.Rows[j][7].ToString().Trim();
                        string s8 = dt.Rows[j][8] == DBNull.Value ? "" : dt.Rows[j][8].ToString().Trim();
                        string s9 = dt.Rows[j][9] == DBNull.Value ? "" : dt.Rows[j][9].ToString().Trim();
                        string s10 = dt.Rows[j][10] == DBNull.Value ? "" : dt.Rows[j][10].ToString().Trim();
                        string s11 = dt.Rows[j][11] == DBNull.Value ? "" : dt.Rows[j][11].ToString().Trim();
                        string s12 = dt.Rows[j][12] == DBNull.Value ? "" : dt.Rows[j][12].ToString().Trim();
                        string s13 = dt.Rows[j][13] == DBNull.Value ? "" : dt.Rows[j][13].ToString().Trim();

                        string volume = string.Empty;
                        if (string.IsNullOrEmpty(s13))
                        {
                            volume = "0.0001";
                        }
                        else
                        {
                            volume = "0." + s13;
                        }
                        string vv = (Convert.ToDouble(volume) * 0.95).ToString("f4");

                        string s14 = dt.Rows[j][14] == DBNull.Value ? "" : dt.Rows[j][14].ToString().Trim();
                        string s15 = dt.Rows[j][15] == DBNull.Value ? "" : dt.Rows[j][15].ToString().Trim();
                        string s16 = dt.Rows[j][16] == DBNull.Value ? "" : dt.Rows[j][16].ToString().Trim();
                        string s17 = dt.Rows[j][17] == DBNull.Value ? "" : dt.Rows[j][17].ToString().Trim();
                        string s18 = dt.Rows[j][18] == DBNull.Value ? "" : dt.Rows[j][18].ToString().Trim();
                        string s19 = dt.Rows[j][19] == DBNull.Value ? "" : dt.Rows[j][19].ToString().Trim();
                        string s20 = dt.Rows[j][20] == DBNull.Value ? "" : dt.Rows[j][20].ToString().Trim();
                        string s21 = dt.Rows[j][21] == DBNull.Value ? "" : dt.Rows[j][21].ToString().Trim();
                        string s22 = dt.Rows[j][22] == DBNull.Value ? "" : dt.Rows[j][22].ToString().Trim();
                        string s23 = dt.Rows[j][23] == DBNull.Value ? "" : dt.Rows[j][23].ToString().Trim();
                        string s24 = dt.Rows[j][24] == DBNull.Value ? "" : dt.Rows[j][24].ToString().Trim();
                        string s25 = dt.Rows[j][25] == DBNull.Value ? "" : dt.Rows[j][25].ToString().Trim();
                        string s26 = dt.Rows[j][26] == DBNull.Value ? "" : dt.Rows[j][26].ToString().Trim();
                        string s182 = string.Empty;
                        if (s18.Length == 5)
                        {
                            s182 = s18.Substring(0, 4);
                        }

                        string sql = $@"insert into FromFtp.dbo.InitialData(BatchNo,BatchName,RecordNo,ItemCode,Sku1,ItemName,Qty,ShopNo,ShopName,InnerPackSize,WmsOrder,DepartId,
Volume,DeliveryDate,EtaDate,Sku2,Reserve,Province,Location,ChuteNo,AmountInput,AmountResult,WorkDate,
WorkLine,WorkDay,WorkTime,CreateTime,Flag,Province2) values
('{s1}','{s2}','{s3}','{s4}','{s5}','{s6}','{s7}','{s8}','{s9}','{s10}','{s11}','{s12}',
'{vv}','{s14}','{s15}','{s16}','{s17}','{s18}','{s19}','{s20}','{s21}',
'{s22}','{s23}','{s24}','{s25}','{s26}','{dtTime}',1,'{s182}')";

                        sqlAll.Add(sql);
                    }
                }
                else
                {
                    //如果数据文件存在但是日志文件不存在,那么也生成数据
                    //add by felix 2021=09-01
                    if (!fileExistList.Contains(ctlList[index]))
                    {
                        for (int j = 0; j < dt.Rows.Count; j++)
                        {
                            string s1 = dt.Rows[j][1] == DBNull.Value ? "" : dt.Rows[j][1].ToString().Trim();
                            string s2 = dt.Rows[j][2] == DBNull.Value ? "" : dt.Rows[j][2].ToString().Trim();
                            string s3 = dt.Rows[j][3] == DBNull.Value ? "" : dt.Rows[j][3].ToString().Trim();
                            string s4 = dt.Rows[j][4] == DBNull.Value ? "" : dt.Rows[j][4].ToString().Trim();
                            string s5 = dt.Rows[j][5] == DBNull.Value ? "" : dt.Rows[j][5].ToString().Trim();
                            string s6 = dt.Rows[j][6] == DBNull.Value ? "" : dt.Rows[j][6].ToString().Trim();
                            string s7 = dt.Rows[j][7] == DBNull.Value ? "" : dt.Rows[j][7].ToString().Trim();
                            string s8 = dt.Rows[j][8] == DBNull.Value ? "" : dt.Rows[j][8].ToString().Trim();
                            string s9 = dt.Rows[j][9] == DBNull.Value ? "" : dt.Rows[j][9].ToString().Trim();
                            string s10 = dt.Rows[j][10] == DBNull.Value ? "" : dt.Rows[j][10].ToString().Trim();
                            string s11 = dt.Rows[j][11] == DBNull.Value ? "" : dt.Rows[j][11].ToString().Trim();
                            string s12 = dt.Rows[j][12] == DBNull.Value ? "" : dt.Rows[j][12].ToString().Trim();
                            string s13 = dt.Rows[j][13] == DBNull.Value ? "" : dt.Rows[j][13].ToString().Trim();

                            string volume = string.Empty;
                            if (string.IsNullOrEmpty(s13))
                            {
                                volume = "0.0001";
                            }
                            else
                            {
                                volume = "0." + s13;
                            }
                            string vv = (Convert.ToDouble(volume) * 0.95).ToString("f4");

                            string s14 = dt.Rows[j][14] == DBNull.Value ? "" : dt.Rows[j][14].ToString().Trim();
                            string s15 = dt.Rows[j][15] == DBNull.Value ? "" : dt.Rows[j][15].ToString().Trim();
                            string s16 = dt.Rows[j][16] == DBNull.Value ? "" : dt.Rows[j][16].ToString().Trim();
                            string s17 = dt.Rows[j][17] == DBNull.Value ? "" : dt.Rows[j][17].ToString().Trim();
                            string s18 = dt.Rows[j][18] == DBNull.Value ? "" : dt.Rows[j][18].ToString().Trim();
                            string s19 = dt.Rows[j][19] == DBNull.Value ? "" : dt.Rows[j][19].ToString().Trim();
                            string s20 = dt.Rows[j][20] == DBNull.Value ? "" : dt.Rows[j][20].ToString().Trim();
                            string s21 = dt.Rows[j][21] == DBNull.Value ? "" : dt.Rows[j][21].ToString().Trim();
                            string s22 = dt.Rows[j][22] == DBNull.Value ? "" : dt.Rows[j][22].ToString().Trim();
                            string s23 = dt.Rows[j][23] == DBNull.Value ? "" : dt.Rows[j][23].ToString().Trim();
                            string s24 = dt.Rows[j][24] == DBNull.Value ? "" : dt.Rows[j][24].ToString().Trim();
                            string s25 = dt.Rows[j][25] == DBNull.Value ? "" : dt.Rows[j][25].ToString().Trim();
                            string s26 = dt.Rows[j][26] == DBNull.Value ? "" : dt.Rows[j][26].ToString().Trim();
                            string s182 = string.Empty;
                            if (s18.Length == 5)
                            {
                                s182 = s18.Substring(0, 4);
                            }

                            string sql = $@"insert into FromFtp.dbo.InitialData(BatchNo,BatchName,RecordNo,ItemCode,Sku1,ItemName,Qty,ShopNo,ShopName,InnerPackSize,WmsOrder,DepartId,
Volume,DeliveryDate,EtaDate,Sku2,Reserve,Province,Location,ChuteNo,AmountInput,AmountResult,WorkDate,
WorkLine,WorkDay,WorkTime,CreateTime,Flag,Province2) values
('{s1}','{s2}','{s3}','{s4}','{s5}','{s6}','{s7}','{s8}','{s9}','{s10}','{s11}','{s12}',
'{vv}','{s14}','{s15}','{s16}','{s17}','{s18}','{s19}','{s20}','{s21}',
'{s22}','{s23}','{s24}','{s25}','{s26}','{dtTime}',1,'{s182}')";

                            sqlAll.Add(sql);
                        }
                    }
                    //end add
                }

                ////删除文件
                //for (int j = 0; j < delList.Count; j++)
                //{
                //    string pathDel = "test/" + delList[j];
                //    FTPHelper.DeleteFile(pathDel);
                //}
            }
            //如果有箱子明细数据则生成箱号
            for (int i = 0; i < boxList.Count; i++)
            {
                string name = boxList[i].Split('.')[0];
                DataTable dt = new DataTable();
                string path = filePath + boxList[i];
                FTPHelper.OpenCSVFile(ref dt, path);
                //写入将要删除的文件名
                delList.Add(boxList[i]);
                string dtTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                //已经存在的文件,不需要写入数据
                if (!fileExistList.Contains(boxList[i]))
                {
                    for (int j = 0; j < dt.Rows.Count; j++)
                    {
                        long s1 = Convert.ToInt64(dt.Rows[j][1] == DBNull.Value ? "0" : dt.Rows[j][1].ToString().Trim());
                        long s2 = Convert.ToInt64(dt.Rows[j][2] == DBNull.Value ? "0" : dt.Rows[j][2].ToString().Trim());
                        for (long s = s1; s <= s2; s++)
                        {
                            string sql = $@"IF EXISTS(select * from FromFtp.dbo.BoxInfo where BoxNumber='{s}')
Update FromFtp.dbo.BoxInfo set CreateTime='{dtTime}' where BoxNumber='{s}'
ELSE
insert into FromFtp.dbo.BoxInfo(BoxNumber,IsUsed,CreateTime,Flag)
values('{s}',0,'{dtTime}',1)";
                            sqlAll.Add(sql);
                        }
                    }
                }
            }
            //删除文件
            for (int j = 0; j < delList.Count; j++)
            {
                //string pathDel = "test/" + delList[j];
                string pathDel = ftpFileName + "/" + delList[j];
                FTPHelper.DeleteFile(pathDel);
            }
            int flagInsert = DbHelperSQL.ExecuteSqlTran(sqlAll);
        }

        /// <summary>
        /// 根据 相同部门，门店，出库日期得订单合在一起
        /// </summary>
        public void CombineInitialData()
        {
            try
            {
                int comboOrderCountSingle = Convert.ToInt32(ConfigurationManager.AppSettings["comboOrderCountSingle"]);
                List<string> sqlAll = new List<string>();
                //step1:首先获取所有店铺的出库信息
                string sql = $@"select DepartId,ShopNo,EtaDate from FromFtp.dbo.InitialData where Flag=1
group by DepartId,ShopNo,EtaDate
order by DepartId, ShopNo;
select BatchNo,ShopNo,WmsOrder from FromFtp.dbo.InitialData where Flag=1 group by BatchNo,ShopNo,WmsOrder
order by BatchNo,ShopNo;";
                //第二个sql 语句是为了回传订单数据预先写入  WH_Tunnel.dbo.BatchComplete,WH_Tunnel.dbo.BatchCompleteInfo 表
                /*
                 * insert into WH_Tunnel.dbo.BatchCompleteInfo(BatchNo,ShopNo,WmsOrder,Flag,CreateTime)
values('','','','','')
                 */
                //List<AgvassignmentQueryModel> data = ModelConvertHelper<AgvassignmentQueryModel>.ConvertToModel(DbHelperSQL.Query(sql).Tables[0]);
                List<string> sqlInsertWtList = new List<string>();
                List<string> sqlInsertOrderWtList = new List<string>();
                DataSet dsWh = DbHelperSQL.Query(sql);
                string dtTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string batchNoCp = string.Empty;
                string shopNoCp = string.Empty;
                int orderCount = 0;
                for (int i = 0; i < dsWh.Tables[1].Rows.Count; i++)
                {
                    string batchNo = dsWh.Tables[1].Rows[i]["BatchNo"].ToString();
                    string shopNo = dsWh.Tables[1].Rows[i]["ShopNo"].ToString();
                    string wmsOrder = dsWh.Tables[1].Rows[i]["WmsOrder"].ToString();
                    string sqlInsert1 = $@"insert into WH_Tunnel.dbo.BatchCompleteInfo(BatchNo,ShopNo,WmsOrder,Flag,CreateTime)
values('{batchNo}','{shopNo}','{wmsOrder}','1','{dtTime}')";
                    sqlInsertWtList.Add(sqlInsert1);
                    if (i != 0)
                    {
                        if (shopNo.Equals(shopNoCp) && batchNo.Equals(batchNoCp))
                        {
                            orderCount++;
                        }
                        else
                        {
                            string sqlInsert2 = $@"insert into WH_Tunnel.dbo.BatchComplete(BatchNo,ShopNo,OrderCount,Flag,CreateTime)
values('{batchNoCp}','{shopNoCp}','{orderCount}','1','{dtTime}')";
                            sqlInsertOrderWtList.Add(sqlInsert2);
                            batchNoCp = batchNo;
                            shopNoCp = shopNo;
                            orderCount = 1;
                        }
                    }
                    else
                    {
                        batchNoCp = batchNo;
                        shopNoCp = shopNo;
                        orderCount = 1;
                    }
                    if (i == dsWh.Tables[1].Rows.Count - 1)
                    {
                        string sqlInsert2 = $@"insert into WH_Tunnel.dbo.BatchComplete(BatchNo,ShopNo,OrderCount,Flag,CreateTime)
values('{batchNoCp}','{shopNoCp}','{orderCount}','1','{dtTime}')";
                        sqlInsertOrderWtList.Add(sqlInsert2);
                    }
                }
                if (sqlInsertWtList.Count > 0)
                {
                    sqlAll.AddRange(sqlInsertWtList);
                }
                if (sqlInsertOrderWtList.Count > 0)
                {
                    sqlAll.AddRange(sqlInsertOrderWtList);
                }
                List<GetCombineShopModel> modelShopList = ModelConvertHelper<GetCombineShopModel>.ConvertToModel(DbHelperSQL.Query(sql).Tables[0]);
                List<string> todoCombineWmsOrderList = new List<string>();
                List<CombineOrder> combineOrderList = new List<CombineOrder>();

                for (int i = 0; i < modelShopList.Count; i++)
                {
                    //step2: 获取每个分组的订单集合
                    //string sql2 = $@"select WmsOrder from FromFtp.dbo.InitialData where Flag =1 and DepartId='{modelShopList[i].DepartId}' and ShopNo='{modelShopList[i].ShopNo}' 
                    //and EtaDate='{modelShopList[i].EtaDate}' group by WmsOrder";

                    string sql2 = $@"select WmsOrder,SUM(Qty) totalQty from FromFtp.dbo.InitialData where Flag =1 and DepartId='{modelShopList[i].DepartId}' and ShopNo='{modelShopList[i].ShopNo}' 
                    and EtaDate='{modelShopList[i].EtaDate}' group by WmsOrder having SUM(Qty)<'{comboOrderCountSingle}'";

                    DataSet ds = DbHelperSQL.Query(sql2);
                    //如果有两个及以上的订单符合相同门店，相同部门，相同出库日期，那么合并这些订单
                    //如果只有一个，那么还放在原有订单池,不作处理。
                    if (ds.Tables[0].Rows.Count > 1)
                    {
                        //2-1： 首先生成合并单单号。
                        string cbWmsOrder = "CB" + GetCombineOrderNo();
                        CombineOrder combineOrder = new CombineOrder();
                        combineOrder.combineOrderNo = cbWmsOrder;
                        List<string> initialOrderList = new List<string>();
                        List<CombineOrderInfo> combineOrderInfoList = new List<CombineOrderInfo>();
                        for (int j = 0; j < ds.Tables[0].Rows.Count; j++)
                        {
                            string wmsOrder = ds.Tables[0].Rows[j][0].ToString().Trim();
                            todoCombineWmsOrderList.Add(wmsOrder);//将需要的订单单号装入集合中，后面循环订单池的时候，如果有订单号在合并订单的集合中则跳过。
                            //用合并之后的新的订单信息参与任务单的分配。
                            //combineOrder.initialOrderList.Add(wmsOrder);
                            initialOrderList.Add(wmsOrder);
                            //获取原有订单信息  CombineOrderInfo
                            string sql3 = $@"SELECT [BatchNo],[BatchName],[RecordNo],[ItemCode],[Sku1],[ItemName],[Qty],[ShopNo],[ShopName],[InnerPackSize],[WmsOrder]
      ,[DepartId],[Volume],[DeliveryDate],[EtaDate],[Sku2],[Reserve],[ChuteNo],[AmountInput],[AmountResult],[WorkDate]
      ,[WorkLine],[WorkDay],[WorkTime],[Location],[Province],[Province2]
  FROM [FromFtp].[dbo].[InitialData] where WmsOrder='{wmsOrder}' order by Location";
                            List<CombineOrderInfo> orderInfoList = ModelConvertHelper<CombineOrderInfo>.ConvertToModel(DbHelperSQL.Query(sql3).Tables[0]);
                            for (int k = 0; k < orderInfoList.Count; k++)
                            {
                                int index = combineOrderInfoList.FindIndex(a => a.ItemCode.Trim().Equals(orderInfoList[k].ItemCode.Trim()));
                                if (index >= 0)
                                {
                                    combineOrderInfoList[index].Qty += orderInfoList[k].Qty;
                                }
                                else
                                {
                                    combineOrderInfoList.Add(orderInfoList[k]);
                                }
                            }
                        }
                        combineOrder.initialOrderList = initialOrderList;
                        combineOrder.combineOrderInfoList = combineOrderInfoList;
                        combineOrderList.Add(combineOrder);
                    }
                }
                //循环 FromFtp.dbo.InitialData 表 flag=1 ，将数据存入 FromFtp.dbo.InitialDataHye 中。
                //逻辑： 1. 得到的wmsOrder 订单号如果在 todoCombineWmsOrderList 中，则跳过。
                //2. 最后循环一下 combineOrderList 集合，将合并的订单数据也存入 FromFtp.dbo.InitialDataHye
                string sqlQuery = $@"SELECT [ID],[BatchNo],[BatchName],[RecordNo],[ItemCode],[Sku1],[ItemName],[Qty],[ShopNo],[ShopName],[InnerPackSize],[WmsOrder]
      ,[DepartId],[Volume],[DeliveryDate],[EtaDate],[Sku2],[Reserve],[ChuteNo],[AmountInput],[AmountResult],[WorkDate]
      ,[WorkLine],[WorkDay],[WorkTime],[Location],[Province],[Province2]
  FROM [FromFtp].[dbo].[InitialData] where Flag=1";
                DataSet dsAll = DbHelperSQL.Query(sqlQuery);
                List<string> sqlInsertList = new List<string>();//插入  FromFtp.dbo.InitialDataHye 表
                List<string> sqlUpdateLIst = new List<string>();//更新  FromFtp.dbo.InitialData 表
                List<CombineOrderInfoQuery> InitialInfoList = ModelConvertHelper<CombineOrderInfoQuery>.ConvertToModel(DbHelperSQL.Query(sqlQuery).Tables[0]);
                for (int i = 0; i < InitialInfoList.Count; i++)
                {
                    if (todoCombineWmsOrderList.Contains(InitialInfoList[i].WmsOrder.Trim()))
                    {
                        continue;
                    }
                    else
                    {
                        string v1 = InitialInfoList[i].Volume;
                        string sqlInsert = $@"insert into FromFtp.dbo.InitialDataHye(BatchNo,BatchName,RecordNo,ItemCode,Sku1,ItemName,Qty,ShopNo,ShopName,
InnerPackSize,WmsOrder,DepartId,Volume,DeliveryDate,EtaDate,Sku2,Reserve,ChuteNo,AmountInput,AmountResult,WorkDate,WorkLine,WorkDay,WorkTime,
Location,Province,CreateTime,Flag,BranchNoList,Province2)
values('{InitialInfoList[i].BatchNo}','{InitialInfoList[i].BatchName}','{InitialInfoList[i].RecordNo}','{InitialInfoList[i].ItemCode}','{InitialInfoList[i].Sku1}',
'{InitialInfoList[i].ItemName}','{InitialInfoList[i].Qty}','{InitialInfoList[i].ShopNo}','{InitialInfoList[i].ShopName}','{InitialInfoList[i].InnerPackSize}',
'{InitialInfoList[i].WmsOrder}','{InitialInfoList[i].DepartId}','{InitialInfoList[i].Volume}','{InitialInfoList[i].DeliveryDate}','{InitialInfoList[i].EtaDate}',
'{InitialInfoList[i].Sku2}','{InitialInfoList[i].Reserve}','{InitialInfoList[i].ChuteNo}','{InitialInfoList[i].AmountInput}','{InitialInfoList[i].AmountResult}',
'{InitialInfoList[i].WorkDate}','{InitialInfoList[i].WorkLine}','{InitialInfoList[i].WorkDay}','{InitialInfoList[i].WorkTime}','{InitialInfoList[i].Location}',
'{InitialInfoList[i].Province}','{dtTime}','1','','{InitialInfoList[i].Province2}')";
                        sqlInsertList.Add(sqlInsert);

                        string sqlUpdate = $@"update FromFtp.dbo.InitialData set Flag=2 where ID='{InitialInfoList[i].ID}' and Flag=1";
                        sqlUpdateLIst.Add(sqlUpdate);
                    }
                }
                //合并的订单信息也插入 FromFtp.dbo.InitialDataHye 表  并更新订单号来更新From.dbo.InitialData
                for (int i = 0; i < combineOrderList.Count; i++)
                {
                    for (int j = 0; j < combineOrderList[i].initialOrderList.Count; j++)
                    {
                        string sqlUpdate = $@"update FromFtp.dbo.InitialData set Flag=2 where WmsOrder='{combineOrderList[i].initialOrderList[j]}' and Flag=1";
                        sqlUpdateLIst.Add(sqlUpdate);
                    }
                    string[] strs = combineOrderList[i].initialOrderList.ToArray();
                    string initialOrderStr = string.Join(",", strs);
                    //插入  CombineOrderNoInfo 表
                    string sqlInserCombineOrderNoInfo = $@"insert into FromFtp.dbo.CombineOrderNoInfo(CombineOrderNo,WmsOrderStr,CreateTime)
values('{combineOrderList[i].combineOrderNo}','{initialOrderStr}','{dtTime}')";
                    sqlInsertList.Add(sqlInserCombineOrderNoInfo);
                    for (int j = 0; j < combineOrderList[i].combineOrderInfoList.Count; j++)
                    {
                        //插入合并信息
                        string sqlInsert = $@"insert into FromFtp.dbo.InitialDataHye(BatchNo,BatchName,RecordNo,ItemCode,Sku1,ItemName,Qty,ShopNo,ShopName,
InnerPackSize,WmsOrder,DepartId,Volume,DeliveryDate,EtaDate,Sku2,Reserve,ChuteNo,AmountInput,AmountResult,WorkDate,WorkLine,WorkDay,WorkTime,
Location,Province,CreateTime,Flag,BranchNoList,Province2)
values('{combineOrderList[i].combineOrderInfoList[j].BatchNo}','{combineOrderList[i].combineOrderInfoList[j].BatchName}','{combineOrderList[i].combineOrderInfoList[j].RecordNo}','{combineOrderList[i].combineOrderInfoList[j].ItemCode}','{combineOrderList[i].combineOrderInfoList[j].Sku1}',
'{combineOrderList[i].combineOrderInfoList[j].ItemName}','{combineOrderList[i].combineOrderInfoList[j].Qty}','{combineOrderList[i].combineOrderInfoList[j].ShopNo}','{combineOrderList[i].combineOrderInfoList[j].ShopName}','{combineOrderList[i].combineOrderInfoList[j].InnerPackSize}',
'{combineOrderList[i].combineOrderNo}','{combineOrderList[i].combineOrderInfoList[j].DepartId}','{combineOrderList[i].combineOrderInfoList[j].Volume}','{combineOrderList[i].combineOrderInfoList[j].DeliveryDate}','{combineOrderList[i].combineOrderInfoList[j].EtaDate}',
'{combineOrderList[i].combineOrderInfoList[j].Sku2}','{combineOrderList[i].combineOrderInfoList[j].Reserve}','{combineOrderList[i].combineOrderInfoList[j].ChuteNo}','{combineOrderList[i].combineOrderInfoList[j].AmountInput}','{combineOrderList[i].combineOrderInfoList[j].AmountResult}',
'{combineOrderList[i].combineOrderInfoList[j].WorkDate}','{combineOrderList[i].combineOrderInfoList[j].WorkLine}','{combineOrderList[i].combineOrderInfoList[j].WorkDay}','{combineOrderList[i].combineOrderInfoList[j].WorkTime}','{combineOrderList[i].combineOrderInfoList[j].Location}',
'{combineOrderList[i].combineOrderInfoList[j].Province}','{dtTime}','1','{initialOrderStr}','{combineOrderList[i].combineOrderInfoList[j].Province2}')";
                        sqlInsertList.Add(sqlInsert);
                    }
                }

                sqlAll.AddRange(sqlInsertList);
                sqlAll.AddRange(sqlUpdateLIst);
                if (sqlAll.Count > 0)
                {
                    int flag = DbHelperSQL.ExecuteSqlTran(sqlAll);
                    if (flag > 0)
                    {
                        LogHelper.Logger.Info("CombineInitialData:" + "合并成功！");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Info("CombineInitialData:" + ex.Message);
                LogHelper.Logger.Info("CombineInitialData:" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 获取合并单号
        /// </summary>
        /// <returns></returns>
        public int GetCombineOrderNo()
        {
            string sql = @"update FromFtp.dbo.InitialDataCombineOrderNo set sheetNo=sheetNo+1;
select sheetNo from FromFtp.dbo.InitialDataCombineOrderNo";
            DataSet ds = DbHelperSQL.Query(sql);

            return Convert.ToInt32(ds.Tables[0].Rows[0]["sheetNo"]);
        }

        /// <summary>
        /// 获取分拣单单号
        /// </summary>
        /// <returns></returns>
        public int GetSortTaskCode()
        {
            string sql = @"update FromFtp.dbo.TaskSNCreate set HyeSortNo=HyeSortNo+1;
select HyeSortNo from FromFtp.dbo.TaskSNCreate";
            DataSet ds = DbHelperSQL.Query(sql);

            return Convert.ToInt32(ds.Tables[0].Rows[0]["HyeSortNo"]);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
    /// <summary>
    /// 比较实体对象
    /// </summary>
    public class CompareLocation
    {
        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// 路径集合
        /// </summary>
        public List<string> LocationList { get; set; }

        /// <summary>
        /// 订单明细数据
        /// </summary>
        public List<TempDataInfo> TempDataInfoList { get; set; }

        /// <summary>
        /// 店铺号码
        /// </summary>
        public string ShopNo { get; set; }

        /// <summary>
        /// 店铺名称
        /// </summary>
        public string ShopName { get; set; }

        /// <summary>
        /// 部门号
        /// </summary>
        public string DepartId { get; set; }

        /// <summary>
        /// 出库日期
        /// </summary>
        public string DeliveryDate { get; set; }

        /// <summary>
        /// 到店日期（交货日期）
        /// </summary>
        public string EtaDate { get; set; }

        /// <summary>
        /// 订单共包含多少件
        /// </summary>
        public int SumQty { get; set; }

        /// <summary>
        /// 省份
        /// </summary>
        public string Province { get; set; }

        //Province2

        /// <summary>
        /// 省份缩写
        /// </summary>
        public string Province2 { get; set; }
    }

    public class TempDataInfo
    {
        /// <summary>
        /// 排序号
        /// </summary>
        public int SortNo { get; set; }

        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// PLU
        /// </summary>
        public string ItemCode { get; set; }

        /// <summary>
        /// 商品名称
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 商品品番
        /// </summary>
        public string Sku1 { get; set; }

        /// <summary>
        /// 颜色尺码
        /// </summary>
        public string Sku2 { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Qty { get; set; }

        /// <summary>
        /// 站点坐标
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// 包规则
        /// </summary>
        public int InnerPackSize { get; set; }

        public string volume { get; set; }
    }

    public class ABCOrder
    {
        public string AorderNo { get; set; }

        public string BorderNo { get; set; }

        public string CorderNo { get; set; }

        /// <summary>
        /// 省份编码(缩写) add by felix 2021-07-12
        /// </summary>
        public string ProvinceId2 { get; set; }

        /// <summary>
        /// 部门编码  add by felix 2021-07-12
        /// </summary>
        public string DepartmentId { get; set; }
    }

    public class ABOrder
    {
        public string AorderNo { get; set; }

        public string BorderNo { get; set; }
    }

    public class GetCombineShopModel
    {
        //select DepartId,ShopNo,DeliveryDate from FromFtp.dbo.InitialData
        public string DepartId { get; set; }

        public string ShopNo { get; set; }

        public string DeliveryDate { get; set; }

        public string EtaDate { get; set; }
    }

    public class CombineOrder
    {
        public string combineOrderNo { get; set; }

        public List<string> initialOrderList { get; set; }

        public List<CombineOrderInfo> combineOrderInfoList { get; set; }
    }

    public class CombineOrderInfo
    {
        /*[BatchNo],[BatchName],[RecordNo],[ItemCode],[Sku1],[ItemName],[Qty],[ShopNo],[ShopName],[InnerPackSize],[WmsOrder],[DepartId]
      ,[Volume],[DeliveryDate],[EtaDate],[Sku2],[Reserve],[ChuteNo],[AmountInput],[AmountResult],[WorkDate],[WorkLine],[WorkDay],[WorkTime]
      ,[Location],[Province]
         */
        public string BatchNo { get; set; }
        public string BatchName { get; set; }
        public string RecordNo { get; set; }
        public string ItemCode { get; set; }
        public string Sku1 { get; set; }
        public string ItemName { get; set; }
        public int Qty { get; set; }
        public string ShopNo { get; set; }
        public string ShopName { get; set; }
        public string InnerPackSize { get; set; }
        public string DepartId { get; set; }
        public string Volume { get; set; }
        public string DeliveryDate { get; set; }
        public string EtaDate { get; set; }
        public string Sku2 { get; set; }
        public string Reserve { get; set; }
        public string ChuteNo { get; set; }
        public string AmountInput { get; set; }
        public string AmountResult { get; set; }
        public string WorkDate { get; set; }
        public string WorkLine { get; set; }
        public string WorkDay { get; set; }
        public string WorkTime { get; set; }
        public string Location { get; set; }
        public string Province { get; set; }
        public string Province2 { get; set; }
    }

    public class CombineOrderInfoQuery
    {
        /*[BatchNo],[BatchName],[RecordNo],[ItemCode],[Sku1],[ItemName],[Qty],[ShopNo],[ShopName],[InnerPackSize],[WmsOrder],[DepartId]
      ,[Volume],[DeliveryDate],[EtaDate],[Sku2],[Reserve],[ChuteNo],[AmountInput],[AmountResult],[WorkDate],[WorkLine],[WorkDay],[WorkTime]
      ,[Location],[Province]
         */
        public int ID { get; set; }
        public string BatchNo { get; set; }
        public string BatchName { get; set; }
        public string RecordNo { get; set; }
        public string ItemCode { get; set; }
        public string Sku1 { get; set; }
        public string ItemName { get; set; }
        public int Qty { get; set; }
        public string ShopNo { get; set; }
        public string ShopName { get; set; }
        public string InnerPackSize { get; set; }
        public string WmsOrder { get; set; }
        public string DepartId { get; set; }
        public string Volume { get; set; }
        public string DeliveryDate { get; set; }
        public string EtaDate { get; set; }
        public string Sku2 { get; set; }
        public string Reserve { get; set; }
        public string ChuteNo { get; set; }
        public string AmountInput { get; set; }
        public string AmountResult { get; set; }
        public string WorkDate { get; set; }
        public string WorkLine { get; set; }
        public string WorkDay { get; set; }
        public string WorkTime { get; set; }
        public string Location { get; set; }
        public string Province { get; set; }
        public string Province2 { get; set; }
    }

    /// <summary>
    /// 订单路径相似度比较
    /// </summary>
    public class OrderLocationLikeCompare
    {
        /// <summary>
        /// 单号
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// 重复路径集合
        /// </summary>
        public int LocationLikeCount { get; set; }
    }
}
