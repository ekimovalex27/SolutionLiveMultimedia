using System;
using System.Web;

using System.Threading.Tasks;
using System.Collections.Specialized;
using System.IO;
using System.Diagnostics;

namespace JetSAS
{
  public class GetMultimedia : HttpTaskAsyncHandler
  {
    #region Define vars
    
    LiveMultimediaService LiveMultimediaClient = new LiveMultimediaService();

    private readonly string AccountKey = "***";

    private HttpRequest Request;
    private HttpResponse Response;
    private HttpApplication Application;

    string Id, uniqueMultimediaRequest;
    int ServerSpeed;

    long Range1, Range2; string MimeType;
    #endregion Define vars

    public override async Task ProcessRequestAsync(HttpContext context)
    {
      try
      {
        Request = context.Request;
        Response = context.Response;
        Application = context.ApplicationInstance;

        try
        {
          Id = Request.QueryString["id"] as string;
          uniqueMultimediaRequest = Request.QueryString["UMR"] as string;

          if (LiveMultimediaLibrary.CheckGoodString(Id) && LiveMultimediaLibrary.CheckGoodString(uniqueMultimediaRequest))
          {
            string IdJob = await PrepareOutput();
            await WriteMultimediaBuffer(IdJob);
          }
        }
        catch (Exception ex)
        {
        }

        Application.CompleteRequest();
      }
      catch (Exception ex)
      {
      }
    }
  
    private async Task<string> PrepareOutput()
    {
      #region Define vars
      string IdJob = null;
      long MultimediaFileLength = -1;      
      string textError = "";      
      bool IsHTTP_RANGE;

      StringDictionary ListAttributes;
      #endregion Define vars

      #region HTTP_RANGE
      var ServerVariables = Request.ServerVariables["HTTP_RANGE"];
      if (!LiveMultimediaLibrary.CheckGoodString(ServerVariables))
      {
        IsHTTP_RANGE = false;
        Range1 = 0; Range2 = -1;
      }
      else
      {
        IsHTTP_RANGE = true;

        try
        {
          var aServerVariables = ServerVariables.Split(new Char[] { '=' });
          var Range = aServerVariables[1];
          var aRange = Range.Split(new Char[] { '-' });

          if (aRange.Length == 2)
          {
            if (!Int64.TryParse(aRange[0], out Range1)) Range1 = 0;
            if (!Int64.TryParse(aRange[1], out Range2)) Range2 = -1;
          }
          else
          {
            Range1 = 0; Range2 = -1;
          }
        }
        catch (Exception)
        {
          Range1 = 0; Range2 = -1;
        }
      }
      #endregion HTTP_RANGE

      #region Try
      try
      {
        #region Create Multimedia Job
        var returnValue = await LiveMultimediaClient.RemoteCreateMultimediaJob(AccountKey, Id, uniqueMultimediaRequest);
        ListAttributes = returnValue.Item1;
        textError = returnValue.Item2;

        if (LiveMultimediaLibrary.CheckGoodString(textError)) throw new ApplicationException(textError);

        IdJob = ListAttributes["IdJob"];
        MimeType = ListAttributes["MimeType"];
        MultimediaFileLength = Convert.ToInt64(ListAttributes["MultimediaFileLength"]);
        ServerSpeed = Convert.ToInt32(ListAttributes["ServerSpeed"]);
        #endregion Create Multimedia Job

        #region Recalc Range
        if (Range2 == -1)
        {
          Range2 = MultimediaFileLength - 1;
        }
        #endregion Recalc Range

        #region Append headers
        Response.AppendHeader("Connection", "Keep-Alive");
        Response.AppendHeader("Accept-Ranges", "bytes");

        if (MultimediaFileLength < long.MaxValue) //MaxValue - without append header "Content-Length"
        {
          if (!IsHTTP_RANGE)
          {
            Response.AppendHeader("Content-Length", MultimediaFileLength.ToString());
            Response.ContentType = MimeType;
            Response.StatusCode = 200;
          }
          else
          {
            Response.AppendHeader("Content-Length", (Range2 - Range1 + 1).ToString());
            Response.AppendHeader("Content-Range", "bytes " + Range1.ToString() + "-" + Range2.ToString() + "/" + MultimediaFileLength.ToString());
            Response.ContentType = MimeType;
            Response.StatusCode = 206;
          }
        }
        #endregion Append headers

        Response.BufferOutput = true;
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
      }
      #endregion Catch

      return IdJob;
    }

    /* Рабочая версия */
    //private async Task WriteMultimediaBuffer(string IdJob)
    //{
    //  #region Define vars
    //  int lengthBufferChunk;
    //  #endregion Define vars

    //  #region Try
    //  try
    //  {
    //    #region Define size of buffer of multimedia
    //    int lengthBuffer;
    //    var typeMultimedia=MimeType.Split(new char[] { '/' })[0].ToLower();
    //    switch (typeMultimedia)
    //    {
    //      case "audio":
    //        lengthBuffer = 131072;
    //        lengthBufferChunk = 1024;
    //        break;
    //      case "video":
    //        lengthBuffer = ServerSpeed * 1024 / 8; // ServerSpeed have got in KiloBits per seconds
    //        lengthBufferChunk = 16384;
    //        lengthBufferChunk = 1024;
    //        break;
    //      default:
    //        lengthBuffer = ServerSpeed * 1024 / 8; // ServerSpeed have got in KiloBits per seconds
    //        lengthBufferChunk = 8192;
    //        lengthBufferChunk = 1024;
    //        break;
    //    }
    //    var bufferMultimedia = new byte[lengthBuffer];

    //    #endregion Define size of buffer of multimedia

    //    #region Write stream multimedia
    //    using (var MultimediaStream = await LiveMultimediaClient.GetMultimedia(AccountKey, IdJob, Range1, Range2))
    //    {
    //      int readCount;

    //      #region Read first small buffer for video
    //      if (Range1 == 0 && typeMultimedia== "video")
    //      {
    //        readCount = await MultimediaStream.ReadAsync(bufferMultimedia, 0, 131072);
    //        using (MemoryStream chunkStream = new MemoryStream(bufferMultimedia, 0, readCount))
    //        {
    //          #region Flush small chunks video
    //          byte[] chunkBufferVideo = new byte[8192];
    //          var readCountFirstBuffer = await chunkStream.ReadAsync(chunkBufferVideo, 0, chunkBufferVideo.Length);
    //          while (readCountFirstBuffer > 0)
    //          {                
    //            using (MemoryStream chunkFirstStream = new MemoryStream(chunkBufferVideo, 0, readCountFirstBuffer))
    //            {
    //              await chunkFirstStream.CopyToAsync(Response.OutputStream);
    //            }
    //            Response.Flush();                

    //            readCountFirstBuffer = await chunkStream.ReadAsync(chunkBufferVideo, 0, chunkBufferVideo.Length);
    //          }
    //          #endregion Flush small chunks video
    //        }
    //      }
    //      #endregion Read first small buffer for video

    //      #region Read multimedia buffers
    //      byte[] chunkBuffer = new byte[lengthBufferChunk];
    //      readCount = await MultimediaStream.ReadAsync(bufferMultimedia, 0, bufferMultimedia.Length);
    //      while (readCount > 0)
    //      {
    //        #region Flush small chunks
    //        using (MemoryStream chunkStream = new MemoryStream(bufferMultimedia, 0, readCount))
    //        {              
    //          var readCountBuffer = await chunkStream.ReadAsync(chunkBuffer, 0, chunkBuffer.Length);
    //          while (readCountBuffer > 0)
    //          {
    //            using (MemoryStream chunkFirstStream = new MemoryStream(chunkBuffer, 0, readCountBuffer))
    //            {
    //              await chunkFirstStream.CopyToAsync(Response.OutputStream);
    //            }                
    //            Response.Flush();

    //            readCountBuffer = await chunkStream.ReadAsync(chunkBuffer, 0, chunkBuffer.Length);
    //          }              
    //        }
    //        #endregion Flush small chunks

    //        readCount = await MultimediaStream.ReadAsync(bufferMultimedia, 0, bufferMultimedia.Length);            
    //      }
    //      #endregion Read multimedia buffers
    //    }
    //    #endregion Write stream multimedia
    //  }

    //  //var ms= await LiveMultimediaClient.GetMultimediaTest(IdJob, R1, R2);
    //  //using (FileStream fs = new FileStream(@"C:\LiveMultimediaDemo\audio\Тест.mp3", FileMode.Create, FileAccess.Write, FileShare.Write, 8, true))
    //  //{
    //  //  await ms.CopyToAsync(fs);
    //  //}
    //  #endregion Try

    //  #region Catch
    //  catch (HttpException)
    //  {
    //  }
    //  catch (Exception)
    //  {
    //  }
    //  #endregion Catch
    //}

    private async Task WriteMultimediaBuffer(string IdJob)
    {
      #region Define vars
      int lengthBufferChunk;
      #endregion Define vars

      #region Try
      try
      {
        #region Define size of buffer of multimedia
        int lengthBuffer;
        var typeMultimedia = MimeType.Split(new char[] { '/' })[0].ToLower();
        switch (typeMultimedia)
        {
          case "audio":
            lengthBuffer = 131072;
            lengthBufferChunk = 1024;
            break;
          case "video":
            lengthBuffer = ServerSpeed * 1024 / 8; // ServerSpeed have got in KiloBits per seconds
            lengthBufferChunk = 16384;
            lengthBufferChunk = 1024;
            break;
          default:
            lengthBuffer = ServerSpeed * 1024 / 8; // ServerSpeed have got in KiloBits per seconds
            lengthBufferChunk = 8192;
            lengthBufferChunk = 1024;
            break;
        }
        var bufferMultimedia = new byte[lengthBuffer];
        #endregion Define size of buffer of multimedia

        #region Write stream multimedia
        using (var MultimediaStream = await LiveMultimediaClient.GetMultimedia(AccountKey, IdJob, Range1, Range2))
        {
          int readCount;

          #region Read first small buffer for video
          if (Range1 == 0 && typeMultimedia == "video")
          {
            readCount = await MultimediaStream.ReadAsync(bufferMultimedia, 0, 131072);
            using (MemoryStream chunkStream = new MemoryStream(bufferMultimedia, 0, readCount))
            {
              #region Flush small chunks video
              byte[] chunkBufferVideo = new byte[8192];
              var readCountFirstBuffer = await chunkStream.ReadAsync(chunkBufferVideo, 0, chunkBufferVideo.Length);
              while (readCountFirstBuffer > 0)
              {
                using (MemoryStream chunkFirstStream = new MemoryStream(chunkBufferVideo, 0, readCountFirstBuffer))
                {
                  await chunkFirstStream.CopyToAsync(Response.OutputStream);
                }
                Response.Flush();

                readCountFirstBuffer = await chunkStream.ReadAsync(chunkBufferVideo, 0, chunkBufferVideo.Length);
              }
              #endregion Flush small chunks video
            }
          }
          #endregion Read first small buffer for video

          #region Read multimedia buffers
          byte[] chunkBuffer = new byte[lengthBufferChunk];

          readCount = await MultimediaStream.ReadAsync(bufferMultimedia, 0, bufferMultimedia.Length);

          while (readCount > 0)
          {
            #region Что-то
            //var taskReadBuffer = Task.Run<byte[]>(async () =>
            //{
            //  byte[] newbuffer;

            //  var nextBufferMultimedia = new byte[lengthBuffer];
            //  var nextReadCount = await MultimediaStream.ReadAsync(nextBufferMultimedia, 0, nextBufferMultimedia.Length);
            //  if (nextReadCount > 0)
            //  {
            //    newbuffer = new byte[nextReadCount];
            //    nextBufferMultimedia.CopyTo(newbuffer, 0);
            //  }
            //  else
            //  {
            //    newbuffer = null;
            //  }

            //  return newbuffer;
            //}
            //);
            //t.Restart();
            #endregion Что-то

            #region Flush small chunks
            using (var chunkStream = new MemoryStream(bufferMultimedia, 0, readCount))
            {
              var readCountBuffer = await chunkStream.ReadAsync(chunkBuffer, 0, chunkBuffer.Length);
              while (readCountBuffer > 0)
              {
                using (var chunkFirstStream = new MemoryStream(chunkBuffer, 0, readCountBuffer))
                {
                  await chunkFirstStream.CopyToAsync(Response.OutputStream);
                }
                Response.Flush();

                readCountBuffer = await chunkStream.ReadAsync(chunkBuffer, 0, chunkBuffer.Length);
              }
            }
            #endregion Flush small chunks

            #region Что-то
            //watchNext.Restart();
            //while (!taskReadBuffer.IsCompleted)
            //{
            //  await Task.Delay(10);
            //}
            //watchNext.Stop(); Debug.WriteLine("watchNext={0}", watchNext.ElapsedMilliseconds);

            //if (taskReadBuffer.IsCompleted)
            //{
            //  readCount = taskReadBuffer.Result.Length;
            //  if (readCount > 0)
            //  {
            //    taskReadBuffer.Result.CopyTo(bufferMultimedia, 0);
            //  }
            //}
            #endregion Что-то

            readCount = await MultimediaStream.ReadAsync(bufferMultimedia, 0, bufferMultimedia.Length);
          }
          #endregion Read multimedia buffers
        }
        #endregion Write stream multimedia

        #region Test write stream multimedia to file
        //var ms = await LiveMultimediaClient.GetMultimedia(AccountKey, IdJob, Range1, Range2);
        //using (FileStream fs = new FileStream(@"D:\LiveMultimediaDemo\Тест.mp3", FileMode.Create, FileAccess.Write, FileShare.Write, 8, true))
        //{
        //  await ms.CopyToAsync(fs);
        //}
        #endregion Test write stream multimedia to file
      }
      #endregion Try

      #region Catch
      catch (HttpException ex)
      {
      }
      catch (Exception ex)
      {
      }
      #endregion Catch
    }

    //private async Task<byte[]> GetNextBuffer(Stream MultimediaStream, byte[] nextBufferMultimedia)
    //{
    //  var taskReadBuffer = Task.Run<byte[]>(async () =>
    //  {
    //    var readCount = await MultimediaStream.ReadAsync(nextBufferMultimedia, 0, nextBufferMultimedia.Length);
    //    if (readCount <= 0) nextBufferMultimedia = null;
    //    return nextBufferMultimedia;
    //  }
    //  );

    //  //newbuffer = new byte[taskReadBuffer.Result.Length];
    //  //taskReadBuffer.Result.CopyTo(newbuffer, 0);
    //}

    private async Task WriteNextChunk(Stream MultimediaStream, int lengthBuffer, int lengthBufferChunk)
    {
      var bufferMultimedia = new byte[lengthBuffer];
      var readCount = await MultimediaStream.ReadAsync(bufferMultimedia, 0, bufferMultimedia.Length);
      while (readCount > 0)
      {
#region Flush small chunks
        using (MemoryStream chunkStream = new MemoryStream(bufferMultimedia, 0, readCount))
        {
          byte[] chunkBuffer = new byte[lengthBufferChunk];
          var readCountBuffer = await chunkStream.ReadAsync(chunkBuffer, 0, chunkBuffer.Length);
          while (readCountBuffer > 0)
          {
            using (MemoryStream chunkFirstStream = new MemoryStream(chunkBuffer, 0, readCountBuffer))
            {
              await chunkFirstStream.CopyToAsync(Response.OutputStream);
            }
            Response.Flush();

            readCountBuffer = await chunkStream.ReadAsync(chunkBuffer, 0, chunkBuffer.Length);
          }
        }
#endregion Flush small chunks

        readCount = await MultimediaStream.ReadAsync(bufferMultimedia, 0, bufferMultimedia.Length);
      }
    }

    public override bool IsReusable
    {
      get { return true; }
    }

    //// Оригинал
    //public bool IsReusable
    //{
    //  get
    //  {
    //    return false;
    //  }
    //}
  }
}