using System;
using System.Web;
using System.Threading.Tasks;

using LiveMultimediaSite.LiveMultimediaService;

public partial class Demo : System.Web.UI.Page
{
  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

  protected void Page_Load(object sender, EventArgs e)
  {
    #region Define vars
    Task<Tuple<string, string>> returnValue;
    #endregion Define vars

    string Username = "demo@live-mm.com"; string Password = "non set here";
    string UserToken = "";

#if (DEBUG)
    Username = "***";
    Password = "***";
#endif

    try
    {
      using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
      {
        returnValue = LiveMultimediaService.RemoteLoginAsync(Application["ServerAccountKey"].ToString(), Username, Password);
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
    catch (Exception ex)
    {
      UserToken = "";
    }

    if (!string.IsNullOrEmpty(UserToken))
    {
      Session["UserToken"] = UserToken;
      Session["Username"] = Username;
      Response.Redirect("~/Default.aspx", false);      
    }
    else
    {
      Session["Username"] = "";
      Session["UserToken"] = "";
      Response.Redirect("~/Login.aspx", false);
    }

    HttpContext.Current.ApplicationInstance.CompleteRequest();
  }
}