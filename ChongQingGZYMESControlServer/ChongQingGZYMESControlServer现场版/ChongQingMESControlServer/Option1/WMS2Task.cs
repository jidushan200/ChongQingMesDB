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
    public static class WMS2Task
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("WMS2Task");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        public static bool bProductEnable = false;                                                 //生产使能标志
        //static bool bProductRunning = false;                                                        //生产状态
        //static bool bProductEnable = false;                                                         //生产使能标志

        static int iStage = 0;                                                                      //状态机

        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskWMS2Func());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskWMS2Func()
        {
            string strmsg = "";
            int iTimeWait = 0;

            //临时变量
            int id = 0;                                                                             //订单id
            string ordernumber = String.Empty;                                                      //订单编号
            string productSN = String.Empty;
            string pencupSN = String.Empty;                                                         //原料串号
            int materialid = 0;                                                                     //原料id
            int productid = 0;                                                                      //笔筒成品id
            int dstloc = 0;                                                                         //目标库位号
            int srcloc = 0;                                                                         //原始库位号
            int countflag = 0;
            int palletid = 0;
            string RobotCodeD1 = string.Empty;
            strmsg = "WMS2任务启动线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //初始化数据
            iTimeWait = 0;
            bRunningFlag = true;                                                                    //设置任务线程运行标志
            while (true)
            {
                if (bStop && !WMS2Task.bRunningFlag && iStage == 0)                                 //结束线程
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
                            if (bStop && !WMS2Task.bRunningFlag && iStage == 0)                     //结束线程
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

                            #region 立库状态空闲,做动作判断（0）
                            if (iStage == 0)
                            {
                                if (VarComm.GetVar(conn, "WMS1", "ProductEnable") != "")//订单启动状态
                                {
                                    bProductEnable = true;
                                    VarComm.SetVar(conn, "WMS1", "ProductRunning", "1");//仓库运行标志开始
                                    string ishavePalletD1 = OpcUaHelper.R_BeltArriveD1_Method();//D1点有无托盘
                                    string isArrivedD1 = VarComm.GetVar(conn, "AGV", "ArrivedD1");//AGV小车到达D1
                                    string isArrivedD2 = VarComm.GetVar(conn, "AGV", "ArrivedD2");//AGV小车到达D2
                                    string ishavePalletD2 = OpcUaHelper.R_SeizeD2_Method();//D2点有无托盘
                                    RobotCodeD1 = VarComm.GetVar(conn, "AGV", "RobotCodeD1");
                                    string preStationEmpty = OpcUaHelper.R_PreStationEmpty_Method();//准备位有无托盘
                                    string AssemblyFinished = OpcUaHelper.R_AssemblyFinished_Method();//装配完成
                                    string isProductReady = OpcUaHelper.R_ProductReady_Method();//成品准备位有托盘
                                    string isArriveD2Roll = VarComm.GetVar(conn, "AGV", "ArrivedD2Roll");//D2点小车传送带转动
                                    if (ishavePalletD1 == "False" && isArrivedD1 == "1")//D1无货并且小车到了D1  
                                    {
                                        iStage = 1;//传送带入库
                                    }
                                    else if (ishavePalletD2 == "True" && isArrivedD2 == "1" && isArriveD2Roll == "1")//D2有托盘并且小车到D2
                                    {
                                        iStage = 22;
                                    }
                                    else if (preStationEmpty == "False")//准备位无托盘
                                    {
                                        iStage = 3;//出库
                                    }
                                    else if (preStationEmpty == "True" && ishavePalletD1 == "True")//准备位有托盘并且D1点有托盘
                                    {
                                        string PreHavePenCup = VarComm.GetVar(conn, "WMS2", "PreHavePenCup");//准备位有无笔筒
                                        if (PreHavePenCup == "False")
                                        {
                                            iStage = 8;//搬移笔筒
                                        }
                                        else if (PreHavePenCup == "True")
                                        {
                                            iStage = 10;//判断产线是否开，以及是否是符合产线的笔筒
                                        }
                                    }
                                    else if (AssemblyFinished == "True" && isProductReady == "True")//装配完成&&成品准备位有托盘
                                    {
                                        iStage = 18;//成品入库
                                    }
                                    else
                                    {
                                        iTimeWait = 2;
                                    }
                                }
                                else
                                {
                                    VarComm.SetVar(conn, "WMS2", "ProductRunning", "");//仓库运行标志开始
                                    iTimeWait = 1;
                                }

                            }
                            #endregion
                            #region 传送带转动入库（1）
                            if (iStage == 1)//
                            {
                                OpcUaHelper.W_BeltRunD1_Method();//D1点传送带转动
                                VarComm.SetVar(conn, "WMS2", "BeltRunD1","1");//写入变量
                                iStage = 2;
                            }
                            #endregion
                            #region 入D1点到位（2）
                            if (iStage == 2)
                            {
                                string ishavePalletD1 = OpcUaHelper.R_BeltArriveD1_Method();//D1点有托盘
                                if (ishavePalletD1 == "True")//D1点有托盘
                                {
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_sn_get", new SqlParameter("agvnumber", RobotCodeD1));//查询车上产品串号
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                    {
                                        pencupSN = ds.Tables[0].Rows[0]["pencupSN"].ToString();
                                        VarComm.SetVar(conn, "WMS2", "MaterialSND1", pencupSN);//写入变量
                                        strmsg = $"笔筒到达D1，写入SN号:{pencupSN}";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iStage = 0;
                                    }
                                    else
                                    {
                                        VarComm.SetVar(conn, "WMS2", "MaterialSND1", "");//写入变量
                                        strmsg = $"笔筒到达D1，未能写入SN号，小车上无SN号";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iStage = 0;
                                    }
                                }
                                else
                                {
                                    iTimeWait = 2;
                                }
                            }
                            #endregion

                            #region 准备位无托盘，查看订单情况（3）
                            if (iStage == 3)
                            {
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_order_sel", null);//订单情况
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)//订单未完成
                                {
                                    id = (int)ds.Tables[0].Rows[0]["id"];
                                    productid = (int)ds.Tables[0].Rows[0]["productid"];
                                    ordernumber = ds.Tables[0].Rows[0]["ordernumber"].ToString();
                                    strmsg = $"执行订单{id}";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    countflag = 0;//防止重复提示
                                    iStage = 4;
                                }
                                else
                                {
                                    if (countflag == 0)
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
                            #region 读WMS状态（有库存）（4）
                            if (iStage == 4)
                            {
                                string state = OpcUaHelper.R_WMS2State_Method();//状态空闲
                                int isexists = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "wms_stock_exists_sel", new SqlParameter("type", 1), new SqlParameter("stockid", 2), new SqlParameter("storedtypeid", 2));//0:没有库存,1:有库存

                                if (isexists == 1&& state=="3")
                                {
                                    iStage = 6;
                                }
                                else
                                {
                                    strmsg = "出库等待状态，请等待，时间过长请查看是否有库存";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 5;
                                }
                            }
                            #endregion

                            #region 恢复出库状态（5）
                            if (iStage == 5)
                            {
                                string state = OpcUaHelper.R_WMS2State_Method();//状态空闲
                                string isPrehavePallet = OpcUaHelper.R_PreStationEmpty_Method();//D1点有无托盘
                                int isexists = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "wms_stock_exists_sel", new SqlParameter("type", 1));//0:没有库存,1:有库存
                                if (state == "3" && isPrehavePallet == "False" && isexists == 1)
                                {
                                    iStage = 6;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion

                            #region 出库（6）
                            if (iStage == 6)
                            {
                                //获取出库库位
                                int stockid = 2;
                                int storedtypeid = 2;
                                int typeid = 1;
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//出库库位
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    srcloc = (int)ds.Tables[0].Rows[0]["locationid"];
                                    materialid = (int)ds.Tables[0].Rows[0]["materialid"];
                                    dstloc = 61;//61为出库准备位，62为入库准备位
                                    OpcUaHelper.W_OriginalLocation2_Method(srcloc);//原始库位
                                    OpcUaHelper.W_TargetLocation2_Method(dstloc);//目标库位
                                    strmsg = string.Format("出库参数下发：库位:{0}出库", srcloc);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_StockerRun2_Method();//堆垛机启动命令信号（上升沿）
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                    strmsg = "立库启动信号已发送";
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

                            #region 出库完成到准备位（7）
                            if (iStage == 7)
                            {
                                if (OpcUaHelper.R_StockerFinished_Method() == "True" && OpcUaHelper.R_PreStationEmpty_Method() == "True")//命令执行完成且准备位有货
                                {
                                    strmsg = "出库完成到准备位";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    int srctypeid = 2;

                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stock_typeupdate", new SqlParameter("srcstockid", 2), new SqlParameter("srctypeid", srctypeid), new SqlParameter("locationid", srcloc), new SqlParameter("type", 1), null, null, null);//库位更新
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_ordercnc_update", new SqlParameter("type", 1));//加订单上线数
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    VarComm.SetVar(conn, "WMS2", "PreHavePenCup", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method();//Mes更新完成
                                    strmsg = "MES更新数据完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_productSN_insert", new SqlParameter("productSN ", pencupSN), new SqlParameter("action ", "WMS1出库完成"), new SqlParameter("stationcode ", "WMS1"));//串号更新
                                    //Tracking.Insert(conn, "WMS1", "WMS1出库完成", pencupSN);//串号更新
                                    //strmsg = $"产品串号{pencupSN}更新";
                                    iStage = 0;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion

                            #region 机械手搬移笔筒（8）
                            if (iStage == 8)
                            {
                                OpcUaHelper.W_RobotType_Method(1);//搬移类型
                                OpcUaHelper.W_RobotStart_Method();//机械手启动
                                iStage = 9;//

                            }
                            #endregion

                            #region 机械手搬移完成（9）
                            if (iStage == 9)
                            {
                                string robotFinsihed = OpcUaHelper.R_RobotFinished_Method();//机械手完成
                                if (robotFinsihed == "True")
                                { 
                                    string rfidfinished=OpcUaHelper.R_PreStationRFIDFinshied_Method();//扫描完成
                                    if (rfidfinished=="True")
                                    {
                                        palletid= OpcUaHelper.R_PreStationPalletNo_Method();
                                        //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_pallet_update", new SqlParameter("palletid", palletid), new SqlParameter("type", 1));//托盘更新
                                        strmsg = "机械手搬移完成";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        VarComm.SetVar(conn, "WMS2", "PreHavePenCup", "1");
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_pallet_update", new SqlParameter("type", 1), new SqlParameter("palletid", palletid), new SqlParameter("sn", pencupSN));//托盘更新,写入笔筒串号
                                        iStage = 10;
                                    }
                                    else
                                    {
                                        iTimeWait = 1;
                                    }
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion
                            
                            #region 判断环线第一工位，环线开启，笔筒符合订单（10）
                            if (iStage == 10)
                            {
                                string firstEmpty = OpcUaHelper.R_AssemblyFirstEmtpy_Method();//环线第一工位空
                                string assemblyState= OpcUaHelper.R_AssemblyFirstEmtpy_Method();//环线是否开启
                                string pencupmaterialid = pencupSN.Substring(pencupSN.Length - 1, 1);//取出笔筒原料id
                                string ordermaterialid = string.Empty;
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_ordermaterialid_sel");//取订单原料productid，然后得到笔筒原料
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    ordermaterialid= ds.Tables[0].Rows[0]["materialid"].ToString();
                                    //productid = ds.Tables[0].Rows[0]["id"]!=null? (int)ds.Tables[0].Rows[0]["id"]:99;//99为获取异常
                                }                                    
                                if (firstEmpty == "False"&& assemblyState=="3"&& ordermaterialid== pencupmaterialid)//环线1工位空&&环线开&&笔筒符合订单
                                {
                                    iStage = 11;//搬移笔筒到环线1
                                }
                                else if (ordermaterialid != pencupmaterialid|| assemblyState != "3")//笔筒不符合订单或者环线未开
                                {
                                    iStage = 20;//配件入库
                                }
                            }
                            #endregion
                            #region 搬移准备位托盘到产线（11）
                            if (iStage == 11)
                            {
                                srcloc = 61;
                                dstloc = 63;//61为出库准备位，62为入库准备位
                                OpcUaHelper.W_OriginalLocation2_Method(srcloc);//原始库位
                                OpcUaHelper.W_TargetLocation2_Method(dstloc);//目标库位
                                strmsg = string.Format("出库参数下发：搬移托盘到产线");
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                OpcUaHelper.W_StockerRun2_Method();//堆垛机启动命令信号（上升沿）
                                VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                strmsg = "立库启动信号已发送";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 12;
                            }
                            #endregion
                            #region 搬移准备位托盘到产线（12）
                            if (iStage == 12)
                            {
                                if (OpcUaHelper.R_StockerFinished_Method() == "True" && OpcUaHelper.R_PreStationEmpty_Method() == "False"&& OpcUaHelper.R_AssemblyFirstEmtpy_Method()=="True")//命令执行完成且准备位无货&&第一工位有货
                                {
                                    strmsg = "笔筒搬移进入产线";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_order_update", new SqlParameter("type", 1));//加订单上线数
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    VarComm.SetVar(conn, "WMS2", "PreHavePenCup", "");
                                    VarComm.SetVar(conn, "BELT", "PALLETNUMBER", palletid.ToString());
                                    strmsg = "搬移完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method();//Mes更新完成
                                    strmsg = "MES更新数据完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    productSN= ordernumber + new Random(Guid.NewGuid().GetHashCode()).Next(1000, 9999).ToString() + productid.ToString(); //成品串号：订单编号+4位随机数+成品id
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_pallet_update", new SqlParameter("type", 2), new SqlParameter("palletid", palletid), new SqlParameter("sn", productSN)); //串号写入托盘
                                    Tracking.Insert(conn, "WMS2", "WMS2入环线", productSN);//串号更新
                                    iStage = 13;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion
                            #region 产线装配启动（13）
                            if (iStage == 13)
                            {
                                if (OpcUaHelper.R_AssemblyFirstRFIDFinished_Method() == "true")
                                {
                                    OpcUaHelper.W_AssemblyStart_Method();//产线装配启动
                                    iStage = 14;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }                               
                            }
                            #endregion
                            #region 产线装配完成（14）
                            if (iStage == 14)
                            {
                                if(OpcUaHelper.R_AssemblyFinished_Method()=="True")//产线装配完成
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_order_update", new SqlParameter("type", 2));//加订单完成数
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "bas_station_update", new SqlParameter("type", 2));//加显示时戳
                                    iStage = 15;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion
                            #region 产线装配完成（15）
                            if (iStage == 15)
                            {
                                if (OpcUaHelper.R_AssemblyFinished_Method() == "True")//产线装配完成
                                {
                                    iStage = 0;//跳转入库公共部分
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion
                            #region 出库到环线（16）
                            if (iStage == 16)
                            {
                                int stockid = 2;
                                int storedtypeid = 2;
                                int typeid = 3;
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//出库库位
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    srcloc = (int)ds.Tables[0].Rows[0]["locationid"];
                                    materialid = (int)ds.Tables[0].Rows[0]["materialid"];
                                    dstloc = 62;//61为出库准备位，62为环线准备位
                                    OpcUaHelper.W_OriginalLocation2_Method(srcloc);//原始库位
                                    OpcUaHelper.W_TargetLocation2_Method(dstloc);//目标库位
                                    strmsg = string.Format("出库参数下发：库位:{0}出库", srcloc);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_StockerRun2_Method();//堆垛机启动命令信号（上升沿）
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                    strmsg = "立库启动信号已发送";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 17;
                                }
                            }
                            #endregion
                            #region 出库到环线完成（17）
                            if (iStage == 17)
                            {
                                if (OpcUaHelper.R_StockerFinished_Method() == "True" && OpcUaHelper.R_PreStationEmpty_Method() == "True")//命令执行完成且准备位有货
                                {
                                    strmsg = "出库完成到环线一工位";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    int srctypeid = 2;

                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stock_typeupdate", new SqlParameter("srcstockid", 2), new SqlParameter("srctypeid", srctypeid), new SqlParameter("locationid", srcloc), new SqlParameter("type", 1), null, null, null);//库位更新
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_ordercnc_update", new SqlParameter("type", 1));//加订单上线数
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method();//Mes更新完成
                                    strmsg = "MES更新数据完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    productSN = ordernumber + new Random(Guid.NewGuid().GetHashCode()).Next(1000, 9999).ToString() + productid.ToString(); //成品串号：订单编号+4位随机数+成品id
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_pallet_update", new SqlParameter("type", 2), new SqlParameter("palletid", palletid), new SqlParameter("sn", productSN)); //串号写入托盘
                                    Tracking.Insert(conn, "WMS2", "WMS2入环线", productSN);//串号更新
                                    iStage = 13;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion
                            #region 入库（18）
                            if (iStage == 18)
                            {
                                palletid=OpcUaHelper.R_ProductPalletNo_Method();//托盘号
                                int stockid = 2;
                                int storedtypeid = 4;
                                int typeid = 2;
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//入库库位
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    srcloc = 62;//61为出库准备位，62为入库准备位
                                    dstloc = (int)ds.Tables[0].Rows[0]["locationid"]; ;
                                    OpcUaHelper.W_OriginalLocation2_Method(srcloc);//原始库位
                                    OpcUaHelper.W_TargetLocation2_Method(dstloc);//目标库位
                                    strmsg = string.Format("入库参数下发：入库到库位:{0}入库", dstloc);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_StockerRun2_Method();//堆垛机启动命令信号（上升沿）
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                    strmsg = "立库启动信号已发送";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 19;
                                }
                            }
                            #endregion
                            #region 入库完成（19）
                            if (iStage == 19)
                            {
                                if (OpcUaHelper.R_StockerFinished2_Method() == "True" && OpcUaHelper.R_ProductReady_Method() == "False")//命令执行完成且成品准备位无货
                                {
                                    strmsg = "入库完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_pallet_sel", new SqlParameter("palletid", palletid));//查询SN号
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                    {
                                        productSN = ds.Tables[0].Rows[0]["productSN"].ToString();
                                    }
                                    else
                                    {
                                        productSN = "999";
                                    }
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stock_typeupdate", new SqlParameter("srcstockid", 2), null, new SqlParameter("locationid", dstloc), new SqlParameter("type", 3), null, null, new SqlParameter("productSN ", productSN));//库位更新
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method();//Mes更新完成
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
                            #region 全配件入库（20）
                            if (iStage == 20)
                            {
                                int stockid = 2;
                                int storedtypeid = 4;
                                int typeid = 2;
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//入库库位
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    srcloc = 62;//61为出库准备位，62为入库准备位
                                    dstloc = (int)ds.Tables[0].Rows[0]["locationid"]; ;
                                    OpcUaHelper.W_OriginalLocation2_Method(srcloc);//原始库位
                                    OpcUaHelper.W_TargetLocation2_Method(dstloc);//目标库位
                                    strmsg = string.Format("入库参数下发：入库到库位:{0}入库", dstloc);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_StockerRun2_Method();//堆垛机启动命令信号（上升沿）
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                    strmsg = "立库启动信号已发送";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 21;
                                }
                            }
                            #endregion
                            #region 入库完成（21）
                            if (iStage == 21)
                            {
                                if (OpcUaHelper.R_StockerFinished2_Method() == "True" && OpcUaHelper.R_PreStationEmpty_Method() == "False")//命令执行完成且准备位无货
                                {
                                    string productmaterial = string.Empty;
                                    strmsg = "入库完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    string pencupmaterialid = pencupSN.Substring(pencupSN.Length - 1, 1);//取出笔筒原料id
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_materialid_sel", new SqlParameter("pencupmaterial", pencupmaterialid));//通过笔筒原料id查询配件全的物料号
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                    {
                                        productmaterial = ds.Tables[0].Rows[0]["id"].ToString();//成品配件id
                                    }
                                    else
                                    {
                                        productmaterial = "999";
                                    }
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stock_typeupdate", new SqlParameter("srcstockid", 2), null, new SqlParameter("locationid", dstloc), new SqlParameter("type", 2), new SqlParameter("materialid", productmaterial), null,null);//库位更新
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    VarComm.SetVar(conn, "WMS2", "PreHavePenCup", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method();//Mes更新完成
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
                            #region D2传送带转动（22）
                            if (iStage == 22)
                            {
                                OpcUaHelper.W_BeltRunD2_Method();//D2传送带转动
                                VarComm.SetVar(conn, "WMS2", "BeltRunD2", "1");
                                iStage = 0;                                
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

            strmsg = "Wms2Task线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            return;
        }
    }

    public static class WMS2TaskVarRefresh
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("WMS2TaskVarRefresh");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;

        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志

        public static void thWMS2TaskVarRefreshFunc()
        {
            log.Info("WMS2变量显示线程启动");
            bRunningFlag = true;

            int iTimeWait = 0;
            string strmsg = "";
            DateTime oldts = new DateTime(1970, 1, 1);                                              //数据库最后一次刷新时戳

            while (true)
            {
                if (bStop && !WMS2Task.bRunningFlag)
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
                            if (bStop && !WMS2Task.bRunningFlag)
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //查询变量最新操作时戳
                            DateTime ts = VarComm.GetLastTime(conn, "WMS2");
                            if (ts > oldts)
                            {
                                //读取变量列表
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "bas_comm_getallvar",
                                    new SqlParameter("sectionname", "WMS2"));

                                //刷新显示
                                formmain.Invoke(new EventHandler(delegate
                                {
                                    formmain.dgwms2.DataSource = ds.Tables[0];//修改
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
            log.Info("WMS2Task变量显示线程结束");
        }

        public static void Start()
        {
            //启动BeltTaskC变量显示线程
            Task.Run(() => thWMS2TaskVarRefreshFunc());

        }

        public static void Stop()
        {
            //停止BeltTaskC变量显示线程
            bStop = true;
        }
    }
}
