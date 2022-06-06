using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppAPI.CommonClass
{
    public class API_ReturnStruct
    {
        /// 返回状态：true 正常，false：异常
        public bool State;
        /// 返回消息正文
        public string Msg;
        /// 返回数据（Hash）
        public Dictionary<string, object> RData;

        /// 构造函数 默认State=false,Msg="is a empty info", RData=new Dictionary ＜string, object＞ ();
        public API_ReturnStruct()
        {
            State = false;
            Msg = "is a empty info";
            RData = new Dictionary<string, object>();
        }
    }

    public class API_ReturnStruct_MSG
    {
        /// 返回状态：true 正常，false：异常
        public bool State;

        /// 返回消息正文
        public string Msg;

        /// 构造函数 默认State=false,Msg="is a empty info", RData=new Dictionary ＜string, object＞ ();
        public API_ReturnStruct_MSG()
        {
            State = false;
            Msg = "is a empty info";
        }
    }
}