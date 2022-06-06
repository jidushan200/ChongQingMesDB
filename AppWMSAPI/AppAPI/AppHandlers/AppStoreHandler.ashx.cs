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
    /// 立库APP后台API
    /// </summary>
    public class AppStoreHandler : IHttpHandler
    {
        public class paramUpdate
        {
            public Int64 store_id { get; set; }
            public string stocknums { get; set; }
            public string changeName { get; set; }
            public string actionName { get; set; }
        }

        private HttpRequest Request = null;
        private HttpResponse Response = null;

        //定义lasttime为DateTime类型并给它设置一个时间
        DateTime lasttime = DateTime.MinValue;                //最后更新显示的时间

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
                case "ChangeState":
                    ChangeState();
                    break;
                case "OpenWebSocket":
                    OpenWebSocket(context);
                    break;
            }
        }

        //查询数据
        private void Query()
        {
            Int64 store_id = 0;
            if (!string.IsNullOrEmpty(Request.Params["store_id"]))
                store_id = Convert.ToInt64(Request.Params["store_id"].ToString());
            else
                store_id = 0;

            API_ReturnStruct rModel = new API_ReturnStruct();
            rModel.State = false;
            rModel.Msg = "";

            DataSet ds = SQLHelper.QueryDataSet(CommandType.StoredProcedure, "app_wms_stock_sel", new SqlParameter("store_id", store_id));
            rModel.State = true;
            rModel.RData.Add("bothLib", JSONHelper.DataTableToList(ds.Tables[0]));
            rModel.RData.Add("midLib", JSONHelper.DataTableToList(ds.Tables[1]));
            rModel.RData.Add("store_type", store_id == 1 ? "机加工库" : "环线库 ");
            Response.Write(JSONHelper.ObjectToJSON(rModel));
            Response.End();
        }

        //转为原料G、原料R、空托盘、无托盘、产品出库
        private void ChangeState()
        {
            paramUpdate param = new paramUpdate();
            API_ReturnStruct_MSG rMsg = new API_ReturnStruct_MSG();
            rMsg.State = false;
            rMsg.Msg = "";
            try
            {
                if (!string.IsNullOrEmpty(Request.Params["store_id"]))
                    param.store_id = Convert.ToInt64(Request.Params["store_id"].ToString());
                else
                    param.store_id = -1;
                if (!string.IsNullOrEmpty(Request.Params["stocknums"]))
                    param.stocknums = Request.Params["stocknums"].ToString();
                else
                    param.stocknums = "-1";
                if (!string.IsNullOrEmpty(Request.Params["changeName"]))
                    param.changeName = Request.Params["changeName"].ToString();
                else
                    param.changeName = "";
                if (!string.IsNullOrEmpty(Request.Params["actionName"]))
                    param.actionName = Request.Params["actionName"].ToString();
                else
                    param.actionName = "";

                DataSet ds = SQLHelper.QueryDataSet(CommandType.StoredProcedure, "app_wms_stock_statechange", SQLHelper.ModelToParameterList(param).ToArray());

                rMsg.State = BitToBool(ds.Tables[0].Rows[0]["State"]);
                rMsg.Msg = ds.Tables[0].Rows[0]["Msg"].ToString();
            }
            catch (Exception ex)
            {
                rMsg.Msg = ex.Message;
            }
            Response.Write(JSONHelper.ObjectToJSON(rMsg));
            Response.End();
        }
        // changeState工具函数
        private bool BitToBool(object BitValue)
        {
            bool bit = false;
            if (BitValue != null)
            {
                string strValue = BitValue.ToString().ToLower();
                if (strValue == "1" || strValue == "true")
                {
                    bit = true;
                }
            }
            return bit;
        }

        //开启Websocket
        private void OpenWebSocket(HttpContext httpContext)
        {
            if (httpContext.IsWebSocketRequest)
                httpContext.AcceptWebSocketRequest(ProcessChatAsync);
        }
        private async Task ProcessChatAsync(AspNetWebSocketContext socketContext)
        {
            WebSocket socket = null;

            DataSet ds;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);

            Int64 store_id = 0;
            if (!string.IsNullOrEmpty(Request.Params["store_id"]))
                store_id = Convert.ToInt64(Request.Params["store_id"].ToString());
            else
                store_id = 0;

            try
            {
                using (socket = socketContext.WebSocket)
                {
                    int i = 0;
                    while (true)
                    {
                        if (socket.State == WebSocketState.Open)
                        {
                            ds = SQLHelper.QueryDataSet(CommandType.StoredProcedure, "app_wms_stock_check", new SqlParameter("store_id", store_id));      //查询最近的更新时间
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                List<Dictionary<string, object>> tableList = JSONHelper.DataTableToList(ds.Tables[0]);
                                Dictionary<string, object> dict = isStockChange(tableList, null);

                                bool flag = (bool)dict["flag"];
                                if (flag)
                                {
                                    buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("updated"));
                                    await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                    //保留本次更新时间
                                    lasttime = (DateTime)dict["value"];
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                        Thread.Sleep(1000);
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
        private Dictionary<string, object> isStockChange(List<Dictionary<string, object>> tableList, object value)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            bool flag = false;
            object maxValue = lasttime;
            foreach (var row in tableList)
            {
                foreach (var column in row)
                {
                    value = column.Value;
                    //判断数据有更新
                    if (!string.IsNullOrEmpty(value.ToString()) && (DateTime)value > lasttime)
                    {
                        flag = true;
                        maxValue = (DateTime)value > (DateTime)maxValue ? value : maxValue;
                    }
                }
            }

            dict.Add("flag", flag);
            dict.Add("value", maxValue);
            return dict;
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
