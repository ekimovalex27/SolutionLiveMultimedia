using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace LiveMultimediaSite
{
  public partial class getwidthscreen : System.Web.UI.Page
    // Добавлено для Callback вызовов клиента:
  , System.Web.UI.ICallbackEventHandler
  {
    protected void Page_Load(object sender, EventArgs e)
    {
      var cbReference = Page.ClientScript.GetCallbackEventReference(this, "arg", "ReceiveServerData", "context");
      var callbackScript = "function CallServer(arg, context)" + "{ " + cbReference + ";}";
      Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "CallServer", callbackScript, true);
    }
    public void RaiseCallbackEvent(String eventArgument)
    {
      Session["ScreenResolution"] = eventArgument;
    }

    public String GetCallbackResult()
    {
      return null;
    }

  }
}