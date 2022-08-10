using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChongQingControlServer.Option2;
using CommonClass;
using HPSocket;
using HPSocket.Adapter;
using HPSocket.Tcp;

namespace ChongQingControlServer
{
    public class AGVBeltControl
    {
        public class BinaryDataReceiveAdapter : FixedSizeDataReceiveAdapter1<byte[]>
        {
            ///////////////////////////////定包长///////////////////////////////////////
            /// <summary>
            /// 构造函数调用父类构造, 指定定长包长度
            /// </summary>
            public BinaryDataReceiveAdapter()
                : base(
                    packetSize: 6// 固定包长1K字节
                )
            {
            }
 
            /// <summary>
            /// 解析请求体
            /// <remarks>子类必须覆盖此方法</remarks>
            /// </summary>
            /// <param name="data">父类处理好的定长数据</param>
            /// <returns></returns>
            public override byte[] ParseRequestBody(byte[] data)
            {
                // 因为继承自FixedSizeDataReceiveAdapter<byte[]>, 所以这里直接返回了, 如果是其他类型, 请做完转换在返回
                return data;
            }

        }


        public static ITcpServer<byte[]> tcpPackServer = new TcpServer<byte[]>
        {
            Address = "192.168.0.202",
            Port = 5555,
            // 指定数据接收适配器
            DataReceiveAdapter = new BinaryDataReceiveAdapter(),
        };

        class Agv
        {
            private string _robotCode;
            /// <summary>
            /// 车号
            /// </summary>
            public string RobotCode
            {
                get { return _robotCode; }
                set { _robotCode = value; }
            }
            private string _robotIP;
            /// <summary>
            /// IP地址
            /// </summary>
            public string RobotIP
            {
                get { return _robotIP; }
                set { _robotIP = value; }
            }

            private IntPtr _robotConnId;
            /// <summary>
            /// 连接ID
            /// </summary>
            public IntPtr RobotConnId
            {
                get { return _robotConnId; }
                set { _robotConnId = value; }
            }
            private int _execReply;
            /// <summary>
            /// 执行结果 1:装货完成，2：卸货完成
            /// </summary>
            public int ExecReply
            {
                get { return _execReply; }
                set { _execReply = value; }
            }

        }
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        static log4net.ILog log = log4net.LogManager.GetLogger("AgvBeltControl");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;

        static List<Agv> listAgv = new List<Agv>();//agv列表
        static byte[] cmd_heartbeat = { 0xFC, 0x01, 0x00, 0x00, 0x00, 0xAD };//心跳
        static byte[] cmd_loading = { 0xFC, 0x01, 0x01, 0x01, 0x00, 0xAD };//装货指令
        static byte[] cmd_unloading = { 0xFC, 0x01, 0x01, 0x02, 0x00, 0xAD };//卸货指令
        static byte[] cmd_loadend = { 0xFC, 0x01, 0x02, 0x01, 0x00, 0xAD };//装货完成指令
        static byte[] cmd_unloadend = { 0xFC, 0x01, 0x03, 0x02, 0x00, 0xAD };//卸货完成指令
        //static string heartbeat = "FC01000000AD";//心跳
        //static string loading = "FC01010100AD";//装货指令
        //static string unloading = "FC01020100AD";//卸货指令
        //static string loadend = "FC01020100AD";//装货完成指令
        //static string unloadend = "FC01020200AD";//卸货完成指令
        static List<Thread> listThread = new List<Thread>();//agv列表


        private static void ShowMSG(string msg)
        {
            formmain.logToView(msg);
            log.Info(msg);
        }
        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thAgvBeltInitFunc());
        }
        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thAgvBeltInitFunc()
        {

            tcpPackServer.OnPrepareListen += new ServerPrepareListenEventHandler(server_OnPrepareListen);//监听
            tcpPackServer.OnAccept += new ServerAcceptEventHandler(server_OnAccept);//连接请求
            tcpPackServer.OnSend += new ServerSendEventHandler(server_OnSend);//发数据
            tcpPackServer.OnParseRequestBody += new HPSocket.Adapter.ParseRequestBody<HPSocket.ITcpServer,byte[]>(server_OnParseRequestBody);//接收数据
            tcpPackServer.OnClose += new ServerCloseEventHandler(server_OnClose);//连接关闭
            tcpPackServer.OnShutdown += new ServerShutdownEventHandler(server_OnShutdown);//服务器关闭

            string strmsg = "";
            int iTimeWait = 0;

            iTimeWait = 0;
            bRunningFlag = true;

            ShowMSG("AGV传送带Socket任务线程启动");

            //读取和初始化AGVBelt数据
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

                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_init");//初始化Agv状态

                        DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel");

                        foreach (DataRow dr1 in ds.Tables[0].Rows)
                        {
                            Agv agv = new Agv()
                            {
                                RobotCode = dr1["robotcode"].ToString(),
                                RobotIP = dr1["robotip"].ToString(),
                                RobotConnId = IntPtr.Zero
                            };
                            listAgv.Add(agv);//加入到数据list
                        }
                        ShowMSG("AGV数据初始化完成");
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
            //启动Agv传送带控制线程
            foreach (Agv agv in listAgv)
            {
                //object obj = agv.RobotCode;
                //Thread t = new Thread(new ParameterizedThreadStart(thAgvBeltFunc));
                //t.Start(obj);
                Task.Run(() => thAgvBeltFunc(agv.RobotCode));

            }
            //启动AGVBelt Socket服务
            try
            {
                ShowMSG("启动AGV Socket服务");
                if (!tcpPackServer.Start())
                {
                    throw new Exception(string.Format("原因：{0}，错误代码：{1}", tcpPackServer.ErrorMessage, tcpPackServer.ErrorCode));
                }
            }
            catch (Exception ex)
            {
                ShowMSG("TCP_Pack_Server启动失败:" + ex.Message);
            }
            while (true)
            {
                if (bStop && !A_CTask.bRunningFlag && !C2_BTask.bRunningFlag && !C2_DTask.bRunningFlag && !D2_BTask.bRunningFlag)                                 //结束线程
                    break;
                Thread.Sleep(200);
            }
            tcpPackServer.Stop();
            bRunningFlag = false;
            ShowMSG("AGV传送带Socket任务线程已结束");
        }
        private static void thAgvBeltFunc(string robotcode)
        {
            //string robotcode = o.ToString();
            ShowMSG(String.Format("AGV{0}传送带任务启动", robotcode));
            string strmsg = "";
            int iTimeWait = 0;
            int iStage = 0;
            int beltcmd = 0;
            int Index = 0;
            byte[] cmd = new byte[6];
            while (true)
            {
                if (bStop && iStage == 0 && !A_CTask.bRunningFlag && !C2_BTask.bRunningFlag && !C2_DTask.bRunningFlag && !D2_BTask.bRunningFlag)                                 //结束线程
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
                        while (true)
                        {
                            if (bStop && iStage == 0 && !A_CTask.bRunningFlag && !C2_BTask.bRunningFlag && !C2_DTask.bRunningFlag && !D2_BTask.bRunningFlag)                     //结束线程
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }

                            //状态机：
                            //0：查询传送带命令
                            //1：发送传送带命令
                            //2：等待传送带命令完成

                            #region 查询传送带命令（0）
                            if (iStage == 0)
                            {
                                Index = listAgv.Select((s, index) => new { Value = s.RobotCode, Index = index }).Where(t => t.Value == robotcode).Select(t => t.Index).First();//通过code查找
                                Send(cmd_heartbeat, listAgv[Index].RobotConnId);//回一个心跳，置位小车指令
                                DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_info_sel", new SqlParameter("robotcode", robotcode));
                                if (ds.Tables[0].Rows.Count > 0)
                                {
                                    DataRow r = ds.Tables[0].Rows[0];
                                    if (r["beltcmd"] != DBNull.Value)
                                    {
                                        beltcmd = (int)r["beltcmd"];
                                        ShowMSG(String.Format("Agv{0}发现待执行命令:{1}", robotcode, beltcmd));
                                        iStage = 1;
                                    }
                                }
                            }
                            #endregion
                            #region 发送传送带命令（1）
                            if (iStage == 1)
                            {
                                Index = listAgv.Select((s, index) => new { Value = s.RobotCode, Index = index }).Where(t => t.Value == robotcode).Select(t => t.Index).First();//通过code查找
                                if (listAgv[Index].RobotConnId != IntPtr.Zero)
                                {
                                    if (beltcmd == 1)
                                    {
                                        AGVBeltControl.Send(AGVBeltControl.cmd_loading, listAgv[Index].RobotConnId);//装货指令
                                        ShowMSG(String.Format("Agv{0}装货命令已发送", robotcode));
                                        cmd = AGVBeltControl.cmd_loading;
                                    }
                                    else
                                    {
                                        AGVBeltControl.Send(AGVBeltControl.cmd_unloading, listAgv[Index].RobotConnId);//卸货指令
                                        ShowMSG(String.Format("Agv{0}卸货命令已发送", robotcode));
                                        cmd = AGVBeltControl.cmd_unloading;
                                    }

                                    listAgv[Index].ExecReply = 0;
                                    iStage = 2;
                                }
                            }
                            #endregion
                            #region 等待传送带命令完成（2）
                            if (iStage == 2)
                            {
                                Index = listAgv.Select((s, index) => new { Value = s.RobotCode, Index = index }).Where(t => t.Value == robotcode).Select(t => t.Index).First();//通过code查找
                                if (listAgv[Index].ExecReply == 0)
                                {
                                    AGVBeltControl.Send(cmd, listAgv[Index].RobotConnId);//装货指令
                                }
                                if (listAgv[Index].ExecReply == 1)
                                {
                                    ShowMSG(String.Format("Agv{0}装货完成", robotcode));
                                    iStage = 3;
                                }
                                else if (listAgv[Index].ExecReply == 2)
                                {
                                    ShowMSG(String.Format("Agv{0}卸货完成", robotcode));
                                    iStage = 4;
                                }
                            }
                            #endregion
                            #region 装货完成通知（3）
                            if (iStage == 3)
                            {
                                SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltstat", new SqlParameter("robotcode", robotcode), new SqlParameter("beltstat", 1));//状态写入数据库（装货完成）
                                iStage = 0;
                            }
                            #endregion
                            #region 卸货完成通知（4）
                            if (iStage == 4)
                            {
                                SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_info_setbeltstat", new SqlParameter("robotcode", robotcode), new SqlParameter("beltstat", 2));//状态写入数据库（卸货完成）令
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
                        log.Error(strmsg);
                        iTimeWait = 10;
                        continue;
                    }

                }
            }
            foreach(Agv agv in listAgv)
            {
                AGVBeltControl.tcpPackServer.Disconnect(agv.RobotConnId, true);
                strmsg = "关闭AGV TCP服务成功";
                formmain.logToView(strmsg);
                log.Info(strmsg);
            }
            ShowMSG(String.Format("AGV{0}传送带任务结束", robotcode));
            bRunningFlag = false;                                                                   //设置AGV主线程停止标志
            return;

        }
        public static void Send(byte[] bytes, IntPtr connId)
        {
            string ip = "";
            ushort port = 0;
            if (bytes.Length < 1)
            {
                ShowMSG("发送数据长度为0，被放弃");
                return;
            }
            tcpPackServer.GetRemoteAddress(connId, out ip, out port);
            //if (!Enumerable.SequenceEqual(cmd_heartbeat, bytes))
            //    ShowMSG(string.Format("发送TCP消息：IP:{0} ，内容：{1}", ip, Utils.ToHexStrFromBytes(bytes, true)));
            tcpPackServer.Send(connId, bytes, bytes.Length);
        }
        #region 事件处理方法
        private static HandleResult server_OnPrepareListen(IServer sender, IntPtr soListen)
        {
            ShowMSG("TCP事件：AGV传送带TCP服务端已经开始监听");

            return HandleResult.Ok;
        }

        private static HandleResult server_OnAccept(IServer sender, IntPtr connId, IntPtr pClient)
        {
            string ip = "";
            ushort port = 0;
            ShowMSG(string.Format("TCP事件：接受客户端连接请求，连接ID：{0}", connId));
            tcpPackServer.GetRemoteAddress(connId, out ip, out port);
            if (listAgv.Where(t => t.RobotIP == ip).Count() > 0)
            {
                int Index = listAgv.Select((s, index) => new { Value = s.RobotIP, Index = index })
                    .Where(t => t.Value == ip)
                    .Select(t => t.Index).First();//通过ip查找
                listAgv[Index].RobotConnId = connId;
            }
            return HandleResult.Ok;
        }

        private static HandleResult server_OnSend(IServer sender, IntPtr connId, byte[] bytes)
        {
            
            string ip = "";
            ushort port = 0;
            tcpPackServer.GetRemoteAddress(connId, out ip, out port);
            //ShowMSG(string.Format("{0}:{1}", Utils.ToHexStrFromBytes(bytes, true), ip));
            //byte[] bytemeg = new byte[6];
            //Array.ConstrainedCopy(bytes, 0, bytemeg, 0, 6);
            //if (!Enumerable.SequenceEqual(cmd_heartbeat, bytemeg))//是心跳就不发送信息
            //{
            //    ShowMSG(string.Format("TCP事件：已发送TCP消息：IP:{0} ，内容：{1}", ip, Utils.ToHexStrFromBytes(bytes, true)));
            //}
            return HandleResult.Ok;
        }

        private static HandleResult server_OnParseRequestBody(IServer sender, IntPtr connId, byte[] bytes)
        {
            //ShowMSG(string.Format("{0}", Utils.ToHexStrFromBytes(bytes, true)));
            string ip = "";
            ushort port = 0;
            tcpPackServer.GetRemoteAddress(connId, out ip, out port);//通过connid获取ip
            byte[] bytemeg = new byte[6];
            if(bytes.Length>=6)
            {
                Array.ConstrainedCopy(bytes, 0, bytemeg, 0, 6);
            }
            if (Enumerable.SequenceEqual(cmd_loadend, bytemeg))//装货完成cmd_loadend
            {
                if (listAgv.Where(t => t.RobotIP == ip).Count() > 0)
                {
                    int Index = listAgv.Select((s, index) => new { Value = s.RobotIP, Index = index }).Where(t => t.Value == ip).Select(t => t.Index).First();//通过ip查找
                    listAgv[Index].ExecReply = 1;
                    //ShowMSG(string.Format("AGV{0}装货完成", listAgv[Index].RobotCode));
                }
            }
            else if (Enumerable.SequenceEqual(cmd_unloadend, bytemeg))//卸货完成
            {
                if (listAgv.Where(t => t.RobotIP == ip).Count() > 0)
                {
                    int Index = listAgv.Select((s, index) => new { Value = s.RobotIP, Index = index }).Where(t => t.Value == ip).Select(t => t.Index).First();//通过ip查找
                    listAgv[Index].ExecReply = 2;
                    //ShowMSG(string.Format("AGV{0}卸货完成", listAgv[Index].RobotCode));
                }
            }

            return HandleResult.Ok;
        }

        //当触发了OnClose事件时，表示连接已经被关闭，并且OnClose事件只会被触发一次
        //通过errorCode参数判断是正常关闭还是异常关闭，0表示正常关闭
        private static HandleResult server_OnClose(IServer sender, IntPtr connId, SocketOperation enOperation, int errorCode)
        {
            if (errorCode == 0)
            {
                string ip = "";
                ushort port = 0;
                tcpPackServer.GetRemoteAddress(connId, out ip, out port);
                int Index = listAgv.Select((s, index) => new { Value = s.RobotIP, Index = index })
                    .Where(t => t.Value == ip)
                    .Select(t => t.Index).First();//通过ip查找
                listAgv[Index].RobotConnId = IntPtr.Zero;
                ShowMSG(string.Format("TCP事件：连接已断开，连接ID：{0}", connId));
            }
            else
            {
                ShowMSG(string.Format("TCP事件：客户端连接发生异常，已经断开连接，连接ID：{0}，错误代码：{1}", connId, errorCode));
            }

            return HandleResult.Ok;
        }

        private static HandleResult server_OnShutdown(IServer sender)
        {

            ShowMSG("TCP事件：AGV传送带TCP服务端已经停止");

            return HandleResult.Ok;
        }

        #endregion

    }
}
