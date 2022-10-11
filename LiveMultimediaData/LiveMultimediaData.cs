using System;

namespace LiveMultimediaData
{
  public enum enumTypeLog : int { Information = 1, Warning=2, Error=3 };

  public enum enumTypeUser : int { Local = 1, Remote=2 };

  public enum enumTypeMultimediaItem : int { Source = 1, Folder = 2, Audio = 3, Video = 4, Picture = 5, Document = 6, Unsupported = 99 }

  public enum enumActionMultimedia : int { StartTransferAttributes = 1, StartTransfer = 2, StopTransfer = 3}

  public enum enumTopic : int { Service=1, Local=2, Remote=3}

  public class MultimediaFile
  {
    public string FullPath;
    public string DisplayName;
    public string MultimediaFileGUID;
    public string TypeMultimedia;
    public string Album;
    public bool isSelectMultimediaFile;
    public string Title;
    public string Subject;
    public string Category;
    public string Keywords;
    public string Comments;
    public string Source;
    public string Author;
    /*
  public string FullPath
  {
    get { return FullPath; }
    set { FullPath = value; }
  }

  public string DisplayName
  {
    get { return DisplayName; }
    set { DisplayName = value; }
  }

  public string GUID
  {
    get { return GUID; }
    set { GUID = value; }
  }
    */
  }

  public class ClientInternetBrowser
  {
    private const string BrowserNameInternetExplorer = "IE";
    private const string BrowserNameOpera = "Opera";
    private const string BrowserNameChrome = "Chrome";
    private const string BrowserNameFirefox = "Firefox";
    private const string BrowserNameSafari = "Safari";

    public string BrowserName = "";
    public string BrowserVersion = "";

    private bool isBrowserInternetExplorer()
    {
      if (BrowserName == BrowserNameInternetExplorer)
      { return true; }
      else { return false; }
    }

    private bool isBrowserOpera()
    {
      if (BrowserName == BrowserNameOpera)
      { return true; }
      else { return false; }
    }

    private bool isBrowserChrome()
    {
      if (BrowserName == BrowserNameChrome)
      { return true; }
      else { return false; }
    }

    private bool isBrowserFirefox()
    {
      if (BrowserName == BrowserNameFirefox)
      { return true; }
      else { return false; }
    }

    private bool isBrowserSafari()
    {
      if (BrowserName == BrowserNameSafari)
      { return true; }
      else { return false; }
    }

    private bool isSupportMP3()
    {
      bool isSupportIE = (isBrowserInternetExplorer() == true && BrowserVersion == "9.0");
      bool isSupportChrome = (isBrowserChrome() == true && BrowserVersion == "8");

      if (isSupportIE == true || isSupportChrome == true)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    private bool isSupportOGG()
    {
      bool isSupportOpera = (isBrowserOpera() == true /*&& BrowserVersion == "11"*/);
      bool isSupportChrome = (isBrowserChrome() == true && BrowserVersion == "8");
      bool isSupportFirefox = (isBrowserFirefox() == true && BrowserVersion == "4");

      if (isSupportOpera == true || isSupportChrome == true || isSupportFirefox == true)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public string GetSupportedTypeAudio()
    {
      string SupportedTypeAudio = "wav";

      if (isSupportMP3() == true) SupportedTypeAudio = "mp3";
      if (isSupportOGG() == true) SupportedTypeAudio = "ogg";

      return SupportedTypeAudio;
    }


  }

  public class MultimediaSource
  {
    public int IdTypeMultimediaSource;
    public int IdParentTypeMultimediaSource;
    public string NameMultimediaSource;
    public string TitleMultimediaSource;
    public int StyleWidth;
    public int StyleHeight;
    public string StyleForeColor;
    public string StyleBackColor;
    public string StyleBorderColor;
    public Int32 StyleFontSize;        
    public string StyleName;
    public string Style;
    public int OrderSort;
    public bool IsAvailable;
    public bool IsEnabled;
    public bool IsTranslateTitle;
    public bool IsHasChild;
    public string Description;
    public int IdPublisher;
    public string ImageUrlLarge;
    public string ImageUrlMedium;
    public string ImageUrlSmall;
  }

  public class MultimediaItem
  {
    public string Id { get; set; }
    public int IdSource { get; set; }
    public string Name { get; set; }
    public int StyleWidth { get; set; }
    public int StyleHeight { get; set; }
    public string StyleForeColor { get; set; }
    public string StyleBackColor { get; set; }
    public string StyleFontFamily { get; set; }
    public Int32 StyleFontSize { get; set; }
    public bool IsEnabled { get; set; }
    public string Description { get; set; }
    public string UrlImage { get; set; }
    public string Url { get; set; }
    public string TypeItem { get; set; }
  }

  /*
      public string Album { get; set; }    
      public string Subject;
      public string Category;
      public string Keywords;
      public string Comments;

      public string Title { get; set; }    
      public string Picture { get; set; }
      public string Description { get; set; }
      public string Type { get; set; }    
      public string Artist { get; set; }
      public string AlbumArtist { get; set; }
      public string Genre { get; set; }
      public int Duration { get; set; }    
   */


  public class PlaylistObject
  {
    public Int64 IdPlaylist;
    public string Playlist;
  }

  public class MultimediaServer
  {
    public int IdMultimediaServer;
    public string NameMultimediaServer;
    public string AccountKey;
    public enumTypeUser TypeUser;
  }

  public class BreadCrumps
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsRequiredAuthorization { get; set; }
  }

  //public class AccountKey
  //{
  //  public int IdAccountKey;
  //  public string AccountKey2;
  //}

  //public class Publisher
  //{
  //  public int IdPublisher;
  //  public string Company;
  //  public string Server;
  //  public string Note;
  //}

  //public class PublisherAccountKey
  //{
  //  public int IdPublisher;
  //  public string AccountKey;
  //  public enumTypeUser TypeUser;
  //  public string Note;
  //}

}
