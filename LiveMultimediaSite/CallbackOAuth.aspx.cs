using System;
using System.Collections.Generic;

using System.IO;
using System.Net;
using System.Web;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;

using LiveMultimediaSite.LiveMultimediaService;

namespace CallbackOAuth
{
  public static class OAuthConstants
  {
    #region OAuth 2.0 standard parameters
    public const string ClientID = "client_id";
    public const string ClientSecret = "client_secret";
    public const string Callback = "redirect_uri";
    public const string ClientState = "state";
    public const string Scope = "scope";
    public const string Code = "code";
    public const string AccessToken = "access_token";
    public const string AuthenticationToken = "authentication_token";
    public const string ExpiresIn = "expires_in";
    public const string RefreshToken = "refresh_token";
    public const string ResponseType = "response_type";
    public const string GrantType = "grant_type";
    public const string Error = "error";
    public const string ErrorDescription = "error_description";
    public const string Display = "display";
    #endregion
  }

  public class OAuthError
  {
    public string Code { get; set; }
    public string Description { get; set; }
  }

  public partial class Callback : System.Web.UI.Page
  {
    #region Define vars

    private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

    private string ServerAccountKey;

    private string UserToken;
    private string Id;
    private string RedirectUri;

    #endregion Define vars

    protected void Page_Init(object sender, EventArgs e)
    {
      ServerAccountKey = Application["ServerAccountKey"] as string;
      UserToken = LiveMultimediaLibrary.ConvertObjectToString(Session["UserToken"]);

      var aRedirectUri= Page.Request.Url.AbsoluteUri.Split(new char[] { '?' });
      RedirectUri = HttpUtility.UrlEncode(aRedirectUri[0]);

      try
      {
        Id = Session["Id"].ToString();
      }
      catch (Exception)
      {
        Id = null;
      }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
      #region Check UserToken
      if (UserToken == "")
      {
        Session["PrevPage"] = "Default.aspx";
        Response.Redirect("~/Default.aspx", false);
        HttpContext.Current.ApplicationInstance.CompleteRequest();
        return;
      }
      #endregion Check UserToken

      #region Sign In
      if (Convert.ToString(Session["ActionOAuth"]) == "SignIn")
      {
        OAuthSignIn();
        return;
      }
      #endregion Sign In

      #region Sign out before sign in
      if (Convert.ToString(Session["ActionOAuth"]) == "SignOutAndSignIn")
      {
        OAuthSignOut();
        return;
      }
      #endregion Sign out before sign in

      #region Check "code" from external service after sign in
      string code = Request.QueryString[OAuthConstants.Code];

      if (!LiveMultimediaLibrary.CheckGoodString(code))
      {
        Response.Redirect("~/Default.aspx", false);
        HttpContext.Current.ApplicationInstance.CompleteRequest();
        return;
      }
      #endregion Check "code" from external service after sign in

      MakeCallBackOAuth(code);

      Session["ActionOAuth"] = "";

      Response.Redirect("~/Default.aspx?id=" + Id, false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();
    }

    private void OAuthSignIn()
    {
      var OAuthUrlProvider = Session["OAuthUrlSignIn"].ToString() + "&redirect_uri=" + RedirectUri;
      Response.Redirect(OAuthUrlProvider, false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();

      Session["ActionOAuth"] = "GetCode";
    }

    private void OAuthSignOut()
    {
      var OAuthUrlProvider = Session["OAuthUrlSignOut"].ToString() + "&redirect_uri=" + RedirectUri;
      Response.Redirect(OAuthUrlProvider, false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();

      Session["ActionOAuth"] = "SignIn";
    }

    private async void MakeCallBackOAuth(string code)
    {
      try
      {
        //if (!string.IsNullOrEmpty(Request.QueryString[OAuthConstants.AccessToken]))
        //{
        //  // There is a token available already. It should be the token flow. Ignore it.
        //  return;
        //}
        OAuthError error; OAuthToken token;

        RequestAccessTokenByVerifier(code, out token, out error);

        if (token != null)
        {
          using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
          {
            var IsSuccess = await LiveMultimediaService.OAuthSetTokenAsync(ServerAccountKey, UserToken, Id, token);
          }
        }
      }
      catch (Exception)
      {
      }
    }

    private void RequestAccessTokenByVerifier(string verifier, out OAuthToken token, out OAuthError error)
    {
      string content;

      var aUrl = Session["OAuthUrlToken"].ToString().Split(new char[] { '?' });

      var OAuthUrlProvider = aUrl[0];
      content = aUrl[1] + "&redirect_uri=" + RedirectUri + "&code=" + verifier;

      RequestAccessToken(content, OAuthUrlProvider, out token, out error);
    }

    private void RequestAccessToken(string postContent, string OAuthUrlProvider, out OAuthToken token, out OAuthError error)
    {
      token = null;
      error = null;

      var request = WebRequest.Create(OAuthUrlProvider) as HttpWebRequest;
      request.Method = "POST";
      //request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
      request.ContentType = "application/x-www-form-urlencoded";

      try
      {
        var StartExpires = DateTime.Now;

        using (var writer = new StreamWriter(request.GetRequestStream()))
        {
          writer.Write(postContent);
        }
        var response = request.GetResponse() as HttpWebResponse;
        if (response != null)
        {
          var serializer = new DataContractJsonSerializer(typeof(OAuthToken));
          token = serializer.ReadObject(response.GetResponseStream()) as OAuthToken;
          if (token.expires_in == "0" || string.IsNullOrEmpty(token.expires_in))
          {
            token.expires_in = DateTime.MinValue.ToString();
          }
          else
          {
            var Expires = StartExpires.AddSeconds(Convert.ToDouble(token.expires_in));
            token.expires_in = Expires.ToString();
          }
        }
      }
      catch (WebException e)
      {
        HttpWebResponse response = e.Response as HttpWebResponse;
        if (response != null)
        {
          DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(OAuthError));
          error = serializer.ReadObject(response.GetResponseStream()) as OAuthError;
        }
      }
      catch (IOException)
      {
      }
      catch (Exception)
      {
      }

      //if (error == null)
      //{
      //  error = new OAuthError("request_failed", "Failed to retrieve user access token.");
      //}
    }

    private Dictionary<string, string> deserializeJson(string json)
    {
      var jss = new JavaScriptSerializer();
      var d = jss.Deserialize<Dictionary<string, string>>(json);
      return d;
    }
  }
}