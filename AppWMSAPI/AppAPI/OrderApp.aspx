<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="OrderApp.aspx.cs" Inherits="AppAPI.OrderApp" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport"
        content="width=device-width,initial-scale=1,minimum-scale=1,maximum-scale=1,user-scalable=no" />
    <title>订单屏</title>

    <link rel="stylesheet" href="css/bootstrap.min.css" />
    <!-- <link rel="stylesheet" href="css/bootstrap-table.css"/> -->
    <link rel="stylesheet" href="css/bootstrap-table.min.css" />
    <style>
        * {
            margin: 0;
            padding: 0;
            touch-action: pan-y;
        }

        html,
        body,
        .main {
            width: 100%;
            height: 100%;
            background: #EEEEEE;
            display: flex;
        }

            .main .main_part,
            .main .top_part {
                height: 90%;
                margin-top: 3%;
            }

            /* 左边订单列表占比 */
            .main .main_part {
                flex: 3;
                /* background-color: #212529; */
            }

            /* 右边状态信息占比 */
            .main .top_part {
                flex: 1;
                /* background-color: #007AFF; */
            }

            /* 左边:订单表格详细样式 */
            .main .main_part .part_order {
                width: 96%;
                height: 100%;
                margin: 0 auto;
                margin-right: 5px;
                background: #FFFFFF;
                border-radius: 3px;
            }

                .main .main_part .part_order table {
                    width: 98%;
                    margin: 0 auto;
                    height: 100%;
                    font-size: 15px;
                    /* background:#1B6D85; */
                }

        .table {
            table-layout: fixed;
        }

        /* 右边状态显示详细样式 */
        .main .top_part .part_operation {
            width: 90%;
            height: 100%;
            margin: 0 auto;
            /* background: #FFFFFF; */
        }

            .main .top_part .part_operation table {
                width: 100%;
                height: 100%;
                font-size: 18px;
                /* border: 1px solid #101010; */
            }

                .main .top_part .part_operation table tr {
                    vertical-align: top;
                }

                    .main .top_part .part_operation table tr:first-child td {
                        padding-top: 3px;
                    }

                    .main .top_part .part_operation table tr:last-child {
                        height: 12%;
                    }

                    .main .top_part .part_operation table tr td {
                        width: 50%;
                        /* border: 1px solid #101010; */
                    }

                        .main .top_part .part_operation table tr td button {
                            width: 85%;
                            height: 100%;
                        }

        /* "停止"按钮样式 */
        .btn-danger.active,
        .btn-danger.active:focus,
        .btn-danger.active:hover {
            background-color: #d9534f;
            border-color: #d43f3a;
            box-shadow: none;
            outline: none;
        }

        /* "运行"按钮样式 */
        .btn-success.active,
        .btn-success.active:focus,
        .btn-success.active:hover {
            background-color: #4cae4c;
            border-color: #449d44;
            box-shadow: none;
            outline: none;
        }
    </style>
</head>
<body>
    <div class="main">
        <!-- 左边:订单 -->
        <div class="main_part">
            <div class="part_order">
                <!--这里放置真实显示的DOM内容-->
                <table id="table_main" data-toggle="table" data-row-style="rowStyle"
							data-header-style="headerStyle" data-classes="table-borderless">
                    <thead>
                        <tr>
                            <th data-width="12.5" data-width-unit="%" data-align="center" data-field="ordernumber">订单编号</th>
                            <th data-width="12.5" data-width-unit="%" data-field="name" data-align="center">产品名称
                            </th>
                            <th data-width="12.5" data-width-unit="%" data-field="productImg"
                                data-formatter="cellImg" data-align="center">产品展示</th>
                            <th data-width="12.5" data-width-unit="%" data-field="quantity" data-align="center">订单数量
                            </th>
                            <th data-width="12.5" data-width-unit="%" data-field="onlinecnt" data-align="center">上线数量
                            </th>
                            <th data-width="12.5" data-width-unit="%" data-field="finishedcnt" data-align="center">完成数量</th>
                            <th data-width="12.5" data-width-unit="%" data-field="orderUp" data-formatter="cellUP"
                                data-align="center">上移</th>
                            <th data-width="12.5" data-width-unit="%" data-field="orderDown"
                                data-formatter="cellDown" data-align="center">下移</th>
                        </tr>
                    </thead>
                </table>
            </div>
        </div>
        <!-- 右边:状态显示 -->
        <div class="top_part">
            <div class="part_operation">
                <table>
                    <tr>
                        <td>订单编号:</td>
                        <td><span id="ordernumber">FR09</span></td>
                    </tr>
                    <tr>
                        <td>产品名称:</td>
                        <td><span id="productname">产品W</span></td>
                    </tr>
                    <tr>
                        <td>订单数量:</td>
                        <td><span id="quantity">50</span></td>
                    </tr>
                    <tr>
                        <td>上线数量:</td>
                        <td><span id="onlinecnt">20</span></td>
                    </tr>
                    <tr>
                        <td>完成数量:</td>
                        <td><span id="finishedcnt">30</span></td>
                    </tr>
                    <tr>
                        <td>
                            <button type="button" class="btn btn-danger active" id="btnStop">停止</button></td>
                        <td>
                            <button type="button" class="btn btn-success active" id="btnRun">运行</button></td>
                    </tr>
                </table>
            </div>
        </div>
    </div>

    <script src="js/jquery.min.js"></script>
    <script src="js/bootstrap.min.js"></script>
    <script src="js/bootstrap-table.js"></script>
    <%--<script src="js/bootstrap-table.min.js"></script>--%>
    <!-- <script src="js/bootstrap-table-zh-CN.js"></script> -->
    <script src="js/bootstrap-table-zh-CN.min.js"></script>
    <script src="js/layer.mobile-v2.0/layer.js"></script>
    <%--<script src="js/jqueryAjax.js"></script>--%>
    <script type="text/javascript" charset="utf-8">
        var socket = null;
        //if (window.plus) {
        //    plusReady();
        //} else {
        //    document.addEventListener('plusready', plusReady, false);
        //}

        //function plusReady() {
        //    // 横屏显示
        //    plus.screen.lockOrientation("landscape");
        //    //初始化信息显示
        //    setInfo();
        //}
        //setWebSocket()

        //设置表格高度
        var tableHeight = document.body.clientHeight * 0.88;
        $('#table_main').bootstrapTable({
				height: tableHeight,
				url:'http://192.168.60.185:8022/AppHandlers/AppOrderHandler.ashx?method=Query',
				// 列点击事件
				onClickCell:function(field, value, row, element){
					if (field == 'orderUp') {
						var data = {
							method: 'MoveUp',
							num: row.num,
							id: row.id
						};
						jqueryAJAX(data, function(dataStr) {
							setTimeout(function() {
								$('#table_main').bootstrapTable('refresh', {
									silent: true
								});
								setInfo();
								// layer.open({
								// 	content: dataStr.Msg,
								// 	skin: 'msg',
								// 	time: 1
								// });
							}, 700);
						});
					}
					if (field == 'orderDown') {
						var data = {
							method: 'MoveDown',
							num: row.num,
							id: row.id
						};     
						jqueryAJAX(data, function(dataStr) {
							setTimeout(function() {
								$('#table_main').bootstrapTable('refresh', {
									silent: true
								});
								setInfo();
								// layer.open({
								// 	content: dataStr.Msg,
								// 	skin: 'msg',
								// 	time: 1
								// });
							}, 700);
						});
					}
				}
			});

        //定时器: 确保websocket处于连接状态
        setInterval(function () {
            if (socket) {
                if (socket.readyState == 0 || socket.readyState == 3)
                    setWebSocket();
            } else
                setWebSocket();
        }, 6000);

        // 禁止 按钮事件绑定
        $('#btnStop').click(function () {
            var data = {
                method: 'RunState',
                runstate: 0
            };
            jqueryAJAX(data, function (dataStr) {
                //layer.open({
                //    content: dataStr.Msg,
                //    skin: 'msg',
                //    time: 1
                //});
            });
            //$(this).attr('disabled', 'disabled');
            //$('#btnRun').removeAttr('disabled');
        });

        // 运行 按钮事件绑定
        $('#btnRun').click(function () {
            var data = {
                method: 'RunState',
                runstate: 1
            };
            jqueryAJAX(data, function (dataStr) {
                //layer.open({
                //    content: dataStr.Msg,
                //    skin: 'msg',
                //    time: 1
                //});
            });
            //$(this).attr('disabled', 'disabled');
            //$('#btnStop').removeAttr('disabled');
        });

        //获取信息显示部分数据函数
        function setInfo() {
            var data = {
                method: 'QueryInfo'
            }
            jqueryAJAX(data, function (dataStr) {
                var dataList = dataStr.RData['Data0'][0];
                document.getElementById('ordernumber').innerHTML = dataList.ordernumber;
                document.getElementById('productname').innerHTML = dataList.name;
                document.getElementById('quantity').innerHTML = dataList.quantity;
                document.getElementById('onlinecnt').innerHTML = dataList.onlinecnt;
                document.getElementById('finishedcnt').innerHTML = dataList.finishedcnt;

                //var dataState = dataStr.RData["Data1"][0].state;
                //if (dataState == '1') {
                //    //$('#btnStop').attr('disabled', 'disabled');
                //    //$('#btnRun').removeAttr('disabled');
                //} else {
                //    //$('#btnRun').attr('disabled', 'disabled');
                //    //$('#btnStop').removeAttr('disabled');
                //}
            });
        }

        //启动WebSocket
        function setWebSocket() {
            if (!this.WebSocket) {
                this.WebSocket = this.MozWebSocket;
            }
            if (this.WebSocket) {
                socket = new WebSocket("ws://localhost:58836/AppHandlers/AppOrderHandler.ashx?method=OpenWebSocket");
                socket.onmessage = function (e) {
                    console.log("服务器向客户端传输数据" + e.data);
                    var data = JSON.parse(e.data);
                    
                    if (data.updatedStr == 'updated') {
                        setInfo();
                        $('#table_main').bootstrapTable('refresh', {
                            silent: true
                        });
                    }
                        
                    if (data.EnStart == '0' && data.EnStop == '1') {
                        $('#btnRun').attr("disabled", true);
                        $('#btnStop').attr("disabled", false);
                    }
                    else if (data.EnStart == '1' && data.EnStop == '0') {
                        $('#btnRun').attr("disabled", false);
                        $('#btnStop').attr("disabled", true);
                    }
                    else if (data.EnStart == '0' && data.EnStop == '0') {
							$('#btnRun').attr("disabled", true);
							$('#btnStop').attr("disabled", true);
						}
                    //else {
                    //    $('#btnRun').attr("disabled",false);
                    //   $('#btnStop').attr("disabled",false);
                    //}
                    
                };
                socket.onopen = function (event) {
                    console.log("连接开启");
                };
                socket.onclose = function (event) {
                    console.log("连接被关闭");
                };
            } else {
                alert("你的系统不支持 WebSocket！");
            }
        }

        //行样式 
        function rowStyle(row, index) {
            return {
                css: {
                    'height': document.body.clientHeight * 0.125 + 'px',
                    'padding-top': '0',
                    'padding-bottom': '0',
                    // 'background':'red'
                }
            }
        }

        //列头样式
        function headerStyle(column) {
            return {
                css: {
                    'color': '#666',
                    'padding-top': '12px',
                    'padding-bottom': '10px',
                    'font-size': '16px'
                }
            }
        }

        // '产品展示'列图片样式
        function cellImg(value, row, index, field) {
            var imgUrl = 'RF00';
            imgUrl = row.productcode;
            // return '<img src = "img/' + imgUrl + '.jpg"  style="width:auto;height:55px"/>';
            // return '<div   style="width:100%;height:100%;background:green"></div>';
            return '<div style="width:100%;height:100%;"><img src = "img/' + imgUrl +
                '.jpg"  style="width:100%;height:100%"/></div>';
        }

        //'上移'列样式
        function cellUP(value, row, index, field) {
            return '<a href="#" style="width:98%" role="button" class="btn btn-primary"><span style="color:#FFF" class="glyphicon glyphicon-arrow-up"></span></a>';
        }

        //'下移'列样式
        function cellDown(value, row, index, field) {
            return '<a href="#" style="width:98%" class="btn btn-primary"><span style="color:#FFF" class="glyphicon glyphicon-arrow-down"></span></a>';
        }

        function jqueryAJAX(data, callBackFunction) {
            $.ajax({
                type: "POST",
                async: false,
                url: "<%=ResolveUrl("~/AppHandlers/AppOrderHandler.ashx") %>",
                data: data,
                success: function (data) {
                    var dataStr = JSON.parse(data);
                    if (dataStr.State) {
                        callBackFunction(dataStr);
                    } else
                        layer.open({
                            style: 'width:30%;',
                            content: dataStr.Msg,
                            btn: '确定'
                        });
                },
                error: function () {
                    closeMask();
                    err_alert('网络调用失败!');
                }
            });
        }
    </script>
</body>
</html>
