<%@ Application Language="C#" %>

<script RunAt="server">

  void Application_Start(object sender, EventArgs e)
  {
    // Код, выполняемый при запуске приложения

    //For working new validation in .net 4.5 (ValidationSettings:UnobtrusiveValidationMode)
    ScriptManager.ScriptResourceMapping.AddDefinition("jquery", new ScriptResourceDefinition
    {
      Path = "~/scripts/jquery-1.4.1.min.js",
      DebugPath = "~/scripts/jquery-1.4.1.js",
      CdnPath = "http://ajax.microsoft.com/ajax/jQuery/jquery-1.4.1.min.js",
      CdnDebugPath = "http://ajax.microsoft.com/ajax/jQuery/jquery-1.4.1.js"
    }
    );

    Application["ServerAccountKey"] = "***";

    Application["DefaultLanguage"] = "en";
    Application["DefaulNativeName"] = "English";
  }

  void Application_End(object sender, EventArgs e)
  {
    //  Код, выполняемый при завершении работы приложения
  }

  void Application_Error(object sender, EventArgs e)
  {
    var SiteException = Server.GetLastError();
    var context = HttpContext.Current;
    if (context != null && context.Session != null)
    {
      Session["ErrorMessage"] = SiteException;
    }

    Server.ClearError();
    Response.Redirect("~/oops.aspx", true);
    //HttpContext.Current.ApplicationInstance.CompleteRequest();      
  }

  private void ClearVars()
  {
    Session["UserToken"] = "";
    Session["Username"] = "";
    Session["IdTypeMultimediaSource"] = -1;
    Session["isListSongLoaded"] = false;
    Session["PrevPage"] = "";
    Session["MultimediaFileGUID"] = "";
    Session["ListAlbum"] = null;
    Session["ListIdAlbum"] = null;
    Session["IsViewMultimediaFolder"] = true;
    Session["SelectedMultimediaFolder"] = null;
    Session["SelectedAlbum"] = "";
    Session["PlayingSong"] = "";
    Session["ActionOAuth"] = "";
    Session["fromPage"] = "Default.aspx";
    Session["ErrorMessage"] = "";
    Session["ScreenResolution"] = null;
    Session["ScreenPixelsWidth"] = null;
    //Session["TitleSource"] = "Albums";
    //Session["IsWriteLogScreenPixelsWidth"] = false; Не помню, зачем это надо
    Session["IsMobileDevice"] = false;
  }

  void LoadSessionLanguage()
  {
    string Language;

    #region Get Language
    try
    {
      //First, get Language from query string (for example, after - Logout)
      Language = Request.QueryString["Language"] as string;
      if (!LiveMultimediaLibrary.CheckGoodString(Language))
      {
        //Second, get language from user agent's browser, if query string is empty
        var aNewLanguageWithQuality = Request.UserLanguages[0].Split(new Char[] { ';' });
        var aNewLanguage = aNewLanguageWithQuality[0].Split(new Char[] { '-' });
        Language = aNewLanguage[0].Trim().ToLower();

        //Third, if get Language is fault from First and Second, beacause Language will default
        if (!LiveMultimediaLibrary.CheckGoodString(Language)) throw new ArgumentNullException("Language", "Language is not correct");
      }
    }
    catch (Exception)
    {
      Language = Application["DefaultLanguage"] as string;
    }
    #endregion Get Language

    Session["Language"] = Language;
  }

  void Session_Start(object sender, EventArgs e)
  {
    // Код, выполняемый при запуске нового сеанса

    Session.Timeout = 180; //Default is 20 min. Now is 3h

    ClearVars();

    Session["IsMobileDevice"] = Request.Browser.IsMobileDevice;

    LoadSessionLanguage();
  }

  void Session_End(object sender, EventArgs e)
  {
    // Код, выполняемый при запуске приложения. 
    // Примечание: Событие Session_End вызывается только в том случае, если для режима sessionstate
    // задано значение InProc в файле Web.config. Если для режима сеанса задано значение StateServer 
    // или SQLServer, событие не порождается.

    //System.Diagnostics.Debug.WriteLine("{0} Global.asax Session_End: UserToken={1}", DateTime.Now, (Session["UserToken"] ?? ""));

    //if (LiveMultimediaLibrary.CheckGoodString(LiveMultimediaLibrary.ConvertObjectToString(Session["UserToken"])))
    //{
    //    var LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

    //    using (var LiveMultimediaService = new LiveMultimediaSite.LiveMultimediaService.LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
    //    {
    //        var returnValue = LiveMultimediaService.RemoteLogout(Application["ServerAccountKey"].ToString(), Session["UserToken"].ToString());
    //    }          
    //}

    ClearVars();
  }

  //void Application_Error(Object sender, EventArgs e)
  //{
  //  if (!System.Diagnostics.EventLog.SourceExists
  //          ("ASPNETApplication"))
  //  {
  //    System.Diagnostics.EventLog.CreateEventSource
  //       ("ASPNETApplication", "Application");
  //  }
  //  System.Diagnostics.EventLog.WriteEntry
  //      ("ASPNETApplication",
  //      Server.GetLastError().Message);
  //}           

  /// <summary>
  //void Application_Error(object sender, EventArgs e)
  //{
  //  // Code that runs when an unhandled error occurs

  //  // Get the exception object.
  //  Exception exc = Server.GetLastError();

  //  // Handle HTTP errors
  //  if (exc.GetType() == typeof(HttpException))
  //  {
  //    // The Complete Error Handling Example generates
  //    // some errors using URLs with "NoCatch" in them;
  //    // ignore these here to simulate what would happen
  //    // if a global.asax handler were not implemented.
  //    if (exc.Message.Contains("NoCatch") || exc.Message.Contains("maxUrlLength"))
  //      return;

  //    //Redirect HTTP errors to HttpError page
  //    Server.Transfer("HttpErrorPage.aspx");
  //  }

  //  // For other kinds of errors give the user some information
  //  // but stay on the default page
  //  Response.Write("<h2>Global Page Error</h2>\n");
  //  Response.Write(
  //      "<p>" + exc.Message + "</p>\n");
  //  Response.Write("Return to the <a href='Default.aspx'>" +
  //      "Default Page</a>\n");

  //  // Log the exception and notify system operators
  //  ExceptionUtility.LogException(exc, "DefaultPage");
  //  ExceptionUtility.NotifySystemOps(exc);

  //  // Clear the error from the server
  //  Server.ClearError();
  //}
  /// 
  /// </summary>

</script>
