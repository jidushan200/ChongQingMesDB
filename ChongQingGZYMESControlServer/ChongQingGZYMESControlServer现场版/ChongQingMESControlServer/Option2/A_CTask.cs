using CommonClass;
using OpcUaHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChongQingControlServer.Option2
{
    static public class A_CTask
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("A_CTask");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        public static AgvTaskData TaskDataA_C1 = new AgvTaskData();                                     //任务数据
        public static int iRunCnt = 0;
        //static bool bProductRunning = false;                                                       //生产状态
        //static bool bProductEnable = false;                                                        //生产使能标志
        public static string robotIP = string.Empty;
        public static string robotCode = string.Empty;
        public static IntPtr robotConnId;
        public static List<AGVInform> agvInfromList = new List<AGVInform>();                        //存储agv信息数据结构
        public static int iStage = 0;                                                                      //状态机
        //public static AutoResetEvent lckTaskData = new AutoResetEvent(true);                      //任务变量锁
        public static bool bProductEnable = false;                                                 //生产使能标志


        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskA_CFunc());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskA_CFunc()
        {
            string strmsg = "";
            int iTimeWait = 0;

            //临时变量
            int id = 0;                                                                             //订单id
            string ordernumber = String.Empty;                                                      //订单编号
            string pencupSN = String.Empty;                                                         //原料串号
            string reqcode = String.Empty;
            string taskcode = String.Empty;
            OpcUaClient ua = new OpcUaClient();

            strmsg = "A_C任务启动线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //初始化数据
            iTimeWait = 0;
            bRunningFlag = true;                                                                    //设置A-C任务线程运行标志
            while (true)
            {
                if (bStop  && iStage == 0 && !WMSOneTask.bRunningFlag)                                 //结束线程
                    break;

                //延时等待
                if (iTimeWait > 0)
                {
                    iTimeWait--;
                    Thread.Sleep(1000);
                    continue;
                }
                //加opc连接
                using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnString))
                {
                    try
                    {
                        conn.Open();
                        var t1 = ua.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
                        Task.WaitAll(t1);
                        while (true)
                        {
                            if (bStop  && iStage == 0 && !WMSOneTask.bRunningFlag)                     //结束线程
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //状态机：
                            //0：申请任务（0）
                            //1：申请任务成功（1）
                            //2：AGV车A点到位,包含车号（2）
                            //3：A点agv传输线转动（3）
                            //4：A点准备位传输线启动（4）
                            //5：货物到位（5）
                            //6：继续任务去B（6）
                            //7：AGV到B点（7）
                            //8: 继续任务去C1（8）
                            //9 :AGV到C1点（9）
                            //10:C1传送带转动（10）
                            //11:AGV传送带转动,送入CNC（11）
                            //12:AGV继续任务（结束任务）（12）
                            //13:AGV任务确认完成（13）
                            //14:C1点货物到位（14）
                            ///////////如果慢的话，新加一个AC任务出库锁///////////
                            #region 申请任务（0）
                            if (iStage == 0 && VarComm.GetVar(conn, "WMS1", "ProductEnable") != "")
                            {
                                string applyAGV=VarComm.GetVar(conn, "WMS1", "ApplyAGV");//申请AGV小车
                                if (applyAGV == "1")
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_apply_upd", new SqlParameter("code", "AC1"), new SqlParameter("subcode", "A"));//申请A到C1任务
                                    iStage = 1;
                                }else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                         
                            #region 申请任务成功（1）
                            if (iStage == 1)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_applyfinished_sel", new SqlParameter("code", "AC1"));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    taskcode = ds.Tables[0].Rows[0]["taskcode"].ToString();
                                    VarComm.SetVar(conn, "WMS1", "ApplyAGV", "");//申请AGV小车
                                    iStage = 2;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion

                            #region AGV车A点到位,包含车号（2）
                            if (iStage == 2)
                            {
                                string arrivalcode = "A";
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_arrivedfinished_sel", new SqlParameter("code", "AC1"), new SqlParameter("taskcode", taskcode), new SqlParameter("arrivalcode", arrivalcode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    robotCode = ds.Tables[0].Rows[0]["robotcode"].ToString();
                                    //string arrivalcode=ds.Tables[0].Rows[0]["arrivalcode"].ToString();
                                    strmsg = String.Format("AGV车:{0}A点到位", robotCode);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    //设置交互变量
                                    VarComm.SetVar(conn, "AGV", "RobotCodeA", robotCode);
                                    VarComm.SetVar(conn, "AGV", "ContinueA", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedA", "1");
                                    iStage = 3;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }

                            #endregion
                            #region A点agv传输线转动（3）
                            if (iStage == 3)
                            {
                                if (OpcUaHelper.R_SeizeA_Method(ua) == "True")//a点准备位有货就开始转动 改
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltcmd", new SqlParameter("robotcode", robotCode), new SqlParameter("beltcmd", 1));//装货指令
                                    strmsg = "AGV传动带转动接货";
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

                            #region A点准备位传输线启动（4）
                            if (iStage == 4)
                            {
                                OpcUaHelper.W_BeltRunA_Method(ua, true);//基本不需要做判断
                                strmsg = "传动带A转动";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 5;
                            }
                            #endregion

                            #region 货物到位（5）
                            if (iStage == 5)
                            {
                                int stat = 0;
                                //string isInFinished = VarComm.GetAgvVar(conn, robotIP);//是否装货完成 改
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", robotCode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["beltstat"].ToString() != "")
                                {
                                    stat = (int)ds.Tables[0].Rows[0]["beltstat"];
                                    if (stat == 1)
                                    {
                                        //VarComm.SetVar(conn, "AGV", "ArrivedARoll", "");//A点小车传送带转动
                                        VarComm.SetVar(conn, "AGV", "ContinueA", "1");//小车A点继续任务
                                        OpcUaHelper.W_BeltRunA_Method(ua,false);//传送带停止
                                        pencupSN = VarComm.GetVar(conn, "WMS1", "ProductSN"); //获取WMS1的产品串号
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setSN", new SqlParameter("robotcode", robotCode), new SqlParameter("productSN", pencupSN));//更新agv上的产品
                                        Tracking.Insert(conn, "AGV", "A点装货完成", pencupSN);//串号更新
                                        strmsg = $"A点AGV{robotCode}装货完成,串号{pencupSN}";
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
                            }
                            #endregion
                            #region  继续任务去C1（6）
                            if (iStage == 6)
                            {
                                if (VarComm.GetVar(conn, "AGV", "ContinueA") != "") //C1点可进入&&查询AGV继续信号
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_continue_upd", new SqlParameter("code", "AC1"), new SqlParameter("taskcode", taskcode), new SqlParameter("subcode", "B"));//继续A到B任务(为了避让加去B点)
                                    VarComm.SetVar(conn, "AGV", "ArrivedA", "");//下车不在A点，已离开
                                    VarComm.SetVar(conn, "AGV", "RobotCodeA", "");
                                    strmsg = "小车离开A点继续去B点";
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
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_arrivedfinished_sel", new SqlParameter("code", "AC1"), new SqlParameter("taskcode", taskcode), new SqlParameter("arrivalcode", "B"));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    strmsg = "小车已到B点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                    //           SQLHelper.ModelToParameterList(data).ToArray());//保存AGV任务数据到数据库(把小车号和callback时间写入数据库)
                                    VarComm.SetVar(conn, "AGV", "RobotCodeB", robotCode);
                                    VarComm.SetVar(conn, "AGV", "ArrivedB", "1");
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_continueAB_upd", new SqlParameter("code", "AC1"), new SqlParameter("taskcode", taskcode));//清空继续任务，让小车能去C1点AC1任务专用
                                    iStage = 8;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            #endregion

                            #region  继续任务去C1（8）
                            if (iStage == 8)
                            {
                                string canComeIn = OpcUaHelper.R_CanIncomC1_Method(ua);
                                if (VarComm.GetVar(conn, "AGV", "ContinueA") != "" && canComeIn == "True") //C1点可进入&&查询AGV继续信号
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_continue_upd", new SqlParameter("code", "AC1"), new SqlParameter("taskcode", taskcode), new SqlParameter("subcode", "C1"));//继续A到C1任务
                                    VarComm.SetVar(conn, "AGV", "ArrivedA", "");//下车不在A点，已离开
                                    VarComm.SetVar(conn, "AGV", "RobotCodeA", "");
                                    strmsg = "小车离开B点继续去C2点";
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

                            #region AGV到C1点（9）
                            if (iStage == 9)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_arrivedfinished_sel", new SqlParameter("code", "AC1"), new SqlParameter("taskcode", taskcode), new SqlParameter("arrivalcode", "C1"));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    strmsg = "小车已到C1点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                    //           SQLHelper.ModelToParameterList(data).ToArray());//保存AGV任务数据到数据库(把小车号和callback时间写入数据库)
                                    VarComm.SetVar(conn, "AGV", "RobotCodeC1", robotCode);
                                    VarComm.SetVar(conn, "AGV", "ContinueA", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedC1", "1");
                                    iStage = 10;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            #endregion
                            #region C1传送带转动（10）
                            if (iStage == 10)
                            {
                                
                                OpcUaHelper.W_BeltRunC1_Method(ua);//传送带转动
                                if (pencupSN == "EmptyPallet")
                                {
                                    OpcUaHelper.W_InEmptyPallet_Method(ua);//告诉CNC空托盘
                                }
                                else
                                {
                                    OpcUaHelper.W_MaterialC1_Method(ua, pencupSN);//写入CNC  
                                }
                                strmsg = "C1点传送带转动接货";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 11;
                            }
                            #endregion
                            #region AGV传送带转动,送入CNC（11）
                            if (iStage == 11)
                            {
                                SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltcmd", new SqlParameter("robotcode", robotCode), new SqlParameter("beltcmd", 2));//卸货指令
                                strmsg = $"C1点AGV{robotCode}传送带转动卸货";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 12;
                            }
                            #endregion
                            #region AGV继续任务（结束任务）（12）
                            if (iStage == 12)
                            {
                                int stat = 0;
                                //是否装货完成
                                string isSeizeC1 = OpcUaHelper.R_BeltArriveC1_Method(ua);
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", robotCode));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["beltstat"].ToString() != "")
                                {
                                    stat = (int)ds.Tables[0].Rows[0]["beltstat"];
                                    if (stat == 2 && isSeizeC1 == "True")//C1货物到位有时会读不到，通过C1不可进入补救
                                    {
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_cancel_upd", new SqlParameter("code", "AC1"), new SqlParameter("taskcode", taskcode));//继续A到C1任务
                                        VarComm.SetVar(conn, "AGV", "ArrivedC1", "");//小车不在A点，已离开
                                        VarComm.SetVar(conn, "AGV", "RobotCodeC1", "");//小车不在A点，已离开
                                        strmsg = $"AGV{robotCode}离开C1点结束任务";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iStage = 13;
                                    }
                                    else
                                    {
                                        iTimeWait = 1;
                                        continue;
                                    }
                                }
                            }
                            #endregion
                            #region AGV任务确认完成（13）
                            if (iStage == 13)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_cancelfinished_sel", new SqlParameter("code", "AC1"));
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count>0&& ds.Tables[0].Rows[0]["applytime"].ToString()=="" && ds.Tables[0].Rows[0]["applyfinished"].ToString() == ""&& ds.Tables[0].Rows[0]["robotcode"].ToString() == "")
                                {

                                    strmsg = "AGV:AC1任务完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 14;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region C1点货物到位（14）
                            if (iStage == 14)
                            {
                                string isArrivedC1 = OpcUaHelper.R_BeltArriveC1_Method(ua);//C1货物到位||
                                if (isArrivedC1 == "True")
                                {
                                    OpcUaHelper.W_GetC1Arrived_Method(ua,true);//告诉plc收到C1货物到位信号，可以清除了
                                    Thread.Sleep(2000);
                                    OpcUaHelper.W_GetC1Arrived_Method(ua, false);//清除信号

                                    strmsg = "C1货物到位已知道";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    Tracking.Insert(conn, "CNC", "C1点货物到位", pencupSN);//写入追踪表                                                
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setSN", new SqlParameter("robotcode", robotCode), new SqlParameter("productSN", ""));//清空车上产品串号
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
            strmsg = "A_CTask线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置AGV主线程停止标志
            return;
        }

    }
}
