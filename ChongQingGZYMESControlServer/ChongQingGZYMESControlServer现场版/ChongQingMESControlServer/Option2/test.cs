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
    public static class testTask
    {

        static log4net.ILog log = log4net.LogManager.GetLogger("testTask");
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        //线程控制变量
        public static bool bStop = false;                                                           //线程停止信号
        public static bool bRunningFlag = false;                                                    //线程运行标志
        static int iStage = 0;                                                                      //状态机

        public static void Start()
        {
            //启动任务A控制
            Task.Run(() => thtestTaskFunc());
        }

        public static void Stop()
        {
            //停止任务A控制
            bStop = true;
        }
        public static void thtestTaskFunc()
        {
            string strmsg = "";
            OpcUaClient ua = new OpcUaClient();

            strmsg = "Test任务启动线程启动";
            formmain.logToView(strmsg);
            log.Info(strmsg);

            //初始化数据
            bRunningFlag = true;                                                                    //设置C2-B任务线程运行标志
            while (true)
            {
                if (bStop)                                 //结束线程
                    break;

                //延时等待

                using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnString))
                {
                    try
                    {
                        conn.Open();
                        var t1 = ua.ConnectServer(Properties.Settings.Default.OpcUrl);               //连接OPCUA服务
                        Task.WaitAll(t1);

                        while (true)
                        {
                            if (bStop && !C2_BTask.bRunningFlag && iStage == 0)                     //结束线程
                                break;
                            string state = OpcUaHelper.R_WMSState_Method(ua);//状态空闲
                            strmsg = state;
                            log.Info(strmsg);
                            Thread.Sleep(20);

                        }
                    }
                    catch (Exception ex)
                    {
                        strmsg = "Error: " + ex.Message + " 等待一会儿再试!";
                        formmain.logToView(strmsg);
                        log.Info(strmsg);
                        Thread.Sleep(2000);
                        continue;
                    }
                }
            }
            strmsg = "Test线程停止";
            formmain.logToView(strmsg);
            log.Info(strmsg);
            bRunningFlag = false;                                                                   //设置WMS任务线程停止标志
            return;
        }
    }
}
