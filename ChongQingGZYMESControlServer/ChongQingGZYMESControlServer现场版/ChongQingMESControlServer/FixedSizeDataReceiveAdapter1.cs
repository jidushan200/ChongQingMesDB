using HPSocket;
using HPSocket.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChongQingControlServer
{
    //
    // 摘要:
    //     定长数据接收适配器基类
    //
    // 类型参数:
    //   TRequestBodyType:
    //     包体解析对象类型
    public abstract class FixedSizeDataReceiveAdapter1<TRequestBodyType> : HPSocket.Adapter.DataReceiveAdapter<TRequestBodyType>
    {
        //
        // 摘要:
        //     封包长度
        private readonly int _packetSize;

        //
        // 摘要:
        //     定长数据接收适配器基类构造函数
        //
        // 参数:
        //   packetSize:
        //     包长
        protected FixedSizeDataReceiveAdapter1(int packetSize)
        {
            _packetSize = packetSize;
        }

        public override HPSocket.HandleResult OnReceive<TSender>(TSender sender, IntPtr connId, byte[] data, HPSocket.Adapter.ParseRequestBody<TSender, TRequestBodyType> parseRequestBody)
        {
            try
            {
                DataReceiveAdapterInfo dataReceiveAdapterInfo = DataReceiveAdapterCache.Get(connId);
                if (dataReceiveAdapterInfo == null)
                {
                    return HandleResult.Error;
                }

                dataReceiveAdapterInfo.Data.AddRange(data);
                HandleResult handleResult;
                while (true)
                {
                    if (dataReceiveAdapterInfo.Data.Count < _packetSize)
                    {
                        byte[] data2 = dataReceiveAdapterInfo.Data.GetRange(0, data.Length ).ToArray();
                        dataReceiveAdapterInfo.Data.RemoveRange(0, data.Length);
                        TRequestBodyType obj = ParseRequestBody(data2);
                        handleResult = parseRequestBody(sender, connId, obj);
                        if (handleResult == HandleResult.Error)
                        {
                            break;
                        }

                        //dataReceiveAdapterInfo.Data.RemoveRange(0, data.Length);
                        if (dataReceiveAdapterInfo.Data.Count == 0)
                        {
                            break;
                        }
                    }

                    if (dataReceiveAdapterInfo.Data.Count >= _packetSize)
                    {
                        byte[] data2 = dataReceiveAdapterInfo.Data.GetRange(0, data.Length ).ToArray();
                        TRequestBodyType obj = ParseRequestBody(data2);
                        handleResult = parseRequestBody(sender, connId, obj);
                        if (handleResult == HandleResult.Error)
                        {
                            break;
                        }

                        dataReceiveAdapterInfo.Data.RemoveRange(0, data.Length);
                        if (dataReceiveAdapterInfo.Data.Count == 0)
                        {
                            break;
                        }
                    }


                }

                return handleResult;
            }
            catch (Exception)
            {
                return HandleResult.Error;
            }
        }

        //
        // 摘要:
        //     解析请求包体到对象
        //
        // 参数:
        //   data:
        //     包体
        //
        // 返回结果:
        //     需子类根据包体data自己解析对象并返回
        public virtual TRequestBodyType ParseRequestBody(byte[] data)
        {
            return default(TRequestBodyType);
        }
    }
}
