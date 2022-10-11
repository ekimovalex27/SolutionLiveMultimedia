namespace LiveMultimediaREST
{
  using System;
  using System.Collections.Generic;
  using System.Text;

  using System.Threading;
  using System.Threading.Tasks;
  using System.Net;
  using System.Web.Script.Serialization;
  using System.Web;
  using System.Collections;
  using System.Collections.Concurrent;
  using System.Diagnostics;
  using System.IO;
  using System.Collections.Specialized;

  using LiveMultimediaOAuth;

  public class REST : IREST
  {
    #region Define vars

    private string apiUrl;
    private string AccessToken;
    private string RootFolder;
    private string filter;

    private static List<OAuthObjectFolder> ListObjectMultimediaFolder;
    private static List<OAuthObjectAudio> ListObjectMultimediaAudio;

    private static long usingResource = 0;

    ConcurrentBag<OAuthObjectFolder> BagObjectFolder = new ConcurrentBag<OAuthObjectFolder>();

    private string ObjectAudioName = "";
    private int ObjectAudioSize = 0;

    #endregion Define vars

    public REST(string ApiUrl, string AccessToken, string RootFolder="", string filter="")
    {
      bool IsNullOrEmptyApiUrl=string.IsNullOrEmpty(ApiUrl);
      bool IsNullOrEmptyAccessToken=string.IsNullOrEmpty(AccessToken);
      bool IsNullOrEmptyRootFolder = (RootFolder == null);

      if (!IsNullOrEmptyApiUrl && !IsNullOrEmptyAccessToken && !IsNullOrEmptyRootFolder)
      {
        this.AccessToken = AccessToken;
        this.apiUrl = ApiUrl;
        this.RootFolder = RootFolder;
        ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
        ListObjectMultimediaAudio=new List<OAuthObjectAudio>();

        if (string.IsNullOrEmpty(filter) || string.IsNullOrWhiteSpace(filter))
          this.filter = "";
        else
          this.filter = filter;
      }
      else
      {
        if (IsNullOrEmptyApiUrl && IsNullOrEmptyAccessToken)
          throw new ArgumentNullException("AccessToken and ApiUrl", "AccessToken and ApiUrl is null or empty");
        else if (IsNullOrEmptyApiUrl)
          throw new ArgumentNullException("ApiUrl", "ApiUrl is null or empty");
        else if (IsNullOrEmptyAccessToken)
          throw new ArgumentNullException("AccessToken", "AccessToken is null or empty");
        else if (IsNullOrEmptyRootFolder)
          throw new ArgumentNullException("RootFolder", "RootFolder is null");
      }
    }

    private bool IsUsing()
    {
      long CurrentUsing = Interlocked.Read(ref usingResource);
      if (CurrentUsing > 0 )
        return true;
      else
        return false;
    }

    #region Request Object Folder OneDrive

    private string GetFacetOneDrive(IDictionary<string, object> multimediaObject)
    {
      if (multimediaObject.ContainsKey("folder")) return "Folder";
      if (multimediaObject.ContainsKey("audio")) return "Audio";
      if (multimediaObject.ContainsKey("video")) return "Video";
      if (multimediaObject.ContainsKey("photo")) return "Image";
      if (multimediaObject.ContainsKey("image")) return "Image";

      return "Unsupported";
    }

    public async Task<List<OAuthObjectFolder>> GetItemsByNoneOneDriveAsync(string IdItem)
    {
      #region Define vars
      string query;
      string requestUrl;
      #endregion Define vars

      try
      {
        #region Define Root folder
        if (!JetSASLibrary.CheckGoodString(IdItem))
          RootFolder = "drive/root/children";
        else
          RootFolder = "drive/items/" + IdItem + "/children";        
        #endregion Define Root folder

        #region Define filter
        if (filter != "")
          query = string.Format("folder ne null or {0} ne null", filter);
        else
          query = "";
        #endregion Define filter

        requestUrl = apiUrl + RootFolder + "?filter=" + HttpUtility.UrlEncode(query) + "&access_token=" + AccessToken;

        string ResponseString;
        using (var wc = new WebClient())
        {
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        var MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("value"))
        {
          var localData = (ArrayList)MultimediaFolder["value"];

          ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
          OAuthObjectFolder multimediaObject;

          foreach (IDictionary<string, object> file in localData)
          {
            multimediaObject = new OAuthObjectFolder();
            
            multimediaObject.Id = file["id"].ToString();
            multimediaObject.Name = file["name"].ToString();
            multimediaObject.Type = GetFacetOneDrive(file);

            ListObjectMultimediaFolder.Add(multimediaObject);
          }
        }
      }
      catch (Exception ex)
      {
        ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
      }

      return ListObjectMultimediaFolder;
    }

    public List<OAuthObjectFolder> GetListFolderGroupOneDrive()
    {
      try
      {
        var cancellationToken = new CancellationToken();
        //await Task.Factory.StartNew(() => ReadFolder(RootFolder, cancellationToken), TaskCreationOptions.LongRunning);
        ReadFolder(RootFolder, cancellationToken);
      }
      catch (Exception)
      {
      }

      OAuthObjectFolder objectFolder;
      ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
      foreach (var itemBag in BagObjectFolder)
      {
        objectFolder = new OAuthObjectFolder();

        ListObjectMultimediaFolder.Add(objectFolder);
      }

      return ListObjectMultimediaFolder;
    }

    private async void ReadFolder(string FolderName, CancellationToken ct)
    {
      try
      {
        if (ct.IsCancellationRequested) return;

        var requestUrl = apiUrl + FolderName + "?filter=folder%20ne%20null&access_token=" + AccessToken;
        //var requestUrl = apiUrl + FolderName + "?select=id,name&filter=folder%20ne%20null&access_token=" + AccessToken;

        byte[] b;
        using (var wc = new WebClient())
        {
          //b = await wc.DownloadDataTaskAsync(requestUrl);
          b = wc.DownloadData(requestUrl);
        }

        var ResponseString = Encoding.UTF8.GetString(b);

        var MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("value"))
        {
          var localData = (System.Collections.ArrayList)MultimediaFolder["value"];

          #region Convert to ConcurrentBag
          ConcurrentBag<string> bag = new ConcurrentBag<string>();
          foreach (IDictionary<string, object> file in localData)
          {
            Debug.WriteLine("id={0}, name={1}", file["id"], file["name"]);

            var itemPath = "drive/items/" + file["id"].ToString() + "/children";
            bag.Add(itemPath);
          }
          #endregion Convert to ConcurrentBag

          ConcurrentBag<Task> ListTask = new ConcurrentBag<Task>();

          Parallel.ForEach(bag, itemPath =>
          {            
            //var newTask = Task.Run(async () =>
            var newTask = Task.Run(() =>
            {
              if (ct.IsCancellationRequested) return;

              //Рекурсивный вызов для папки
              Task.Factory.StartNew(() => ReadFolder(itemPath, ct), TaskCreationOptions.AttachedToParent);

              //Проверка наличия audio в папке
              //Task.Factory.StartNew(() => ReadFiles(itemPath, ct), TaskCreationOptions.AttachedToParent);
            }
            );

            ListTask.Add(newTask);
          }
          );

          Task.WaitAll(ListTask.ToArray());
        }

        if (ct.IsCancellationRequested) return;
      }
      catch (Exception ex)
      {
      }
    }
    private async void ReadFiles(string FolderName, CancellationToken ct)
    {
      //var requestUrl = apiUrl + FolderName + "?select=id&filter=audio%20ne%20null&access_token=" + AccessToken;
      //var requestUrl = apiUrl + FolderName + "?select=name&access_token=" + AccessToken;
      //var requestUrl = apiUrl + FolderName + "?access_token=" + AccessToken;
      var requestUrl = apiUrl + FolderName + "?q=128&access_token=" + AccessToken;

      byte[] b;
      using (var wc = new WebClient())
      {
        b = await wc.DownloadDataTaskAsync(requestUrl);
      }

      var ResponseString = Encoding.UTF8.GetString(b);

      var MultimediaFolder = new Dictionary<string, object>();
      MultimediaFolder = deserializeJsonData(ResponseString);

      if (MultimediaFolder.ContainsKey("value"))
      {
        var localData = (System.Collections.ArrayList)MultimediaFolder["value"];
        if (localData.Count>0)
        {

        }
      }
    }

    //private void ReadFiles(string FolderName, CancellationToken ct)
    //{
    //  string ContentCountFiles;
    //  //MultimediaFile mf;
    //  string CurrentExtension;

    //  Parallel.ForEach(ListMultimediaFileExtension, Extension =>
    //  {
    //    //May by this "TRY" is not nesessary
    //    try //Check access to files in FolderName by extension
    //    {
    //      if (ct.IsCancellationRequested) return;
    //      IEnumerable<string> ListFiles = Directory.EnumerateFiles(FolderName, "*." + Extension, System.IO.SearchOption.TopDirectoryOnly);

    //      //Parallel.ForEach(ListFiles, FullPath => // Проблема со "слабым" процессором- превышение кол-ва потоков. Здесь замена на "foreach" попытка уменьшить нагрузку
    //      foreach (var FullPath in ListFiles)
    //      {
    //        try //Check access to file
    //        {
    //          if (ct.IsCancellationRequested) return;

    //          //Commented beacause it is long works
    //          //FileInfo CheckAccessFile = new FileInfo(FullName);
    //          //FileAttributes CheckFA = CheckAccessFile.Attributes;
    //          // OR
    //          //if (File.Exists(FullPath))
    //          //{
    //          //OK. File is available for adding     

    //          #region This block because EnumerateFiles get all extension after end our Extension
    //          var aPartName = FullPath.Split(new Char[] { '.' });
    //          if (aPartName.Length > 0)
    //            CurrentExtension = aPartName[aPartName.Length - 1];
    //          else
    //            CurrentExtension = "";
    //          #endregion This block because EnumerateFiles get all extension after end our Extension

    //          if (CurrentExtension == Extension)
    //          {
    //            mf = new MultimediaFile();
    //            mf.FullPath = FullPath;
    //            BagObjectFolder.Add(mf);
    //            ContentCountFiles = Interlocked.Increment(ref usingResource).ToString();
    //            this.lblStatus2SearchFilesResult.Dispatcher.InvokeAsync((Action)(() => this.lblStatus2SearchFilesResult.Content = ContentCountFiles));
    //          }
    //          //};

    //        }
    //        catch (Exception) { } //Bad access to file
    //      }
    //      //);
    //    }
    //    catch (Exception) { } // Bad access to directory
    //  }
    //  );
    //}

    public List<OAuthObjectFolder> GetListMultimediaFolderOneDrive()
    {
      RequestMultimediaFolderOneDrive(RootFolder);
      RequestMultimediaNoFolder(RootFolder);
      do
      {
        Thread.Sleep(500);
      } while (IsUsing());
      return ListObjectMultimediaFolder;
    }

    private void RequestMultimediaFolderOneDrive(string RESTFolder)
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string requestUrl = apiUrl + RESTFolder + "?filter=folders&access_token=" + AccessToken;
        WebClient wc = new WebClient();
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadDataCallbackOneDrive);
        wc.DownloadDataAsync(new Uri(requestUrl));
      }
      catch (Exception)
      {
      }

      #region Рассмотреть
      //byte[] b =await wc.DownloadDataTaskAsync(new Uri(requestUrl));
      //string ResponseString = Encoding.UTF8.GetString(b);

      //Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
      //MultimediaFolder = deserializeJsonData(ResponseString);

      //if (MultimediaFolder.ContainsKey("data"))
      //{
      //  System.Collections.ArrayList localData = (System.Collections.ArrayList)MultimediaFolder["data"];
      //  foreach (IDictionary<string, object> file in localData)
      //  {
      //    RequestMultimediaFolderByParentId(file);
      //    RequestMultimediaFolder(file["id"].ToString() + "/files");
      //  }
      //}
      #endregion Рассмотреть
    }

    private void DownloadDataCallbackOneDrive(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("data"))
        {
          System.Collections.ArrayList localData = (System.Collections.ArrayList)MultimediaFolder["data"];
          foreach (IDictionary<string, object> file in localData)
          {
            RequestMultimediaFolderByParentId(file);
            RequestMultimediaFolderOneDrive(file["id"].ToString() + "/files");
          }
        }
      }
      catch (Exception ex)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }      
    }

    private void RequestMultimediaFolderByParentId(IDictionary<string, object> RESTObject)
    {
      string requestUrl;

      Interlocked.Increment(ref usingResource);

      try
      {
        requestUrl = apiUrl + RESTObject["id"].ToString() + "/files" + "?filter=audio&limit=1&access_token=" + AccessToken;
        WebClient wc = new WebClient();
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadMultimediaFolderName_Completed);
        wc.DownloadDataAsync(new Uri(requestUrl), RESTObject);

      }
      catch (Exception)
      {
      }      
    }

    private void DownloadMultimediaFolderName_Completed(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("data"))
        {
          System.Collections.ArrayList localData = (System.Collections.ArrayList)MultimediaFolder["data"];
          foreach (IDictionary<string, object> file in localData)
          {
            if (file["type"].ToString() == "audio")
            {
              OAuthObjectFolder ObjectFolder = new OAuthObjectFolder();
              IDictionary<string, object> MultimediaFile = (IDictionary<string, object>)e.UserState;
              ObjectFolder.Name = MultimediaFile["name"].ToString();
              ObjectFolder.Id = MultimediaFile["id"].ToString();
              ListObjectMultimediaFolder.Add(ObjectFolder);

              break;
            }
          }
        }
      }
      catch (Exception)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }      
    }

    private void RequestMultimediaNoFolder(string RESTFolder)
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string requestUrl = apiUrl + RESTFolder + "?filter=audio&limit=1&access_token=" + AccessToken;
        WebClient wc = new WebClient();
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadMultimediaNoFolderName_Completed);
        wc.DownloadDataAsync(new Uri(requestUrl));
      }
      catch (Exception ex)
      {
      }
    }

    private void DownloadMultimediaNoFolderName_Completed(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("data"))
        {
          System.Collections.ArrayList localData = (System.Collections.ArrayList)MultimediaFolder["data"];
          foreach (IDictionary<string, object> file in localData)
          {
            if (file["type"].ToString() == "audio")
            {
              OAuthObjectFolder ObjectFolder = new OAuthObjectFolder();
              ObjectFolder.Name = "No Album";
              ObjectFolder.Id = "0";
              ListObjectMultimediaFolder.Add(ObjectFolder);

              break;
            }
          }
        }
      }
      catch (Exception ex)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }
    }

    public async Task<long> GetMultimediaItemSourceOneDrive(string IdItem)
    {
      long IdItemSize;

      try
      {

        //var requestUrl = apiUrl + "drive/items/" + IdItem + "?access_token=" + AccessToken;
        var requestUrl = apiUrl + "drive/items/" + IdItem;

        var request = WebRequest.Create(requestUrl) as HttpWebRequest;
        request.Method = "GET";
        request.ContentType = "application/json";
        request.Headers.Add("Authorization", "Bearer " + AccessToken);

        var response = await request.GetResponseAsync() as HttpWebResponse;
        using (var stream = response.GetResponseStream())
        {
          using (var readStream = new StreamReader(stream, Encoding.GetEncoding("utf-8")))
          {
            var ResponseString = await readStream.ReadToEndAsync();

            //var MultimediaFolder = new Dictionary<string, object>();
            //MultimediaFolder = deserializeJsonData(ResponseString);

            var MultimediaItem = new Dictionary<string, object>();
            MultimediaItem = deserializeJsonData(ResponseString);
            IdItemSize = Convert.ToInt64(MultimediaItem["size"]);

            //if (MultimediaFolder.ContainsKey("entries"))
            //{
            //  var localData = (ArrayList)MultimediaFolder["entries"];

            //  ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
            //  OAuthObjectFolder multimediaObject;

            //  foreach (IDictionary<string, object> file in localData)
            //  {
            //    #region Get filter by extension
            //    if (file[".tag"].ToString().ToLower() == "folder")
            //      TypeMultimedia = "Folder";
            //    else
            //    {
            //      var aPartFile = file["name"].ToString().Split(new char[] { '.' });
            //      if (aPartFile.Length >= 2)
            //      {
            //        var Extension = aPartFile[aPartFile.Length - 1].ToLower();
            //        if (ListFilterExtensions.ContainsKey(Extension))
            //        {
            //          TypeMultimedia = ListFilterExtensions[Extension];
            //        }
            //        else
            //        {
            //          TypeMultimedia = "";
            //        }
            //      }
            //      else
            //      {
            //        TypeMultimedia = "";
            //      }
            //    }
            //    #endregion Get filter by extension

            //    if (JetSASLibrary.CheckGoodString(TypeMultimedia))
            //    {
            //      multimediaObject = new OAuthObjectFolder();

            //      multimediaObject.Id = file["id"].ToString();
            //      multimediaObject.Name = file["name"].ToString();
            //      multimediaObject.Type = TypeMultimedia;

            //      ListObjectMultimediaFolder.Add(multimediaObject);
            //    }
            //  }
            //}
          }
        }

        //using (var writer = new StreamWriter(request.GetRequestStream()))
        //{
        //  writer.Write(postContent);
        //}


        //var requestUrl = apiUrl + "drive/items/" + IdItem + "?access_token=" + AccessToken;

        //string ResponseString;
        //using (var wc = new WebClient())
        //{
        //  var b = await wc.DownloadDataTaskAsync(requestUrl);
        //  ResponseString = Encoding.UTF8.GetString(b);
        //}

        //var MultimediaItem = new Dictionary<string, object>();
        //MultimediaItem = deserializeJsonData(ResponseString);
        //IdItemSize = Convert.ToInt64(MultimediaItem["size"]);
      }
      catch (Exception ex)
      {
        IdItemSize = 0;
      }
      return IdItemSize;
    }

    #endregion Request Object Folder OneDrive

    #region Request Object Audio OneDrive

    public List<OAuthObjectAudio> GetListMultimediaItem(string[] ListId)
    {
      string IdFolder;
      for (int i = 0; i < ListId.Length; i++)
      {
        IdFolder = ListId[i];
        if (IdFolder == "0") IdFolder = RootFolder;
        RequestMultimediaItem(IdFolder);
      }

      do
      {
        Thread.Sleep(500);
      } while (IsUsing() == true);

      return ListObjectMultimediaAudio;
    }

    private void RequestMultimediaItem(string RESTFolder)
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string requestUrl = apiUrl + RESTFolder + "/files" + "?filter=audio&access_token=" + AccessToken;

        WebClient wc = new WebClient();
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadMultimediaItem);
        wc.DownloadDataAsync(new Uri(requestUrl));
      }
      catch (Exception)
      {
      }      
    }

    private void DownloadMultimediaItem(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("data"))
        {
          System.Collections.ArrayList localData = (System.Collections.ArrayList)MultimediaFolder["data"];
          foreach (IDictionary<string, object> file in localData)
          {
            if (file["type"].ToString() == "audio")
            {
              OAuthObjectAudio ObjectAudio = new OAuthObjectAudio();
              ObjectAudio.Id = GetNotNullString(file["id"]);
              ObjectAudio.Name = GetNotNullString(file["name"]);
              ObjectAudio.IsEmbeddable = Convert.ToBoolean(file["is_embeddable"]);
              ObjectAudio.Source = GetNotNullString(file["source"]);
              ObjectAudio.Link = GetNotNullString(file["link"]);
              ObjectAudio.Title = GetNotNullString(file["title"]);
              ObjectAudio.Artist = GetNotNullString(file["artist"]);
              ObjectAudio.Album = GetNotNullString(file["album"]);
              ObjectAudio.AlbumArtist = GetNotNullString(file["album_artist"]);
              ListObjectMultimediaAudio.Add(ObjectAudio);
              //RequestMultimediaItemSource(file["id"].ToString(), file["name"].ToString());
            }
          }
        }
      }
      catch (Exception)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }      
    }
    
    private void RequestMultimediaItemSource(string IdObject, string NameObject)
    {
      Interlocked.Increment(ref usingResource);

      string requestUrl;
      requestUrl = apiUrl + IdObject + "/shared_read_link" + "?access_token=" + AccessToken;
      //requestUrl = apiUrl + IdObject + "/content?suppress_redirects=true" + "?&access_token=" + AccessToken; // Content of file
      WebClient wc = new WebClient();
      wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadMultimediaItemSource);
      wc.DownloadDataAsync(new Uri(requestUrl), NameObject);
    }

    private void DownloadMultimediaItemSource(object sender, DownloadDataCompletedEventArgs e)
    {
      string ResponseString = Encoding.UTF8.GetString(e.Result);

      Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
      MultimediaFolder = deserializeJsonData(ResponseString);
      string NameObject = (string)e.UserState;

      OAuthObjectAudio ObjectAudio = new OAuthObjectAudio();
      ObjectAudio.Name = NameObject;
      ObjectAudio.Source = MultimediaFolder["link"].ToString();
      ListObjectMultimediaAudio.Add(ObjectAudio);

      Interlocked.Decrement(ref usingResource);
    }

    #endregion Request Object Audio OneDrive

    #region VKontakte

    public async Task<List<OAuthObjectFolder>> GetItemsByNoneVKontakteAsync(string IdItem)
    {
      #region Define vars
      string ResponseString, requestUrl, code;
      OAuthObjectFolder OAuthObject;
      bool IsSuccess;
      #endregion Define vars

      #region Try
      try
      {
        #region Define code for method Execute
        switch (filter)
        {
          #region Define Type Audio
          case "audio":
            if (!JetSASLibrary.CheckGoodString(IdItem))
            {
              code = "return {\"folders\":API.audio.getAlbums().items,\"items\":API.audio.get().items};";
            }
            else
            {
              code = "return {\"stub\":false,\"items\":API.audio.get({\"album_id\":" + IdItem + "}).items};";
            }
            break;
          #endregion Define Type Audio

          #region Define Type Video
          case "video":
            code = "return {\"IsFoundNoAlbum\":\"false\", \"items\":API.video.get().items};";
            break;
          #endregion Define Type Video

          default:
            code = "";
            break;
        }
        #endregion Define code for method Execute

        //Номер версии является принципиальным
        var version = "5.45";
        requestUrl = apiUrl + RootFolder + "?code=" + HttpUtility.UrlEncode(code) + "&v=" + version + "&access_token=" + AccessToken;

        using (var wc = new WebClient())
        {
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        var MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        #region There's response
        if (MultimediaFolder.ContainsKey("response"))
        {
          ListObjectMultimediaFolder = new List<OAuthObjectFolder>();

          var response = (IDictionary<string, object>)MultimediaFolder["response"];

          #region Define NoAlbum
          //object objIsFoundNoAlbum;
          //IsSuccess = response.TryGetValue("IsFoundNoAlbum", out objIsFoundNoAlbum);
          //if (IsSuccess)
          //{
          //  var IsFoundNoAlbum = false;
          //  IsSuccess = bool.TryParse(objIsFoundNoAlbum.ToString(), out IsFoundNoAlbum);
          //  if (IsSuccess && IsFoundNoAlbum)
          //  {
          //    OAuthObject = new OAuthObjectFolder();
          //    OAuthObject.Id = "0";
          //    OAuthObject.Name = "No Album";
          //    OAuthObject.Type = "Folder";

          //    ListObjectMultimediaFolder.Add(OAuthObject);
          //  }
          //}
          #endregion Define NoAlbum

          object items;

          #region Define folders
          IsSuccess = response.TryGetValue("folders", out items);
          if (IsSuccess)
          {
            var localData = (ArrayList)items;            

            foreach (IDictionary<string, object> file in localData)
            {
              OAuthObject = new OAuthObjectFolder();
              OAuthObject.Id = file["id"].ToString();
              OAuthObject.Name = file["title"].ToString();
              OAuthObject.Type = "Folder";

              ListObjectMultimediaFolder.Add(OAuthObject);
            }
          }
          #endregion Define folders

          #region Define items          
          IsSuccess = response.TryGetValue("items", out items);
          if (IsSuccess)
          {
            var localData = (ArrayList)items;

            foreach (IDictionary<string, object> file in localData)
            {
              if (!JetSASLibrary.CheckGoodString(IdItem))
              {
                if (!file.ContainsKey("album_id"))
                {
                  OAuthObject = new OAuthObjectFolder();
                  OAuthObject.Id = file["id"].ToString();
                  OAuthObject.Name = file["title"].ToString();
                  OAuthObject.Type = filter;

                  ListObjectMultimediaFolder.Add(OAuthObject);
                }
              }
              else
              {
                OAuthObject = new OAuthObjectFolder();
                OAuthObject.Id = file["id"].ToString();
                OAuthObject.Name = file["title"].ToString();
                OAuthObject.Type = filter;

                ListObjectMultimediaFolder.Add(OAuthObject);
              }
            }

            //foreach (IDictionary<string, object> file in localData)
            //{
            //  if (IdItem != "0")
            //  {
            //    OAuthObject = new OAuthObjectFolder();
            //    OAuthObject.Id = file["id"].ToString();
            //    OAuthObject.Name = file["title"].ToString();
            //    OAuthObject.Type = filter;

            //    ListObjectMultimediaFolder.Add(OAuthObject);
            //  }
            //  else
            //  {
            //    if (!file.ContainsKey("album_id"))
            //    {
            //      OAuthObject = new OAuthObjectFolder();
            //      OAuthObject.Id = file["id"].ToString();
            //      OAuthObject.Name = file["title"].ToString();
            //      OAuthObject.Type = filter;

            //      ListObjectMultimediaFolder.Add(OAuthObject);
            //    }
            //  }              
            //}
          }
          #endregion Define items
        }
        #endregion There's response
      }
      #endregion Try

      #region catch
      catch (Exception)
      {
        ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
      }
      #endregion catch

      return ListObjectMultimediaFolder;
    }

    #region Request Object Folder VKontakte

    public List<OAuthObjectFolder> GetListMultimediaFolderVKontakte()
    {
      RequestMultimediaFolderVKontakte("audio.getAlbums");
      RequestMultimediaNoFolderVKontakte("audio.get");
      do
      {
        Thread.Sleep(500);
      } while (IsUsing() == true);
      return ListObjectMultimediaFolder;
    }

    private void RequestMultimediaFolderVKontakte(string Method)
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string requestUrl;
        requestUrl = apiUrl + Method + "?access_token=" + AccessToken;
        WebClient wc = new WebClient();
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadFolderCallbackVKontakte);
        wc.DownloadDataAsync(new Uri(requestUrl));
      }
      catch (Exception)
      {
      }
    }

    private void DownloadFolderCallbackVKontakte(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("response"))
        {
          ArrayList localData = (ArrayList)MultimediaFolder["response"];
          foreach (Object ObjectFile in localData)
          {
            if (ObjectFile.GetType().Name == "Dictionary`2")
            {
              IDictionary<string, object> file = (IDictionary<string, object>)ObjectFile;

              OAuthObjectFolder ObjectFolder = new OAuthObjectFolder();
              ObjectFolder.Name = file["title"].ToString();
              ObjectFolder.Id = file["album_id"].ToString();
              ObjectFolder.Type = "audio";
              ListObjectMultimediaFolder.Add(ObjectFolder);
            }
          }
        }
      }
      catch (Exception)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }
    }

    private void RequestMultimediaNoFolderVKontakte(string Method)
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string requestUrl;
        requestUrl = apiUrl + Method + "?access_token=" + AccessToken;
        WebClient wc = new WebClient();
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadNoFolderCallbackVKontakte);
        wc.DownloadDataAsync(new Uri(requestUrl));
      }
      catch (Exception)
      {
      }
    }

    private void DownloadNoFolderCallbackVKontakte(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("response"))
        {
          ArrayList localData = (ArrayList)MultimediaFolder["response"];
          foreach (IDictionary<string, object> file in localData)
          {
            if (file.ContainsKey("album") == false)
            {
              OAuthObjectFolder ObjectFolder = new OAuthObjectFolder();
              ObjectFolder.Name = "No Album";
              ObjectFolder.Id = "0";
              ObjectFolder.Type = "audio";
              ListObjectMultimediaFolder.Add(ObjectFolder);

              break;
            }
          }
        }
      }
      catch (Exception)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }      
    }

    #endregion Request Object Folder VKontakte

    #region Request Object Audio VKontakte

    public List<OAuthObjectAudio> GetListMultimediaItemVKontakte(string[] ListIdFolder)
    {
      string IdFolder;
      for (int i = 0; i < ListIdFolder.Length; i++)
      {
        IdFolder = ListIdFolder[i];
        RequestMultimediaItemVKontakte(IdFolder);
      }

      do
      {
        Thread.Sleep(500);
      } while (IsUsing() == true);

      return ListObjectMultimediaAudio;
    }

    private void RequestMultimediaItemVKontakte(string RESTFolder)
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string requestUrl;
        requestUrl = apiUrl + "audio.get" + "?filter=audio&access_token=" + AccessToken;
        WebClient wc = new WebClient();
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadMultimediaItemCompletedVKontakte);
        wc.DownloadDataAsync(new Uri(requestUrl), RESTFolder);
      }
      catch (Exception)
      {
      }
    }

    private void DownloadMultimediaItemCompletedVKontakte(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string AlbumId;

        string RESTFolder = (string)e.UserState;

        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("response"))
        {
          ArrayList localData = (ArrayList)MultimediaFolder["response"];
          foreach (IDictionary<string, object> file in localData)
          {
            if (file.ContainsKey("album") == true)
            {
              AlbumId = file["album"].ToString();
            }
            else
            {
              AlbumId = "0";
            }

            if (RESTFolder == AlbumId)
            {
              string[] aParam = file["url"].ToString().Split(new Char[] { '?' });

              string[] a = aParam[0].Split(new Char[] { '.' });
              string TypeMultimedia = a[a.Length - 1].ToLower();

              OAuthObjectAudio ObjectAudio = new OAuthObjectAudio();
              ObjectAudio.Id = GetNotNullString(file["aid"]);
              ObjectAudio.Name = GetNotNullString(file["title"]) + "." + TypeMultimedia;
              ObjectAudio.Source = GetNotNullString(file["url"]);
              ObjectAudio.Title = GetNotNullString(file["title"]);
              ObjectAudio.Artist = GetNotNullString(file["artist"]);
              ListObjectMultimediaAudio.Add(ObjectAudio);
            }
          }
        }
      }
      catch (Exception)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }      
    }

    public async Task<string> GetDownloadURLVKontakte(string IdItem)
    {
      #region Define vars
      string ResponseString;
      string requestUrl;
      string code;
      string DownloadURL;
      #endregion Define vars     

      try
      {
        DownloadURL = "";

        var uid = await RequestUserInfoVKontakte();
        if (!JetSASLibrary.CheckGoodString(uid)) throw new ArgumentException("user id VKontakte is empty", "AccessToken");

        #region Define code for method Execute
        switch (filter)
        {
          #region Define Type Audio
          case "audio":
            //code = "return API.audio.getById({\"audios\":\"" + uid + "_" + IdItem + "\"})@.url;";
            code = "return {\"url\":API.audio.getById({\"audios\":\"" + uid + "_" + IdItem + "\"})@.url};";
            break;
          #endregion Define Type Audio

          #region Define Type Video
          case "video":
            //code = "return API.video.get({\"videos\":\""+uid+"_"+ IdItem+"\"}).items@.files;";
            //code = "return API.video.get({\"videos\":\"" + uid + "_" + IdItem + "\"}).items@.files;";
            //code = "return {\"IsFoundNoAlbum\":\"false\", \"items\":API.video.get().items};";
            //code = "return {\"urls\": API.video.get({\"videos\":\"" + uid + "_" + IdItem + "\"}).items@.files};";
            code = "return {\"url\": API.video.get({\"videos\":\"" + uid + "_" + IdItem + "\"}).items@.player};";
            break;
          #endregion Define Type Video

          default:
            code = "";
            break;
        }
        #endregion Define code for method Execute

        //Номер версии является принципиальным
        var version = "5.45";
        requestUrl = apiUrl + RootFolder + "?code=" + HttpUtility.UrlEncode(code) + "&v=" + version + "&access_token=" + AccessToken;

        using (var wc = new WebClient())
        {
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        var OAuthObject = new Dictionary<string, object>();
        OAuthObject = deserializeJsonData(ResponseString);

        if (OAuthObject.ContainsKey("response"))
        {
          //var response = (ArrayList)MultimediaObject["response"];
          var response = (IDictionary<string, object>)OAuthObject["response"];

          bool IsSuccess;
          object objectURL;
          #region Define folders
          IsSuccess = response.TryGetValue("url", out objectURL);
          if (IsSuccess) //Audio
          {
            var localData = (ArrayList)objectURL;
            DownloadURL = localData[0].ToString();
          }
          #endregion Define folders

          IsSuccess = response.TryGetValue("urls", out objectURL);
          if (IsSuccess) //Video
          {
            var localData = (ArrayList)objectURL;
            foreach (IDictionary<string, object> file in localData)
            {
            }
          }

          //foreach (IDictionary<string, object> file in response)
          //{
          //  if (file.ContainsKey("url"))
          //  {
          //    DownloadURL = file["url"].ToString();
          //    break;
          //  }
          //}
        }
      }
      catch (Exception)
      {
        DownloadURL = "";
      }

      return DownloadURL;
    }

    #endregion Request Object Audio VKontakte

    public async Task<long> GetMultimediaJobAttributesVKontakte(string IdItem)
    {
      long IdItemSize;

      try
      {
        var requestUrl = apiUrl + "/files/" + IdItem + "?fields=size&access_token=" + AccessToken;

        string ResponseString;
        using (var wc = new WebClient())
        {
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        var MultimediaItem = new Dictionary<string, object>();
        MultimediaItem = deserializeJsonData(ResponseString);
        IdItemSize = Convert.ToInt64(MultimediaItem["size"]);
      }
      catch (Exception)
      {
        IdItemSize = 0;
      }
      return IdItemSize;
    }

    private async Task<string> RequestUserInfoVKontakte()
    {
      string ResponseString;
      string uid = "";

      try
      {        
        var code = "return API.users.get();";
        var requestUrl = apiUrl + RootFolder + "?code=" + HttpUtility.UrlEncode(code) + "&v=5.44&access_token=" + AccessToken;

        using (var wc = new WebClient())
        {
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        Dictionary<string, object> UserInfo = new Dictionary<string, object>();
        UserInfo = deserializeJsonData(ResponseString);

        if (UserInfo.ContainsKey("response"))
        {
          var localData = (ArrayList)UserInfo["response"];
          foreach (IDictionary<string, object> file in localData)
          {
            if (file.ContainsKey("id"))
            {
              uid = file["id"].ToString();
              break;
            }
          }
        }
      }
      catch (Exception ex)
      {
        uid = "";
      }

      return uid;
    }

    #endregion VKontakte

    private void DownloadMultimediaItemComplete(object sender, DownloadDataCompletedEventArgs e)
    {
      string ResponseString = Encoding.UTF8.GetString(e.Result);

      Dictionary<string, object> MultimediaAudio = new Dictionary<string, object>();
      MultimediaAudio = deserializeJsonData(ResponseString);
      ObjectAudioName = MultimediaAudio["source"].ToString();
      ObjectAudioSize = Convert.ToInt32(MultimediaAudio["size"]);

      Interlocked.Decrement(ref usingResource);
    }

    #region Google Drive

    #region Request Object Folder Google Drive

    private string GetFacetGoogleDrive(IDictionary<string, object> multimediaObject)
    {
      if (multimediaObject["mimeType"].ToString() == "application/vnd.google-apps.folder") return "Folder";
      if (multimediaObject["mimeType"].ToString() == "application/vnd.google-apps.audio" || multimediaObject["mimeType"].ToString().Contains("audio")) return "Audio";
      if (multimediaObject["mimeType"].ToString() == "application/vnd.google-apps.video" || multimediaObject["mimeType"].ToString().Contains("video")) return "Video";
      if (multimediaObject["mimeType"].ToString() == "application/vnd.google-apps.photo" || multimediaObject["mimeType"].ToString().Contains("image")) return "Image";

      return "Unsupported";
    }

    public async Task<List<OAuthObjectFolder>> GetItemsByNoneGoogleDriveAsync(string IdItem)
    {
      #region Define vars
      string ResponseString, requestUrl, query;
      #endregion Define vars

      try
      {
        #region Define Root folder
        if (!JetSASLibrary.CheckGoodString(IdItem))
        {
          requestUrl = apiUrl + "/root";

          using (var wc = new WebClient())
          {
            wc.Headers.Add("Authorization", "OAuth " + AccessToken);
            var b = await wc.DownloadDataTaskAsync(new Uri(requestUrl));
            ResponseString = Encoding.UTF8.GetString(b);
          }

          Dictionary<string, object> MultimediaFolderRoot = new Dictionary<string, object>();
          MultimediaFolderRoot = deserializeJsonData(ResponseString);
          IdItem = MultimediaFolderRoot["id"].ToString();
        }
        #endregion Define Root folder

        #region Define filter
        if (filter != "")
          query = string.Format("(mimeType='application/vnd.google-apps.folder' or mimeType contains '{0}') and '{1}' in parents and trashed=false", filter, IdItem);
        else
          query = string.Format("'{0}' in parents and trashed=false", IdItem);
        #endregion Define filter

        requestUrl = apiUrl + "?q=" + HttpUtility.UrlEncode(query) + "&files(id, name, mimeType)";

        using (var wc = new WebClient())
        {
          wc.Headers.Add("Authorization", "OAuth " + AccessToken);
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        var MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("files"))
        {
          var localData = (ArrayList)MultimediaFolder["files"];

          ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
          OAuthObjectFolder multimediaObject;

          foreach (IDictionary<string, object> file in localData)
          {
            multimediaObject = new OAuthObjectFolder();

            multimediaObject.Id = file["id"].ToString();
            multimediaObject.Name = file["name"].ToString();
            multimediaObject.Type = GetFacetGoogleDrive(file);

            ListObjectMultimediaFolder.Add(multimediaObject);
          }
        }
      }
      catch (Exception ex)
      {
        ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
      }

      return ListObjectMultimediaFolder;
    }

    public List<OAuthObjectFolder> GetListMultimediaFolderGoogleDrive()
    {
      RequestMultimediaFolderGoogleDrive(RootFolder);
      RequestMultimediaFolderRootGoogleDrive();
      do
      {
        Thread.Sleep(500);
      } while (IsUsing() == true);
      return ListObjectMultimediaFolder;
    }

    private void RequestMultimediaFolderGoogleDrive(string RESTFolder)
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string filter = String.Format("mimeType={0}&trashed={1}&fields={2}",
          HttpUtility.UrlEncode("'application/vnd.google-apps.folder'"),
          HttpUtility.UrlEncode("false"),
          HttpUtility.UrlEncode("items(id,title)"));

        string requestUrl = apiUrl + "?q=" + filter;

        WebClient wc = new WebClient();
        wc.Headers.Add("Authorization", "OAuth " + AccessToken);
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadFolderGoogleDrive_Completed);
        wc.DownloadDataAsync(new Uri(requestUrl));
      }
      catch (Exception)
      {
      }
    }

    private void DownloadFolderGoogleDrive_Completed(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("items"))
        {
          ArrayList localData = (ArrayList)MultimediaFolder["items"];
          foreach (IDictionary<string, object> folder in localData)
          {
            RequestMultimediaFolderByParentIdGoogleDrive(folder);
          }
        }
      }
      catch (Exception)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }
    }

    private void RequestMultimediaFolderByParentIdGoogleDrive(IDictionary<string, object> RESTObject)
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string filter = String.Format("mimeType contains {0}&trashed={1}&fields={2}",
          HttpUtility.UrlEncode("'audio'"),
          HttpUtility.UrlEncode("false"),
          HttpUtility.UrlEncode("items/id"));

        string requestUrl = apiUrl + "/" + RESTObject["id"].ToString() + "/children" + "?q=" + filter;

        WebClient wc = new WebClient();
        wc.Headers.Add("Authorization", "OAuth " + AccessToken);
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadFolderNameGoogleDrive_Completed);
        wc.DownloadDataAsync(new Uri(requestUrl), RESTObject);
      }
      catch (Exception)
      {
      }
    }

    private void DownloadFolderNameGoogleDrive_Completed(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolderChild = new Dictionary<string, object>();
        MultimediaFolderChild = deserializeJsonData(ResponseString);

        if (MultimediaFolderChild.ContainsKey("items"))
        {
          System.Collections.ArrayList localData = (System.Collections.ArrayList)MultimediaFolderChild["items"];
          if (localData.Count > 0)
          {
            OAuthObjectFolder ObjectFolder = new OAuthObjectFolder();
            IDictionary<string, object> MultimediaFolder = (IDictionary<string, object>)e.UserState;
            ObjectFolder.Id = MultimediaFolder["id"].ToString();
            ObjectFolder.Name = MultimediaFolder["title"].ToString();
            ListObjectMultimediaFolder.Add(ObjectFolder);
          }
        }
      }
      catch(Exception)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }
    }

    private void RequestMultimediaFolderRootGoogleDrive()
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string filter = String.Format("mimeType contains {0}&trashed={1}&fields={2}",
          HttpUtility.UrlEncode("'audio'"),
          HttpUtility.UrlEncode("false"),
          HttpUtility.UrlEncode("items/id"));

        string requestUrl = apiUrl + "/" + "root" + "/children" + "?q=" + filter;

        WebClient wc = new WebClient();
        wc.Headers.Add("Authorization", "OAuth " + AccessToken);
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadFolderRootGoogleDrive_Completed);
        wc.DownloadDataAsync(new Uri(requestUrl));
      }
      catch (Exception)
      {
      }
    }

    private void DownloadFolderRootGoogleDrive_Completed(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolderRoot = new Dictionary<string, object>();
        MultimediaFolderRoot = deserializeJsonData(ResponseString);

        if (MultimediaFolderRoot.ContainsKey("items"))
        {
          System.Collections.ArrayList localData = (System.Collections.ArrayList)MultimediaFolderRoot["items"];
          if (localData.Count > 0)
          {
            OAuthObjectFolder ObjectFolder = new OAuthObjectFolder();
            IDictionary<string, object> MultimediaFolder = (IDictionary<string, object>)e.UserState;
            ObjectFolder.Id = "0";
            ObjectFolder.Name = "No Album";
            ListObjectMultimediaFolder.Add(ObjectFolder);
          }
        }
      }        
      catch(Exception)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }
    }

    public async Task<long> GetMetadataGoogleDriveAsync(string IdItem)
    {
      long IdItemSize;

      try
      {
        var requestUrl = apiUrl + "/files/" + IdItem + "?fields=size&access_token=" + AccessToken;

        string ResponseString;
        using (var wc = new WebClient())
        {
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        var MultimediaItem = new Dictionary<string, object>();
        MultimediaItem = deserializeJsonData(ResponseString);
        IdItemSize = Convert.ToInt64(MultimediaItem["size"]);
      }
      catch (Exception)
      {
        IdItemSize = 0;
      }
      return IdItemSize;
    }

    #endregion Request Object Folder Google Drive

    #region Request Object Audio Google Drive

    public List<OAuthObjectAudio> GetListMultimediaItemGoogleDrive(string[] ListId)
    {
      string IdFolder;
      for (int i = 0; i < ListId.Length; i++)
      {
        IdFolder = ListId[i];
        RequestMultimediaItemGoogleDrive(IdFolder);
      }

      do
      {
        Thread.Sleep(500);
      } while (IsUsing() == true);

      return ListObjectMultimediaAudio;
    }

    private void RequestMultimediaItemGoogleDrive(string IdFolder)
    {
      Interlocked.Increment(ref usingResource);

      try
      {
        string requestUrl; WebClient wc;

        if (IdFolder == "0")
        {
          requestUrl = apiUrl + "/root";
          wc = new WebClient();
          wc.Headers.Add("Authorization", "OAuth " + AccessToken);

          byte[] b = wc.DownloadData(new Uri(requestUrl));
          string ResponseString = Encoding.UTF8.GetString(b);

          Dictionary<string, object> MultimediaFolderRoot = new Dictionary<string, object>();
          MultimediaFolderRoot = deserializeJsonData(ResponseString);
          IdFolder = MultimediaFolderRoot["id"].ToString();
        }

        string filter = String.Format("mimeType contains {0} and {1} in parents&trashed={2}&fields={3}",
          HttpUtility.UrlEncode("'audio'"),
          HttpUtility.UrlEncode("'" + IdFolder + "'"),
          HttpUtility.UrlEncode("false"),
          HttpUtility.UrlEncode("items(id,downloadUrl,title)"));

        requestUrl = apiUrl + "?q=" + filter;

        wc = new WebClient();
        wc.Headers.Add("Authorization", "OAuth " + AccessToken);
        wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadMultimediaItemGoogleDrive_Completed);
        wc.DownloadDataAsync(new Uri(requestUrl));
      }
      catch (Exception)
      {
      }
    }

    private void DownloadMultimediaItemGoogleDrive_Completed(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        string ResponseString = Encoding.UTF8.GetString(e.Result);

        Dictionary<string, object> MultimediaFolderChild = new Dictionary<string, object>();
        MultimediaFolderChild = deserializeJsonData(ResponseString);

        if (MultimediaFolderChild.ContainsKey("items"))
        {
          System.Collections.ArrayList localData = (System.Collections.ArrayList)MultimediaFolderChild["items"];
          foreach (IDictionary<string, object> file in localData)
          {
            OAuthObjectAudio ObjectAudio = new OAuthObjectAudio();
            ObjectAudio.Id = GetNotNullString(file["id"]);
            ObjectAudio.Name = GetNotNullString(file["title"]);
            ObjectAudio.Source = GetNotNullString(file["downloadUrl"]);
            ObjectAudio.Title = GetNotNullString(file["title"]);
            ObjectAudio.Artist = "";
            ListObjectMultimediaAudio.Add(ObjectAudio);
          }
        }
      }
      catch (Exception)
      {
      }
      finally
      {
        Interlocked.Decrement(ref usingResource);
      }      
    }

    public async Task<string> GetMultimediaItemSourceGoogleDrive(string IdObjectAudio)
    {
      string MultimediaSource;      

      Interlocked.Increment(ref usingResource);

      try
      {
        string MultimediaSourceUrl; string MultimediaSourceSize; string requestUrl;
        requestUrl = apiUrl + IdObjectAudio;
        WebClient wc = new WebClient();
        wc.Headers.Add("Authorization", "OAuth " + AccessToken);

        byte[] b = await wc.DownloadDataTaskAsync(new Uri(requestUrl));
        string ResponseString = Encoding.UTF8.GetString(b);

        Dictionary<string, object> MultimediaObjectAudio = new Dictionary<string, object>();
        MultimediaObjectAudio = deserializeJsonData(ResponseString);
        MultimediaSourceUrl = MultimediaObjectAudio["downloadUrl"].ToString();
        MultimediaSourceSize = MultimediaObjectAudio["fileSize"].ToString();
        MultimediaSource = MultimediaSourceUrl + "|" + MultimediaSourceSize;
      }
      catch (Exception)
      {
        MultimediaSource = "";
      }
      return MultimediaSource;
    }

    #endregion Request Object Audio Google Drive

    #endregion Google Drive

    #region Dropbox

    public async Task<List<OAuthObjectFolder>> GetItemsByNoneDropboxAsync(string IdItem, StringDictionary ListFilterExtensions)
    {
      #region Define vars
      string requestUrl;
      string postContent;
      string TypeMultimedia;
      #endregion Define vars

      #region Try
      try
      {
        if (!JetSASLibrary.CheckGoodString(IdItem))
        {
          postContent = "{\"path\":\"\",\"recursive\":false,\"include_media_info\":false,\"include_deleted\":false}";
        }
        else
        {
          var path_lower = (await GetMetadataDropboxAsync(IdItem)).Item1;
          postContent = "{\"path\":\"" + path_lower + "\",\"recursive\":false,\"include_media_info\":false,\"include_deleted\":false}";
        }

        requestUrl = apiUrl + RootFolder+ "/list_folder";

        var request = WebRequest.Create(requestUrl) as HttpWebRequest;
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers.Add("Authorization", "Bearer " + AccessToken);

        using (var writer = new StreamWriter(request.GetRequestStream()))
        {
          writer.Write(postContent);
        }

        var response = await request.GetResponseAsync() as HttpWebResponse;
        using (var stream = response.GetResponseStream())
        {
          using (var readStream = new StreamReader(stream, Encoding.GetEncoding("utf-8")))
          {
            var ResponseString = await readStream.ReadToEndAsync();

            var MultimediaFolder = new Dictionary<string, object>();
            MultimediaFolder = deserializeJsonData(ResponseString);

            if (MultimediaFolder.ContainsKey("entries"))
            {
              var localData = (ArrayList)MultimediaFolder["entries"];

              ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
              OAuthObjectFolder multimediaObject;

              foreach (IDictionary<string, object> file in localData)
              {
                #region Get filter by extension
                if (file[".tag"].ToString().ToLower() == "folder")
                  TypeMultimedia = "Folder";
                else
                {
                  var aPartFile = file["name"].ToString().Split(new char[] { '.' });
                  if (aPartFile.Length>=2)
                  {
                    var Extension = aPartFile[aPartFile.Length - 1].ToLower();
                    if (ListFilterExtensions.ContainsKey(Extension))
                    {
                      TypeMultimedia = ListFilterExtensions[Extension];
                    }
                    else
                    {
                      TypeMultimedia = "";
                    }
                  }
                  else
                  {
                    TypeMultimedia = "";
                  }
                }
                #endregion Get filter by extension

                if (JetSASLibrary.CheckGoodString(TypeMultimedia))
                {
                  multimediaObject = new OAuthObjectFolder();

                  multimediaObject.Id = file["id"].ToString();
                  multimediaObject.Name = file["name"].ToString();
                  multimediaObject.Type = TypeMultimedia;

                  ListObjectMultimediaFolder.Add(multimediaObject);
                }
              }
            }
          }
        }
      }
      #endregion Try

      #region Catch
      catch (Exception)
      {
        ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
      }
      #endregion Catch

      return ListObjectMultimediaFolder;
    }

    public async Task<Tuple<string, long>> GetMetadataDropboxAsync(string id)
    {
      #region Define vars
      string path_lower; long IdItemSize;
      Tuple<string, long> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        var postContent = "{\"path\":\"" + id + "\"}";

        var requestUrl = apiUrl + RootFolder + "/get_metadata";

        var request = WebRequest.Create(requestUrl) as HttpWebRequest;
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers.Add("Authorization", "Bearer " + AccessToken);

        using (var writer = new StreamWriter(request.GetRequestStream()))
        {
          writer.Write(postContent);
        }

        var response = await request.GetResponseAsync() as HttpWebResponse;
        using (var stream = response.GetResponseStream())
        {
          using (var readStream = new StreamReader(stream, Encoding.GetEncoding("utf-8")))
          {
            var ResponseString = await readStream.ReadToEndAsync();

            var MultimediaFolder = new Dictionary<string, object>();
            MultimediaFolder = deserializeJsonData(ResponseString);

            path_lower = MultimediaFolder["path_lower"].ToString();

            if (MultimediaFolder.ContainsKey("size"))
            {
              IdItemSize = Convert.ToInt64(MultimediaFolder["size"]);
            }
            else
            {
              IdItemSize = 0;
            }              

            returnValue = new Tuple<string, long>(path_lower, IdItemSize);
          }
        }
      }
      #endregion Try

      #region Catch
      catch (Exception)
      {
        returnValue = new Tuple<string, long>("", 0);
      }
      #endregion Catch

      return returnValue;
    }

    #endregion Dropbox

    #region YandexDisk

    private string GetFacetYandexDisk(IDictionary<string, object> multimediaObject)
    {
      string TypeMultimedia;

      if (multimediaObject["type"].ToString().ToLower() == "dir")
      {
        TypeMultimedia = "Folder";
      }
      else
      {
        var MediaType = multimediaObject["media_type"].ToString().ToLower();
        switch (MediaType)
        {
          case "audio":
            TypeMultimedia = "Audio";
            break;
          case "video":
            TypeMultimedia = "Video";
            break;
          case "image":
            TypeMultimedia = "Image";
            break;
          case "document":
          case "spreadsheet":
          case "text":
            TypeMultimedia = "Document";
            break;
          default:
            TypeMultimedia = "Unsupported";
            break;
        }
      }

      return TypeMultimedia;
    }

    public async Task<List<OAuthObjectFolder>> GetItemsYandexDiskAsync(string IdItem)
    {
      #region Define vars
      string query;
      string requestUrl;
      #endregion Define vars

      try
      {
        query = "/files?fields=" + HttpUtility.UrlEncode("items.name,items.media_type,items.path,items.type");
        requestUrl = apiUrl + RootFolder + query + "&media_type=" + filter + "&limit=999999999&access_token=" + AccessToken;

        string ResponseString;
        using (var wc = new WebClient())
        {
          wc.Headers.Add("Authorization", "OAuth " + AccessToken);
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        var MultimediaFolder = new Dictionary<string, object>();
        MultimediaFolder = deserializeJsonData(ResponseString);

        if (MultimediaFolder.ContainsKey("items"))
        {
          var localData = (ArrayList)MultimediaFolder["items"];

          ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
          OAuthObjectFolder multimediaObject;

          string[] aPartFilename;

          foreach (IDictionary<string, object> file in localData)
          {
            multimediaObject = new OAuthObjectFolder();

            multimediaObject.Id = file["path"].ToString();
            multimediaObject.Name = file["name"].ToString();
            multimediaObject.Type = GetFacetYandexDisk(file);

            aPartFilename= file["path"].ToString().Split(new char[] { '/' });
            multimediaObject.IdFromObject = aPartFilename[aPartFilename.Length - 2]; //album - folder's name
            if (multimediaObject.IdFromObject == "disk:") multimediaObject.IdFromObject = "No Album";

            ListObjectMultimediaFolder.Add(multimediaObject);
          }
        }
      }
      catch (Exception ex)
      {
        ListObjectMultimediaFolder = new List<OAuthObjectFolder>();
      }

      return ListObjectMultimediaFolder;
    }

    public async Task<long> GetMetadataYandexDiskAsync(string IdItem)
    {
      #region Define vars
      long IdItemSize;
      #endregion Define vars

      #region Try
      try
      {
        var requestUrl = apiUrl + RootFolder + "?path=" + HttpUtility.UrlEncode(IdItem) + "&fields=size";

        string ResponseString;
        using (var wc = new WebClient())
        {
          wc.Headers.Add("Authorization", "OAuth " + AccessToken);
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        var MultimediaItem = new Dictionary<string, object>();
        MultimediaItem = deserializeJsonData(ResponseString);
        IdItemSize = Convert.ToInt64(MultimediaItem["size"]);
      }
      #endregion Try

      #region Catch
      catch (Exception)
      {
        IdItemSize = 0;
      }
      #endregion Catch

      return IdItemSize;
    }

    public async Task<string> GetDownloadURLYandexDiskAsync(string IdItem)
    {
      #region Define vars
      string DownloadUrl;
      #endregion Define vars

      #region Try
      try
      {
        var requestUrl = apiUrl + RootFolder + "?path=" + HttpUtility.UrlEncode(IdItem) + "&fields=href";

        string ResponseString;
        using (var wc = new WebClient())
        {
          wc.Headers.Add("Authorization", "OAuth " + AccessToken);
          var b = await wc.DownloadDataTaskAsync(requestUrl);
          ResponseString = Encoding.UTF8.GetString(b);
        }

        var MultimediaItem = new Dictionary<string, object>();
        MultimediaItem = deserializeJsonData(ResponseString);
        DownloadUrl = MultimediaItem["href"].ToString();
      }
      #endregion Try

      #region Catch
      catch (Exception)
      {
        DownloadUrl = "";
      }
      #endregion Catch

      return DownloadUrl;
    }
    #endregion YandexDisk

    private Dictionary<string, object> deserializeJsonData(string json)
    {
      var jss = new JavaScriptSerializer();
      var d = jss.Deserialize<Dictionary<string, object>>(json);
      return d;
    }

    private string GetNotNullString(Object CurrentString)
    {
      string NewString;
      if (CurrentString != null)
        NewString = CurrentString.ToString();
      else
        NewString = "";
      return NewString;
    }

  }
}
