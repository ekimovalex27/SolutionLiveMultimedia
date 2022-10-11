using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace LiveMultimediaSite
{
  public partial class LogoutOAuth1 : System.Web.UI.Page
  {

    #region Define vars

    private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

    string UserToken;
    int IdTypeMultimediaSource;

    #endregion Define vars
   
    protected void Page_Load(object sender, EventArgs e)
    {
      if (Session["UserToken"] == null) Session["UserToken"] = "";
      UserToken = Session["UserToken"].ToString();

      IdTypeMultimediaSource = Convert.ToInt32(Session["IdTypeMultimediaSource"]);
      bool IsSuccess = Int32.TryParse(Session["IdTypeMultimediaSource"].ToString(), out IdTypeMultimediaSource);
      if (!IsSuccess) IdTypeMultimediaSource = 1;

      string REDIRECT_URI = "http%3A%2F%2Fwww.live-mm.com%2FCallbackOAuth.aspx";

      string ClientId;
      string SCOPES;
      string OAuthUrl;
      //const string RedirectUri="http://www.live-mm.com/Default.aspx";
      string REDIRECT_URL = "http%3A%2F%2Fwww.live-mm.com%2FDefault.aspx";

      //switch (IdTypeMultimediaSource)
      //{
      //  case 3: //OneDrive
      //    // OAuth 2.0 http://msdn.microsoft.com/ru-ru/library/hh243649.aspx
      //    // http://msdn.microsoft.com/onedrive/
      //    // Using the REST API http://msdn.microsoft.com/library/dn659752.aspx
      //    // http://msdn.microsoft.com/ru-ru/library/windows/apps/hh243647
      //    ClientId = "***";
      //    SCOPES = "wl.signin,wl.skydrive,wl.offline_access";
      //    OAuthUrl = "https://login.live.com/oauth20_authorize.srf?client_id=" + ClientId + "&scope=" + SCOPES + "&response_type=code&redirect_uri=" + REDIRECT_URI;
      //    Response.Redirect(OAuthUrl, true);
      //    break;
      //  case 4: //Google Drive
      //    //Documentation https://developers.google.com/accounts/docs/OAuth2 
      //    ClientId = "***.apps.googleusercontent.com";
      //    SCOPES = "https://www.googleapis.com/auth/drive.readonly";
      //    OAuthUrl = "https://accounts.google.com/o/oauth2/auth?client_id=" + ClientId + "&scope=" + SCOPES + "&response_type=" + "code" + "&redirect_uri=" + REDIRECT_URI + "&access_type=offline" + "&approval_prompt=" + "force";
      //    Response.Redirect(OAuthUrl, true);
      //    break;
      //  case 5: //Facebook
      //    ClientId = "***";
      //    SCOPES = "audio,offline";
      //    OAuthUrl = "https://www.facebook.com/dialog/oauth?client_id=" + ClientId + "&redirect_uri=" + REDIRECT_URI + "&state=LMS";
      //    Response.Redirect(OAuthUrl, true);
      //    break;
      //  case 6: //VKontakte
      //    ClientId = "***";
      //    SCOPES = "audio,offline";
      //    //OAuthUrl = "https://oauth.vk.com/authorize?client_id=" + ClientId + "&scope=" + SCOPES + "&response_type=code&redirect_uri=" + REDIRECT_URI+"&revoke=1";
      //    //OAuthUrl = "https://oauth.vk.com/authorize?client_id=" + ClientId + "&scope=" + SCOPES + "&response_type=code&redirect_uri=" + REDIRECT_URI + "&revoke=0";
      //    OAuthUrl = "https://oauth.vk.com/authorize?client_id=" + ClientId + "&scope=" + SCOPES + "&redirect_uri=" + REDIRECT_URI + "&response_type=code" + "&v=5.21" + "&revoke=1";
      //    Response.Redirect(OAuthUrl, true);
      //    break;
      //  default:
      //    break;
      //}

    }
  }
}