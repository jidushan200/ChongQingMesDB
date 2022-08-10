using CommonClass;
using OpcUaHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChongQingControlServer.Option2
{
    internal class AssemblyTask
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("AssemblyTaskTask");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志

        static int iStage = 0;                                                                      //状态机

        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskAssemblyFunc());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskAssemblyFunc()
        {
            string strmsg = "";
            int iTimeWait = 0;
            bool flag = false;
            //临时变量
            int id = 0;                                                                             //订单id
            string productSN = String.Empty;                                                        //成品串号
            int palletid = 0;
            OpcUaClient ua = new OpcUaClient();
            strmsg = "环线任务启动线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //初始化数据
            iTimeWait = 0;
            bRunningFlag = true;                                                                    //设置任务线程运行标志
            while (true)
            {
                if (bStop && iStage == 0 && !WMSTwoTask.bRunningFlag)                                 //结束线程
                    break;

                //延时等待
                if (iTimeWait > 0)
                {
                    iTimeWait--;
                    Thread.Sleep(1000);
                    continue;
                }

                using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnString))
                {
                    try
                    {
                        conn.Open();
                        var t1 = ua.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
                        Task.WaitAll(t1);
                        while (true)
                        {
                            if (bStop && iStage == 0 && !WMSTwoTask.bRunningFlag)                     //结束线程
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //状态机：


                            #region 第一工位有托盘上升沿（0）
                            if (iStage == 0&& VarComm.GetVar(conn, "WMS2", "ProductEnable") != "")
                            {
                                string firstEmpty = OpcUaHelper.R_AssemblyFirstEmtpy_Method(ua);//环线第一工位空true代表空，false代表有东西
                                palletid = OpcUaHelper.R_AssemblyFirstPalletNo_Method(ua);
                                if (firstEmpty == "False" && palletid != 0)//WMS2入库口有托盘，代表装配完成
                                {

                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_pallet_update", new SqlParameter("type", 2), new SqlParameter("palletid", palletid), new SqlParameter("sn", WMSTwoTask.ProductSN)); //串号写入托盘
                                    iStage = 1;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion
                            #region 第一工位有托盘上升沿（1）
                            if (iStage == 1)
                            {
                                string firstEmpty = OpcUaHelper.R_AssemblyFirstEmtpy_Method(ua);//环线第一工位空true代表空，false代表有东西
                                if (firstEmpty == "True")
                                {
                                    strmsg = $"托盘号{palletid}物料更新完成，成品串号为{WMSTwoTask.ProductSN}";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 0;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion



                            Thread.Sleep(200);
                        }
                    }
                    catch (Exception ex)
                    {
                        strmsg = "Error: " + ex.Message + " 等待一会儿再试!";
                        formmain.logToView(strmsg);
                        log.Info(strmsg);
                        iTimeWait = 1;
                        continue;
                    }
                }
            }

            strmsg = "AssemblyTask线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            return;
        }
    }
}
