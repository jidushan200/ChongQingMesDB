using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppAPI.CommonClass
{
    public class BootStrapTable_ReturnStruct
    {
        ///// <summary>
        // 返回状态：true 正常，false：异常
        /// </summary>
        public bool State;
        /// <summary>
        /// 返回总条数
        /// </summary>
        public string total;
        /// <summary>
        /// 返回数据（Hash）
        /// </summary>
        public List<Dictionary<string, object>> rows;
    }
}