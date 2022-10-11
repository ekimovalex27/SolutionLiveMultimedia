<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="getwidthscreen.aspx.cs" Inherits="LiveMultimediaSite.getwidthscreen" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server"></form>

<script type="text/javascript">
    GetScreenResolution();

    //$(document).ready(function () {
    //    GetScreenResolution();
    //});

    function GetScreenResolution() {
        var userScreenResolution = screen.width + "x" + screen.height + "&d=" + screen.colorDepth
        CallServer(userScreenResolution, "");
    }

    function ReceiveServerData(rValue) {

    }
    </script>

</body>
</html>
