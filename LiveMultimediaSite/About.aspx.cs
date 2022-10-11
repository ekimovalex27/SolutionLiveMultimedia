using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

using LiveMultimediaSite.LiveMultimediaService;

public partial class About : System.Web.UI.Page
{
  #region Define vars
  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

  private string ServerAccountKey;
  private bool IsMobileDevice;

  public string header1, body1;
  public string header2, body2;
  public string header3, body3;
  public string header4, body4;
  public string header5, body5;
  public string header6, body6;
  public string header7, body7;
  public string header8, body8;
  public string header9, body9;
  public string header10, body10;
  #endregion Define vars

  protected void Page_Init(object sender, EventArgs e)
  {
    ServerAccountKey = Application["ServerAccountKey"] as string;

    if (!Page.IsCallback)
    {
      IsMobileDevice = Convert.ToBoolean(Session["IsMobileDevice"]);
    }
  }

  protected void Page_Load(object sender, EventArgs e)
  {
    if (!Page.IsPostBack && !Page.IsCallback)
    {
      SetLocalization();
      ShowAbout();
    }
  }

  private void ShowAbout()
  {
    Panel AboutContainer;

    var ContentPlaceHolder = Master.FindControl("ContentPlaceHolder1") as ContentPlaceHolder;

    var AboutPage = new Panel();
    AboutPage.Attributes.Add("class", "AboutPage");

    if (!IsMobileDevice)
    {
      #region Computer, Tablet
      AboutContainer = AddDivRow("AboutItem Header", header1, header2);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRow("AboutItem Body", body1, body2);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRow("AboutItem Header", header3, header4);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRow("AboutItem Body", body3, body4);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRow("AboutItem Header", header5, header6);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRow("AboutItem Body", body5, body6);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRow("AboutItem Header", header7, header8);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRow("AboutItem Body", body7, body8);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRow("AboutItem Header", header9, header10);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRow("AboutItem Body", body9, body10);
      AboutPage.Controls.Add(AboutContainer);
      #endregion Computer, Tablet
    }
    else
    {
      #region Smartphone
      AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header1, body1);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header2, body2);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header3, body3);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header4, body4);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header5, body5);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header6, body6);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header7, body7);
      AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header8, body8);
      AboutPage.Controls.Add(AboutContainer);

      //AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header9, body9);
      //AboutPage.Controls.Add(AboutContainer);

      AboutContainer = AddDivRowMobile("AboutItemMobile AboutHeaderMobile", "AboutItemMobile AboutBodyMobile", header10, body10);
      AboutPage.Controls.Add(AboutContainer);
      #endregion Smartphone
    }

    ContentPlaceHolder.Controls.Add(AboutPage);
  }

  private Panel AddDivRow(string classNameItem, string ItemValue1, string ItemValue2)
  {
    Panel AboutContainer, AboutItem;
    Label AboutLabel;

    AboutContainer = new Panel();
    AboutContainer.Attributes.Add("class", "AboutContainer");

    AboutItem = new Panel();
    AboutItem.Attributes.Add("class", classNameItem);
    AboutLabel = new Label(); AboutLabel.Text = ItemValue1;
    AboutItem.Controls.Add(AboutLabel);
    AboutContainer.Controls.Add(AboutItem);

    AboutItem = new Panel();
    AboutItem.Attributes.Add("class", classNameItem);
    AboutLabel = new Label(); AboutLabel.Text = ItemValue2;
    AboutItem.Controls.Add(AboutLabel);
    AboutContainer.Controls.Add(AboutItem);

    return AboutContainer;
  }

  private Panel AddDivRowMobile(string classNameHeader, string classNameBody, string ItemValueHeader, string ItemValueBody)
  {
    System.Web.UI.WebControls.Panel AboutContainer, AboutItem;
    System.Web.UI.WebControls.Label AboutLabel;

    AboutContainer = new Panel();
    //AboutContainer.Attributes.Add("class", "AboutContainer");

    AboutItem = new Panel();
    AboutItem.Attributes.Add("class", classNameHeader);
    AboutLabel = new Label(); AboutLabel.Text = ItemValueHeader;
    AboutItem.Controls.Add(AboutLabel);
    AboutContainer.Controls.Add(AboutItem);

    AboutItem = new Panel();
    AboutItem.Attributes.Add("class", classNameBody);
    AboutLabel = new Label(); AboutLabel.Text = ItemValueBody;
    AboutItem.Controls.Add(AboutLabel);
    AboutContainer.Controls.Add(AboutItem);

    return AboutContainer;
  }

  private void SetLocalization()
  {
    header1 = GetElementLocalization("About_Header1", "You can listen and watch multimedia using Web browser");
    body1 = GetElementLocalization("About_Body1", "First step. You need to register to sign-in. Second step. Select multimedia source on the main page. Also, you can try Demo.");

    header2 = GetElementLocalization("About_Header2", "Settings are not needed to listening to and watching home multimedia");
    body2 = GetElementLocalization("About_Body2", "You need to run LiveMultimediaServer on the local computer. No additional configuration is required.");

    header3 = GetElementLocalization("About_Header3", "Access to home multimedia on your local computer");
    body3 = GetElementLocalization("About_Body3", "You need to download and run LiveMultimediaServer on your local computer. After this add multimedia files in the application.");

    header4 = GetElementLocalization("About_Header4", "Playlists");
    body4 = GetElementLocalization("About_Body4", "You can manage playlists from different sources: from home, clouds, social networks.");

    header5 = GetElementLocalization("About_Header5", "Access to multimedia in cloud storages");
    body5 = GetElementLocalization("About_Body5", "If you store your multimedia files in Microsoft OneDrive or GoogleDrive, you can access to the multimedia.");

    header6 = GetElementLocalization("About_Header6", "Available platforms for listening and watching multimedia");
    body6 = GetElementLocalization("About_Body6", "You can use any platform (Windows, iOS, Andriod, Linux, etc.) with a Web browser with HTML5.");

    header7 = GetElementLocalization("About_Header7", "Access to multimedia in Social networks");
    body7 = GetElementLocalization("About_Body7", "If you store your multimedia files in VKontakte, you can access to the multimedia.");

    header8 = GetElementLocalization("About_Header8", "Available platforms for LiveMutimediaServer run");
    body8 = GetElementLocalization("About_Body8", "For launch LiveMultimediaServer you need to download Microsoft dotNET 4.5 and above.");

    header9 = GetElementLocalization("About_Header9", "");
    body9 = GetElementLocalization("About_Body9", "");

    header10 = GetElementLocalization("About_Header10", "Available today");
    body10 = GetElementLocalization("About_Body10", "Home Library on your local computer. Microsoft OneDrive, GoogleDrive, VKontakte");
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
