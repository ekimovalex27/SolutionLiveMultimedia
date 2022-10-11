using System;
using System.Collections.Generic;
using System.Linq;

using LiveMultimediaSite.LiveMultimediaService;

public partial class Download : System.Web.UI.Page
{
  #region Define vars

  private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

  #endregion Define vars

  protected void Page_Load(object sender, EventArgs e)
    {
      SetLocalization();
    }

  #region Localization

  private void SetLocalization()
  {
    this.LabelDownload.Text = GetElementLocalization("Download_LabelDownload", "Download or Run");
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

  #endregion Localization
}