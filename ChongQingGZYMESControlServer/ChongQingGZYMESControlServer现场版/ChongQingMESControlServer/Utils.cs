using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChongQingControlServer
{
    public class Utils
    {
        // byte数组转16进制字符串
        // bSpace：是否用空格分隔，缺省无分隔
        public static string ToHexStrFromBytes(byte[] byteDatas, bool bSpace = false)
        {
            StringBuilder strbuilder = new StringBuilder();
            for (int i = 0; i < byteDatas.Length; i++)
            {
                strbuilder.Append(string.Format("{0:X2}", byteDatas[i]));
                if (bSpace)
                    strbuilder.Append(" ");
                if (i > 0 && (i + 1) % 32 == 0)
                    strbuilder.Append("\n");
            }
            return strbuilder.ToString().Trim();
        }
    }
}
