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
    static public class WMSOneTask
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("WMS1Task");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        public static int iStage = 0;                                                               //状态机
        //public static AutoResetEvent lckTaskData = new AutoResetEvent(true);                      //任务变量锁
        public static bool bProductEnable = false;                                                 //生产使能标志

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
            string pencupSN = String.Empty;                                                         //原料串号
            int materialid = 0;                                                                     //原料id
            int productid = 0;                                                                      //笔筒成品id
            int dstloc = 0;                                                                         //目标库位号
            int srcloc = 0;                                                                         //原始库位号
            string reqcode = String.Empty;
            string taskcode = String.Empty;
            OpcUaClient ua = new OpcUaClient();
            int storedtypeid = 99;
            int outtype = 0;//出库类型:0,初始 1:空托盘 2:原料
            strmsg = "WMS1任务启动线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //初始化数据
            iTimeWait = 0;
            bRunningFlag = true;                                                                    //设置A-C任务线程运行标志
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
                            //0：订单任务未完成（0）
                            //1：WMS出库（物料，库位）（1）
                            //2：WMS出库（空托盘，库位）（2）
                            //3：写入笔筒串号（3）
                            //4：出库完成到接驳位（4）
                            //5：WMS1入库（5）
                            //6：入库完成（6）
                            #region 订单任务未完成（0）
                            if (iStage == 0)
                            {
                                if (VarComm.GetVar(conn, "WMS1", "ProductEnable") != "")//订单启动状态
                                {
                                    VarComm.SetVar(conn, "WMS1", "ProductRunning", "1");//仓库运行标志开始
                                    string havPallet = OpcUaHelper.R_SeizeA_Method(ua);//A点出库准备位有货
                                    string state = OpcUaHelper.R_WMSState_Method(ua);//状态空闲
                                    string seizeC2 = OpcUaHelper.R_SeizeC2_Method(ua);//C2是否有托盘
                                    string canComeIn = OpcUaHelper.R_CanIncomC1_Method(ua);//C1可进入
                                    string seizeB = OpcUaHelper.R_SeizeB_Method(ua);//B点是否有货
                                    string needEmptyPallet= OpcUaHelper.R_NeedEmptyPallet_Method(ua);//CNC需要空托盘
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "sch_ordercnc_sel");//订单情况
                                    int isexistsEmpty = (int)SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "wms_stock_exists_sel", new SqlParameter("type", 2), new SqlParameter("stockid", 1), new SqlParameter("storedtypeid", 4));//0:没有库位,1:有库位
                                    if (state == "3" && isexistsEmpty == 1 && seizeB == "True")//立库空闲&&存在空库位&&B点有货
                                    {
                                        iStage = 5;//空托盘回库
                                    }
                                    else if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && state == "3" && havPallet == "False")//订单未完成
                                    {
                                        id = (int)ds.Tables[0].Rows[0]["id"];
                                        productid = (int)ds.Tables[0].Rows[0]["productid"];//没用到
                                        ordernumber = ds.Tables[0].Rows[0]["ordernumber"].ToString();//笔筒id+2位流水号
                                        iStage = 1;//原料出库
                                    }
                                    else if (ds.Tables[0].Rows.Count == 0&& needEmptyPallet=="True" && havPallet == "False"&&A_CTask.iStage==0 && state == "3")//所有订单完成了&&上线数等于订单数&&空托盘没出够3个(清料)
                                    {
                                        iStage = 2;//空托盘出库
                                    }
                                }
                                else
                                {
                                    VarComm.SetVar(conn, "WMS1", "ProductRunning", "");//仓库运行标志开始
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion
                            #region WMS出库（物料，库位）（1）
                            if (iStage == 1)//WMS出库
                            {
                                string state = OpcUaHelper.R_WMSState_Method(ua);//状态空闲
                                string seizeB = OpcUaHelper.R_SeizeB_Method(ua);
                                if (state == "3" && seizeB == "False" && D2_BTask.inFalg == false)
                                {
                                    //获取出库库位
                                    int stockid = 1;
                                    storedtypeid = 2;
                                    int typeid = 1;
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//出库库位
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                    {
                                        srcloc = (int)ds.Tables[0].Rows[0]["locationid"];
                                        materialid = (int)ds.Tables[0].Rows[0]["materialid"];
                                        outtype = 2; //出库类型位原料
                                        dstloc = 62;//61为出库准备位，62为入库准备位
                                        OpcUaHelper.W_OriginalLocation_Method(ua, srcloc);//原始库位
                                        OpcUaHelper.W_TargetLocation_Method(ua, dstloc);//目标库位
                                        strmsg = string.Format("出库参数下发：库位:{0}出库", srcloc);
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        OpcUaHelper.W_StockerRun_Method(ua);//堆垛机启动命令信号（上升沿）
                                        VarComm.SetVar(conn, "WMS1", "TaskRunning", "1");
                                        VarComm.SetVar(conn, "WMS1", "ApplyAGV", "1");//申请AGV小车
                                        strmsg = "立库启动信号已发送";
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
                                else
                                {
                                    iStage = 0;
                                    continue;
                                }

                            }
                            #endregion
                            #region WMS出库（空托盘，库位）（2）
                            if (iStage == 2)//WMS出库
                            {
                                string state = OpcUaHelper.R_WMSState_Method(ua);//状态空闲
                                string seizeB = OpcUaHelper.R_SeizeB_Method(ua);
                                if (state == "3" && seizeB == "False")
                                {
                                    //获取出库库位
                                    int stockid = 1;
                                    storedtypeid = 1;//出库类型
                                    int typeid = 1;
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//出库库位
                                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                    {
                                        srcloc = (int)ds.Tables[0].Rows[0]["locationid"];
                                        outtype = 1; //出库类型位原料
                                        dstloc = 62;//61为出库准备位，62为入库准备位
                                        OpcUaHelper.W_OriginalLocation_Method(ua, srcloc);//原始库位
                                        OpcUaHelper.W_TargetLocation_Method(ua, dstloc);//目标库位
                                        strmsg = string.Format("出库参数下发：库位:{0}出库", srcloc);
                                        formmain.logToView(strmsg);
                                        log.Info(strmsg);
                                        OpcUaHelper.W_StockerRun_Method(ua);//堆垛机启动命令信号（上升沿）
                                        VarComm.SetVar(conn, "WMS1", "TaskRunning", "1");
                                        VarComm.SetVar(conn, "WMS1", "ProductSN", pencupSN);//串号更新
                                        Tracking.Insert(conn, "WMS1", "WMS1出库", pencupSN);//串号更新
                                        VarComm.SetVar(conn, "WMS1", "ApplyAGV", "1");//申请AGV小车
                                        strmsg = "立库启动信号已发送";
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
                                else
                                {
                                    iStage = 0;
                                    continue;
                                }
                            }
                            #endregion
                            #region 写入笔筒串号（3）
                            if (iStage == 3)
                            {
                                string seizeA = OpcUaHelper.R_SeizeA_Method(ua);

                                if (seizeA == "True")//命令执行完成
                                {
                                    if (outtype == 1)
                                    {
                                        pencupSN = "EmptyPallet";
                                    }
                                    else if (outtype == 2)
                                    {
                                        pencupSN = ordernumber + new Random(Guid.NewGuid().GetHashCode()).Next(100000, 999999).ToString() + materialid.ToString(); //笔筒串号：订单编号+6位随机数+原料毛坯id 改存储过程
                                    }
                                    VarComm.SetVar(conn, "WMS1", "ProductSN", pencupSN);//串号更新
                                    strmsg = $"笔筒串号已写入，串号为{pencupSN}";
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

                            #region 出库完成到接驳位（4）
                            if (iStage == 4)
                            {
                                string stockerFinished = OpcUaHelper.R_StockerFinished_Method(ua);
                                string seizeA = OpcUaHelper.R_SeizeA_Method(ua);

                                if (stockerFinished == "True")//命令执行完成
                                {
                                    strmsg = "出库完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    int srctypeid = storedtypeid;

                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stockout_upd", new SqlParameter("srcstockid", 1), new SqlParameter("srctypeid", srctypeid), new SqlParameter("locationid", srcloc), new SqlParameter("type", 1), new SqlParameter("materialid", ""), new SqlParameter("pencupSN", ""), new SqlParameter("productSN", ""));//库位更新
                                    if (storedtypeid == 2)
                                    {
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_ordercnc_update", new SqlParameter("type", 1));//加CNC上线数
                                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_running_finish", new SqlParameter("id", 1));//更新app上线数（更新running表运行时戳 id=1:cnc,id=2:WMS2）
                                    }
                                    VarComm.SetVar(conn, "WMS1", "TaskRunning", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Method(ua);//Mes更新完成
                                    Thread.Sleep(2000);//为了plc能够读到
                                    OpcUaHelper.W_MESUpdatedFinished_Method(ua);//Mes更新完成清空
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
                            #region  WMS1入库（5）

                            if (iStage == 5)
                            {
                                int stockid = 1;
                                storedtypeid = 4;
                                int typeid = 2;
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "wms_stock_out_sel", new SqlParameter("stockid", stockid), new SqlParameter("storedtypeid", storedtypeid), new SqlParameter("typeid", typeid));//入库库位
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    srcloc = 63;//61为出库准备位，62为入库准备位
                                    dstloc = (int)ds.Tables[0].Rows[0]["locationid"]; ;
                                    OpcUaHelper.W_OriginalLocation_Method(ua, srcloc);//原始库位
                                    OpcUaHelper.W_TargetLocation_Method(ua, dstloc);//目标库位
                                    strmsg = string.Format("入库参数下发：入库到库位:{0}入库", dstloc);
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_StockerRun_Method(ua);//堆垛机启动命令信号（上升沿)
                                    VarComm.SetVar(conn, "WMS1", "TaskRunning", "1");
                                    strmsg = "立库启动信号已发送";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 6;
                                    continue;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                    continue;
                                }
                            }
                            #endregion

                            #region 入库完成（6）
                            if (iStage == 6)
                            {
                                if (OpcUaHelper.R_StockerFinished_Method(ua) == "True")//命令执行完成
                                {
                                    strmsg = "入库完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "wms_stockout_upd", new SqlParameter("srcstockid", 1), new SqlParameter("srctypeid ", 4), new SqlParameter("locationid", dstloc), new SqlParameter("type", 4), new SqlParameter("materialid", 99), new SqlParameter("pencupSN ", ""), new SqlParameter("productSN", ""));//库位更新
                                    VarComm.SetVar(conn, "WMS1", "TaskRunning", "");
                                    strmsg = "库位更新完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    OpcUaHelper.W_MESUpdated_Method(ua);//Mes更新完成
                                    Thread.Sleep(2000);//为了plc能够读到
                                    OpcUaHelper.W_MESUpdatedFinished_Method(ua);//Mes更新完成清空
                                    strmsg = "MES更新数据完成";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 0;
                                }
                            }
                            #endregion

                            //#region 订单全部完成提示（7）
                            //if (iStage == 7)
                            //{
                            //    strmsg = "CNC订单出库已完成";
                            //    formmain.logToView(strmsg);
                            //    log.Info(strmsg);
                            //    iStage = 0;
                            //}
                            //#endregion
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
            strmsg = "WMS1Task线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置AGV主线程停止标志
            using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnString))
            {
                try
                {
                    conn.Open();
                    VarComm.SetVar(conn, "WMS1", "ProductRunning", "");//仓库运行标志停止
                }
                catch
                {
                    conn.Close();
                }

            }
            return;
        }

    }
}
