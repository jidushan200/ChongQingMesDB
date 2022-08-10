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
    public static  class CNCTask
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("CNCTask");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志

        static int iStage = 0;                                                                      //状态机

        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thTaskCNCFunc());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thTaskCNCFunc()
        {
            string strmsg = "";
            int iTimeWait = 0;

            //临时变量

            string robotcodeC1 = "";                                                                  //C1点AGV车号
            string robotcodeC2 = "";                                                                  //C2点AGV车号
            string pencupSN = String.Empty;                                                           //笔筒串号
            string productSN = String.Empty;                                                          //产品串号
            int R_HandlingCount = 0;                                                                  //CNC加工物料数
            string ishavePalletC2 = string.Empty;
            string iscanOut = string.Empty;

            strmsg = "CNC任务启动线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //初始化数据
            iTimeWait = 0;
            bRunningFlag = true;                                                                    //设置CNC任务线程运行标志
            while (true)
            {
                if (bStop && !CNCTask.bRunningFlag && iStage == 0)                                 //结束线程
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
                            if (bStop && !CNCTask.bRunningFlag && iStage == 0)                     //结束线程
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //状态机：
                            //0：等待AGV到达,AGV到达后任务跳转（C1,C2）
                            //1：C1到达，判断C1有无货，无货则入，有货等待
                            //2：等待无货
                            //3：C2到达，判断cnc是空托盘，还是带货
                            //4：C2为空托盘，询问是否能出
                            //5：出空托盘
                            //6：C2为笔筒，出笔筒

                            #region 等待AGV到达（0）
                            if (iStage == 0)
                            {
                                ishavePalletC2 = OpcUaHelper.R_SeizeC2_Method();//C2点是否有托盘
                                iscanOut = OpcUaHelper.R_CanOutC2_Method();//C2点是否可以出货,可出货代表有货，不可出代表空托盘
                                int.TryParse(OpcUaHelper.R_HandlingCount_Method(), out R_HandlingCount);//CNC加工物料数，提前刷新下
                                VarComm.SetVar(conn, "CNC", "HandlingCount", R_HandlingCount.ToString());
                                string isArrivedC1 = VarComm.GetVar(conn, "AGV", "ArrivedC1");//2022-5-20用不用确定Agv上有货呢？但是没货去C1干嘛
                                string isArrivedC2 = VarComm.GetVar(conn, "AGV", "ArrivedC2");
                                if(isArrivedC1=="1")
                                {
                                    //获取AGV车号
                                    robotcodeC1 = VarComm.GetVar(conn, "AGV", "RobotCodeC1");
                                    VarComm.SetVar(conn, "CNC", "RobotCodeInC1", robotcodeC1);
                                    DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_sn_get",new SqlParameter("agvnumber", robotcodeC1));//查询车上产品串号
                                    if (ds.Tables.Count>0&&ds.Tables[0].Rows.Count > 0)
                                    {
                                        pencupSN = ds.Tables[0].Rows[0]["pencupSN"].ToString();
                                    }          
                                    strmsg = "AGV已到达C1点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    iStage = 1;
                                }
                                else if(isArrivedC2 == "1")
                                {
                                    //获取AGV车号
                                    robotcodeC2 = VarComm.GetVar(conn, "AGV", "RobotCodeC2");
                                    VarComm.SetVar(conn, "CNC", "RobotCodeInC2", robotcodeC2);
                                    strmsg = "AGV已到达C2点";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);

                                    iStage = 5;//判断C2点是托盘还是货
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion
                            #region C1到达，是否入CNC（1）
                            if (iStage == 1)
                            {
                               if(OpcUaHelper.R_BeltArriveC1_Method()=="True")//C1点有货
                                {
                                    strmsg = "C1点有货，等待入CNC";
                                    formmain.logToView(strmsg);
                                    log.Info(strmsg);
                                    iStage = 2;
                                }
                                else
                                {

                                    iStage = 3;//
                                }
                            }
                            #endregion
                            #region C1有货等待（2）
                            if (iStage == 2)
                            {
                                if (OpcUaHelper.R_BeltArriveC1_Method() == "False")//C1点无货
                                {
                                    iStage = 3;//
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion

                            #region 入CNC（3）
                            if (iStage == 3)
                            {
                                OpcUaHelper.W_BeltRunC1_Method();//传送带转动
                                VarComm.SetVar(conn, "CNC", "BeltRunC1", "1");//传送带转动
                                iStage = 4;
                            }
                            #endregion

                            #region 货物到位，写入SN（4）
                            if (iStage == 4)
                            {
                                string isArrivedC1= OpcUaHelper.R_BeltArriveC1_Method();//C1货物到位
                                if (isArrivedC1 == "True")
                                {
                                    VarComm.SetVar(conn, "CNC", "BeltRunC1", "");//写入变量表，C1点笔筒SN
                                    Tracking.Insert(conn, "CNC", "C1点货物到位", pencupSN);//写入追踪表
                                    VarComm.SetVar(conn, "CNC", "SNInC1", pencupSN);//写入变量表，C1点笔筒SN
                                    VarComm.SetVar(conn, "CNC", "RobotCodeInC1", "");//清空AGV车号
                                    //VarComm.SetVar(conn, "AGV", "ArrivedC1", "");//清空AGV到达C1点标识
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_sn_set", new SqlParameter("agvnumber", robotcodeC1), new SqlParameter("pencupSN", ""));//清空车上产品串号
                                    iStage = 0;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion

                            #region 判断C2点是托盘还是货（5）
                            if (iStage == 5)
                            {
                               ishavePalletC2= OpcUaHelper.R_SeizeC2_Method();//C2点是否有托盘
                               iscanOut=OpcUaHelper.R_CanOutC2_Method();//C2点是否可以出货,可出货代表有货，不可出代表空托盘
                                
                                if (iscanOut == "True"&& ishavePalletC2=="True")//C2点可以出货并且有托盘
                                {
                                    productSN=OpcUaHelper.R_MaterialC2_Method();
                                    VarComm.SetVar(conn, "CNC", "R_CanOutC2", "1");//C2点可出料
                                    VarComm.SetVar(conn, "CNC", "SNInC2", productSN);//写入变量表，C2点笔筒SN
                                    VarComm.SetVar(conn, "CNC", "ProductSN", productSN);//写入变量表，CNC产品SN
                                    iStage = 6;//出货
                                }
                                else if (ishavePalletC2 == "True"&& iscanOut=="false")//C2点有托盘
                                {
                                    productSN = "empty";
                                    VarComm.SetVar(conn, "CNC", "R_CanOutC2", "");//C2点可出料
                                    VarComm.SetVar(conn, "CNC", "SNInC2", productSN);//写入变量表，C2点笔筒SN
                                    VarComm.SetVar(conn, "CNC", "ProductSN", productSN);//写入变量表，CNC产品SN
                                    iStage = 7;//是否可出空托盘
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion

                            #region CNC出货（6）
                            if (iStage == 6)
                            {
                                string isbeltrun=VarComm.GetVar(conn, "AGV", "ArrivedC2Roll");//C2点AGV传送带转动
                                if (isbeltrun=="1")//传送带转动
                                {
                                    OpcUaHelper.W_BeltRunC2_Method();//传送带转动
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "sch_ordercnc_update", new SqlParameter("type", 2));//加订单完成数
                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "bas_station_update", new SqlParameter("type", 1));//加显示时戳
                                    iStage = 0;
                                }
                                else
                                {
                                    iTimeWait = 1;
                                }
                            }
                            #endregion

                            #region 能否出空托盘（7）
                            if (iStage == 7)
                            {
                                
                                if(R_HandlingCount<4)
                                {
                                    string canout = OpcUaHelper.R_AllowTakeC2_Method();//C2点准许拿空托盘
                                    if (canout == "True")
                                    {
                                        iStage = 6;
                                    }else
                                    {
                                        iStage = 5;//不让出说明正在搬货，返回5重新验证是货是托盘
                                    }
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

            strmsg = "CNCControl线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            return;
        }
    }

    class CNCTasVarRefresh
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("CNCTasVarRefresh");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;

        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志

        public static void thCNCTaskVarRefreshFunc()
        {
            log.Info("CNC变量显示线程启动");
            bRunningFlag = true;

            int iTimeWait = 0;
            string strmsg = "";
            DateTime oldts = new DateTime(1970, 1, 1);                                              //数据库最后一次刷新时戳

            while (true)
            {
                if (bStop && !CNCTask.bRunningFlag)
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
                            if (bStop && !CNCTask.bRunningFlag)
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //查询变量最新操作时戳
                            DateTime ts = VarComm.GetLastTime(conn, "CNC");
                            if (ts > oldts)
                            {
                                //读取变量列表
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "bas_comm_getallvar",
                                    new SqlParameter("sectionname", "CNC"));

                                //刷新显示
                                formmain.Invoke(new EventHandler(delegate
                                {
                                    formmain.dgCnc.DataSource = ds.Tables[0];
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
            log.Info("CNCTask变量显示线程结束");
        }

        public static void Start()
        {
            //启动BeltTaskC变量显示线程
            Task.Run(() => thCNCTaskVarRefreshFunc());

        }

        public static void Stop()
        {
            //停止BeltTaskC变量显示线程
            bStop = true;
        }
    }

}
