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
    public static class C2Task
    {

        static log4net.ILog log = log4net.LogManager.GetLogger("C2Task");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        public static string robotIP = string.Empty;
        public static string robotCode = string.Empty;
        public static IntPtr robotConnId;
        public static string taskCode;                                                              //任务需要保持
        //public static readonly object ObjC2_B = new object();
        //public static bool C2_BlockFlag = false;                                                           //锁定C2_B标识
        static int iStage = 0;                                                                      //状态机

        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskC2Func());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskC2Func()
        {
            string strmsg = "";
            int iTimeWait = 0;

            //临时变量
            string ordernumber = String.Empty;                                                      //订单编号
            string pencupSN = String.Empty;                                                         //原料串号
            string reqcode = String.Empty;
            string robotcode = String.Empty;
            OpcUaClient ua = new OpcUaClient();

            strmsg = "C2任务启动线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //初始化数据
            iTimeWait = 0;
            bRunningFlag = true;                                                                    //设置C2-B任务线程运行标志
            while (true)
            {
                if (bStop && !WMSOneTask.bRunningFlag && iStage == 0)                                 //结束线程
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
                            if (bStop && !WMSOneTask.bRunningFlag && iStage == 0)                     //结束线程
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //状态机：
                            //0：C2点是否可下发任务
                            //1：申请任务C2任务成功
                            //2：AGV车C2点到位
                            //3：检查小车是否离开C2


                            #region C2点是否可下发任务（0）
                            if (iStage == 0 && VarComm.GetVar(conn, "WMS1", "ProductEnable") != "")//暂停情况
                            {
                                string iscanoutC2 = OpcUaHelper.R_CanOutC2_Method(ua);//C2点可出料
                                string ishavePalletC2 = OpcUaHelper.R_SeizeC2_Method(ua);//C2点是否有托盘
                                if (iscanoutC2 == "True"||ishavePalletC2=="True")//C2点是空托盘&&加工物料数量小于4,后期可能有问题
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_apply_upd", new SqlParameter("code", "C2"), new SqlParameter("subcode", "C2"));//申请任务
                                    strmsg = "申请AGV到C2点任务";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 1;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region 申请任务C2任务成功（1）
                            if (iStage == 1)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_applyfinished_sel", new SqlParameter("code", "C2"));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    taskCode = ds.Tables[0].Rows[0]["taskcode"].ToString();
                                    iStage = 2;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region AGV车C2点到位（2）
                            if (iStage == 2)
                            {
                                string arrivalcode = "C2";
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_arrivedfinished_sel", new SqlParameter("code", "C2"), new SqlParameter("taskcode", taskCode), new SqlParameter("arrivalcode", arrivalcode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 )
                                {
                                    robotCode = ds.Tables[0].Rows[0]["robotcode"].ToString();
                                    VarComm.SetVar(conn, "AGV", "RobotCodeC2", robotCode);
                                    VarComm.SetVar(conn, "AGV", "ContinueC2", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedC2", "1");
                                    strmsg = "AGV到达C2点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 3;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region  检查小车是否离开C2（3）
                            if (iStage == 3)
                            {
                                string robotCodeC2 = VarComm.GetVar(conn, "AGV", "RobotCodeC2");//C2点车补号
                                string arrivedC2 = VarComm.GetVar(conn, "AGV", "ArrivedC2");//AGV车是否在C2
                                if (robotCodeC2 == "" && arrivedC2 == "")//小车已经离开
                                {
                                    strmsg = "C2点可继续下发AGV任务";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 0;//继续看是否可以下发任务
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
            //try
            //{
            //    AGVBeltControl.tcpPackServer.Disconnect(robotConnId, true);
            //    strmsg = "关闭AGV TCP服务成功";
            //    formmain.logToView(strmsg);
            //    log.Info(strmsg);
            //}
            //catch (Exception ex)
            //{
            //    strmsg = "关闭AGV TCP服务失败：" + ex.Message;
            //    formmain.logToView(strmsg);
            //    log.Error(strmsg);
            //}
            strmsg = "C2线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            return;
        }
    }
}
