<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Login" Async="true" Codebehind="Login.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
  </asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">

    <div class="LiveMultimediaLogin">
          <asp:Login ID="LoginLiveMultimedia" runat="server" FailureText="Sign-in error. Repeat please." LoginButtonText="Login" PasswordLabelText="Password:" PasswordRequiredErrorMessage="Password is required" RememberMeText="Remember me." TitleText="Log in to Live Multimedia Market" UserNameLabelText="Username (E-Mail):" CreateUserText="New user" CreateUserUrl="~/RegisterUser.aspx" DisplayRememberMe="False" Height="337px" style="font-size: medium" UserNameRequiredErrorMessage="The E-mail field is required" Width="705px" OnAuthenticate="LoginLiveMultimedia_Authenticate" CssClass="LiveMultimediaLoginDialog" PasswordRecoveryUrl="~/RegisterUser.aspx">
              <HyperLinkStyle CssClass="LoginHyperlinkStyle" />
              <LabelStyle CssClass="LoginLabelStyle" />
              <LoginButtonStyle CssClass="LoginButtonStyle" />
              <TextBoxStyle CssClass="LoginTextBoxStyle" />
              <TitleTextStyle CssClass="LoginTitleStyle" />
              <ValidatorTextStyle CssClass="LoginValidatorStyle" />
          </asp:Login>
    </div>
    <div class="LoginDemoMode">
        <asp:HyperLink ID="HyperLinkDemoMode" runat="server" NavigateUrl="~/Demo.aspx">You can log into the demo mode</asp:HyperLink>
    </div>
          
          </asp:Content>

