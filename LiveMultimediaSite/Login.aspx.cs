using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Threading.Tasks;

//using Microsoft.Live;

using LiveMultimediaSite.LiveMultimediaService;
using LiveMultimediaServiceConnection;

public partial class Login : System.Web.UI.Page
{

  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

    protected void Page_Load(object sender, EventArgs e)
    {
      SetLocalization();

      if (!Page.IsPostBack)
      {
        if (Session["UserToken"].ToString() != "")
        {
          //make logout
        }
      }
    }

  protected void LoginLiveMultimedia_Authenticate(object sender, AuthenticateEventArgs e)
  {
    #region Define vars
    Task<Tuple<string, string>> returnValue;
    #endregion Define vars

    System.Web.UI.WebControls.Login LoginLiveMultimedia;
    string Username = ""; string Password = "";
    string UserToken = "";

    try
    {
      LoginLiveMultimedia = (System.Web.UI.WebControls.Login)sender;
      Username = LoginLiveMultimedia.UserName.Trim();
      Password = LoginLiveMultimedia.Password.Trim();

      using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
      {
        returnValue = LiveMultimedia.RemoteLoginAsync(Application["ServerAccountKey"].ToString(), Username, Password);
        returnValue.Wait();
      }
      if (!LiveMultimediaLibrary.CheckGoodString(returnValue.Result.Item2))
      {
        UserToken = returnValue.Result.Item1;
      }
      else
      {
        throw new ArgumentException(returnValue.Result.Item2, "Username");
      }
    }
    catch (Exception)
    {
      UserToken = "";
    }

    if (UserToken != "")
    {
      e.Authenticated = true;
      Session["UserToken"] = UserToken;
      Session["Username"] = Username;
      if (LiveMultimediaLibrary.CheckGoodString(Session["PrevPage"].ToString()))
      {
        //Response.Redirect("~/" + Session["PrevPage"].ToString(), false);          
        //HttpContext.Current.ApplicationInstance.CompleteRequest();
        Response.Redirect("~/" + Session["PrevPage"].ToString());
      }
      else
      {
        Response.Redirect("~/Default.aspx", false);
        HttpContext.Current.ApplicationInstance.CompleteRequest();
      }
    }
    else
    {
      e.Authenticated = false;
      Session["Username"] = "";
      Session["UserToken"] = "";
    }
  }

  private void SetLocalization()
    {
      this.LoginLiveMultimedia.CreateUserText = GetElementLocalization("Login_CreateUserText", "New user");
      this.LoginLiveMultimedia.FailureText = GetElementLocalization("Login_FailureText", "Sign-in error. Repeat please")+".";
      this.LoginLiveMultimedia.LoginButtonText = GetElementLocalization("Login_LoginButtonText", "Login");
      this.LoginLiveMultimedia.PasswordLabelText = GetElementLocalization("Login_PasswordLabelText", "Password")+":";
      this.LoginLiveMultimedia.PasswordRequiredErrorMessage = GetElementLocalization("Login_PasswordRequiredErrorMessage", "Password cannot be empty");
      this.LoginLiveMultimedia.RememberMeText = GetElementLocalization("Login_RememberMeText", "Remember me")+".";
      this.LoginLiveMultimedia.TitleText = GetElementLocalization("Login_TitleText", "Log in to") + " "+ "Live Multimedia Market";
      this.LoginLiveMultimedia.UserNameLabelText = GetElementLocalization("Login_UserNameLabelText", "Username") + " (E-Mail):";
      this.LoginLiveMultimedia.UserNameRequiredErrorMessage = GetElementLocalization("Login_UserNameRequiredErrorMessage", "The username cannot be empty");
      this.HyperLinkDemoMode.Text = GetElementLocalization("Login_DemoMode", "You can log into the demo mode");
    }

  public string GetElementLocalization(string ElementName, string DefaultElementValue)
  {
    string ElementValue;

    var Language = Session["Language"] as string;
    var ListLocalization = Application[Language] as List<LocalizationElement>;

    try
    {
      var LocalizationDictionaryItem = ListLocalization.Single(LocalizationElement => LocalizationElement.ElementName == ElementName);
      ElementValue = LocalizationDictionaryItem.ElementValue;
    }
    catch (Exception)
    {
      ElementValue = DefaultElementValue;
    }

    return ElementValue;
  }
}