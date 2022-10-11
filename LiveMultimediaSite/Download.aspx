<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Async="true" Inherits="Download" Codebehind="Download.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <br />
    <asp:Label ID="LabelDownload" runat="server" Text="Download or Run"></asp:Label>
    <a style="margin-left: 10px;" href="http://storage.live-mm.com/download/LiveMultimediaServer.msi">Live Multimedia Server</a>
</asp:Content>

