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
    public static class C2_BTask
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("C2_BTask");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        public static IntPtr robotConnId;
        static int iStage = 0;                                                                      //状态机
        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskC2_BFunc());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskC2_BFunc()
        {
            string strmsg = "";
            int iTimeWait = 0;

            //临时变量
            string pencupSN = String.Empty;                                                         //原料串号
            string reqcode = String.Empty;
            string taskcode = String.Empty;
            string robotcode = String.Empty;
            OpcUaClient ua = new OpcUaClient();

            strmsg = "C2_B任务启动线程启动";
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
                            //0：C2点是否可出空托盘
                            //1：C2点可出空托盘
                            //2：C2点agv传输线转动
                            //3：C2点准备位传输线启动
                            //4：货物到位
                            //5：继续任务去B
                            //6：AGV到B点
                            //7：B传送带转动
                            //8: AGV传送带转动,送入WMS1
                            //9: 继续任务（结束任务）
                            //10:AGV任务确实完成
                            //11: B点货物到位

                            #region C2点是否可出空托盘（0）

                            if (iStage == 0 && VarComm.GetVar(conn, "WMS1", "ProductEnable") != "")//暂停情况
                            {
                                string iscanoutC2 = OpcUaHelper.R_CanOutC2_Method(ua);//C2点可出料
                                string ishavePalletC2 = OpcUaHelper.R_SeizeC2_Method(ua);//C2点是否有托盘
                                string arrivedC2 = VarComm.GetVar(conn, "AGV", "ArrivedC2");//AGV车是否在C2                                                         //string canout = OpcUaHelper.R_AllowTakeC2_Method(ua);//C2点准许拿空托盘
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_arrivedC2_sel", new SqlParameter("code", "C2"), new SqlParameter("arrivalcode", "C2"));

                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && arrivedC2=="1"&&iscanoutC2 == "False" && ishavePalletC2 == "True")//AGV车到C2&&C2不能出料&&C2点有空托盘&&CNC加工数量小于4
                                {
                                    //OpcUaHelper.W_AskTakeC2_Method(ua);//C2点是否可出空托盘，暂时没用2022-7-9
                                    strmsg = $"AGV{C2Task.robotCode}到达C2点";
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
                            #region C2点可出空托盘（1）
                            if (iStage == 1)//C2点可出空托盘
                            {
                                string canoutC2 = OpcUaHelper.R_SeizeC2_Method(ua);//C2点是否可出空托盘
                                if (canoutC2 == "True")//准许出料
                                { 
                                    iStage = 2;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region C2点agv传输线转动（2）
                            if (iStage == 2)
                            {
                                if (OpcUaHelper.R_SeizeC2_Method(ua) == "True")//C2点准备位有货就开始转动
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltcmd", new SqlParameter("robotcode", C2Task.robotCode), new SqlParameter("beltcmd", 1));//装货指令
                                    strmsg = $"C2点申请AGV{C2Task.robotCode}传动带转动";
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

                            #region C2点准备位传输线启动（3）
                            if (iStage == 3)
                            {
                                //VarComm.SetVar(conn, "CNC", "R_CanOutC2", "");//C2点可出料
                                //VarComm.SetVar(conn, "CNC", "SNInC2", productSN);//写入变量表，C2点笔筒SN
                                //VarComm.SetVar(conn, "CNC", "ProductSN", productSN);//写入变量表，CNC产品SN
                                OpcUaHelper.W_BeltRunC2_Method(ua);//基本不需要做判断
                                strmsg = "传动带C2转动";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 4;
                            }
                            #endregion
                            #region 货物到位（4）
                            if (iStage == 4)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", C2Task.robotCode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0&& ds.Tables[0].Rows[0]["beltstat"].ToString()!="")
                                {
                                    int stat = (int)ds.Tables[0].Rows[0]["beltstat"];
                                    if (stat == 1)
                                    {
                                        //VarComm.SetVar(conn, "AGV", "ArrivedC2Roll", "");//A点小车传送带转动
                                        OpcUaHelper.W_AGVArriveC2_Method(ua);//告诉plc货物已经到agv上
                                        VarComm.SetVar(conn, "AGV", "ContinueC2", "1");//小车A点继续任务
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setSN", new SqlParameter("robotcode", C2Task.robotCode), new SqlParameter("productSN", pencupSN));//更新agv上的产品
                                        Tracking.Insert(conn, "AGV", "C2点装空托盘完成", pencupSN);//串号更新
                                        strmsg = $"C2点AGV{C2Task.robotCode}装空托盘完成";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iStage = 5;
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

                            #region  继续任务去B（5）
                            if (iStage == 5)
                            {
                                if (VarComm.GetVar(conn, "AGV", "ContinueC2") != "")
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_continueC2_upd", new SqlParameter("code", "C2B"), new SqlParameter("subcode", "B"));//继续C2到B任务
                                    VarComm.SetVar(conn, "AGV", "ArrivedC2", "");//下车不在A点，已离开
                                    VarComm.SetVar(conn, "AGV", "RobotCodeC2", "");
                                    strmsg = "小车离开C2点继续去B点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 6;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion

                            #region AGV到B点（6）
                            if (iStage == 6)
                            {
                                string arrivalcode = "B";
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_arrivedfinished_sel",new SqlParameter("code", "C2B"), new SqlParameter("taskcode", C2Task.taskCode), new SqlParameter("arrivalcode", arrivalcode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    strmsg = "小车已到B点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                    //           SQLHelper.ModelToParameterList(data).ToArray());//保存AGV任务数据到数据库
                                    VarComm.SetVar(conn, "AGV", "RobotCodeB", C2Task.robotCode);
                                    VarComm.SetVar(conn, "AGV", "ContinueC2", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedB", "1");
                                    iStage = 7;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region B传送带转动（7）
                            if (iStage == 7)
                            {
                                string isArrivedB = OpcUaHelper.R_SeizeB_Method(ua);//B点货物到位
                                if (isArrivedB=="False")
                                {
                                    OpcUaHelper.W_BeltRunB_Method(ua);//传送带转动
                                    strmsg = "B点传送带转动";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 8;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }

                            }
                            #endregion
                            #region AGV传送带转动,送入WMS1（8）
                            if (iStage == 8)
                            {
                                SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltcmd", new SqlParameter("robotcode", C2Task.robotCode), new SqlParameter("beltcmd", 2));//卸货指令
                                strmsg = "B点AGV传送带转动卸货";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 9;
                            }
                            #endregion
                            #region 继续任务（结束任务）（9）
                            if (iStage == 9)
                            {
                                int stat = 0;
                                string isArrivedB = OpcUaHelper.R_SeizeB_Method(ua);//B点货物到位
                                //是否装货完成
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", C2Task.robotCode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0&& ds.Tables[0].Rows[0]["beltstat"].ToString() != ""&& isArrivedB=="True")
                                {
                                    stat = (int)ds.Tables[0].Rows[0]["beltstat"];
                                    if (stat == 2)
                                    {
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_cancel_upd", new SqlParameter("code", "C2B"), new SqlParameter("taskcode", C2Task.taskCode));//继续A到C1任务
                                        VarComm.SetVar(conn, "AGV", "ArrivedB", "");//小车不在B点，已离开
                                        VarComm.SetVar(conn, "AGV", "RobotCodeB", "");//小车不在B点，已离开
                                        strmsg = $"AGV{C2Task.robotCode}离开B点结束任务";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);

                                        iStage = 10;
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
                            #region AGV任务确实完成（10）
                            if (iStage == 10)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_cancelfinished_sel", new SqlParameter("code", "C2B"));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["applytime"].ToString() == "" && ds.Tables[0].Rows[0]["applyfinished"].ToString() == "" && ds.Tables[0].Rows[0]["robotcode"].ToString() == "")
                                {
                                    strmsg = "AGV:C2B任务完成";
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
                            #endregion
                            #region B点货物到位（11）
                            if (iStage == 11)
                            {
                                string isArrivedB = OpcUaHelper.R_SeizeB_Method(ua);//B点货物到位
                                if (isArrivedB == "True")
                                {
                                    Tracking.Insert(conn, "WMS1", "B点空托盘到位", pencupSN);//写入追踪表
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setSN", new SqlParameter("robotcode", C2Task.robotCode), new SqlParameter("productSN", ""));//清空车上产品串号
                                    strmsg = "B点空托盘到位";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
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
            strmsg = "C2_B线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            return;
        }
    }
}

