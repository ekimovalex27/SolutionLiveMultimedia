<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="RegisterUser" Async="true" Codebehind="RegisterUser.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">   

<script src="Scripts/RegisterUser.js" type="text/javascript"></script>

    <div class="NewUser">
        <div class="NewUserTitleStyleRow">
            <div class="NewUserTitle">
                <asp:Label ID="LabelTitle" runat="server" Text="Register in Live Multimedia Market" CssClass="NewUserTitleStyle"></asp:Label>
            </div>
        </div>

  <table>
    <tr>
      <td>
        <asp:Label ID="LabelFirstName" runat="server" Text="First Name" CssClass="NewUserLabelStyle"></asp:Label>
      </td>
      <td>
        <asp:TextBox ID="tbFirstName" runat="server" MaxLength="50" Width="400px" CssClass="NewUserTextBoxStyle"></asp:TextBox>
        <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="tbFirstName" ErrorMessage="*" ForeColor="Red"></asp:RequiredFieldValidator>
      </td>
    </tr>
    <tr>
      <td>
        <asp:Label ID="LabelLastName" runat="server" Text="Last Name" CssClass="NewUserLabelStyle"></asp:Label>
      </td>
      <td>
        <asp:TextBox ID="tbLastName" runat="server" MaxLength="50" Width="400px" CssClass="NewUserTextBoxStyle"></asp:TextBox>
        <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ControlToValidate="tbLastName" ErrorMessage="*" ForeColor="Red"></asp:RequiredFieldValidator>
      </td>
    </tr>
    <tr>
      <td>
        <asp:Label ID="LabelEMail" runat="server" Text="E-Mail" CssClass="NewUserLabelStyle"></asp:Label>
      </td>
      <td>
        <asp:TextBox ID="tbUsername" runat="server" MaxLength="50" TextMode="Email" Width="400px" CssClass="NewUserTextBoxStyle"></asp:TextBox>
        <asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server" ControlToValidate="tbUsername" ErrorMessage="*" ForeColor="Red"></asp:RequiredFieldValidator>
      </td>
    </tr>
    <tr>
      <td>
        <asp:Label ID="LabelPassword" runat="server" Text="Password" CssClass="NewUserLabelStyle"></asp:Label>
      </td>
      <td>
        <asp:TextBox ID="tbPassword" runat="server" MaxLength="50" TextMode="Password" Width="300px" CssClass="NewUserTextBoxStyle"></asp:TextBox>
        <asp:RequiredFieldValidator ID="RequiredFieldValidator4" runat="server" ControlToValidate="tbPassword" ErrorMessage="*" ForeColor="Red"></asp:RequiredFieldValidator>        
        <asp:CompareValidator ID="ComparePasswordValidator" runat="server" ControlToCompare="tbPassword" ControlToValidate="tbPasswordReType" ErrorMessage="Passwords do not match" ForeColor="Red"></asp:CompareValidator>
      </td>
    </tr>
    <tr>
      <td>
        <asp:Label ID="LabelPasswordReType" runat="server" Text="Password re-type" CssClass="NewUserLabelStyle"></asp:Label>
      </td>
      <td>
        <asp:TextBox ID="tbPasswordReType" runat="server" MaxLength="50" TextMode="Password" Width="300px" CssClass="NewUserTextBoxStyle"></asp:TextBox>
        <asp:RequiredFieldValidator ID="RequiredFieldValidator5" runat="server" ControlToValidate="tbPasswordReType" ErrorMessage="*" ForeColor="Red"></asp:RequiredFieldValidator>
      </td>
    </tr>
    <tr>
      <td></td>
      <td>
        <div class="g-recaptcha" data-sitekey="6Le0FhATAAAAAI8ebigHlN4v8t2yXlbZYk-avP-Q"></div>
      </td>
    </tr>
  </table>

        <div class="NewUserRow">
            <asp:Label ID="LabelErrorRegister" runat="server" ForeColor="Red" Text="Error register new user"></asp:Label>
        </div>

        <div class="NewUserButtonStyleRow">
            <div class="NewUserButton">
                <asp:Button ID="cmdOk" runat="server" CssClass="NewUserButtonStyle" Text="Register" OnClick="cmdOk_Click" />            
                <asp:Button ID="cmdCancel" runat="server" CssClass="NewUserButtonStyle" Text="Cancel" OnClick="cmdCancel_Click" CausesValidation="False" />
            </div>
        </div>

    </div>

  <div id="lockScreenPanel" class="LockScreenOff">
    <div id="lockScreenTitle1" class="lockScreenTitle1"></div>
    <div id="lockScreenTitle2" class="lockScreenTitle2"></div>
    <div id="spinner" class="spinner">
      <div class="rect1"></div>
      <div class="rect2"></div>
      <div class="rect3"></div>
      <div class="rect4"></div>
      <div class="rect5"></div>
    </div>
  </div> 

</asp:Content>

