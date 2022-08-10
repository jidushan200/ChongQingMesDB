using CommonClass;
using OpcUaHelper;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChongQingControlServer.Option2
{
    public static class C2_DTask
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("C2_DTask");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        public static IntPtr robotConnId;
        public static bool bProductEnable = false;                                                  //生产使能标志
        public static int iStage = 0;                                                                      //状态机

        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskC2_DFunc());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskC2_DFunc()
        {
            string strmsg = "";
            int iTimeWait = 0;

            //临时变量
            int id = 0;                                                                             //订单id
            string ordernumber = String.Empty;                                                      //订单编号
            string pencupSN = String.Empty;                                                         //原料串号
            string reqcode = String.Empty;
            string productSN = String.Empty;
            OpcUaClient ua = new OpcUaClient();                                                      //产品串号
            strmsg = "C2_D1任务启动线程启动";
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
                            //0：C2点是否可出空托盘（0）
                            //1：C2点agv传输线转动（1）
                            //2：C2点准备位传输线启动（2）
                            //3：货物到位
                            //4：继续任务去D1
                            //5：AGV到D1点
                            //6：D1传送带转动
                            //7：AGV传送带转动
                            //8: 继续任务（结束任务）
                            //9: AGV任务确实完成
                            //10: D1点货物到位

                            #region C2点是否可出空托盘（0）
                            if (iStage == 0 && VarComm.GetVar(conn, "WMS1", "ProductEnable") != "")//暂停情况
                            {
                                string iscanoutC2 = OpcUaHelper.R_CanOutC2_Method(ua);//C2点可出料
                                //string ishavePalletC2 = OpcUaHelper.R_SeizeC2_Method(ua);//C2点是否有托盘
                                //string canout = OpcUaHelper.R_AllowTakeC2_Method(ua);//C2点准许拿空托盘
                                string arrivedC2 = VarComm.GetVar(conn, "AGV", "ArrivedC2");//AGV车是否在C2
                                if (arrivedC2 == "1" && iscanoutC2 == "True")//AGV车到C2&&C2可以出料&&C2点有空托盘
                                {
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
                            #region C2点agv传输线转动（1）
                            if (iStage == 1)
                            {
                                SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltcmd", new SqlParameter("robotcode", C2Task.robotCode), new SqlParameter("beltcmd", 1));//装货指令
                                strmsg = $"C2点申请AGV{C2Task.robotCode}传动带转动";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 2;
                            }
                            #endregion

                            #region C2点准备位传输线启动（2）
                            if (iStage == 2)
                            {
                                OpcUaHelper.W_BeltRunC2_Method(ua);//基本不需要做判断
                                strmsg = $"C2点收到笔筒SN:{productSN}";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                strmsg = "传动带C2转动";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 3;
                            }
                            #endregion
                            #region 货物到位（3）
                            if (iStage == 3)
                            {
                                int stat = 0;
                                //string isInFinished = VarComm.GetAgvVar(conn, robotIP);//是否装货完成 改
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", C2Task.robotCode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["beltstat"].ToString() != "")
                                {
                                    stat = (int)ds.Tables[0].Rows[0]["beltstat"];
                                    if (stat == 1)
                                    {
                                        //VarComm.SetVar(conn, "AGV", "ArrivedC2Roll", "");//A点小车传送带转动
                                        VarComm.SetVar(conn, "AGV", "ContinueC2", "1");//小车A点继续任务
                                        OpcUaHelper.W_AGVArriveC2_Method(ua);//告诉plc货物已经到agv上
                                        productSN = OpcUaHelper.R_MaterialC2_Method(ua);//从plc获取笔筒串号
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setSN", new SqlParameter("robotcode", C2Task.robotCode), new SqlParameter("productSN", productSN));//更新agv上的产品
                                        Tracking.Insert(conn, "AGV", "C2点装货完成", productSN);//串号更新
                                        strmsg = $"C2点AGV{C2Task.robotCode}装货完成，串号{productSN}";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iStage = 4;
                                        continue;
                                    }
                                    else
                                    {
                                        iTimeWait = 2;
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

                            #region  继续任务去D1（4）
                            if (iStage == 4)
                            {
                                //查询AGV继续信号
                                string canIncomeD1=OpcUaHelper.R_CanIncomeD1_Method(ua);
                                if (VarComm.GetVar(conn, "AGV", "ContinueC2") != ""&& canIncomeD1=="True")//c2任务继续&&D1可进入
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_continueC2_upd", new SqlParameter("code", "C2D1"), new SqlParameter("subcode", "D1"));//继续C2到B任务
                                    VarComm.SetVar(conn, "AGV", "ArrivedC2", "");//下车不在A点，已离开
                                    VarComm.SetVar(conn, "AGV", "RobotCodeC2", "");
                                    strmsg = "AGV继续去D1点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 5;
                                    continue;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion

                            #region AGV到D1点（5）
                            if (iStage == 5)
                            {
                                string arrivalcode = "D1";
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_arrivedfinished_sel", new SqlParameter("code", "C2D1"), new SqlParameter("taskcode", C2Task.taskCode), new SqlParameter("arrivalcode", arrivalcode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 )
                                {
                                    strmsg = "小车已到D1点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                    //           SQLHelper.ModelToParameterList(data).ToArray());//保存AGV任务数据到数据库
                                    VarComm.SetVar(conn, "AGV", "RobotCodeD1", C2Task.robotCode);
                                    VarComm.SetVar(conn, "AGV", "ContinueC2", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedD1", "1");
                                    iStage = 6;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region D1传送带转动（6）
                            if (iStage == 6)
                            {
                                string sn = string.Empty;
                                OpcUaHelper.W_BeltRunD1_Method(ua);//传送带转动
                                strmsg = "D1点传送带转动";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", C2Task.robotCode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                     sn = ds.Tables[0].Rows[0]["productSN"].ToString();
                                    pencupSN = sn;
                                }
                                OpcUaHelper.W_MaterialD1_Method(ua, sn);//告诉plc 笔筒串号
                                strmsg = $"D1点串号写入{sn}";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                VarComm.SetVar(conn, "WMS2", "PenCupSN", sn);//串号存入数据库
                                iStage = 7;
                            }
                            #endregion
                            #region AGV传送带转动（7）
                            if (iStage == 7)
                            {
                                SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltcmd", new SqlParameter("robotcode", C2Task.robotCode), new SqlParameter("beltcmd", 2));//卸货指令
                                strmsg = $"D1点AGV{C2Task.robotCode}传送带转动卸货";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 8;
                            }
                            #endregion
                            #region 继续任务（结束任务）（8）
                            if (iStage == 8)
                            {
                                int stat = 0;
                                string isArrivedD1 = OpcUaHelper.R_BeltArriveD1_Method(ua);//D1点货物到位
                                //是否装货完成
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", C2Task.robotCode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["beltstat"].ToString() != "")
                                {
                                    stat = (int)ds.Tables[0].Rows[0]["beltstat"];
                                    if (stat == 2 && isArrivedD1 == "True")
                                    {
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_cancel_upd", new SqlParameter("code", "C2D1"), new SqlParameter("taskcode", C2Task.taskCode));//继续A到C1任务
                                        VarComm.SetVar(conn, "AGV", "ArrivedD1", "");//小车不在D1点，已离开
                                        VarComm.SetVar(conn, "AGV", "RobotCodeD1", "");//小车不在D1点，已离开
                                        strmsg = $"AGV{C2Task.robotCode}离开D1点结束任务";
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
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region AGV任务确实完成（9）
                            if (iStage == 9)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_cancelfinished_sel", new SqlParameter("code", "C2D1"));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["applytime"].ToString() == "" && ds.Tables[0].Rows[0]["applyfinished"].ToString() == "" && ds.Tables[0].Rows[0]["robotcode"].ToString() == "")
                                {
                                    strmsg = "AGV:C2D任务完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setSN", new SqlParameter("robotcode", C2Task.robotCode), new SqlParameter("productSN", "0"));//清空agv上的产品
                                    iStage = 10;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion

                            #region D1点货物到位（10）
                            if (iStage == 10)
                            {
                                string isArrivedD1 = OpcUaHelper.R_BeltArriveD1_Method(ua);//D1点货物到位
                                if (isArrivedD1 == "True")
                                {
                                    Tracking.Insert(conn, "WMS2", "D1点货物到位", pencupSN);//写入追踪表
                                    VarComm.SetVar(conn, "AGV", "RobotCodeD1", "");//清空AGV车号
                                    OpcUaHelper.W_GetD1Arrived_Method(ua,true);//告诉plc收到D1货物到位信号，可以清除了
                                    Thread.Sleep(2000);
                                    OpcUaHelper.W_GetD1Arrived_Method(ua, false);//可以清除信号
                                    strmsg = "D1货物到位已知道";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setSN", new SqlParameter("robotcode", C2Task.robotCode), new SqlParameter("productSN", ""));//清空车上产品串号
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_ordercnc_update", new SqlParameter("type", 2));//加CNC完成数
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_running_finish", new SqlParameter("id", 1));//更新APP完成数(更新running表运行时戳 id=1:cnc,id=2:WMS2)
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
            strmsg = "C2_D1线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            return;
        }
    }
}

