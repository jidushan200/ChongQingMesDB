using ChongQingControlServer.Option2;
using CommonClass;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChongQingControlServer
{
    public class AGVRouteControl
    {
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        static log4net.ILog log = log4net.LogManager.GetLogger("AgvRouteControl");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        public static List<AgvRouteModel> listRouteModel = new List<AgvRouteModel>();                   //存储结构，存储回调信息
        public static readonly object ObjCallback = new object();                                  //锁
        static ServiceHost host = null;                                                            //Web回调主机
        static int iStage;
        private static void ShowMSG(string msg)
        {
            formmain.logToView(msg);
            log.Info(msg);
        }
        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thAgvRouteFunc());
        }
        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        private static void thAgvRouteFunc()
        {
            string strmsg = string.Empty;
            int iTimeWait = 0;
            string taskcode = string.Empty;
            string reqcode = String.Empty;
            int id;
            string code;
            string subcode;
            string applytime;
            string applyfinished;
            string continuetime;
            string cancel;
            string cmdfinished;
            AgvAnswerModel ret = new AgvAnswerModel();
            //启动AGV回调接收
            string baseAddress = "http://192.168.0.9:9999";
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

            string finished;
            ShowMSG(String.Format("AGV路径控制任务开始"));
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
                using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnString))//jiage chushihua
                {
                    try
                    {
                        conn.Open();
                        //初始化
                        SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_init");//初始化数据表
                        DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_sel");

                        //iRunCnt = ds.Tables[0].Rows.Count; //获取AGV总数量
                        foreach (DataRow dr1 in ds.Tables[0].Rows)
                        {
                            AgvRouteModel agvRouteModel = new AgvRouteModel()
                            {
                                Id = (int)dr1["id"],
                            };
                            listRouteModel.Add(agvRouteModel);//加入到数据list
                        }
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
            while (true)
            {
                if (bStop && !A_CTask.bRunningFlag && !C2_BTask.bRunningFlag && !C2_DTask.bRunningFlag && !D2_BTask.bRunningFlag)                                 //结束线程
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
                            if (bStop && !A_CTask.bRunningFlag && !C2_BTask.bRunningFlag && !C2_DTask.bRunningFlag && !D2_BTask.bRunningFlag)                     //结束线程
                                break;

                            //延时等待
                            if (iTimeWait > 0)
                            {
                                iTimeWait--;
                                Thread.Sleep(1000);
                                continue;
                            }
                            DataSet ds = SQLHelper.QueryDataSet(conn, null, CommandType.StoredProcedure, "agv_control_sel");//
                            int rowindex = 0;
                            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                            {
                                while (rowindex < ds.Tables[0].Rows.Count)
                                {
                                    try
                                    {
                                        applytime = ds.Tables[0].Rows[rowindex]["applytime"].ToString();
                                        applyfinished = ds.Tables[0].Rows[rowindex]["applyfinished"].ToString();
                                        continuetime = ds.Tables[0].Rows[rowindex]["continue"].ToString();
                                        cancel = ds.Tables[0].Rows[rowindex]["cancel"].ToString();
                                        cmdfinished = ds.Tables[0].Rows[rowindex]["cmdfinished"].ToString();
                                        taskcode = ds.Tables[0].Rows[rowindex]["taskcode"].ToString();
                                        code = ds.Tables[0].Rows[rowindex]["code"].ToString();
                                        subcode= ds.Tables[0].Rows[rowindex]["subcode"].ToString();
                                        id = (int)ds.Tables[0].Rows[rowindex]["id"];
                                        //检查任务申请
                                        if (iStage == 0)
                                        {
                                            if (applytime != "" && applyfinished == "")//检查任务申请
                                            {
                                                iStage = 1;//检查任务
                                            }
                                            else if (applyfinished != "" && continuetime != "" && cmdfinished == "")//检查继续操作
                                            {
                                                iStage = 3;//继续任务
                                            }
                                            else if (applyfinished != "" && cancel != "" && cmdfinished == "")//检查取消操作
                                            {
                                                iStage = 5;//取消任务
                                            }
                                            else if (applyfinished != "" && continuetime != "" && cancel != "")//检查完成操作
                                            {
                                                iStage = 7;//完成任务
                                            }
                                        }

                                        if (iStage == 1)//
                                        {
                                            try
                                            {
                                                reqcode = Guid.NewGuid().ToString().Substring(0,32);                                     //AGV请求代码
                                                taskcode = SQLHelper.ExecuteScalar(conn, null, CommandType.StoredProcedure, "bas_gencode_req",
                                                                           new SqlParameter("code", "agv_taskcode")).ToString();            //AGV任务代码

                                                //创建新任务
                                                strmsg = String.Format("创建AGV任务{0}，reqcode:{1},taskcode:{2}", code, reqcode, taskcode);
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);
                                                ret = AgvAPI.CreatTask(reqcode, taskcode, code, subcode);//创建任务
                                                
                                                if (ret.code == "0")
                                                {
                                                    iStage = 2;
                                                }
                                                else
                                                {
                                                    iStage = 0;
                                                    strmsg = String.Format("创建AGV任务{0}失败,{1}", code, ret.code);
                                                    formmain.logToView(strmsg);
                                                    log.Error(strmsg);
                                                    iTimeWait = 2;

                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                iStage = 0;
                                                strmsg = String.Format("创建AGV任务{0}失败", code);
                                                formmain.logToView(strmsg);
                                                log.Error(strmsg);
                                                iTimeWait = 2;
                                                
                                            }
                                        }
                                        if (iStage == 2)
                                        {
                                            strmsg = string.Format("创建AGV任务{0}成功，reqcode：{1}", code, ret.reqCode);
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_applyfinished_upd", new SqlParameter("id", id), new SqlParameter("taskcode", taskcode));//设置申请完成标志
                                            try
                                            {
                                                Monitor.Enter(ObjCallback);
                                                int Index = listRouteModel.Select((s, index) => new { Value = s.Id, Index = index }).Where(t => t.Value == id).Select(t => t.Index).First(); //通过id找到对应的下标索引
                                                listRouteModel[Index].Taskcode = taskcode;
                                                listRouteModel[Index].Flag = true;
                                            }
                                            finally
                                            {
                                                Monitor.Exit(ObjCallback);
                                            }
                                            iStage = 0;
                                        }

                                        if (iStage == 3)
                                        {
                                            try
                                            {
                                                reqcode = Guid.NewGuid().ToString().Substring(0, 32);                                       //AGV请求代码

                                                strmsg = String.Format("继续AGV任务{0}，reqcode:{1},taskcode:{2}", code, reqcode, taskcode);//继续任务
                                                formmain.logToView(strmsg);
                                                log.Info(strmsg);
                                                ret = AgvAPI.ContinueTask(reqcode, taskcode, subcode);//继续任务

                                                if (ret.code == "0")
                                                {
                                                    iStage = 4;
                                                }
                                                else
                                                {
                                                    iStage = 0;
                                                    strmsg = String.Format("继续AGV任务{0}失败,{1}", code, ret.code);
                                                    formmain.logToView(strmsg);
                                                    log.Error(strmsg);
                                                    iTimeWait = 2;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                iStage = 0;
                                                strmsg = String.Format("继续AGV任务{0}失败", code);
                                                formmain.logToView(strmsg);
                                                log.Error(strmsg);
                                                iTimeWait = 2;
                                            }

                                        }
                                        if (iStage == 4)
                                        {
                                            strmsg = string.Format("继续AGV任务{0}成功，reqcode：{1}", code, ret.reqCode);
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_cmdfinished_upd", new SqlParameter("id", id)); //设置操作完成
                                            iStage = 0;
                                        }

                                        if (iStage == 5)
                                        {
                                            //code = ds.Tables[0].Rows[rowindex]["code"].ToString();
                                            try
                                            {
                                                reqcode = reqcode = Guid.NewGuid().ToString().Substring(0, 32);                             //AGV请求代码

                                                //取消任务
                                                strmsg = string.Format("取消AGV任务，reqcode：{0}，taskcode：{1}", reqcode, taskcode);
                                                log.Info(strmsg);
                                                ret = AgvAPI.CancelTask(reqcode, taskcode);

                                                if (ret.code == "0")
                                                {
                                                    iStage = 6;
                                                }
                                                else
                                                {
                                                    iStage = 0;
                                                    strmsg = String.Format("取消AGV任务{0}失败,{1}", code, ret.code);
                                                    formmain.logToView(strmsg);
                                                    log.Error(strmsg);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                iStage = 0;
                                                strmsg = String.Format("取消AGV任务{0}失败", code);
                                                formmain.logToView(strmsg);
                                                log.Error(strmsg);
                                            }
                                        }
                                        if (iStage == 6)
                                        {
                                            strmsg = "取消AGV任务成功，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_cmdfinished_upd", new SqlParameter("id", id));//继续时间写数据库，清除申请时间
                                            iStage = 0;
                                        }
                                        if (iStage == 7)//完成任务
                                        {
                                            //code = ds.Tables[0].Rows[rowindex]["code"].ToString();
                                            try
                                            {
                                                reqcode = Guid.NewGuid().ToString().Substring(0, 32);                              //AGV请求代码

                                                //取消任务
                                                strmsg = string.Format("完成AGV任务，reqcode：{0}，taskcode：{1}", reqcode, taskcode);
                                                log.Info(strmsg);
                                                ret = AgvAPI.ContinueTask(reqcode, taskcode, subcode);

                                                if (ret.code == "0")
                                                {
                                                    iStage = 8;
                                                }
                                                else
                                                {
                                                    iStage = 0;
                                                    strmsg = String.Format("完成AGV任务{0}失败,{1}", code, ret.code);
                                                    formmain.logToView(strmsg);
                                                    log.Error(strmsg);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                iStage = 0;
                                                strmsg = String.Format("完成AGV任务{0}失败", code);
                                                formmain.logToView(strmsg);
                                                log.Error(strmsg);
                                            }
                                        }
                                        if (iStage == 8)
                                        {
                                            strmsg = "完成AGV任务成功，reqcode：" + ret.reqCode;
                                            formmain.logToView(strmsg);
                                            log.Info(strmsg);
                                            SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_finishedend_upd", new SqlParameter("code", code), new SqlParameter("taskcode", taskcode));//继续时间写数据库，清除申请时间
                                            iStage = 0;
                                        }
                                        try
                                        {
                                            Monitor.Enter(ObjCallback);
                                            foreach (AgvRouteModel item in listRouteModel)
                                            {
                                                if (item.Flag == false&& item.Taskcode!=null)//如果回调内容不为空&&其他为空
                                                {
                                                    SQLHelper.ExecuteNonQuery(conn, null, CommandType.StoredProcedure, "agv_control_arrived_upd", new SqlParameter("taskcode", item.Taskcode), new SqlParameter("arrivalcode", item.CurrentCode), new SqlParameter("robotcode", item.Robotcode));//会不会导致锁出问题
                                                    strmsg = String.Format("AGV车{0}到达{1}点,写入数据库", taskcode, item.CurrentCode);//继续任务
                                                    formmain.logToView(strmsg);
                                                    log.Info(strmsg);
                                                    item.Flag = true;//false时候可以做修改
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            Monitor.Exit(ObjCallback);
                                        }
                                        rowindex++;
                                    }
                                    catch(Exception ex)
                                    {
                                        iStage = 0;
                                        strmsg = string.Format("Error: {0},数据表第{1}行出错" ,ex.Message,rowindex);//继续任务
                                        formmain.logToView(strmsg);
                                        log.Error(strmsg);
                                        rowindex++;
                                        continue;
                                    }
                                }
                            }
                            //检查是否有新的任务

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
            try
            {
                host.Close();
                strmsg = "关闭AGV回调接收服务成功";
                formmain.logToView(strmsg);
                log.Info(strmsg);

                //OpcUaHelper.Disconnect();先不关闭
            }
            catch (Exception ex)
            {
                strmsg = "关闭AGV回调接收服务失败：" + ex.Message;
                formmain.logToView(strmsg);
                log.Error(strmsg);
            }
            ShowMSG(String.Format("AGV路径控制任务结束"));
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
                    Monitor.Enter(AGVRouteControl.ObjCallback);
                    int Index = AGVRouteControl.listRouteModel.Select((s, index) => new { Value = s.Taskcode, Index = index }).Where(t => t.Value == taskcode).Select(t => t.Index).First(); //通过taskcode找到对应的下标索引
                    if (AGVRouteControl.listRouteModel[Index].Flag == true)
                    {
                        AGVRouteControl.listRouteModel[Index].Robotcode = robotcode;
                        AGVRouteControl.listRouteModel[Index].CurrentCode = destcode;
                        AGVRouteControl.listRouteModel[Index].Flag = false;//未写入数据库时候，置位false；写入数据库后，置位true
                        b = true;
                    }
                }
                catch(Exception ex)
                {
                    strmsg = String.Format(ex.Message);
                    formmain.logToView(strmsg);
                    log.Error(strmsg);
                }
                finally
                {
                    Monitor.Exit(AGVRouteControl.ObjCallback);  //  释放锁
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

    public class AgvRouteModel
    {
        public int Id { get; set; }
        public string Taskcode { get; set; }
        public string Robotcode { get; set; }
        public string CurrentCode { get; set; }
        public bool Flag { get; set; }

    }
}
