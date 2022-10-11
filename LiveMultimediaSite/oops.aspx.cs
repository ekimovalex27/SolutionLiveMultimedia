using System;
using System.Collections.Generic;
using System.Linq;

using LiveMultimediaSite.LiveMultimediaService;

namespace LiveMultimediaSite
{
  public partial class oops : System.Web.UI.Page
  {
    private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();
    //private LiveMultimediaLibrary.LiveMultimediaLibrary Utilites = new LiveMultimediaLibrary.LiveMultimediaLibrary();

    protected void Page_Load(object sender, EventArgs e)
    {
      this.TextBoxDebug.Visible = false;

      SetLocalization();

#if (DEBUG)
      ///string UserToken;
      var UserToken = Session["UserToken"] as string;

      //if (Session["UserToken"] != null)
      //  UserToken = Session["UserToken"].ToString();
      //else
      //  UserToken = "EMPTY";

      try
      {
        var s = Session["ErrorMessage"] as Exception;
        if (s != null)
        {
          this.TextBoxDebug.Text = "UserToken=" + UserToken ?? "EMPTY";
          this.TextBoxDebug.Text += "\n, Exception=" + s.ToString() ?? "";
        }
        else
          this.TextBoxDebug.Text = "No error. It is strange...";

        this.TextBoxDebug.Visible = true;
      }
      catch (Exception)
      {
      }
#endif
    }    

    private void SetLocalization()
    {
      this.LabelMessage1.Text = GetElementLocalization("ErrorPage_LabelMessage1", "Something happened at the site");
      this.LabelMessage2.Text = GetElementLocalization("ErrorPage_LabelMessage2", "Try again or go to the home page");
    }

    public string GetElementLocalization(string ElementName, string DefaultElementValue)
    {
      string ElementValue;

      try
      {
        var LanguageInfoItem = Session["LanguageInfo"] as LanguageInfo;
        var ListLocalization = Application[LanguageInfoItem.Language] as List<LocalizationElement>;

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

}