using AppAPI.CommonClass;
using CommonClass;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace AppAPI.AppHandlers
{
    /// <summary>
    /// 订单控制APP后台API
    /// </summary>
    public class AppOrderCNCHandler : IHttpHandler
    {
        private HttpRequest Request = null;
        private HttpResponse Response = null;

        //定义lasttime为DateTime类型并给它设置一个时间
        DateTime lasttimestamp = DateTime.MinValue, lasttexectimestamp = DateTime.MinValue;

        //WebSocket socket = null;
        public void ProcessRequest(HttpContext context)
        {
            Request = context.Request;
            Response = context.Response;
            context.Response.ContentType = "text/plain";

            string method = Request.Params["method"];
            switch (method)
            {
                case "Query":
                    Query();
                    break;
                case "QueryInfo":
                    QueryInfo();
                    break;
                case "OpenWebSocket":
                    OpenWebSocket(context);
                    break;
                case "BtnState":
                    BtnState();
                    break;
                case "MoveUp":
                    MoveUp();
                    break;
                case "MoveDown":
                    MoveDown();
                    break;
                case "RunState":
                    RunState();
                    break;
            }
        }

        //查询数据
        private void Query()
        {
            BootStrapTable_ReturnStruct rModel = new BootStrapTable_ReturnStruct();
            rModel.State = false;

            DataSet ds = SQLHelper.QueryDataSet(CommandType.StoredProcedure, "app_sch_ordercnc_sel", null);
            rModel.State = true;
            rModel.total = ds.Tables[0].Rows[0][0].ToString();
            rModel.rows = JSONHelper.DataTableToList(ds.Tables[0]);
            Response.Write(JSONHelper.ObjectToJSON(rModel));
            Response.End();
        }

        private void QueryInfo()
        {
            API_ReturnStruct rModel = new API_ReturnStruct();
            rModel.State = false;
            rModel.Msg = "";

            DataSet ds = SQLHelper.QueryDataSet(CommandType.StoredProcedure, "app_sch_ordercnc_info_sel", null);
            rModel.State = true;
            rModel.RData.Add("Data0", JSONHelper.DataTableToList(ds.Tables[0]));
            Response.Write(JSONHelper.ObjectToJSON(rModel));
            Response.End();
        }

        //开启Websocket
        private void OpenWebSocket(HttpContext httpContext)
        {
            if (httpContext.IsWebSocketRequest)
                httpContext.AcceptWebSocketRequest(ProcessChatAsync);
        }

        private async Task ProcessChatAsync(AspNetWebSocketContext socketContext)
        {
            DateTime lasttime = DateTime.MinValue;                //最后更新显示的时间

            WebSocket socket = null;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);

            string station_code;
            if (!string.IsNullOrEmpty(Request.Params["station_code"]))
            {
                station_code = Request.Params["station_code"].ToString();
            }
            else
                station_code = null;
            try
            {
                using (socket = socketContext.WebSocket)
                {
                    int i = 0;
                    while (true)
                    {
                        if (socket.State == WebSocketState.Open)
                        {
                            DataSet ds = SQLHelper.QueryDataSet(CommandType.StoredProcedure, "app_sch_order_check", new SqlParameter("station_code", station_code));      //查询最近的更新时间
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                //判断数据有更新
                                if (!string.IsNullOrEmpty(ds.Tables[0].Rows[0][0].ToString()) && (DateTime)ds.Tables[0].Rows[0][0] > lasttime)
                                {
                                    buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("updated"));
                                    await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                    //保留本次更新时间
                                    lasttime = (DateTime)ds.Tables[0].Rows[0][0];
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                        Thread.Sleep(200);
                        if (i++ > 100)
                        {
                            i = 0;
                            GC.Collect();
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                GC.Collect();
            }
        }

        private void BtnState()
        {
            API_ReturnStruct rModel = new API_ReturnStruct();
            rModel.State = false;
            rModel.Msg = "";
            Int64 order_type;
            if (!string.IsNullOrEmpty(Request.Params["order_type"]))
            {
                order_type = Convert.ToInt64(Request.Params["order_type"]);
            }
            else
                order_type = -1;

            DataSet ds = SQLHelper.QueryDataSet(CommandType.StoredProcedure, "sch_running_sel", new SqlParameter("order_type", order_type));
            rModel.State = true;

            rModel.RData.Add("Data0", JSONHelper.DataTableToList(ds.Tables[0]));
            Response.Write(JSONHelper.ObjectToJSON(rModel));
            Response.End();
        }

        private void MoveUp()
        {
            API_ReturnStruct rModel = new API_ReturnStruct();
            rModel.State = false;
            rModel.Msg = "";

            string id = "";
            try
            {
                if (!string.IsNullOrEmpty(Request.Params["id"]))
                {
                    id = Request.Params["id"].ToString();
                }
                else
                    id = null;

                SQLHelper.ExecuteNonQuery(CommandType.StoredProcedure, "app_sch_ordercnc_move_up", new SqlParameter("id", id));
                rModel.State = true;
                Response.Write(JSONHelper.ObjectToJSON(rModel));
            }
            catch (Exception ex)
            {
                rModel.Msg = ex.Message;
                Response.Write(JSONHelper.ObjectToJSON(rModel));
            }
            Response.End();
        }

        private void MoveDown()
        {
            API_ReturnStruct rModel = new API_ReturnStruct();
            rModel.State = false;
            rModel.Msg = "";

            string id = "";
            try
            {
                if (!string.IsNullOrEmpty(Request.Params["id"]))
                {
                    id = Request.Params["id"].ToString();
                }
                else
                    id = null;

                SQLHelper.ExecuteNonQuery(CommandType.StoredProcedure, "app_sch_ordercnc_move_down", new SqlParameter("id", id));
                rModel.State = true;
                Response.Write(JSONHelper.ObjectToJSON(rModel));
            }
            catch (Exception ex)
            {
                rModel.Msg = ex.Message;
                Response.Write(JSONHelper.ObjectToJSON(rModel));
            }
            Response.End();
        }

        private void RunState()
        {
            API_ReturnStruct rModel = new API_ReturnStruct();
            rModel.State = false;
            rModel.Msg = "";

            Int32 cmd;
            Int32 order_type;

            if (!string.IsNullOrEmpty(Request.Params["cmd"]))
            {
                cmd = Convert.ToInt32(Request.Params["cmd"]);
            }
            else
                cmd = -1;
            if (!string.IsNullOrEmpty(Request.Params["order_type"]))
            {
                order_type = Convert.ToInt32(Request.Params["order_type"]);
            }
            else
                order_type = -1;

            try
            {
                DataSet ds = SQLHelper.QueryDataSet(CommandType.StoredProcedure, "sch_running_cmd",
                    new SqlParameter("cmd", cmd), new SqlParameter("order_type", order_type));
                rModel.State = true;
                rModel.Msg = (order_type == 1) ? "CNC订单 指令下发完毕" : "环线订单 指令下发完毕";
                Response.Write(JSONHelper.ObjectToJSON(rModel));
            }
            catch (Exception ex)
            {
                rModel.Msg = ex.Message;
                Response.Write(JSONHelper.ObjectToJSON(rModel));
            }
            Response.End();
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}
