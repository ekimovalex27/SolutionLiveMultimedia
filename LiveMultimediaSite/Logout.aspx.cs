using System;
using System.Web;
using System.Web.UI;

//using Microsoft.Live;

using LiveMultimediaSite.LiveMultimediaService;

public partial class Logout : Page
{
  #region Define vars
  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();
  #endregion Define vars

  protected void Page_Load(object sender, EventArgs e)
  {
    #region Define vars    
    string UserToken;
    #endregion Define vars

    #region Check UserToken
    if (Session["UserToken"] == null) Session["UserToken"] = "";
    UserToken = Session["UserToken"] as string;
    #endregion Check UserToken

    var Language = Session["Language"] as string;

    if (!string.IsNullOrEmpty(UserToken))
    {
      try
      {
        using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnValue = LiveMultimediaService.RemoteLogoutAsync(Application["ServerAccountKey"] as string, UserToken);
          returnValue.Wait();
          var returnMessage = returnValue.Result;
        }
      }
      catch (Exception)
      {
        // Nothing TO DO
      }

      //
      // Сделать выход для сессий к сетевым хранилищам
      //
    }

    Session.Abandon();

    Response.Redirect("~/Default.aspx" + "?language=" + Language, false);
    HttpContext.Current.ApplicationInstance.CompleteRequest();
  }
}