using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Data;
using System.Drawing;
using System.Threading.Tasks;
//using Microsoft.Live;

using LiveMultimediaSite.LiveMultimediaService;

public partial class LiveMultimedia : System.Web.UI.Page
  // Добавлено для Callback вызовов клиента:
  ,System.Web.UI.ICallbackEventHandler
{
  #region Define vars

  private Unit ScreenPixelsWidth;  
  private const int MultimediaWidth = 200;
  private const int MultimediaHeight = 100;

  private string UserToken;
  //private ClientInternetBrowser ClientBrowser = new ClientInternetBrowser();

  private Unit MetroStyleWidth = new Unit(MultimediaWidth);
  private Unit MetroStyleHeight = new Unit(MultimediaHeight);

  protected string returnValue;
  protected string MFG;

  protected string validationLookUpStock = "LookUpStock";
  protected string validationLookUpSale = "LookUpSale";

  private MultimediaSource[] ListMultimediaSource;
  private OAuthObjectFolder[] ListMultimediaFolder;
  private OAuthObjectAudio[] ListMultimediaItem;
  private Style[] ListMultimediaItemStyle;
  private PlaylistObject[] ListPlaylist;

  private MultimediaSource SelectedMultimediaSource;

  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();
  //private LiveMultimediaLibrary.LiveMultimediaLibrary Utilites = new LiveMultimediaLibrary.LiveMultimediaLibrary();

  private static string SourcePlaylist_ToolTip;

  #endregion Define vars

  protected void Page_Load(object sender, EventArgs e)
  {
    //await WriteLogAsync("Page_Load", enumTypeLog.Information, null);

    if (!Page.IsCallback && !Page.IsPostBack)
    {      
      SetLocalization();
    }    

    #region Рассмотреть, что это такое (взят пример откуда-то)
    //if (!ScriptManager1.IsInAsyncPostBack)
// {
// Label1.Text = "В первый раз"; 
// }
//Данное условие добавляется в обработчики этапов создания страницы.
    //Все что находится внутри этого условия не будет выполняться при каждой асинхронной загрузке, а все что else — будет.
    #endregion Рассмотреть, что это такое (взят пример откуда-то)

    #region Check UserToken
    UserToken = LiveMultimediaLibrary.ConvertObjectToString(Session["UserToken"]);

    if (UserToken=="")
    {
      Session["PrevPage"] = "LiveMultimedia.aspx";

      Response.Redirect("~/Login.aspx", false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();
      return;
    }
    #endregion Check UserToken
    
    ScreenPixelsWidth = (Unit)Session["ScreenPixelsWidth"];

    #region ScreenResolution 2
    //Request.Browser.IsMobileDevice
    //Request.Browser.ScreenPixelsWidth
    //Request.Browser.ScreenPixelsHeight
    #endregion ScreenResolution 2

    #region Page.IsPostBack
    ListPlaylist = null;
    if (!Page.IsPostBack)
    {
      Session["ListAlbum"] = null;

      using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
      {
        Task<PlaylistObject[]> t = LiveMultimedia.RemotePlaylistLoadAsync(UserToken);
        t.Wait();
        ListPlaylist = t.Result;

        this.ddlPlaylist.DataSource = ListPlaylist;
        this.ddlPlaylist.DataValueField = "IdPlaylist";
        this.ddlPlaylist.DataTextField = "Playlist";
        this.ddlPlaylist.DataBind();
      }

      this.ddlPlaylist.Width = (MultimediaWidth * 2) + 2;
      this.ddlPlaylist.Height = 30;

      this.txtNewPlaylist.Width = this.ddlPlaylist.Width;
      this.txtNewPlaylist.Height = this.ddlPlaylist.Height;

      this.cmdNewPlaylist.Height = this.ddlPlaylist.Height;
      this.cmdSavePlaylist.Height = this.ddlPlaylist.Height;
      this.cmdCancelSavePlaylist.Height = this.ddlPlaylist.Height;

      //this.cmdBackContentSource.Width = MultimediaWidth;
      //this.cmdBackContentSource.Height = MultimediaHeight;
    }
    #endregion Page.IsPostBack

    #region Page.IsCallback
    if (!Page.IsCallback)
    {
      // Зачем ниже?
      //if (UserToken == "")
      //{
      //  Session["PrevPage"] = "LiveMultimedia.aspx";
      //  Response.Redirect("~/Login.aspx", true);
      //}

      HiddenFieldGUID.Value = UserToken.ToUpper();

      //ClientBrowser.BrowserName = Page.Request.Browser.Browser;
      //ClientBrowser.BrowserVersion = Page.Request.Browser.Version;
      
      if (!Page.IsPostBack)
      {
        Session["IsViewMultimediaFolder"] = true;

        //ListMultimediaSource_Load();  
        //SetSelectedMultimediaSource();
        //ListMultimediaAlbum_Load();
      }
      ListMultimediaGridView_Load();

      #region RegisterScript

      String cbReference = Page.ClientScript.GetCallbackEventReference(this, "arg", "ReceiveServerData", "context");
      String callbackScript= "function CallServer(arg, context)" + "{ " + cbReference + ";}";
      Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "CallServer", callbackScript, true);

      //Page.ClientScript.RegisterClientScriptBlock(this.GetType(),
      //           validationLookUpStock, "function LookUpStock() {  " +
      //           "var lb = document.forms[0].ListBox1; " +
      //           "if (lb.selectedIndex == -1) { alert ('Please make a selection.'); return; } " +
      //           "var product = lb.options[lb.selectedIndex].text;  " +
      //           @"CallServer(product, ""LookUpStock"");}  ", true);
      //if (User.Identity.IsAuthenticated)
      //{
      //  Page.ClientScript.RegisterClientScriptBlock(this.GetType(),
      //  validationLookUpSale, "function LookUpSale() {  " +
      //  "var lb = document.forms[0].ListBox1; " +
      //  "if (lb.selectedIndex == -1) { alert ('Please make a selection.'); return; } " +
      //  "var product = lb.options[lb.selectedIndex].text;  " +
      //  @"CallServer(product, ""LookUpSale"");} ", true);
      //}

      //String cbReference = "var param = arg + '|' + context;" +
      //    Page.ClientScript.GetCallbackEventReference(this,
      //    "param", "ReceiveServerData", "context");

      //String callbackScript;

      //callbackScript = "function CallServer(arg, context)" +
      //    "{ " + cbReference + "} ;";

      //Page.ClientScript.RegisterClientScriptBlock(this.GetType(),
      //    "CallServer", callbackScript, true);

      #endregion RegisterScript
    }
    #endregion Page.IsCallback
  }

  #region Load Multimedia

  private void SetSelectedMultimediaSource()
  {
    try
    {
      int IdTypeMultimediaSource = (int)Session["IdTypeMultimediaSource"];
      if (IdTypeMultimediaSource > 0)
      {
        foreach (MultimediaSource MultimediaSourceItem in ListMultimediaSource)
        {
          if (MultimediaSourceItem.IdTypeMultimediaSource == IdTypeMultimediaSource)
          {
            SelectedMultimediaSource = MultimediaSourceItem;
            break;
          }
        }
      }
      else
      {
        SelectedMultimediaSource = ListMultimediaSource[0];
        Session["IdTypeMultimediaSource"] = SelectedMultimediaSource.IdTypeMultimediaSource;
      }
    }
    catch (Exception)
    {
      Response.Redirect("~/Default.aspx", false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();
    }
  }

  //private void SetSelectedMultimediaSource()
  //{
  //  int IdTypeMultimediaSource = Convert.ToInt32(Session["IdTypeMultimediaSource"]);
  //  if (IdTypeMultimediaSource > 0)
  //  {
  //    foreach (MultimediaSource MultimediaSourceItem in ListMultimediaSource)
  //    {
  //      if (MultimediaSourceItem.IdTypeMultimediaSource == IdTypeMultimediaSource)
  //      {
  //        SelectedMultimediaSource = MultimediaSourceItem;
  //        break;
  //      }
  //    }
  //  }
  //  else
  //  {
  //    SelectedMultimediaSource = ListMultimediaSource[0];
  //    Session["IdTypeMultimediaSource"] = SelectedMultimediaSource.IdTypeMultimediaSource;
  //  }
  //}

  public class TemplateMultimediaSource : ITemplate
  {
    #region Define vars
    const int MultimediaWidth = 200;
    const int MultimediaHeight = 100;

    Unit MetroStyleWidth = new Unit(MultimediaWidth);
    Unit MetroStyleHeight = new Unit(MultimediaHeight);

    ListItemType _type;
    string _CommandName;
    string _colName;
    #endregion Define vars

    public TemplateMultimediaSource(ListItemType type, string CommandName, string colname)
    {
      _type = type;
      _CommandName = CommandName;
      _colName = colname;
    }

    void ITemplate.InstantiateIn(System.Web.UI.Control container)
    {
      Button bt;

      if (_type == ListItemType.Item)
      {
        switch (_CommandName)
        {
          #region SelectMultimediaSource
          case "SelectMultimediaSource":            
            bt = new Button();
              bt.DataBinding += new EventHandler(bt_DataBinding);
              //bt.Click += bt_Click;
              container.Controls.Add(bt);

            break;
          #endregion SelectMultimediaSource

          #region SelectMultimediaFolder
          case "SelectMultimediaFolder":
              bt = new Button();
              bt.DataBinding += new EventHandler(bt_DataBinding);
              container.Controls.Add(bt);

              System.Web.UI.WebControls.Style StyleRemovePlaylist = new System.Web.UI.WebControls.Style();
              StyleRemovePlaylist = new System.Web.UI.WebControls.Style();
              StyleRemovePlaylist.CssClass = "RemovePlaylist";

              Button RemovePlaylist = new Button();
              RemovePlaylist.ApplyStyle(StyleRemovePlaylist);
              container.Controls.Add(RemovePlaylist);

            break;
          #endregion SelectMultimediaFolder

          #region SelectMultimediaItem
          case "SelectMultimediaItem":
            System.Web.UI.WebControls.Style StyleMultimediaItem;

            StyleMultimediaItem = new System.Web.UI.WebControls.Style();
            StyleMultimediaItem.CssClass = "MultimediaItem";

            Label lbl = new Label();
            lbl.DataBinding += new EventHandler(bt_DataBinding);
            lbl.ApplyStyle(StyleMultimediaItem);
            container.Controls.Add(lbl);            
           
            StyleMultimediaItem = new System.Web.UI.WebControls.Style();
            StyleMultimediaItem.CssClass = "SourcePlaylist";

            //System.Web.UI.WebControls.Label LabelSource = new System.Web.UI.WebControls.Label();
            System.Web.UI.WebControls.Button LabelSource = new System.Web.UI.WebControls.Button();
            LabelSource.ApplyStyle(StyleMultimediaItem);
            container.Controls.Add(LabelSource);

            StyleMultimediaItem = new System.Web.UI.WebControls.Style();
            StyleMultimediaItem.CssClass = "AddToPlaylist";

            Button AddPlaylist = new Button();
            AddPlaylist.ApplyStyle(StyleMultimediaItem);
            container.Controls.Add(AddPlaylist);

            //System.Web.UI.WebControls.Table TableInLabel = new System.Web.UI.WebControls.Table();
            //TableInLabel.BorderStyle = BorderStyle.Solid;
            //System.Web.UI.WebControls.TableRow RowInTable;
            //System.Web.UI.WebControls.TableCell CellInTable;
            //RowInTable = new System.Web.UI.WebControls.TableRow();
            //TableInLabel.Rows.Add(RowInTable);
            //RowInTable = new System.Web.UI.WebControls.TableRow();
            //CellInTable = new System.Web.UI.WebControls.TableCell();
            //CellInTable.BorderStyle = BorderStyle.Solid;
            //RowInTable.Cells.Add(CellInTable);
            //CellInTable = new System.Web.UI.WebControls.TableCell();
            //CellInTable.BorderStyle = BorderStyle.Solid;
            //CellInTable.Controls.Add(AddPlaylist);
            //RowInTable.Cells.Add(CellInTable);
            //TableInLabel.Rows.Add(RowInTable);
            //container.Controls.Add(TableInLabel);

            break;
          #endregion SelectMultimediaItem
        }
      }
    }

    void bt_DataBinding(object sender, EventArgs e)
    {
      GridViewRow container;
      object dataValue=null; Button bt=null; Label lbl = null; Button ButtonPlaylist=null;
      //Label SourcePlaylist = null;
      Button SourcePlaylist = null;
      string sIdTypeMultimediaSource;

      #region First step DataBinding
      switch (_CommandName)
      {
        case "SelectMultimediaSource":
          bt = (Button)sender;
          container = (GridViewRow)bt.NamingContainer;
          dataValue = DataBinder.Eval(container.DataItem, _colName);

          break;
        case "SelectMultimediaFolder":
          bt = (Button)sender;
          container = (GridViewRow)bt.NamingContainer;
          dataValue = DataBinder.Eval(container.DataItem, _colName);

          ButtonPlaylist = (Button)bt.Parent.Controls[1];

          break;
        case "SelectMultimediaItem":
          lbl = (Label)sender;
          container = (GridViewRow)lbl.NamingContainer;
          dataValue = DataBinder.Eval(container.DataItem, _colName);

          //ButtonPlaylist = (Button)lbl.Parent.Controls[1];
          //ButtonPlaylist = (Button)lbl.Parent.Controls[1].Controls[1].Controls[1].Controls[0];
          ButtonPlaylist = (Button)lbl.Parent.Controls[2];
          //SourcePlaylist = (Label)lbl.Parent.Controls[1];
          SourcePlaylist = (Button)lbl.Parent.Controls[1];

          break;
      }
      #endregion First step DataBinding

      if (dataValue != DBNull.Value)
      {
        string[] a = dataValue.ToString().Split(new Char[] { '|' });

        MultimediaSource MultimediaSourceItem = new MultimediaSource();
        MultimediaSourceItem.StyleBackColor = a[2];
        MultimediaSourceItem.StyleBorderColor = a[3];
        try
        {
          MultimediaSourceItem.StyleFontSize = Convert.ToInt32(a[4]);
        }
        catch (Exception)
        {
        }
        
        MultimediaSourceItem.StyleForeColor = a[5];

        switch (_CommandName)
        {
          #region SelectMultimediaSource
          case "SelectMultimediaSource":
            bt.Text = a[0];
            bt.CommandArgument = a[1];
            bt.CommandName = _CommandName;

            SetStyleMultimediaSource(bt, MultimediaSourceItem);

            break;
          #endregion SelectMultimediaSource

          #region SelectMultimediaFolder
          case "SelectMultimediaFolder":
            bt.Text = a[0];
            bt.CommandArgument = a[1];
            bt.CommandName = _CommandName;
            bt.Attributes.Add("onclick", "ScrollToTop(this)"); // For scrolling page to the left after selecting of Album

            SetStyleMultimediaFolder(bt, MultimediaSourceItem);         

            sIdTypeMultimediaSource = a[6];
            if (sIdTypeMultimediaSource == "2")
            {              
              ButtonPlaylist.Visible = true;
              ButtonPlaylist.CommandName = "DeletePlaylist";
              ButtonPlaylist.CommandArgument = a[1];
              ButtonPlaylist.Text = "-";
              ButtonPlaylist.ToolTip = "Delete playlist";
            }
            else
            {
              ButtonPlaylist.Visible = false;
            }

            break;
          #endregion SelectMultimediaFolder

          #region SelectMultimediaItem
          case "SelectMultimediaItem":
            lbl.Text = a[0];

            string DisplayName = lbl.Text;
            string MultimediaFileGUID = a[6];
            sIdTypeMultimediaSource = a[7];
            string sIdTypeMultimediaSourceOriginal = a[8];

            // Работает и с несответствующим типом файла. Проверить, нужно ли это вообще! 
            string[] a2 = lbl.Text.Split(new Char[] { '.' });
            string TypeMultimedia = a2[a2.Length - 1].ToLower();

            string GUID = MultimediaFileGUID + "*" + sIdTypeMultimediaSource + "*" + TypeMultimedia;
            if (sIdTypeMultimediaSource == "2")
            {
              GUID = GUID + "*" + sIdTypeMultimediaSourceOriginal;
            }
            lbl.Attributes.Add("GUID", GUID);
            lbl.Attributes.Add("onclick", "SelectMultimediaItem(this)");
            lbl.Attributes.Add("IsError", "0");
            lbl.Attributes.Add("IsSelected", "0");
            if (MultimediaSourceItem.StyleBackColor == a[9])
            {
              lbl.Attributes.Add("IsPlayed", "0");
            }
            else
            {
              lbl.Attributes.Add("IsPlayed", "1");
            }
            MultimediaSourceItem.StyleBackColor = a[9];
            SetStyleMultimediaItem(lbl, MultimediaSourceItem);

            if (sIdTypeMultimediaSource != "2") // Non Playist
            {
              ButtonPlaylist.CommandName = "AddToPlayList";
              ButtonPlaylist.CommandArgument = a[1];
              ButtonPlaylist.Text = "+";
              ButtonPlaylist.ToolTip = "Add to playlist";

              SourcePlaylist.Visible = false;
            }
            else
            {
              ButtonPlaylist.CommandName = "RemoveFromPlayList";
              ButtonPlaylist.CommandArgument = a[1];
              ButtonPlaylist.Text = "-";
              ButtonPlaylist.ToolTip = "Remove from playlist";
              //ButtonPlaylist.Attributes.Add("onclick", "RemoveLiveAudio(this)");

              switch (sIdTypeMultimediaSourceOriginal)
              {
                case "1":
                  SourcePlaylist.Text = "Home albums";
                  break;
                case "3":
                  SourcePlaylist.Text = "OneDrive";
                  break;
                case "4":
                  SourcePlaylist.Text = "Google Drive";
                  break;
                case "6":
                  SourcePlaylist.Text = "VKontakte";
                  break;
              }

              SourcePlaylist.CommandName = "SelectMultimediaSource";
              SourcePlaylist.CommandArgument = sIdTypeMultimediaSourceOriginal;
              SourcePlaylist.BackColor = Color.FromName(a[10]);
              SourcePlaylist.BorderColor = Color.FromName(a[10]);
              SourcePlaylist.ForeColor = Color.FromName(a[5]);
              SourcePlaylist.ToolTip = SourcePlaylist_ToolTip;
            }

            break;
          #endregion SelectMultimediaItem
        }
      }
      else
      {
        switch (_CommandName)
        {
          case "SelectMultimediaSource":
            bt.Visible = false;

            break;
          case "SelectMultimediaFolder":
            bt.Visible = false;
            ButtonPlaylist.Visible = false;

            break;
          case "SelectMultimediaItem":
            lbl.Visible = false;
            ButtonPlaylist.Visible = false;
            SourcePlaylist.Visible = false;

            break;
        }
      }
    }

    void bt_Click(object sender, EventArgs e)
    {
      var bt = sender as Button;
      if (bt != null)
      {
        //DoIt(bt.CommandName, bt.CommandArgument);
      }
    }

    void SetStyleMultimediaSource(System.Web.UI.WebControls.Button MultimediaControl, MultimediaSource MultimediaSourceStyle)
    {
      System.Web.UI.WebControls.Style ControlStyle = new Style();

      ControlStyle = new Style();
      ControlStyle.Width = MetroStyleWidth;
      ControlStyle.Height = MetroStyleHeight;
      ControlStyle.BackColor = Color.FromName(MultimediaSourceStyle.StyleBackColor);
      ControlStyle.BorderColor = Color.FromName(MultimediaSourceStyle.StyleBorderColor);
      ControlStyle.ForeColor = Color.FromName(MultimediaSourceStyle.StyleForeColor);
      ControlStyle.Font.Size = new FontUnit(MultimediaSourceStyle.StyleFontSize);

      MultimediaControl.ApplyStyle(ControlStyle);
    }

    void SetStyleMultimediaFolder(System.Web.UI.WebControls.Button MultimediaControl, MultimediaSource MultimediaFolderStyle)
    {
      System.Web.UI.WebControls.Style ControlStyle = new Style();

      ControlStyle = new Style();
      ControlStyle.CssClass = "MultimediaFolder";
      ControlStyle.Width = MetroStyleWidth;
      ControlStyle.Height = MetroStyleHeight;
      ControlStyle.BackColor = Color.FromName(MultimediaFolderStyle.StyleBackColor);
      ControlStyle.BorderColor = Color.FromName(MultimediaFolderStyle.StyleBorderColor);
      ControlStyle.ForeColor = Color.FromName(MultimediaFolderStyle.StyleForeColor);
      ControlStyle.Font.Size = new FontUnit(MultimediaFolderStyle.StyleFontSize - MultimediaFolderStyle.StyleFontSize * 0.25);

      MultimediaControl.ApplyStyle(ControlStyle);
    }

    void SetStyleMultimediaItem(System.Web.UI.WebControls.Label MultimediaControl, MultimediaSource MultimediaFolderStyle)
    {
      System.Web.UI.WebControls.Style ControlStyle = new Style();

      ControlStyle = new Style();
      ControlStyle.Width = MetroStyleWidth;
      ControlStyle.Height = MetroStyleHeight;
      ControlStyle.BackColor = Color.FromName(MultimediaFolderStyle.StyleBackColor);
      ControlStyle.BorderColor = Color.FromName(MultimediaFolderStyle.StyleBorderColor);
      ControlStyle.ForeColor = Color.FromName(MultimediaFolderStyle.StyleForeColor);
      ControlStyle.Font.Size = new FontUnit(MultimediaFolderStyle.StyleFontSize - MultimediaFolderStyle.StyleFontSize * 0.25);      

      MultimediaControl.ApplyStyle(ControlStyle);
    }
  }

  protected async void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
  {
    int IdTypeMultimediaSource;
    OAuthObjectFolder ObjectFolder;
    OAuthObjectAudio ObjectMultimedia;
    Int64 IdPlaylist;
    bool IsSuccess;

    string CommandName = e.CommandName;
    string CommandArgument = e.CommandArgument.ToString();

    await WriteLogAsync("GridView1_RowCommand", enumTypeLog.Information, "CommandName="+CommandName ?? "EMPTY!!!"+". CommandArgument=" + CommandArgument ?? "EMPTY");

    switch (CommandName)
    {
      #region SelectMultimediaSource
      case "SelectMultimediaSource":
        this.LabelSelectedAlbum.Text = GetElementLocalization("LiveMultimedia_LabelSelectedAlbum", "Not selected an album");
        this.LabelSelectedSong.Text = GetElementLocalization("LiveMultimedia_LabelSelectedSong", "Not selected a song");

        IdTypeMultimediaSource = Convert.ToInt32(CommandArgument);
        Session["IdTypeMultimediaSource"] = IdTypeMultimediaSource;
        Session["ListMultimediaSource"] = null;
        Session["ListObjectFolder"] = null;
        Session["IsViewMultimediaFolder"] = true;

        await SelectMultimediaSource(IdTypeMultimediaSource);

        break;
      #endregion SelectMultimediaSource

      #region SelectMultimediaFolder
      case "SelectMultimediaFolder":
        string Album = ListMultimediaFolder[Convert.ToInt32(CommandArgument)].name;
        Session["SelectedMultimediaFolder"] = ListMultimediaFolder[Convert.ToInt32(CommandArgument)];

        List<string> ListIdAlbum=new List<string>();
        foreach (OAuthObjectFolder ObjectFolderItem in ListMultimediaFolder)
        {
          if (Album == ObjectFolderItem.name) { ListIdAlbum.Add(ObjectFolderItem.id); }
        }
        Session["ListIdAlbum"] = ListIdAlbum.ToArray();
        this.LabelSelectedAlbum.Text = GetElementLocalization("LiveMultimedia_LabelSelectedAlbumDone", "Selected album")+": " + Album;

        Session["ListMultimediaItem"] = null;
        Session["IsViewMultimediaFolder"] = false;
        ListMultimediaGridView_Load();

        break;
      #endregion SelectMultimediaFolder

      #region SelectMultimediaItem
      case "SelectMultimediaItem":        
        break;
      #endregion SelectMultimediaItem

      #region Add to PlayList
      case "AddToPlayList":
        if (ddlPlaylist.SelectedIndex >= 0)
        {
          IdTypeMultimediaSource = Convert.ToInt32(Session["IdTypeMultimediaSource"]);
          IdPlaylist = Convert.ToInt64(ddlPlaylist.SelectedValue);

          ObjectMultimedia = ListMultimediaItem[Convert.ToInt32(CommandArgument)];
          string IdMultimediaItem = ObjectMultimedia.id;
          string MultimediaItem = ObjectMultimedia.name;

          using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
          {
            //IsSuccess = await LiveMultimedia.RemotePlaylistItemSaveAsync(UserToken, IdPlaylist, IdTypeMultimediaSource, IdMultimediaItem, MultimediaItem);
            Task<bool> t = LiveMultimedia.RemotePlaylistItemSaveAsync(UserToken, IdPlaylist, IdTypeMultimediaSource, IdMultimediaItem, MultimediaItem);
            t.Wait();
            IsSuccess = t.Result;
          }
        }
        if (Session["PlayingSong"]!=null)
        {
          this.LabelSelectedSong.Text = Session["PlayingSong"].ToString();
        }

        break;
      #endregion Add to PlayList

      #region RemoveFromPlayList

      case "RemoveFromPlayList":
        ObjectMultimedia = ListMultimediaItem[Convert.ToInt32(CommandArgument)];
        int IdPlaylistItem = Convert.ToInt32(ObjectMultimedia.idfromobject);
                
        using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          //IsSuccess = await LiveMultimedia.RemotePlaylistItemRemoveAsync(UserToken, IdPlaylistItem);
          Task<bool> t = LiveMultimedia.RemotePlaylistItemRemoveAsync(UserToken, IdPlaylistItem);
          t.Wait();
          IsSuccess = t.Result;
        }

        if (IsSuccess == true)
        {
          Session["ListMultimediaItem"] = null;
          ListMultimediaGridView_Load();
        }
        else
        {
          if (Session["PlayingSong"] != null)
          {
            this.LabelSelectedSong.Text = Session["PlayingSong"].ToString();
          }
        }

        break;
      #endregion RemoveFromPlayList

      #region DeletePlayList

      case "DeletePlaylist":
        ObjectFolder = ListMultimediaFolder[Convert.ToInt32(CommandArgument)];
        IdPlaylist = Convert.ToInt64(ObjectFolder.id);

        using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          //IsSuccess = await LiveMultimedia.RemotePlaylistDeleteAsync(UserToken, IdPlaylist);
          Task<bool> t = LiveMultimedia.RemotePlaylistDeleteAsync(UserToken, IdPlaylist);
          t.Wait();
          IsSuccess = t.Result;
        
          if (IsSuccess)
          {
            Session["ListObjectFolder"] = null;

            //ListPlaylist = LiveMultimedia.RemotePlaylistLoad(UserToken);
            Task<PlaylistObject[]> t2= LiveMultimedia.RemotePlaylistLoadAsync(UserToken);
            t2.Wait();
            ListPlaylist = t2.Result;

            this.ddlPlaylist.DataSource = ListPlaylist;
            this.ddlPlaylist.DataBind();

            ListMultimediaGridView_Load();
          }
        }
        break;
      #endregion DeletePlayList

      default:
        break;
    }
  }

  private async Task SelectMultimediaSource(int IdTypeMultimediaSource)
  {
    Session["IdTypeMultimediaSource"] = IdTypeMultimediaSource;

    bool IsSuccess;
    using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
    {
      Task<bool> t = LiveMultimediaService.OAuthSetTokenAsync("",UserToken, "", null);
      t.Wait();
      IsSuccess = t.Result;
    }

    if (!IsSuccess)
    {
      #region "Sign in" only or "Sign in and Sign out"

      string REDIRECT_URI;
      switch (IdTypeMultimediaSource)
      {
        case 1:
          REDIRECT_URI = "~/LiveMultimedia.aspx";
          break;
        case 2:
          REDIRECT_URI = "~/LiveMultimedia.aspx";
          break;
        case 3:
          REDIRECT_URI = "~/CallbackOAuth.aspx";
          Session["ActionOAuth"] = "SignOutAndSignIn";
          break;
        case 4:
          REDIRECT_URI = "~/CallbackOAuth.aspx";
          Session["ActionOAuth"] = "SignIn";
          break;
        case 6:
          REDIRECT_URI = "~/CallbackOAuth.aspx";
          Session["ActionOAuth"] = "SignIn";
          break;
        case 9:
          REDIRECT_URI = "~/LiveMultimedia.aspx";
          break;
        default:
          REDIRECT_URI = "~/Default.aspx";
          break;
      }
      Response.Redirect(REDIRECT_URI, false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();

      #endregion "Sign in" only or "Sign in and Sign out"

      #region Sign in
      //switch (IdTypeMultimediaSource)
      //{
      //  case 3: //OneDrive
      //    // OAuth 2.0 http://msdn.microsoft.com/ru-ru/library/hh243649.aspx
      //    // http://msdn.microsoft.com/onedrive/
      //    // Using the REST API http://msdn.microsoft.com/library/dn659752.aspx
      //    // http://msdn.microsoft.com/ru-ru/library/windows/apps/hh243647
      //    ClientId = "****";
      //    SCOPES = "wl.signin,wl.skydrive,wl.offline_access";
      //    OAuthUrl = "https://login.live.com/oauth20_authorize.srf?client_id=" + ClientId + "&display=page" + "&scope=" + SCOPES + "&response_type=code&redirect_uri=" + REDIRECT_URI;
      //    //https://login.live.com/oauth20_authorize.srf?client_id=0000000040034407&display=page&locale=ru&redirect_uri=http%3A%2F%2Fisdk.dev.live.com%2Fdev%2Fisdk%2Fcallback.aspx&response_type=token&scope=wl.signin&state=redirect_type%3Dauth%26display%3Dpage%26request_ts%3D1402303508689%26response_method%3Durl%26secure_cookie%3Dfalse
      //    //Response.Redirect(OAuthUrl, true);

      //    Response.Redirect(OAuthUrl, false);
      //    HttpContext.Current.ApplicationInstance.CompleteRequest();
      //    break;
      //  case 4: //Google Drive
      //    //Documentation https://developers.google.com/accounts/docs/OAuth2 
      //    ClientId = "***";
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
      //    ClientId = "****";
      //    SCOPES = "audio,offline";
      //    OAuthUrl = "https://oauth.vk.com/authorize?client_id=" + ClientId + "&scope=" + SCOPES + "&redirect_uri=" + REDIRECT_URI + "&response_type=code" + "&v=5.21" + "&revoke=1";
      //    Response.Redirect(OAuthUrl, true);
      //    break;
      //  default:
      //    ListMultimediaGridView_Load();
      //    break;
      //}
      #endregion Sign in
    }
    else
    {
      ListMultimediaGridView_Load();
    }

    await WriteLogAsync("SelectMultimediaSource", enumTypeLog.Information, "IdTypeMultimediaSource=" + IdTypeMultimediaSource.ToString());
  }

  private void LoadMultimediaSource()
  {
    using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
    {
      LiveMultimedia.Open();
      ListMultimediaSource = LiveMultimedia.GetListMultimediaSource(UserToken);
      Session["ListMultimediaSource"] = ListMultimediaSource;
    }    
  }

  private void LoadMultimediaFolder()
  {    
    try
    {
      int IdTypeMultimediaSource = Convert.ToInt32(Session["IdTypeMultimediaSource"]);

      using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
      {
        Task<OAuthObjectFolder[]> t = LiveMultimedia.RemoteGetContentMultimediaSourceAsync(UserToken, IdTypeMultimediaSource);
        t.Wait();
        ListMultimediaFolder = t.Result;

        Session["ListObjectFolder"] = ListMultimediaFolder;
      }
    }
    catch (Exception)
    {
      Session["ListObjectFolder"] = new OAuthObjectFolder[0];
    }
  }

  private void LoadMultimediaItem()
  {
    try
    {
      string[] ListIdAlbum = (string[])Session["ListIdAlbum"];
      int IdTypeMultimediaSource = Convert.ToInt32(Session["IdTypeMultimediaSource"]);

      using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
      {
        Task<OAuthObjectAudio[]> t = LiveMultimediaSystem.RemoteGetListMultimediaByIdAlbumAsync(UserToken, IdTypeMultimediaSource.ToString(), ListIdAlbum, "ru-RU", "ru",null);
        t.Wait();
        ListMultimediaItem = t.Result;
      }
      ListMultimediaItemStyle = new Style[ListMultimediaItem.Count()];
      Style ItemStyle; int i = 0;
      foreach (OAuthObjectAudio ObjectAudioOriginal in ListMultimediaItem)
      {
        ItemStyle = new Style();
        ItemStyle.BackColor = Color.FromName(SelectedMultimediaSource.StyleBackColor);
        ListMultimediaItemStyle[i] = ItemStyle;
        i++;
      }
      Session["ListMultimediaItem"] = ListMultimediaItem;
      Session["ListMultimediaItemStyle"] = ListMultimediaItemStyle;
    }
    catch (Exception)
    {
      ListMultimediaItem = null;
      Session["ListMultimediaItem"] = null;

      ListMultimediaItemStyle = null;
      Session["ListMultimediaItemStyle"] = null;
    }
  }

  private void ListMultimediaGridView_Load()
  {
    #region Set visible fields
    bool IsViewMultimediaFolder = Convert.ToBoolean(Session["IsViewMultimediaFolder"]);
    int IdTypeMultimediaSource=Convert.ToInt32(Session["IdTypeMultimediaSource"]);

    if (IsViewMultimediaFolder || IdTypeMultimediaSource == 2)
    { // Playlist' folders
      this.ddlPlaylist.Visible = false;
      this.cmdNewPlaylist.Visible = false;
      this.txtNewPlaylist.Visible = false;
      this.cmdSavePlaylist.Visible = false;
      this.cmdCancelSavePlaylist.Visible = false;
      this.rfvnewPlaylist.Visible = false;
      //this.cmdBackContentSource.Visible = false;
      this.cmdBackContentSource.Enabled = false;
    }
    else
    { // Another folders
      this.ddlPlaylist.Visible = true;
      this.cmdNewPlaylist.Visible = true;
      this.txtNewPlaylist.Visible = false;
      this.cmdSavePlaylist.Visible = false;
      this.cmdCancelSavePlaylist.Visible = false;
      this.rfvnewPlaylist.Visible = true;
      //this.cmdBackContentSource.Visible = true;
      this.cmdBackContentSource.Enabled = true;
    }

    if (IsViewMultimediaFolder)
    {
      //this.cmdBackContentSource.Visible = false;
      this.cmdBackContentSource.Enabled = false;
    }
    else
    {
      //this.cmdBackContentSource.Visible = true;
      this.cmdBackContentSource.Enabled = true;
    }

    #endregion Set visible fields

    #region Load Multimedia

    #region Load Multimedia Source
    if (Session["ListMultimediaSource"] == null)
    {      
      LoadMultimediaSource();
    }
    else
    {
      ListMultimediaSource = (MultimediaSource[])Session["ListMultimediaSource"];
    }
    SetSelectedMultimediaSource();      
    #endregion Load Multimedia Source

    int MaxCountColumn; int MaxCountRow;

    if (Convert.ToBoolean(Session["IsViewMultimediaFolder"]))
    {
      #region Load Multimedia Folder

      if (Session["ListObjectFolder"] == null)
      {
        LoadMultimediaFolder();
      }
      else
      {
        ListMultimediaFolder = (OAuthObjectFolder[])Session["ListObjectFolder"];
      }
      if (ListMultimediaFolder!=null)
      {
        if (ListMultimediaFolder.Count() > 0)
        {
          // Вычислить правильно - целое/нецелое число
          //ColumnCount = (ListMultimediaFolder.Count() / ListMultimediaSource.Length) + 1;          
        }
        else
          MaxCountColumn = 0;
      }
      else
        MaxCountColumn = 0;

      #endregion Load Multimedia Folder
    }
    else
    {
      #region Load Multimedia Item
      if (Session["ListMultimediaItem"] == null)
      {
        LoadMultimediaItem();  
      }
      else
      {
        ListMultimediaItem = (OAuthObjectAudio[])Session["ListMultimediaItem"];
        ListMultimediaItemStyle = (Style[])Session["ListMultimediaItemStyle"];
      }
      if (ListMultimediaItem.Count() > 0)
      {
        // Вычислить правильно - целое/нецелое число
        //ColumnCount = (ListMultimediaItem.Count() / ListMultimediaSource.Length) + 1;
      }
      else MaxCountColumn = 0;
      #endregion Load Multimedia Item
    }

    //Debug for fail screen width
    //ScreenPixelsWidth = new Unit(320);

    #region Define Max count Columns and Rows
    int MaxPixelsBetweenItems = 1;

    int MaxCountColumnOnScreen = (int)(ScreenPixelsWidth.Value / MetroStyleWidth.Value);
    Unit NewScreenPixelsWidth = new Unit(ScreenPixelsWidth.Value - (double)((MaxCountColumnOnScreen-1) * MaxPixelsBetweenItems));

    MaxCountColumn = (int)(NewScreenPixelsWidth.Value / MetroStyleWidth.Value) - 1;
    if (MaxCountColumn < 1) MaxCountColumn = 1;

    int MaxLength;
    if (IsViewMultimediaFolder)
    {
      if (ListMultimediaFolder.Length >= ListMultimediaSource.Length)
        MaxLength = ListMultimediaFolder.Length;
      else
        MaxLength = ListMultimediaSource.Length;
    }
    else
    {
      if (ListMultimediaItem.Length >= ListMultimediaSource.Length)
        MaxLength = ListMultimediaItem.Length;
      else
        MaxLength = ListMultimediaSource.Length;
    }

    double doubleMaxCountRow = (double)(MaxLength) / (double)MaxCountColumn;
    MaxCountRow = (int)doubleMaxCountRow;
    if (MaxCountRow != doubleMaxCountRow) MaxCountRow++;

    if (MaxCountRow < ListMultimediaSource.Length) MaxCountRow = ListMultimediaSource.Length;

    #endregion Define Max count Columns and Rows

    #region Load TableMultimedia

    System.Data.DataTable TableMultimedia;
    System.Data.DataRow dr;

    TableMultimedia = new DataTable("TableMultimedia");

    #region Define columns and rows

    TableMultimedia.Columns.Add(new DataColumn("TitleMultimediaSource"));
    for (int i = 0; i < MaxCountColumn; i++)
    {
      TableMultimedia.Columns.Add(new DataColumn("Name" + i.ToString()));
    }
    for (int i = 0; i < MaxCountRow; i++)
    {
      dr = TableMultimedia.NewRow();
      TableMultimedia.Rows.Add(dr);
    }

    #endregion Define columns and rows

    int CountRow = 0; int CountColumn = 0;

    #region ListMultimediaSource to TableMultimedia

    foreach (MultimediaSource MultimediaSourceItem in ListMultimediaSource)
    {
      dr = TableMultimedia.Rows[CountRow];
      dr["TitleMultimediaSource"] = MultimediaSourceItem.TitleMultimediaSource
        + "|" + MultimediaSourceItem.IdTypeMultimediaSource.ToString()
        + "|" + MultimediaSourceItem.StyleBackColor
        + "|" + MultimediaSourceItem.StyleBorderColor
        + "|" + MultimediaSourceItem.StyleFontSize.ToString()
        + "|" + MultimediaSourceItem.StyleForeColor;
      CountRow++;
    }

    #endregion ListMultimediaSource to TableMultimedia

    CountRow = 0;

    if (IsViewMultimediaFolder)
    {
      #region ListMultimediaFolder to TableMultimedia

      for (int CountMultimedia = 0; CountMultimedia < ListMultimediaFolder.Length; CountMultimedia++)
      {
        dr = TableMultimedia.Rows[CountRow];
        string ColumnName = "Name" + CountColumn.ToString();
        dr[ColumnName] = ListMultimediaFolder[CountMultimedia].name
          + "|" + CountMultimedia.ToString()
          + "|" + SelectedMultimediaSource.StyleBackColor
          + "|" + SelectedMultimediaSource.StyleBorderColor
          + "|" + SelectedMultimediaSource.StyleFontSize.ToString()
          + "|" + SelectedMultimediaSource.StyleForeColor
          + "|" + Session["IdTypeMultimediaSource"].ToString();

        CountColumn++;
        if (CountColumn == MaxCountColumn)
        {
          CountColumn = 0;
          CountRow++;
        }
      }

      #endregion ListMultimediaFolder to TableMultimedia

      #region ORG
      //#region ListMultimediaFolder to TableMultimedia

      //for (int CountMultimedia = 0; CountMultimedia < ListMultimediaFolder.Length; CountMultimedia++)
      //{
      //  dr = TableMultimedia.Rows[CountRow];
      //  string ColumnName = "Name" + CountColumn.ToString();
      //  dr[ColumnName] = ListMultimediaFolder[CountMultimedia].name
      //    + "|" + CountMultimedia.ToString()
      //    + "|" + SelectedMultimediaSource.StyleBackColor
      //    + "|" + SelectedMultimediaSource.StyleBorderColor
      //    + "|" + SelectedMultimediaSource.StyleFontSize.ToString()
      //    + "|" + SelectedMultimediaSource.StyleForeColor
      //    + "|" + Session["IdTypeMultimediaSource"].ToString();

      //  CountRow++;
      //  if (CountRow == TableMultimedia.Rows.Count)
      //  {
      //    CountRow = 0;
      //    CountColumn++;
      //  }
      //}

      //#endregion ListMultimediaFolder to TableMultimedia
      #endregion ORG
    }
    else
    {
      try
      {
        #region ListMultimediaItem to TableMultimedia

        int OriginalIdTypeMultimdiaSource;
        string OriginalBackColor;
        for (int CountMultimedia = 0; CountMultimedia < ListMultimediaItem.Length; CountMultimedia++)
        {
          if (IdTypeMultimediaSource == 2)
          {
            OriginalIdTypeMultimdiaSource = Convert.ToInt32(ListMultimediaItem[CountMultimedia].album); //Album contains original IdTypeMultimdiaSource for Playlist
            MultimediaSource MultimediaSourceObject = ListMultimediaSource.Single(m => m.IdTypeMultimediaSource == OriginalIdTypeMultimdiaSource);
            OriginalBackColor = MultimediaSourceObject.StyleBackColor;
          }
          else
          {
            OriginalIdTypeMultimdiaSource = 0;
            OriginalBackColor = "";
          }

          dr = TableMultimedia.Rows[CountRow];
          string ColumnName = "Name" + CountColumn.ToString();
          bool test = ListMultimediaItem[CountMultimedia].name.Contains("|");

          dr[ColumnName] = ListMultimediaItem[CountMultimedia].name
            + "|" + CountMultimedia.ToString()
            + "|" + SelectedMultimediaSource.StyleBackColor
            + "|" + SelectedMultimediaSource.StyleBorderColor
            + "|" + SelectedMultimediaSource.StyleFontSize.ToString()
            + "|" + SelectedMultimediaSource.StyleForeColor
            + "|" + ListMultimediaItem[CountMultimedia].id
            + "|" + Session["IdTypeMultimediaSource"].ToString()
            + "|" + OriginalIdTypeMultimdiaSource.ToString()
            + "|" + ListMultimediaItemStyle[CountMultimedia].BackColor.Name
            + "|" + OriginalBackColor;
          //+ "|" + ListMultimediaItemStyle[CountMultimedia].CssClass;

          CountColumn++;
          if (CountColumn == MaxCountColumn)
          {
            CountColumn = 0;
            CountRow++;
          }
        }
        #endregion ListMultimediaItem to TableMultimedia
        #region ORG
        //#region ListMultimediaItem to TableMultimedia

        //int OriginalIdTypeMultimdiaSource;
        //string OriginalBackColor;
        //for (int CountMultimedia = 0; CountMultimedia < ListMultimediaItem.Length; CountMultimedia++)
        //{
        //  if (IdTypeMultimediaSource == 2)
        //  {
        //    OriginalIdTypeMultimdiaSource = Convert.ToInt32(ListMultimediaItem[CountMultimedia].album); //Album contains original IdTypeMultimdiaSource for Playlist
        //    MultimediaSource MultimediaSourceObject = ListMultimediaSource.Single(m => m.IdTypeMultimediaSource == OriginalIdTypeMultimdiaSource);
        //    OriginalBackColor = MultimediaSourceObject.StyleBackColor;
        //  }
        //  else
        //  {
        //    OriginalIdTypeMultimdiaSource = 0;
        //    OriginalBackColor = "";
        //  }

        //  dr = TableMultimedia.Rows[CountRow];
        //  string ColumnName = "Name" + CountColumn.ToString();
        //  bool test = ListMultimediaItem[CountMultimedia].name.Contains("|");

        //  dr[ColumnName] = ListMultimediaItem[CountMultimedia].name
        //    + "|" + CountMultimedia.ToString()
        //    + "|" + SelectedMultimediaSource.StyleBackColor
        //    + "|" + SelectedMultimediaSource.StyleBorderColor
        //    + "|" + SelectedMultimediaSource.StyleFontSize.ToString()
        //    + "|" + SelectedMultimediaSource.StyleForeColor
        //    + "|" + ListMultimediaItem[CountMultimedia].id
        //    + "|" + Session["IdTypeMultimediaSource"].ToString()
        //    + "|" + OriginalIdTypeMultimdiaSource.ToString()
        //    + "|" + ListMultimediaItemStyle[CountMultimedia].BackColor.Name
        //    + "|" + OriginalBackColor;
        //  //+ "|" + ListMultimediaItemStyle[CountMultimedia].CssClass;

        //  CountRow++;
        //  if (CountRow == TableMultimedia.Rows.Count)
        //  {
        //    CountRow = 0;
        //    CountColumn++;
        //  }
        //}
        //#endregion ListMultimediaItem to TableMultimedia
        #endregion ORG
      }
      catch (Exception)
      {
      }
      
    }

    #endregion Load TableMultimedia

    #endregion Load Multimedia

    try
    {
      #region Load GridView

      TemplateField GridViewColumn;

      this.GridView1.BorderStyle = BorderStyle.NotSet;
      this.GridView1.BorderWidth = 0;
      this.GridView1.Columns.Clear();

      GridViewColumn = new TemplateField();
      GridViewColumn.ItemTemplate = new TemplateMultimediaSource(ListItemType.Item, "SelectMultimediaSource", "TitleMultimediaSource");
      GridView1.Columns.Add(GridViewColumn);

      for (int i = 0; i < TableMultimedia.Columns.Count - 1; i++)
      {
        GridViewColumn = new TemplateField();
        if (Convert.ToBoolean(Session["IsViewMultimediaFolder"]))
        {
          GridViewColumn.ItemTemplate = new TemplateMultimediaSource(ListItemType.Item, "SelectMultimediaFolder", "Name" + i.ToString());
        }
        else
        {
          GridViewColumn.ItemTemplate = new TemplateMultimediaSource(ListItemType.Item, "SelectMultimediaItem", "Name" + i.ToString());
        }
        GridView1.Columns.Add(GridViewColumn);
      }

      this.GridView1.DataSource = TableMultimedia;
      this.GridView1.DataBind();

      if (GridView1.Columns.Count == 1)
      {
        Style StyleEmptyCell;
        StyleEmptyCell = new Style();
        StyleEmptyCell.Width = MetroStyleWidth;
        StyleEmptyCell.Height = MetroStyleHeight;
        StyleEmptyCell.BackColor = Color.FromName(SelectedMultimediaSource.StyleBackColor);
        StyleEmptyCell.BorderColor = Color.FromName(SelectedMultimediaSource.StyleBorderColor);
        StyleEmptyCell.ForeColor = Color.FromName(SelectedMultimediaSource.StyleForeColor);
        StyleEmptyCell.Font.Size = new FontUnit(SelectedMultimediaSource.StyleFontSize - SelectedMultimediaSource.StyleFontSize * 0.25);

        Label EmptyCell = new Label();
        EmptyCell.Text = "Selected is empty";
        //EmptyCell.ApplyStyle(StyleEmptyCell);

        this.GridView1.Rows[0].Cells[0].Controls.Add(EmptyCell);
      }

      #endregion Load GridView
    }
    catch (Exception)
    {
    }
    
  }

  //protected  MultimediaSource GetMultimediaSourceById(int IdTypeMultimediaSource)
  //{
  //  MultimediaSource ms = ListMultimediaSource.Single(m => m.IdTypeMultimediaSource == IdTypeMultimediaSource);
  //}

  protected void cmdBackContentSource_Click(object sender, EventArgs e)
  {
    Session["IsViewMultimediaFolder"] = true;
    ListMultimediaGridView_Load();
  }

  #endregion Load Multimedia

  #region Set Metro Style

  private void SetMetroStyleMultimediaSource(System.Web.UI.WebControls.Button MultimediaSourceControl, MultimediaSource MultimediaSource)
  {
    System.Web.UI.WebControls.Style ControlStyle = new Style();

    ControlStyle = new Style();
    ControlStyle.Width = MetroStyleWidth;
    ControlStyle.Height = MetroStyleHeight;
    ControlStyle.BackColor = Color.FromName(MultimediaSource.StyleBackColor);
    ControlStyle.BorderColor = Color.FromName(MultimediaSource.StyleBorderColor);
    ControlStyle.ForeColor = Color.FromName(MultimediaSource.StyleForeColor);
    ControlStyle.Font.Size = new FontUnit(MultimediaSource.StyleFontSize);

    MultimediaSourceControl.ApplyStyle(ControlStyle);
  }

  private void SetMetroStyleMultimediaItem(System.Web.UI.WebControls.TableCell MultimediaItem)
  {
    System.Web.UI.WebControls.Style ControlStyleCell = new Style();

    ControlStyleCell = new Style();
    ControlStyleCell.Width = MetroStyleWidth;
    ControlStyleCell.Height = MetroStyleHeight;
    ControlStyleCell.BackColor = Color.FromName(SelectedMultimediaSource.StyleBackColor);
    ControlStyleCell.BorderColor = Color.FromName(SelectedMultimediaSource.StyleBorderColor);
    ControlStyleCell.ForeColor = Color.FromName(SelectedMultimediaSource.StyleForeColor);
    ControlStyleCell.Font.Size = new FontUnit(SelectedMultimediaSource.StyleFontSize - SelectedMultimediaSource.StyleFontSize * 0.25);

    MultimediaItem.ApplyStyle(ControlStyleCell);
  }

  private void SetMetroStyleMultimediaItem(System.Web.UI.WebControls.Label MultimediaItem)
  {
    System.Web.UI.WebControls.Style ControlStyleCell = new Style();

    ControlStyleCell = new Style();
    ControlStyleCell.Width = MetroStyleWidth;
    ControlStyleCell.Height = MetroStyleHeight;
    ControlStyleCell.BackColor = Color.FromName(SelectedMultimediaSource.StyleBackColor);
    ControlStyleCell.BorderColor = Color.FromName(SelectedMultimediaSource.StyleBorderColor);
    ControlStyleCell.ForeColor = Color.FromName(SelectedMultimediaSource.StyleForeColor);
    ControlStyleCell.Font.Size = new FontUnit(SelectedMultimediaSource.StyleFontSize - SelectedMultimediaSource.StyleFontSize * 0.25);

    MultimediaItem.ApplyStyle(ControlStyleCell);
  }

  private void SetMetroStyleEmpty(System.Web.UI.WebControls.TableCell Control)
  {
    System.Web.UI.WebControls.Style ControlStyle = new Style();
    ControlStyle.Width = MetroStyleWidth;
    ControlStyle.Height = MetroStyleHeight;
    ControlStyle.BackColor = Color.FromName("#0094ff");
    ControlStyle.BorderColor = Color.FromName("#0094ff");
    ControlStyle.ForeColor = Color.White;

    Control.ApplyStyle(ControlStyle);
  }

  private void SetMetroStyleEmptySource(System.Web.UI.WebControls.TableCell Control)
  {
    //Unit NewMetroStyleWidth;
    //Unit NewMetroStyleHeight;

    //if (MaxCountColumns != 1)
    //{
    //  NewMetroStyleWidth = new Unit(MultimediaWidth);
    //  NewMetroStyleHeight = new Unit(MultimediaHeight);
    //}
    //else
    //{
    //  int NewStyleWidth = Convert.ToInt32((ScreenPixelsWidth.Value - 41) / 2);
    //  int NewStyleHeight = NewStyleWidth / 2;
    //  NewMetroStyleWidth = new Unit(NewStyleWidth);
    //  NewMetroStyleHeight = new Unit(NewStyleHeight);
    //}     

    System.Web.UI.WebControls.Style ControlStyle = new Style();
    ControlStyle.Width = MetroStyleWidth;
    ControlStyle.Height = MetroStyleHeight;
    ControlStyle.BackColor = Color.FromName(SelectedMultimediaSource.StyleBackColor);
    ControlStyle.BorderColor = Color.FromName(SelectedMultimediaSource.StyleBorderColor);
    ControlStyle.ForeColor = Color.FromName(SelectedMultimediaSource.StyleForeColor);
    ControlStyle.Font.Size = new FontUnit(SelectedMultimediaSource.StyleFontSize - SelectedMultimediaSource.StyleFontSize * 0.25);

    Control.ApplyStyle(ControlStyle);
  }

#endregion Set Metro Style

  #region Action registered scripts

  public void RaiseCallbackEvent(string eventArgument)
  {
    string[] argParts;

    #region Test Arguments

    if (string.IsNullOrEmpty(eventArgument))
    {
    //  returnValue = "BAD";
      return;
    }

    argParts = eventArgument.Split('*');
    if (argParts == null)
    {
      return;
    }

    if ((argParts.Length != 4) && (argParts.Length != 5))
    {
      return;
    }

    if (argParts[0] != Session["UserToken"].ToString())
    {
      return;
    }
    #endregion Test Arguments

    try
    {
      int IdTypeMultimediaSource = Convert.ToInt32(argParts[2]);
      if (IdTypeMultimediaSource == 2)
      {
        IdTypeMultimediaSource = Convert.ToInt32(argParts[4]);
      }
      string IdMultimediaItem=argParts[1];
      ListMultimediaItem = (OAuthObjectAudio[])Session["ListMultimediaItem"];
      ListMultimediaItemStyle = (Style[])Session["ListMultimediaItemStyle"];
      int i = 0;
      foreach (OAuthObjectAudio ObjectAudio in ListMultimediaItem)
      {
        if (ObjectAudio.id == IdMultimediaItem)
        {
          ListMultimediaItemStyle[i].BackColor = Color.Gray;
          //ListMultimediaItemStyle[i].CssClass = "PlayedItem";
          Session["PlayingSong"] = ObjectAudio.name;
          break;
        }
        i++;
      }
      Session["ListMultimediaItemStyle"] = ListMultimediaItemStyle;
    }
    catch (Exception)
    {
      return;
    }    
  }

  public string GetCallbackResult()
  {
    return "";
    //return returnValue;
  }

  #endregion Action registered scripts

  #region Playlist
  protected void cmdNewPlaylist_Click(object sender, EventArgs e)
  {
    SetVisible_NewPlaylist();
  }

  protected void cmdSavePlaylist_Click(object sender, EventArgs e)
  {
    string ErrorMessage;

    if (Page.IsValid)
    {
      try
      {
        int IdTypeMultimediaSource = Convert.ToInt32(Session["IdTypeMultimediaSource"]);
        string PlaylistName = this.txtNewPlaylist.Text.Trim();
        using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          //ErrorMessage = await LiveMultimediaSystem.RemotePlaylistSaveAsync(UserToken, PlaylistName);
          Task<string> t = LiveMultimediaSystem.RemotePlaylistSaveAsync(UserToken, PlaylistName);
          t.Wait();
          ErrorMessage = t.Result;

          if (string.IsNullOrEmpty(ErrorMessage) || string.IsNullOrWhiteSpace(ErrorMessage))
          {
            this.ddlPlaylist.DataSource = LiveMultimediaSystem.RemotePlaylistLoad(UserToken);
            this.ddlPlaylist.DataBind();
            ListItem SelectedItem=this.ddlPlaylist.Items.FindByText(PlaylistName);
            int SelectedIndex = this.ddlPlaylist.Items.IndexOf(SelectedItem);
            this.ddlPlaylist.SelectedIndex = SelectedIndex;

            SetVisible_SaveNewPlaylist();
          }
          else
            SetVisible_NewPlaylist();
        }        
      }
      catch (Exception)
      {
        SetVisible_NewPlaylist();
      }
    }
    else
    {
      SetVisible_NewPlaylist();
    }
  }

  protected void cmdCancelSavePlaylist_Click(object sender, EventArgs e)
  {
    SetVisible_SaveNewPlaylist();
  }

  private void SetVisible_NewPlaylist()
  {
    this.ddlPlaylist.Visible = false;
    this.cmdNewPlaylist.Visible = false;
    this.txtNewPlaylist.Visible = true;
    this.cmdSavePlaylist.Visible = true;
    this.cmdCancelSavePlaylist.Visible = true;
    this.rfvnewPlaylist.Visible = true;

    this.txtNewPlaylist.Text = "";
    this.txtNewPlaylist.Focus();
  }

  private void SetVisible_SaveNewPlaylist()
  {
    this.ddlPlaylist.Visible = true;
    this.cmdNewPlaylist.Visible = true;
    this.txtNewPlaylist.Visible = false;
    this.cmdSavePlaylist.Visible = false;
    this.cmdCancelSavePlaylist.Visible = false;
  }

  #endregion Playlist

  #region Localization
  private void SetLocalization()
  {
    this.LabelSelectedAlbum.Text = GetElementLocalization("LiveMultimedia_LabelSelectedAlbum", "Not selected an album");
    // Using in GridView1_RowCommand: GetElementLocalization("LiveMultimedia_LabelSelectedAlbumDone", "Selected album") + ": " + Album;

    this.LabelSelectedSong.Text = GetElementLocalization("LiveMultimedia_LabelSelectedSong", "Not selected a song");
    this.cmdBackContentSource.Text = GetElementLocalization("LiveMultimedia_cmdBackContentSource", "Quick back to the Albums");

    this.cmdNewPlaylist.Text = GetElementLocalization("LiveMultimedia_cmdNewPlaylist", "New the Playlist");
    this.cmdSavePlaylist.Text = GetElementLocalization("LiveMultimedia_cmdSavePlaylist", "Save the Playlist");
    this.cmdCancelSavePlaylist.Text = GetElementLocalization("LiveMultimedia_cmdCancelSavePlaylist", "Cancel");

    SourcePlaylist_ToolTip = GetElementLocalization("LiveMultimedia_SourcePlaylist_ToolTip", "Go to an album of multimedia");

    var LabelLoadingMultimediaStatus = this.UpdateProgressLoadMultimedia.FindControl("LabelLoadingMultimediaStatus") as Label;
    LabelLoadingMultimediaStatus.Text = GetElementLocalization("LiveMultimedia_LabelLoadingMultimediaStatus", "Loading your multimedia")+"...";
  }

  public string GetElementLocalization(string ElementName, string DefaultElementValue)
  {
    string ElementValue;

    var LanguageInfoItem = Session["LanguageInfo"] as LanguageInfo;
    var ListLocalization = Application[LanguageInfoItem.Language] as List<LocalizationElement>;

    try
    {
      LocalizationElement LocalizationDictionaryItem = ListLocalization.Single(LocalizationElement => LocalizationElement.ElementName == ElementName);
      ElementValue = LocalizationDictionaryItem.ElementValue;
    }
    catch (Exception)
    {
      ElementValue = DefaultElementValue;
    }

    return ElementValue;
  }
  
  #endregion Localization

  #region Write log

  private async Task WriteLogAsync(string Procedure, enumTypeLog IdTypeLog, string Message = null)
  {
    try
    {
      var ClientIp = Server.HtmlEncode(Request.UserHostAddress);
      var Scope = Page.Request.Url.Segments[Page.Request.Url.Segments.Length - 1];
      var UserToken = LiveMultimediaLibrary.ConvertObjectToString(Session["UserToken"]);

      using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
      {
        switch (IdTypeLog)
        {
          case enumTypeLog.Information:
            await LiveMultimediaSystem.TraceInformationAsync(Application["ServerAccountKey"].ToString(), Scope, Procedure, Message, ClientIp, null, UserToken);
            break;
          case enumTypeLog.Warning:
            await LiveMultimediaSystem.TraceWarningAsync(Application["ServerAccountKey"].ToString(), Scope, Procedure, Message, ClientIp, null, UserToken);
            break;
          case enumTypeLog.Error:
            await LiveMultimediaSystem.TraceErrorAsync(Application["ServerAccountKey"].ToString(), Scope, Procedure, Message, ClientIp, null, UserToken);
            break;
          default:
            break;
        }
      }
    }
    catch (Exception)
    {
    }
  }

  #endregion Write log

}