<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Async="true" CodeBehind="oops.aspx.cs" Inherits="LiveMultimediaSite.oops" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div>
        <asp:Label ID="LabelMessage1" runat="server" Text="Something happened at the site"></asp:Label>
        <br />
        <asp:Label ID="LabelMessage2" runat="server" Text="Try again or go to the home page"></asp:Label>
        <br />
        <br />
        <asp:TextBox ID="TextBoxDebug" runat="server" TextMode="MultiLine" Visible="False" Height="339px" Width="876px" ReadOnly="True" EnableViewState="False"></asp:TextBox>
    </div>
</asp:Content>
