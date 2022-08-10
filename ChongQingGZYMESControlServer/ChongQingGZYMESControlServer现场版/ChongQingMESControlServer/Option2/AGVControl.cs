using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChongQingControlServer.Option2
{

    [ServiceBehavior(UseSynchronizationContext = false, InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class AgvCallback : IAgvCallback
    {
        static FormMain formmain = (FormMain)MyApplicationContext.CurrentContext.MainForm;
        log4net.ILog log = log4net.LogManager.GetLogger("AgvCallback");
        //private static readonly object ObjCallback = new object();                                     //锁对象
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
                    Monitor.Enter(A_CTask.ObjA_C1);   //进入临界区，防止其他线程同时写入
                    AgvTaskData data = A_CTask.TaskDataA_C1;

                    if (data.taskcode == taskcode)
                    {
                        data.callbacktime = DateTime.Now;
                        data.robotcode = robotcode;
                        b = true;
                    }
                }                    
                finally
                {
                    Monitor.Exit(A_CTask.ObjA_C1);  //  退出临界区
                }
                try
                {
                    Monitor.Enter(C2_BTask.ObjC2_B);   //进入临界区，防止其他线程同时写入
                    AgvTaskData data1 = C2_BTask.TaskDataC2_B;

                    if (data1.taskcode == taskcode)
                    {
                        data1.callbacktime = DateTime.Now;
                        data1.robotcode = robotcode;
                        b = true;
                    }
                }
                finally
                {
                    Monitor.Exit(C2_BTask.ObjC2_B);  //  退出临界区
                }
                try
                {
                    Monitor.Enter(C2_DTask.ObjC2_D1);   //进入临界区，防止其他线程同时写入
                    AgvTaskData data2 = C2_DTask.TaskDataC2_D1;

                    if (data2.taskcode == taskcode)
                    {
                        data2.callbacktime = DateTime.Now;
                        data2.robotcode = robotcode;
                        b = true;
                    }
                }
                finally
                {
                    Monitor.Exit(C2_DTask.ObjC2_D1);  //  退出临界区
                }
                try
                {
                    Monitor.Enter(D2_BTask.ObjD2_B);   //进入临界区，防止其他线程同时写入
                    AgvTaskData data3 = D2_BTask.TaskDataD2_B;

                    if (data3.taskcode == taskcode)
                    {
                        data3.callbacktime = DateTime.Now;
                        data3.robotcode = robotcode;
                        b = true;
                    }
                }
                finally
                {
                    Monitor.Exit(D2_BTask.ObjD2_B);  //  退出临界区
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
}
