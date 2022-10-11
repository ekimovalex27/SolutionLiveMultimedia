using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Threading.Tasks;
using System.Drawing;

using LiveMultimediaSite.LiveMultimediaService;

namespace LiveMultimediaSite
{
  public partial class Default : System.Web.UI.Page
    // Добавлено для Callback вызовов клиента:
  , System.Web.UI.ICallbackEventHandler
  {   
    #region Define const
    private enum enumTypeMultimediaItem : int { Source = 1, Folder = 2, Audio = 3, Video = 4, Picture = 5, Document = 6 }

    const int MaxColumnsOnPageSource = 3;
    const int MinWidthScreen = 320;
    const double RatioWidthItem = 3.1415926535; //Number PI
    const double RatioItem = 1.5;
    const double RatioItemMobile = 2;    
    #endregion Define const

    #region Define vars

    private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();
    private string ServerAccountKey;

    List<MultimediaItem> ListMultimediaItem;
    string id;
    bool IsRequiredAuthorization, IsRequiredOAuth;
    string UserToken;
    Unit ScreenPixelsWidth, ScreenPixelsHeight;

    Unit ItemWidth;
    Unit ItemHeight;
    int ColumnsOnPage;

    List<BreadCrumps> ListBreadCrumbs;

    private bool IsMobileDevice;
    #endregion Define vars

    private const int MultimediaWidth = 200;
    private const int MultimediaHeight = 100;
    
    private Unit MetroStyleWidth = new Unit(MultimediaWidth);
    private Unit MetroStyleHeight = new Unit(MultimediaHeight);    

    protected void Page_Init(object sender, EventArgs e)
    {
      ServerAccountKey = Application["ServerAccountKey"] as string;

      if (!Page.IsCallback)
      {
        IsMobileDevice = Convert.ToBoolean(Session["IsMobileDevice"]);
        
        ScreenPixelsWidth = (Unit)Session["ScreenPixelsWidth"];
        ScreenPixelsHeight = (Unit)Session["ScreenPixelsHeight"];

        //For Debug
        //IsMobileDevice = true;
        //ScreenPixelsWidth = new Unit(338, UnitType.Pixel);
        //ScreenPixelsHeight = new Unit(563, UnitType.Pixel);

        UserToken = LiveMultimediaLibrary.ConvertObjectToString(Session["UserToken"]);

        id = Page.Request.QueryString["id"] as string;
      }
    }

    protected async void Page_Load(object sender, EventArgs e)
    {
      if (LiveMultimediaLibrary.CheckGoodString(id))
      {
        if (!await CheckAuthorizationSource()) return;

        #region Check UserToken
        if (!LiveMultimediaLibrary.CheckGoodString(UserToken) && IsRequiredAuthorization)
        {
          Session["PrevPage"] = "Default.aspx?" + Request.QueryString;
          Response.Redirect("~/Login.aspx", false);
          HttpContext.Current.ApplicationInstance.CompleteRequest();
          return;
        }
        #endregion Check UserToken

        #region Check OAuth
        if (IsRequiredOAuth)
        {
          Session["Id"] = id;

          if (LiveMultimediaLibrary.CheckGoodString(LiveMultimediaLibrary.ConvertObjectToString(Session["OAuthUrlSignOut"])))
          {
            Session["ActionOAuth"] = "SignOutAndSignIn";
          }
          else
          {
            Session["ActionOAuth"] = "SignIn";
          }

          Session["PrevPage"] = "Default.aspx?" + Request.QueryString;
          Response.Redirect("~/CallbackOAuth.aspx", false);
          HttpContext.Current.ApplicationInstance.CompleteRequest();
          return;
        }
        #endregion Check OAuth
      }

      if (!Page.IsPostBack && !Page.IsCallback)
      {
        await LoadBreadCrumbs();
        ShowBreadCrumps();

        await LoadSourceAsync();

        if (ListMultimediaItem.Count > 0)
        {
          if (GetTypeMultimediaIem(ListMultimediaItem[0].TypeItem) == enumTypeMultimediaItem.Source)
          {
            ShowSource();
          }
          else
          {
            ShowSourceItem();
          }
        }
      }

      #region Page.IsCallback
      if (!Page.IsCallback)
      {
        #region RegisterScript

        var cbReference = Page.ClientScript.GetCallbackEventReference(this, "arg", "ReceiveServerData", "context");
        var callbackScript = "function CallServer(arg, context)" + "{ " + cbReference + ";}";
        Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "CallServer", callbackScript, true);

        #endregion RegisterScript
      }
      #endregion Page.IsCallback
    }

    private async Task<bool> CheckAuthorizationSource()
    {
      bool returnValue;

      try
      {
        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnValueAuthorization = await LiveMultimediaService.CheckAuthorizationAsync(ServerAccountKey, id, UserToken);

          if (!LiveMultimediaLibrary.CheckGoodString(returnValueAuthorization["error"]))
          {
            bool.TryParse(returnValueAuthorization["IsRequiredAuthorization"], out IsRequiredAuthorization);
            bool.TryParse(returnValueAuthorization["IsRequiredOAuth"], out IsRequiredOAuth);

            Session["OAuthUrlSignIn"] = returnValueAuthorization["OAuthUrlSignIn"];
            Session["OAuthUrlSignOut"] = returnValueAuthorization["OAuthUrlSignOut"];
            Session["OAuthUrlToken"] = returnValueAuthorization["OAuthUrlToken"];

            returnValue = true;
          }
          else
            throw new ApplicationException(returnValueAuthorization["error"]);
        }
      }
      catch (Exception)
      {
        IsRequiredAuthorization = true;
        IsRequiredOAuth = true;
        Session["OAuthUrlSignIn"] = null;
        Session["OAuthUrlSignOut"] = null;
        Session["OAuthUrlToken"] = null;

        returnValue = false;
      }

      return returnValue;
    }

    //private async Task<bool> SetOAuthURLProvider()
    //{
    //  bool IsSuccess;

    //  try
    //  {
    //    using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
    //    {
    //      var returnTypeMultimediaOAuth = await LiveMultimediaService.OAuthGetTypeMultimediaAsync(ServerAccountKey, id, UserToken);

    //      if (!LiveMultimediaLibrary.CheckGoodString(returnTypeMultimediaOAuth.Item2))
    //      {
    //        Session["OAuthUrlSignIn"] = returnTypeMultimediaOAuth.Item1.OAuthUrlSignIn;
    //        Session["OAuthUrlSignOut"] = returnTypeMultimediaOAuth.Item1.OAuthUrlSignOut;
    //        Session["OAuthUrlToken"] = returnTypeMultimediaOAuth.Item1.OAuthUrlToken;

    //        IsSuccess = true;
    //      }
    //      else
    //        throw new ApplicationException(returnTypeMultimediaOAuth.Item2);
    //    }
    //  }
    //  catch (Exception)
    //  {
    //    IsSuccess = false;

    //    Session["OAuthUrlSignIn"] = null;
    //    Session["OAuthUrlSignOut"] = null;
    //    Session["OAuthUrlToken"] = null;
    //  }

    //  return IsSuccess;
    //}

    private async Task LoadBreadCrumbs()
    {
      try
      {
        using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var Language = Session["Language"] as string;
          var returnValue = await LiveMultimediaService.GetBreadCrumbsAsync(ServerAccountKey, Language, id, null, UserToken);

          if (!LiveMultimediaLibrary.CheckGoodString(returnValue.Item2))
            ListBreadCrumbs = returnValue.Item1.ToList();
          else
            throw new ApplicationException(returnValue.Item2);
        }
      }
      catch (Exception)
      {
        throw;
      }
    }

    private async Task LoadSourceAsync()
    {
      string ErrorMessage;

      try
      {
        var Language = Session["Language"] as string;
        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          //var returnValue = await LiveMultimediaService.GetItemsAsync(ServerAccountKey, Language, id, "folder", "title", UserToken);
          var returnValue = await LiveMultimediaService.GetItemsAsync(ServerAccountKey, Language, id, "none", "title", UserToken);
          ListMultimediaItem = returnValue.Item1.ToList();
          ErrorMessage = returnValue.Item2;
        }

        if (LiveMultimediaLibrary.CheckGoodString(ErrorMessage))
        {
          throw new ApplicationException(ErrorMessage);
        }
      }
      catch (Exception ex)
      {
        throw;
      }
    }

    private int GetColumnsOnPage()
    {
      int ColumnsOnPage;

      if (IsMobileDevice)
      {
        ColumnsOnPage = 1;
        return ColumnsOnPage;
      }

      if (ScreenPixelsWidth.Value <= MinWidthScreen * 2)
      {
        ColumnsOnPage = 2;
        return ColumnsOnPage;
      }

      ColumnsOnPage = MaxColumnsOnPageSource;

      return ColumnsOnPage;
    }

    private void CalculateSizeItems()
    {
      ColumnsOnPage = GetColumnsOnPage();

      if (ColumnsOnPage == 1)
      {
        ItemWidth = new Unit(ScreenPixelsWidth.Value, UnitType.Pixel);
        ItemHeight = new Unit(ItemWidth.Value / RatioItem, UnitType.Pixel);
      }
      else
      {
        ItemWidth = new Unit((ScreenPixelsWidth.Value / RatioWidthItem) - 10, UnitType.Pixel);
        ItemHeight = new Unit(ItemWidth.Value / RatioItem, UnitType.Pixel);
      }
    }

    private void GetColumnsOnPageAndSizeItems()
    {
      int MaxPixelsBetweenItems = 1;

      if (IsMobileDevice)
      {
        ColumnsOnPage = 1;
        ItemWidth = new Unit(ScreenPixelsWidth.Value - 10);
        ItemHeight = new Unit(ItemWidth.Value / 2);
      }
      else
      {
        ColumnsOnPage = (int)(ScreenPixelsWidth.Value / MetroStyleWidth.Value);
        var NewScreenPixelsWidth = new Unit(ScreenPixelsWidth.Value - (double)((ColumnsOnPage - 1) * MaxPixelsBetweenItems));
        ItemWidth = new Unit(NewScreenPixelsWidth.Value / ColumnsOnPage);
        ItemHeight = new Unit(ItemWidth.Value / 2);
      }
    }

    private void ShowBreadCrumps()
    {
      #region Define vars
      HyperLink BreadCrumbsHyperLink;
      Label BreadCrumbsArrow;
      FontUnit FontSize;
      #endregion Define vars

      if (IsMobileDevice)
      {
        FontSize = FontUnit.XXLarge;
      }
      else
      {
        FontSize = FontUnit.Medium;
      }

      foreach (var item in ListBreadCrumbs)
      {
        BreadCrumbsHyperLink = new HyperLink();
        BreadCrumbsHyperLink.Text = item.Name;

        if (LiveMultimediaLibrary.CheckGoodString(item.Id))
          BreadCrumbsHyperLink.NavigateUrl = "~/Default.aspx?id=" + item.Id + "&ca=" + Convert.ToInt32(item.IsRequiredAuthorization).ToString();
        else
          BreadCrumbsHyperLink.NavigateUrl = "~/Default.aspx";

        BreadCrumbsHyperLink.CssClass = "BreadCrumpsItem";
        BreadCrumbsHyperLink.Font.Size = FontSize;        
        this.PlaceBreadCrumbs.Controls.Add(BreadCrumbsHyperLink);

        BreadCrumbsArrow = new Label();
        BreadCrumbsArrow.Text = ">";
        BreadCrumbsArrow.CssClass = "BreadCrumpsArrow";
        BreadCrumbsArrow.Font.Size = FontSize;
        this.PlaceBreadCrumbs.Controls.Add(BreadCrumbsArrow);
      }
      
      if (this.PlaceBreadCrumbs.Controls.Count >= 2)
      {
        //Remove the last BreadCrumbsArrow
        this.PlaceBreadCrumbs.Controls.RemoveAt(this.PlaceBreadCrumbs.Controls.Count - 1);

        // Set another style for the last crumb
        var BreadCrumbsSelected = this.PlaceBreadCrumbs.Controls[this.PlaceBreadCrumbs.Controls.Count - 1] as HyperLink;
        BreadCrumbsSelected.CssClass = "BreadCrumpsSelectedItem";
        BreadCrumbsSelected.Font.Size = FontSize;
        //BreadCrumbsSelected.NavigateUrl = "";
      }
    }

    private void ShowSource()
    {
      #region Define vars
      TableRow RowSource;
      TableCell CellSource;
      HyperLink HyperLinkSource;
      #endregion Define vars

      CalculateSizeItems();

      TableItem.CssClass = "TableSource";
      RowSource = new TableRow();
      int ColumnCount = 0;

      int maxLengthName=0;
      FontUnit fu = FontUnit.Empty;
      if (IsMobileDevice)
      {
        //maxLengthName пока непонятно, как применить в расчётах размера шрифта
        maxLengthName = ListMultimediaItem.Max(ItemSource => ItemSource.Name.Length);

        var delta = Math.Abs(ScreenPixelsHeight.Value - ScreenPixelsWidth.Value);
        var ratio = ScreenPixelsWidth.Value / delta;
        fu = new FontUnit(220/ratio, UnitType.Pixel);

        // Пересчитать высоту элемента
        ItemHeight = new Unit(fu.Unit.Value * RatioItem, UnitType.Pixel); 

        //Для отладки. Показывает внизу значения разрешений.
        //MultimediaItem mi = new MultimediaItem();
        //mi.Name = "Width=" + ScreenPixelsWidth.Value.ToString()+ ", Height=" + ScreenPixelsHeight.Value.ToString() + ", Length=" + maxLengthName.ToString() + ", fu=" + fu.Unit.Value.ToString();
        //mi.Id = "qwe";
        //mi.StyleBackColor = "white";
        //mi.StyleForeColor = "black";
        //ListMultimediaItem.Add(mi);
      }

      foreach (var ItemSource in ListMultimediaItem)
      {
        #region Set HyperLink
        HyperLinkSource = new HyperLink();
        HyperLinkSource.Text = ItemSource.Name;

        if (ItemSource.IsEnabled)
        {
          //HyperLinkSource.NavigateUrl = "~/Default.aspx?id=" + ItemSource.Id + "&ca=" + Convert.ToInt32(ItemSource.IsRequiredAuthorization).ToString() + "&co=" + Convert.ToInt32(ItemSource.IsRequiredOAuth).ToString();
          HyperLinkSource.NavigateUrl = "~/Default.aspx?id=" + ItemSource.Id;
          HyperLinkSource.Enabled = true;
        }
        else
        {
          HyperLinkSource.Enabled = false;
          HyperLinkSource.Style["opacity"] = "0.5";
        }

        #region Set style of item
        HyperLinkSource.BackColor = Color.FromName(ItemSource.StyleBackColor);
        HyperLinkSource.ForeColor = Color.FromName(ItemSource.StyleForeColor);
        if (IsMobileDevice)
        {          
          HyperLinkSource.Font.Size = fu;
        }
        else
        {
          HyperLinkSource.Font.Size = (FontUnit)(ItemWidth.Value / 10);
        }
        
        HyperLinkSource.CssClass = "HyperLinkSource";
        #endregion Set style of item

        #endregion Set HyperLink

        #region Set Cell
        CellSource = new TableCell();
        CellSource.CssClass = "CellSource";
        CellSource.Width = ItemWidth;
        CellSource.Height = ItemHeight;

        if (!ItemSource.IsEnabled)
          CellSource.Style["opacity"] = "0.5";

        CellSource.Controls.Add(HyperLinkSource);
        #endregion Set Cell

        if (ColumnCount < ColumnsOnPage)
        {
          RowSource.Cells.Add(CellSource);
          ColumnCount++;
        }
        else
        {
          this.TableItem.Rows.Add(RowSource);

          RowSource = new TableRow();
          RowSource.Cells.Add(CellSource);
          ColumnCount = 1;
        }
      }
      if (RowSource.Cells.Count > 0) this.TableItem.Rows.Add(RowSource);
    }

    private void ShowSourceItem()
    {
      #region Define vars
      System.Web.UI.WebControls.TableRow RowSource;
      System.Web.UI.WebControls.TableCell CellSource;
      System.Web.UI.WebControls.HyperLink HyperLinkSource;
      System.Web.UI.WebControls.Style StyleMultimediaItem;
      #endregion Define vars

      //var ListSourceItem = Session["ListSourceItem"] as List<MultimediaItem>;

      GetColumnsOnPageAndSizeItems();

      //var StyleTd = "<style type=\"text/css\">td {width: 200px;height: 100px;background: #b6ff00; color:#000000}</style>";
      //Response.Write(StyleTd);

      //TableSourceItem.PreRender += TableSourceItem_PreRender;
      //HtmlTextWriter h = new HtmlTextWriter()

      //TableItem.CssClass = "TableSourceItem";
      //TableSourceItem.RenderBeginTag()
      //if (ListSourceItem.Count()<ColumnsOnPage) TableSourceItem.Style["width"] = "auto";

      if (IsMobileDevice)
      {
        TableItem.Style["width"] = "100%";
      }

      RowSource = new TableRow();
      int ColumnCount = 0;
      foreach (var ItemSource in ListMultimediaItem)
      {
        #region Set Cell
        CellSource = new TableCell();
        CellSource.CssClass = "CellSource";
        if (!IsMobileDevice)
        {
          CellSource.Width = ItemWidth;
        }
        CellSource.Height = ItemHeight;

        if (!ItemSource.IsEnabled)
        {
          CellSource.Enabled = false;
          CellSource.Style["opacity"] = "0.5";
        }
        #endregion Set Cell

        var TypeMultimediaItem = GetTypeMultimediaIem(ItemSource.TypeItem);
        if (TypeMultimediaItem==enumTypeMultimediaItem.Folder)
        {
          #region Folder

          HyperLinkSource = new HyperLink();
          HyperLinkSource.Text = ItemSource.Name;
          //HyperLinkSource.NavigateUrl = "~/Default.aspx?id=" + ItemSource.Id + "&ca=" + Convert.ToInt32(ItemSource.IsRequiredAuthorization).ToString();
          HyperLinkSource.NavigateUrl = "~/Default.aspx?id=" + ItemSource.Id;

          //Проверить в работе с UpdatePanel
          HyperLinkSource.Attributes.Add("onclick", "ScrollToTop(this)"); // For scrolling page to the top and left after selecting of Album

          #region Set style of item
          HyperLinkSource.BackColor = Color.FromName(ItemSource.StyleBackColor);
          HyperLinkSource.ForeColor = Color.FromName(ItemSource.StyleForeColor);

          if (IsMobileDevice)
          {
            HyperLinkSource.Font.Size = (FontUnit)(ItemWidth.Value / 8);
          }
          else
          {
            HyperLinkSource.Font.Size = (FontUnit)(ItemWidth.Value / 16);
          }

          HyperLinkSource.CssClass = "HyperLinkSource";
          #endregion Set style of item

          HyperLinkSource.Attributes.Add("TypeMultimediaItem", ItemSource.TypeItem);

          CellSource.Controls.Add(HyperLinkSource);
          
          // Отладить для Избранного
          if (ItemSource.IdSource == 10)
          {
            #region Favorites RemovePlaylist
            StyleMultimediaItem = new Style();
            StyleMultimediaItem.CssClass = "RemovePlaylist";

            var RemovePlaylist = new Button();
            RemovePlaylist.ApplyStyle(StyleMultimediaItem);

            RemovePlaylist.Visible = true;
            RemovePlaylist.CommandArgument = ItemSource.Id;
            RemovePlaylist.Text = "-";
            RemovePlaylist.ToolTip = "Delete playlist";
            RemovePlaylist.Command += new CommandEventHandler(this.DeletePlaylist);

            CellSource.Controls.Add(RemovePlaylist);
            #endregion Favorites RemovePlaylist
          }
          #endregion Folder
        }
        else
        {
          #region Item

          #region Audio
          //if (TypeMultimediaItem == enumTypeMultimediaItem.Audio)
          //{            
          //  var Audio = new System.Web.UI.HtmlControls.HtmlAudio();
          //  Audio.InnerText = "Your Internet browser don't support Live Multimedia Market";

          //  #region Set style of item
          //  //Audio.Attributes.Add("width", ItemWidth.Value.ToString());
          //  //Audio.Attributes.Add("height", ItemHeight.Value.ToString());
          //  //Audio.Attributes.Add("color", ItemSource.StyleForeColor);
          //  //Audio.Attributes.Add("font-size", (ItemWidth.Value / 16).ToString());
          //  #endregion Set style of item

          //  #region Set attributes audio
          //  Audio.Attributes.Add("id", "liveaudio" + ItemSource.Id); // + "&ca=" + Convert.ToInt32(ItemSource.IsRequiredAuthorization).ToString()
          //  //Audio.Attributes.Add("src", ItemSource.UrlPlay);
          //  Audio.Attributes.Add("preload", "none");
          //  Audio.Attributes.Add("onended", "LiveAudio_ended()");
          //  Audio.Attributes.Add("onerror", "LiveAudio_onerror(this)");
          //  Audio.Attributes.Add("onvolumechange", "LiveAudio_onvolumechange(this)");
          //  //Audio.Attributes.Add("controls", "controls");
          //  #endregion Set attributes audio

          //  CellSource.Controls.Add(Audio);
          //}
          #endregion Audio

          #region Video
          //if (TypeMultimediaItem == enumTypeMultimediaItem.Video)
          //{            
          //  var Video = new System.Web.UI.HtmlControls.HtmlVideo();
          //  Video.InnerText = "Your Internet browser don't support Live Multimedia Market";

          //  #region Set style of item
          //  Video.Attributes.Add("width", ItemWidth.Value.ToString());
          //  Video.Attributes.Add("height", ItemHeight.Value.ToString());
          //  Video.Attributes.Add("color", ItemSource.StyleForeColor);
          //  Video.Attributes.Add("font-size", (ItemWidth.Value / 16).ToString());
          //  #endregion Set style of item

          //  #region Set attributes video
          //  Video.Attributes.Add("id", "liveaudio");
          //  Video.Attributes.Add("preload", "none");
          //  Video.Attributes.Add("onended", "LiveAudio_ended()");
          //  Video.Attributes.Add("onerror", "LiveAudio_onerror(this)");
          //  Video.Attributes.Add("onvolumechange", "LiveAudio_onvolumechange(this)");
          //  Video.Attributes.Add("controls", "controls");
          //  Video.Attributes.Add("src", ItemSource.UrlPlay);
          //  Video.Attributes.Add("class", "HyperLinkSource");
          //  #endregion Set attributes video

          //  CellSource.Controls.Add(Video);
          //}
          #endregion Video

          var lbl = new Label();
          lbl.Text = ItemSource.Name;

          #region Set common style of item
          lbl.BackColor = Color.FromName(ItemSource.StyleBackColor);
          lbl.ForeColor = Color.FromName(ItemSource.StyleForeColor);
          if (IsMobileDevice)
          {
            lbl.Font.Size = (FontUnit)(ItemWidth.Value / 8);
          }
          else
          {
            lbl.Font.Size = (FontUnit)(ItemWidth.Value / 16);
          }          
          //lbl.Width = ItemWidth;
          //lbl.Height = ItemHeight;

          lbl.CssClass = "LabelSourceItem";
          #endregion Set common style of item

          #region Add style of item
          lbl.Style["font-family"] = "Calibri";
          lbl.Style["font-style"] = "normal";
          #endregion Add style of item

          //var GUID = ItemSource.Id;
          //if (IdSource == 10)
          //{
          //  var sIdTypeMultimediaSourceOriginal = "что-то";
          //  GUID = GUID + "*" + sIdTypeMultimediaSourceOriginal;
          //}
          if (ItemSource.IsEnabled)
          {
            lbl.Attributes.Add("onclick", "SelectMultimediaItem(this)");
            lbl.Attributes.Add("IsError", "0");
            lbl.Attributes.Add("IsSelected", "0");
            lbl.Attributes.Add("IsPlayed", "0");
            lbl.Attributes.Add("TypeMultimediaItem", ItemSource.TypeItem);
            lbl.Attributes.Add("Url", ItemSource.Url);
            lbl.Attributes.Add("savedText", "");
          }

          //System.Web.UI.HtmlControls.HtmlIframe
          CellSource.Controls.Add(lbl);

          #endregion Item

          if (ItemSource.IdSource != 10) // Non Favorites
          {
            #region AddToPlaylist
            //StyleMultimediaItem = new Style();
            //StyleMultimediaItem.CssClass = "AddToPlaylist";

            //var AddPlaylist = new Button();
            //AddPlaylist.Text = "+";
            //AddPlaylist.ToolTip = "Add to playlist";
            //AddPlaylist.CommandArgument = ItemSource.Id;
            //AddPlaylist.Command += new CommandEventHandler(this.AddToPlaylist);
            //AddPlaylist.ApplyStyle(StyleMultimediaItem);
            //CellSource.Controls.Add(AddPlaylist);
            #endregion AddToPlaylist
          }
          else  //Favorites
          {
            #region Source in Playlist
            StyleMultimediaItem = new Style();
            StyleMultimediaItem.CssClass = "SourcePlaylist";
            // Получать цвета источников
            StyleMultimediaItem.BackColor = Color.FromName("Gray");
            StyleMultimediaItem.BorderColor = Color.FromName("Gray");
            StyleMultimediaItem.ForeColor = Color.FromName("Gray");
            //SourcePlaylist.ToolTip = SourcePlaylist_ToolTip;

            var hyperlinkSourceInFavorites = new HyperLink();
            //IdSourceOriginal Передавать из базы. Текст для описания.
            var IdSourceOriginal = ItemSource.IdSource;
            hyperlinkSourceInFavorites.Text = "В работе";
            hyperlinkSourceInFavorites.NavigateUrl = "~/ShowItem.aspx?Source=" + ItemSource.IdSource.ToString();
            hyperlinkSourceInFavorites.ApplyStyle(StyleMultimediaItem);
            CellSource.Controls.Add(hyperlinkSourceInFavorites);
            #endregion Source in Playlist

            #region Remove From Playlist
            var RemoveFromPlaylist = new Button();
            RemoveFromPlaylist.CommandArgument = ItemSource.Id;
            RemoveFromPlaylist.Text = "-";
            RemoveFromPlaylist.ToolTip = "Remove from playlist";
            RemoveFromPlaylist.Command += new CommandEventHandler(this.DeleteFromPlaylist);
            CellSource.Controls.Add(RemoveFromPlaylist);
            #endregion Remove From Playlist
          }
        }

        #region Calc new row
        if (ColumnCount < ColumnsOnPage)
        {
          RowSource.Cells.Add(CellSource);
          ColumnCount++;
        }
        else
        {
          this.TableItem.Rows.Add(RowSource);

          RowSource = new TableRow();
          RowSource.Cells.Add(CellSource);
          ColumnCount = 1;
        }
        #endregion Calc new row
      }
      if (RowSource.Cells.Count > 0) this.TableItem.Rows.Add(RowSource);
    }

    private enumTypeMultimediaItem GetTypeMultimediaIem(string TypeMultimediaIem)
    {
      return (enumTypeMultimediaItem)Convert.ToInt32(TypeMultimediaIem);
    }

    void TableSourceItem_PreRender(object sender, EventArgs e)
    {
      var StyleTd = "<style type=\"text/css\">td {width: 200px;height: 100px;background: #b6ff00; color:#000000}</style>";
      Response.Write(StyleTd);
    }

    void AddToPlaylist(object sender, CommandEventArgs e)
    {
      var bt = (Button)sender;
    }

    void DeleteFromPlaylist(object sender, CommandEventArgs e)
    {
      var bt = (Button)sender;
    }

    void DeletePlaylist(object sender, CommandEventArgs e)
    {
      var bt = (Button)sender;
    }

    #region Action registered scripts

    public void RaiseCallbackEvent(string eventArgument)
    {
      if (!string.IsNullOrEmpty(eventArgument))
      {
        int duration;
        if (int.TryParse(eventArgument, out duration))
        {
          Session.Timeout += (int)(duration/60);
        }
      }

      return;
    }

    public string GetCallbackResult()
    {
      // Nothing TODO
      return "";
    }

    #endregion Action registered scripts
  }
}