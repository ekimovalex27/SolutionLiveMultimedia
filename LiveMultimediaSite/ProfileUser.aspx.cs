using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Linq;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

using LiveMultimediaSite.LiveMultimediaService;

public partial class ProfileUser : System.Web.UI.Page
{

  #region Define vars

  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();
  private string ServerAccountKey;
  string UserToken;
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

  protected void Page_Init(object sender, EventArgs e)
  {
    ServerAccountKey = Application["ServerAccountKey"] as string;

    if (!Page.IsCallback)
    {
      UserToken = LiveMultimediaLibrary.ConvertObjectToString(Session["UserToken"]);
    }
  }

  protected void Page_Load(object sender, EventArgs e)
  {
    #region Check UserToken
    UserToken = LiveMultimediaLibrary.ConvertObjectToString(Session["UserToken"]);

    if (UserToken == "")
    {
      Response.Redirect("~/Login.aspx", false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();

      return;
    }
    #endregion Check UserToken

    SetLocalization();

    if (!Page.IsPostBack)
    {
      tbOldPassword.Text = "";
      tbNewPassword1.Text = "";
      tbNewPassword2.Text = "";

      tbOldPassword.Focus();

      cmdOk.Attributes.Add("onclick", "LockScreenOnProcessing('" + LockScreenTitle1 + "', '" + LockScreenTitle2 + "')");
    }
    LabelErrorUpdateUser.Visible = false;
    LabelOkUpdateUser.Visible = false;

    Response.AppendHeader("Pragma", "no-cache");
    Response.Cache.SetAllowResponseInBrowserHistory(false);
    Response.Cache.SetCacheability(HttpCacheability.NoCache);
    Response.Cache.SetNoStore();
  }

  protected void cmdOk_Click(object sender, EventArgs e)
  {
    #region Define vars
    string returnValue;
    #endregion Define vars

    Page.Validate();
    if (!Page.IsValid) return;

    #region Compare validator
    CompareValidator1.Validate();
    if (!CompareValidator1.IsValid) return;

    CompareValidator2.Validate();
    if (!CompareValidator2.IsValid) return;

    CompareValidatorPassword.Validate();
    if (!CompareValidatorPassword.IsValid) return;
    #endregion Compare validator

    #region Recaptcha
    var gRecaptchaResponse = Request.Form["g-recaptcha-response"];
    if (!LiveMultimediaLibrary.CheckGoodString(gRecaptchaResponse)) return;
    if (!VerifyCaptcha(gRecaptchaResponse)) return;
    #endregion Recaptcha

    var OldPassword = tbOldPassword.Text;
    var NewPassword1 = tbNewPassword1.Text;
    var NewPassword2 = tbNewPassword2.Text;

    using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
    {
      var returnTask = LiveMultimediaService.RemoteUpdateUserInfoAsync(ServerAccountKey, UserToken, OldPassword, NewPassword1);
      returnTask.Wait();
      returnValue = returnTask.Result;
    }

    if (!LiveMultimediaLibrary.CheckGoodString(returnValue))
    {
      LabelOkUpdateUser.Visible = true;
    }
    else
    {
      LabelErrorUpdateUser.Visible = true;
    }
  }

  protected void cmdCancel_Click(object sender, EventArgs e)
  {
    Response.Redirect("~/Default.aspx", false);
    HttpContext.Current.ApplicationInstance.CompleteRequest();
  }

  private void SetLocalization()
  {
    LabelTitle.Text = GetElementLocalization("UpdateUserInfo_Title", LabelTitle.Text);
    LabelChangePassword.Text = GetElementLocalization("UpdateUserInfo_TitleChangePassword", LabelChangePassword.Text);
    OldPassword.Text = GetElementLocalization("UpdateUserInfo_LabelOldPassword", OldPassword.Text);
    NewPassword1.Text = GetElementLocalization("UpdateUserInfo_LabelNewPassword1", NewPassword1.Text);
    NewPassword2.Text = GetElementLocalization("UpdateUserInfo_LabelNewPassword2", NewPassword2.Text);
    CompareValidator1.ErrorMessage = GetElementLocalization("UpdateUserInfo_CompareOldPassword_with_1_ErrorMessage", CompareValidator1.ErrorMessage);
    CompareValidator2.ErrorMessage = GetElementLocalization("UpdateUserInfo_CompareOldPassword_with_2_ErrorMessage", CompareValidator2.ErrorMessage);
    CompareValidatorPassword.ErrorMessage = GetElementLocalization("UpdateUserInfo_ComparePassword_ErrorMessage", CompareValidatorPassword.ErrorMessage);
    LabelErrorUpdateUser.Text = GetElementLocalization("UpdateUserInfo_LabelErrorUpdateUser", LabelErrorUpdateUser.Text);
    LabelOkUpdateUser.Text = GetElementLocalization("UpdateUserInfo_LabelOkUpdateUser", LabelOkUpdateUser.Text);
    cmdOk.Text = GetElementLocalization("UpdateUserInfo_cmdOk", cmdOk.Text);
    cmdCancel.Text = GetElementLocalization("UpdateUserInfo_cmdCancel", cmdCancel.Text);
    LockScreenTitle1 = GetElementLocalization("UpdateUserInfo_LockScreenTitle1", "Updating information about user");
    LockScreenTitle2 = GetElementLocalization("UpdateUserInfo_LockScreenTitle2", "Please wait");
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

    bool IsSuccess = false;

    try
    {
      var postContent = String.Format("{0}?secret={1}&response={2}", CaptchaURL, CaptchaSecret, responseTokenReCAPTCHA);
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
