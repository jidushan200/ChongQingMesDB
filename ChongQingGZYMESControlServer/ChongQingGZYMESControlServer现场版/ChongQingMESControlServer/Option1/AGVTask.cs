using CommonClass;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChongQingControlServer
{
    public class AGVTask
    {

        static log4net.ILog log = log4net.LogManager.GetLogger("AGVTask");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                               //线程停止信号
        public static bool bRunningFlag = false;                                                        //线程运行标志
        static int iRunCnt = 0;                                                                         //AGV运行数量
        static List<string> arRobots = new List<string>();                                                                   //AGV车号列表
        static bool bDispatchStart = false;                                                             //AGV调度启动标志：true：启动调度（AGV调度停止时自动置为false）
        static bool bDispatchRunning = false;                                                           //AGV调度运行状态：true：正在运行，false：已停止
        static int iTotalCnt = 0;                                                                       //AGV总数量
        static ServiceHost host = null;                                                                 //Web回调主机
        static List<int> listStage = new List<int>();                                                   //AGV调度序号列表
        public static AutoResetEvent lckTaskData = new AutoResetEvent(true);                                //任务变量锁
        public static Dictionary<int, AgvTaskData> dicTaskData = new Dictionary<int, AgvTaskData>();        //任务变量
        static string[] arRobotCodes;                                                                   //AGV调度任务车号
        static string[] arRobotIPs;                                                                     //AGV调度任务车ip
        static IntPtr[] arRobotConnIds;                                                                 //AGV调度任务车连接id
        public static List<AGVInform> agvInfromList = new List<AGVInform>();
        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskAgvFunc());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        static void thTaskAgvFunc()
        {
            string strmsg = "";
            int iTimeWait = 0;
            bool isusingAC1 = false;
            bool isusingC2D1 = false;
            bool isusingD2B = false;
            bool isusingC2B = false;
            string reqcode = String.Empty;
            string taskcode = String.Empty;
            string pencupSN = String.Empty;

            //初始化AGV数据
            strmsg = "AgvControl线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //启动AGV回调接收
            string baseAddress = "http://localhost:8040";//后期需要修改2022-5-23
            try
            {
                AgvCallback ss = new AgvCallback();
                host = new ServiceHost(ss, new Uri(baseAddress));
                host.AddServiceEndpoint(typeof(IAgvCallback), new WebHttpBinding(), "").Behaviors.Add(new WebHttpBehavior());
                host.Open();

                strmsg = "启动AGV回调接收服务成功";
                formmain.logToView(strmsg);
                log.Info(strmsg);
            }
            catch (Exception ex)
            {
                strmsg = "启动AGV回调接收服务失败：" + ex.Message;
                formmain.logToView(strmsg);
                log.Error(strmsg);
                return;
            }


            bRunningFlag = true;                                                                    //设置AGV主线程运行标志
            while (true)
            {
                //停止线程
                if (bStop)
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

                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_comm_init", null);

                        strmsg = "AGV通讯变量初始化完毕";
                        formmain.logToView(strmsg);
                        log.Info(strmsg);

                        DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_sn_list");

                        iTotalCnt = ds.Tables[0].Rows.Count; //获取AGV总数量
                        foreach (DataRow dr1 in ds.Tables[0].Rows)
                        {
                            AGVInform agvs = new AGVInform()
                            {
                                RobotCode = dr1["robotcode"].ToString(),
                                RobotIP = dr1["robotip"].ToString(),
                            };
                            agvInfromList.Add(agvs);//加入到数据list
                        }
                        //arRobots = new string[iTotalCnt];
                        arRobotCodes = new string[iTotalCnt];
                        arRobotIPs = new string[iTotalCnt];
                        arRobotConnIds = new IntPtr[iTotalCnt];
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            arRobots.Add(dr["robotcode"].ToString());
                            listStage.Add(0);                                                       //初始化AGV调度状态机
                        }

                        VarComm.SetVar(conn, "AGV", "TotalCnt", iTotalCnt.ToString());
                        VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());

                        strmsg = "AGV数量：" + iTotalCnt.ToString() + "，AGV列表：" + string.Join(",", arRobots);
                        formmain.logToView(strmsg);
                        log.Info(strmsg);
                        break;
                    }
                    catch (Exception ex)
                    {
                        strmsg = "DB Error: " + ex.Message + " 等待一会儿再试!";
                        formmain.logToView(strmsg);
                        log.Error(strmsg);
                        iTimeWait = 10;
                        continue;
                    }
                }

            }

            try
            {
                TCP_Pack_Server.Init();
                TCP_Pack_Server.Start();
                strmsg = "启动AGV TCP服务成功";
                formmain.logToView(strmsg);
                log.Info(strmsg);
            }
            catch (Exception ex)
            {
                strmsg = "启动AGV TCP服务失败：" + ex.Message;
                formmain.logToView(strmsg);
                log.Error(strmsg);
                return;
            }

            iTimeWait = 0;
            int tn = 0; //调度序号
            bool b;

            while (true)
            {
                //检测调度是否全部没有开始
                b = true;
                for (int i = 0; i < iTotalCnt; i++)
                {
                    if (listStage[i] != 0)
                    {
                        b = false;
                        break;
                    }
                }

                if (bStop && b) //AGV调度任务尚未开始时，可以结束线程
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
                        OpcUaHelper.t1 = OpcUaHelper.ua_wms1.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
                        Task.WaitAll(OpcUaHelper.t1);
                        OpcUaHelper.t2 = OpcUaHelper.ua_CNC.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
                        Task.WaitAll(OpcUaHelper.t2);
                        OpcUaHelper.t3 = OpcUaHelper.ua_wms2.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
                        Task.WaitAll(OpcUaHelper.t3);
                        OpcUaHelper.t4 = OpcUaHelper.ua_LoopLine.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
                        Task.WaitAll(OpcUaHelper.t4);
                        while (true)
                        {
                            //开启AGV调度启动
                            if (!bStop && !bDispatchStart && !bDispatchRunning)
                            {
                                VarComm.SetVar(conn, "AGV", "DispatchRunning", "1");///调度启动标志，在数据库里交互，2022-4-25李
                                VarComm.SetVar(conn, "AGV", "DispatchStart", "1");
                                bDispatchRunning = true;
                                bDispatchStart = true;
                                strmsg = "AGV调度启动...";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                            }

                            //检测调度是否全部没有开始
                            b = true;
                            for (int i = 0; i < iTotalCnt; i++)
                            {
                                if (listStage[i] != 0)
                                {
                                    b = false;
                                    break;
                                }
                            }

                            if (bStop && b)                                                         //AGV调度任务尚未开始时，可以结束线程
                                break;

                            if (!bDispatchRunning)                                                  //尚未开始任务调度，延时等待
                            {
                                Thread.Sleep(1000);
                                continue;
                            }

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            if (tn < iTotalCnt)
                            {
                                #region 启动A_C1任务（0）
                                if (listStage[tn] == 0)//bDispatchRunning2022-6-13改
                                {
                                    if (isusingAC1 == true)                                             //任务占用中
                                    {
                                        listStage[tn] = 30; //启动别的任务
                                    }
                                    else if (isusingAC1 == false && WMS1Task.bProductEnable == true)
                                    {
                                        DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_ordercnc_sel", null);//订单情况
                                        if (isusingAC1 == false && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)//笔筒订单未完成
                                        {
                                            reqcode = Guid.NewGuid().ToString();                        //AGV请求代码
                                            taskcode = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "bas_gencode_req",
                                                                               new SqlParameter("code", "agv_taskcode")).ToString();            //AGV任务代码

                                            //创建新任务ykby
                                            strmsg = "创建AGV任务ykby，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + reqcode + "，taskcode：" + taskcode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            AgvAnswerModel ret = AgvAPI.CreatTask(reqcode, taskcode, "A-C1");//创建A到C1任务
                                            if (ret.code == "0")
                                            {
                                                isusingAC1 = true;
                                                strmsg = "创建AGV任务A-C1成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                    + "，reqcode：" + ret.reqCode;
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);

                                                //生成任务数据
                                                AgvTaskData data = new AgvTaskData();
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "A-C1";//后期需要改
                                                data.robotcode = "";
                                                data.srccode = "";
                                                data.destcode = "A";
                                                data.callbacktime = null;
                                                data.cmd = 1;
                                                data.sendtime = DateTime.Now;

                                                try
                                                {
                                                    lckTaskData.WaitOne();
                                                    dicTaskData[tn] = data;
                                                }
                                                finally
                                                {
                                                    lckTaskData.Set();
                                                }

                                                listStage[tn] = 1;
                                            }
                                            else
                                            {
                                                strmsg = "创建AGV任务ykby失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                    + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);
                                                iTimeWait = 10;
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            listStage[tn] = 30;
                                        }
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（1）
                                if (listStage[tn] == 1)
                                {
                                    strmsg = "AGV任务数据追加到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV任务记录到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        object o = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_task_ins",
                                                    SQLHelper.ModelToParameterList(data).ToArray());
                                        data.id = Convert.ToInt32(o);

                                        strmsg = "AGV任务数据追加到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 2;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 任务已到A点，等待回调（2）
                                if (listStage[tn] == 2)
                                {
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        if (data.callbacktime != null)
                                        {
                                            strmsg = "A-C1任务已到A点，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iRunCnt++;                                              //AGV运行数量加1
                                            listStage[tn] = 3;
                                        }
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（3）
                                if (listStage[tn] == 3)
                                {
                                    strmsg = "AGV任务数据更新到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV回调更新到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        arRobotCodes[tn] = data.robotcode;
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                                SQLHelper.ModelToParameterList(data).ToArray());

                                        strmsg = "AGV任务数据更新到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                            + "，callbacktime：" + data.callbacktime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 4;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region  设定交互变量（4）
                                if (listStage[tn] == 4)
                                {
                                    strmsg = "AGV已到达A点，设定交互变量";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    VarComm.SetVar(conn, "AGV", "RobotCodeA", arRobotCodes[tn]);
                                    //VarComm.SetVar(conn, "WMS1", "RobotCodeA", arRobotCodes[tn]);
                                    VarComm.SetVar(conn, "AGV", "ContinueA", "");
                                    VarComm.SetVar(conn, "AGV", "StopA", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedA", "1");
                                    VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());


                                    listStage[tn] = 5;
                                }
                                #endregion
                                #region 传送带转动（5）
                                if (listStage[tn] == 5)
                                {
                                    if (OpcUaHelper.R_SeizeA_Method() == "True")//a点准备位有货就开始转动
                                    {
                                        //arRobotIPs[tn] = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_id_select",new SqlParameter("robotcode", arRobotCodes[tn])).ToString();  //通过agvCode找到对应的ID
                                        int Index = AGVTask.agvInfromList.Select((s, index) => new { Value = s.RobotCode, Index = index }).Where(t => t.Value == arRobotCodes[tn]).Select(t => t.Index).First(); //通过agvCode找到对应的ID
                                        arRobotIPs[tn] = AGVTask.agvInfromList[Index].RobotIP;
                                        arRobotConnIds[tn] = AGVTask.agvInfromList[Index].RobotConnId;
                                        TCP_Pack_Server.Send(TCP_Pack_Server.str_loading, arRobotConnIds[tn]);//装货指令
                                        VarComm.SetVar(conn, "AGV", "ArrivedARoll", "1");//A点小车传送带转动
                                        listStage[tn] = 6;
                                    }
                                    else
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion
                                #region 装货完成（6）
                                if (listStage[tn] == 6)
                                {
                                    //int isexist = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_tcp_select", new SqlParameter("robotip",arRobotIPs[tn]), new SqlParameter("type", 1));//找到回应的时间戳
                                    string isInFinished = VarComm.GetAgvVar(conn, arRobotIPs[tn]);//是否装货完成
                                    if (isInFinished == "3")
                                    {
                                        VarComm.SetVar(conn, "AGV", "ArrivedARoll", "");//A点小车传送带转动
                                        VarComm.SetVar(conn, "AGV", "ContinueA", "1");//小车A点继续任务
                                        //VarComm.SetVar(conn, "AGV", "ArrivedA", "");//A点小车传送带转动
                                        pencupSN = VarComm.GetVar(conn, "WMS1", "ProductSN"); //获取WMS1的产品串号
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_sn_set", new SqlParameter("agvnumber", arRobotCodes[tn]), new SqlParameter("pencupSN", pencupSN));//更新agv上的产品
                                        Tracking.Insert(conn, "AGV", "A点装货完成", pencupSN);//串号更新
                                        listStage[tn] = 7;
                                    }
                                    else if (isInFinished != "3")
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion

                                #region  继续任务去C1（7）
                                if (listStage[tn] == 7)
                                {
                                    //查询AGV继续信号
                                    if (VarComm.GetVar(conn, "AGV", "ContinueA") != "")
                                        b = true;

                                    if (b)
                                    {
                                        try
                                        {
                                            lckTaskData.WaitOne();
                                            AgvTaskData data = dicTaskData[tn];
                                            taskcode = data.taskcode;
                                        }
                                        finally
                                        {
                                            lckTaskData.Set();
                                        }

                                        reqcode = Guid.NewGuid().ToString();                        //AGV请求代码

                                        //继续任务ykby1
                                        strmsg = "继续AGV任务，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + reqcode + "，taskcode：" + taskcode + "，robotcode：" + arRobotCodes[tn];
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        AgvAnswerModel ret = AgvAPI.ContinueTask(reqcode, taskcode);

                                        if (ret.code == "0")
                                        {
                                            strmsg = "继续AGV任务成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);

                                            //更新任务数据
                                            try
                                            {
                                                lckTaskData.WaitOne();
                                                AgvTaskData data = dicTaskData[tn];
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "A-C1";//后期需要改
                                                data.robotcode = arRobotCodes[tn];
                                                data.srccode = "A";
                                                data.destcode = "C1";
                                                data.callbacktime = null;
                                                data.cmd = 2;
                                                data.sendtime = DateTime.Now;
                                            }
                                            finally
                                            {
                                                lckTaskData.Set();
                                            }
                                            VarComm.SetVar(conn, "AGV", "ArrivedA", "");//下车不在A点，已离开
                                            listStage[tn] = 81;
                                        }
                                        else
                                        {
                                            strmsg = "继续AGV任务失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iTimeWait = 10;
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                #region 保存AGV状态到数据库（81）
                                if (listStage[tn] == 81)
                                {
                                    strmsg = "AGV任务数据追加到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV任务记录到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        object o = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_task_ins",
                                                    SQLHelper.ModelToParameterList(data).ToArray());
                                        data.id = Convert.ToInt32(o);

                                        strmsg = "AGV任务数据追加到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 8;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region A-C1任务到C1点，等待回调（8）
                                if (listStage[tn] == 8)
                                {
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        if (data.callbacktime != null)
                                        {
                                            strmsg = "小车已到C1点，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            listStage[tn] = 9;
                                        }
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（9）
                                if (listStage[tn] == 9)
                                {
                                    strmsg = "AGV任务数据更新到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV回调更新到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                                    SQLHelper.ModelToParameterList(data).ToArray());

                                        strmsg = "AGV任务数据更新到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                            + "，callbacktime：" + data.callbacktime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 10;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region  C1设定交互变量（10）
                                if (listStage[tn] == 10)
                                {
                                    strmsg = "AGV已到达C1点，设定交互变量";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    VarComm.SetVar(conn, "AGV", "RobotCodeC1", arRobotCodes[tn]);
                                    VarComm.SetVar(conn, "AGV", "ContinueA", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedC1", "1");
                                    //VarComm.SetVar(conn, "CNC", "RobotCodeInC1", arRobotCodes[tn]);
                                    listStage[tn] = 11;
                                }
                                #endregion

                                #region AGV传送带转动,送入CNC（11）
                                if (listStage[tn] == 11)
                                {
                                    string isBeltRunC1 = VarComm.GetVar(conn, "CNC", "BeltRunC1");
                                    if (isBeltRunC1 == "1")
                                    {
                                        TCP_Pack_Server.Send(TCP_Pack_Server.str_unloading, arRobotConnIds[tn]);//卸货指令
                                        listStage[tn] = 12;
                                    }
                                }
                                #endregion

                                #region AGV卸货完成（12）
                                if (listStage[tn] == 12)
                                {
                                    string isOutFinished = VarComm.GetAgvVar(conn, arRobotIPs[tn]);//是否装货完成
                                    //int isexist = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_tcp_select", new SqlParameter("robotip", arRobotIPs[tn]), new SqlParameter("type", 2));//找到回应的时间戳
                                    string ishavePalletC1 = OpcUaHelper.R_BeltArriveC1_Method();
                                    if (isOutFinished == "4" && ishavePalletC1 == "True")
                                    {
                                        VarComm.SetVar(conn, "AGV", "ContinueC1", "1");//小车A点继续任务
                                                                                       //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_sn_set", new SqlParameter("agvnumber", arRobotCodes[tn]), new SqlParameter("pencupSN", ""));//

                                        listStage[tn] = 13;
                                    }
                                    else if (isOutFinished != "4")
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion

                                #region  继续任务（13）
                                if (listStage[tn] == 13)
                                {
                                    //strmsg = "AGV已离开B1点，设定交互变量";
                                    //formmain.logToView(strmsg);
                                    //log.Info(strmsg);

                                    //VarComm.SetVar(conn, "AGV", "ArrivedB1", "");
                                    //VarComm.SetVar(conn, "AGV", "RobotCodeB1", "");
                                    //VarComm.SetVar(conn, "AGV", "ContinueB1", "");
                                    //查询AGV继续信号
                                    if (VarComm.GetVar(conn, "AGV", "ContinueC1") != "")
                                        b = true;

                                    if (b)
                                    {
                                        try
                                        {
                                            lckTaskData.WaitOne();
                                            AgvTaskData data = dicTaskData[tn];
                                            taskcode = data.taskcode;
                                        }
                                        finally
                                        {
                                            lckTaskData.Set();
                                        }

                                        reqcode = Guid.NewGuid().ToString();                        //AGV请求代码

                                        //继续任务ykby1
                                        strmsg = "继续AGV任务，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + reqcode + "，taskcode：" + taskcode + "，robotcode：" + arRobotCodes[tn];
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        AgvAnswerModel ret = AgvAPI.ContinueTask(reqcode, taskcode);

                                        if (ret.code == "0")
                                        {
                                            strmsg = "继续AGV任务成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);

                                            //更新任务数据
                                            try
                                            {
                                                lckTaskData.WaitOne();
                                                AgvTaskData data = dicTaskData[tn];
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "A-C1";//后期需要改
                                                data.robotcode = arRobotCodes[tn];
                                                data.srccode = "A";
                                                data.destcode = "C1";
                                                data.callbacktime = null;
                                                data.cmd = 2;
                                                data.sendtime = DateTime.Now;
                                            }
                                            finally
                                            {
                                                lckTaskData.Set();
                                            }
                                            VarComm.SetVar(conn, "AGV", "ArrivedC1", "");//下车不在A点，已离开
                                            listStage[tn] = 14;
                                        }
                                        else
                                        {
                                            strmsg = "继续AGV任务失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iTimeWait = 10;
                                            continue;
                                        }
                                    }
                                }
                                #endregion

                                #region A-C任务结束（14）
                                if (listStage[tn] == 14)//是否还需要回调？
                                {
                                    iRunCnt--;
                                    VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());
                                    //VarComm.SetVar(conn, "AGV", "ArrivedC1", "");//下车不在D1点，已离开
                                    VarComm.SetVar(conn, "AGV", "RobotCodeC1", "");
                                    isusingAC1 = false;
                                    listStage[tn] = 0;

                                }
                                #endregion

                                #region 启动C2-D1任务（15）
                                if (listStage[tn] == 15)
                                {
                                    if (isusingC2D1 == true)                                             //任务占用中
                                    {
                                        listStage[tn] = 49; //启动其他的任务
                                    }
                                    else if (isusingC2D1 == false && WMS2Task.bProductEnable == true)
                                    {
                                        DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_order_sel", null);//订单情况
                                        string iscanoutC2 = VarComm.GetVar(conn, "CNC", "R_CanOutC2");//C2点可出料
                                        string iscanInD1 = OpcUaHelper.R_CanIncomeD1_Method();//D1点可进入
                                        if (isusingC2D1 == false && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && iscanoutC2 == "1" && iscanInD1 == "True")//成品订单未完成且D1可进料
                                        {
                                            reqcode = Guid.NewGuid().ToString();                        //AGV请求代码
                                            taskcode = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "bas_gencode_req",
                                                                               new SqlParameter("code", "agv_taskcode")).ToString();            //AGV任务代码

                                            //创建新任务ykby
                                            strmsg = "创建AGV任务C2-D1，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + reqcode + "，taskcode：" + taskcode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            AgvAnswerModel ret = AgvAPI.CreatTask(reqcode, taskcode, "C2-D1");//创建C2到D1任务,后期需要改2022-5-26
                                            if (ret.code == "0")
                                            {
                                                isusingC2D1 = true;
                                                strmsg = "创建AGV任务C2-D1成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                    + "，reqcode：" + ret.reqCode;
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);

                                                //生成任务数据
                                                AgvTaskData data = new AgvTaskData();
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "ykby";//后期需要改
                                                data.robotcode = "";
                                                data.srccode = "";
                                                data.destcode = "C2";
                                                data.callbacktime = null;
                                                data.cmd = 1;
                                                data.sendtime = DateTime.Now;

                                                try
                                                {
                                                    lckTaskData.WaitOne();
                                                    dicTaskData[tn] = data;
                                                }
                                                finally
                                                {
                                                    lckTaskData.Set();
                                                }

                                                listStage[tn] = 16;
                                            }
                                            else
                                            {
                                                strmsg = "创建AGV任务C2-D1失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                    + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);
                                                iTimeWait = 10;
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            listStage[tn] = 49;
                                        }
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（16）
                                if (listStage[tn] == 16)
                                {
                                    strmsg = "AGV任务数据追加到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV任务记录到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        object o = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_task_ins",
                                                    SQLHelper.ModelToParameterList(data).ToArray());
                                        data.id = Convert.ToInt32(o);

                                        strmsg = "AGV任务数据追加到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 17;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 任务已到C2点，等待回调（17）
                                if (listStage[tn] == 17)
                                {
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        if (data.callbacktime != null)
                                        {
                                            strmsg = "C2-D1任务已到C2点，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iRunCnt++;                                              //AGV运行数量加1
                                            listStage[tn] = 18;
                                        }
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（18）
                                if (listStage[tn] == 18)
                                {
                                    strmsg = "AGV任务数据更新到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV回调更新到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        arRobotCodes[tn] = data.robotcode;
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                                SQLHelper.ModelToParameterList(data).ToArray());

                                        strmsg = "AGV任务数据更新到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                            + "，callbacktime：" + data.callbacktime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 19;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region  设定交互变量（19）
                                if (listStage[tn] == 19)
                                {
                                    strmsg = "AGV已到达C2点，设定交互变量";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    VarComm.SetVar(conn, "AGV", "RobotCodeC2", arRobotCodes[tn]);
                                    //VarComm.SetVar(conn, "WMS1", "RobotCodeA", arRobotCodes[tn]);
                                    VarComm.SetVar(conn, "AGV", "ContinueC2", "");
                                    VarComm.SetVar(conn, "AGV", "StopA", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedC2", "1");
                                    VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());


                                    listStage[tn] = 20;
                                }
                                #endregion

                                #region 传送带转动（20）
                                if (listStage[tn] == 20)
                                {
                                    if (OpcUaHelper.R_SeizeC2_Method() == "True")//a点准备位有货就开始转动
                                    {
                                        int Index = AGVTask.agvInfromList.Select((s, index) => new { Value = s.RobotCode, Index = index }).Where(t => t.Value == arRobotCodes[tn]).Select(t => t.Index).First(); //通过agvCode找到对应的ID
                                        arRobotIPs[tn] = AGVTask.agvInfromList[Index].RobotIP;
                                        arRobotConnIds[tn] = AGVTask.agvInfromList[Index].RobotConnId;
                                        TCP_Pack_Server.Send(TCP_Pack_Server.str_loading, arRobotConnIds[tn]);//装货指令
                                        VarComm.SetVar(conn, "AGV", "ArrivedC2Roll", "1");//A点小车传送带转动
                                        listStage[tn] = 21;
                                    }
                                    else
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion

                                #region 装货完成（21）
                                if (listStage[tn] == 21)
                                {
                                    //int isexist = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_tcp_select", new SqlParameter("robotip", arRobotIPs[tn]), new SqlParameter("type", 1));//找到回应的时间戳
                                    string isInFinished = VarComm.GetAgvVar(conn, arRobotIPs[tn]);//是否装货完成
                                    if (isInFinished == "3")
                                    {
                                        VarComm.SetVar(conn, "AGV", "ArrivedC2Roll", "");//A点小车传送带转动
                                        VarComm.SetVar(conn, "AGV", "ContinueC2", "1");//小车A点继续任务
                                        //VarComm.SetVar(conn, "AGV", "ArrivedA", "");//A点小车传送带转动
                                        pencupSN = VarComm.GetVar(conn, "CNC", "SNInC2");//读C2点笔筒SN
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_sn_set", new SqlParameter("agvnumber", arRobotCodes[tn]), new SqlParameter("pencupSN", pencupSN));//更新agv上的产品
                                        Tracking.Insert(conn, "AGV", "C2点装货完成", pencupSN);//串号更新
                                        listStage[tn] = 22;
                                    }
                                    else if (isInFinished != "3")
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion

                                #region  继续任务去D1（22）
                                if (listStage[tn] == 22)
                                {
                                    //查询AGV继续信号
                                    if (VarComm.GetVar(conn, "AGV", "ContinueC2") != "")
                                        b = true;

                                    if (b)
                                    {
                                        try
                                        {
                                            lckTaskData.WaitOne();
                                            AgvTaskData data = dicTaskData[tn];
                                            taskcode = data.taskcode;
                                        }
                                        finally
                                        {
                                            lckTaskData.Set();
                                        }

                                        reqcode = Guid.NewGuid().ToString();                        //AGV请求代码

                                        //继续任务
                                        strmsg = "继续AGV任务，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + reqcode + "，taskcode：" + taskcode + "，robotcode：" + arRobotCodes[tn];
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        AgvAnswerModel ret = AgvAPI.ContinueTask(reqcode, taskcode);

                                        if (ret.code == "0")
                                        {
                                            strmsg = "继续AGV任务成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);

                                            //更新任务数据
                                            try
                                            {
                                                lckTaskData.WaitOne();
                                                AgvTaskData data = dicTaskData[tn];
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "C2-D1";//后期需要改
                                                data.robotcode = arRobotCodes[tn];
                                                data.srccode = "C2";
                                                data.destcode = "D1";
                                                data.callbacktime = null;
                                                data.cmd = 2;
                                                data.sendtime = DateTime.Now;
                                            }
                                            finally
                                            {
                                                lckTaskData.Set();
                                            }
                                            VarComm.SetVar(conn, "AGV", "ArrivedC2", "");//下车不在C2点，已离开
                                            listStage[tn] = 231;
                                        }
                                        else
                                        {
                                            strmsg = "继续AGV任务失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iTimeWait = 10;
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                #region 保存AGV状态到数据库（23-1）
                                if (listStage[tn] == 231)
                                {
                                    strmsg = "AGV任务数据追加到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV任务记录到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        object o = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_task_ins",
                                                    SQLHelper.ModelToParameterList(data).ToArray());
                                        data.id = Convert.ToInt32(o);

                                        strmsg = "AGV任务数据追加到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 23;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region C2-D1任务到D1点，等待回调（23）
                                if (listStage[tn] == 23)
                                {
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        if (data.callbacktime != null)
                                        {
                                            strmsg = "小车已到D1点，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            listStage[tn] = 24;
                                        }
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库24）
                                if (listStage[tn] == 24)
                                {
                                    strmsg = "AGV任务数据更新到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV回调更新到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                                    SQLHelper.ModelToParameterList(data).ToArray());

                                        strmsg = "AGV任务数据更新到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                            + "，callbacktime：" + data.callbacktime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 25;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region  C1设定交互变量（25）
                                if (listStage[tn] == 25)
                                {
                                    strmsg = "AGV已到达D1点，设定交互变量";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    VarComm.SetVar(conn, "AGV", "RobotCodeD1", arRobotCodes[tn]);
                                    VarComm.SetVar(conn, "AGV", "ContinueC2", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedD1", "1");
                                    //VarComm.SetVar(conn, "CNC", "RobotCodeInC1", arRobotCodes[tn]);
                                    listStage[tn] = 26;
                                }
                                #endregion

                                #region AGV传送带转动,送入WMS2 D1点（26）
                                if (listStage[tn] == 26)
                                {
                                    string isBeltRunD1 = VarComm.GetVar(conn, "WMS2", "BeltRunD1");
                                    if (isBeltRunD1 == "1")
                                    {
                                        TCP_Pack_Server.Send(TCP_Pack_Server.str_unloading, arRobotConnIds[tn]);//卸货指令
                                        listStage[tn] = 27;
                                    }
                                }
                                #endregion

                                #region AGV卸货完成（27）
                                if (listStage[tn] == 27)
                                {
                                    //int isexist = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_tcp_select", new SqlParameter("robotip", arRobotIPs[tn]), new SqlParameter("type", 2));//找到回应的时间戳
                                    string isOutFinished = VarComm.GetAgvVar(conn, arRobotIPs[tn]);//是否装货完成
                                    string ishavePalletD1 = OpcUaHelper.R_BeltArriveD1_Method();//D1点有无托盘
                                    if (isOutFinished == "4" && ishavePalletD1 == "True")
                                    {
                                        VarComm.SetVar(conn, "AGV", "ContinueD1", "1");//小车D点继续任务
                                        //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_sn_set", new SqlParameter("agvnumber", arRobotCodes[tn]), new SqlParameter("pencupSN", ""));//清空小车SN号

                                        listStage[tn] = 28;
                                    }
                                    else if (isOutFinished != "4")
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion
                                #region  AGV继续任务（28）
                                if (listStage[tn] == 28)
                                {
                                    //strmsg = "AGV已离开B1点，设定交互变量";
                                    //formmain.logToView(strmsg);
                                    //log.Info(strmsg);

                                    //VarComm.SetVar(conn, "AGV", "ArrivedB1", "");
                                    //VarComm.SetVar(conn, "AGV", "RobotCodeB1", "");
                                    //VarComm.SetVar(conn, "AGV", "ContinueB1", "");
                                    //查询AGV继续信号
                                    if (VarComm.GetVar(conn, "AGV", "ContinueD1") != "")
                                        b = true;

                                    if (b)
                                    {
                                        try
                                        {
                                            lckTaskData.WaitOne();
                                            AgvTaskData data = dicTaskData[tn];
                                            taskcode = data.taskcode;
                                        }
                                        finally
                                        {
                                            lckTaskData.Set();
                                        }

                                        reqcode = Guid.NewGuid().ToString();                        //AGV请求代码

                                        //继续任务ykby1
                                        strmsg = "继续AGV任务，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + reqcode + "，taskcode：" + taskcode + "，robotcode：" + arRobotCodes[tn];
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        AgvAnswerModel ret = AgvAPI.ContinueTask(reqcode, taskcode);

                                        if (ret.code == "0")
                                        {
                                            strmsg = "继续AGV任务成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);

                                            //更新任务数据
                                            try
                                            {
                                                lckTaskData.WaitOne();
                                                AgvTaskData data = dicTaskData[tn];
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "ykby1";//后期需要改
                                                data.robotcode = arRobotCodes[tn];
                                                data.srccode = "C2";
                                                data.destcode = "D1";
                                                data.callbacktime = null;
                                                data.cmd = 2;
                                                data.sendtime = DateTime.Now;
                                            }
                                            finally
                                            {
                                                lckTaskData.Set();
                                            }
                                            VarComm.SetVar(conn, "AGV", "ArrivedD1", "");//下车不在A点，已离开
                                            listStage[tn] = 14;
                                        }
                                        else
                                        {
                                            strmsg = "继续AGV任务失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iTimeWait = 10;
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                #region C2-D1任务结束（29）
                                if (listStage[tn] == 29)//是否还需要回调？
                                {
                                    iRunCnt--;
                                    VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());
                                    //VarComm.SetVar(conn, "AGV", "ArrivedD1", "");//下车不在D1点，已离开
                                    VarComm.SetVar(conn, "AGV", "RobotCodeD1", "");
                                    isusingC2D1 = false;
                                    listStage[tn] = 0;

                                }
                                #endregion

                                #region 启动C2-B任务（30）
                                if (listStage[tn] == 30)
                                {
                                    if (isusingC2B == true)                                             //任务占用中
                                    {
                                        listStage[tn] = 15; //启动其他的任务
                                    }
                                    else if (isusingC2B == false && WMS1Task.bProductEnable == true)
                                    {
                                        int handlingCount = 0;
                                        int.TryParse(VarComm.GetVar(conn, "CNC", "HandlingCount"), out handlingCount);//CNC正加工物料数
                                        string iscanoutC2 = VarComm.GetVar(conn, "CNC", "R_CanOutC2");//C2点可出料
                                        string ishavePalletC2 = OpcUaHelper.R_SeizeC2_Method();//C2点是否有托盘
                                        if (isusingC2B == false && iscanoutC2 == "" && handlingCount < 4 && ishavePalletC2 == "True")//C2点是空托盘&&未占用任务&&加工物料数量小于4,后期可能有问题
                                        {
                                            reqcode = Guid.NewGuid().ToString();                        //AGV请求代码
                                            taskcode = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "bas_gencode_req",
                                                                               new SqlParameter("code", "agv_taskcode")).ToString();            //AGV任务代码

                                            //创建新任务ykby
                                            strmsg = "创建AGV任务C2-B，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + reqcode + "，taskcode：" + taskcode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            AgvAnswerModel ret = AgvAPI.CreatTask(reqcode, taskcode, "C2-B");//创建A到C1任务,后期需要改2022-5-26
                                            if (ret.code == "0")
                                            {
                                                isusingC2B = true;
                                                strmsg = "创建AGV任务C2-B成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                    + "，reqcode：" + ret.reqCode;
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);

                                                //生成任务数据
                                                AgvTaskData data = new AgvTaskData();
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "ykby";//后期需要改
                                                data.robotcode = "";
                                                data.srccode = "";
                                                data.destcode = "C2";
                                                data.callbacktime = null;
                                                data.cmd = 1;
                                                data.sendtime = DateTime.Now;

                                                try
                                                {
                                                    lckTaskData.WaitOne();
                                                    dicTaskData[tn] = data;
                                                }
                                                finally
                                                {
                                                    lckTaskData.Set();
                                                }

                                                listStage[tn] = 31;
                                            }
                                            else
                                            {
                                                strmsg = "创建AGV任务C2-B失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                    + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);
                                                iTimeWait = 10;
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            listStage[tn] = 15;
                                        }
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（31）
                                if (listStage[tn] == 31)
                                {
                                    strmsg = "AGV任务数据追加到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV任务记录到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        object o = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_task_ins",
                                                    SQLHelper.ModelToParameterList(data).ToArray());
                                        data.id = Convert.ToInt32(o);

                                        strmsg = "AGV任务数据追加到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 32;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 任务已到C2点，等待回调（32）
                                if (listStage[tn] == 32)
                                {
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        if (data.callbacktime != null)
                                        {
                                            strmsg = "C2-B任务已到C2点，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iRunCnt++;                                              //AGV运行数量加1
                                            listStage[tn] = 33;
                                        }
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（33）
                                if (listStage[tn] == 33)
                                {
                                    strmsg = "AGV任务数据更新到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV回调更新到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        arRobotCodes[tn] = data.robotcode;
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                                SQLHelper.ModelToParameterList(data).ToArray());

                                        strmsg = "AGV任务数据更新到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                            + "，callbacktime：" + data.callbacktime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 34;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region  检查C2点货物，看是否取消C2-B任务（34）
                                if (listStage[tn] == 34)
                                {
                                    string iscanoutC2 = VarComm.GetVar(conn, "CNC", "R_CanOutC2");//C2点可出料
                                    string ishavePalletC2 = OpcUaHelper.R_SeizeC2_Method();//C2点是否有托盘
                                    string canout = OpcUaHelper.R_AllowTakeC2_Method();//C2点准许拿空托盘
                                    if (iscanoutC2 == "1" && ishavePalletC2 == "True")
                                    {
                                        strmsg = "C2点有货，取消C2-B任务";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 35;
                                    }
                                    else if (iscanoutC2 == "" && ishavePalletC2 == "True" && canout == "True")
                                    {
                                        strmsg = "C2点无货，继续C2-B任务";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 38;
                                    }
                                    else
                                    {
                                        iTimeWait = 1;
                                    }
                                }
                                #endregion

                                #region  取消C2-B任务（35）
                                if (listStage[tn] == 35)
                                {
                                    string tasktype = "";
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        taskcode = data.taskcode;
                                        tasktype = data.tasktype;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }

                                    reqcode = Guid.NewGuid().ToString();                            //AGV请求代码

                                    //取消任务
                                    strmsg = "取消AGV任务，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                        + "，reqcode：" + reqcode + "，taskcode：" + taskcode + "，robotcode：" + arRobotCodes[tn] + "，tasktype" + tasktype;
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    AgvAnswerModel ret = AgvAPI.CancelTask(reqcode, taskcode);

                                    if (ret.code == "0")
                                    {
                                        strmsg = "取消AGV任务成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + ret.reqCode;
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);

                                        //更新任务数据
                                        try
                                        {
                                            lckTaskData.WaitOne();
                                            AgvTaskData data = dicTaskData[tn];
                                            data.id = null;
                                            data.taskcode = taskcode;
                                            data.tasktype = "ykby1";
                                            data.robotcode = arRobotCodes[tn];
                                            data.srccode = "C2";
                                            data.destcode = "";
                                            data.callbacktime = null;
                                            data.cmd = 3;
                                            data.sendtime = DateTime.Now;
                                        }
                                        finally
                                        {
                                            lckTaskData.Set();
                                        }
                                        arRobotCodes[tn] = "";                                      //清空AGV车号暂存

                                        listStage[tn] = 36;

                                        iRunCnt--;                                                  //AGV运行数量减1
                                    }
                                    else
                                    {
                                        strmsg = "取消AGV任务失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iTimeWait = 10;
                                    }
                                }
                                #endregion
                                #region 保存AGV状态到数据库（36）
                                if (listStage[tn] == 36)
                                {
                                    strmsg = "AGV任务数据追加到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV任务记录到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        object o = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_task_ins",
                                                    SQLHelper.ModelToParameterList(data).ToArray());
                                        data.id = Convert.ToInt32(o);

                                        ; strmsg = "AGV任务数据追加到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                              + "，id：" + data.id + ",taskcode：" + data.taskcode
                                              + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                              + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                              + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 37;                                         //调度状态更新
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region  设定交互变量（37）
                                if (listStage[tn] == 37)
                                {
                                    strmsg = "AGV任务C2-B已取消，设定交互变量";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    VarComm.SetVar(conn, "AGV", "ArrivedC2", "");
                                    VarComm.SetVar(conn, "AGV", "RobotCodeC2", "");
                                    VarComm.SetVar(conn, "AGV", "ContinueC2", "");
                                    VarComm.SetVar(conn, "AGV", "StopA", "");
                                    VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());

                                    listStage[tn] = 15;//尝试做C2-D1任务
                                }
                                #endregion
                                #region  设定交互变量（38）
                                if (listStage[tn] == 38)
                                {
                                    strmsg = "AGV已到达C2点，设定交互变量";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    VarComm.SetVar(conn, "AGV", "RobotCodeC2", arRobotCodes[tn]);
                                    //VarComm.SetVar(conn, "WMS1", "RobotCodeA", arRobotCodes[tn]);
                                    VarComm.SetVar(conn, "AGV", "ContinueC2", "");
                                    VarComm.SetVar(conn, "AGV", "StopA", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedC2", "1");
                                    VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());

                                    listStage[tn] = 39;
                                }
                                #endregion
                                #region 传送带转动（39）
                                if (listStage[tn] == 39)
                                {
                                    if (OpcUaHelper.R_SeizeC2_Method() == "True")//a点准备位有货就开始转动
                                    {
                                        //arRobotIPs[tn] = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_id_select", new SqlParameter("robotcode", arRobotCodes[tn])).ToString();  //通过agvCode找到对应的ID
                                        int Index = AGVTask.agvInfromList.Select((s, index) => new { Value = s.RobotCode, Index = index }).Where(t => t.Value == arRobotCodes[tn]).Select(t => t.Index).First(); //通过agvCode找到对应的ID
                                        arRobotIPs[tn] = AGVTask.agvInfromList[Index].RobotIP;
                                        arRobotConnIds[tn] = AGVTask.agvInfromList[Index].RobotConnId;
                                        TCP_Pack_Server.Send(TCP_Pack_Server.str_loading, arRobotConnIds[tn]);//装货指令
                                        //TCP_Pack_Server.Send(TCP_Pack_Server.str_loading, Marshal.StringToHGlobalAnsi(arRobotIPs[tn]));//装货指令
                                        VarComm.SetVar(conn, "AGV", "ArrivedC2Roll", "1");//A点小车传送带转动
                                        listStage[tn] = 40;
                                    }
                                    else
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion
                                #region 装货完成（40）
                                if (listStage[tn] == 40)
                                {
                                    //int isexist = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_tcp_select", new SqlParameter("robotip", arRobotIPs[tn]), new SqlParameter("type", 1));//找到回应的时间戳
                                    string isInFinished = VarComm.GetAgvVar(conn, arRobotIPs[tn]);//是否装货完成
                                    if (isInFinished == "3")
                                    {
                                        VarComm.SetVar(conn, "AGV", "ArrivedC2Roll", "");//A点小车传送带转动
                                        VarComm.SetVar(conn, "AGV", "ContinueC2", "1");//小车A点继续任务
                                        //VarComm.SetVar(conn, "AGV", "ArrivedA", "");//A点小车传送带转动
                                        pencupSN = VarComm.GetVar(conn, "CNC", "SNInC2"); //获取WMS1的产品串号
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_sn_set", new SqlParameter("agvnumber", arRobotCodes[tn]), new SqlParameter("pencupSN", pencupSN));//更新agv上的产品
                                        Tracking.Insert(conn, "AGV", "C2点装货完成", pencupSN);//串号更新
                                        listStage[tn] = 7;
                                    }
                                    else if (isInFinished != "3")
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion

                                #region  继续任务去B（41）
                                if (listStage[tn] == 41)
                                {
                                    //查询AGV继续信号
                                    if (VarComm.GetVar(conn, "AGV", "ContinueC2") != "")
                                        b = true;

                                    if (b)
                                    {
                                        try
                                        {
                                            lckTaskData.WaitOne();
                                            AgvTaskData data = dicTaskData[tn];
                                            taskcode = data.taskcode;
                                        }
                                        finally
                                        {
                                            lckTaskData.Set();
                                        }

                                        reqcode = Guid.NewGuid().ToString();                        //AGV请求代码

                                        //继续任务ykby1
                                        strmsg = "继续AGV任务，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + reqcode + "，taskcode：" + taskcode + "，robotcode：" + arRobotCodes[tn];
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        AgvAnswerModel ret = AgvAPI.ContinueTask(reqcode, taskcode);

                                        if (ret.code == "0")
                                        {
                                            strmsg = "继续AGV任务成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);

                                            //更新任务数据
                                            try
                                            {
                                                lckTaskData.WaitOne();
                                                AgvTaskData data = dicTaskData[tn];
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "ykby1";//后期需要改
                                                data.robotcode = arRobotCodes[tn];
                                                data.srccode = "C2";
                                                data.destcode = "B";
                                                data.callbacktime = null;
                                                data.cmd = 2;
                                                data.sendtime = DateTime.Now;
                                            }
                                            finally
                                            {
                                                lckTaskData.Set();
                                            }
                                            VarComm.SetVar(conn, "AGV", "ArrivedC2", "");//下车不在A点，已离开
                                            listStage[tn] = 42;
                                        }
                                        else
                                        {
                                            strmsg = "继续AGV任务失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iTimeWait = 10;
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                #region 保存AGV状态到数据库（421）
                                if (listStage[tn] == 421)
                                {
                                    strmsg = "AGV任务数据追加到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV任务记录到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        object o = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_task_ins",
                                                    SQLHelper.ModelToParameterList(data).ToArray());
                                        data.id = Convert.ToInt32(o);

                                        strmsg = "AGV任务数据追加到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 42;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region C2-B任务到B点，等待回调（42）
                                if (listStage[tn] == 42)
                                {
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        if (data.callbacktime != null)
                                        {
                                            strmsg = "小车已到B点，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            listStage[tn] = 43;
                                        }
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（43）
                                if (listStage[tn] == 43)
                                {
                                    strmsg = "AGV任务数据更新到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV回调更新到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                                    SQLHelper.ModelToParameterList(data).ToArray());

                                        strmsg = "AGV任务数据更新到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                            + "，callbacktime：" + data.callbacktime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 44;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region  D2设定交互变量（44）
                                if (listStage[tn] == 44)
                                {
                                    strmsg = "AGV已到达B点，设定交互变量";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    VarComm.SetVar(conn, "AGV", "RobotCodeB", arRobotCodes[tn]);
                                    VarComm.SetVar(conn, "AGV", "ContinueC2", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedB", "1");
                                    //VarComm.SetVar(conn, "CNC", "RobotCodeInC1", arRobotCodes[tn]);
                                    listStage[tn] = 45;
                                }
                                #endregion

                                #region AGV传送带转动,送入CNC（45）
                                if (listStage[tn] == 45)
                                {
                                    string isBeltRunB = VarComm.GetVar(conn, "CNC", "BeltRunB");
                                    if (isBeltRunB == "1")
                                    {
                                        TCP_Pack_Server.Send(TCP_Pack_Server.str_unloading, arRobotConnIds[tn]);//卸货指令
                                        listStage[tn] = 46;
                                    }
                                }
                                #endregion

                                #region AGV卸货完成（46）
                                if (listStage[tn] == 46)
                                {
                                    //int isexist = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_tcp_select", new SqlParameter("robotip", arRobotIPs[tn]), new SqlParameter("type", 2));//找到回应的时间戳
                                    string isOutFinished = VarComm.GetAgvVar(conn, arRobotIPs[tn]);//是否装货完成
                                    string ishavePalletB = OpcUaHelper.R_SeizeB_Method();//D1点有无托盘
                                    if (isOutFinished == "4" && ishavePalletB == "True")
                                    {
                                        VarComm.SetVar(conn, "AGV", "ContinueB", "1");//小车A点继续任务
                                        //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_sn_set", new SqlParameter("agvnumber", arRobotCodes[tn]), new SqlParameter("pencupSN", ""));//

                                        listStage[tn] = 47;
                                    }
                                    else if (isOutFinished != "4")
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion

                                #region  继续任务（47）
                                if (listStage[tn] == 47)
                                {
                                    //strmsg = "AGV已离开B1点，设定交互变量";
                                    //formmain.logToView(strmsg);
                                    //log.Info(strmsg);

                                    //VarComm.SetVar(conn, "AGV", "ArrivedB1", "");
                                    //VarComm.SetVar(conn, "AGV", "RobotCodeB1", "");
                                    //VarComm.SetVar(conn, "AGV", "ContinueB1", "");
                                    //查询AGV继续信号
                                    if (VarComm.GetVar(conn, "AGV", "ContinueB") != "")
                                        b = true;

                                    if (b)
                                    {
                                        try
                                        {
                                            lckTaskData.WaitOne();
                                            AgvTaskData data = dicTaskData[tn];
                                            taskcode = data.taskcode;
                                        }
                                        finally
                                        {
                                            lckTaskData.Set();
                                        }

                                        reqcode = Guid.NewGuid().ToString();                        //AGV请求代码

                                        //继续任务ykby1
                                        strmsg = "继续AGV任务，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + reqcode + "，taskcode：" + taskcode + "，robotcode：" + arRobotCodes[tn];
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        AgvAnswerModel ret = AgvAPI.ContinueTask(reqcode, taskcode);

                                        if (ret.code == "0")
                                        {
                                            strmsg = "继续AGV任务成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);

                                            //更新任务数据
                                            try
                                            {
                                                lckTaskData.WaitOne();
                                                AgvTaskData data = dicTaskData[tn];
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "ykby1";//后期需要改
                                                data.robotcode = arRobotCodes[tn];
                                                data.srccode = "C2";
                                                data.destcode = "B";
                                                data.callbacktime = null;
                                                data.cmd = 2;
                                                data.sendtime = DateTime.Now;
                                            }
                                            finally
                                            {
                                                lckTaskData.Set();
                                            }
                                            VarComm.SetVar(conn, "AGV", "ArrivedB", "");//下车不在A点，已离开
                                            listStage[tn] = 48;
                                        }
                                        else
                                        {
                                            strmsg = "继续AGV任务失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iTimeWait = 10;
                                            continue;
                                        }
                                    }
                                }
                                #endregion

                                #region C2-B任务结束（48）
                                if (listStage[tn] == 48)//是否还需要回调？
                                {
                                    iRunCnt--;
                                    VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());
                                    //VarComm.SetVar(conn, "AGV", "ArrivedB", "");//下车不在B点，已离开
                                    VarComm.SetVar(conn, "AGV", "RobotCodeB", "");
                                    isusingC2B = false;
                                    listStage[tn] = 0;

                                }
                                #endregion
                                #region 启动D2-B任务（49）
                                if (listStage[tn] == 49)
                                {
                                    if (isusingD2B == true)                                             //任务占用中
                                    {
                                        listStage[tn] = 0; //启动其他的任务
                                    }
                                    else if (isusingD2B == false && WMS2Task.bProductEnable == true)
                                    {
                                        string ishavePalletD2 = OpcUaHelper.R_SeizeD2_Method();//D2点是否有托盘
                                        if (isusingD2B == false && ishavePalletD2 == "True")//D2有空托盘
                                        {
                                            reqcode = Guid.NewGuid().ToString();                        //AGV请求代码
                                            taskcode = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "bas_gencode_req",
                                                                               new SqlParameter("code", "agv_taskcode")).ToString();            //AGV任务代码

                                            //创建新任务ykby
                                            strmsg = "创建AGV任务D2-B，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + reqcode + "，taskcode：" + taskcode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            AgvAnswerModel ret = AgvAPI.CreatTask(reqcode, taskcode, "D2-B");//创建D2到B任务,后期需要改2022-5-26
                                            if (ret.code == "0")
                                            {
                                                isusingC2D1 = true;
                                                strmsg = "创建AGV任务D2-B成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                    + "，reqcode：" + ret.reqCode;
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);

                                                //生成任务数据
                                                AgvTaskData data = new AgvTaskData();
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "ykby";//后期需要改
                                                data.robotcode = "";
                                                data.srccode = "";
                                                data.destcode = "D2";
                                                data.callbacktime = null;
                                                data.cmd = 1;
                                                data.sendtime = DateTime.Now;

                                                try
                                                {
                                                    lckTaskData.WaitOne();
                                                    dicTaskData[tn] = data;
                                                }
                                                finally
                                                {
                                                    lckTaskData.Set();
                                                }

                                                listStage[tn] = 50;
                                            }
                                            else
                                            {
                                                strmsg = "创建AGV任务D2-B失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                    + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);
                                                iTimeWait = 10;
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            listStage[tn] = 0;
                                        }
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（50）
                                if (listStage[tn] == 50)
                                {
                                    strmsg = "AGV任务数据追加到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV任务记录到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        object o = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_task_ins",
                                                    SQLHelper.ModelToParameterList(data).ToArray());
                                        data.id = Convert.ToInt32(o);

                                        strmsg = "AGV任务数据追加到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 51;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 任务已到D2点，等待回调（51）
                                if (listStage[tn] == 51)
                                {
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        if (data.callbacktime != null)
                                        {
                                            strmsg = "D2-B任务已到D2点，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iRunCnt++;                                              //AGV运行数量加1
                                            listStage[tn] = 52;
                                        }
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库（52）
                                if (listStage[tn] == 52)
                                {
                                    strmsg = "AGV任务数据更新到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV回调更新到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        arRobotCodes[tn] = data.robotcode;
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                                SQLHelper.ModelToParameterList(data).ToArray());

                                        strmsg = "AGV任务数据更新到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                            + "，callbacktime：" + data.callbacktime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 53;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region  设定交互变量（53）
                                if (listStage[tn] == 53)
                                {
                                    strmsg = "AGV已到达D2点，设定交互变量";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    VarComm.SetVar(conn, "AGV", "RobotCodeD2", arRobotCodes[tn]);
                                    //VarComm.SetVar(conn, "WMS1", "RobotCodeA", arRobotCodes[tn]);
                                    VarComm.SetVar(conn, "AGV", "ContinueD2", "");
                                    VarComm.SetVar(conn, "AGV", "StopA", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedD2", "1");
                                    VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());


                                    listStage[tn] = 54;
                                }
                                #endregion

                                #region 传送带转动（54）
                                if (listStage[tn] == 20)
                                {
                                    if (OpcUaHelper.R_SeizeD2_Method() == "True")//a点准备位有货就开始转动
                                    {
                                        //arRobotIPs[tn] = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_id_select", new SqlParameter("robotcode", arRobotCodes[tn])).ToString();  //通过agvCode找到对应的ID
                                        int Index = AGVTask.agvInfromList.Select((s, index) => new { Value = s.RobotCode, Index = index }).Where(t => t.Value == arRobotCodes[tn]).Select(t => t.Index).First(); //通过agvCode找到对应的ID
                                        arRobotIPs[tn] = AGVTask.agvInfromList[Index].RobotIP;
                                        arRobotConnIds[tn] = AGVTask.agvInfromList[Index].RobotConnId;
                                        TCP_Pack_Server.Send(TCP_Pack_Server.str_loading, arRobotConnIds[tn]);//装货指令
                                        VarComm.SetVar(conn, "AGV", "ArrivedD2Roll", "1");//D2点小车传送带转动
                                        listStage[tn] = 55;
                                    }
                                    else
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion

                                #region 装货完成（55）
                                if (listStage[tn] == 55)
                                {
                                    //int isexist = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_tcp_select", new SqlParameter("robotip", arRobotIPs[tn]), new SqlParameter("type", 1));//找到回应的时间戳
                                    string isInFinished = VarComm.GetAgvVar(conn, arRobotIPs[tn]);//是否装货完成
                                    if (isInFinished == "3")
                                    {
                                        VarComm.SetVar(conn, "AGV", "ArrivedD2Roll", "");//A点小车传送带转动
                                        VarComm.SetVar(conn, "AGV", "ContinueD2", "1");//小车A点继续任务
                                        //VarComm.SetVar(conn, "AGV", "ArrivedA", "");//A点小车传送带转动
                                        pencupSN = "";
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_sn_set", new SqlParameter("agvnumber", arRobotCodes[tn]), new SqlParameter("pencupSN", pencupSN));//更新agv上的产品
                                        Tracking.Insert(conn, "AGV", "D2点装货完成", pencupSN);//串号更新
                                        listStage[tn] = 56;
                                    }
                                    else if (isInFinished != "3")
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                #endregion

                                #region  继续任务去B（56）
                                if (listStage[tn] == 56)
                                {
                                    //查询AGV继续信号
                                    if (VarComm.GetVar(conn, "AGV", "ContinueD2") != "")
                                        b = true;

                                    if (b)
                                    {
                                        try
                                        {
                                            lckTaskData.WaitOne();
                                            AgvTaskData data = dicTaskData[tn];
                                            taskcode = data.taskcode;
                                        }
                                        finally
                                        {
                                            lckTaskData.Set();
                                        }

                                        reqcode = Guid.NewGuid().ToString();                        //AGV请求代码

                                        //继续任务
                                        strmsg = "继续AGV任务，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + reqcode + "，taskcode：" + taskcode + "，robotcode：" + arRobotCodes[tn];
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        AgvAnswerModel ret = AgvAPI.ContinueTask(reqcode, taskcode);

                                        if (ret.code == "0")
                                        {
                                            strmsg = "继续AGV任务成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);

                                            //更新任务数据
                                            try
                                            {
                                                lckTaskData.WaitOne();
                                                AgvTaskData data = dicTaskData[tn];
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "D2-B";//后期需要改
                                                data.robotcode = arRobotCodes[tn];
                                                data.srccode = "D2";
                                                data.destcode = "B";
                                                data.callbacktime = null;
                                                data.cmd = 2;
                                                data.sendtime = DateTime.Now;
                                            }
                                            finally
                                            {
                                                lckTaskData.Set();
                                            }
                                            VarComm.SetVar(conn, "AGV", "ArrivedD2", "");//下车不在C2点，已离开
                                            listStage[tn] = 561;
                                        }
                                        else
                                        {
                                            strmsg = "继续AGV任务失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iTimeWait = 10;
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                #region 保存AGV状态到数据库561）
                                if (listStage[tn] == 561)
                                {
                                    strmsg = "AGV任务数据追加到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV任务记录到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        object o = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_task_ins",
                                                    SQLHelper.ModelToParameterList(data).ToArray());
                                        data.id = Convert.ToInt32(o);

                                        strmsg = "AGV任务数据追加到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 57;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region D2-B任务到B点，等待回调（57）
                                if (listStage[tn] == 57)
                                {
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        if (data.callbacktime != null)
                                        {
                                            strmsg = "小车已到B点，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            listStage[tn] = 58;
                                        }
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region 保存AGV状态到数据库58）
                                if (listStage[tn] == 58)
                                {
                                    strmsg = "AGV任务数据更新到数据库，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString();
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    //AGV回调更新到数据库
                                    try
                                    {
                                        lckTaskData.WaitOne();
                                        AgvTaskData data = dicTaskData[tn];
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_task_upd",
                                                    SQLHelper.ModelToParameterList(data).ToArray());

                                        strmsg = "AGV任务数据更新到数据库成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，id：" + data.id + ",taskcode：" + data.taskcode
                                            + ",tasktype：" + data.tasktype + "，robotcode：" + data.robotcode
                                            + "，srccode：" + data.srccode + "，destcode：" + data.destcode + "，cmd：" + data.cmd
                                            + "，sendtime：" + data.sendtime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                            + "，callbacktime：" + data.callbacktime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        listStage[tn] = 59;
                                    }
                                    finally
                                    {
                                        lckTaskData.Set();
                                    }
                                }
                                #endregion

                                #region  B设定交互变量（59）
                                if (listStage[tn] == 59)
                                {
                                    strmsg = "AGV已到达B点，设定交互变量";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    VarComm.SetVar(conn, "AGV", "RobotCodeB", arRobotCodes[tn]);
                                    VarComm.SetVar(conn, "AGV", "ContinueB", "");
                                    VarComm.SetVar(conn, "AGV", "ArrivedB", "1");
                                    //VarComm.SetVar(conn, "CNC", "RobotCodeInC1", arRobotCodes[tn]);
                                    listStage[tn] = 60;
                                }
                                #endregion

                                #region AGV传送带转动,送入WMS1 B点（60）
                                if (listStage[tn] == 60)
                                {
                                    string isBeltRunB = VarComm.GetVar(conn, "WMS1", "BeltRunB");
                                    if (isBeltRunB == "1")
                                    {
                                        TCP_Pack_Server.Send(TCP_Pack_Server.str_unloading, Marshal.StringToHGlobalAnsi(arRobotIPs[tn]));//卸货指令
                                        listStage[tn] = 61;
                                    }
                                }
                                #endregion

                                #region AGV卸货完成（61）
                                if (listStage[tn] == 61)
                                {
                                    //int isexist = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "agv_sn_tcp_select", new SqlParameter("robotip", arRobotIPs[tn]), new SqlParameter("type", 2));//找到回应的时间戳
                                    string isOutFinished = VarComm.GetAgvVar(conn, arRobotIPs[tn]);//是否装货完成
                                    string ishavePalletB = OpcUaHelper.R_SeizeB_Method();//D1点有无托盘
                                    if (isOutFinished == "4" && ishavePalletB == "True")
                                    {
                                        VarComm.SetVar(conn, "AGV", "ContinueB", "1");//小车B点继续任务
                                        //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_sn_set", new SqlParameter("agvnumber", arRobotCodes[tn]), new SqlParameter("pencupSN", ""));//

                                        listStage[tn] = 62;
                                    }
                                    else if (isOutFinished != "4")
                                    {
                                        iTimeWait = 1;
                                    }
                                }
                                #endregion
                                #region  AGV继续任务（62）
                                if (listStage[tn] == 62)
                                {
                                    //strmsg = "AGV已离开B1点，设定交互变量";
                                    //formmain.logToView(strmsg);
                                    //log.Info(strmsg);

                                    //VarComm.SetVar(conn, "AGV", "ArrivedB1", "");
                                    //VarComm.SetVar(conn, "AGV", "RobotCodeB1", "");
                                    //VarComm.SetVar(conn, "AGV", "ContinueB1", "");
                                    //查询AGV继续信号
                                    if (VarComm.GetVar(conn, "AGV", "ContinueB") != "")
                                        b = true;

                                    if (b)
                                    {
                                        try
                                        {
                                            lckTaskData.WaitOne();
                                            AgvTaskData data = dicTaskData[tn];
                                            taskcode = data.taskcode;
                                        }
                                        finally
                                        {
                                            lckTaskData.Set();
                                        }

                                        reqcode = Guid.NewGuid().ToString();                        //AGV请求代码

                                        //继续任务ykby1
                                        strmsg = "继续AGV任务，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                            + "，reqcode：" + reqcode + "，taskcode：" + taskcode + "，robotcode：" + arRobotCodes[tn];
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        AgvAnswerModel ret = AgvAPI.ContinueTask(reqcode, taskcode);

                                        if (ret.code == "0")
                                        {
                                            strmsg = "继续AGV任务成功，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);

                                            //更新任务数据
                                            try
                                            {
                                                lckTaskData.WaitOne();
                                                AgvTaskData data = dicTaskData[tn];
                                                data.id = null;
                                                data.taskcode = taskcode;
                                                data.tasktype = "ykby1";//后期需要改
                                                data.robotcode = arRobotCodes[tn];
                                                data.srccode = "D2";
                                                data.destcode = "B";
                                                data.callbacktime = null;
                                                data.cmd = 2;
                                                data.sendtime = DateTime.Now;
                                            }
                                            finally
                                            {
                                                lckTaskData.Set();
                                            }
                                            VarComm.SetVar(conn, "AGV", "ArrivedB", "");//下车不在B点，已离开
                                            listStage[tn] = 63;
                                        }
                                        else
                                        {
                                            strmsg = "继续AGV任务失败，listStage[" + tn.ToString() + "]：" + listStage[tn].ToString()
                                                + "，reqcode：" + ret.reqCode + "，code：" + ret.code + "，message：" + ret.message;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            iTimeWait = 10;
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                #region D2-B任务结束（63）
                                if (listStage[tn] == 63)//是否还需要回调？
                                {
                                    iRunCnt--;
                                    VarComm.SetVar(conn, "AGV", "RunCnt", iRunCnt.ToString());
                                    //VarComm.SetVar(conn, "AGV", "ArrivedB", "");//下车不在D1点，已离开
                                    VarComm.SetVar(conn, "AGV", "RobotCodeB", "");

                                    isusingC2D1 = false;
                                    listStage[tn] = 0;

                                }
                                #endregion

                                tn++;
                            }
                            else
                                tn = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        strmsg = "Error: " + ex.Message + " 等待一会儿再试!";
                        formmain.logToView(strmsg);
                        log.Error(strmsg);
                        iTimeWait = 10;
                        continue;
                    }
                }
            }

            try
            {
                host.Close();
                List<IntPtr> robotconnids = arRobotConnIds.ToList();
                TCP_Pack_Server.Disconnect(robotconnids);
                strmsg = "关闭AGV TCP服务成功";
                formmain.logToView(strmsg);
                log.Info(strmsg);

            }
            catch (Exception ex)
            {
                strmsg = "关闭AGV回调接收服务失败：" + ex.Message;
                formmain.logToView(strmsg);
                log.Error(strmsg);
            }

            log.Info("AgvControl线程停止");
            bRunningFlag = false;                                                                   //设置AGV主线程停止标志
            return;
        }

    }
    [ServiceBehavior(UseSynchronizationContext = false, InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class AgvCallback : IAgvCallback
    {
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        log4net.ILog log = log4net.LogManager.GetLogger("AgvCallback");

        public AgvAnswerModel agvCallback(AgvCallbackModel o)
        {
            string strmsg = "";

            string taskcode = "";
            string robotcode = "";
            string destcode = "";

            bool b = false;

            AgvAnswerModel jsonmd = new AgvAnswerModel();
            if (o == null)
            {
                strmsg = "AGV回调参数为空";
                formmain.logToView(strmsg);
                log.Error(strmsg);
            }
            else
            {
                //foreach (PropertyInfo p in typeof(AgvCallbackModel).GetProperties())
                //{
                //    log.Info(string.Format("{0}:{1}", p.Name, p.GetValue(o)));
                //}

                taskcode = o.taskCode;
                robotcode = o.robotCode;
                destcode = o.currentCallCode;

                strmsg = "AGV回调参数，currentCallCode：" + destcode + "，taskCode：" + taskcode + "，robotCode：" + robotcode;
                formmain.logToView(strmsg);
                log.Info(strmsg);

                //更新任务数据
                b = false;
                try
                {
                    AGVTask.lckTaskData.WaitOne();
                    foreach (int tn in AGVTask.dicTaskData.Keys)
                    {
                        AgvTaskData data = AGVTask.dicTaskData[tn];
                        if (data.taskcode == taskcode)
                        {
                            data.callbacktime = DateTime.Now;
                            data.robotcode = robotcode;
                            b = true;
                            break;
                        }
                    }
                }
                finally
                {
                    AGVTask.lckTaskData.Set();
                }
                if (!b)
                {
                    strmsg = "AGV回调更新失败，任务没发现，currentCallCode：" + destcode + "，taskCode：" + taskcode + "，robotCode：" + robotcode;
                    formmain.logToView(strmsg);
                    log.Error(strmsg);
                }
            }
            jsonmd.code = "";
            jsonmd.message = "";
            jsonmd.reqCode = "";
            jsonmd.code = "0";
            jsonmd.message = "成功";
            return jsonmd;
        }
    }

    #region AGV变量显示
    class AgvVarRefresh
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("AgvVarRefresh");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;

        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        static void thAgvVarRefreshFunc()
        {
            log.Info("AGV变量显示线程启动");
            bRunningFlag = true;

            int iTimeWait = 0;
            string strmsg = "";
            DateTime oldts = new DateTime(1960, 1, 1);                                              //数据库最后一次刷新时戳

            while (true)
            {
                if (bStop && !AGVTask.bRunningFlag)
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

                        while (true)
                        {
                            if (bStop && !AGVTask.bRunningFlag)
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //查询变量最新操作时戳
                            DateTime ts = VarComm.GetLastTime(conn, "AGV");
                            if (ts > oldts)
                            {
                                //读取变量列表
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "bas_comm_getallvar",
                                    new SqlParameter("sectionname", "AGV"));

                                //刷新显示
                                formmain.Invoke(new EventHandler(delegate
                                {
                                    formmain.dgAgv.DataSource = ds.Tables[0];
                                }));
                                oldts = ts;
                            }
                            Thread.Sleep(200);
                        }
                    }
                    catch (Exception ex)
                    {
                        strmsg = "Error: " + ex.Message + " 等待一会儿再试!";
                        formmain.logToView(strmsg);
                        log.Error(strmsg);
                        iTimeWait = 10;
                        continue;
                    }
                }
            }
            bRunningFlag = false;
            log.Info("AGV变量显示线程结束");
        }

        public static void Start()
        {
            //启动AGV变量显示线程
            Task.Run(() => thAgvVarRefreshFunc());
        }

        public static void Stop()
        {
            //停止AGV变量显示线程
            bStop = true;
        }
    }
    #endregion
}
