<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="ProfileUser"  Async="true" Codebehind="ProfileUser.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server"></asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
  
<script src="Scripts/UpdateUser.js" type="text/javascript"></script>

  <div class="NewUser">     
       
    <div class="NewUserTitleStyleRow">
      <div class="NewUserTitle">
        <asp:Label ID="LabelTitle" runat="server" Text="User information" CssClass="NewUserTitleStyle"></asp:Label>
      </div>
    </div>

    <div class="NewUserTitleStyleRow">
      <div class="NewUserTitle">
        <asp:Label ID="LabelChangePassword" runat="server" Text="Change password" CssClass="NewUserTitleStyle"></asp:Label>
      </div>
    </div>

    <div>
    <asp:Label ID="OldPassword" runat="server" Text="Old password" CssClass="NewUserLabelStyle"></asp:Label>
    <asp:TextBox ID="tbOldPassword" runat="server" TextMode="Password" CssClass="NewUserTextBoxStyle"></asp:TextBox>      
    <asp:RequiredFieldValidator ID="RequiredFieldValidatorOldPassword" runat="server" ErrorMessage="*" ControlToValidate="tbOldPassword" ForeColor="Red"></asp:RequiredFieldValidator>        
    </div>

    <div>
      <asp:Label ID="NewPassword1" runat="server" Text="New password" CssClass="NewUserLabelStyle"></asp:Label>
      <asp:TextBox ID="tbNewPassword1" runat="server" TextMode="Password" CssClass="NewUserTextBoxStyle"></asp:TextBox>
      <asp:RequiredFieldValidator ID="RequiredFieldValidatorNewPassword1" runat="server" ErrorMessage="*" ControlToValidate="tbNewPassword1" ForeColor="Red"></asp:RequiredFieldValidator>    
    </div>

    <div>
      <asp:Label ID="NewPassword2" runat="server" Text="Re-type new password" CssClass="NewUserLabelStyle"></asp:Label>
      <asp:TextBox ID="tbNewPassword2" runat="server" TextMode="Password" CssClass="NewUserTextBoxStyle"></asp:TextBox>
      <asp:RequiredFieldValidator ID="RequiredFieldValidatorNewPassword2" runat="server" ErrorMessage="*" ControlToValidate="tbNewPassword2" ForeColor="Red"></asp:RequiredFieldValidator>
    </div>

    <div class="g-recaptcha" data-sitekey="6Le0FhATAAAAAI8ebigHlN4v8t2yXlbZYk-avP-Q"></div>
    <div><asp:CompareValidator ID="CompareValidator1" runat="server" ErrorMessage="Old password should not match the new" ControlToCompare="tbOldPassword" ControlToValidate="tbNewPassword1" Operator="NotEqual" ForeColor="Red"></asp:CompareValidator></div>
    <div><asp:CompareValidator ID="CompareValidator2" runat="server" ErrorMessage="Old password should not match the new" ControlToCompare="tbOldPassword" ControlToValidate="tbNewPassword2" ForeColor="Red" Operator="NotEqual"></asp:CompareValidator></div>
    <div><asp:CompareValidator ID="CompareValidatorPassword" runat="server" ErrorMessage="New passwords do not match" ForeColor="Red" ControlToCompare="tbNewPassword1" ControlToValidate="tbNewPassword2" Display="Dynamic"></asp:CompareValidator></div>
    <div><asp:Label ID="LabelErrorUpdateUser" runat="server" ForeColor="Red" Text="Error updating information about user"></asp:Label></div>
    <div><asp:Label ID="LabelOkUpdateUser" runat="server" ForeColor="Green" Text="Successful update information about user"></asp:Label></div>

    <div class="NewUserButtonStyleRow">
      <div class="NewUserButton">
        <asp:Button ID="cmdOk" runat="server" CssClass="NewUserButtonStyle" Text="Save" OnClick="cmdOk_Click" />
        <asp:Button ID="cmdCancel" runat="server" CssClass="NewUserButtonStyle" Text="Cancel" OnClick="cmdCancel_Click" CausesValidation="False"/>
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

