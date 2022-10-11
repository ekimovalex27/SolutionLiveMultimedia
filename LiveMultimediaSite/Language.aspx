<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Language" Async="true" Codebehind="Language.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
  <asp:GridView ID="GridViewLanguage" runat="server">
    <RowStyle CssClass="LanguageRow" />
  </asp:GridView>
</asp:Content>

