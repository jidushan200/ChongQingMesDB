using CommonClass;
using OpcUaHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChongQingControlServer.Option2
{
    public static class D2_BTask
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("D2_BTask");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        public static string robotCode = string.Empty;
        public static IntPtr robotConnId;
        public static bool bProductEnable;
        static int iStage = 0;                                                                      //状态机
        public static bool inFalg = false;                                                          //设置入库状态标志

        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskD2_BFunc());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskD2_BFunc()
        {
            string strmsg = "";
            int iTimeWait = 0;

            //临时变量
            string pencupSN = String.Empty;                                                         //原料串号
            string reqcode = String.Empty;
            string taskcode = String.Empty;
            string robotcode = String.Empty;
            string productSN = String.Empty;                                                        //产品串号
            OpcUaClient ua = new OpcUaClient();
            strmsg = "D2_B任务启动线程启动";
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
                            //0：申请D2-B任务
                            //1: 申请任务成功
                            //1：AGV车D2点到位
                            //2：设定交互变量
                            //3：D2点agv传输线转动
                            //4：D2点准备位传输线启动
                            //5：货物到位
                            //6：继续任务去B
                            //7：AGV到B点
                            //8：B点传送带转动
                            //9：AGV传送带转动
                            //10：继续任务（结束任务)
                            //11：AGV任务确认完成（11）
                            //12：B点货物到位（12）

                            #region 申请D2-B任务（0）
                            if (iStage == 0 && VarComm.GetVar(conn, "WMS1", "ProductEnable") != "")
                            {
                                string ishavePalletD2 = OpcUaHelper.R_SeizeD2_Method(ua);//D2点是否有托盘
                                if (ishavePalletD2 == "True")//D2有空托盘
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_apply_upd", new SqlParameter("code", "D2B"), new SqlParameter("subcode", "D2"));//申请A到C1任务
                                    iStage = 1;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region 申请任务成功（1）
                            if (iStage == 1)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_applyfinished_sel", new SqlParameter("code", "D2B"));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 )
                                {
                                    taskcode = ds.Tables[0].Rows[0]["taskcode"].ToString();
                                    iStage = 2;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region AGV车D2点到位（2）
                            if (iStage == 2)
                            {
                                string arrivalcode = "D2";
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_arrivedfinished_sel", new SqlParameter("code", "D2B"), new SqlParameter("taskcode", taskcode), new SqlParameter("arrivalcode", arrivalcode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    robotCode = ds.Tables[0].Rows[0]["robotcode"].ToString();
                                    //string arrivalcode=ds.Tables[0].Rows[0]["arrivalcode"].ToString();
                                    strmsg = String.Format("AGV车:{0}D2点到位", robotCode);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    //设置交互变量
                                    VarComm.SetVar(conn, "AGV", "RobotCodeD2", robotCode);
                                    VarComm.SetVar(conn, "AGV", "ContinueD2", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedD2", "1");
                                    iStage = 3;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region D2点agv传输线转动（3）
                            if (iStage == 3)
                            {
                                if (OpcUaHelper.R_SeizeD2_Method(ua) == "True")//D2点准备位有货就开始转动
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltcmd", new SqlParameter("robotcode", robotCode), new SqlParameter("beltcmd", 1));//装货指令
                                    strmsg = $"D2点申请AGV{C2Task.robotCode}传动带转动";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 4;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }

                            }
                            #endregion

                            #region D2点准备位传输线启动（4）
                            if (iStage == 4)
                            {
                                productSN = "empty";
                                OpcUaHelper.W_BeltRunD2_Method(ua);//D2点传送带转动
                                strmsg = "传动带D2转动";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 5;
                            }
                            #endregion
                            #region 货物到位（5）
                            if (iStage == 5)
                            {
                                int stat = 0;
                                pencupSN = "";
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", robotCode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0&& ds.Tables[0].Rows[0]["beltstat"].ToString()!="")
                                {
                                    stat = (int)ds.Tables[0].Rows[0]["beltstat"];
                                    if (stat == 1)
                                    {
                                        //VarComm.SetVar(conn, "AGV", "ArrivedD2Roll", "");//D2点传送带转动标志清空
                                        VarComm.SetVar(conn, "AGV", "ContinueD2", "1");//小车D2点继续任务
                                        OpcUaHelper.W_AGVArriveD2_Method(ua);//告诉plc货物已经到agv上
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setSN", new SqlParameter("robotcode", robotCode), new SqlParameter("productSN", pencupSN));//更新agv上的产品
                                        Tracking.Insert(conn, "AGV", "D2点装空托盘完成", pencupSN);//串号更新
                                        iStage = 6;
                                    }
                                    else
                                    {
                                        iTimeWait = 1;
                                        continue;
                                    }
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }

                            }
                            #endregion

                            #region  继续任务去B（6）
                            if (iStage == 6)
                            {
                                //查询AGV继续信号
                                if (VarComm.GetVar(conn, "AGV", "ContinueD2") != "")
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_continue_upd", new SqlParameter("code", "D2B"), new SqlParameter("taskcode", taskcode),new SqlParameter("subcode", "B"));//继续A到C1任务

                                    VarComm.SetVar(conn, "AGV", "ArrivedD2", "");//下车不在D2点，已离开
                                    VarComm.SetVar(conn, "AGV", "RobotCodeD2", "");//下车不在D2点，已离开
                                    strmsg = "小车离开D2点继续去B点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 7;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }

                            }
                            #endregion

                            #region AGV到B点（7）
                            if (iStage == 7)
                            {
                                string arrivalcode = "B";
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_arrivedfinished_sel", new SqlParameter("code", "D2B"), new SqlParameter("taskcode", taskcode), new SqlParameter("arrivalcode", arrivalcode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    strmsg = "小车已到B点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                    //           SQLHelper.ModelToParameterList(data).ToArray());//保存AGV任务数据到数据库
                                    VarComm.SetVar(conn, "AGV", "RobotCodeB", robotCode);
                                    VarComm.SetVar(conn, "AGV", "ContinueD2", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedB", "1");
                                    iStage = 8;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion

                            #region B传送带转动（8）
                            if (iStage == 8)
                            {
                                if(OpcUaHelper.R_SeizeB_Method(ua)=="False")//B点无托盘
                                {
                                    OpcUaHelper.W_BeltRunB_Method(ua);//传送带转动
                                    strmsg = "B点传送带转动";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 9;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }

                            }
                            #endregion
                            #region AGV传送带转动（9）
                            if (iStage == 9)
                            {
                                SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltcmd", new SqlParameter("robotcode", robotCode), new SqlParameter("beltcmd", 2));//卸货指令
                                strmsg = "B点AGV传送带转动卸货";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 10;
                            }
                            #endregion
                            #region 继续任务（结束任务）（10）
                            if (iStage == 10)
                            {
                                string isArrivedB = OpcUaHelper.R_SeizeB_Method(ua);
                                int stat = 0;
                                //是否装货完成
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", robotCode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["beltstat"].ToString() != ""&& isArrivedB=="True")
                                {
                                    stat = (int)ds.Tables[0].Rows[0]["beltstat"];
                                    if (stat == 2)
                                    {
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_cancel_upd", new SqlParameter("code", "D2B"), new SqlParameter("taskcode", taskcode));//继续A到C1任务
                                        VarComm.SetVar(conn, "AGV", "ArrivedB", "");//下车不在A点，已离开
                                        VarComm.SetVar(conn, "AGV", "RobotCodeB", "");//下车不在A点，已离开
                                        strmsg = $"AGV{robotCode}离开B点结束任务";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iStage = 11;
                                    }
                                    else
                                    {
                                        iTimeWait = 1;
                                        continue;
                                    }
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region AGV任务确认完成（11）
                            if (iStage == 11)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_cancelfinished_sel", new SqlParameter("code", "D2B"));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["applytime"].ToString() == "" && ds.Tables[0].Rows[0]["applyfinished"].ToString() == "" && ds.Tables[0].Rows[0]["robotcode"].ToString() == "")
                                {
                                    strmsg = "AGV:D2B任务完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 12;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region B点货物到位（12）
                            if (iStage == 12)
                            {
                                string isArrivedB = OpcUaHelper.R_SeizeB_Method(ua);//B点货物到位
                                if (isArrivedB == "True")
                                {
                                    //VarComm.SetVar(conn, "WMS1", "MaterialSN", pencupSN);//写入变量
                                    //Tracking.Insert(conn, "WMS1", "B点到位", pencupSN);//写入追踪表
                                    strmsg = "B点空托盘到位";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    VarComm.SetVar(conn, "AGV", "RobotCodeB", "");//清空AGV车号
                                    VarComm.SetVar(conn, "AGV", "ArrivedB", "");//清空AGV到达B点标识
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setSN", new SqlParameter("robotcode", robotcode), new SqlParameter("productSN", pencupSN));//清空车上产品串号
                                    iStage = 0;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
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

            strmsg = "D2_B线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            return;
        }
    }
}

