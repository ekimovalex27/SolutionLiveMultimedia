using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Data;
using System.Threading.Tasks;

using LiveMultimediaSite.LiveMultimediaService;

public partial class Language : System.Web.UI.Page
{
  #region Define vars

  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

  private string ServerAccountKey;
  private static bool IsMobileDevice;

  private List<LanguageInfo> ListLanguage;
  static string fromPage;

  #endregion Define vars

  protected void Page_Init(object sender, EventArgs e)
  {
    ServerAccountKey = Application["ServerAccountKey"] as string;    

    if (!Page.IsCallback)
    {
      IsMobileDevice = Convert.ToBoolean(Session["IsMobileDevice"]);
    }
  }

  protected async void Page_Load(object sender, EventArgs e)
  {   
    var NewLanguage=Page.Request.QueryString["Language"] as string;

    if (LiveMultimediaLibrary.CheckGoodString(NewLanguage))
    {
      Session["Language"] = NewLanguage;
      Response.Redirect(Session["fromPage"] as string, false);
      HttpContext.Current.ApplicationInstance.CompleteRequest();
      return;
    }

    var Language = Session["Language"] as string;
    ListLanguage = await GetListLanguages(Language);

    SetLocalization();

    int MaxCountColumn;

    if (!IsMobileDevice)
    {
      MaxCountColumn = 3;
      GridViewLanguage.CssClass = "LanguageList";
    }
    else
    {
      MaxCountColumn = 2;
      GridViewLanguage.CssClass = "LanguageListMobile";
    }
    
    GridViewLanguage.GridLines = GridLines.None;
    GridViewLanguage.ShowHeader = false;
    GridViewLanguage.AutoGenerateColumns = false;

    try
    {
      #region Load GridView

      TemplateField GridViewColumn;

      GridViewLanguage.Columns.Clear();

      DataTable TableLanguage;
      DataRow datarowLanguage;

      TableLanguage = new DataTable("TableLanguage");

      double doubleMaxCountRow = (double)(ListLanguage.Count + 1) / MaxCountColumn;
      int MaxCountRow = (int)doubleMaxCountRow;
      if (MaxCountRow != doubleMaxCountRow) MaxCountRow++;

      string ColumnName;
      for (int i = 0; i < MaxCountColumn; i++)
      {
        ColumnName = "ColumnLanguage" + i.ToString();

        TableLanguage.Columns.Add(new DataColumn(ColumnName));

        GridViewColumn = new TemplateField();
        GridViewColumn.ItemTemplate = new TemplateLanguageColumn(ListItemType.Item, ColumnName);
        GridViewLanguage.Columns.Add(GridViewColumn);
      }

      int CountRow = 0; int CountColumn = 0;
      string LanguageItem;
      foreach (var itemLanguageInfo in ListLanguage)
      {
        //LanguageItem = itemLanguageInfo.Language + "|" + itemLanguageInfo.NativeName;
        LanguageItem = itemLanguageInfo.Language + "|" + itemLanguageInfo.DisplayName;

        if (CountColumn == 0)
        {
          datarowLanguage = TableLanguage.NewRow();
          datarowLanguage[CountColumn] = LanguageItem;
          TableLanguage.Rows.Add(datarowLanguage);
        }
        else
        {
          datarowLanguage = TableLanguage.Rows[CountRow];
          datarowLanguage[CountColumn] = LanguageItem;
        }
        CountRow++;
        if (CountRow == MaxCountRow)
        {
          CountRow = 0;
          CountColumn++;
        }
      }

      GridViewLanguage.DataSource = TableLanguage;
      GridViewLanguage.DataBind();

      #endregion Load GridView
    }
    catch (Exception)
    {
    }
  }

  public class TemplateLanguageColumn : ITemplate
  {
    ListItemType _type;
    string _colName;

    public TemplateLanguageColumn(ListItemType type, string colname)
    {
      _type = type;
      _colName = colname;
    }

    void ITemplate.InstantiateIn(System.Web.UI.Control container)
    {
      if (_type == ListItemType.Item)
      {
        var HyperLinkLanguage = new HyperLink();
        HyperLinkLanguage.DataBinding += new EventHandler(item_DataBinding);
        
        if (!IsMobileDevice)
        {
          HyperLinkLanguage.CssClass = "LanguageItem";
        }
        else
        {
          HyperLinkLanguage.CssClass = "LanguageItemMobile";
        }
        
        container.Controls.Add(HyperLinkLanguage);
      }
    }

    void item_DataBinding(object sender, EventArgs e)
    {
      string SitePath = "~/" + "Language.aspx" + "?Language=";

      var HyperLinkLanguage = (HyperLink)sender;
      var container = (GridViewRow)HyperLinkLanguage.NamingContainer;
      object dataValue = DataBinder.Eval(container.DataItem, _colName);
      if (dataValue != DBNull.Value)
      {
        var aLanguageItem = dataValue.ToString().Split(new Char[] { '|' });

        HyperLinkLanguage.NavigateUrl = SitePath + aLanguageItem[0];
        HyperLinkLanguage.Text = aLanguageItem[1];        
      }
    }
  }

  private async Task<List<LanguageInfo>> GetListLanguages(string Language)
  {
    var ListLanguagesInfo = Application["ListLanguage" + Language] as List<LanguageInfo>;

    try
    {
      if (ListLanguagesInfo == null)
      {
        using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnValue = await LiveMultimediaService.GetLanguagesAsync(Language);
          if (LiveMultimediaLibrary.CheckGoodString(returnValue.Item2)) throw new ApplicationException(returnValue.Item2);
          ListLanguagesInfo = returnValue.Item1.ToList();
          Application["ListLanguage" + Language] = ListLanguagesInfo;
        }
      }
    }
    catch (Exception)
    {
      throw;
    }

    return ListLanguagesInfo;
  }

  private void SetLocalization()
  {
    GridViewLanguage.Caption = GetElementLocalization("Language_LabelTitle", "Select a language");
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
