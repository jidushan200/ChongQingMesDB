<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="store.aspx.cs" Inherits="AppAPI.index" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width,initial-scale=1,minimum-scale=1,maximum-scale=1,user-scalable=no" />
    <title>立库屏</title>

    <link href="css/mui.min.css" rel="stylesheet" />
    <style type="text/css">
        * {
            margin: 0;
            padding: 0;
        }

        html,
        body {
            width: 100%;
            height: 100%;
            font-family: PingFang SC;
        }

        .mui-content {
            width: 100%;
            height: 100%;
            /* background: #FFFFFF; */
            background: url(img/bg.png);
            background-size: 100% 100%;
            background-repeat: no-repeat;
        }

            .mui-content .main {
                width: 97%;
                height: 100%;
                margin: 0 auto;
                /* margin-top: 0.5rem; */
                /* background-color: #2AC845; */
                display: flex;
            }

                .mui-content .main .left_title,
                .mui-content .main .middle_stock,
                .mui-content .main .right_btn {
                    margin-top: 0.28rem;
                    margin-bottom: 0.28rem;
                }

                /* 左边标题显示 */
                .mui-content .main .left_title {
                    flex: 2;
                    /* background-color: #555555; */
                }

                /* 中间库位 */
                .mui-content .main .middle_stock {
                    flex: 13;
                    /* background-color: #8A6DE9; */
                    /* display: flex; */
                }

                /* 右边操作按钮 */
                .mui-content .main .right_btn {
                    flex: 2;
                    /* background-color: #555555; */
                }

                /* 左边显示详细样式 */
                .mui-content .main .left_title .div_table {
                    width: 86%;
                    /* background-color: #242424; */
                    float: left;
                    margin-left: 0.01rem;
                }

                    /* 左边按钮部分样式 */
                    .mui-content .main .left_title .div_table .div_button {
                        width: 100%;
                        height: 0.45rem;
                        /* background: url(img/stock.png); */
                        background-color: #007AFF;
                        background-size: 100% 100%;
                        background-repeat: no-repeat;
                    }

                        .mui-content .main .left_title .div_table .div_button img {
                            /* width: 100%; */
                            width: 90%;
                            height: 90%;
                        }

                    /* 左侧表格 */
                    .mui-content .main .left_title .div_table table {
                        width: 100%;
                        font-size: 0.18rem;
                    }

                        .mui-content .main .left_title .div_table table tr td {
                            text-align: left;
                            vertical-align: top;
                            /* border: 1px solid #8A6DE9; */
                            /* padding: 0.09rem 0.08rem; */
                            padding-bottom: 0.33rem;
                            color: #333333;
                            padding-left: 0.10rem;
                        }

                        .mui-content .main .left_title .div_table table tr:first-child td {
                            padding-top: 0.33rem;
                        }

                        .mui-content .main .left_title .div_table table tr td:last-child {
                            /* text-align: right; */
                            color: #FF0000;
                            padding-left: 0.05rem;
                            /* padding-right: 0.23rem; */
                        }

                /* 右边显示详细样式 */
                .mui-content .main .right_btn .right_table {
                    width: 86%;
                    /* background-color: #4CD964; */
                    float: right;
                    margin-right: 0.03rem;
                }

                    .mui-content .main .right_btn .right_table .right_button {
                        width: 100%;
                        height: 0.45rem;
                    }

                    .mui-content .main .right_btn .right_table table {
                        width: 100%;
                        /* background-color: #8A6DE9; */
                    }

                        .mui-content .main .right_btn .right_table table tr td {
                            /* border: 1px solid #8A6DE9; */
                            vertical-align: top;
                            padding-top: 0.03rem;
                        }

                            .mui-content .main .right_btn .right_table table tr td div {
                                margin: 0 auto;
                                text-align: center;
                                padding-bottom: 0.39rem;
                            }

                    .mui-content .main .right_btn .right_table button {
                        /* font-size: 0.16rem; */
                        width: 94%;
                        margin: 0 auto;
                    }

                /* 中间库位详细样式 */

                .mui-content .main .middle_stock {
                    display: flex;
                }

                    .mui-content .main .middle_stock .stock_left,
                    .mui-content .main .middle_stock .stock_right {
                        flex: 5;
                    }

                    .mui-content .main .middle_stock .stock_center {
                        flex: 2;
                    }

                    .mui-content .main .middle_stock .rowNum {
                        display: flex;
                        /* border-top: 1px solid #FFFFFF; */
                        /* border-left: 1px solid #FFFFFF; */
                    }

                        .mui-content .main .middle_stock .rowNum .colNum {
                            flex: 1;
                            text-align: center;
                            font-size: 0.2rem;
                            font-family: PingFang SC;
                            /* font-weight: bold; */
                            /* color: #FF3139; */
                            /* border-left: 1px solid #FFFFFF; */
                            position: relative;
                        }

                    .mui-content .main .middle_stock .rowNum {
                        height: 0.45rem;
                    }

                        .mui-content .main .middle_stock .rowNum .colNum button {
                            padding-left: 0.03rem;
                            padding-right: 0.03rem;
                        }

                    /* 左右两侧库位背景 */
                    .mui-content .main .middle_stock .rowNum1 .colNum.stock_area {
                        /* background: #DDDDDD; */
                        background: url(img/stock.png);
                        background-repeat: no-repeat;
                        background-size: 96% 98%;
                        background-position: center;
                    }

                        /* 左右两侧库位串号,库位编号 */
                        .mui-content .main .middle_stock .rowNum1 .colNum.stock_area .productId,
                        .mui-content .main .middle_stock .rowNum1 .colNum.stock_area .productNum {
                            position: absolute;
                            margin-bottom: 0;
                        }

                        .mui-content .main .middle_stock .rowNum1 .colNum.stock_area .productId {
                            bottom: 0.03rem;
                            display: block;
                            width: 100%;
                            text-align: center;
                            font-size: 0.12rem;
                            font-family: PingFang SC;
                            /* font-weight: bold; */
                            color: #666666;
                            /* line-height: 21px; */
                        }

                        .mui-content .main .middle_stock .rowNum1 .colNum.stock_area .productNum {
                            top: 0.04rem;
                            right: 0.12rem;
                            color: #fff;
                            font-size: 0.15rem;
                            /* font-weight: bold; */
                            color: #FFFFFF;
                        }

                    /*库位中间部分: 21,22库位样式 */
                    .mui-content .main .middle_stock .stock_center {
                        /* background-color: #007AFF; */
                    }

                        .mui-content .main .middle_stock .stock_center div#center_area {
                            /* background-color: #2AC845; */
                            width: 30%;
                            /* height: 100%; */
                            margin: 0 auto;
                        }

                            .mui-content .main .middle_stock .stock_center div#center_area .mid_stock_area {
                                background: url(img/stock.png);
                                background-repeat: no-repeat;
                                background-size: 100% 100%;
                                background-size: 98% 98%;
                                background-position: center;
                                width: 100%;
                                /* height: 2rem; */
                                /* margin-top: 1.4rem; */
                                font-size: 0.16rem;
                                text-align: center;
                                color: #333;
                                position: relative;
                            }

                                .mui-content .main .middle_stock .stock_center div#center_area .mid_stock_area span {
                                    text-align: center;
                                    font-size: 0.2rem;
                                    font-family: PingFang SC;
                                    /* font-weight: bold; */
                                    /* color: #FF3139; */
                                }

                                .mui-content .main .middle_stock .stock_center div#center_area .mid_stock_area p.stock_serial {
                                    position: absolute;
                                    top: 0.65rem;
                                    display: block;
                                    width: 100%;
                                    text-align: center;
                                    font-size: 0.12rem;
                                    color: #666;
                                    /* margin: auto; */
                                }

                                .mui-content .main .middle_stock .stock_center div#center_area .mid_stock_area p.stock_num {
                                    position: absolute;
                                    top: 0.04rem;
                                    right: 0.12rem;
                                    color: #fff;
                                    font-size: 0.15rem;
                                    /* font-weight: bold; */
                                    color: #FFFFFF;
                                }

                                .mui-content .main .middle_stock .stock_center div#center_area .mid_stock_area p.stock_note {
                                    position: absolute;
                                    bottom: 0.03rem;
                                    display: block;
                                    width: 100%;
                                    text-align: center;
                                    font-size: 0.12rem;
                                    font-family: PingFang SC;
                                    /* font-weight: bold; */
                                    color: #666666;
                                    margin-bottom: 0;
                                }

        /* 多选框 */
        .mui-checkbox input[type=checkbox] {
            top: 0.03rem;
            left: 0.07rem;
        }

            .mui-checkbox input[type=checkbox]:before {
                font-size: 0.20rem;
                color: #1E6CEB;
            }

            .mui-checkbox input[type=checkbox]:checked:before {
                color: #1E6CEB;
            }

        /* 确认框换行 */
        .mui-popup-title + .mui-popup-text {
            word-wrap: break-word;
        }

        /* 多选框默认隐藏 */
        /* .mui-checkbox{
				display: none;
			} */
        .display_none {
            display: none;
        }
    </style>
</head>
<body>
    <div class="mui-content">
        <div class="main">
            <div class="left_title" id="left_title">
                <div class="div_table">
                    <div class="div_button">
                        <!-- <p>WMS</p> -->
                        <img src="img/bg.png">
                    </div>
                    <table>
                        <tr>
                            <td>库位:</td>
                            <td><span id="tit_stock">46</span></td>
                        </tr>
                        <tr>
                            <td>RF原料:</td>
                            <td><span id="tit_material">20</span></td>
                        </tr>
                        <tr>
                            <td>RF产品:</td>
                            <td><span id="tit_product">4</span></td>
                        </tr>
                        <tr>
                            <td>空托盘:</td>
                            <td><span id="tit_null">2</span></td>
                        </tr>
                        <tr>
                            <td>无托盘:</td>
                            <td><span id="tit_allnull">18</span></td>
                        </tr>
                        <tr>
                            <td>U盘原料:</td>
                            <td><span id="tit_Udisk">10</span></td>
                        </tr>
                    </table>
                </div>
            </div>
            <div class="middle_stock">
                <div class="stock_left">
                    <div class="rowNum" id="">
                        <div class="colNum">
                            <button minval="1" maxval="5" class="mui-btn mui-btn-blue btn_select"
                                id="btnSelect01">
                                选择01-05</button>
                        </div>
                        <div class="colNum">
                            <button minval="6" maxval="10" class="mui-btn mui-btn-blue btn_select"
                                id="btnSelect06">
                                选择06-10</button>
                        </div>
                        <div class="colNum">
                            <button minval="11" maxval="15" class="mui-btn mui-btn-blue btn_select"
                                id="btnSelect11">
                                选择11-15</button>
                        </div>
                        <div class="colNum">
                            <button minval="16" maxval="20" class="mui-btn mui-btn-blue btn_select"
                                id="btnSelect16">
                                选择16-20</button>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="05" type="checkbox" id="check05">
                            </div>
                            <span class="colNum_span" id="span05">产品G</span>
                            <p class="productId" id="serialNum05">RFID2019/1</p>
                            <p class="productNum" id="">05</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="10" type="checkbox" id="check10">
                            </div>
                            <span class="colNum_span" id="span10">产品G</span>
                            <p class="productId" id="serialNum10">RFID2019/1</p>
                            <p class="productNum" id="">10</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="15" type="checkbox" id="check15">
                            </div>
                            <span class="colNum_span" id="span15">产品G</span>
                            <p class="productId" id="serialNum15">RFID2019/1</p>
                            <p class="productNum" id="">15</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="20" type="checkbox" id="check20">
                            </div>
                            <span class="colNum_span" id="span20">产品R</span>
                            <p class="productId" id="serialNum20">RFID2019090</p>
                            <p class="productNum" id="">20</p>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="04" type="checkbox" id="check04">
                            </div>
                            <span class="colNum_span" id="span04">原料G</span>
                            <p class="productId" id="serialNum04">RFID2019/1</p>
                            <p class="productNum" id="">04</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="09" type="checkbox" id="check09">
                            </div>
                            <span class="colNum_span" id="span09">原料G</span>
                            <p class="productId" id="serialNum09">RFID2019/1</p>
                            <p class="productNum" id="">09</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="14" type="checkbox" id="check14">
                            </div>
                            <span class="colNum_span" id="span14">原料R</span>
                            <p class="productId" id="serialNum14">RFID2019/1</p>
                            <p class="productNum" id="">14</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="19" type="checkbox" id="check19">
                            </div>
                            <span class="colNum_span" id="span19">原料R</span>
                            <p class="productId" id="serialNum19">RFID2019/1</p>
                            <p class="productNum" id="">19</p>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="03" type="checkbox" id="check03">
                            </div>
                            <span class="colNum_span" id="span03">原料G</span>
                            <p class="productId" id="serialNum03">RFID2019/1</p>
                            <p class="productNum" id="">03</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="08" type="checkbox" id="check08">
                            </div>
                            <span class="colNum_span" id="span08">原料G</span>
                            <p class="productId" id="serialNum08">RFID2019/1</p>
                            <p class="productNum" id="">08</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="13" type="checkbox" id="check13">
                            </div>
                            <span class="colNum_span" id="span13">原料G</span>
                            <p class="productId" id="serialNum13">RFID2019/1</p>
                            <p class="productNum" id="">13</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="18" type="checkbox" id="check18">
                            </div>
                            <span class="colNum_span" id="span18">原料G</span>
                            <p class="productId" id="serialNum18">RFID2019/1</p>
                            <p class="productNum" id="">18</p>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="02" type="checkbox" id="check02">
                            </div>
                            <span class="colNum_span" id="span02">原料R</span>
                            <p class="productId" id="serialNum02">RFID2019/1</p>
                            <p class="productNum" id="">02</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="07" type="checkbox" id="check07">
                            </div>
                            <span class="colNum_span" id="span07">原料G</span>
                            <p class="productId" id="serialNum07">RFID2019/1</p>
                            <p class="productNum" id="">07</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="12" type="checkbox" id="check12">
                            </div>
                            <span class="colNum_span" id="span12">原料G</span>
                            <p class="productId" id="serialNum12">RFID2019/1</p>
                            <p class="productNum" id="">12</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="17" type="checkbox" id="check17">
                            </div>
                            <span class="colNum_span" id="span17">原料G</span>
                            <p class="productId" id="serialNum17">RFID2019/1</p>
                            <p class="productNum" id="">17</p>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="01" type="checkbox" id="check01">
                            </div>
                            <span class="colNum_span" id="span01">产品G</span>
                            <p class="productId" id="serialNum01">RFID2019/1</p>
                            <p class="productNum" id="">01</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="06" type="checkbox" id="check06">
                            </div>
                            <span class="colNum_span" id="span06">产品G</span>
                            <p class="productId" id="serialNum06">RFID2019/1</p>
                            <p class="productNum" id="">06</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="11" type="checkbox" id="check11">
                            </div>
                            <span class="colNum_span" id="span11">产品G</span>
                            <p class="productId" id="serialNum11">RFID2019/1</p>
                            <p class="productNum" id="">11</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="16" type="checkbox" id="check16">
                            </div>
                            <span class="colNum_span" id="span16">产品G</span>
                            <p class="productId" id="serialNum16">RFID2019/1</p>
                            <p class="productNum" id="">16</p>
                        </div>
                    </div>
                </div>
                <div class="stock_center">
                    <div id="center_area">
                        <div style="height: 0.45rem;"></div>
                        <div class="center_rowNum"></div>
                        <div class="mid_stock_area">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="21" type="checkbox" id="check21">
                            </div>
                            <span class="colNum_span" id="stock21">产品</span>
                            <p class="stock_serial" id="stock_serial21">FR002002006</p>
                            <p class="stock_num" id="">21</p>
                            <p class="stock_note">自动出/入口</p>
                        </div>
                        <div class="center_rowNum"></div>
                        <div class="mid_stock_area">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="22" type="checkbox" id="check22">
                            </div>
                            <span class="colNum_span" id="stock22">产品</span>
                            <p class="stock_serial" id="stock_serial22">FR002002007</p>
                            <p class="stock_num">22</p>
                            <p class="stock_note">手动出/入口</p>
                        </div>
                    </div>
                </div>
                <div class="stock_right">
                    <div class="rowNum">
                        <div class="colNum">
                            <button minval="23" maxval="27" class="mui-btn mui-btn-blue btn_select"
                                id="btnSelect23">
                                选择23-27</button>
                        </div>
                        <div class="colNum">
                            <button minval="28" maxval="32" class="mui-btn mui-btn-blue btn_select"
                                id="btnSelect28">
                                选择28-32</button>
                        </div>
                        <div class="colNum">
                            <button minval="33" maxval="37" class="mui-btn mui-btn-blue btn_select"
                                id="btnSelect33">
                                选择33-37</button>
                        </div>
                        <div class="colNum">
                            <button minval="38" maxval="42" class="mui-btn mui-btn-blue btn_select"
                                id="btnSelect38">
                                选择38-42</button>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="27" type="checkbox" id="check27">
                            </div>
                            <span class="colNum_span" id="span27">原料R</span>
                            <p class="productId" id="serialNum27">RFID2019/1</p>
                            <p class="productNum" id="">27</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="32" type="checkbox" id="check32">
                            </div>
                            <span class="colNum_span" id="span32">原料R</span>
                            <p class="productId" id="serialNum32">RFID2019/1</p>
                            <p class="productNum" id="">32</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="37" type="checkbox" id="check37">
                            </div>
                            <span class="colNum_span" id="span37">原料R</span>
                            <p class="productId" id="serialNum37">RFID2019/1</p>
                            <p class="productNum" id="">37</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox">
                                <input class="check_input" name="checkbox" value="42" type="checkbox" id="check42">
                            </div>
                            <span class="colNum_span" id="span42">原料R</span>
                            <p class="productId" id="serialNum42">RFID2019/1</p>
                            <p class="productNum" id="">42</p>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="26" type="checkbox" id="check26">
                            </div>
                            <span class="colNum_span" id="span26">原料R</span>
                            <p class="productId" id="serialNum26">RFID2019/1</p>
                            <p class="productNum" id="">26</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="31" type="checkbox" id="check31">
                            </div>
                            <span class="colNum_span" id="span31">原料R</span>
                            <p class="productId" id="serialNum31">RFID2019/1</p>
                            <p class="productNum" id="">31</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox">
                                <input class="check_input" name="checkbox" value="36" type="checkbox" id="check36">
                            </div>
                            <span class="colNum_span" id="span36">原料R</span>
                            <p class="productId" id="serialNum36">RFID2019/1</p>
                            <p class="productNum" id="">36</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox">
                                <input class="check_input" name="checkbox" value="41" type="checkbox" id="check41">
                            </div>
                            <span class="colNum_span" id="span41">原料R</span>
                            <p class="productId" id="serialNum41">RFID2019/1</p>
                            <p class="productNum" id="">41</p>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="25" type="checkbox" id="check25">
                            </div>
                            <span class="colNum_span" id="span25">原料R</span>
                            <p class="productId" id="serialNum25">RFID2019/1</p>
                            <p class="productNum" id="">25</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="30" type="checkbox" id="check30">
                            </div>
                            <span class="colNum_span" id="span30">原料R</span>
                            <p class="productId" id="serialNum30">RFID2019/1</p>
                            <p class="productNum" id="">30</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="35" type="checkbox" id="check35">
                            </div>
                            <span class="colNum_span" id="span35">原料R</span>
                            <p class="productId" id="serialNum35">RFID2019/1</p>
                            <p class="productNum" id="">35</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox">
                                <input class="check_input" name="checkbox" value="40" type="checkbox" id="check40">
                            </div>
                            <span class="colNum_span" id="span40">原料R</span>
                            <p class="productId" id="serialNum40">RFID2019/1</p>
                            <p class="productNum" id="">40</p>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox">
                                <input class="check_input" name="checkbox" value="24" type="checkbox" id="check24">
                            </div>
                            <span class="colNum_span" id="span24">原料R</span>
                            <p class="productId" id="serialNum24">RFID2019/1</p>
                            <p class="productNum" id="">24</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="29" type="checkbox" id="check29">
                            </div>
                            <span class="colNum_span" id="span29">原料R</span>
                            <p class="productId" id="serialNum29">RFID2019/1</p>
                            <p class="productNum" id="">29</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="34" type="checkbox" id="check34">
                            </div>
                            <span class="colNum_span" id="span34">原料R</span>
                            <p class="productId" id="serialNum34">RFID2019/1</p>
                            <p class="productNum" id="">34</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox">
                                <input class="check_input" name="checkbox" value="39" type="checkbox" id="check39">
                            </div>
                            <span class="colNum_span" id="span39">原料R</span>
                            <p class="productId" id="serialNum39">RFID2019/1</p>
                            <p class="productNum" id="">39</p>
                        </div>
                    </div>
                    <div class="rowNum rowNum1" id="">
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="23" type="checkbox" id="check23">
                            </div>
                            <span class="colNum_span" id="span23">原料R</span>
                            <p class="productId" id="serialNum23">RFID2019/1</p>
                            <p class="productNum" id="">23</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox ">
                                <input class="check_input" name="checkbox" value="28" type="checkbox" id="check28">
                            </div>
                            <span class="colNum_span" id="span28">原料R</span>
                            <p class="productId" id="serialNum28">RFID2019/1</p>
                            <p class="productNum" id="">28</p>
                        </div>
                        <div class="colNum stock_area" id="">
                            <div class="mui-checkbox">
                                <input class="check_input" name="checkbox" value="33" type="checkbox" id="check33">
                            </div>
                            <span class="colNum_span" id="span33">无托盘</span>
                            <p class="productId" id="serialNum33">RFID2019/1</p>
                            <p class="productNum" id="">33</p>
                        </div>
                        <div class="colNum stock_area">
                            <div class="mui-checkbox">
                                <input class="check_input" name="checkbox" value="38" type="checkbox" id="check38">
                            </div>
                            <span class="colNum_span" id="span38">空托盘</span>
                            <p class="productId" id="serialNum38">RFID2019/1</p>
                            <p class="productNum" id="">38</p>
                        </div>
                    </div>
                </div>
            </div>
            <div class="right_btn" id="btn_header">
                <div class="right_table">
                    <div class="right_button">
                        <!-- <button type="button" class="mui-btn mui-btn-blue" id="btnChecked">选择库位</button> -->
                    </div>
                    <table>
                        <tr>
                            <td>
                                <div>
                                    <button type="button" class="mui-btn mui-btn-blue" id="btnChecked">选择库位</button>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div>
                                    <button type="button" class="mui-btn mui-btn-blue" id="btnCancel">取消选择</button>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div>
                                    <button type="button" changename="原料G" actionname="转为原料G"
                                        class="btn_change mui-btn mui-btn-blue" id="btnToG">
                                        转为原料G</button>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div>
                                    <button type="button" changename="原料R" actionname="转为原料R"
                                        class="btn_change mui-btn mui-btn-blue" id="btnToR">
                                        转为原料R</button>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div>
                                    <button type="button" changename="空托盘" actionname="转为空托盘"
                                        class="btn_change mui-btn mui-btn-blue" id="btnToNull">
                                        转为空托盘</button>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div>
                                    <button type="button" changename="无托盘" actionname="转为无托盘"
                                        class="btn_change mui-btn mui-btn-blue" id="btnToAllNull">
                                        转为无托盘</button>

                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <div>
                                    <button class="mui-btn mui-btn-blue" id="btnUdisk">U盘原料数量</button>
                                </div>
                            </td>
                        </tr>
                        <!-- <tr>
								<td>
									<div>
										<button type="button" changeName = "产品出库" class="btn_change mui-btn mui-btn-blue" id="btnOut">产品出库</button>
									</div>
								</td>
							</tr> -->
                    </table>
                </div>
            </div>
        </div>
    </div>

    <script src="js/mui.min.js"></script>
    <%--<script src="js/jquery.min.js"></script>--%>
    <%--<script src="js/muiAjax.js"></script>--%>
    <script src="js/rem.js" type="text/javascript"></script>
    <script type="text/javascript" charset="utf-8">
        //mui.init();
        var ws = null;
        var colHeight = 0;
        var colWidth = 0;
        var socket;
        //mui.plusReady(function () {
        // 横屏显示
        //plus.screen.lockOrientation("landscape");
        //ws = plus.webview.currentWebview();

        //动态设置库位和多选框区域高度
        onresizeHeight();

        //获取数据设置值
        setData();

        //多选框默认不显示
        var checkList = document.getElementsByClassName('check_input');
        for (i = 0; i < checkList.length; i++) {
            //document.getElementsByClassName('mui-checkbox')[i].classList.add("display_none");
            document.getElementsByClassName('check_input')[i].style.display = "none";
        }
        //});

        //动态设置库位和多选框区域高度
        window.onresize = function () {
            onresizeHeight();
        }

        //定时器: 确保websocket处于连接状态
        setInterval(function () {
            if (socket) {
                if (socket.readyState == 0 || socket.readyState == 3)
                    setWebSocket();
            } else
                setWebSocket();
        }, 6000);

        //点击"U盘原料"
        document.getElementById('btnUdisk').addEventListener('tap', function () {
            mui.prompt('', '请输入U盘数量', 'U盘数量', ['取消', '确定'], function (e) {
                if (e.index == 1) {
                    if (e.value.length == 0) {
                        mui.alert('数量不能为空', '错误提示', '确定', function (e) {
                            e.index
                        }, 'div');
                        return;
                    }
                    var data = {
                        method: 'SubmitUNum',
                        cnt: e.value
                    }
                    muiAJAX(data, function (dataStr) {
                        mui.toast(dataStr.Msg);
                    });
                }
            }, 'div');
        });

        //选择库位事件
        document.getElementById('btnChecked').addEventListener('tap', function () {
            //显示多选框并设置多选框状态为未选中
            var checkList = document.getElementsByClassName('check_input');
            for (i = 0; i < checkList.length; i++) {
                checkList[i].style.display = "block";
                //checkList[i].checked = false;
            }
        });

        //选择库位(m-n) 按钮点击事件绑定:
        mui('.middle_stock').on('tap', 'button.btn_select', function () {
            //alert(this.getAttribute('maxVal'))
            var min = parseInt(this.getAttribute('minVal'));
            var max = parseInt(this.getAttribute('maxVal'));

            //显示多选框并设置多选框状态为未选中
            var checkList = document.getElementsByClassName('check_input');
            for (i = 0; i < checkList.length; i++) {
                checkList[i].style.display = "block";
                //checkList[i].checked = false;
            }
            //根据min和max的范围选择对应的库位
            for (i = min; i <= max; i++) {
                var k = i < 10 ? '0' + i : i;
                document.getElementById('check' + k).checked = true;
            }
        });

        //取消库位事件
        document.getElementById('btnCancel').addEventListener('tap', function () {
            //隐藏多选框
            var checkList = document.getElementsByClassName('check_input');
            for (i = 0; i < checkList.length; i++) {
                checkList[i].checked = false;
                checkList[i].style.display = "none";
            }
        });

        //转换状态按钮点击事件绑定: 转为原料R,转为原料G,转为空托盘,转为无托盘,产品出库
        mui('.right_table').on('tap', 'button.btn_change', function () {
            //alert(this.innerHTML);
            var changeName = this.getAttribute('changeName');
            var actionName = this.getAttribute('actionName');

            //得到选中的库位号
            var checkList = getRadioRes('check_input');
            if (checkList.length == 0) {
                mui.alert('请先选择库位！', '提示信息', '确定', function (e) {
                    e.index
                }, 'div');
                return;
            }
            //向后台发送数据,进行转换
            mui.confirm('库位' + checkList + actionName + '?', '确认信息', ['取消', '确认'], function (e) {
                if (e.index == 1) {
                    var data = {
                        changeName: changeName,
                        actionName: actionName,
                        checkList: checkList,
                        method: 'ChangeState'
                    }
                    muiAJAX(data, function (dataStr) {
                        mui.toast(dataStr.Msg);
                        // setTimeout(function() {
                        // 	setData();
                        // }, 500);
                    });
                }
            }, 'div');
        })

        function setData() {
            console.log('查找')
            var data = {
                method: 'Query'
            }
            muiAJAX(data, function (dataStr) {
                //alert(dataStr[0].length)
                var data0 = dataStr.RData["Data0"][0];
                document.getElementById('tit_stock').innerHTML = data0.locations;
                document.getElementById('tit_material').innerHTML = data0.material;
                document.getElementById('tit_product').innerHTML = data0.product;
                document.getElementById('tit_null').innerHTML = data0.storenull;
                document.getElementById('tit_allnull').innerHTML = data0.storeallnull;
                document.getElementById('tit_Udisk').innerHTML = data0.cnt;

                var data1 = dataStr.RData["Data1"];
                for (i = 0; i < data1.length; i++) {
                    var num = data1[i].location < 10 ? '0' + data1[i].location : data1[i].location;
                    document.getElementById('span' + num).innerHTML = data1[i].stockname;
                    document.getElementById('serialNum' + num).innerHTML = data1[i].serialnumber;
                    var storedtypeid = data1[i].storedtypeid;
                    if (storedtypeid == '1') {
                        document.getElementById('span' + num).style.color = '#31A952';
                    } else if (storedtypeid == '2') {
                        document.getElementById('span' + num).style.color = '#964EA5';
                    } else if (storedtypeid == '3') {
                        document.getElementById('span' + num).style.color = '#1E6CEB';
                    } else if (storedtypeid == null) {
                        document.getElementById('span' + num).style.color = '#FF3139';
                    } else {
                        document.getElementById('span' + num).style.color = '#333333';
                    }
                }

                var data2 = dataStr.RData["Data2"];
                for (i = 0; i < data2.length; i++) {
                    var num = data2[i].location < 10 ? '0' + data2[i].location : data2[i].location;
                    document.getElementById('stock' + num).innerHTML = data2[i].stockname;
                    document.getElementById('stock_serial' + num).innerHTML = data2[i].serialnumber;
                    var storedtypeid = data2[i].storedtypeid;
                    if (storedtypeid == '1') {
                        document.getElementById('stock' + num).style.color = '#31A952';
                    } else if (storedtypeid == '2') {
                        document.getElementById('stock' + num).style.color = '#964EA5';
                    } else if (storedtypeid == '3') {
                        document.getElementById('stock' + num).style.color = '#1E6CEB';
                    } else if (storedtypeid == null) {
                        document.getElementById('stock' + num).style.color = '#FF3139';
                    } else {
                        document.getElementById('stock' + num).style.color = '#333333';
                    }
                }
            });
        }

        //获取复选框的值-------使用数组
        function getRadioRes(className) {
            var rdsObj = document.getElementsByClassName(className); /*获取值*/
            // alert(rdsObj.length);测试一下找到几个节点
            var checkVal = new Array();
            var k = 0;
            for (i = 0; i < rdsObj.length; i++) {
                if (rdsObj[i].checked) {
                    checkVal[k] = rdsObj[i].value;
                    k++;
                }
            }
            return checkVal;
        }

        //动态设置库位和多选框区域高度
        function onresizeHeight() {
            colWidth = (document.body.clientWidth * 1.05 / 14).toFixed(2);
            colHeight = (document.body.clientWidth * 1.20 / 14).toFixed(2);
            var rightHeight = document.body.clientHeight;
            // var leftTable = '.mui-content .main .left_title .div_table table';
            // var rightTable = '.mui-content .main .right_btn .right_table table';

            // // 左边标题table的高度
            // document.querySelector(leftTable).style.height = colHeight * 5 + 'px';
            // // 右边按钮table的高度
            // document.querySelector(rightTable).style.height = colHeight * 5 + 'px';

            // 设置每一行库位的高度

            var rowObj = document.getElementsByClassName('rowNum1');
            for (i = 0; i < rowObj.length; i++) {
                rowObj[i].style.height = colHeight + 'px';
            }
            //设置每列库位的选择按钮宽度
            var btnObj = document.getElementsByClassName('btn_select');
            for (i = 0; i < btnObj.length; i++) {
                btnObj[i].style.width = (colWidth - 4) + 'px';
            }

            //这是中间库位的宽高
            document.getElementById('center_area').style.width = colWidth + 'px';
            var midObj = document.getElementsByClassName('mid_stock_area');
            for (i = 0; i < midObj.length; i++) {
                midObj[i].style.height = colHeight + 'px';
                document.getElementsByClassName('center_rowNum')[i].style.height = colHeight + 'px';
            }
            //document.querySelector(rightTable).style.height = colHeight * 5 + 'px';

            // 设置每一个多选框的可选区域
            var cksObj = document.getElementsByClassName('check_input');
            for (i = 0; i < cksObj.length; i++) {
                cksObj[i].style.width = colWidth + 'px';
                cksObj[i].style.height = colHeight + 'px';
                // cksObj[i].style.margin = '0 auto';
                // cksObj[i].style.border = "1px solid red";  
                document.getElementsByClassName('colNum_span')[i].style.lineHeight = colHeight + 'px';
                //document.getElementsByClassName('stock_span')[i].style.lineHeight = colHeight + 'px';
            }
        }

        //启动WebSocket
        function setWebSocket() {
            if (!this.WebSocket) {
                this.WebSocket = this.MozWebSocket;
            }
            if (this.WebSocket) {
                socket = new WebSocket("ws://localhost:58836/AppHandlers/AppStoreHandler.ashx?method=OpenWebSocket");
                socket.onmessage = function (e) {
                    console.log("服务器向客户端传输数据" + e.data);
                    if (e.data == 'updated') {
                        console.log(e.data)
                        setData();
                    }
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

        function muiAJAX(data, callBackFunction) {
            mui.ajax("<%=ResolveUrl("~/AppHandlers/AppStoreHandler.ashx") %>", {
                data: data,
                type: 'post', //HTTP请求类型
                timeout: 10000, //超时时间设置为10秒；
                // headers: {
                // 	token: localStorage.getItem("appToken")
                // },
                traditional: true,
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

            <%--$.ajax({
                type: "POST",
                async: false,
                url: "<%=ResolveUrl("~/AppHandlers/AppStoreHandler.ashx") %>",
                data: data,
                traditional: true,
                success: function (data) {
                    var dataStr = JSON.parse(data);
                    if (dataStr.State) {
                        callBackFunction(dataStr);
                    } else
                        mui.alert(dataStr.Msg, '提示信息', '确定', function (e) {
                            e.index
                        }, 'div')
                },
                error: function () {
                    mui.alert('网络调用失败!', '错误信息', '确定', function (e) {
                        e.index
                    }, 'div');
                }
            });--%>
        }
    </script>
</body>
</html>

