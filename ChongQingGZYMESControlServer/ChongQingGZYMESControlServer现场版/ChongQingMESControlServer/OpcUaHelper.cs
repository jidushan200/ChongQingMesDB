using Opc.Ua;
using OpcUaHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChongQingControlServer
{
    public static class OpcUaHelper
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("WmsControl");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;

        #region 常量WMS1             
        const string W_StockerRun = "ns=2;s=PLC1200_1.WMS1.WMS1.W_StockerRun";                   //堆垛机启动命令信号（上升沿）
        const string W_OriginalLocation = "ns=2;s=PLC1200_1.WMS1.WMS1.W_OriginalLocation";             //原始库位
        const string W_TargetLocation = "ns=2;s=PLC1200_1.WMS1.WMS1.W_TargetLocation";               //目标库位
        const string R_StockerFinished = "ns=2;s=PLC1200_1.WMS1.WMS1.R_StockerFinished";              //堆垛机命令执行完成
        const string W_MESUpdated = "ns=2;s=PLC1200_1.WMS1.WMS1.W_MESUpdated";                  //Mes更新完成
        const string R_SeizeA = "ns=2;s=PLC1200_1.WMS1.WMS1.R_SeizeA";
        const string R_SeizeB = "ns=2;s=PLC1200_1.WMS1.WMS1.R_SeizeB";                      //B点托盘到位
        const string R_WMSState = "ns=2;s=PLC1200_1.WMS1.WMS1.R_WMSState";                  //WMS1状态
        const string W_BeltRunA = "ns=2;s=PLC1200_1.WMS1.WMS1.W_BeltRunA";                  //A点传送带转动（上升沿）：托盘离开后，延时2秒停止
        const string W_BeltRunB = "ns=2;s=PLC1200_1.WMS1.WMS1.W_BeltRunB";                  //B点传送带转动（上升沿）：托盘到位后，立即停止
        #endregion
        #region 常量CNC
        const string R_CanIncomeC1 = "ns=2;s=PLC1500.CNC.CNC.R_CanIncomeC1";                          //C1点可进入
        const string W_BeltRunC1 = "ns=2;s=PLC1500.CNC.CNC.W_BeltRunC1";                        //C1点传送带转动（托盘到位后，立即停止）
        const string R_BeltArriveC1 = "ns=2;s=PLC1500.CNC.CNC.R_BeltArriveC1";                    //C1点托盘到位
        const string W_MaterialC1 = "ns=2;s=PLC1500.CNC.CNC.W_MaterialC1";                      //C1点物料属性（上升沿）
        const string R_HandlingCount = "ns=2;s=PLC1500.CNC.CNC.R_HandlingCount";                   //正在加工的物料数量
        const string R_SeizeC2 = "ns=2;s=PLC1500.CNC.CNC.R_SeizeC2";                         //C2托盘占位
        const string R_CanOutC2 = "ns=2;s=PLC1500.CNC.CNC.R_CanOutC2";                  //C2点可出料
        const string W_BeltRunC2 = "ns=2;s=PLC1500.CNC.CNC.W_BeltRunC2";                  //C2点传送带转动（0：停止，1：转动）
        const string R_MaterialC2 = "ns=2;s=PLC1500.CNC.CNC.W_MaterialC2";                  //C2物料属性
        const string W_AskTakeC2 = "ns=2;s=testMES.PLC.CNC.W_AskTakeC2";                  //C2申请拿空托盘
        const string R_AllowTakeC2 = "ns=2;s=PLC1500.CNC.CNC.R_AllowTakeC2";                  //C2准许拿空托盘        
        const string W_TakeFinsihedC2 = "ns=2;s=PLC1500.CNC.CNC.W_TakeFinsihedC2";                  //C2空托盘已拿走
        const string W_InEmptyPallet = "ns=2;s=PLC1500.CNC.CNC.W_InEmptyPallet";                    //C1进空托盘
        const string W_AGVArriveC2 = "ns=2;s=PLC1500.CNC.CNC.W_AGVArriveC2";                    //C2点AGV托盘到位
        const string R_NeedEmptyPallet = "ns=2;s=PLC1500.CNC.CNC.R_NeedEmptyPallet";                    //CNC需要空托盘
        const string W_GetC1Arrived = "ns=2;s=PLC1500.CNC.CNC.W_GetC1Arrived";                    //收到C1托盘到位信号
        const string W_GetD1Arrived = "ns=2;s=PLC1500.CNC.CNC.W_GetD1Arrived";                    //收到D1托盘到位信号
        #endregion
        #region 常量WMS2
        const string R_CanIncomeD1 = "ns=2;s=PLC1500.CNC.CNC.R_CanIncomeD1";                          //D1点可进入
        const string W_BeltRunD1 = "ns=2;s=PLC1500.CNC.CNC.W_BeltRunD1";                        //D1点传送带转动（上升沿）托盘到位后，立即停止
        const string R_BeltArriveD1 = "ns=2;s=PLC1500.CNC.CNC.R_BeltArriveD1";                    //D1点托盘到位
        const string W_MaterialD1 = "ns=2;s=PLC1500.CNC.CNC.W_MaterialD1";                      //D1点物料属性
        const string R_SeizeD2 = "ns=2;s=PLC1500.CNC.CNC.R_SeizeD2";                         //D2点托盘到位
        const string W_BeltRunD2 = "ns=2;s=PLC1500.CNC.CNC.W_BeltRunD2";                             //D2点传送带转动（上升沿）托盘离开后，延时2秒停止
        const string R_PreStationStatus = "ns=2;s=PLC1200_2.WMS2.WMS2.R_PreStationStatus";                  //准备工位无托盘
        const string R_PreStationRFIDFinshied = "ns=2;s=PLC1200_2.WMS2.WMS2.R_PreStationRFIDFinshied";              //准备位扫描完成
        const string R_PreStationPalletNo = "ns=2;s=PLC1200_2.WMS2.WMS2.R_ProductPalletNo";             //准备工位托盘号
        const string W_StockerRun2 = "ns=2;s=PLC1200_2.WMS2.WMS2.W_StockerRun";                          //堆垛机启动命令信号（上升沿）
        const string W_OriginalLocation2 = "ns=2;s=PLC1200_2.WMS2.WMS2.W_OriginalLocation";                  //原始库位
        const string W_TargetLocation2 = "ns=2;s=PLC1200_2.WMS2.WMS2.W_TargetLocation";                  //目标库位
        const string R_StockerFinished2 = "ns=2;s=PLC1200_2.WMS2.WMS2.R_StockerFinished";                  //命令执行完成
        const string W_MESUpdated_Stocker = "ns=2;s=PLC1200_2.WMS2.WMS2.W_MESUpdated_Stocker";                 //堆垛机动作后MES更新                                      
        const string W_RobotType = "ns=2;s=PLC1500.CNC.CNC.W_RobotType";                      //机械手搬移类型（0：不动，1;搬移笔筒，2：搬移成品）
        const string W_RobotStart = "ns=2;s=PLC1500.CNC.CNC.W_RobotStart";                     //机械手搬移开始（上升沿）
        const string R_RobotFinished = "ns=2;s=PLC1500.CNC.CNC.R_RobotFinished";                  //机械手搬移完成
        const string W_MESUpdated_Robert = "ns=2;s=PLC1500.CNC.CNC.W_MESUpdated_Robert";              //机械手搬移后MES更新
        const string R_ProductReady = "ns=2;s=PLC1200_2.WMS2.WMS2.R_ProductReady";                   //入库成品就位
        const string R_ProductPalletNo = "ns=2;s=PLC1200_2.WMS2.WMS2.R_ProductPalletNo";                //成品托盘号
        const string R_WMS2State = "ns=2;s=PLC1200_2.WMS2.WMS2.R_WMSState";                  //WMS2状态
        const string W_AGVArriveD2 = "ns=2;s=PLC1500.CNC.CNC.W_AGVArriveD2";                    //D2点AGV托盘到位
        #endregion
        #region 环线
        const string R_AssemblyPalletExist = "ns=2;s=PLC1200_3.AssemblyLine_1.AssemblyLine.R_AssemblyPalletExist";                          //环线有托盘
        const string R_AssemblyFirstEmtpy = "ns=2;s=PLC1200_3.AssemblyLine_1.AssemblyLine.R_AssemblyFirstEmtpy";                            //环线第一工位空
        const string W_AssemblyStart = "ns=2;s=PLC1200_3.AssemblyLine_1.AssemblyLine.W_AssemblyStart";                               //装配启动
        const string R_AssemblyFinished = "ns=2;s=PLC1200_3.AssemblyLine_1.AssemblyLine.R_AssemblyFinished";                           //装配完成
        const string R_AssemblyFirstRFIDFinished = "ns=2;s=PLC1200_3.AssemblyLine_1.AssemblyLine.R_AssemblyFinished";                         //环线第一工位扫描完成
        const string R_AssemblyFirstPalletNo = "ns=2;s=PLC1200_3.AssemblyLine_1.AssemblyLine.R_AssemblyFirstPalletNo";                              //环线第一工位托盘号
        const string R_AssemblyState = "ns=2;s=PLC1200_3.AssemblyLine_1.AssemblyLine.R_AssemblyState";                                               //环线状态(是否启动)
        const string R_AssemblyFirstStatus = "ns=2;s=PLC1200_3.AssemblyLine_1.AssemblyLine.R_AssemblyFirstStatus";                                   //环线第一工位状态


        #endregion
        static OpcUaHelper()
        {

            //ua = new OpcUaClient();
            //dv = new DataValue();
            //ua = new OpcUaClient();
            //dv = new DataValue();
            //ua = new OpcUaClient();
            //dv = new DataValue();
        }
        //public static void Connect()
        //{
        //    string strmsg = "";
        //    bool isConnect = false;
        //    while (!isConnect)
        //    {
        //        try
        //        {
        //            var t1 = ua.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
        //            Task.WaitAll(t1);
        //            var t2 = ua.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
        //            Task.WaitAll(t2);
        //            var t3 = ua.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
        //            Task.WaitAll(t3);
        //            var t4 = ua.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
        //            Task.WaitAll(t4);
        //            Thread.Sleep(2000);
        //            if (ua.Connected && ua.Connected && ua.Connected && ua.Connected)
        //            {
        //                isConnect = true;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            isConnect = false;
        //            strmsg = "Error: " + ex.Message + " 等待一会儿再试!";
        //            formmain.logToView(strmsg);
        //            log.Info(strmsg);
        //            Thread.Sleep(5000);
        //            continue;
        //        }
        //    }
        //}

        #region 交互方法
        #region WMS1方法
        /// <summary>
        /// 启动命令信号（上升沿）
        /// </summary>
        /// <returns></returns>
        
        public static void W_StockerRun_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_StockerRun, true);
        }
        /// <summary>
        /// 原始库位
        /// </summary>
        /// <param name="oriLocation"></param>
        public static void W_OriginalLocation_Method(OpcUaClient ua,int oriLocation)
        {
            ua.WriteNode(W_OriginalLocation, (UInt16)oriLocation);
        }
        /// <summary>
        /// 目标库位
        /// </summary>
        /// <param name="targetLocation"></param>
        public static void W_TargetLocation_Method(OpcUaClient ua, int targetLocation)
        {
            ua.WriteNode(W_TargetLocation, (UInt16)targetLocation);
        }
        /// <summary>
        /// 命令是否完成
        /// </summary>
        /// <returns></returns>
        public static string R_StockerFinished_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_StockerFinished));
            return dv.Value.ToString();
        }
        /// <summary>
        /// MES更新完成
        /// </summary>
        public static void W_MESUpdated_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_MESUpdated, true);
        }
        /// <summary>
        /// MES更新完成
        /// </summary>
        public static void W_MESUpdatedFinished_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_MESUpdated, false);
        }
        /// <summary>
        /// A点托盘到位
        /// </summary>
        /// <returns></returns>
        public static string R_SeizeA_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_SeizeA));
            return dv.Value.ToString();
        }
        /// <summary>
        /// B点托盘到位
        /// </summary>
        /// <returns></returns>
        public static string R_SeizeB_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_SeizeB));
            return dv.Value.ToString();
        }
        /// <summary>
        /// WMS1状态
        /// </summary>
        /// <returns></returns>
        public static string R_WMSState_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_WMSState));
            return dv.Value.ToString();
        }
        /// <summary>
        /// A点传送带转动（上升沿）
        /// </summary>
        public static void W_BeltRunA_Method(OpcUaClient ua,bool flag)
        {
            ua.WriteNode(W_BeltRunA, flag);
        }
        /// <summary>
        /// B点传送带转动（上升沿）
        /// </summary>
        public static void W_BeltRunB_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_BeltRunB, true);
        }
        #endregion

        #region CNC方法
        /// <summary>
        /// C1点可进入
        /// </summary>
        /// <returns></returns>
        public static string R_CanIncomC1_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_CanIncomeC1));
            return dv.Value.ToString();
        }
        /// <summary>
        /// C1点传送带转动（0：停止，1：转动）
        /// </summary>
        public static void W_BeltRunC1_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_BeltRunC1,true);
        }
        /// <summary>
        /// C1点托盘到位
        /// </summary>
        /// <returns></returns>
        public static string R_BeltArriveC1_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_BeltArriveC1));
            return dv.Value.ToString();
        }
        /// <summary>
        /// C1点物料属性
        /// </summary>
        /// <param name="materialSN"></param>
        public static void W_MaterialC1_Method(OpcUaClient ua,string material)
        {
            //byte[] bytearray = new byte[18];
            //for (int i = 0; i < bytearray.Length; i++)
            //    bytearray[i] = 0;
            //byte[] arr = Encoding.Default.GetBytes(materialSN);
            //for (int i = 0; i < arr.Length; i++)
            //{
            //    if (i < bytearray.Length)
            //    {
            //        bytearray[i] = arr[i];
            //    }
            //}
            //byte[] bytearray = Encoding.Default.GetBytes(material);
            ua.WriteNode(W_MaterialC1, material);
        }
        /// <summary>
        /// 告诉CNC为空托盘
        /// </summary>
        public static void W_InEmptyPallet_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_InEmptyPallet, true);
        }
        /// <summary>
        /// 正在加工的物料数量
        /// </summary>
        /// <returns></returns>
        public static int R_HandlingCount_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_HandlingCount));
            return Convert.ToInt32(dv.Value);
        }
        /// <summary>
        /// C2托盘占位
        /// </summary>
        /// <returns></returns>
        public static string R_SeizeC2_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_SeizeC2));
            return dv.Value.ToString();
        }
        /// <summary>
        /// C2点可出料
        /// </summary>
        /// <returns></returns>
        public static string R_CanOutC2_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_CanOutC2));
            return dv.Value.ToString();
        }
        /// <summary>
        /// C2点传送带转动（0：停止，1：转动）
        /// </summary>
        public static void W_BeltRunC2_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_BeltRunC2, true);
        }
        /// <summary>
        /// C2成品信息
        /// </summary>
        /// <param name="productSN"></param>
        public static string R_MaterialC2_Method(OpcUaClient ua)
        {
            try
            {
                DataValue dv;
                dv = ua.ReadNode(new NodeId(R_MaterialC2));
                return dv.Value.ToString();//可能会出问题
            }
            catch (Exception ex)
            {
                return "出错" + ex.Message.ToString().Substring(0,10);
            }

        }
        /// <summary>
        /// C2申请拿空托盘
        /// </summary>
        public static void W_AskTakeC2_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_AskTakeC2, true);
        }
        /// <summary>
        /// C2准许拿空托盘
        /// </summary>
        /// <returns></returns>
        public static string R_AllowTakeC2_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_AllowTakeC2));
            return dv.Value.ToString();
        }
        /// <summary>
        /// C2空托盘已拿走
        /// </summary>
        public static void W_TakeFinishedC2_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_TakeFinsihedC2, true);
        }
        /// <summary>
        /// C2点AGV托盘到位
        /// </summary>
        /// <returns></returns>
        public static void W_AGVArriveC2_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_AGVArriveC2, true);
        }
        #endregion
        /// <summary>
        /// CNC需要空托盘
        /// </summary>
        /// <returns></returns>
        public static string R_NeedEmptyPallet_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_NeedEmptyPallet));//
            return dv.Value.ToString();
        }
        /// <summary>
        /// 收到C1点货物到位信号
        /// </summary>
        public static void W_GetC1Arrived_Method(OpcUaClient ua,bool flag)
        {
            ua.WriteNode(W_GetC1Arrived, flag);
        }
        /// <summary>
        /// 收到D1点货物到位信号
        /// </summary>
        public static void W_GetD1Arrived_Method(OpcUaClient ua,bool flag)
        {
            ua.WriteNode(W_GetD1Arrived, flag);
        }
        #region WMS2方法
        /// <summary>
        /// D1点可进入
        /// </summary>
        /// <returns></returns>
        public static string  R_CanIncomeD1_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_CanIncomeD1));
            return dv.Value.ToString();
        }
        /// <summary>
        /// D1点传送带转动（0：停止，1：转动）
        /// </summary>
        /// <returns></returns>
        public static void W_BeltRunD1_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_BeltRunD1, true);
        }
        /// <summary>
        /// D1点托盘到位
        /// </summary>
        /// <returns></returns>
        public static string R_BeltArriveD1_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_BeltArriveD1));
            return dv.Value.ToString();
        }
        /// <summary>
        /// D1点物料属性
        /// </summary>
        /// <param name="materialSN"></param>
        public static void W_MaterialD1_Method(OpcUaClient ua,string materialSN)
        {
            //byte[] bytearray = Encoding.Default.GetBytes(materialSN);
            ua.WriteNode(W_MaterialD1, materialSN);
        }
        /// <summary>
        /// D2点托盘到位
        /// </summary>
        /// <returns></returns>
        public static string R_SeizeD2_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_SeizeD2));
            return dv.Value.ToString();
        }
        /// <summary>
        /// D2点传送带转动（0：停止，1：转动）
        /// </summary>
        public static void W_BeltRunD2_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_BeltRunD2, true);
        }
        /// <summary>
        /// 准备工位无托盘
        /// </summary>
        /// <returns></returns>
        public static string R_PreStationStatus_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_PreStationStatus)); 
            return dv.Value.ToString();
        }
        /// <summary>
        ///准备位扫描完成
        /// </summary>
        /// <returns></returns>
        public static string R_PreStationRFIDFinshied_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_PreStationRFIDFinshied));
            return dv.Value.ToString();
        }
        /// <summary>
        /// 准备位托盘号
        /// </summary>
        /// <returns></returns>
        public static int R_PreStationPalletNo_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_PreStationPalletNo));
            return Convert.ToInt32(dv.Value) ;
        }
        /// <summary>
        /// 堆垛机启动命令信号（上升沿）
        /// </summary>
        public static void W_StockerRun2_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_StockerRun2, true);
        }
        /// <summary>
        /// 原始库位
        /// </summary>
        /// <param name="originalLocation"></param>
        public static void W_OriginalLocation2_Method(OpcUaClient ua,int originalLocation)
        {
            ua.WriteNode(W_OriginalLocation2, (UInt16)originalLocation);
        }
        /// <summary>
        /// 目标库位
        /// </summary>
        /// <param name="targetLocation"></param>
        public static void W_TargetLocation2_Method(OpcUaClient ua,int targetLocation)
        {
            ua.WriteNode(W_TargetLocation2, (UInt16)targetLocation);
        }
        /// <summary>
        /// 命令执行完成
        /// </summary>
        /// <returns></returns>
        public static string R_StockerFinished2_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_StockerFinished2));
            return dv.Value.ToString();
        }

        public static void W_MESUpdated_Stocker_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_MESUpdated_Stocker, true);
        }
        public static void W_MESUpdatedFinished_Stocker_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_MESUpdated_Stocker, false);
        }
        /// <summary>
        /// 机械手搬移类型(0:不动，1：搬移笔筒，2：搬移成品
        /// </summary>
        /// <param name="robotType"></param>
        public static void W_RobotType_Method(OpcUaClient ua,int robotType)
        {
            ua.WriteNode(W_RobotType, (UInt16)robotType);
        }
        /// <summary>
        /// 机械手搬移开始（上升沿）
        /// </summary>
        public static void W_RobotStart_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_RobotStart, true);
        }
        /// <summary>
        /// 机械手搬移完成
        /// </summary>
        /// <returns></returns>
        public static string R_RobotFinished_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_RobotFinished));
            return dv.Value.ToString();
        }
        /// <summary>
        /// 机械手动作后MES更新
        /// </summary>
        public static void W_MESUpdated_Robert_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_MESUpdated_Robert, true);
        }

        /// <summary>
        /// 入库成品就位
        /// </summary>
        /// <returns></returns>
        public static string R_ProductReady_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_ProductReady));
            return dv.Value.ToString();
        }
        /// <summary>
        /// 成品准备位托盘号
        /// </summary>
        /// <returns></returns>
        public static int  R_ProductPalletNo_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_ProductPalletNo));
            return Convert.ToInt32(dv.Value);
        }
        /// <summary>
        /// WMS2状态
        /// </summary>
        /// <returns></returns>
        public static string R_WMS2State_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_WMS2State));
            return dv.Value.ToString();
        }
        /// <summary>
        /// D2点AGV托盘到位
        /// </summary>
        /// <returns></returns>
        public static void W_AGVArriveD2_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_AGVArriveD2, true);
        }
        #endregion
        #region 环线方法
        /// <summary>
        /// 环线有托盘
        /// </summary>
        /// <returns></returns>
        public static string R_AssemblyPalletCount_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_AssemblyPalletExist));
            return dv.Value.ToString();
        }
        /// <summary>
        /// 环线第一工位空
        /// </summary>
        public static string  R_AssemblyFirstEmtpy_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_AssemblyFirstEmtpy));
            return dv.Value.ToString();
        }
        /// <summary>
        /// 装配启动
        /// </summary>
        /// <returns></returns>
        public static void  W_AssemblyStart_Method(OpcUaClient ua)
        {
            ua.WriteNode(W_AssemblyStart, true);
        }
        /// <summary>
        /// 装配完成
        /// </summary>
        /// <returns></returns>
        public static string R_AssemblyFinished_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_AssemblyFinished));
            return dv.Value.ToString();
        }
        /// <summary>
        /// 环线第一工位扫描完成
        /// </summary>
        /// <param name="productSN"></param>
        public static string  R_AssemblyFirstRFIDFinished_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_AssemblyFirstRFIDFinished));
            return dv.Value.ToString();
        }
        /// <summary>
        /// 环线第一工位托盘号
        /// </summary>
        /// <returns></returns>
        public static int R_AssemblyFirstPalletNo_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_AssemblyFirstPalletNo));
            return Convert.ToInt32(dv.Value);
        }
        /// <summary>
        /// 环线状态
        /// </summary>
        /// <returns></returns>
        public static string R_AssemblyState_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_AssemblyState));
            return dv.Value.ToString();
        }
        /// <summary>
        /// 环线第一工位状态
        /// </summary>
        /// <returns></returns>
        public static string R_AssemblyFirstStatus_Method(OpcUaClient ua)
        {
            DataValue dv;
            dv = ua.ReadNode(new NodeId(R_AssemblyFirstStatus));
            return dv.Value.ToString();
        }
        #endregion
        #endregion

    }
}

