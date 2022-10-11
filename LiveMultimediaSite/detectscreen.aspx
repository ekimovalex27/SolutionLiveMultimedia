<script runat="server" language="C#">

  public void Page_Load(Object sender, EventArgs e)
  {
    if (Request.QueryString["action"] != null)
    {
      try
      {
        Session["ScreenResolution"] = Request.QueryString["res"] as string;
        Session["ScreenPixelsWidth"] = new Unit(Session["ScreenResolution"].ToString().Split(new Char[] { 'x' })[0]);
        Session["ScreenPixelsHeight"] = new Unit(Session["ScreenResolution"].ToString().Split(new Char[] { 'x' })[1]);
      }
      catch (Exception)
      {
        Session["ScreenResolution"] = "800x600";
        Session["ScreenPixelsWidth"] = new Unit("800");
        Session["ScreenPixelsHeight"] = new Unit("600");
      }

      try
      {
        Response.Redirect(Session["PrevPageForScreenResolution"].ToString(), false);
        HttpContext.Current.ApplicationInstance.CompleteRequest();
      }
      catch (Exception)
      {
        Response.Redirect("~/Default.aspx", false);
        HttpContext.Current.ApplicationInstance.CompleteRequest();
      }
    }
  }
</script>

<HTML><BODY>
<script lang="javascript"> 
  res = "&res=" + screen.availWidth + "x" + screen.availHeight + "&d=" + screen.colorDepth;
  top.location.href = "detectscreen.aspx?action=set" + res;
</script>
</BODY></HTML>