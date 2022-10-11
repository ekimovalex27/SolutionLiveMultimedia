using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

using LiveMultimediaSite.LiveMultimediaService;

public partial class RegisterUser : System.Web.UI.Page
{
  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

  #region Define vars
  string LockScreenTitle1, LockScreenTitle2;
  #endregion Define vars

  protected override void OnPreRender(EventArgs e)
  {
    base.OnPreRender(e);

    // reCAPTCHA API version 2.0
    // https://developers.google.com/recaptcha/intro

    var head = Master.FindControl("head");
    var Language = Session["Language"] as string;
    head.Controls.Add(new LiteralControl(string.Format("<script src='https://www.google.com/recaptcha/api.js?hl={0}'></script>", Language)));
  }

  protected void Page_Load(object sender, EventArgs e)
  {
    SetLocalization();

    if (!Page.IsPostBack)
    {
      tbFirstName.Text = "";
      tbLastName.Text = "";
      tbUsername.Text = "";
      tbPassword.Text = "";
      tbPasswordReType.Text = "";

      tbFirstName.Focus();

      cmdOk.Attributes.Add("onclick", "LockScreenOnProcessing('" + LockScreenTitle1 + "', '" + LockScreenTitle2 + "')");
    }
    LabelErrorRegister.Visible = false;

    Response.AppendHeader("Pragma", "no-cache");
    Response.Cache.SetAllowResponseInBrowserHistory(false);
    Response.Cache.SetCacheability(HttpCacheability.NoCache);
    Response.Cache.SetNoStore();

//#if DEBUG
//    tbFirstName.Text = "Тест";
//    tbLastName.Text = "Тестов";
//    tbUsername.Text = "qqq@live-mm.com";
//    tbPassword.Text = "11";
//    tbPasswordReType.Text = "11";
//#endif
  }

  protected void cmdOk_Click(object sender, EventArgs e)
  {
    #region Define vars
    Task<Tuple<string, string>> returnValue;
    #endregion Define vars

    Page.Validate();
    if (!Page.IsValid) return;

    string UserToken = "";
    string FirstName; string LastName; string Username; string Password; int IdTariffPlan = 1;

    ComparePasswordValidator.Validate();
    if (!ComparePasswordValidator.IsValid) return;

    var gRecaptchaResponse = Request.Form["g-recaptcha-response"];
    if (!LiveMultimediaLibrary.CheckGoodString(gRecaptchaResponse)) return;
    if (!VerifyCaptcha(gRecaptchaResponse)) return;

    FirstName = tbFirstName.Text.Trim();
    LastName = tbLastName.Text.Trim();
    Username = tbUsername.Text.Trim();
    Password = tbPassword.Text;

    try
    {
      var Language = Session["Language"] as string;
      using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
      {
        returnValue = LiveMultimediaService.RemoteRegisterNewUserAsync(Application["ServerAccountKey"].ToString(), FirstName, LastName, Username, Password, IdTariffPlan, Language);
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
      Username = "";
    }
    finally
    {
      Session["Username"] = Username;
      Session["UserToken"] = UserToken;
    }

    if (LiveMultimediaLibrary.CheckGoodString(Username) && LiveMultimediaLibrary.CheckGoodString(UserToken))
    {
      Response.Redirect("~/Default.aspx", false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();
    }
    else
    {
      LabelErrorRegister.Visible = true;
      //this.lblErrorRegisterNewUser.Text = Message; Возвращается из хранимой процедуры в DataLayer не то.
    }
  }

  protected void cmdCancel_Click(object sender, EventArgs e)
    {
      Response.Redirect("~/Default.aspx", false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();
    }

  private void SetLocalization()
  {
    LabelTitle.Text = GetElementLocalization("NewUser_Title", "Register in") + " " + "Live Multimedia Market";
    LabelFirstName.Text = GetElementLocalization("NewUser_LabelFirstName", "First Name");
    LabelLastName.Text = GetElementLocalization("NewUser_LabelLastName", "Last Name");
    LabelEMail.Text = "E-Mail";
    LabelPassword.Text = GetElementLocalization("NewUser_LabelPassword", "Password");
    LabelPasswordReType.Text = GetElementLocalization("NewUser_LabelPasswordReType", "Password re-type");
    LabelErrorRegister.Text = GetElementLocalization("NewUser_LabelErrorRegister", "Error register new user");
    cmdOk.Text = GetElementLocalization("NewUser_cmdOk", "Register");
    cmdCancel.Text = GetElementLocalization("NewUser_cmdCancel", "Cancel");
    ComparePasswordValidator.ErrorMessage = GetElementLocalization("NewUser_ComparePassword_ErrorMessage", "Passwords do not match");
    LockScreenTitle1 = GetElementLocalization("NewUser_LockScreenTitle1", "New user registration");
    LockScreenTitle2 = GetElementLocalization("NewUser_LockScreenTitle2", "Please wait");
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

  private class ResponseReCAPTCHA
  {
    public string success { get; set; }
    public string errorcodes { get; set; }
  }

  private bool VerifyCaptcha(string responseTokenReCAPTCHA)
  {
    string CaptchaURL = "https://www.google.com/recaptcha/api/siteverify";
    string CaptchaSecret = "6Le0FhATAAAAABBCQaythc5r8GnFjaiYy1V-Nyot";

    bool IsSuccess=false;

    try
    {
      var postContent = string.Format("{0}?secret={1}&response={2}", CaptchaURL, CaptchaSecret, responseTokenReCAPTCHA);
      var CaptchaVerifyRequest = WebRequest.Create(postContent) as HttpWebRequest;
      using (var CaptchaVerifyResponse = CaptchaVerifyRequest.GetResponse())
      {
        using (var readStream = new StreamReader(CaptchaVerifyResponse.GetResponseStream()))
        {
          var js = new JavaScriptSerializer();
          var responseReCAPTCHA = js.Deserialize<ResponseReCAPTCHA>(readStream.ReadToEnd());
          bool.TryParse(responseReCAPTCHA.success, out IsSuccess);
        }
      }
    }
    catch (Exception)
    {
      IsSuccess = false;
    }

    return IsSuccess;
  }
}