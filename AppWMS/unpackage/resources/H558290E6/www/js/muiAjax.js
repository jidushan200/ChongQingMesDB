function muiAJAX(url, data, callBackFunction) {
    mui.ajax(url, {
        data: data,
        type: 'post', //HTTP请求类型
        timeout: 8000, //超时时间设置为8秒；
        traditional: true,
        success: function (data, textStatus, xhr) {
            var dataStr = JSON.parse(data);
            if (dataStr.State) {
                callBackFunction(dataStr);
            } else
                mui.alert(dataStr.Msg || "接收消息失败", '提示信息', '确定', function (e) {
                    e.index
                }, 'div')
        },
        error: function (xhr, type, errorThrown) {
            //异常处理；
            //console.log('请求失败:\n xhr实例对象:' + xhr + '\n错误描述:' + type + '\n可捕获的异常对象' + errorThrown);
            mui.alert('网络调用失败!', '错误信息', '确定', function (e) {
                e.index
            }, 'div');
        }
    });
}
