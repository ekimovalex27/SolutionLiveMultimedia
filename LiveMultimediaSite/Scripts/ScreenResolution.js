function GetScreenResolution() {
    //var userScreenResolution = screen.width + ";" + screen.height;
    var userScreenResolution = screen.width + "x" + screen.height + "&d=" + screen.colorDepth    
    CallServer(userScreenResolution, "");
}

function ReceiveServerData(rValue) {

}