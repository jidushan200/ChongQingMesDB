//iis
// const IP = "192.168.0.202"
// const PORT = "9997"
// 本地
const IP = "192.168.60.108"
const PORT = "8001"
const WMS = {
    order_station_code: "ORD2",
    orderCNC_station_code: "ORD1",
    order_type: 2,
    orderCNC_type: 1,
    url: "http://" + IP + ":" + PORT + "/AppHandlers/AppStoreHandler.ashx",
    orderUrl: "http://" + IP + ":" + PORT + "/AppHandlers/AppOrderHandler.ashx",
    orderCNCUrl: "http://" + IP + ":" + PORT + "/AppHandlers/AppOrderCNCHandler.ashx"
}