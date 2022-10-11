using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using LiveMultimediaSite.LiveMultimediaService;

public partial class MasterPage : System.Web.UI.MasterPage
{
  #region Define vars
  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

  const double MaximumResolutionMobile = 1024;

  private string ServerAccountKey;
  private bool IsMobileDevice;

  //LanguageInfo LanguageInfoItem;  
  private HyperLink HyperLinkItemMenu;

  private string BottomSupport, BottomPrivacy, BottomTermsOfUse, BottomTrademarks, BottomHelp;

  #endregion Define vars

  public int SessionLengthMinutes
  {
    get { return Session.Timeout; }
  }

  public string SessionExpireDestinationUrl
  {
    get { return "/Logout.aspx"; }
  }

  protected override void OnPreRender(EventArgs e)
  {
    base.OnPreRender(e);

    if (!Page.IsPostBack && !Page.IsCallback)
    {
      if (LiveMultimediaLibrary.CheckGoodString(Session["UserToken"].ToString()))
      {
        head.Controls.Add(new LiteralControl(
            string.Format("<script>setTimeout(\"location.href='{0}'\", {1});</script>",
            SessionExpireDestinationUrl, SessionLengthMinutes * 60 * 1000)));
      }

      //this.head.Controls.Add(new LiteralControl(
      //    string.Format("<meta http-equiv='refresh' content='{0};url={1}'/>",
      //    SessionLengthMinutes * 60, SessionExpireDestinationUrl)));
    }
  }

  protected void LoadLocalization()
  {
    var Language = Session["Language"] as string;

    try
    {
      if (Application[Language] == null)
      {
        using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnValue = LiveMultimediaService.RemoteGetLocalizationAsync(ServerAccountKey, Language);
          returnValue.Wait();

          if (LiveMultimediaLibrary.CheckGoodString(returnValue.Result.Item3)) throw new ApplicationException(returnValue.Result.Item3);

          Application[Language] = returnValue.Result.Item1.ToList();
          Application["NativeName" + Language] = returnValue.Result.Item2;
        }
      }
    }
    catch (Exception ex)
    {
      Language = Application["DefaultLanguage"] as string;

      Session["Language"] = Language;
      Application["NativeName" + Language] = Application["DefaultNativeName"];
      Application[Language] = null;
    }
  }

  protected void Page_Init(object sender, EventArgs e)
  {
    Unit ScreenPixelsWidth, ScreenPixelsHeight;

    ServerAccountKey = Application["ServerAccountKey"] as string;

    if (!Page.IsCallback)
    {
      #region Define ScreenResolution
      //#if DEBUG
      //    Session["ScreenResolution"] = "1600x900";
      //    Session["ScreenPixelsWidth"] = new Unit(Session["ScreenResolution"].ToString().Split(new Char[] { 'x' })[0]);
      //    Session["ScreenPixelsHeight"] = new Unit(Session["ScreenResolution"].ToString().Split(new Char[] { 'x' })[1]);
      //#endif

      if (Session["ScreenResolution"] == null)
      {
        Session["PrevPageForScreenResolution"] = "~" + Page.Request.Url.AbsolutePath;
        Server.Transfer("~/detectscreen.aspx", false);
      }
      else
      {
        IsMobileDevice = Convert.ToBoolean(Session["IsMobileDevice"]);
        if (IsMobileDevice)
        {
          ScreenPixelsWidth = (Unit)Session["ScreenPixelsWidth"];
          ScreenPixelsHeight = (Unit)Session["ScreenPixelsHeight"];

          if (ScreenPixelsWidth.Value > ScreenPixelsHeight.Value && ScreenPixelsWidth.Value >= MaximumResolutionMobile)
          {
            Session["IsMobileDevice"] = false;
            IsMobileDevice = Convert.ToBoolean(Session["IsMobileDevice"]);
          }
        }
      }

      //Не помню, зачем это надо
      //if ((bool)Session["IsWriteLogScreenPixelsWidth"] == false)
      //{
      //  ScreenPixelsWidth = (Unit)Session["ScreenPixelsWidth"];
      //  ScreenPixelsHeight = (Unit)Session["ScreenPixelsHeight"];
      //  Session["IsWriteLogScreenPixelsWidth"] = true;

      //}
      #endregion Define ScreenResolution

      LoadLocalization();
    }
  }

  protected void Page_Load(object sender, EventArgs e)
  {
    #region Define vars
    const string SkipFromLanguage = "/language.aspx";
    string UserToken;
    #endregion Define vars

    if (!Page.IsCallback)
    {
      SetLocalization();

      if (Page.Request.Url.AbsolutePath.ToLower() != SkipFromLanguage) Session["fromPage"] = "~" + Page.Request.RawUrl;
      
      if (Session["UserToken"] == null) Session["UserToken"] = "";
      UserToken = Session["UserToken"].ToString();

      if (!LiveMultimediaLibrary.CheckGoodString(UserToken))      
      {
        LoadMenuStart();
      }
      else
      {
        LoadMenuUser();
      }

      ShowBottom();
    }
  }

  protected void LoadMenuStart()
  {
    HyperLinkItemMenu = new HyperLink();
    HyperLinkItemMenu.Text = GetElementLocalization("MasterPage_Demo_Text", "Demo");
    HyperLinkItemMenu.NavigateUrl = "~/" + "Demo.aspx";
    HyperLinkItemMenu.ToolTip = GetElementLocalization("MasterPage_Demo_ToolTip", "Free Demo");
    HyperLinkItemMenu.CssClass = "MasterTitleRight";
    PanelTopMenu.Controls.Add(HyperLinkItemMenu);

    HyperLinkItemMenu = new HyperLink();
    HyperLinkItemMenu.Text = GetElementLocalization("MasterPage_Download_Text", "Download");
    HyperLinkItemMenu.NavigateUrl = "~/" + "Download.aspx";
    HyperLinkItemMenu.ToolTip = GetElementLocalization("MasterPage_Download_ToolTip", "Download free multimedia server");
    HyperLinkItemMenu.CssClass = "MasterTitleRight";
    PanelTopMenu.Controls.Add(HyperLinkItemMenu);

    HyperLinkItemMenu = new HyperLink();
    HyperLinkItemMenu.Text = Application["NativeName" + Session["Language"] as string] as string;
    HyperLinkItemMenu.NavigateUrl = "~/" + "Language.aspx";
    HyperLinkItemMenu.ToolTip = GetElementLocalization("MasterPage_Language_ToolTip", "Select a language");
    HyperLinkItemMenu.CssClass = "MasterTitleRight";
    PanelTopMenu.Controls.Add(HyperLinkItemMenu);

    HyperLinkItemMenu = new HyperLink();
    HyperLinkItemMenu.Text = GetElementLocalization("MasterPage_SignIn_Text", "Sign in");
    HyperLinkItemMenu.NavigateUrl = "~/" + "Login.aspx";
    HyperLinkItemMenu.ToolTip = GetElementLocalization("MasterPage_SignIn_ToolTip", "Sign in") + " " + "Live Multimedia Market";
    HyperLinkItemMenu.CssClass = "MasterTitleRight";
    PanelTopMenu.Controls.Add(HyperLinkItemMenu);
  }

  protected void LoadMenuUser()
  {
    HyperLinkItemMenu = new HyperLink();
    HyperLinkItemMenu.Text = GetElementLocalization("MasterPage_Download_Text", "Download");
    HyperLinkItemMenu.NavigateUrl = "~/" + "Download.aspx";
    HyperLinkItemMenu.ToolTip = GetElementLocalization("MasterPage_Download_ToolTip", "Download free multimedia server");
    HyperLinkItemMenu.CssClass = "MasterTitleRight";
    this.PanelTopMenu.Controls.Add(HyperLinkItemMenu);

    HyperLinkItemMenu = new HyperLink();
    HyperLinkItemMenu.Text = Application["NativeName" + Session["Language"] as string] as string;
    HyperLinkItemMenu.NavigateUrl = "~/" + "Language.aspx";
    HyperLinkItemMenu.ToolTip = GetElementLocalization("MasterPage_Language_ToolTip", "Select a language");
    HyperLinkItemMenu.CssClass = "MasterTitleRight";
    this.PanelTopMenu.Controls.Add(HyperLinkItemMenu);

    HyperLinkItemMenu = new HyperLink();
    HyperLinkItemMenu.Text = Session["Username"].ToString();
    HyperLinkItemMenu.NavigateUrl = "~/" + "ProfileUser.aspx";
    HyperLinkItemMenu.ToolTip = GetElementLocalization("MasterPage_Username_ToolTip", "Username");
    HyperLinkItemMenu.CssClass = "MasterTitleRightUsername";
    this.PanelTopMenu.Controls.Add(HyperLinkItemMenu);

    HyperLinkItemMenu = new HyperLink();
    HyperLinkItemMenu.Text = GetElementLocalization("MasterPage_SignOut_Text", "Sign out");
    HyperLinkItemMenu.NavigateUrl = "~/" + "Logout.aspx";
    HyperLinkItemMenu.ToolTip = GetElementLocalization("MasterPage_SignOut_ToolTip", "Sign out from") + " " + "Live Multimedia Market";
    HyperLinkItemMenu.CssClass = "MasterTitleRight";
    this.PanelTopMenu.Controls.Add(HyperLinkItemMenu);
  }

  private void ShowBottom()
  {
    if (!IsMobileDevice)
    {
      SetBottom();      
    }
    else
    {
      SetBottomMobile();
    }
  }

  private void SetBottom()
  {
    Panel BottomContainer; Label BottomLabel; HyperLink BottomLink;

    var placeHolderBottom = FindControl("PlaceHolderBottom") as PlaceHolder;

    BottomContainer = new Panel();
    BottomContainer.Attributes.Add("class", "MasterBottom MasterBottomFont");

    BottomLabel = new Label(); BottomLabel.Text = BottomSupport;
    BottomLabel.Attributes.Add("class", "MasterBottomFirstChild");
    BottomContainer.Controls.Add(BottomLabel);

    BottomLink = new HyperLink(); BottomLink.Text = "support@live-mm.com"; BottomLink.NavigateUrl = "mailto:support@live-mm.com";
    BottomLink.Attributes.Add("class", "MasterBottomSupportEmail");
    BottomContainer.Controls.Add(BottomLink);

    BottomLink = new HyperLink(); BottomLink.Text = BottomPrivacy; BottomLink.NavigateUrl = "~/Privacy.aspx";
    if (BottomLink.NavigateUrl != "")
    {
      BottomLink.Attributes.Add("class", "MasterBottomCommon");
      BottomContainer.Controls.Add(BottomLink);
    }

    BottomLink = new HyperLink(); BottomLink.Text = BottomTermsOfUse; BottomLink.NavigateUrl = "";
    if (BottomLink.NavigateUrl != "")
    {
      BottomLink.Attributes.Add("class", "MasterBottomCommon");
      BottomContainer.Controls.Add(BottomLink);
    }

    BottomLink = new HyperLink(); BottomLink.Text = BottomTrademarks; BottomLink.NavigateUrl = "";
    if (BottomLink.NavigateUrl != "")
    {
      BottomLink.Attributes.Add("class", "MasterBottomCommon");
      BottomContainer.Controls.Add(BottomLink);
    }

    BottomLink = new HyperLink(); BottomLink.Text = BottomHelp; BottomLink.NavigateUrl = "~/About.aspx";
    if (BottomLink.NavigateUrl != "")
    {
      BottomLink.Attributes.Add("class", "MasterBottomCommon");
      BottomContainer.Controls.Add(BottomLink);
    }

    BottomLabel = new Label(); BottomLabel.Text = "&copy;" + DateTime.Now.Year.ToString();
    BottomLabel.Attributes.Add("class", "MasterBottomCopyright");
    BottomContainer.Controls.Add(BottomLabel);

    BottomLink = new HyperLink(); BottomLink.Text = "Jet Software And Service Ltd."; BottomLink.NavigateUrl = "http://www.jetsas.com/"; BottomLink.Target = "_blank";
    BottomLink.Attributes.Add("class", "MasterBottomCompany");
    BottomContainer.Controls.Add(BottomLink);

    placeHolderBottom.Controls.Add(BottomContainer);
  }

  private void SetBottomMobile()
  {
    Panel BottomContainer; Label BottomLabel; HyperLink BottomLink;

    var placeHolderBottom = FindControl("PlaceHolderBottom") as PlaceHolder;

    BottomContainer = new Panel(); BottomContainer.Attributes.Add("class", "MasterBottom MasterBottomFontMobile");

    BottomLabel = new Label(); BottomLabel.Text = BottomSupport;
    BottomLabel.Attributes.Add("class", "MasterBottomFirstChild");
    BottomContainer.Controls.Add(BottomLabel);

    BottomLink = new HyperLink(); BottomLink.Text = "support@live-mm.com"; BottomLink.NavigateUrl = "mailto:support@live-mm.com";
    BottomLink.Attributes.Add("class", "MasterBottomSupportEmail");
    BottomContainer.Controls.Add(BottomLink);

    placeHolderBottom.Controls.Add(BottomContainer);

    BottomContainer = new Panel(); BottomContainer.Attributes.Add("class", "MasterBottom MasterBottomFontMobile");

    BottomLink = new HyperLink(); BottomLink.Text = BottomHelp; BottomLink.NavigateUrl = "~/About.aspx";
    if (BottomLink.NavigateUrl != "")
    {
      BottomLink.Attributes.Add("class", "MasterBottomCommon");
      BottomContainer.Controls.Add(BottomLink);
    }

    placeHolderBottom.Controls.Add(BottomContainer);

    BottomContainer = new Panel(); BottomContainer.Attributes.Add("class", "MasterBottom MasterBottomFontMobile");

    BottomLink = new HyperLink(); BottomLink.Text = BottomPrivacy; BottomLink.NavigateUrl = "~/Privacy.aspx";
    if (BottomLink.NavigateUrl != "")
    {
      BottomLink.Attributes.Add("class", "MasterBottomCommon");
      BottomContainer.Controls.Add(BottomLink);
    }

    placeHolderBottom.Controls.Add(BottomContainer);

    BottomContainer = new Panel(); BottomContainer.Attributes.Add("class", "MasterBottom MasterBottomFontMobile");

    BottomLink = new HyperLink(); BottomLink.Text = BottomTermsOfUse; BottomLink.NavigateUrl = "";
    if (BottomLink.NavigateUrl != "")
    {
      BottomLink.Attributes.Add("class", "MasterBottomCommon");
      BottomContainer.Controls.Add(BottomLink);
    }

    placeHolderBottom.Controls.Add(BottomContainer);

    BottomContainer = new Panel(); BottomContainer.Attributes.Add("class", "MasterBottom MasterBottomFontMobile");

    BottomLink = new HyperLink(); BottomLink.Text = BottomTrademarks; BottomLink.NavigateUrl = "";
    if (BottomLink.NavigateUrl != "")
    {
      BottomLink.Attributes.Add("class", "MasterBottomCommon");
      BottomContainer.Controls.Add(BottomLink);
    }

    placeHolderBottom.Controls.Add(BottomContainer);

    BottomContainer = new Panel(); BottomContainer.Attributes.Add("class", "MasterBottom MasterBottomFontMobile");

    BottomLabel = new Label(); BottomLabel.Text = "&copy;"+ DateTime.Now.Year.ToString();
    BottomLabel.Attributes.Add("class", "MasterBottomCopyrightMobile");
    BottomContainer.Controls.Add(BottomLabel);

    BottomLink = new HyperLink(); BottomLink.Text = "Jet Software And Service Ltd."; BottomLink.NavigateUrl = "http://www.jetsas.com/"; BottomLink.Target = "_blank";
    BottomLink.Attributes.Add("class", "MasterBottomCompanyMobile");
    BottomContainer.Controls.Add(BottomLink);

    placeHolderBottom.Controls.Add(BottomContainer);
  }  

  private void SetLocalization()
  {
    BottomSupport = GetElementLocalization("MasterPage_Bottom_Support", "Support") + " ";
    BottomPrivacy = GetElementLocalization("MasterPage_Bottom_Privacy", "Privacy");
    BottomTermsOfUse = GetElementLocalization("MasterPage_Bottom_TermsOfUse", "Terms of use");
    BottomTrademarks = GetElementLocalization("MasterPage_Bottom_Trademarks", "Trademarks");
    BottomHelp = GetElementLocalization("MasterPage_Bottom_Help", "Help");
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

//protected void LoadMenuStart()
//{
//  this.HyperLinkDemo.Visible = true;
//  this.HyperLinkLogout.Visible = false;

//  LoadMenuStart_old();
//}

//protected void LoadMenuUser()
//{
//  this.HyperLinkDemo.Visible = false;
//  this.HyperLinkLogout.Visible = true;

//  LoadMenuUser_old();
//}

//private HyperLink GetHyperLinkMenu(string Text, string NavigateUrl, string ToolTip)
//{
//  HyperLink HyperLinkMenu;
//  HyperLinkMenu = new HyperLink();
//  HyperLinkMenu.Text = Text;
//  HyperLinkMenu.NavigateUrl = NavigateUrl;
//  HyperLinkMenu.ToolTip = ToolTip;

//  return HyperLinkMenu;
//}

//protected void LoadMenuStart()
//{
//  //TableRow TableMenuRow = new TableRow();
//  //TableCell TableMenuCell;      

//  //TableMenuCell = new TableCell();
//  //TableMenuCell.BorderStyle = BorderStyle.Solid;
//  //TableMenuCell.Controls.Add(GetHyperLinkMenu("Home", SitePath + "Default.aspx", "Main page"));
//  //TableMenuRow.Cells.Add(TableMenuCell);

//  //TableMenuCell = new TableCell();
//  //TableMenuCell.Controls.Add(GetHyperLinkMenu("Demo", SitePath + "Demo.aspx", "Free Demo Multimedia"));
//  //TableMenuCell.BorderStyle = BorderStyle.Solid;
//  //TableMenuRow.Cells.Add(TableMenuCell);

//  //TableMenuCell = new TableCell();
//  //TableMenuCell.Text = "Media";
//  //TableMenuCell.BorderStyle = BorderStyle.Solid;
//  //TableMenuRow.Cells.Add(TableMenuCell);

//  //TableMenuCell = new TableCell();
//  //TableMenuCell.Text = "Download";
//  //TableMenuCell.BorderStyle = BorderStyle.Solid;
//  //TableMenuRow.Cells.Add(TableMenuCell);

//  //TableMenuCell = new TableCell();
//  //TableMenuCell.Text = "About";
//  //TableMenuCell.BorderStyle = BorderStyle.Solid;
//  //TableMenuRow.Cells.Add(TableMenuCell);

//  //this.TableMenu.Rows.Add(TableMenuRow);
//  //TableMenu.BorderStyle = BorderStyle.Solid;

//  System.Web.UI.WebControls.Style StyleRemovePlaylist = new System.Web.UI.WebControls.Style();
//  StyleRemovePlaylist = new System.Web.UI.WebControls.Style();
//  StyleRemovePlaylist.CssClass = "ul";
//  MainMenu.ApplyStyle(StyleRemovePlaylist);

//  System.Web.UI.WebControls.MenuItem MainMenuItem;

//  MainMenuItem = new MenuItem();
//  MainMenuItem.Text = "Home";
//  MainMenuItem.NavigateUrl = SitePath + "Default.aspx";
//  MainMenuItem.ToolTip = "Main page";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  MainMenuItem.Text = "Demo";
//  MainMenuItem.NavigateUrl = SitePath + "Demo.aspx";
//  MainMenuItem.ToolTip = "Free Demo Multimedia";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  MainMenuItem.Text = "Media";
//  MainMenuItem.NavigateUrl = SitePath + "LiveMultimedia.aspx";
//  MainMenuItem.ToolTip = "Enter to your multimedia library";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  MainMenuItem.Text = "Download";
//  MainMenuItem.NavigateUrl = SitePath + "Download.aspx";
//  MainMenuItem.ToolTip = "Download the application for listen your library";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  MainMenuItem.Text = "About";
//  MainMenuItem.NavigateUrl = SitePath + "About.aspx";
//  MainMenuItem.ToolTip = "About the Live Multimedia Market";
//  MainMenu.Items.Add(MainMenuItem);      
//}

//protected void LoadMenuStart()
//{
//  System.Web.UI.WebControls.MenuItem MainMenuItem;

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/Home.png";
//  MainMenuItem.Text = "Main page";
//  MainMenuItem.NavigateUrl = SitePath + "Default.aspx" + Language;
//  MainMenuItem.ToolTip = "Main page";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/Demo.png";
//  MainMenuItem.NavigateUrl = SitePath + "Demo.aspx" + Language;
//  MainMenuItem.ToolTip = "Free Demo Multimedia";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/Library.png";
//  MainMenuItem.NavigateUrl = SitePath + "LiveMultimedia.aspx" + Language;
//  MainMenuItem.ToolTip = "Enter to your multimedia library";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/Download.png";
//  MainMenuItem.NavigateUrl = SitePath + "Download.aspx" + Language;
//  MainMenuItem.ToolTip = "Download the application for listen your library";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/About.png";
//  MainMenuItem.NavigateUrl = SitePath + "About.aspx" + Language;
//  MainMenuItem.ToolTip = "About the Live Multimedia Market";
//  MainMenu.Items.Add(MainMenuItem);

//}

//protected void LoadMenuUser()
//{
//  System.Web.UI.WebControls.MenuItem MainMenuItem;

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/Home.png";
//  MainMenuItem.NavigateUrl = SitePath + "Default.aspx" + Language;
//  MainMenuItem.ToolTip = "Main page";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/Library.png";
//  MainMenuItem.NavigateUrl = SitePath + "LiveMultimedia.aspx" + Language;
//  MainMenuItem.ToolTip = "Enter to your multimedia library";
//  MainMenu.Items.Add(MainMenuItem);

//  //MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/Profile.png";
//  //MainMenuItem.NavigateUrl = SitePath + "ProfileUser.aspx";
//  //MainMenuItem.ToolTip = Session["Username"].ToString();
//  //MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/Download.png";
//  MainMenuItem.NavigateUrl = SitePath + "Download.aspx" + Language;
//  MainMenuItem.ToolTip = "Download the application for listen your library";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/About.png";
//  MainMenuItem.NavigateUrl = SitePath + "About.aspx" + Language;
//  MainMenuItem.ToolTip = "About the Live Multimedia Market";
//  MainMenu.Items.Add(MainMenuItem);

//  MainMenuItem = new MenuItem();
//  //MainMenuItem.ImageUrl = "~/images/menu/Logout.png";
//  MainMenuItem.NavigateUrl = SitePath + "Logout.aspx" + Language;
//  MainMenuItem.ToolTip = "Logout from Live Multimedia Market";
//  MainMenu.Items.Add(MainMenuItem);
//}
//this.HyperLinkMain.NavigateUrl = SitePath + "Default.aspx" + Language;
//this.HyperLinkDemo.NavigateUrl = SitePath + "Demo.aspx" + Language;
//this.HyperLinkDownload.NavigateUrl = SitePath + "Download.aspx" + Language;
//this.HyperLinkLogout.NavigateUrl = SitePath + "Logout.aspx" + Language;
//this.HyperLinkAbout.NavigateUrl = SitePath + "About.aspx" + Language;

//protected void SetLanguageMasterPage()
//{
//  LanguageInfoItem = Session["LanguageInfo"] as LanguageInfo;

//  try
//  {
//    var SelectedLanguage = Page.Request.QueryString["Language"] as string;
//    if (string.IsNullOrEmpty(SelectedLanguage) || string.IsNullOrWhiteSpace(SelectedLanguage)) SelectedLanguage = LanguageInfoItem.Language;

//    if (LanguageInfoItem.Language != SelectedLanguage)
//    {
//      var ListLanguage = (List<LiveMultimediaSite.LiveMultimediaService.LanguageInfo>)Application["ListLanguage"];
//      LanguageInfoItem = ListLanguage.First(SelectedLanguageInfoItem => SelectedLanguageInfoItem.Language.ToLower() == SelectedLanguage.ToLower());
//      Session["LanguageInfo"] = LanguageInfoItem;
//    }
//  }
//  catch (Exception)
//  {
//    //Nothing TO DO
//  }

//  //Load locale resource
//  HyperLink HyperLinkSelectLanguage = (HyperLink)this.FindControl("SelectLanguage");
//  HyperLinkSelectLanguage.NavigateUrl = "~/Language.aspx?Language=" + LanguageInfoItem.Language;
//  HyperLinkSelectLanguage.Text = LanguageInfoItem.NativeName;

//  SetLocalization();
//}
