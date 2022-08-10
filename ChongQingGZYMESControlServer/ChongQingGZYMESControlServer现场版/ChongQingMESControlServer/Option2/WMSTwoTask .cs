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
    public static class WMSTwoTask
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("WMSTwoTask");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        public static string ProductSN = String.Empty;
        static bool bProductRunning = false;                                                        //生产状态
        public static bool bProductEnable = false;                                                         //生产使能标志

        static int iStage = 0;                                                                      //状态机

        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskWMSTwoFunc());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskWMSTwoFunc()
        {
            string strmsg = "";
            int iTimeWait = 0;

            //临时变量
            int id = 0;                                                                             //订单id
            string ordernumber = String.Empty;                                                      //订单编号
            string productSN = String.Empty;                                                        //成品串号
            string pencupSN = String.Empty;                                                         //原料串号
            int materialid = 0;                                                                     //原料id
            int productid = 0;                                                                      //笔筒成品id
            int dstloc = 0;                                                                         //目标库位号
            int srcloc = 0;                                                                         //原始库位号
            int countflag = 0;                                                                      //提示标志位
            int palletid = 0;                                                                       //托盘id
            string RobotCodeD1 = string.Empty;
            int onlinecnt = 0;                                                                      //上线数
            int quantity = 0;                                                                       //当前订单数
            bool isfull = false;                                                                    //立库2满盘标志
            DataSet orderStatus=null;                                                                        //订单情况容器
            OpcUaClient ua = new OpcUaClient();
            strmsg = "WMS2任务启动线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //初始化数据
            iTimeWait = 0;
            bRunningFlag = true;                                                                    //设置任务线程运行标志
            while (true)
            {
                if (bStop && iStage == 0)                                 //结束线程
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
                            if (bStop && iStage == 0)                     //结束线程
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //状态机：
                            //0：准备位无托盘，查看订单情况
                            //1：立库2状态空闲,做动作判断
                            //2：出库2到准备位
                            //3：机械手搬移笔筒
                            //4：机械手搬移笔筒
                            //5：机械手搬移完成
                            //6：判断环线第一工位，环线开启，笔筒符合订单（6）
                            //7：搬移准备位托盘到环线
                            //8：搬移准备位托盘到产线到位
                            //9：出库2全配件到环线
                            //10：出库2到环线完成
                            //11：成品入库2
                            //12：搬移准备位托盘到环线

                            #region 准备位无托盘，查看订单情况（1）
                            if (iStage == 0)
                            {
                                if (VarComm.GetVar(conn, "WMS2", "ProductEnable") != "")
                                {
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_orderfinished_sel", null);//订单完成情况
                                    orderStatus = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_order_sel", null);//订单情况,方便这个订单完成后，马上进行下个订单
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0&& orderStatus.Tables.Count > 0 && orderStatus.Tables[0].Rows.Count > 0)//订单未完成
                                    {
                                        id = (int)orderStatus.Tables[0].Rows[0]["id"];
                                        productid = (int)orderStatus.Tables[0].Rows[0]["productid"];
                                        ordernumber = orderStatus.Tables[0].Rows[0]["ordernumber"].ToString();
                                        onlinecnt = (int)orderStatus.Tables[0].Rows[0]["onlinecnt"];
                                        quantity = (int)orderStatus.Tables[0].Rows[0]["quantity"];
                                        countflag = 0;//防止重复提示
                                        iStage = 1;
                                    }
                                    else if(ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0&&orderStatus.Tables.Count > 0 && orderStatus.Tables[0].Rows.Count==0)//没有下一个订单
                                    {
                                        id = (int)ds.Tables[0].Rows[0]["id"];
                                        productid = (int)ds.Tables[0].Rows[0]["productid"];
                                        ordernumber = ds.Tables[0].Rows[0]["ordernumber"].ToString();
                                        onlinecnt = (int)ds.Tables[0].Rows[0]["onlinecnt"];
                                        quantity = (int)ds.Tables[0].Rows[0]["quantity"];
                                        countflag = 0;//防止重复提示
                                        iStage = 1;
                                    }
                                    else
                                    {
                                        if (countflag == 0)
                                        {
                                            strmsg = "当前产品原料已全部上线";
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            countflag = 1;
                                            iStage = 0;
                                        }

                                    }
                                }
                                else
                                {
                                    VarComm.SetVar(conn, "WMS2", "ProductRunning", "");//仓库运行标志开始
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region 立库2状态空闲,做动作判断（1）
                            if (iStage == 1)
                            {
                                bProductEnable = true;
                                VarComm.SetVar(conn, "WMS2", "ProductRunning", "1");//仓库运行标志开始
                                string ishavePalletD1 = OpcUaHelper.R_BeltArriveD1_Method(ua);//D1点有无托盘
                                string wms2State = OpcUaHelper.R_WMS2State_Method(ua);//立库22状态
                                string ishavePalletD2 = OpcUaHelper.R_SeizeD2_Method(ua);//D2点有无托盘
                                RobotCodeD1 = VarComm.GetVar(conn, "AGV", "RobotCodeD1");
                                string preStationStatus = OpcUaHelper.R_PreStationStatus_Method(ua);//准备位状态(0:初始1:可放2:可取)
                                pencupSN = VarComm.GetVar(conn, "WMS2", "PenCupSN");//串号存入数据库                                                                  
                                string isProductReady = OpcUaHelper.R_ProductReady_Method(ua);//成品准备位有托盘
                                //string PerHaveEmptyPallet = VarComm.GetVar(conn, "WMS2", "PreHaveEmtpyPallet");//准备位有无空托盘
                                string firstEmpty = OpcUaHelper.R_AssemblyFirstEmtpy_Method(ua);//环线第一工位空true代表空，false代表有东西
                                //string PreHavePenCup = VarComm.GetVar(conn, "WMS2", "PreHavePenCup");//准备位有无笔筒
                                string assemblyFirstStatus=OpcUaHelper.R_AssemblyFirstStatus_Method(ua);//环线第一工位状态
                                OpcUaHelper.W_MESUpdatedFinished_Stocker_Method(ua);//Mes更新完成,防止网络出问题，导致没清信号
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", 2), new SqlParameter("storedtypeid", 2), new SqlParameter("typeid", 3));//出库2全料库位
                                if (preStationStatus == "1" && wms2State == "3" )//准备位可放空托盘&&立库2空闲
                                {
                                    strmsg = $"准备位当前状态{preStationStatus}";//记录下，防止扯皮
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 2;//出库2到准备位
                                }
                                //else if (PerHaveEmptyPallet == "1" && ishavePalletD1 == "True" && PreHavePenCup == "")//准备位有托盘并且D1点有托盘
                                //{
                                //    iStage = 4;//搬移笔筒
                                //}
                                else if (preStationStatus == "2" && firstEmpty == "True")//可取&&第一工位空
                                {
                                    iStage = 6;//判断产线是否开，以及是否是符合产线的笔筒
                                }
                                else if ((firstEmpty == "False" || assemblyFirstStatus == "False")&& wms2State == "3"&& isfull == false&& preStationStatus == "2")
                                {
                                    iStage = 13;
                                }
                                else if ((preStationStatus == "0" || preStationStatus == "1") && wms2State == "3"  && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && firstEmpty == "True" && assemblyFirstStatus == "True" && (int)orderStatus.Tables[0].Rows[0]["quantity"] > (int)orderStatus.Tables[0].Rows[0]["onlinecnt"])//准备位没有托盘&&库里有库存&&第一工位空&&上线数小于订单数量&&立库2状态空闲(&& PerHaveEmptyPallet == "")
                                {
                                    iStage = 9;//从库里出全配件给环线
                                }
                                else if (isProductReady == "True" && wms2State == "3" && isfull == false)//装配完成&&成品准备位有托盘&&立库2空闲&&不是满托盘
                                {
                                    iStage = 11;//成品入库2
                                }
                                else
                                {
                                    iStage = 0;
                                    continue;
                                }
                            }
                            #endregion


                            #region 出库2到准备位（2）
                            if (iStage == 2)
                            {
                                string state = OpcUaHelper.R_WMS2State_Method(ua);//状态空闲
                                string preStationStatus = OpcUaHelper.R_PreStationStatus_Method(ua);//准备位状态(0:初始1:可放2:可取)
                                if (state == "3" && preStationStatus == "1")
                                {
                                    strmsg = $"执行订单{ordernumber}";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    //获取出库2库位
                                    int stockid = 2;
                                    int storedtypeid = 2;
                                    int typeid = 1;
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//出库2库位
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                    {
                                        srcloc = (int)ds.Tables[0].Rows[0]["locationid"];
                                        materialid = (int)ds.Tables[0].Rows[0]["materialid"];
                                        dstloc = 61;//61为出库2准备位
                                        OpcUaHelper.W_OriginalLocation2_Method(ua, srcloc);//原始库位
                                        OpcUaHelper.W_TargetLocation2_Method(ua, dstloc);//目标库位
                                        strmsg = string.Format("出库2参数下发：库位:{0}出库2", srcloc);
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        OpcUaHelper.W_StockerRun2_Method(ua);//堆垛机启动命令信号（上升沿）
                                        VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                        strmsg = "立库2启动信号已发送";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iStage = 3;
                                    }
                                    else
                                    {
                                        strmsg = "立库2无库存";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
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

                            #region 出库2完成到准备位（3）
                            if (iStage == 3)
                            {
                                string preStationStatus = OpcUaHelper.R_PreStationStatus_Method(ua);
                                string stockerFinished = OpcUaHelper.R_StockerFinished2_Method(ua);
                                if (stockerFinished == "True" )//命令执行完成
                                {
                                    strmsg = "出库2完成到准备位";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    int srctypeid = 2;
                                    isfull = false;//不是满托盘

                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stock_typeupdate", new SqlParameter("srcstockid", 2), new SqlParameter("srctypeid", srctypeid), new SqlParameter("locationid", srcloc), new SqlParameter("type", 1), null, null, null);//库位更新
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stockout_upd", new SqlParameter("srcstockid", 2), new SqlParameter("srctypeid", srctypeid), new SqlParameter("locationid", srcloc), new SqlParameter("type", 1), new SqlParameter("materialid", ""), new SqlParameter("pencupSN", ""), new SqlParameter("productSN", ""));//库位更新
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    //VarComm.SetVar(conn, "WMS2", "PreHaveEmtpyPallet", "1");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method(ua);//Mes更新完成
                                    Thread.Sleep(2000);
                                    OpcUaHelper.W_MESUpdatedFinished_Stocker_Method(ua);//Mes更新完成
                                    strmsg = "MES更新数据完成";
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

                            #region 机械手搬移笔筒（4）
                            if (iStage == 4)
                            {
                                OpcUaHelper.W_RobotType_Method(ua, 1);//搬移类型
                                OpcUaHelper.W_RobotStart_Method(ua);//机械手启动
                                strmsg = "机械手搬移启动";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                iStage = 5;//

                            }
                            #endregion

                            #region 机械手搬移完成（5）
                            if (iStage == 5)
                            {
                                string robotFinsihed = OpcUaHelper.R_RobotFinished_Method(ua);//机械手完成
                                if (robotFinsihed == "True")
                                {
                                    string rfidfinished = OpcUaHelper.R_PreStationRFIDFinshied_Method(ua);//扫描完成
                                    if (rfidfinished == "True")
                                    {
                                        palletid = OpcUaHelper.R_PreStationPalletNo_Method(ua);
                                        //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_pallet_update", new SqlParameter("palletid", palletid), new SqlParameter("type", 1));//托盘更新
                                        strmsg = "机械手搬移完成";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        VarComm.SetVar(conn, "WMS2", "PreHavePenCup", "1");
                                        pencupSN=VarComm.GetVar(conn, "WMS2", "PenCupSN");//串号存入数据库
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_pallet_update", new SqlParameter("type", 1), new SqlParameter("palletid", palletid), new SqlParameter("sn", pencupSN));//托盘更新,写入笔筒串号
                                        iStage = 0;
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

                            #region 判断环线第一工位，环线开启，笔筒符合订单（6）
                            if (iStage == 6)
                            {
                                string wms2State = OpcUaHelper.R_WMS2State_Method(ua);//立库22状态
                                string firstEmpty = OpcUaHelper.R_AssemblyFirstEmtpy_Method(ua);//环线第一工位空true代表空，false代表有东西
                                pencupSN = VarComm.GetVar(conn, "WMS2", "PenCupSN");//串号存入数据库
                                //string pencupmaterialid = pencupSN.Substring(pencupSN.Length - 1, 1);//取出笔筒原料id
                                //string ordermaterialid = string.Empty;
                                string assemblyFirstStatus = OpcUaHelper.R_AssemblyFirstStatus_Method(ua);//环线第一工位状态（是否开启）
                                string preStationStatus = OpcUaHelper.R_PreStationStatus_Method(ua);//准备位状态(0:初始1:可放2:可取)
                                palletid = OpcUaHelper.R_PreStationPalletNo_Method(ua);
                                DataSet ds1 = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_order_sel", null);//订单情况
                                if (firstEmpty == "True" && assemblyFirstStatus=="True"&& wms2State=="3" && ds1.Tables.Count > 0 && ds1.Tables[0].Rows.Count > 0&& preStationStatus=="2")//环线1工位空&&笔筒符合订单&&环线第一工位开
                                {
                                    iStage = 7;//搬移笔筒到环线1
                                }
                                else 
                                {
                                    iStage = 13;//全配件入库2
                                }
                            }
                            #endregion
                            #region 搬移准备位托盘到环线（7）
                            if (iStage == 7)
                            {
                                srcloc = 61;
                                dstloc = 62;//61为出库2准备位，62为入库2准备位
                                OpcUaHelper.W_OriginalLocation2_Method(ua, srcloc);//原始库位
                                OpcUaHelper.W_TargetLocation2_Method(ua, dstloc);//目标库位
                                strmsg = string.Format("出库2参数下发：搬移准备位托盘到产线");
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                OpcUaHelper.W_StockerRun2_Method(ua);//堆垛机启动命令信号（上升沿）
                                VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                strmsg = "立库2启动信号已发送";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                productSN=ProductSN = ordernumber + new Random(Guid.NewGuid().GetHashCode()).Next(1000, 9999).ToString() + productid.ToString(); //成品串号：订单编号+4位随机数+成品id
                                iStage = 8;
                            }
                            #endregion
                            #region 搬移准备位托盘到产线到位（8）
                            if (iStage == 8)
                            {
                                string stockerFinished=OpcUaHelper.R_StockerFinished2_Method(ua);
                                //string preStationStatus= OpcUaHelper.R_PreStationStatus_Method(ua);
                                if (stockerFinished == "True")//命令执行完成且准备位无货&&第一工位有货
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_order_update", new SqlParameter("type", 1));//加订单上线数
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_running_finish", new SqlParameter("id", 1));//更新app上线数（更新running表运行时戳 id=1:cnc,id=2:WMS2）
                                    strmsg = "托盘搬移进入产线";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    VarComm.SetVar(conn, "BELT", "PalletNum", palletid.ToString());
                                    strmsg = "托盘搬移完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method(ua);//Mes更新完成
                                    Thread.Sleep(2000);
                                    OpcUaHelper.W_MESUpdatedFinished_Stocker_Method(ua);//Mes更新完成
                                    strmsg = "MES更新数据完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    Tracking.Insert(conn, "WMS2", "WMS2入环线", productSN);//串号更新
                                    //VarComm.SetVar(conn, "WMS2", "PreHaveEmtpyPallet", "");
                                    iStage = 0;

                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region 出库2全配件到环线（9）
                            if (iStage == 9)
                            {
                                strmsg = $"执行订单{ordernumber}";
                                formmain.logToView(strmsg);
                                log.Info(strmsg);
                                int stockid = 2;
                                int storedtypeid = 2;
                                int typeid = 3;
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//出库2库位
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    srcloc = (int)ds.Tables[0].Rows[0]["locationid"];
                                    materialid = (int)ds.Tables[0].Rows[0]["materialid"];
                                    dstloc = 62;//61为出库2准备位，62为环线准备位
                                    OpcUaHelper.W_OriginalLocation2_Method(ua, srcloc);//原始库位
                                    OpcUaHelper.W_TargetLocation2_Method(ua, dstloc);//目标库位
                                    strmsg = string.Format("出库2参数下发：库位:{0}出库2", srcloc);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_StockerRun2_Method(ua);//堆垛机启动命令信号（上升沿）
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                    strmsg = "立库2启动信号已发送";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    productSN=ProductSN = ordernumber + new Random(Guid.NewGuid().GetHashCode()).Next(1000, 9999).ToString() + productid.ToString(); //成品串号：订单编号+4位随机数+成品id
                                    iStage = 10;
                                }
                            }
                            #endregion
                            #region 出库2到环线完成（10）
                            if (iStage == 10)
                            {
                                string firstEmpty = OpcUaHelper.R_AssemblyFirstEmtpy_Method(ua);//环线第一工位空true代表空，false代表有东西
                                string stockerFinished = OpcUaHelper.R_StockerFinished2_Method(ua);
                                if (stockerFinished == "True")//命令执行完成且准备位有货
                                {
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_order_update", new SqlParameter("type", 1));//加订单上线数
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_running_finish", new SqlParameter("id", 1));//更新app上线数（更新running表运行时戳 id=1:cnc,id=2:WMS2）
                                    strmsg = "出库2完成到环线一工位";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    isfull = false;//不是满托盘
                                    int srctypeid = 2;//库位是原料
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stockout_upd", new SqlParameter("srcstockid", 2), new SqlParameter("srctypeid", srctypeid), new SqlParameter("locationid", srcloc), new SqlParameter("type", 1), new SqlParameter("materialid", ""), new SqlParameter("pencupSN", ""), new SqlParameter("productSN", ""));//库位更新
                                                                                                                                                                                                                                                                                                                                                                                      //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_ordercnc_update", new SqlParameter("type", 1));//加订单上线数
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method(ua);//Mes更新完成
                                    Thread.Sleep(2000);
                                    OpcUaHelper.W_MESUpdatedFinished_Stocker_Method(ua);//Mes更新完成
                                    strmsg = "MES更新数据完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    Tracking.Insert(conn, "WMS2", "WMS2入环线", productSN);//串号更新
                                    iStage = 0;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion

                            #region 成品入库2（11）
                            if (iStage == 11)
                            {

                                palletid = OpcUaHelper.R_ProductPalletNo_Method(ua).ToString() == "" ? 99 : OpcUaHelper.R_ProductPalletNo_Method(ua);//托盘号
                                if(palletid!=0)
                                {
                                    strmsg = string.Format("扫描到的托盘号为{0}", palletid);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    int stockid = 2;
                                    int storedtypeid = 4;
                                    int typeid = 2;
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//入库2库位
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                    {
                                        isfull = false;//不是满托盘
                                        srcloc = 63;//61为出库2准备位，63为入库2准备位
                                        dstloc = (int)ds.Tables[0].Rows[0]["locationid"]; ;
                                        OpcUaHelper.W_OriginalLocation2_Method(ua, srcloc);//原始库位
                                        OpcUaHelper.W_TargetLocation2_Method(ua, dstloc);//目标库位
                                        strmsg = string.Format("入库2参数下发：入库到库位:{0}入库", dstloc);
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        OpcUaHelper.W_StockerRun2_Method(ua);//堆垛机启动命令信号（上升沿）
                                        VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                        strmsg = "立库2启动信号已发送";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_order_update", new SqlParameter("type", 2));//加订单完成数
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_running_finish", new SqlParameter("id", 2));//更新app上线数（更新running表运行时戳 id=1:cnc,id=2:WMS2）
                                        iStage = 12;
                                    }
                                    else
                                    {
                                        strmsg = "立库2没有空余位置";
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        iStage = 0;
                                        isfull = true;//满托盘
                                        continue;
                                    }
                                }
                                else
                                {
                                    strmsg = string.Format("未读到托盘号，无法入库2");
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iTimeWait = 1;
                                    continue;
                                }
                                
                            }
                            #endregion
                            #region 入库2完成（12）
                            if (iStage == 12)
                            {
                                string stockerFinished = OpcUaHelper.R_StockerFinished2_Method(ua);
                                string productReady = OpcUaHelper.R_ProductReady_Method(ua);
                                if (stockerFinished == "True")//命令执行完成且成品准备位无货
                                {
                                    strmsg = "入库2完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_pallet_sel", new SqlParameter("palletid", palletid));//查询SN号
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                    {
                                        productSN = ds.Tables[0].Rows[0]["productSN"].ToString() == "" ? "999" : ds.Tables[0].Rows[0]["productSN"].ToString();
                                    }
                                    else
                                    {
                                        productSN = "999";//缺损，托盘号未赋值或者没有成品
                                    }
                                    //SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stock_typeupdate", new SqlParameter("srcstockid", 2), null, new SqlParameter("locationid", dstloc), new SqlParameter("type", 3), null, null, new SqlParameter("productSN ", productSN));//库位更新
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stockout_upd", new SqlParameter("srcstockid", 2), new SqlParameter("srctypeid", 4), new SqlParameter("locationid", dstloc), new SqlParameter("type", 3), new SqlParameter("materialid", ""), new SqlParameter("pencupSN", ""), new SqlParameter("productSN", productSN));//库位更新
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method(ua);//Mes更新完成
                                    Thread.Sleep(2000);
                                    OpcUaHelper.W_MESUpdatedFinished_Stocker_Method(ua);//Mes更新完成
                                    strmsg = "MES更新数据完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    Tracking.Insert(conn, "WMS2", "成品入库2", productSN);//串号更新
                                    iStage = 0;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region 全配件入库2（13）
                            if (iStage == 13)
                            {
                                int stockid = 2;
                                int storedtypeid = 4;
                                int typeid = 2;
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//入库2库位
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    srcloc = 61;//61为出库2准备位，62为入库2准备位
                                    dstloc = (int)ds.Tables[0].Rows[0]["locationid"]; ;
                                    OpcUaHelper.W_OriginalLocation2_Method(ua, srcloc);//原始库位
                                    OpcUaHelper.W_TargetLocation2_Method(ua, dstloc);//目标库位
                                    strmsg = string.Format("入库2参数下发：入库2到库位:{0}入库", dstloc);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_StockerRun2_Method(ua);//堆垛机启动命令信号（上升沿）
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "1");
                                    strmsg = "立库2启动信号已发送";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 14;
                                }
                                else
                                {
                                    strmsg = "立库2没有空余位置";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 0;
                                    isfull = true;//满托盘
                                    continue;
                                }
                            }
                            #endregion
                            #region 入库2完成（14）
                            if (iStage == 14)
                            {
                                if (OpcUaHelper.R_StockerFinished2_Method(ua) == "True")//命令执行完成且准备位无货
                                {
                                    string productmaterial = string.Empty;
                                    strmsg = "入库2完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    string pencupmaterialid = pencupSN.Substring(pencupSN.Length - 1, 1);//取出笔筒原料id
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_materialid_sel", new SqlParameter("pencupmaterial", pencupmaterialid));//通过笔筒原料id查询配件全的物料号
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                    {
                                        productmaterial = ds.Tables[0].Rows[0]["id"].ToString() == "" ? "999" : ds.Tables[0].Rows[0]["id"].ToString();//成品配件id
                                    }
                                    else
                                    {
                                        productmaterial = "999";
                                    }
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stockout_upd", new SqlParameter("srcstockid", 2), new SqlParameter("srctypeid", 4), new SqlParameter("locationid", dstloc), new SqlParameter("type", 2), new SqlParameter("materialid", productmaterial), new SqlParameter("pencupSN", ""), new SqlParameter("productSN", ""));//库位更新
                                    VarComm.SetVar(conn, "WMS2", "TaskRunning", "");
                                    VarComm.SetVar(conn, "WMS2", "PreHavePenCup", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Stocker_Method(ua);//Mes更新完成
                                    Thread.Sleep(2000);
                                    OpcUaHelper.W_MESUpdatedFinished_Stocker_Method(ua);//Mes更新完成
                                    strmsg = "MES更新数据完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 0;
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

            strmsg = "Wms2Task线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            bProductEnable = false;
            using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnString))
            {
                conn.Open();
                VarComm.SetVar(conn, "WMS2", "ProductRunning", "");//仓库运行标志停止
            }
            return;
        }
    }
}
