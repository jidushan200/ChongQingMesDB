<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ScreenApp.aspx.cs" Inherits="AppAPI.screen" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>工位屏</title>
    <link href="css/mui.min.css" rel="stylesheet" />
    <link rel="stylesheet" type="text/css" href="css/mui.picker.min.css" />
    <style type="text/css">
        * {
            padding: 0;
            margin: 0;
            touch-action: pan-y;
        }

        html,
        body {
            width: 100%;
            height: 100%;
            /* background-color: #EEEEEE; */
        }

            body::after {
                content: none;
            }

        .mui-content {
            width: 100%;
            height: 100%;
            background-color: #EEEEEE;
        }

            /* 下部分:图片和按钮区 */
            .mui-content .con_main {
                width: 100%;
                height: 100%;
                /* border-top: 1px solid #555555; */
                display: flex;
            }

                /* 左边: 图片占比 */
                .mui-content .con_main .left_con {
                    flex: 3;
                    height: 100%;
                    /* background-color: #2AC845; */
                }

                /* 右边: 信息占比 */
                .mui-content .con_main .right_con {
                    flex: 2;
                    height: 100%;
                    /* background-color: #333333; */
                }

                /* 左边: 图片----详细样式 */
                .mui-content .con_main .left_con .div_img {
                    width: 93%;
                    height: 90%;
                    margin: auto;
                    margin-right: 0;
                    /* background-color: #CF2D28; */
                }

                    .mui-content .con_main .left_con .div_img img {
                        width: 100%;
                        height: 100%;
                        border-radius: 5px;
                    }

                /* 右边: 信息----详细样式 */
                .mui-content .con_main .right_con .div_table {
                    width: 92%;
                    height: 90%;
                    margin: auto;
                    /* background: #8A6DE9; */
                }

                    .mui-content .con_main .right_con .div_table table {
                        width: 100%;
                        height: 100%;
                        font-size: 0.24rem;
                        color: #777;
                        /* border: 1px solid #CCCCCC; */
                    }

                        .mui-content .con_main .right_con .div_table table tr {
                            width: 100%;
                            vertical-align: top;
                        }

                            .mui-content .con_main .right_con .div_table table tr:first-child,
                            .mui-content .con_main .right_con .div_table table tr:last-child {
                                height: 0;
                            }

                            .mui-content .con_main .right_con .div_table table tr td {
                                /* border: 1px solid #DD524D; */
                            }

                                .mui-content .con_main .right_con .div_table table tr td .div_td {
                                    width: 100%;
                                }

                                    .mui-content .con_main .right_con .div_table table tr td .div_td svg {
                                        float: left;
                                        /* padding-top: 0.02rem;
				padding-right: 0.02rem; */
                                        padding: 0.00rem 0.02rem;
                                        width: 10%;
                                    }

                                    .mui-content .con_main .right_con .div_table table tr td .div_td .row_style {
                                        padding: 0.03rem 0 0.08rem 0;
                                        margin-left: 0.05rem;
                                        float: left;
                                        display: inline-block;
                                        width: 88%;
                                        border-bottom: 1px dotted #8F8F94;
                                    }

                                    .mui-content .con_main .right_con .div_table table tr td .div_td .btn_middle {
                                        width: 25%;
                                        display: inline-block;
                                    }

                                    .mui-content .con_main .right_con .div_table table tr td .div_td.div_btn button {
                                        width: 33%;
                                        height: 0.5rem;
                                        color: #FFFFFF;
                                        font-size: 0.22rem;
                                    }

                .mui-content .con_main .right_con button#btnCall {
                    background-color: #FFAD5C;
                    border-color: #FFAD5C;
                }

                .mui-content .con_main .right_con button#btnWarn {
                    background-color: #FF5C5C;
                    border-color: #FF5C5C;
                }

        /* 选择工位弹窗样式 */
        .mui-popover {
            position: relative;
        }

            .mui-popover .mui-table-view {
                max-height: none;
                /* background-color: #FFFFFF; */
                width: 45%;
                position: absolute;
                left: 50%;
                bottom: 50%;
                transform: translate(-50%, -25%);
            }

                .mui-popover .mui-table-view .mui-table-view-cell table {
                    width: 100%;
                }

                    .mui-popover .mui-table-view .mui-table-view-cell table tr td {
                        width: 50%;
                        /* text-align: left; */
                    }

                        .mui-popover .mui-table-view .mui-table-view-cell table tr td button {
                            margin: 0.05rem;
                            width: 80%;
                            /* background-color: #2AC845; */
                        }

        .mui-table-view-cell > .mui-btn {
            position: unset;
            /* margin: 0.1rem; */
        }

        /* 点击li时背景不变色 */
        .mui-table-view-cell.mui-active {
            background-color: #f7f7f7;
        }
    </style>
</head>
<body>
    <div class="mui-content">
        <div class="con_main">
            <div class="left_con">
                <div class="div_img div_height" id="">
                    <img id="station_img" src="img/000.jpg">
                </div>
            </div>
            <div class="right_con">
                <div class="div_table div_height">
                    <table>
                        <tr>
                            <td>
                                <a href="#" id="btn_station" class="mui-btn mui-btn-block mui-btn-primary "
                                    style="padding: 0.05rem 0.2rem;">选择工位
                                </a>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div class="div_td">
                                    <svg t="1618902122730" class="icon" viewBox="0 0 1024 1024" version="1.1"
                                        xmlns="http://www.w3.org/2000/svg" p-id="13525" width="36" height="36">
                                        <path
                                            d="M966.4 454.4h-44.8V236.8c0-12.8-6.4-19.2-19.2-19.2-12.8 0-19.2 6.4-19.2 19.2V448H768V320c0-12.8-6.4-19.2-19.2-19.2-12.8 0-19.2 6.4-19.2 19.2v128H384v-38.4h192c19.2 0 32-12.8 32-32V102.4c0-19.2-12.8-32-32-32H147.2c-19.2-6.4-38.4 12.8-38.4 32v275.2c0 19.2 12.8 32 32 32h198.4V448H70.4c-19.2 0-32 12.8-32 32v428.8c0 19.2 12.8 38.4 32 38.4h64c12.8 0 19.2-6.4 19.2-19.2V569.6h499.2v364.8c0 12.8 6.4 19.2 19.2 19.2H960c19.2 0 32-12.8 32-32V486.4c6.4-19.2-12.8-32-25.6-32z"
                                            p-id="13526" fill="#8a8a8a">
                                        </path>
                                    </svg>
                                    <div class="row_style">
                                        工位描述:&nbsp;
											<span id="description">--</span>
                                    </div>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div class="div_td">
                                    <svg t="1618899560048" class="icon" viewBox="0 0 1024 1024" version="1.1"
                                        xmlns="http://www.w3.org/2000/svg" p-id="11233" width="36" height="36">
                                        <path
                                            d="M896 910.222222a42.666667 42.666667 0 1 1 0 85.333334H128a42.666667 42.666667 0 1 1 0-85.333334h768zM881.777778 28.444444a113.777778 113.777778 0 0 1 113.777778 113.777778v568.888889a113.777778 113.777778 0 0 1-113.777778 113.777778h-739.555556a113.777778 113.777778 0 0 1-113.777778-113.777778v-568.888889a113.777778 113.777778 0 0 1 113.777778-113.777778h739.555556z m0 85.333334h-739.555556a28.444444 28.444444 0 0 0-27.989333 23.324444L113.777778 142.222222v568.888889a28.444444 28.444444 0 0 0 23.324444 27.989333L142.222222 739.555556h739.555556a28.444444 28.444444 0 0 0 27.989333-23.324445L910.222222 711.111111v-568.888889a28.444444 28.444444 0 0 0-23.324444-27.989333L881.777778 113.777778zM563.086222 233.187556l3.413334 3.811555 161.336888 211.456 78.222223-58.88a31.288889 31.288889 0 0 1 40.049777 1.991111l3.754667 4.209778a31.288889 31.288889 0 0 1-1.991111 39.992889l-4.152889 3.811555L740.693333 517.12a31.288889 31.288889 0 0 1-39.936-1.820444l-3.754666-4.152889-139.377778-182.670223-61.098667 275.626667a31.288889 31.288889 0 0 1-53.873778 14.165333l-3.185777-4.380444L316.529778 416.995556 221.468444 514.048a31.288889 31.288889 0 0 1-39.879111 3.982222l-4.380444-3.527111a31.288889 31.288889 0 0 1-3.982222-39.822222l3.527111-4.437333 122.88-125.496889a31.288889 31.288889 0 0 1 45.681778 1.024l3.242666 4.323555 103.424 165.717334 59.164445-266.581334a31.288889 31.288889 0 0 1 51.939555-16.042666z"
                                            p-id="11234" fill="#8a8a8a">
                                        </path>
                                    </svg>
                                    <div class="row_style">
                                        生产计数:&nbsp;
											<span id="onlinecnt">--</span>
                                    </div>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div class="div_td">
                                    <svg t="1618902193813" class="icon" viewBox="0 0 1024 1024" version="1.1"
                                        xmlns="http://www.w3.org/2000/svg" p-id="14436" width="40" height="40">
                                        <path
                                            d="M398.897 710.736h-65.47l-215.197-332.878c-6.078-9.392-10.96-18.69-14.642-27.901h-1.658c1.472 9.579 2.21 29.558 2.21 59.946v300.832h-54.697v-427.906h69.337l209.395 327.076c9.945 15.469 15.837 25.324 17.679 29.558h1.105c-1.842-12.157-2.763-32.964-2.763-62.432v-294.203h54.697v427.906z"
                                            p-id="14437" fill="#8a8a8a">
                                        </path>
                                        <path
                                            d="M481.771 501.894c0-69.062 18.69-124.034 56.077-164.918 37.385-40.884 88.028-61.327 151.936-61.327 59.483 0 107.364 19.985 143.648 59.946 36.279 39.965 54.42 91.99 54.42 156.080 0 69.434-18.604 124.497-55.802 165.195-37.202 40.704-86.927 61.052-149.172 61.052-60.775 0-109.489-19.981-146.134-59.946-36.651-39.962-54.973-91.99-54.973-156.080zM539.782 497.474c0 51.753 13.212 93.233 39.641 124.449 26.425 31.217 60.913 46.823 103.454 46.823 45.671 0 81.535-14.96 107.597-44.89 26.058-29.924 39.090-71.777 39.090-125.554 0-55.25-12.755-97.882-38.26-127.902-25.51-30.016-60.455-45.027-104.835-45.027-43.647 0-79.007 15.841-106.079 47.515-27.071 31.678-40.607 73.205-40.607 124.587z"
                                            p-id="14438" fill="#8a8a8a">
                                        </path>
                                        <path
                                            d="M920.726 682.284c0-9.945 3.406-18.322 10.22-25.138 6.811-6.811 15.284-10.221 25.415-10.221 10.311 0 18.876 3.453 25.691 10.358 6.811 6.906 10.221 15.242 10.221 25.001 0 9.577-3.41 17.817-10.221 24.723-6.815 6.906-15.469 10.359-25.966 10.359-10.13 0-18.556-3.454-25.277-10.359-6.724-6.906-10.082-15.146-10.082-24.723z"
                                            p-id="14439" fill="#8a8a8a">
                                        </path>
                                    </svg>
                                    <div class="row_style">
                                        订单编号:&nbsp;
											<span id="ordernumber">--</span>
                                    </div>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div class="div_td">
                                    <svg t="1618902301041" class="icon" viewBox="0 0 1024 1024" version="1.1"
                                        xmlns="http://www.w3.org/2000/svg" p-id="2024" width="36" height="36">
                                        <path
                                            d="M378.1 140h130.5v798.3H378.1zM580.8 140h87v798.3h-87zM953.7 140h65.2v798.3h-65.2zM229.6 140h65.2v798.3h-65.2zM729.2 140h152.2v798.3H729.2zM5.1 140h152.2v798.3H5.1z"
                                            p-id="2025" fill="#8a8a8a">
                                        </path>
                                    </svg>
                                    <div class="row_style">
                                        托盘编码:&nbsp;
											<span id="palletnumber">--</span>
                                    </div>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div class="div_td">
                                    <svg t="1618902357802" class="icon" viewBox="0 0 1024 1024" version="1.1"
                                        xmlns="http://www.w3.org/2000/svg" p-id="2959" width="36" height="36">
                                        <path
                                            d="M897.661586 564.476394L186.49835 1012.538912c-38.591959 24.575974-90.495903 11.839987-90.495903-59.839935V70.399924c0-72.126923 48.319948-82.109912 90.495903-59.839935l711.163236 448.062518c39.807957 25.791972 41.023956 77.759916 0 105.854887zM170.946366 923.322008L845.757641 511.54945 170.946366 99.775893v823.546115z"
                                            p-id="2960" fill="#8a8a8a">
                                        </path>
                                    </svg>
                                    <div class="row_style">
                                        产线动作:&nbsp;
											<span id="action">--</span>
                                    </div>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div class="div_td div_btn">
                                    <button id="btnCall">呼叫</button>
                                    <div class="btn_middle"></div>
                                    <button id="btnWarn">告警</button>
                                </div>
                            </td>
                        </tr>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <div id="stationPopover" class="mui-popover mui-popover-action">
        <ul class="mui-table-view">
            <li class="mui-table-view-cell">
                <h5><b>选择工位</b></h5>
            </li>
            <li class="mui-table-view-cell" style="padding: 0.2rem 0.2rem">
                <table id="stationTable" border="0" cellspacing="0" cellpadding="0">
                    <!-- <tr>
							<td><button class="mui-btn mui-btn-primary mui-btn-outlined">上下料(OP01)</button></td>
							<td><button  class="mui-btn mui-btn-primary mui-btn-outlined">打螺丝(OP02)</button></td>
						</tr>
						<tr>
							<td><button class="mui-btn mui-btn-primary mui-btn-outlined">自动焊锡(OP03)</button></td>
							<td><button class="mui-btn mui-btn-primary mui-btn-outlined">电性能检测(OP04)</button></td>
						</tr>
						<tr>
							<td><button class="mui-btn mui-btn-primary mui-btn-outlined">视觉检测(OP05)</button></td>
							<td><button type="button" class="mui-btn mui-btn-primary mui-btn-outlined">翻转(OP06)</button></td>
						</tr>
						<tr>
							<td><button class="mui-btn mui-btn-primary mui-btn-outlined">外壳螺钉(OP07)</button></td>
							<td><button type="button" class="mui-btn mui-btn-primary mui-btn-outlined">称重(OP08)</button></td>
						</tr>
						<tr>
							<td><button class="mui-btn mui-btn-primary mui-btn-outlined">翻转NG(OP09)</button></td>
							<td><button class="mui-btn mui-btn-primary mui-btn-outlined">激光打标(OP10)</button></td>
						</tr> -->
                </table>
            </li>
            <li class="mui-table-view-cell">
                <a href="#stationPopover"><b>取消</b></a>
            </li>
        </ul>
    </div>

    <script src="js/mui.min.js"></script>
    <script src="js/mui.picker.min.js"></script>
    <%--<script src="js/muiAjax.js"></script>--%>
    <script src="js/rem.js"></script>
    <script type="text/javascript" charset="utf-8">
        //mui.init();
        var socket = null;
        var btnStation = '';
        //mui.plusReady(function () {
        // 横屏显示
        //plus.screen.lockOrientation("landscape");

        var stationDesc = localStorage.getItem('stationDesc');
        if (stationDesc != null || stationDesc != '') {
            //mui('#description')[0].value = station;
            document.getElementById('description').innerHTML = stationDesc;
            btnStation = localStorage.getItem('station');
            setData(btnStation);
        }

        //动态设置库位和多选框区域高度
        onresizeHeight();

        //});

        //工位选择按钮
        document.getElementById('btn_station').addEventListener('tap', function () {
            // 删除数据列表中的内容: 
            var dtList = document.getElementById("stationTable");
            //2.获取列表中下的所有子节点
            var trObjs = dtList.childNodes;
            //3.遍历并删除
            for (var i = trObjs.length - 1; i >= 0; i--) { // 一定要倒序
                dtList.removeChild(trObjs[i]);
            }
            data = {
                method: 'GetStation'
            }
            muiAJAX(data, function (dataStr) {
                var dataArray = dataStr.RData.Data;
                for (var i = 0; i < 5; i++) {
                    var btn1 = document.createElement('button');
                    btn1.classList.add('mui-btn', 'mui-btn-primary', 'mui-btn-outlined');
                    btn1.setAttribute('id', dataArray[2 * i].code);
                    btn1.setAttribute('desc', dataArray[2 * i].text);
                    var btn1node = document.createTextNode(dataArray[2 * i].text);
                    btn1.appendChild(btn1node);

                    var td1 = document.createElement('td');
                    td1.appendChild(btn1);

                    var btn2 = document.createElement('button');
                    btn2.classList.add('mui-btn', 'mui-btn-primary', 'mui-btn-outlined');
                    btn2.setAttribute('id', dataArray[2 * i + 1].code);
                    btn2.setAttribute('desc', dataArray[2 * i + 1].text);
                    var btn2node = document.createTextNode(dataArray[2 * i + 1].text);
                    btn2.appendChild(btn2node);

                    var td2 = document.createElement('td');
                    td2.appendChild(btn2);

                    var tr = document.createElement('tr');
                    tr.appendChild(td1);
                    tr.appendChild(td2);

                    document.getElementById('stationTable').appendChild(tr);
                }
            });
            mui('#stationPopover').popover('toggle');
        });

        mui('body').on('tap', '.mui-popover-action li button', function () {
            var a = this,
                parent;
            //根据点击按钮，反推当前是哪个actionsheet
            for (parent = a.parentNode; parent != document.body; parent = parent.parentNode) {
                if (parent.classList.contains('mui-popover-action')) {
                    break;
                }
            }
            //关闭actionsheet
            mui('#' + parent.id).popover('toggle');
            //给工位描述赋值
            document.getElementById('description').innerHTML = a.getAttribute('desc');
            localStorage.setItem('station', a.getAttribute('id'));
            localStorage.setItem('stationDesc', a.getAttribute('desc'));
            btnStation = localStorage.getItem('station');
            setData(btnStation);
            if (socket)
                socket.send(btnStation);
        });

        //报警按钮
        document.getElementById('btnWarn').addEventListener('tap', function (event) {

        })

        //呼叫按钮
        var callState = 0;
        var call = 0;
        document.getElementById('btnCall').addEventListener('tap', function (event) {
            call++;
            //奇数,表示正在呼叫
            if (call % 2 != 0) {
                this.style.backgroundColor = "#FF9429";
                this.style.borderColor = "#FF9429";
                callState = 1;
                //alert('呼叫'+call)
            } else {
                this.style.backgroundColor = "#FFAD5C";
                this.style.borderColor = "#FFAD5C";
                callState = 0;
                //alert('停止'+call)
            }
            if (call >= 2) {
                call = 0;
            }
            alert(callState)
            data = {
                method: 'CallState',
                callState: callState,
                station: mui('#stationVal')[0].value
            }
            muiAJAX(data, function (dataStr) {
                mui.alert('hujiaochenggong', '111', '确定', function (e) {
                    e.index
                }, 'div');
            });
        })

        //动态设置左右两边div 高度
        window.onresize = function () {
            onresizeHeight();
        }

        //定时器要实现的函数内容
        setInterval(function () {
            if (socket) {
                if (socket.readyState == 0 || socket.readyState == 3)
                    setWebSocket();
            } else
                setWebSocket();
        }, 6000);

        function setData(stationStr) {
            data = {
                station: stationStr,
                method: 'SetInfo'
            }
            muiAJAX(data, function (dataStr) {
                document.getElementById('station_img').src = 'img/000.jpg';
                document.getElementById('onlinecnt').innerHTML = '--';
                document.getElementById('ordernumber').innerHTML = '--';
                document.getElementById('palletnumber').innerHTML = '--';
                document.getElementById('action').innerHTML = '--';

                var imgStr = dataStr.RData.ImgUrl;
                document.getElementById('station_img').src = imgStr;

                var dataArray = dataStr.RData.Data[0];
                //生产计数,订单编号,托盘编码,产线动作,图片
                document.getElementById('onlinecnt').innerHTML = dataArray.onlinecnt;
                document.getElementById('ordernumber').innerHTML = dataArray.ordernumber;
                document.getElementById('palletnumber').innerHTML = dataArray.palletnumber;
                document.getElementById('action').innerHTML = dataArray.action;

            })
        }

        //启动WebSocket
        function setWebSocket() {
            if (!this.WebSocket) {
                this.WebSocket = this.MozWebSocket;
            }
            if (this.WebSocket) {
                socket = new WebSocket("ws://localhost:58836/AppHandlers/AppScreenHandler.ashx?method=OpenWebSocket");
                socket.onmessage = function (e) {
                    console.log("服务器向客户端传输数据" + e.data);
                    // var imgUrl = e.data;
                    // document.getElementById('station_img').src = imgUrl;
                    if (e.data == 'updated') {
                        setData(btnStation)
                    }
                };
                socket.onopen = function (event) {
                    console.log("连接开启");
                    socket.send(btnStation);
                };
                socket.onclose = function (event) {
                    console.log("连接被关闭");
                };
            } else {
                alert("你的系统不支持 WebSocket！");
            };
        }

        //动态设置左右两边div 高度
        function onresizeHeight() {
            divHeight = (document.body.clientHeight * 0.05).toFixed(2);
            var queryList = document.querySelectorAll('.div_height');
            for (var i = 0; i < queryList.length; i++)
                queryList[i].style.marginTop = divHeight + 'px';
        }

        function muiAJAX(data, callBackFunction) {
            mui.ajax("<%=ResolveUrl("~/AppHandlers/AppScreenHandler.ashx") %>", {
                async: false,
                data: data,
                type: 'post', //HTTP请求类型
                timeout: 10000, //超时时间设置为10秒；
                // headers: {
                // 	token: localStorage.getItem("appToken")
                // },
                success: function (data, textStatus, xhr) {
                    var dataStr = JSON.parse(data);
                    if (dataStr.State) {
                        callBackFunction(dataStr);
                    } else
                        mui.alert(dataStr.Msg, '提示信息', '确定', function (e) {
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
    </script>
</body>
</html>
