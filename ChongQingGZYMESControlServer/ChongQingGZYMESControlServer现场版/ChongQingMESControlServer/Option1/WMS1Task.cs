using CommonClass;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChongQingControlServer
{
    public static class WMS1Task
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("WMS1Task");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        public static bool bProductEnable = false;                                                         //生产使能标志
        //static bool bProductRunning = false;                                                        //生产状态
        //static bool bProductEnable = false;                                                         //生产使能标志

        static int iStage = 0;                                                                      //状态机
        
        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskWMS1Func());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskWMS1Func()
        {
            string strmsg = "";
            int iTimeWait = 0;

            //临时变量
            int id = 0;                                                                             //订单id
            string ordernumber = String.Empty;                                                      //订单编号
            string pencupSN = String.Empty;                                                                        //原料串号
            int materialid = 0;                                                                     //原料id
            int productid = 0;                                                                      //笔筒成品id
            int dstloc = 0;                                                                         //目标库位号
            int srcloc = 0;                                                                         //原始库位号
            int countflag = 0;

            strmsg = "WMS1任务启动线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //初始化数据
            iTimeWait = 0;
            bRunningFlag = true;                                                                    //设置A-C任务线程运行标志
            while (true)
            {
                if (bStop && !WMS1Task.bRunningFlag && iStage == 0)                                 //结束线程
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
                            if (bStop && !WMS1Task.bRunningFlag && iStage == 0)                     //结束线程
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //状态机：
                            //0：立库状态空闲
                            //1：入库
                            //2：订单任务是否完成
                            //3：读WMS状态（准备位无托盘，有库存）
                            //4：恢复出库状态
                            //5：出库
                            //6：出库完成到接驳位
                            //7：A点传送带转动

                            #region 立库状态空闲（0）
                            if (iStage == 0) 
                            {
                                if(VarComm.GetVar(conn, "WMS1", "ProductEnable")!="")
                                {
                                    bProductEnable = true;
                                    VarComm.SetVar(conn, "WMS1", "ProductRunning", "1");
                                    string havPalletB = OpcUaHelper.R_SeizeB_Method();//B点入库准备位
                                    string isArrivedB = VarComm.GetVar(conn, "AGV", "ArrivedB");
                                    if (havPalletB == "False" && isArrivedB == "1")//准备位无货
                                    {
                                        OpcUaHelper.W_BeltRunB_Method();//传送带入库
                                        VarComm.SetVar(conn, "WMS1", "BeltRunB", "1");
                                        strmsg = "传动带B转动";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);

                                    }
                                    string isArrivedA = VarComm.GetVar(conn, "AGV", "ArrivedA");
                                    string isArrivedARoll = VarComm.GetVar(conn, "AGV", "ArrivedARoll");//到达A点的小车转送带转动
                                    if (OpcUaHelper.R_SeizeA_Method() == "True" && isArrivedA == "1" && isArrivedARoll == "1")//A点有货并且有小车
                                    {
                                        OpcUaHelper.W_BeltRunA_Method();
                                        strmsg = "传动带A转动";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);

                                    }
                                    string state = OpcUaHelper.R_WMSState_Method();//状态空闲
                                    if (state == "3")
                                    {

                                        iStage = 1;
                                    }
                                    else
                                    {
                                        iTimeWait = 1;
                                    }
                                }
                                else
                                {
                                    VarComm.SetVar(conn, "WMS1", "ProductRunning", "");//仓库运行标志开始
                                    iTimeWait = 1;
                                }
                            }
                            #endregion
                            #region 入库（1）
                            if (iStage == 1)//立库空闲，就可以入库
                            {
                                string seizeB = OpcUaHelper.R_SeizeB_Method();
                                if(seizeB == "True") VarComm.SetVar(conn, "WMS1", "BeltRunB", "");
                                int isexistsEmpty = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "wms_stock_exists_sel", new SqlParameter("type", 2), new SqlParameter("stockid", 1), new SqlParameter("storedtypeid", 4));//0:没有库位,1:有库位
                                if(seizeB=="True"&& isexistsEmpty==1)
                                {
                                    
                                    int stockid = 1;
                                    int storedtypeid = 4;
                                    int typeid = 2;
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//入库库位
                                    if (ds.Tables.Count > 0&& ds.Tables[0].Rows.Count > 0)
                                    {
                                        srcloc = 62;//61为出库准备位，62为入库准备位
                                        dstloc = (int)ds.Tables[0].Rows[0]["locationid"]; ;
                                        OpcUaHelper.W_OriginalLocation_Method(srcloc);//原始库位
                                        OpcUaHelper.W_TargetLocation_Method(dstloc);//目标库位
                                        strmsg = string.Format("入库参数下发：入库到库位:{0}入库", dstloc);
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        OpcUaHelper.W_StockerRun_Method();//堆垛机启动命令信号（上升沿）
                                        VarComm.SetVar(conn, "WMS1", "TaskRunning", "1");
                                        strmsg = "立库启动信号已发送";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iStage = 8;
                                    }
                                    else
                                    {
                                        iStage = 2;//
                                    }
                                }
                                else
                                {
                                    iStage = 2;
                                }
                            }
                            #endregion
                            #region 订单任务是否完成（2）
                            if (iStage == 2)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_ordercnc_sel", null);//订单情况
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    id = (int)ds.Tables[0].Rows[0]["id"];
                                    productid= (int)ds.Tables[0].Rows[0]["productid"];//没用到
                                    ordernumber = ds.Tables[0].Rows[0]["ordernumber"].ToString();//笔筒id+2位流水号                                   
                                    strmsg = $"执行订单{id}";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    countflag = 0;//防止重复提示
                                    iStage = 3;
                                }
                                else
                                {
                                    if(countflag==0)
                                    {
                                        strmsg = "当前无订单";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        countflag = 1;
                                        iStage = 0;
                                    }
                                }
                            }
                            #endregion

                            #region 读WMS状态（准备位无托盘，有库存）（3）
                            if (iStage == 3)
                            {
                                string havPallet = OpcUaHelper.R_SeizeA_Method();//A点出库准备位有货
                                int isexists = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "wms_stock_exists_sel", new SqlParameter("type",1), new SqlParameter("stockid", 1), new SqlParameter("storedtypeid", 2));//0:没有库存,1:有库存
                                
                                if (havPallet=="False"&& isexists==1)
                                {
                                    iStage = 5;
                                }
                                else
                                {
                                    strmsg = "出库等待状态，请等待，时间过长请查看是否有库存";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 4;
                                }
                            }
                            #endregion

                            #region 恢复出库状态（4）
                            if (iStage == 4)
                            {
                                string state = OpcUaHelper.R_WMSState_Method();//状态空闲
                                string havPallet = OpcUaHelper.R_SeizeA_Method();//A点出库准备位有货
                                int isexists = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "wms_stock_exists_sel", new SqlParameter("type", 1));//0:没有库存,1:有库存
                                if (state == "3" && havPallet == "False" && isexists == 1)
                                {
                                    iStage = 5;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion

                            #region 出库（5）
                            if (iStage == 5)
                            {
                                //获取出库库位
                                int stockid = 1;
                                int storedtypeid = 2;
                                int typeid = 1;
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//出库库位
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    srcloc = (int)ds.Tables[0].Rows[0]["locationid"];
                                    materialid= (int)ds.Tables[0].Rows[0]["materialid"];
                                    pencupSN = ordernumber+ new Random(Guid.NewGuid().GetHashCode()).Next(100000, 999999).ToString()+ materialid.ToString(); //笔筒串号：订单编号+6位随机数+原料毛坯id
                                    dstloc = 61;//61为出库准备位，62为入库准备位
                                    OpcUaHelper.W_OriginalLocation_Method(srcloc);//原始库位
                                    OpcUaHelper.W_TargetLocation_Method(dstloc);//目标库位
                                    strmsg = string.Format("出库参数下发：库位:{0}出库",srcloc);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_StockerRun_Method();//堆垛机启动命令信号（上升沿）
                                    VarComm.SetVar(conn, "WMS1", "TaskRunning", "1");
                                    strmsg = "立库启动信号已发送";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 6;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }

                            }    
                            #endregion

                            #region 出库完成到接驳位（6）
                            if (iStage == 6)
                            {
                                if(OpcUaHelper.R_StockerFinished_Method()=="True" && OpcUaHelper.R_SeizeA_Method()=="True")//命令执行完成且A点有货
                                {
                                    strmsg = "出库完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    int srctypeid = 2;
                                    
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stock_typeupdate", new SqlParameter("srcstockid", 1),new SqlParameter("srctypeid", srctypeid), new SqlParameter("locationid", srcloc),new SqlParameter("type",1),null,null,null);//库位更新
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_ordercnc_update", new SqlParameter("type", 1));//加CNC上线数
                                    VarComm.SetVar(conn, "WMS1", "TaskRunning", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Method();//Mes更新完成
                                    strmsg = "MES更新数据完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    VarComm.SetVar(conn, "WMS1", "ProductSN", pencupSN); ;//串号更新
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_productSN_insert", new SqlParameter("productSN ", pencupSN), new SqlParameter("action ", "WMS1出库完成"), new SqlParameter("stationcode ", "WMS1"));//串号更新
                                    Tracking.Insert(conn, "WMS1", "WMS1出库完成", pencupSN);//串号更新
                                    strmsg = $"产品串号{pencupSN}更新";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 7;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion

                            #region A点传送带转动（7）
                            if (iStage == 7)
                            {
                                iStage = 0;//2022-5-18可能会有问题，先测试，后修改,动作迁移到0

                            }
                            #endregion

                            #region 入库完成（8）
                            if (iStage == 8)
                            {
                                if (OpcUaHelper.R_StockerFinished_Method() == "True" && OpcUaHelper.R_SeizeB_Method() == "False")//命令执行完成且A点有货
                                {
                                    strmsg = "入库完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stock_typeupdate", new SqlParameter("srcstockid", 1), null, new SqlParameter("locationid", dstloc), new SqlParameter("type", 4), null, null, null);//库位更新
                                    VarComm.SetVar(conn, "WMS1", "TaskRunning", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Method();//Mes更新完成
                                    strmsg = "MES更新数据完成";
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
                        iTimeWait = 10;
                        continue;
                    }
                }
            }

            strmsg = "Wms1Control线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            return;
        }
    }

    class WMS1TaskVarRefresh
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("WMS1TaskVarRefresh");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;

        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志

        public static void thWMS1TaskVarRefreshFunc()
        {
            log.Info("WMS1变量显示线程启动");
            bRunningFlag = true;

            int iTimeWait = 0;
            string strmsg = "";
            DateTime oldts = new DateTime(1970, 1, 1);                                              //数据库最后一次刷新时戳

            while (true)
            {
                if (bStop && !WMS1Task.bRunningFlag)
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
                            if (bStop && !WMS1Task.bRunningFlag)
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //查询变量最新操作时戳
                            DateTime ts = VarComm.GetLastTime(conn, "WMS1");
                            if (ts > oldts)
                            {
                                //读取变量列表
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "bas_comm_getallvar",
                                    new SqlParameter("sectionname", "WMS1"));

                                //刷新显示
                                formmain.Invoke(new EventHandler(delegate
                                {
                                    formmain.dgWms.DataSource = ds.Tables[0];//修改
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
            log.Info("WMS1Task变量显示线程结束");
        }

        public static void Start()
        {
            //启动BeltTaskC变量显示线程
            Task.Run(() => thWMS1TaskVarRefreshFunc());

        }

        public static void Stop()
        {
            //停止BeltTaskC变量显示线程
            bStop = true;
        }
    }

}
