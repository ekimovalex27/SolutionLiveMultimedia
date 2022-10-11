using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web;
using System.Web.SessionState;
using System.IO;
using System.ServiceModel;

#if (DEBUG==true)
using LiveMultimediaHttpHandler.LiveMultimediaServiceReferenceIIS;
#endif

#if (DEBUG==false)
using LiveMultimediaHttpHandler.LiveMultimediaServiceReferenceWeb;
#endif
using LiveMultimediaServiceConnection;

namespace LiveMultimediaHttpHandler
{
  public class GetMultimediaHttpHandler : IHttpHandler//, IReadOnlySessionState //, IRequiresSessionState
    {
      private LiveMultimediaServiceConnection.LiveMultimediaServiceConnection LiveConnection = new LiveMultimediaServiceConnection.LiveMultimediaServiceConnection();

      public void ProcessRequest(HttpContext context)
      {
        HttpRequest Request = context.Request;
        HttpResponse Response = context.Response;
        HttpSessionState Session = context.Session;

        string UserToken; string MultimediaFileGUID;

        string GUID = Request.AppRelativeCurrentExecutionFilePath.Substring(2).Split(new Char[] { '.' })[0];
        UserToken = GUID.Substring(0, 36);
        MultimediaFileGUID = GUID.Substring(36);

        //UserToken = Session["UserToken"].ToString();
        //MultimediaFileGUID = Session["MultimediaFileGUID"].ToString();

        long MultimediaFileLength = -1;
        bool IsFirstFlush = true;

        byte[] MultimediaFileBuffer = new byte[65536];
        bool IsStopTransfer = false;
        MemoryStream ms;

        //if (Session["UserToken"] == null) Session["UserToken"] = "";
        //UserToken = Session["UserToken"].ToString();

        if (UserToken == "")
        {
          Session["PrevPage"] = "LiveMultimedia.aspx";
          Response.Redirect("~/Default.aspx", true);
        }
     
        Response.ClearHeaders();      
        Response.ClearContent();      
        Response.Clear();

      ////Response.ContentType = "audio/mp3";
      ////Response.ContentType = "audio/ogg";
      Response.ContentType = "audio/mpeg";
      Response.AppendHeader("Connection", "keep-alive");        
      Response.BufferOutput = true;

      using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
      {
        LiveMultimedia.Open();

        IsStopTransfer = false;
        while (IsStopTransfer == false && Response.IsClientConnected == true)
        {
          IsStopTransfer = LiveMultimedia.RemoteGetMultimediaFilebyUserToken(UserToken, MultimediaFileGUID, ref MultimediaFileBuffer, ref MultimediaFileLength);
          if (MultimediaFileBuffer != null)
          {
            if (IsFirstFlush == true)
            {
              IsFirstFlush = false;
              Response.AddHeader("content-length", MultimediaFileLength.ToString());
            }
            ms = new MemoryStream(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length, true);
            //if (Response.IsClientConnected == true)
            //{
            //  ms.CopyTo(Response.OutputStream);
            //  Response.Flush();
            //}
            if (Response.IsClientConnected == true)
            {
              ms.CopyTo(Response.OutputStream);
              if (Response.IsClientConnected == true)
              {
                Response.Flush();
              }
              else
              {
                IsStopTransfer = true;
              }              
            }
            else
            {
              IsStopTransfer = true;
            }

            //try
            //{
            //  ms.CopyTo(Response.OutputStream);
            //  Response.Flush();
            //}
            //catch (Exception)
            //{             
            //}
          }
        }        
      }
      Response.Close();
      Response.End();

//---------------------------------------------
        //Response.ClearHeaders();
        //Response.ClearContent();
        //Response.Clear();

        ////Response.ContentType = "audio/mp3";    
        //Response.ContentType = "audio/mpeg";
        ////Response.ContentType = "application/octet-stream";  
        ////Response.AddHeader("Content-Disposition", "inline; filename=qwe.mp3");
        ////Response.AddHeader("content-disposition", "inline; filename=qwe.mp3");
        ////Response.AddHeader("content-disposition", "inline;Filename=\"Picture.mp3\"");
        ////Response.AddHeader("Content-Disposition", "attachment; filename=qwe");
        //Response.AddHeader("Connection", "keep-alive");
        ////Response.AddHeader("Accept-ranges:", "bytes");
        //Response.BufferOutput = true;
        ////Response.BufferOutput = false;          

        ////WriteLog(UserToken, "GetMultimedia.aspx Page_Load", "Response.ContentType=" + Response.ContentType.ToString());
        ////MultimediaFileGUID = Session["MultimediaFileGUID"].ToString();
        ////WriteLog(UserToken, "GetMultimedia.aspx Page_Load", "MultimediaFileGUID=" + MultimediaFileGUID);

        //using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        //{
        //  LiveMultimedia.Open();

        //  IsStopTransfer = false;
        //  while (IsStopTransfer == false)
        //  {
        //    IsStopTransfer = LiveMultimedia.RemoteGetMultimediaFilebyUserToken(UserToken, MultimediaFileGUID, ref MultimediaFileBuffer, ref MultimediaFileLength);
        //    if (MultimediaFileBuffer != null)
        //    {
        //      if (IsFirstFlush == true)
        //      {
        //        IsFirstFlush = false;
        //        Response.AddHeader("content-length", MultimediaFileLength.ToString());
        //      }
        //      ms = new MemoryStream(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length, true);
        //      if (Response.IsClientConnected == true)
        //      {
        //        ms.CopyTo(Response.OutputStream);
        //        Response.Flush();
        //      }
        //      else
        //      {
        //        IsStopTransfer = true;
        //      }
        //    }
        //  }    

        //}

//---------------------------------------------
        //using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(binding, endpoint))
        //{
        //  LiveMultimedia.Open();
        //  IsStopTransfer = LiveMultimedia.RemoteGetMultimediaFilebyUserToken(UserToken, MultimediaFileGUID, ref MultimediaFileBuffer, ref IsReadyClient, ref MultimediaFileLength);
        //  Count1++;

        //  if (IsReadyClient == true)
        //  {
        //    IsFirstFlush = false;
        //    Response.AddHeader("content-length", MultimediaFileLength.ToString());
        //    Response.BufferOutput = false;

        //    ms = new MemoryStream(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length, true);
        //    //while (Response.IsClientConnected == false)
        //    //{
        //    //  System.Threading.Thread.Sleep(100);
        //    //}
        //    ms.CopyTo(Response.OutputStream);
        //    //Response.Flush();
        //    Count2++;
        //  }

        //  while (IsStopTransfer == false)
        //  {
        //    IsStopTransfer = LiveMultimedia.RemoteGetMultimediaFilebyUserToken(UserToken, MultimediaFileGUID, ref MultimediaFileBuffer, ref IsReadyClient, ref MultimediaFileLength);
        //    Count1++;

        //    if (IsReadyClient == true)
        //    {
        //      if (IsFirstFlush == true)
        //      {
        //        IsFirstFlush = false;
        //        Response.AddHeader("content-length", MultimediaFileLength.ToString());
        //        Response.BufferOutput = false;
        //      }

        //      ms = new MemoryStream(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length, true);
        //      //while (Response.IsClientConnected == false)
        //      //{
        //      //  System.Threading.Thread.Sleep(100);
        //      //}
        //      ms.CopyTo(Response.OutputStream);
        //      //Response.Flush();
        //      Count2++;
        //    }
        //  }
        //}

//---------------------------

        //using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(binding, endpoint))
        //{
        //  LiveMultimedia.Open();
        //  IsStopTransfer = LiveMultimedia.RemoteGetMultimediaFilebyUserToken(UserToken, MultimediaFileGUID, ref MultimediaFileBuffer, ref IsReadyClient, ref MultimediaFileLength);
        //  Count1++;
        //}

        //if (IsReadyClient == true)
        //{
        //  IsFirstFlush = false;
        //  Response.AddHeader("content-length", MultimediaFileLength.ToString());
        //  Response.BufferOutput = false;

        //  ms = new MemoryStream(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length, true);
        //  //while (Response.IsClientConnected == false)
        //  //{
        //  //  System.Threading.Thread.Sleep(100);
        //  //}
        //  ms.CopyTo(Response.OutputStream);
        //  //Response.Flush();
        //  Count2++;
        //    //WriteLog(UserToken, "GetMultimedia.aspx Page_Load", "IsReadyClient=" + IsReadyClient.ToString());
        //}

        //while (IsStopTransfer == false)
        //{
        //  using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(binding, endpoint))
        //  {
        //    LiveMultimedia.Open();
        //    IsStopTransfer = LiveMultimedia.RemoteGetMultimediaFilebyUserToken(UserToken, MultimediaFileGUID, ref MultimediaFileBuffer, ref IsReadyClient, ref MultimediaFileLength);
        //    Count1++;
        //  }
        //  if (IsReadyClient == true)
        //  {
        //    if (IsFirstFlush == true)
        //    {
        //      IsFirstFlush = false;
        //      Response.AddHeader("content-length", MultimediaFileLength.ToString());
        //      Response.BufferOutput = false;              
        //    }

        //    ms = new MemoryStream(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length, true);
        //    //while (Response.IsClientConnected == false)
        //    //{
        //    //  System.Threading.Thread.Sleep(100);
        //    //}
        //    ms.CopyTo(Response.OutputStream);
        //    //Response.Flush();
        //    Count2++;
        //  }
        //}

        //Response.Close();
        //Response.End();
          //WriteLog(Session["UserToken"].ToString(), "GetMultimediaHttpHandler.aspx Page_Load", "Stop");
      }

      public bool IsReusable
      {
        get
        {
          return false;
        }
      }

      private void WriteLog(string UserToken, string Procedure, string Message)
      {
        using (LiveMultimediaServiceClient LiveMultimedia = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          LiveMultimedia.Open();
          LiveMultimedia.WriteLog(UserToken, 2, "www", Procedure, Message);
        }
      }

    }
}
