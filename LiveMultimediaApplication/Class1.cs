using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Windows.Threading;
using System.IO;
using System.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using Microsoft.Win32;

using LiveMultimediaApplication.LiveMultimediaService;

namespace LiveMultimediaApplication
{
  public partial class LiveMultimediaWindow : Window
  {
    #region Define common variable

    readonly string AccountKey = "***";

    //readonly string TitleLiveMultimediaSystem = "Live Multimedia Server";

    enumLiveMultimedia StatusLiveMultimediaServer;

#if (RELEASE)
    readonly string TitleLiveMultimediaServer = "Live Multimedia Server";
#elif (DEBUG)
    readonly string TitleLiveMultimediaServer = "Live Multimedia Server. Debug version";    
#else
    readonly string TitleLiveMultimediaServer = "Live Multimedia Server. Test version";
#endif

    private ObservableCollection<MultimediaFile> ListMultimediaFile = new ObservableCollection<MultimediaFile>();
    private ConcurrentBag<MultimediaFile> ListMultimediaFileBag;
    private ConcurrentBag<Tuple<string, CancellationTokenSource>> ListJob = new ConcurrentBag<Tuple<string, CancellationTokenSource>>();

    List<Tuple<string, List<string>>> ListMultimediaExtension = new List<Tuple<string, List<string>>>();
    List<string> ListMultimediaFileExtension;
    string MultimediaFileExtension;

    private string UserToken="";
    private bool isExitFromApplication = false;
    private int MultimediaFileBufferLength;

    private LiveMultimediaServiceConnection LiveConnection;// = new LiveMultimediaServiceConnection();

    SolidColorBrush BrushConnect = new SolidColorBrush(Colors.LawnGreen);
    SolidColorBrush BrushStop = new SolidColorBrush(Colors.Yellow);
    SolidColorBrush BrushError = new SolidColorBrush(Colors.Red);
    SolidColorBrush BrushBackground = new SolidColorBrush(Colors.SkyBlue);
    SolidColorBrush BrushControl = new SolidColorBrush(Colors.White);
    SolidColorBrush BrushComboboxBackground = System.Windows.Media.Brushes.AliceBlue;
    SolidColorBrush BrushComboboxForeground = System.Windows.Media.Brushes.Black;

    private System.Windows.Forms.NotifyIcon NotifyIconApplication;
    private System.Windows.Forms.ContextMenu NotifyContextMenu;
    private System.ComponentModel.IContainer components;

    private System.Windows.Forms.MenuItem NotifyMenuItemStart;
    private System.Windows.Forms.MenuItem NotifyMenuItemOpen;
    private System.Windows.Forms.MenuItem NotifyMenuItemViewEvents; 
    private System.Windows.Forms.MenuItem NotifyMenuItemAbout;
    private System.Windows.Forms.MenuItem NotifyMenuItemExit;

    private enum enumLiveMultimedia : int { Connected, Disconnected, Error, LoadDataStart, RequestError, RequestOk, Debug,
    UpdateListMultimediaStart, UpdateListMultimediaStartUpload, UpdateListMultimediaStop, RemoveListMultimediaStart
    };

    private CancellationTokenSource tokenSource=null;
    private CancellationToken cancellationToken;

    private string ErrorRequest = "";

    private TimeSpan DelayRequestMilliseconds;
    private TimeSpan DelayMaxSeconds;
    private TimeSpan DelayErrorSeconds;
    private int IdleTimeSeconds;
    private int MaxConnection;

    Storyboard StoryboardStatus1 = new Storyboard();
    DoubleAnimation myDoubleAnimation = new DoubleAnimation();

    private static long usingResource = 0;

    private string RegistryKeyName = "Software" + "\\" + "JetSAS" + "\\" + "LiveMultimediaMarket";
    private string RegistryValueName = "Language";

    #region Define var for localization
    private string ErrorUsernameEmpty;
    private string ErrorPasswordEmpty;
    private string ErrorProxyEmpty;
    private string ErrorProxyPortEmpty;
    private string ErrorProxyPort;
    private string ErrorProxyAddress;
    private string ErrorProxyFormat;
    private string ErrorLogon;

    private string SelectFileTitle;
    private string DeleteQuestion;
    private string DeleteTitle;
    private string ExtensionAllFiles;

    #endregion Define var for localization

    #region Define vars for ToolTips
    private System.Windows.Controls.ToolTip TipUsername;
    private System.Windows.Controls.ToolTip TipErrorUsername;
    private bool IsAlwaysShowTip=true;
    #endregion Define vars for ToolTips

    //bool IsErrorConnectService = true;

    #endregion Define common variable

    private void InitEndPoint()
    {
      try
      {
        LiveConnection = new LiveMultimediaServiceConnection();
      }
      catch (Exception ex)
      {
        ShowErrorMessage(ex, "InitEndPoint");
      }
    }

    private void LoadNotifayIcon()
    {      
      this.NotifyContextMenu = new System.Windows.Forms.ContextMenu();
      
      this.NotifyMenuItemStart = new System.Windows.Forms.MenuItem();
      this.NotifyMenuItemOpen = new System.Windows.Forms.MenuItem();
      this.NotifyMenuItemViewEvents = new System.Windows.Forms.MenuItem();
      this.NotifyMenuItemAbout = new System.Windows.Forms.MenuItem();
      this.NotifyMenuItemExit = new System.Windows.Forms.MenuItem();

      // Initialize NotifyContextMenu
      this.NotifyContextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
        this.NotifyMenuItemStart,
        this.NotifyMenuItemOpen,
        this.NotifyMenuItemViewEvents,
        this.NotifyMenuItemAbout,
        this.NotifyMenuItemExit });

      // Initialize menuItemStart
      this.NotifyMenuItemStart.Index = 0;
      this.NotifyMenuItemStart.Name = "Start";
      this.NotifyMenuItemStart.Click += new System.EventHandler(this.NotifyMenuItemStart_Click);
      this.NotifyMenuItemStart.DefaultItem = true;

      // Initialize menuItemOpen
      this.NotifyMenuItemOpen.Index = 1;
      this.NotifyMenuItemOpen.Name = "Open";
      this.NotifyMenuItemOpen.Click += new System.EventHandler(this.NotifyMenuItemOpen_Click);
      this.NotifyMenuItemOpen.Visible = false;

      // Initialize menuItemViewEvents
      this.NotifyMenuItemViewEvents.Index = 2;
      this.NotifyMenuItemViewEvents.Name = "View Events";
      this.NotifyMenuItemViewEvents.Click += new System.EventHandler(this.NotifyMenuItemViewEvents_Click);
      this.NotifyMenuItemViewEvents.Visible = false;

      // Initialize menuItemAbout
      this.NotifyMenuItemAbout.Index = 3;
      this.NotifyMenuItemAbout.Name = "About";
      this.NotifyMenuItemAbout.Click += new System.EventHandler(this.NotifyMenuItemAbout_Click);      

      // Initialize menuItemExit
      this.NotifyMenuItemExit.Index = 4;
      this.NotifyMenuItemExit.Name = "Exit";
      this.NotifyMenuItemExit.Click += new System.EventHandler(this.NotifyMenuItemExit_Click);

      this.components = new System.ComponentModel.Container();

      var LiveMultimediaMarketIcon = new Icon((Icon)Properties.Resources.ResourceManager.GetObject("LiveMultimediaMarket"), 32, 32);
      this.NotifyIconApplication = new NotifyIcon(this.components);
      this.NotifyIconApplication.Icon = LiveMultimediaMarketIcon;// new System.Drawing.Icon("IconLiveMultimediaSystem.ico");
      this.NotifyIconApplication.ContextMenu = this.NotifyContextMenu;
      this.NotifyIconApplication.Text = TitleLiveMultimediaServer;
      this.NotifyIconApplication.Visible = true;

      // Handle the DoubleClick event to activate the form.
      this.NotifyIconApplication.DoubleClick += new System.EventHandler(this.NotifyIconApplication_DoubleClick);
    }

    private void LoadBrushElements()
    {
      this.Background = BrushBackground;
      this.Foreground = BrushControl;
      this.BorderBrush = BrushControl;

      this.ShowInTaskbar = true;
      this.WindowStyle = WindowStyle.SingleBorderWindow;

      this.comboAddMultimediaFolder.Background = BrushComboboxBackground;
      this.comboAddMultimediaFolder.Foreground = BrushComboboxForeground;
      this.comboAddMultimediaFolder.BorderBrush = BrushComboboxBackground;

      this.lblUsername.Background = BrushBackground;
      this.lblUsername.Foreground = BrushControl;

      this.tbUsername.Background = BrushBackground;
      this.tbUsername.Foreground = BrushControl;
      this.tbUsername.BorderBrush = BrushControl;

      this.lblPassword.Background = BrushBackground;
      this.lblPassword.Foreground = BrushControl;

      this.pbPassword.Background = BrushBackground;
      this.pbPassword.Foreground = BrushControl;
      this.pbPassword.BorderBrush = BrushControl;

      this.chkAutoConnect.Background = BrushBackground;
      this.chkAutoConnect.Foreground = BrushControl;
      this.chkAutoConnect.BorderBrush = BrushControl;

      this.chkUseProxy.Background = BrushBackground;
      this.chkUseProxy.Foreground = BrushControl;
      this.chkUseProxy.BorderBrush = BrushControl;

      this.tbProxyAddress.Background = BrushBackground;
      this.tbProxyAddress.Foreground = BrushControl;
      this.tbProxyAddress.BorderBrush = BrushControl;

      this.lblProxyPort.Background = BrushBackground;
      this.lblProxyPort.Foreground = BrushControl;

      this.tbProxyPort.Background = BrushBackground;
      this.tbProxyPort.Foreground = BrushControl;
      this.tbProxyPort.BorderBrush = BrushControl;

      this.lbMultimedia.Background = BrushBackground;
      this.lbMultimedia.Foreground = BrushControl;
      this.lbMultimedia.BorderBrush = BrushControl;

      this.pbMultimedia.Background = BrushBackground;
      this.pbMultimedia.Foreground = BrushConnect;
      this.pbMultimedia.BorderBrush = BrushBackground;
      this.pbMultimedia.MaxHeight = this.lbMultimedia.MaxHeight;
      this.pbMultimedia.Height = this.lbMultimedia.Height + 5;

      this.Separator1.Background = BrushBackground;
      this.Separator1.BorderBrush = BrushBackground;
      this.Separator2.Background = BrushBackground;
      this.Separator2.BorderBrush = BrushBackground;
      this.Separator3.Background = BrushBackground;
      this.Separator3.BorderBrush = BrushBackground;
      this.Separator4.Background = BrushBackground;
      this.Separator4.BorderBrush = BrushBackground;
      this.Separator5.Background = BrushBackground;
      this.Separator5.BorderBrush = BrushBackground;

      this.lblRequestError.Background = BrushBackground;
      this.lblRequestError.Foreground = BrushControl;

      this.sbBottom.Background = BrushBackground;
      this.sbBottom.Foreground = BrushControl;
      this.sbBottom.BorderBrush = BrushControl;

      this.lblStatus1FilesInList.Background = BrushBackground;
      this.lblStatus1FilesInList.Foreground = BrushControl;
      this.lblStatus1FilesInList.BorderBrush = BrushControl;

      this.lblStatus1FilesInListResult.Background = BrushBackground;
      this.lblStatus1FilesInListResult.Foreground = BrushControl;
      this.lblStatus1FilesInListResult.BorderBrush = BrushControl;

      this.lblStatus2SearchFiles.Background = BrushBackground;
      this.lblStatus2SearchFiles.Foreground = BrushControl;
      this.lblStatus2SearchFiles.BorderBrush = BrushControl;

      this.lblStatus2SearchFilesResult.Background = BrushBackground;
      this.lblStatus2SearchFilesResult.Foreground = BrushControl;
      this.lblStatus2SearchFilesResult.BorderBrush = BrushControl;

      this.Sep1.Background = BrushControl;
      this.Sep1.BorderBrush = BrushControl;

      this.Sep2.Background = BrushControl;
      this.Sep2.BorderBrush = BrushControl;

      this.Sep3.Background = BrushControl;
      this.Sep3.BorderBrush = BrushControl;

      this.Sep1.Visibility = Visibility.Hidden;
      this.Sep2.Visibility = Visibility.Hidden;
      this.Sep3.Visibility = Visibility.Hidden;
    }

    private void LoadToolTip()
    {
      System.Windows.Controls.ToolTip ToolTip;

      ToolTip = new System.Windows.Controls.ToolTip();
      ToolTip.Content = "This is Username";
      //ToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
      //ToolTip.PlacementRectangle = new Rect(150, 0, 0, 0);

      ToolTip.HorizontalOffset = 5;
      ToolTip.VerticalOffset = 16;
      
      //Create BulletDecorator and set it as the tooltip content.
      var bdec = new System.Windows.Controls.Primitives.BulletDecorator();
      var littleEllipse = new System.Windows.Shapes.Ellipse();
      var littleEllipse2 = new System.Windows.Shapes.Rectangle();
      littleEllipse.Width = 20;
      littleEllipse.Fill = System.Windows.Media.Brushes.Blue;
      bdec.Bullet = littleEllipse;
      TextBlock tipText = new TextBlock();
      tipText.Text = "Uses the ToolTip class";
      bdec.Child = tipText;
      //t.Content = bdec;
      
      this.tbUsername.ToolTip = ToolTip;

      ToolTip = new System.Windows.Controls.ToolTip();
      ToolTip.Content = "This is Password";
      ToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
      ToolTip.PlacementRectangle = new Rect(150, 0, 0, 0);
      ToolTip.HorizontalOffset = 5;
      ToolTip.VerticalOffset = 17;
      this.pbPassword.ToolTip = ToolTip;

      ToolTip = new System.Windows.Controls.ToolTip();
      ToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
      ToolTip.PlacementRectangle = new Rect(150, 0, 0, 0);
      ToolTip.HorizontalOffset = 5;
      ToolTip.VerticalOffset = 15;
      ToolTip.Content = "Press Add File";
      this.cmdAddMultimediaFile.ToolTip = ToolTip;

      ToolTip = new System.Windows.Controls.ToolTip();
      ToolTip.Content = "Press Add Folder";
      ToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
      ToolTip.PlacementRectangle = new Rect(150, 0, 0, 0);
      ToolTip.HorizontalOffset = 5;
      ToolTip.VerticalOffset = 5;
      this.cmdAddMultimediaFolder.ToolTip = ToolTip;

      // Set up the delays for the ToolTip.
      //toolTip1.AutoPopDelay = 5000;
      //toolTip1.InitialDelay = 1000;
      //toolTip1.ReshowDelay = 500;
      // Force the ToolTip text to be displayed whether or not the form is active.
      //toolTip1.ShowAlways = true;

      // Set up the ToolTip text for the Button and Checkbox.
      //toolTip1.SetToolTip(this.tbUsername, "My button1");
      //toolTip1.SetToolTip(this.checkBox1, "My checkBox1");

    }

    //На будущее
    //private void LoadSpeech()
    //{
    //  var s = "data:audio/mp3;base64,";

    //  byte[] MultimediaFileBuffer;
    //  using (var fs = new FileStream(@"\\192.168.1.254\volume_1\Multimedia\music\Русские\!Разное\korova.mp3", FileMode.Open, FileAccess.Read, FileShare.Read))
    //  {
    //    MultimediaFileBuffer = new byte[fs.Length];
    //    fs.Read(MultimediaFileBuffer, 0, (int)fs.Length);
    //  }

    //  //var base64String = Convert.ToBase64String(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length);
    //  //base64String = base64String.Substring(0, 65000);      
    //  //s = s + base64String;

    //  long arrayLength = (long)((4.0d / 3.0d) * MultimediaFileBuffer.Length);
    //  // If array length is not divisible by 4, go up to the next multiple of 4.
    //  if (arrayLength % 4 != 0) arrayLength += 4 - arrayLength % 4;

    //  char[] base64CharArray = new char[arrayLength];
    //  Convert.ToBase64CharArray(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length, base64CharArray, 0);
    //  s += base64CharArray.ToString();

    //  Uri uridata = new Uri(s);

    //  //UriBuilder ProxyUri = new UriBuilder("https://livemultimediastorage.blob.core.windows.net/download/korova.mp3");
    //  //UriBuilder ProxyUri = new UriBuilder(@"X:\Multimedia\music\Русские\!Разное\korova.mp3");
    //  //UriBuilder ProxyUri = new UriBuilder("http://storage.live-mm.com/download/korova.mp3");    

    //  //Uri uridata = new Uri("https://livemultimediastorage.blob.core.windows.net/download/korova.mp3", UriKind.Absolute);
    //  //Uri uridata = ProxyUri.Uri;

    //  //this.me.LoadedBehavior = MediaState.Manual;
    //  //this.me.Source = uridata;
    //  //this.me.Play();

    //  MediaPlayer player = new MediaPlayer();
    //  //player.Open(new Uri(@"X:\Multimedia\music\Русские\!Разное\korova.mp3", UriKind.Absolute));
    //  player.Open(uridata);
    //  VideoDrawing aVideoDrawing = new VideoDrawing();
    //  aVideoDrawing.Rect = new Rect(0, 0, 100, 100);
    //  aVideoDrawing.Player = player;
    //  player.Play();        


    //  //// Initialize a new instance of the SpeechSynthesizer.
    //  //SpeechSynthesizer synth = new SpeechSynthesizer();

    //  //// Configure the audio output. 
    //  //synth.SetOutputToDefaultAudioDevice();

    //  //// Speak a string asynchronously.
    //  //synth.SpeakAsync("Now enter the password");


    //  //using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
    //  //{
    //  //  //var ci = "en-US";
    //  //  //var ci = "ru-RU";
    //  //  //foreach (InstalledVoice voice in synthesizer.GetInstalledVoices(new CultureInfo(ci)))
    //  //  foreach (InstalledVoice voice in synthesizer.GetInstalledVoices())
    //  //  {
    //  //    VoiceInfo info = voice.VoiceInfo;
    //  //    OutputVoiceInfo(info);
    //  //  }

    //  //  // Output information about the current voice.
    //  //  Console.WriteLine();
    //  //  Console.WriteLine("Current voice:");
    //  //  OutputVoiceInfo(synthesizer.Voice);
    //  //}

    //}

    //private static void OutputVoiceInfo(VoiceInfo info)
    //{
    //  Console.WriteLine("  Name: {0}, culture: {1}, gender: {2}, age: {3}.", info.Name, info.Culture, info.Gender, info.Age);
    //  Console.WriteLine("    Description: {0}", info.Description);
    //}

    private void InitCultureName()
    {
      try
      {
        using (var rkLiveMultimediaSystem = Registry.CurrentUser.CreateSubKey(RegistryKeyName))
        {
          LiveMultimediaServiceConnection.Language = Convert.ToString(rkLiveMultimediaSystem.GetValue(RegistryValueName, string.Empty));
          if (!CheckGoodString(LiveMultimediaServiceConnection.Language))
          {
            var windowSelectLanguage = new SelectLangauge();
            windowSelectLanguage.ShowDialog();
            rkLiveMultimediaSystem.SetValue(RegistryValueName, LiveMultimediaServiceConnection.Language);
          }
        }
      }
      catch (Exception ex)
      {
        LiveMultimediaServiceConnection.Language = LiveMultimediaServiceConnection.LanguageDefault;
        ShowErrorMessage(ex, "InitCultureName");        
      }
    }

    private void LoadCulture()
    {
      //int countRepeatConnect = 1, maxCountRepeatConnect = 3;      
      //Exception exceptionService = null;

      //while (countRepeatConnect<=maxCountRepeatConnect && IsErrorConnectService)
      //{
      //  try
      //  {
      //    using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
      //    {
      //      var returnValue = await LiveMultimediaService.LocalGetLocalizationAsync(AccountKey, LiveMultimediaServiceConnection.Language);
      //      if (!CheckGoodString(returnValue.Item2))
      //      {
      //        LiveMultimediaServiceConnection.ListLocalization = returnValue.Item1.ToList();
      //      }
      //      else
      //      {
      //        LiveMultimediaServiceConnection.ListLocalization = null;
      //      }
      //    }
      //    IsErrorConnectService = false;
      //  }
      //  catch (Exception ex)
      //  {
      //    IsErrorConnectService = true;
      //    countRepeatConnect++;
      //    exceptionService = ex;
      //    LiveMultimediaServiceConnection.ListLocalization = null;
      //    await Task.Delay(500);
      //  }        
      //}
      //if (IsErrorConnectService)
      //{
      //  System.Windows.MessageBox.Show("Error connect to Live Multimedia Market", TitleLiveMultimediaServer, MessageBoxButton.OK);
      //  await ApplicationShutdown();
      //}

      try
      {
        using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnValue = LiveMultimediaService.LocalGetLocalization(AccountKey, LiveMultimediaServiceConnection.Language);
          if (!CheckGoodString(returnValue.Item3))
          {
            LiveMultimediaServiceConnection.ListLocalization = returnValue.Item1.ToList();
          }
          else
          {
            LiveMultimediaServiceConnection.ListLocalization = null;
          }
        }
      }
      catch (Exception ex)
      {
        LiveMultimediaServiceConnection.ListLocalization = null;
        ShowErrorMessage(ex, "LoadCulture");
      }
    }

    private void SetCulture()
    {
      this.NotifyMenuItemStart.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemStart_Start", "Start");
      this.NotifyMenuItemOpen.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemOpen", "Open");
      this.NotifyMenuItemViewEvents.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemViewEvents", "View Events");
      this.NotifyMenuItemAbout.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemAbout", "About");
      this.NotifyMenuItemExit.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemExit", "Exit");

      this.cmdAddMultimediaFile.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdAddMultimediaFile", "Add File");
      this.cmdAddMultimediaFolder.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdAddMultimediaFolder", "Add Folder");
      this.cmdConnectDisconnect.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdConnectDisconnect_Start", "Start");
      this.cmdSelectLanguage.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdSelectLanguage", "Language");
      this.cmdAbout.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdAbout", "About");
      this.cmdExit.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdExit", "Exit");

      this.lblUsername.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblUsername", "Username");
      this.lblPassword.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblPassword", "Password");
      this.chkAutoConnect.Content = LiveMultimediaServiceConnection.GetElementLocalization("chkAutoConnect", "Auto connect after start");
      this.chkUseProxy.Content = LiveMultimediaServiceConnection.GetElementLocalization("chkUseProxy", "Use Proxy");
      this.lblProxyPort.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblProxyPort", "Port");
      this.lblStatus1FilesInList.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblStatus1FilesInList", "Number of files in the list");
      this.lblStatus2SearchFiles.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblStatus2SearchFiles", "Search of files");

      ErrorUsernameEmpty = LiveMultimediaServiceConnection.GetElementLocalization("ErrorUsernameEmpty", "Username can't be empty");
      ErrorPasswordEmpty = LiveMultimediaServiceConnection.GetElementLocalization("ErrorPasswordEmpty", "Password can't be empty");
      ErrorProxyEmpty = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyEmpty", "Proxy Address can't be empty");
      ErrorProxyPortEmpty = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyPortEmpty", "Proxy Port can't be empty");
      ErrorProxyPort = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyPort", "Proxy port error");
      ErrorProxyAddress = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyAddress", "Proxy Address error");
      ErrorProxyFormat = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyFormat", "Use this format of proxy server");
      ErrorLogon = LiveMultimediaServiceConnection.GetElementLocalization("ErrorLogon", "Logon failure to");

      DeleteQuestion = LiveMultimediaServiceConnection.GetElementLocalization("DeleteQuestion", "Do you want to delete selected") + "?";
      DeleteTitle = LiveMultimediaServiceConnection.GetElementLocalization("DeleteTitle", "Delete files from list");
      SelectFileTitle = LiveMultimediaServiceConnection.GetElementLocalization("SelectFileTitle", "Select multimedia files");

      ExtensionAllFiles = LiveMultimediaServiceConnection.GetElementLocalization("ExtensionAllFiles", "All files");

      LoadTypeMultimedia();

#if (RELEASE)
      this.StatusLiveMultimediaSystem.ToolTip = LiveMultimediaServiceConnection.GetElementLocalization("StatusLiveMultimediaSystem", "State of the") + " " + TitleLiveMultimediaServer;
#endif
    }

    private void LoadTypeMultimedia() //string ExtensonSetting, out List<Tuple<string, List<string>>> ListExtension, out string MultimediaExtension)
    {
      #region Define vars
      LocalizationElement LocalizationExtensionMultimedia;
      #endregion Define vars

      //if (IsErrorConnectService)
      //  return;

      #region Try
        try
      {        
        #region Get localization multimedia extension

        #region Request webAPI
        //var url = "http://service.live-mm.com:8080/LiveMultimediaService.svc/api/v1/TypeMultimedia/" + LiveMultimediaServiceConnection.Language + "?";
        ////var url = "http://service.live-mm.com:8080/LiveMultimediaService.svc/api/v1/GetTypeMultimedia?accountkey=" + AccountKey + "&language=" + LiveMultimediaServiceConnection.Language;
        ////var url = "http://service.live-mm.com:8080/LiveMultimediaService.svc/api/v1/GetTypeMultimedia/"+LiveMultimediaServiceConnection.Language;
        ////var url = "http://service.live-mm.com:8080/LiveMultimediaService.svc/api/v1/GetTypeMultimedia/" + AccountKey + "/" + LiveMultimediaServiceConnection.Language;
        //var request = WebRequest.Create(url) as HttpWebRequest;
        //request.Method = "POST";
        ////request.Method = "GET";
        ////request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
        //request.ContentType = "application/x-www-form-urlencoded";
        ////request.ContentType = "application/xml";
        ////request.ContentType = "text/xml";

        ////string postContent = String.Format("accountkey={0}&language={1}", System.Web.HttpUtility.UrlEncode(AccountKey), System.Web.HttpUtility.UrlEncode(LiveMultimediaServiceConnection.Language));
        //string postContent = String.Format("AccountKey={0}", System.Web.HttpUtility.UrlEncode(AccountKey));
        ////using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
        ////{
        ////  //writer.Write(postContent);
        ////  //writer.Write("accountkey=" + AccountKey + "&language=" + LiveMultimediaServiceConnection.Language);
        ////  writer.Write("AccountKey=" + AccountKey);
        ////}

        ////byte[] bytes = Encoding.ASCII.GetBytes("AccountKey=" + AccountKey);
        //byte[] bytes = Encoding.ASCII.GetBytes(postContent);
        //request.ContentLength = bytes.Length;
        //using (Stream outputStream = request.GetRequestStream())
        //{
        //  outputStream.Write(bytes, 0, bytes.Length);
        //}

        //var response = request.GetResponse() as HttpWebResponse;
        //var data = new System.Runtime.Serialization.DataContractSerializer(typeof(Tuple<LocalizationElement, string>));
        //var ret = data.ReadObject(response.GetResponseStream()) as Tuple<LocalizationElement, string>;

        ////using (var responseStream = response.GetResponseStream())
        ////{                   
        ////  var returnvalue1 = data.ReadObject(responseStream);
        ////}
        //response.Close();
        #endregion Request webAPI

        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnValue = LiveMultimediaService.GetTypeMultimedia(AccountKey, LiveMultimediaServiceConnection.Language);

          if (!CheckGoodString(returnValue.Item2))
          {
            LocalizationExtensionMultimedia = returnValue.Item1;
          }
          else
          {
            LocalizationExtensionMultimedia = new LocalizationElement();
          }
        }
        #endregion Get localization multimedia extension

        if (CheckGoodString(LocalizationExtensionMultimedia.ElementValue))
        {
          this.comboAddMultimediaFolder.Items.Clear();
          ListMultimediaExtension.Clear();

          #region Multimedia Extensions
          // Example:
          // Word Documents|*.doc|Excel Worksheets|*.xls|PowerPoint Presentations|*.ppt|Office Files|*.doc;*.xls;*.ppt|All Files|*.*

          string TypeMultimediaItem, ExtensionMultimediaItem, AllExtensionMultimediaItem;

          var aMultimediaFileExtensonSetting = LocalizationExtensionMultimedia.ElementValue.Split(new Char[] { '&' });
          string[] aExtensionMultimediaItem;
          string FilterExtension;

          List<string> ListExtension;

          MultimediaFileExtension = ""; AllExtensionMultimediaItem = "";
          foreach (string SingleData in aMultimediaFileExtensonSetting)
          {
            var Index = SingleData.IndexOf('=');
            TypeMultimediaItem = SingleData.Substring(0, Index);
            ExtensionMultimediaItem = SingleData.Substring(Index + 1);

            aExtensionMultimediaItem = ExtensionMultimediaItem.Split(new Char[] { '|' });
            FilterExtension = "";
            ListExtension = new List<string>();
            foreach (var item in aExtensionMultimediaItem)
            {
              ListExtension.Add(item);
              FilterExtension += "*." + item + ";";
            }
            AllExtensionMultimediaItem += FilterExtension;
            FilterExtension = FilterExtension.Remove(FilterExtension.Length - 1, 1);
            ListMultimediaExtension.Add(new Tuple<string, List<string>>(TypeMultimediaItem, ListExtension));

            MultimediaFileExtension += TypeMultimediaItem + "|" + FilterExtension + "|";
          }
          AllExtensionMultimediaItem = AllExtensionMultimediaItem.Remove(AllExtensionMultimediaItem.Length - 1, 1);
          MultimediaFileExtension += ExtensionAllFiles + "|" + AllExtensionMultimediaItem;

          #endregion Multimedia Extensions

          #region Buttons for select type multimedia

          #region Add button
          var buttonAddFolder = new System.Windows.Controls.Button();
          buttonAddFolder.Content = ExtensionAllFiles;
          buttonAddFolder.Width = this.cmdAddMultimediaFile.Width;
          buttonAddFolder.Height = this.cmdAddMultimediaFile.Height;
          buttonAddFolder.Background = BrushComboboxBackground;
          buttonAddFolder.Foreground = BrushComboboxForeground;
          buttonAddFolder.BorderBrush = System.Windows.Media.Brushes.Transparent;
          buttonAddFolder.Click += buttonAddFolder_Click;

          this.comboAddMultimediaFolder.Items.Add(buttonAddFolder);
          #endregion Add button

          foreach (var item in ListMultimediaExtension.GroupBy(MultimediaExtension => MultimediaExtension.Item1))
          {
            #region Add button
            buttonAddFolder = new System.Windows.Controls.Button();
            buttonAddFolder.Content = item.Key;
            buttonAddFolder.Width = this.cmdAddMultimediaFile.Width;
            buttonAddFolder.Height = this.cmdAddMultimediaFile.Height;
            buttonAddFolder.Background = BrushComboboxBackground;
            buttonAddFolder.Foreground = BrushComboboxForeground;
            buttonAddFolder.BorderBrush = System.Windows.Media.Brushes.Transparent;
            buttonAddFolder.Click += buttonAddFolder_Click;

            this.comboAddMultimediaFolder.Items.Add(buttonAddFolder);
            #endregion Add button
          }

          this.comboAddMultimediaFolder.SelectedIndex = 0;
          #endregion Buttons for select type multimedia
        }
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        ShowErrorMessage(ex, "LoadTypeMultimedia");
      }
      #endregion Catch
    }

    public async void InitComponent()
    {
      try
      {
        InitEndPoint();

        System.Windows.Application app = System.Windows.Application.Current;
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                
        this.Title = TitleLiveMultimediaServer;
        this.comboAddMultimediaFolder.Visibility = Visibility.Collapsed;
        this.chkUseProxy.IsChecked = false;
        this.tbUsername.Focus();

#if (RELEASE)
        this.chkAutoConnect.Visibility = Visibility.Hidden;
        this.tbDebug.Text = "";
        this.tbDebug.Visibility = Visibility.Hidden;
#elif (DEBUG)        
        this.tbDebug.Visibility = Visibility.Visible;
        this.StatusLiveMultimediaSystem.ToolTip = LiveConnection.EndPoint.Uri.ToString();        
        this.tbDebug.Text = "";
        //this.chkAutoConnect.IsChecked = true;

        this.tbUsername.Text = "";
        this.pbPassword.Password = "";
#else
        this.tbDebug.Visibility = Visibility.Visible;
        this.StatusLiveMultimediaSystem.ToolTip = LiveConnection.EndPoint.Uri.ToString();
        this.tbDebug.Text = "";
        //this.chkAutoConnect.IsChecked = true;
        this.tbUsername.Text = "***";
        this.pbPassword.Password = "***";
#endif

        myDoubleAnimation.From = 1.0;
        myDoubleAnimation.To = 0.0;
        myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
        myDoubleAnimation.AutoReverse = true;
        myDoubleAnimation.RepeatBehavior = RepeatBehavior.Forever;

        StoryboardStatus1.Children.Add(myDoubleAnimation);
        Storyboard.SetTargetName(myDoubleAnimation, lblStatus2SearchFiles.Name);
        Storyboard.SetTargetProperty(myDoubleAnimation, new PropertyPath(System.Windows.Controls.Label.OpacityProperty));

        LoadNotifayIcon();
        LoadBrushElements();
        LoadToolTip();

        InitCultureName();
        LoadCulture();
        SetCulture();

        SetStatusConnectToLiveMultimedia(enumLiveMultimedia.Disconnected);

        if (this.chkAutoConnect.IsChecked == true) await ApplicationStartStopAsync();
      }
      catch (Exception ex)
      {
        ShowErrorMessage(ex, "InitComponent");
      }
    }

    private async void SetStatusConnectToLiveMultimedia(enumLiveMultimedia StatusLiveMultimediaServer)
    {
      //System.Windows.Controls.ToolTip t;

      this.StatusLiveMultimediaServer = StatusLiveMultimediaServer;

      switch (StatusLiveMultimediaServer)
      {
        #region Connected
        case enumLiveMultimedia.Connected: //Connected (status Started)
          //isDisconnected = false;

          this.cmdAddMultimediaFile.IsEnabled = true;
          this.cmdAddMultimediaFolder.IsEnabled = true;        
          this.tbUsername.IsEnabled = false;
          this.pbPassword.IsEnabled = false;
          this.chkUseProxy.IsEnabled = false;
          this.tbProxyAddress.IsEnabled = false;
          this.tbProxyPort.IsEnabled = false;

          this.cmdConnectDisconnect.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdConnectDisconnect_Stop", "Stop");
          this.cmdConnectDisconnect.IsEnabled = true;
          this.cmdConnectDisconnect.Tag = true;

          this.NotifyMenuItemStart.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemStart_Stop", "Stop");
          this.NotifyMenuItemStart.Enabled = true;

          this.cmdExit.IsEnabled = true;

          this.StatusLiveMultimediaSystem.Visibility = Visibility.Visible;
          await this.StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.Fill = BrushConnect);
          await this.StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.Stroke = BrushConnect);

          //this.StatusLiveMultimediaSystem.Fill = BrushConnect;
          //this.StatusLiveMultimediaSystem.Stroke = BrushConnect;

          //this.pbMultimedia.Visibility = Visibility.Hidden;
          //this.pbMultimedia.IsIndeterminate = false;

          this.lblStatus2SearchFiles.Visibility = Visibility.Hidden;
          this.lblStatus2SearchFilesResult.Visibility = Visibility.Hidden;

          StoryboardStatus1.Stop(this);

          this.lblRequestError.Visibility = Visibility.Hidden;

          //t = this.cmdAddMultimediaFile.ToolTip as System.Windows.Controls.ToolTip;
          //t.IsOpen = true;

          //t = this.cmdAddMultimediaFolder.ToolTip as System.Windows.Controls.ToolTip;
          //t.IsOpen = true;

          break;
        #endregion Connected

        #region Disconnected
        case enumLiveMultimedia.Disconnected: //Start position (status Stopped)
          //isDisconnected = true;

          this.cmdAddMultimediaFile.IsEnabled = false;
          this.cmdAddMultimediaFolder.IsEnabled = false;
          this.cmdSelectLanguage.IsEnabled = true;
          this.tbUsername.IsEnabled = true;
          this.pbPassword.IsEnabled = true;
          this.chkUseProxy.IsEnabled = true;

          if (this.chkUseProxy.IsChecked==true)
          {
            this.tbProxyAddress.IsEnabled = true;
            this.tbProxyPort.IsEnabled = true;
          }
          else
          {
            this.tbProxyAddress.IsEnabled = false;
            this.tbProxyPort.IsEnabled = false;
          }

          cmdConnectDisconnect.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdConnectDisconnect_Start", "Start");
          this.cmdConnectDisconnect.Tag = false;
          NotifyMenuItemStart.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemStart_Start", "Start");

#if (RELEASE)
          this.StatusLiveMultimediaSystem.Visibility = Visibility.Hidden;
#else
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.Fill = BrushStop);
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.Stroke = BrushStop);
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.ToolTip = LiveConnection.EndPoint.Uri.ToString());
#endif

          pbMultimedia.Visibility = Visibility.Hidden;
          pbMultimedia.IsIndeterminate = false;

          lblRequestError.Visibility = Visibility.Hidden;

          lblStatus2SearchFilesResult.Content = "0";
          lblStatus2SearchFiles.Visibility = Visibility.Hidden;
          lblStatus2SearchFilesResult.Visibility = Visibility.Hidden;

          StoryboardStatus1.Stop(this);

          //t = this.cmdAddMultimediaFile.ToolTip as System.Windows.Controls.ToolTip;
          //t.IsOpen = false;

          //t = this.cmdAddMultimediaFolder.ToolTip as System.Windows.Controls.ToolTip;
          //t.IsOpen = false;

          break;
        #endregion Disconnected

        #region Error
        case enumLiveMultimedia.Error: //Error (status Error)
          //isDisconnected = true;

          this.cmdAddMultimediaFile.IsEnabled = false;
          this.cmdAddMultimediaFolder.IsEnabled = false;
          this.tbUsername.IsEnabled = true;
          this.pbPassword.IsEnabled = true;
          this.chkUseProxy.IsEnabled = true;

          if (this.chkUseProxy.IsChecked == true)
          {
            this.tbProxyAddress.IsEnabled = true;
            this.tbProxyPort.IsEnabled = true;
          }
          else
          {
            this.tbProxyAddress.IsEnabled = false;
            this.tbProxyPort.IsEnabled = false;
          }

          this.cmdConnectDisconnect.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdConnectDisconnect_Start", "Start");
          this.NotifyMenuItemStart.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemStart_Start", "Start");

          this.StatusLiveMultimediaSystem.Fill = BrushError;
          this.StatusLiveMultimediaSystem.Stroke = BrushError;

          this.pbMultimedia.Visibility = Visibility.Hidden;
          this.pbMultimedia.IsIndeterminate = false;

          this.lblRequestError.Visibility = Visibility.Hidden;

          break;
        #endregion Error

        #region LoadDataStart
        case enumLiveMultimedia.LoadDataStart: //Start Load multimedia files
          this.cmdAddMultimediaFile.IsEnabled = false;
          this.cmdAddMultimediaFolder.IsEnabled = false;

          this.tbUsername.IsEnabled = false;
          this.pbPassword.IsEnabled = false;
          this.chkUseProxy.IsEnabled = false;
          this.tbProxyAddress.IsEnabled = false;
          this.tbProxyPort.IsEnabled = false;

          this.cmdConnectDisconnect.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdConnectDisconnect_Stop", "Stop");
          this.NotifyMenuItemStart.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemStart_Stop", "Stop");

          //this.pbMultimedia.Visibility = Visibility.Visible;
          //this.pbMultimedia.IsIndeterminate = true;

          this.lblStatus2SearchFiles.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblStatus2SearchFiles_Download", "Download the list of files");
          this.lblStatus2SearchFiles.Visibility = Visibility.Visible;
          this.lblStatus2SearchFilesResult.Visibility = Visibility.Hidden;

          StoryboardStatus1.Begin(this, true);

          this.lblRequestError.Visibility = Visibility.Hidden;

          break;
        #endregion LoadDataStart

        #region RequestOk
        case enumLiveMultimedia.RequestOk: // CheckRequestMultimediaFileBufferAsync Ok
          await cmdAddMultimediaFile.Dispatcher.BeginInvoke(() => this.cmdAddMultimediaFile.IsEnabled = true);
          await this.cmdAddMultimediaFolder.Dispatcher.BeginInvoke(() => this.cmdAddMultimediaFolder.IsEnabled = true);

          //await this.lblRequestError.Dispatcher.BeginInvoke(() => this.lblRequestError.Content = "");
          //await this.lblRequestError.Dispatcher.BeginInvoke(() => this.lblRequestError.ToolTip = "");
          //await this.lblRequestError.Dispatcher.BeginInvoke(() => this.lblRequestError.Visibility = Visibility.Hidden);

          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.Fill = BrushConnect);
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.Stroke = BrushConnect);
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.ToolTip = "");

#if (RELEASE)
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => StatusLiveMultimediaSystem.ToolTip = LiveMultimediaServiceConnection.GetElementLocalization("StatusLiveMultimediaSystem", "State of the") + " " + TitleLiveMultimediaServer);
#elif (DEBUG)
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => StatusLiveMultimediaSystem.ToolTip = LiveConnection.EndPoint.Uri.ToString());
#else
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => StatusLiveMultimediaSystem.ToolTip = LiveConnection.EndPoint.Uri.ToString());
#endif

          break;
        #endregion RequestOk

        #region RequestError
        case enumLiveMultimedia.RequestError: // CheckRequestMultimediaFileBufferAsync Error
          await this.cmdAddMultimediaFile.Dispatcher.BeginInvoke(() => this.cmdAddMultimediaFile.IsEnabled = false);
          await this.cmdAddMultimediaFolder.Dispatcher.BeginInvoke(() => this.cmdAddMultimediaFolder.IsEnabled = false);

          //await this.lblRequestError.Dispatcher.BeginInvoke(() => this.lblRequestError.Content = ErrorRequest);
          //await this.lblRequestError.Dispatcher.BeginInvoke(() => this.lblRequestError.ToolTip = ErrorRequest);
          //await this.lblRequestError.Dispatcher.BeginInvoke(() => this.lblRequestError.Visibility = Visibility.Visible);

          await this.StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.Fill = BrushError);
          await this.StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => this.StatusLiveMultimediaSystem.Stroke = BrushError);

#if (RELEASE)
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => StatusLiveMultimediaSystem.ToolTip = ErrorRequest);
#elif (DEBUG)
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => StatusLiveMultimediaSystem.ToolTip = LiveConnection.EndPoint.Uri.ToString() + ". " + ErrorRequest);
#else
          await StatusLiveMultimediaSystem.Dispatcher.BeginInvoke(() => StatusLiveMultimediaSystem.ToolTip = LiveConnection.EndPoint.Uri.ToString() + ". " + ErrorRequest);
#endif

          //this.lblRequestError.Visibility = Visibility.Visible;

          break;
        #endregion RequestError

        #region UpdateListMultimediaStart
        case enumLiveMultimedia.UpdateListMultimediaStart:
          this.cmdAddMultimediaFile.IsEnabled = false;
          this.cmdAddMultimediaFolder.IsEnabled = false;
          this.cmdSelectLanguage.IsEnabled = false;

          this.lblStatus2SearchFiles.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblStatus2SearchFiles", "Search of files");
          this.lblStatus2SearchFilesResult.Content = "0";
          this.lblStatus2SearchFiles.Visibility = Visibility.Visible;
          this.lblStatus2SearchFilesResult.Visibility = Visibility.Visible;

          StoryboardStatus1.Begin(this, true);

          break;
        #endregion UpdateListMultimediaStart

        #region UpdateListMultimediaStartUpload
        case enumLiveMultimedia.UpdateListMultimediaStartUpload:
          this.cmdAddMultimediaFile.IsEnabled = false;
          this.cmdAddMultimediaFolder.IsEnabled = false;
          this.cmdSelectLanguage.IsEnabled = false;

          this.lblStatus2SearchFiles.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblStatus2SearchFiles_Upload", "Upload the list of files");
          this.lblStatus2SearchFiles.Visibility = Visibility.Visible;
          this.lblStatus2SearchFilesResult.Visibility = Visibility.Hidden;

          StoryboardStatus1.Begin(this, true);

          break;
        #endregion UpdateListMultimediaStartUpload

        #region UpdateListMultimediaStop
        case enumLiveMultimedia.UpdateListMultimediaStop:
          this.cmdAddMultimediaFile.IsEnabled = true;
          this.cmdAddMultimediaFolder.IsEnabled = true;
          this.cmdSelectLanguage.IsEnabled = true;

          this.lblStatus2SearchFiles.Visibility = Visibility.Hidden;
          this.lblStatus2SearchFilesResult.Visibility = Visibility.Hidden;

          StoryboardStatus1.Stop(this);

          break;
        #endregion UpdateListMultimediaStop

        #region RemoveListMultimediaStart
        case enumLiveMultimedia.RemoveListMultimediaStart:
          this.cmdAddMultimediaFile.IsEnabled = false;
          this.cmdAddMultimediaFolder.IsEnabled = false;
          this.cmdSelectLanguage.IsEnabled = false;

          //this.pbMultimedia.Visibility = Visibility.Visible;
          //this.pbMultimedia.IsIndeterminate = true;

          this.lblStatus2SearchFiles.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblStatus2SearchFiles_Remove", "Removing files from the list");
          this.lblStatus2SearchFiles.Visibility = Visibility.Visible;

          StoryboardStatus1.Begin(this, true);

          break;
        #endregion RemoveListMultimediaStart
      }
    }

    #region NotifyIconApplication
    private async void NotifyMenuItemStart_Click(object Sender, EventArgs e)
    {
      await ApplicationStartStopAsync();
    }

    private void NotifyMenuItemOpen_Click(object Sender, EventArgs e)
    {
      ApplicationShow();
    }

    private void NotifyMenuItemViewEvents_Click(object Sender, EventArgs e)
    {
      NotifyIconApplication.ShowBalloonTip(20000, "Information", "This is the text", ToolTipIcon.Info);
    }

    private void NotifyMenuItemAbout_Click(object Sender, EventArgs e)
    {
      ApplicationAbout();
    }

    private async void NotifyMenuItemExit_Click(object Sender, EventArgs e)
    {
      await ApplicationShutdown();
    }

    private void NotifyIconApplication_DoubleClick(object Sender, EventArgs e)
    {
      ApplicationShow();
    }
    #endregion NotifyIconApplication

    private string GetFullPathByMultimediafileGUID(string MultimediaFileGUID)
    {
      string FullPath;

      try
      {
        MultimediaFile MultimediaFileFound = ListMultimediaFile.Single(MultimediaFile => MultimediaFile.MultimediaFileGUID == MultimediaFileGUID);
        FullPath = MultimediaFileFound.FullPath;
      }
      catch (Exception)
      {
        FullPath = "";
      }
      return FullPath;
    }

    private async Task<string> LoginToLiveMultimediaSystemAsync()
    {
      string Username=""; string Password;
      UserToken = "";

      #region Check Username and Password
      Username = tbUsername.Text.Trim(); Password = pbPassword.Password;
      if (!CheckGoodString(Username))
      {
        ShowErrorMessage(ErrorUsernameEmpty);
        this.tbUsername.Focus();
        return "";
      }

      if (!CheckGoodString(Password))
      {
        ShowErrorMessage(ErrorPasswordEmpty);
        this.pbPassword.Focus();
        return "";
      }
      #endregion #region Check Username and Password

      #region Check Proxy
      if (this.chkUseProxy.IsChecked == true)
      {
        if (!CheckGoodString(this.tbProxyAddress.Text))
        {
          ShowErrorMessage(ErrorProxyEmpty);
          this.tbProxyAddress.Focus();
          return "";
        }

        if (!CheckGoodString(this.tbProxyPort.Text))
        {
          ShowErrorMessage(ErrorProxyPortEmpty);
          this.tbProxyPort.Focus();
          this.tbProxyPort.Text = "80";
          return "";
        }

        string ProxyAddress = this.tbProxyAddress.Text.Trim();
        if (ProxyAddress.Substring(ProxyAddress.Length - 1, 1) == @"/")
        {
          ProxyAddress = ProxyAddress.Substring(0, ProxyAddress.Length - 1);
        }

        int ProxyPort;
        try
        {
          ProxyPort = Convert.ToInt32(this.tbProxyPort.Text);
        }
        catch (Exception)
        {
          ShowErrorMessage(ErrorProxyPort);
          this.tbProxyPort.Focus();
          this.tbProxyPort.Text = "80";
          return "";
        }

        Uri Proxy;
        try
        {
          UriBuilder ProxyUri = new UriBuilder("http", ProxyAddress, ProxyPort);
          ProxyUri.UserName = "";
          ProxyUri.Password = "";
          Proxy = ProxyUri.Uri;
        }
        catch (Exception)
        {
          ShowErrorMessage(ErrorProxyAddress + "\n\r" + ErrorProxyFormat + ":" + "\n\r" + "http://proxy.com" + "\r\n" + "http://127.0.0.1");
          this.tbProxyAddress.Focus();
          return "";
        }
        LiveConnection.Binding.ProxyAddress = Proxy;
        LiveConnection.Binding.UseDefaultWebProxy = false;
        LiveConnection.Binding.BypassProxyOnLocal = true;
      }
      else
      {
        LiveConnection.Binding.ProxyAddress = null;
      }
      #endregion Check Proxy

      #region Log in Live Multimedia Market
      try
      {
        using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnValue = await LiveMultimediaService.LocalLoginAsync(AccountKey, Username, Password);
          if (!CheckGoodString(returnValue.Item2))
          {
            UserToken = returnValue.Item1;
          }
          else
          {
            UserToken = "";
          }
        }

        if (UserToken=="")
        {
          SetStatusConnectToLiveMultimedia(enumLiveMultimedia.Error);
          ShowErrorMessage(ErrorLogon + " " + TitleLiveMultimediaServer);
        }
      }
      catch (Exception ex)
      {
        UserToken = "";
        ShowErrorMessage(ex, "LoginToLiveMultimediaSystemAsync");
      }
      #endregion Log in Live Multimedia Market

      return Username;
    }

    private async Task<bool> LoadSettingsAsync()
    {
      #region Define vars
      bool IsSuccess;
      string[] ListSettings;
      #endregion Define vars

      #region Try
      try
      {
        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          ListSettings = await LiveMultimediaService.LocalGetSettingsAsync(AccountKey, UserToken);
        }

        MultimediaFileBufferLength = Convert.ToInt32(ListSettings[0]);        
        DelayRequestMilliseconds = TimeSpan.FromMilliseconds((Convert.ToDouble(ListSettings[1])));
        DelayMaxSeconds = TimeSpan.FromSeconds((Convert.ToDouble(ListSettings[2])));
        DelayErrorSeconds = TimeSpan.FromSeconds((Convert.ToDouble(ListSettings[3])));
        IdleTimeSeconds = Convert.ToInt32(ListSettings[4]);
        MaxConnection = Convert.ToInt32(ListSettings[5]);

        if (MultimediaFileBufferLength > 0 && IdleTimeSeconds > 0)
        {
          var servicepointmanagerLiveMultimediaService = ServicePointManager.FindServicePoint(LiveConnection.EndPoint.Uri);
          servicepointmanagerLiveMultimediaService.ConnectionLimit = MaxConnection;

          IsSuccess = true;
        }
        else
        {
          IsSuccess = false;
        }          
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        IsSuccess = false;
        UserToken = "";
        ShowErrorMessage(ex, "LoadSettingsAsync");
      }
      #endregion Catch

      return IsSuccess;
    }

    private async Task<bool> LoadMultimediaFromServerAsync(CancellationToken ct)
    {
      bool IsSuccess;
      MultimediaFile[] aListMultimediaFile;

      try
      {
        if (ct.IsCancellationRequested) return true;

        using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          LiveMultimediaService.Open();
          var returnValue = await LiveMultimediaService.LocalGetListMultimediaFilesAsync(AccountKey, UserToken);
          if (CheckGoodString(returnValue.Item2)) throw new ArgumentException(returnValue.Item2);
          aListMultimediaFile = returnValue.Item1;
        }

        if (ct.IsCancellationRequested) return true;

        if (aListMultimediaFile != null)
        {
          MultimediaFile mf;
          for (int i = 0; i < aListMultimediaFile.Length; i++)
          {
            mf = new MultimediaFile();
            mf.FullPath = aListMultimediaFile[i].FullPath;
            mf.MultimediaFileGUID = aListMultimediaFile[i].MultimediaFileGUID;
            ListMultimediaFile.Add(mf);
          }
          this.lbMultimedia.ItemsSource = ListMultimediaFile;
          IsSuccess = true;
        }
        else
          IsSuccess = false;
      }
      catch (Exception ex)
      {
        IsSuccess = false;
        ShowErrorMessage(ex, "LoadMultimediaFromServerAsync");
      }

      return IsSuccess;
    }

    //private async Task<bool> LoadMultimediaFromServerAsync(CancellationToken ct)
    //{
    //  MultimediaFile[] aListMultimedia;
    //  using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
    //  {
    //    LiveMultimediaService.Open();
    //    aListMultimedia = await LiveMultimediaService.LocalGetListMultimediaFilesAsync(UserToken);

    //    //На будущее при длительной загрузке. Пример ниже.
    //    //Task<MultimediaFile[]> t = LiveMultimediaService.LocalGetListMultimediaFilesAsync(UserToken);
    //    //t.Start();
    //    //t.Wait(ct);
    //    //aListMultimedia = await t;
    //  }
    //  if (!ct.IsCancellationRequested)
    //  {
    //    ListMultimedia.Clear();
    //    this.lbMultimedia.Items.Clear();
    //    Parallel.ForEach(aListMultimedia, MultimediaFileItem =>
    //    {
    //      ListMultimedia.Add(MultimediaFileItem);
    //      this.lbMultimedia.Dispatcher.BeginInvoke(() => this.lbMultimedia.Items.Add(MultimediaFileItem.FullPath));
    //    }
    //    );
    //  }

    //  // Пример прерывания по token
    //  //CancellationTokenSource cts = new CancellationTokenSource();
    //  //CancellationToken token = cts.Token;

    //  //await Task.Run(() =>
    //  //{
    //  //  cts.Cancel();
    //  //  if (token.IsCancellationRequested)
    //  //    Console.WriteLine("Cancellation requested in Task {0}.",
    //  //                      Task.CurrentId);
    //  //}, token);

    //  //Task t2 = Task.Run(() =>
    //  //{
    //  //  for (int ctr = 0; ctr <= Int32.MaxValue; ctr++)
    //  //  { }
    //  //  Console.WriteLine("Task {0} finished.",
    //  //                    Task.CurrentId);
    //  //});

    //  //try
    //  //{
    //  //  t2.Wait(token);
    //  //}
    //  //catch (OperationCanceledException)
    //  //{
    //  //  Console.WriteLine("OperationCanceledException in Task {0}: The operation was cancelled.",
    //  //                    t2.Id);
    //  //}

    //  return true;
    //}

    private async Task ExitFromTasks()
    {
      if (tokenSource != null)
        tokenSource.Cancel();

      //if (tokenSource != null)
      //{
      //  try
      //  {
      //    tokenSource.Cancel();
      //  }
      //  finally
      //  {
      //    tokenSource.Dispose();
      //    tokenSource = null;
      //  }
      //}

      await CancelSendAllJobsAsync();
    }

    private async Task ApplicationStartStopAsync()
    {
      string Username="";

      NotifyIconApplication.Text = TitleLiveMultimediaServer;
      Title = TitleLiveMultimediaServer;

      if (!(bool)cmdConnectDisconnect.Tag) //Status: Disconnected. Action: Connect
      {
        SetStatusConnectToLiveMultimedia(enumLiveMultimedia.LoadDataStart);
        Username = await LoginToLiveMultimediaSystemAsync();
        if (UserToken != "")
        {
          bool IsSuccess=await LoadSettingsAsync();
          if (IsSuccess)
          {
            tokenSource = new CancellationTokenSource();
            cancellationToken = tokenSource.Token;
            try
            {
              IsSuccess = await LoadMultimediaFromServerAsync(cancellationToken);
              if (IsSuccess)
              {
                var notifyIconTitle = TitleLiveMultimediaServer + ". " + Username;
                if (notifyIconTitle.Length > 63)
                  notifyIconTitle = notifyIconTitle.Substring(0, 63);
                NotifyIconApplication.Text = notifyIconTitle;
                Title = TitleLiveMultimediaServer + ". " + Username;

                SetStatusConnectToLiveMultimedia(enumLiveMultimedia.Connected);
                await CheckRequestMultimediaFileBufferAsync(cancellationToken);
              }
            }
            catch (OperationCanceledException)
            {
              SetStatusConnectToLiveMultimedia(enumLiveMultimedia.Disconnected);
            }
            catch (Exception ex)
            {
              ShowErrorMessage(ex, "ApplicationStartStopAsync");
            }
          }
        }
      }
      else //Status: Connected. Action: Disconnect
      {
        SetStatusConnectToLiveMultimedia(enumLiveMultimedia.Disconnected);
        await ExitFromTasks();
        await ApplicationLogoutAsync();        
      }
    }

    private async Task CheckRequestMultimediaFileBufferAsync(CancellationToken ct)
    {
      #region Define vars
      string ErrorMessage;
      string[] ListRequest = null;
      Stopwatch CountTime = new Stopwatch();
      #endregion Define vars

      bool isRequestError = false; bool isRequestErrorPrevious = false;
      
      while (!ct.IsCancellationRequested)
      {
        Exception exceptionRequest = null;

        #region Get request from server

        #region Try
        try
        {
          #region Get Multimedia Request
          CountTime.Restart();
          using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
          {
            if (ct.IsCancellationRequested) break;
            var returnValue = await LiveMultimediaService.LocalGetMultimediaFileGUIDAsync(AccountKey, UserToken);
            ListRequest = returnValue.Item1;
            ErrorMessage = returnValue.Item2;
          }
          CountTime.Stop();
          await WriteDebugAsync("Check Request", "", 0, false, "", "Delta Check=" + CountTime.ElapsedMilliseconds.ToString());

          if (CheckGoodString(ErrorMessage)) throw new ApplicationException(ErrorMessage);

          #endregion Get Multimedia Request

          if (ct.IsCancellationRequested) break;

          #region Parse Multimedia Request
          if (ListRequest != null)
          {
            if (ListRequest.Length > 0)
            {
              string MultimediaFileGUID;
              long Range1, Range2;

              await WriteDebugAsync("Task.Run Start", "", 0, false, "", "ListRequest.Length=" + ListRequest.Length.ToString());

              if (ct.IsCancellationRequested) break;

              CountTime.Restart();
              await Task.Run(() =>
              {
                Parallel.For(0, ListRequest.Length, async i =>
                {
                  try
                  {
                    string[] aGuidMessage = ListRequest[i].Split(new Char[] { '|' });
                    var ActionMultimedia = Convert.ToInt32(aGuidMessage[0]);
                    var IdJob = aGuidMessage[1];

                    switch (ActionMultimedia)
                    {
                      case 1: // StartTransferAttributes
                        MultimediaFileGUID = aGuidMessage[2];

                        await PrepareAttributes(IdJob, MultimediaFileGUID, ct);

                        break;
                      case 2: //StartTransfer
                        MultimediaFileGUID = aGuidMessage[2];
                        Range1 = Convert.ToInt64(aGuidMessage[3]);
                        Range2 = Convert.ToInt64(aGuidMessage[4]);

                        await SendMultimediaFileBufferAsync(MultimediaFileGUID, IdJob, Range1, Range2, ct);

                        //await PrepareSend(MultimediaFileGUID, IdJob, Range1, Range2, ct);

                        break;
                      case 3:
                        await CancelSendJobAsync(IdJob);
                        break;
                    }
                  }
                  catch (Exception)
                  {
                  }
                }
                );
              }
              );
              CountTime.Stop();
              await WriteDebugAsync("Task.Run Stop", "", 0, false, "", "Task.Run Delta=" + CountTime.ElapsedMilliseconds.ToString());
            }
          }
          #endregion Parse Multimedia Request

          isRequestError = false;
        }
        #endregion Try

        #region Catch
        catch (Exception ex)
        {
          isRequestError = true;
          exceptionRequest = ex;
        }
        #endregion Catch

        #region Finally
        finally
        {
          if (CountTime.IsRunning) CountTime.Stop();
        }
        #endregion Finally

        #endregion Get request from server

        if (isRequestError)
        {
          await WriteDebugAsync("Check Request Error", "", 0, false, "", exceptionRequest.ToString());
        }

        #region Calculation DelayCurrent
        if (!isRequestError)
        {
          if (isRequestErrorPrevious)
          {
            isRequestErrorPrevious = false;
            ErrorRequest = "";
            SetStatusConnectToLiveMultimedia(enumLiveMultimedia.RequestOk);
          }
        }
        else
        {
          isRequestErrorPrevious = true;
          ErrorRequest = exceptionRequest.Message;
          SetStatusConnectToLiveMultimedia(enumLiveMultimedia.RequestError);
        }
        #endregion Calculation DelayCurrent
      }
    }

    //Реализовать позже
    //private async Task GetMultimediaFileAttributes(string MultimediaFileGUID, string IdJob, CancellationToken ct)
    //{
    //  var TaskId = (int)(Task.CurrentId ?? -1);

    //  try
    //  {
    //    #region Check incoming parameters
    //    if (!CheckGoodString(MultimediaFileGUID)) throw new ArgumentException("MultimediaFileGUID is empty", "MultimediaFileGUID");
    //    if (!CheckGoodString(IdJob)) throw new ArgumentException("IdJob is empty", "IdJob");
    //    if (ct==null) throw new ArgumentException("Cancelation ntoken is null", "ct");
    //    #endregion Check incoming parameters

    //    var FullPath = GetFullPathByMultimediafileGUID(MultimediaFileGUID);

    //    FileInfo MultimediaFileInfo = new FileInfo(FullPath);
    //    var MultimediaFileLength = MultimediaFileInfo.Length;

    //    await WriteDebugAsync("Transfer attributes Start", MultimediaFileGUID, 0, false, IdJob, TaskId, MultimediaFileInfo.Name);
    //    var CountTime = new Stopwatch();
    //    CountTime.Start();

    //    using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
    //    {
    //      //var returnValue = await LiveMultimediaService.LocalSetMultimediaFileBufferAsync(AccountKey, UserToken, MultimediaFileBuffer, ChunkCount, MultimediaFileLength, IsStopTransfer, IdJob);
    //      //if (CheckGoodString(returnValue)) throw new ArgumentException(returnValue);
    //    }

    //    CountTime.Stop();
    //    await WriteDebugAsync("Transfer attributes Stop", MultimediaFileGUID, 0, false, IdJob, TaskId, "Delta transfer attributes=" + CountTime.ElapsedMilliseconds.ToString());
    //  }
    //  catch (Exception ex)
    //  {
    //    WriteEventLog("GetMultimediaFileAttributes", ex);
    //  }
    //}

    private async Task PrepareAttributes(string IdJob, string MultimediaFileGUID, CancellationToken ct)
    {
      try
      {
        await WriteDebugAsync("PrepareAttributes Start", MultimediaFileGUID, 0, false, IdJob, 0, "");

        #region Check incoming parameters
        if (!CheckGoodString(MultimediaFileGUID)) throw new ArgumentException("MultimediaFileGUID is empty", "MultimediaFileGUID");
        if (!CheckGoodString(IdJob)) throw new ArgumentException("IdJob is empty", "IdJob");
        if (ct == null) throw new ArgumentException("Cancelation token is null", "ct");

        #endregion Check incoming parameters

        #region Parse Filename
        var FullPath = GetFullPathByMultimediafileGUID(MultimediaFileGUID);
        var aFullPath = FullPath.Split(new Char[] { '\\' });
        var FileName = aFullPath[aFullPath.Length - 1];
        #endregion Parse Filename

        long MultimediaFileLength;
        using (var fs = new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8, FileOptions.Asynchronous))
        {
          MultimediaFileLength = fs.Length;
        }

        #region Check incoming parameters for MultimediaFileLength
        if (MultimediaFileLength <= 0) throw new ArgumentException("Multimedia File Length must be more zero", "MultimediaFileLength");
        #endregion Check incoming parameters for MultimediaFileLength

        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          int SpeedServer = 16000;
          var returnValue = await LiveMultimediaService.LocalSetMultimediaFileAttributesAsync(AccountKey, UserToken, MultimediaFileLength, SpeedServer, IdJob);
          if (CheckGoodString(returnValue)) throw new ArgumentException(returnValue);
        }

        await WriteDebugAsync("PrepareAttributes Stop", MultimediaFileGUID, 0, false, IdJob, 0, "");
      }
      catch (Exception ex)
      {
        WriteEventLog("PrepareAttributes", ex);
      }
    }

    private async Task PrepareSend(string MultimediaFileGUID, string IdJob, long Range1, long Range2, CancellationToken ct)
    {
      #region Define vars
      int ChunkCount;
      var CountTime = new Stopwatch();
      #endregion Define vars

      try
      {
        await WriteDebugAsync("PrepareSend Start", MultimediaFileGUID, 0, false, IdJob, 0, "Range1=" + Range1.ToString() + " Range2=" + Range2.ToString());

        #region Check incoming parameters
        if (!CheckGoodString(MultimediaFileGUID)) throw new ArgumentException("MultimediaFileGUID is empty", "MultimediaFileGUID");
        if (!CheckGoodString(IdJob)) throw new ArgumentException("IdJob is empty", "IdJob");
        if (ct == null) throw new ArgumentException("Cancelation token is null", "ct");

        #region Check position
        if (Range1 < 0) throw new ArgumentException("Range1 must be more or equal zero", "Range1");
        if (Range2 >= 0)
        {
          if (Range1 > Range2) throw new ArgumentException("Range1 must be less Range2", "Range1");
        }
        #endregion Check position

        #endregion Check incoming parameters

        #region Parse Filename
        var FullPath = GetFullPathByMultimediafileGUID(MultimediaFileGUID);
        var aFullPath = FullPath.Split(new Char[] { '\\' });
        var FileName = aFullPath[aFullPath.Length - 1];
        #endregion Parse Filename

        long MultimediaFileLength;
        using (var fs = new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8, FileOptions.Asynchronous))
        {
          MultimediaFileLength = fs.Length;
        }

        if (Range2 < 0) Range2 = MultimediaFileLength - 1;

        #region Check incoming parameters for MultimediaFileLength
        if (MultimediaFileLength <= 0) throw new ArgumentException("Multimedia File Length must be more zero", "MultimediaFileLength");
        if (Range1 >= MultimediaFileLength) throw new ArgumentException("Range1 must be less Multimedia File Length", "Range1");
        if (Range2 >= MultimediaFileLength) throw new ArgumentException("Range2 must be less Multimedia File Length", "Range2");
        if (Range1 > Range2) throw new ArgumentException("Range1 must be less then Range2", "Range1");
        #endregion Check incoming parameters for MultimediaFileLength

        var tokenSourceJob = new CancellationTokenSource();
        ListJob.Add(new Tuple<string, CancellationTokenSource>(IdJob, tokenSourceJob));

        #region Prepare buffers for transfer
        long RangeBuffer1, RangeBuffer2;        
        bool IsStopTransfer = false;

        #region Multiple serial buffers       
        //long currentBufferLength = 65536;
        long currentBufferLength = 4194304;
        int countSerialBuffers = 1;

        RangeBuffer1 = Range1; RangeBuffer2 = 0;
        for (ChunkCount = 1; ChunkCount <= countSerialBuffers; ChunkCount++)
        {
          RangeBuffer2 = RangeBuffer1 + currentBufferLength - 1;

          if (RangeBuffer2 >= Range2)
          {
            RangeBuffer2 = Range2;
            IsStopTransfer = true;
          }

          if (tokenSourceJob.Token.IsCancellationRequested) break;

          await SendMultimediaFileBufferAsync(MultimediaFileGUID, FullPath, FileName, ChunkCount, IdJob, RangeBuffer1, RangeBuffer2, IsStopTransfer, ct, tokenSourceJob);

          RangeBuffer1 = RangeBuffer2 + 1;

          if (IsStopTransfer) break;
        }
        #endregion Multiple serial buffers

        #region Parallel
        if (!IsStopTransfer && !tokenSourceJob.Token.IsCancellationRequested)
        {
          //MultimediaFileBufferLength = 65536;
          //MultimediaFileBufferLength = 131072;
          //MultimediaFileBufferLength = 524288;
          //MultimediaFileBufferLength = 1048576;
          MultimediaFileBufferLength = 2097152;
          //MultimediaFileBufferLength = 4194304;

          int minWorker, minIOC;
          ThreadPool.GetMinThreads(out minWorker, out minIOC);
          var maxCountParallel = 1;          
          //maxCountParallel = minIOC;
          //maxCountParallel = minIOC * 2;
          //maxCountParallel = minIOC/2;
          //maxCountParallel = 40;
          //maxCountParallel = 4;

          await WriteDebugAsync("minIOC", MultimediaFileGUID, 0, false, IdJob, 0, "minIOC=" + minIOC.ToString());

          long MaxChunkCount = ((Range2 - RangeBuffer1) / MultimediaFileBufferLength) + 1;
          long calcCountParallel;
          ConcurrentBag<Task> ListTask;
          for (long chunkCountParallel = 0; chunkCountParallel < MaxChunkCount; chunkCountParallel = chunkCountParallel + maxCountParallel)
          {
            ListTask = new ConcurrentBag<Task>();

            await WriteDebugAsync("Block send: Start", MultimediaFileGUID, chunkCountParallel, false, IdJob, 0, FileName); CountTime.Restart();

            if ((chunkCountParallel + maxCountParallel) < MaxChunkCount)
              calcCountParallel = maxCountParallel;
            else
              calcCountParallel = MaxChunkCount - chunkCountParallel;

            for (int i = 0; i < calcCountParallel; i++)
            {
              if (!tokenSourceJob.Token.IsCancellationRequested)
              {
                var RangeBufferParallel1 = RangeBuffer1 + (chunkCountParallel + i) * MultimediaFileBufferLength;
                var RangeBufferParallel2 = RangeBufferParallel1 + MultimediaFileBufferLength - 1;

                if (RangeBufferParallel2 >= Range2)
                {
                  RangeBufferParallel2 = Range2;
                  IsStopTransfer = true;
                }

                var newTask = Task.Run(async () =>
                //var newTask = Task.Factory.StartNew(async () =>
                {
                  await SendMultimediaFileBufferAsync(MultimediaFileGUID, FullPath, FileName, ChunkCount + chunkCountParallel + i, IdJob, RangeBufferParallel1, RangeBufferParallel2, IsStopTransfer, ct, tokenSourceJob);
                }
                );
                ListTask.Add(newTask);
              }
            }

            //Parallel.For(0, calcCountParallel, i =>
            //{
            //  if (!tokenSourceJob.Token.IsCancellationRequested)
            //  {
            //    var RangeBufferParallel1 = RangeBuffer1 + (chunkCountParallel + i);
            //    var RangeBufferParallel2 = RangeBufferParallel1 + MultimediaFileBufferLength - 1;

            //    if (RangeBufferParallel2 >= Range2)
            //    {
            //      RangeBufferParallel2 = Range2;
            //      IsStopTransfer = true;
            //    }

            //    var newTask = Task.Run(async () =>
            //    //var newTask = Task.Factory.StartNew(async () =>
            //    {
            //      await SendMultimediaFileBufferAsync(MultimediaFileGUID, FullPath, FileName, ChunkCount + chunkCountParallel + i, IdJob, RangeBufferParallel1, RangeBufferParallel2, IsStopTransfer, ct, tokenSourceJob);
            //    }
            //    );
            //    ListTask.Add(newTask);
            //  }
            //}
            //);

            Task.WaitAll(ListTask.ToArray());

            CountTime.Stop(); await WriteDebugAsync("Block send: Stop", MultimediaFileGUID, chunkCountParallel, false, IdJob, 0, "Delta Send block file=" + CountTime.ElapsedMilliseconds.ToString());

            //Debug.WriteLine("Block send: Stop {0}", CountTime.ElapsedMilliseconds);
          }
        }
        #endregion Parallel

        #region Последовательные буферы
        ////Последовательные буферы
        //RangeBuffer1 = Range1;
        //while (RangeBuffer1 <= Range2)
        //{
        //  if (ChunkCount <= 1)
        //    currentBufferLength = 65536;
        //  else
        //    currentBufferLength = MultimediaFileBufferLength;

        //  RangeBuffer2 = RangeBuffer1 + currentBufferLength - 1;

        //  if (RangeBuffer2 >= Range2)
        //  {
        //    RangeBuffer2 = Range2;
        //    IsStopTransfer = true;
        //  }

        //  //Debug.WriteLine("New Range: ChunkCount={2} RangeBuffer1={0} RangeBuffer2={1}", RangeBuffer1, RangeBuffer2, ChunkCount);

        //  if (tokenSourceJob.Token.IsCancellationRequested) break;

        //  await Task.Run(async () =>
        //  {
        //    await SendMultimediaFileBufferAsync(MultimediaFileGUID, FullPath, FileName, ChunkCount, IdJob, RangeBuffer1, RangeBuffer2, IsStopTransfer, ct, tokenSourceJob);
        //  });

        //  ChunkCount++;
        //  RangeBuffer1 = RangeBuffer2 + 1;
        //}
        #endregion Последовательные буферы

        #endregion Prepare buffers for transfer

        var foundJob = ListJob.FirstOrDefault(job => job.Item1 == IdJob);
        if (foundJob != null)
        {
          var IsSuccess = ListJob.TryTake(out foundJob);
          if (!IsSuccess) throw new ArgumentException("TryTake job is not Success");
        }

        await WriteDebugAsync("PrepareSend Stop", MultimediaFileGUID, 0, false, IdJob, 0, "Range1=" + Range1.ToString() + " Range2=" + Range2.ToString() + " ListJob.Count=" + ListJob.Count().ToString());
      }
      catch (Exception ex)
      {
        WriteEventLog("PrepareSend", ex);
      }
    }

    private async Task CancelSendJobAsync(string IdJob)
    {
      try
      {
        await WriteDebugAsync("CancelSendJobAsync Start", "", 0, true, IdJob, 0, "");

        var foundJob = ListJob.First(job => job.Item1 == IdJob);
        if (foundJob != null)
        {
          foundJob.Item2.Cancel();
          var IsSuccess = ListJob.TryTake(out foundJob);

          if (!IsSuccess) throw new ArgumentException("CancelSendAsync: IdJob not found: " + IdJob);
        }
        else
        {
        }

        await WriteDebugAsync("CancelSendJobAsync Stop", "", 0, true, IdJob, 0, "");
      }
      catch (Exception ex)
      {
        WriteEventLog("CancelSendAsync", ex);
      }
    }

    private async Task CancelSendAllJobsAsync()
    {
      try
      {
        await WriteDebugAsync("CancelSendAllJobsAsync Start", "", 0, true, "", 0, "ListJob.Count=" + ListJob.Count().ToString());

        foreach (var itemJob in ListJob)
        {
          itemJob.Item2.Cancel();
        }

        await WriteDebugAsync("CancelSendAllJobsAsync Stop", "", 0, true, "", 0, "");
      }
      catch (Exception ex)
      {
        WriteEventLog("CancelSendAllJobsAsync", ex);
      }
    }

    // Для PrepareSend
    private async Task SendMultimediaFileBufferAsync(string MultimediaFileGUID, string FullPath, string FileName, long ChunkCount, string IdJob, long Range1, long Range2, bool IsStopTransfer, CancellationToken ct, CancellationTokenSource tokenSourceJob)
    {
      #region Define vars
      byte[] MultimediaFileBuffer = null;
      var CountTime = new Stopwatch();
      int TaskId = (int)(Task.CurrentId ?? -1);
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        if (ct == null) throw new ArgumentException("CancellationToken is empty", "CancellationToken");
        if (tokenSourceJob == null) throw new ArgumentException("Token Job is empty", "tokenJob");
        #endregion Check incoming parameters

        if (ct.IsCancellationRequested || tokenSourceJob.IsCancellationRequested)
        {
          await WriteDebugAsync("Send: cancel token", MultimediaFileGUID, ChunkCount, true, IdJob, 0, "");
          return;
        }

        await WriteDebugAsync("Send: read file Start", MultimediaFileGUID, ChunkCount, IsStopTransfer, IdJob, TaskId, FileName);
        CountTime.Restart();

        #region Reading file
        using (var fs = new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8, FileOptions.Asynchronous))
        {
          fs.Seek(Range1, SeekOrigin.Begin);
          MultimediaFileBuffer = new byte[Range2 - Range1+1];
          await fs.ReadAsync(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length);
        }
        #endregion Reading file

        CountTime.Stop();
        await WriteDebugAsync("Send: read file Stop", MultimediaFileGUID, ChunkCount, IsStopTransfer, IdJob, TaskId, "Delta Read file=" + CountTime.ElapsedMilliseconds.ToString());

        if (ct.IsCancellationRequested || tokenSourceJob.IsCancellationRequested)
        {
          await WriteDebugAsync("Send: cancel token", MultimediaFileGUID, ChunkCount, true, IdJob, 0, "");
          return;
        }

        await WriteDebugAsync("Set buffer Start", MultimediaFileGUID, ChunkCount, IsStopTransfer, IdJob, TaskId, FileName);
        CountTime.Restart();

        #region Send buffer to server
        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnValue = await LiveMultimediaService.LocalSetMultimediaFileBufferAsync(AccountKey, UserToken, MultimediaFileBuffer, IsStopTransfer, IdJob);
          if (CheckGoodString(returnValue)) throw new ArgumentException(returnValue);
        }
        #endregion Send buffer to server

        CountTime.Stop();
        await WriteDebugAsync("Set buffer Stop", MultimediaFileGUID, ChunkCount, IsStopTransfer, IdJob, TaskId, "Delta Set Buffer=" + CountTime.ElapsedMilliseconds.ToString());
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        tokenSourceJob.Cancel();

        WriteDebug("SendMultimediaFileBufferAsync", MultimediaFileGUID, ChunkCount, true, IdJob, TaskId, ex.Message + ". FullPath=" + (FullPath ?? ""));
        WriteEventLog("SendMultimediaFileBufferAsync", ex);
      }
      catch (Exception ex)
      {
        tokenSourceJob.Cancel();

        WriteDebug("SendMultimediaFileBufferAsync", MultimediaFileGUID, ChunkCount, true, IdJob, TaskId, ex.ToString() + ". FullPath=" + (FullPath ?? ""));
        WriteEventLog("SendMultimediaFileBufferAsync", ex);
      }
      #endregion Catch

      #region Finally
      finally
      {
        if (CountTime.IsRunning) CountTime.Stop();
      }
      #endregion Finally
    }

    private async Task SendMultimediaFileBufferAsync(string MultimediaFileGUID, string IdJob, long Range1, long Range2, CancellationToken ct)
    {
      #region Define vars
      string FullPath="";
      bool IsStopTransfer = true;
      byte[] MultimediaFileBuffer = null;
      long MultimediaFileLength = 0;
      var CountTime = new Stopwatch();
      int TaskId = (int)(Task.CurrentId ?? -1);
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        if (ct == null) throw new ArgumentException("CancellationToken is empty", "CancellationToken");
        #endregion Check incoming parameters

        if (ct.IsCancellationRequested) return;

        #region Parse Filename
        FullPath = GetFullPathByMultimediafileGUID(MultimediaFileGUID);
        var aFullPath = FullPath.Split(new Char[] { '\\' });
        var FileName = aFullPath[aFullPath.Length - 1];
        #endregion Parse Filename

        await WriteDebugAsync("Send: read file Start", MultimediaFileGUID, 0, false, IdJob, TaskId, FileName);
        CountTime.Restart();

        #region Reading file
        using (var fs = new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8, FileOptions.Asynchronous))
        {
          MultimediaFileLength = fs.Length;
         
          if (Range1 <= Range2)
          {
            fs.Seek(Range1, SeekOrigin.Begin);

            MultimediaFileBufferLength = (int)(Range2 - Range1 + 1);

            #region Read chunk from file
            if ((Range1 + MultimediaFileBufferLength) <= MultimediaFileLength)
            {
              MultimediaFileBuffer = new byte[MultimediaFileBufferLength];
              await fs.ReadAsync(MultimediaFileBuffer, 0, MultimediaFileBufferLength);

              IsStopTransfer = false;
            }
            else
            {
              long remainingBytes = MultimediaFileLength - Range1;
              MultimediaFileBuffer = new byte[remainingBytes];
              await fs.ReadAsync(MultimediaFileBuffer, 0, (int)remainingBytes);

              IsStopTransfer = true;
            }
            #endregion Read chunk from file
          }
          else
          {
            MultimediaFileBuffer = new byte[0];
            MultimediaFileLength = -1;
            IsStopTransfer = true;
          }
        }

        #endregion Reading file

        CountTime.Stop();
        await WriteDebugAsync("Send: read file Stop", MultimediaFileGUID, 0, false, IdJob, TaskId, "Delta Read file=" + CountTime.ElapsedMilliseconds.ToString());

        if (ct.IsCancellationRequested) return;

        await WriteDebugAsync("Set buffer Start", MultimediaFileGUID, 0, IsStopTransfer, IdJob, TaskId, FileName);
        CountTime.Restart();

        #region Send multimedia file buffer

        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnValue = await LiveMultimediaService.LocalSetMultimediaFileBufferAsync(AccountKey, UserToken, MultimediaFileBuffer, IsStopTransfer, IdJob);
          if (CheckGoodString(returnValue)) throw new ArgumentException(returnValue);
        }
        #endregion Send multimedia file buffer

        CountTime.Stop();
        await WriteDebugAsync("Set buffer Stop", MultimediaFileGUID, 0, IsStopTransfer, IdJob, TaskId, "Delta Set Buffer=" + CountTime.ElapsedMilliseconds.ToString());
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        WriteDebug("SendMultimediaFileBufferAsync", MultimediaFileGUID, 0, true, IdJob, TaskId, ex.Message + ". FullPath=" + (FullPath ?? ""));
        WriteEventLog("SendMultimediaFileBufferAsync", ex);
      }
      catch (Exception ex)
      {
        WriteDebug("SendMultimediaFileBufferAsync", MultimediaFileGUID, 0, true, IdJob, TaskId, ex.ToString() + ". FullPath=" + (FullPath ?? ""));
        WriteEventLog("SendMultimediaFileBufferAsync", ex);
      }
      #endregion Catch

      #region Finally
      finally
      {
        if (CountTime.IsRunning) CountTime.Stop();
      }
      #endregion Finally
    }

    class MultimediaFileComparer : IEqualityComparer<MultimediaFile>
    {      
      public bool Equals(MultimediaFile x, MultimediaFile y)
      {
        //Check whether the compared objects reference the same data.
        if (Object.ReferenceEquals(x, y)) return true;

        //Check whether any of the compared objects is null.
        if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null)) return false;

        //Check whether the products' properties are equal.
        return x.FullPath == y.FullPath;
      }

      // If Equals() returns true for a pair of objects 
      // then GetHashCode() must return the same value for these objects.
      public int GetHashCode(MultimediaFile multimediaFile)
      {
        //Check whether the object is null
        if (Object.ReferenceEquals(multimediaFile, null)) return 0;

        //Get hash code for the Name field if it is not null.
        int hashProductName = multimediaFile.FullPath == null ? 0 : multimediaFile.FullPath.GetHashCode();

        //Calculate the hash code for the product.
        return hashProductName;
      }
    }

    private IEnumerable<MultimediaFile> RemoveDuplicates()
    {
      IEnumerable<MultimediaFile> queryExcept;

      try
      {
        queryExcept = ListMultimediaFileBag.Except(ListMultimediaFile, new MultimediaFileComparer());
      }
      catch (Exception)
      {
        queryExcept = null;
      }

      return queryExcept;
    }

//    private void RemoveDuplicates()
//    {
//      bool IsSuccess; MultimediaFile mf_take;

//      //ListMultimediaFileBag.Except()

//      if (ListMultimediaFile.Count() > 0 && ListMultimediaFileBag != null)
//      {
//        if (!ListMultimediaFileBag.IsEmpty)
//        {
//          foreach (var MultimediaFileInList in ListMultimediaFile)
//          {
//            try
//            {
//              if (!ListMultimediaFileBag.IsEmpty)
//              {
//                mf_take = ListMultimediaFileBag.FirstOrDefault(MultimediaFileInBag => MultimediaFileInBag.FullPath == MultimediaFileInList.FullPath);
//                if (mf_take != null)
//                {
//                  IsSuccess = ListMultimediaFileBag.TryTake(out mf_take);
//                  if (!IsSuccess) new System.Exception("Error 'try take' MultimediaFile from collection: " + MultimediaFileInList.FullPath);
//                }
//              }
//              else break;
//            }
//            catch (Exception ex)
//            {
//#if (DEBUG)
//              ErrorMessage(ex, "RemoveDuplicates in DEBUG Mode");
//#endif
//            }
            
//          }
//        }
//      }
//    }

//    private void RemoveDuplicates()
//    {
//      bool IsSuccess; MultimediaFile mf_take;

//      //ListMultimediaFileBag.Except()

//      if (ListMultimediaFile.Count() > 0 && ListMultimediaFileBag != null)
//      {
//        if (!ListMultimediaFileBag.IsEmpty)
//        {
//          Parallel.ForEach(ListMultimediaFile, MultimediaFileInList =>
//          {
//            try
//            {
//              if (!ListMultimediaFileBag.IsEmpty)
//              {
//                mf_take = ListMultimediaFileBag.FirstOrDefault(MultimediaFileInBag => MultimediaFileInBag.FullPath == MultimediaFileInList.FullPath);
//                if (mf_take != null)
//                {
//                  IsSuccess = ListMultimediaFileBag.TryTake(out mf_take);
//                  if (!IsSuccess) new System.Exception("Error 'try take' MultimediaFile from collection: " + MultimediaFileInList.FullPath);
//                }
//              }
//            }
//            catch (Exception ex)
//            {
//#if (DEBUG)
//              ErrorMessage(ex, "RemoveDuplicates in DEBUG Mode");
//#endif
//            }
//          }
//          );
//        }
//      }
//    }

    private async Task AddListMultimediaToServerAsync()
    {
      string[] aListGuids = null;

      try
      {
        var ListExceptMultimediaFile=RemoveDuplicates();

        if (ListExceptMultimediaFile != null)
        {
          if (ListExceptMultimediaFile.Count()>0)
          {
            SetStatusConnectToLiveMultimedia(enumLiveMultimedia.UpdateListMultimediaStartUpload);
            using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
            {
              MultimediaFile[] aListMultimediaFile = ListExceptMultimediaFile.ToArray();
              var returnValue = await LiveMultimediaService.LocalListMultimediaFilesAddAsync(AccountKey, UserToken, aListMultimediaFile);
              if (CheckGoodString(returnValue.Item2)) throw new ArgumentException(returnValue.Item2);
              aListGuids = returnValue.Item1;
              for (int i = 0; i < aListGuids.Length; i++)
              {
                aListMultimediaFile[i].MultimediaFileGUID = aListGuids[i];
                ListMultimediaFile.Add(aListMultimediaFile[i]);
              }
            }
          }
        }
        SetStatusConnectToLiveMultimedia(enumLiveMultimedia.UpdateListMultimediaStop);
      }
      catch (Exception ex)
      {
        ShowErrorMessage(ex, "AddListMultimediaToServerAsync");
      }
    }

    private async Task RemoveListMultimediaToServerAsync()
    {
      try
      {
        SetStatusConnectToLiveMultimedia(enumLiveMultimedia.RemoveListMultimediaStart);
        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          LiveMultimediaService.Open();
          await LiveMultimediaService.LocalListMultimediaFilesRemoveAsync(AccountKey, UserToken, ListMultimediaFileBag.ToArray());

          foreach (var SelectedMultimediaFile in ListMultimediaFileBag)
          {
            try
            {
              ListMultimediaFile.Remove(SelectedMultimediaFile);
            }
            catch (Exception ex)
            {
#if (DEBUG)
              ShowErrorMessage(ex, "RemoveListMultimediaToServerAsync in DEBUG Mode");
#endif
            }
          }
        }
        SetStatusConnectToLiveMultimedia(enumLiveMultimedia.UpdateListMultimediaStop);
      }
      catch (Exception ex)
      {
        ShowErrorMessage(ex, "RemoveListMultimediaToServerAsync");
      }
    }

    private async void cmdAddMultimediaFile_Click(object sender, RoutedEventArgs e)
    {
      string CurrentExtension;
      MultimediaFile NewMultimediaFile;
      try
      {
        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
        dlg.Title = SelectFileTitle;
        dlg.FileName = "";
        dlg.Filter = MultimediaFileExtension;
        dlg.DefaultExt = "";
        dlg.CheckFileExists = true;
        dlg.Multiselect = true;

        Nullable<bool> result = dlg.ShowDialog();

        if (result==true)
        {
          SetStatusConnectToLiveMultimedia(enumLiveMultimedia.UpdateListMultimediaStart);
          ListMultimediaFileBag = new ConcurrentBag<MultimediaFile>();

          foreach (var FullPath in dlg.FileNames)
          {
            #region This block because may be bad extensions
            var aPartName = FullPath.Split(new Char[] { '.' });
            if (aPartName.Length > 0)
              CurrentExtension = aPartName[aPartName.Length - 1];
            else
              CurrentExtension = "";
            #endregion This block because may be bad extensions

            var FindExtension=ListMultimediaExtension.SingleOrDefault(tupleItem => tupleItem.Item2.Contains(CurrentExtension));
            if (FindExtension != null)
            {
              NewMultimediaFile = new MultimediaFile();
              NewMultimediaFile.FullPath = FullPath;
              ListMultimediaFileBag.Add(NewMultimediaFile);
            }
          }
          await AddListMultimediaToServerAsync();
        }
      }
      catch (Exception ex)
      {
        ListMultimediaFileBag = null;
        ShowErrorMessage(ex, "cmdAddMultimediaFile_Click");
      }

    }    

//    private async void cmdAddMultimediaFolder_Click(object sender, RoutedEventArgs e)
//    {
//      #region Побаловаться на "потом"
//      //List<MultimediaFile> ListMultimediaFileUpdate;
//      //MultimediaFile mf;
//      //Func<string, MultimediaFile> ConvertFullPathToMultimediaFile = FullPath =>
//      //  {
//      //    mf = new MultimediaFile();
//      //    mf.FullPath = FullPath;
//      //    return mf;
//      //  };
//      #endregion Побаловаться на "потом"

//      try
//      {
//        FolderBrowserDialog SelectedMultimediaPath = new FolderBrowserDialog();
//        SelectedMultimediaPath.ShowNewFolderButton = true;
//        DialogResult dr = SelectedMultimediaPath.ShowDialog();

//        //DialogResult dr = new DialogResult();
//        //dr = System.Windows.Forms.DialogResult.OK;

//        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia\\music\\Аудиокниги\\S Tzu - Art of War";
//        //SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia\\music\\Аудиокниги\\Аэлита";
//        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia\\music\\Аудиокниги";
//        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia\\music";
//        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia";
//        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Volume_1";
//        if (dr == System.Windows.Forms.DialogResult.OK)
//        {
//          ListMultimediaFileBag = new ConcurrentBag<MultimediaFile>();
//          usingResource = 0;

//          SetStatusConnectToLiveMultimedia((int)enumLiveMultimedia.UpdateListMultimediaStart);
//          await Task.Factory.StartNew(() => ReadFolder(SelectedMultimediaPath.SelectedPath, cancellationToken));          
          
//          long TotalCount = Interlocked.Read(ref usingResource);

//#if (DEBUG)
//          if (TotalCount != ListMultimediaFileBag.Count())
//            ErrorMessage("Error in cmdAddMultimediaFolder_Click: TotalCount and ListFullPathBag.Count not equal: TotalCount=" + TotalCount.ToString() + ", ListFullPathBag.Count=" + ListMultimediaFileBag.Count().ToString());
//#endif

//          if (cancellationToken.IsCancellationRequested)
//          {
//            ListMultimediaFileBag = null;
//            return;
//          }
//          #region Побаловаться на "потом"
//            //var q1 = ListFullPathBag.Select(FullPath => ConvertFullPathToMultimediaFile);
//            //foreach (MultimediaFile obj in q1)
//            //{
//            //  Console.WriteLine(
//            //      "{0}",
//            //      obj.FullPath);
//            //}

//            //ListMultimediaFileUpdate = (List<MultimediaFile>)ListFullPathBag.Select(FullPath => ConvertFullPathToMultimediaFile);
//            //bool IsSuccess = await AddListMultimediaToServerAsync(ListMultimediaFileUpdate);
//            #endregion Побаловаться на "потом"

//          await AddListMultimediaToServerAsync();
//        }
//      }
//      catch (Exception ex)
//      {
//        SetStatusConnectToLiveMultimedia((int)enumLiveMultimedia.UpdateListMultimediaStop);
//        ListMultimediaFileBag = null;
//        ErrorMessage(ex, "cmdAddMultimediaFolder_Click");
//      }
//    }

    private void cmdAddMultimediaFolder_Click(object sender, RoutedEventArgs e)
    {
      this.cmdAddMultimediaFolder.Visibility = Visibility.Collapsed;
      this.comboAddMultimediaFolder.Visibility = Visibility.Visible;
      this.comboAddMultimediaFolder.IsDropDownOpen = true;
      this.comboAddMultimediaFolder.SelectedIndex = 0;
    }

    private void comboAddMultimediaFolder_LostFocus(object sender, RoutedEventArgs e)
    {
      if (!this.comboAddMultimediaFolder.IsDropDownOpen)
      {
        this.cmdAddMultimediaFolder.Visibility = Visibility.Visible;
        this.comboAddMultimediaFolder.Visibility = Visibility.Collapsed;
      }
    }

    private async void buttonAddFolder_Click(object sender, RoutedEventArgs e)
    {
      #region Define vars
      #endregion Define vars

      this.cmdAddMultimediaFolder.Visibility = Visibility.Visible;
      this.comboAddMultimediaFolder.Visibility = Visibility.Collapsed;

      var buttonAddFolder = (System.Windows.Controls.Button)sender;
      var indexOfTypeMultimediaItem = this.comboAddMultimediaFolder.Items.IndexOf(buttonAddFolder);

      #region Fill list extensions
      ListMultimediaFileExtension = new List<string>();

      if (indexOfTypeMultimediaItem == 0)
      {
        foreach (var item in ListMultimediaExtension)
        {
          foreach (var item2 in item.Item2)
          {
            ListMultimediaFileExtension.Add(item2);
          }          
        }
      }
      else
      {
        indexOfTypeMultimediaItem--;
        foreach (var item in ListMultimediaExtension[indexOfTypeMultimediaItem].Item2)
        {
          ListMultimediaFileExtension.Add(item);
        }
      }
      #endregion Fill list extensions

      #region Побаловаться на "потом"
      //List<MultimediaFile> ListMultimediaFileUpdate;
      //MultimediaFile mf;
      //Func<string, MultimediaFile> ConvertFullPathToMultimediaFile = FullPath =>
      //  {
      //    mf = new MultimediaFile();
      //    mf.FullPath = FullPath;
      //    return mf;
      //  };
      #endregion Побаловаться на "потом"

      try
      {
        FolderBrowserDialog SelectedMultimediaPath = new FolderBrowserDialog();
        SelectedMultimediaPath.ShowNewFolderButton = true;
        DialogResult dr = SelectedMultimediaPath.ShowDialog();

        //DialogResult dr = new DialogResult();
        //dr = System.Windows.Forms.DialogResult.OK;

        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia\\music\\Аудиокниги\\S Tzu - Art of War";
        //SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia\\music\\Аудиокниги\\Аэлита";
        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia\\music\\Аудиокниги";
        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia\\music";
        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Multimedia";
        ////SelectedMultimediaPath.SelectedPath = "\\\\DLINK-8C754C\\Volume_1";
        if (dr == System.Windows.Forms.DialogResult.OK)
        {
          ListMultimediaFileBag = new ConcurrentBag<MultimediaFile>();
          usingResource = 0;

          SetStatusConnectToLiveMultimedia(enumLiveMultimedia.UpdateListMultimediaStart);
          await Task.Factory.StartNew(() => ReadFolder(SelectedMultimediaPath.SelectedPath, cancellationToken));

          long TotalCount = Interlocked.Read(ref usingResource);

#if (DEBUG)
          if (TotalCount != ListMultimediaFileBag.Count())
            ShowErrorMessage("Error in cmdAddMultimediaFolder_Click: TotalCount and ListFullPathBag.Count not equal: TotalCount=" + TotalCount.ToString() + ", ListFullPathBag.Count=" + ListMultimediaFileBag.Count().ToString());
#endif

          if (cancellationToken.IsCancellationRequested)
          {
            ListMultimediaFileBag = null;
            return;
          }
          #region Побаловаться на "потом"
          //var q1 = ListFullPathBag.Select(FullPath => ConvertFullPathToMultimediaFile);
          //foreach (MultimediaFile obj in q1)
          //{
          //  Console.WriteLine(
          //      "{0}",
          //      obj.FullPath);
          //}

          //ListMultimediaFileUpdate = (List<MultimediaFile>)ListFullPathBag.Select(FullPath => ConvertFullPathToMultimediaFile);
          //bool IsSuccess = await AddListMultimediaToServerAsync(ListMultimediaFileUpdate);
          #endregion Побаловаться на "потом"

          await AddListMultimediaToServerAsync();
        }
      }
      catch (Exception ex)
      {
        SetStatusConnectToLiveMultimedia(enumLiveMultimedia.UpdateListMultimediaStop);
        ListMultimediaFileBag = null;
        ShowErrorMessage(ex, "buttonAddFolder_Click");
      }
    }

    private void ReadFolder(string FolderName, CancellationToken ct)
    {
      try //Check access to directory
      {
        if (ct.IsCancellationRequested) return;
        IEnumerable<string> ListDirectories = Directory.EnumerateDirectories(FolderName, "*.*", SearchOption.TopDirectoryOnly);
        if (ct.IsCancellationRequested) return;
        Task.Factory.StartNew(() => ReadFiles(FolderName, ct), TaskCreationOptions.AttachedToParent);
        Parallel.ForEach(ListDirectories, FullName =>          
        {            
          if (ct.IsCancellationRequested) return;
          Task.Factory.StartNew(() => ReadFolder(FullName, ct), TaskCreationOptions.AttachedToParent);
        }          
        );
      }
      catch (Exception) { } //Bad access to directory
    }

    private void ReadFiles(string FolderName, CancellationToken ct)
    {
      string ContentCountFiles;
      MultimediaFile mf;
      string CurrentExtension;

      Parallel.ForEach(ListMultimediaFileExtension, Extension =>
      {
        //May by this "TRY" is not nesessary
        try //Check access to files in FolderName by extension
        {
          if (ct.IsCancellationRequested) return;
          IEnumerable<string> ListFiles = Directory.EnumerateFiles(FolderName, "*." + Extension, System.IO.SearchOption.TopDirectoryOnly);

          //Parallel.ForEach(ListFiles, FullPath => // Проблема со "слабым" процессором- превышение кол-ва потоков. Здесь замена на "foreach" попытка уменьшить нагрузку
          foreach (var FullPath in ListFiles)
          {
            try //Check access to file
            {
              if (ct.IsCancellationRequested) return;

              //Commented beacause it is long works
              //FileInfo CheckAccessFile = new FileInfo(FullName);
              //FileAttributes CheckFA = CheckAccessFile.Attributes;
              // OR
              //if (File.Exists(FullPath))
              //{
              //OK. File is available for adding     

              #region This block because EnumerateFiles get all extension after end our Extension
              var aPartName = FullPath.Split(new Char[] { '.' });
              if (aPartName.Length > 0)
                CurrentExtension = aPartName[aPartName.Length - 1];
              else
                CurrentExtension = "";
              #endregion This block because EnumerateFiles get all extension after end our Extension

              if (CurrentExtension == Extension)
              {
                mf = new MultimediaFile();
                mf.FullPath = FullPath;
                ListMultimediaFileBag.Add(mf);
                ContentCountFiles = Interlocked.Increment(ref usingResource).ToString();
                this.lblStatus2SearchFilesResult.Dispatcher.InvokeAsync((Action)(() => this.lblStatus2SearchFilesResult.Content = ContentCountFiles));
              }
              //};

            }
            catch (Exception) { } //Bad access to file
          }
          //);
        }
        catch (Exception) { } // Bad access to directory
      }
      );
    }

    private async void cmdConnectDisconnect_Click(object sender, RoutedEventArgs e)
    {
      await ApplicationStartStopAsync();
    }

    private async void lbMultimedia_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (this.lbMultimedia.Items.Count > 0)
      {
        MessageBoxResult result = System.Windows.MessageBox.Show(DeleteQuestion, DeleteTitle, MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes)
        {
          ListMultimediaFileBag = new ConcurrentBag<MultimediaFile>();
          foreach (var SelectedMultimediaFile in this.lbMultimedia.SelectedItems) ListMultimediaFileBag.Add((MultimediaFile)SelectedMultimediaFile);
          await RemoveListMultimediaToServerAsync();
        }
      }
    }

    private async Task ApplicationLogoutAsync()
    {
      string returnValue;

      if (CheckGoodString(UserToken))
      {
        try
        {
          using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
          {
            returnValue = await LiveMultimediaService.LocalLogoutAsync(AccountKey, UserToken);            
          }          
          UserToken = "";
          ListMultimediaFile.Clear();
          if (CheckGoodString(returnValue)) ShowErrorMessage("Incorrect of log out from Live Multimedia Market");
        }
        catch (Exception ex)
        {
          ShowErrorMessage(ex, "ApplicationLogoutAsync");
        }
      }
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (isExitFromApplication)
      {
        await Task.Yield();
        //await ApplicationLogoutAsync();

        ////if (!string.IsNullOrEmpty(UserToken) && !string.IsNullOrWhiteSpace(UserToken))
        ////{
        ////  try
        ////  {
        ////    using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        ////    {
        ////      LiveMultimediaService.Open();
        ////      bool IsSuccess = LiveMultimediaService.LocalLogout(UserToken);
        ////      if (!IsSuccess)
        ////      {
        ////        //System.Windows.MessageBox.Show("Error logout of user", TitleLiveMultimediaSystem);
        ////      }
        ////    }
        ////  }
        ////  catch (Exception)
        ////  {
        ////  }
        ////}
      }
      else
      {
        this.Hide();
        e.Cancel = true;
        this.NotifyContextMenu.MenuItems["Open"].Visible = true;
        this.ShowInTaskbar = false;
      }
    }

    private void chkUseProxy_Checked(object sender, RoutedEventArgs e)
    {
      this.tbProxyAddress.IsEnabled = true;
      this.tbProxyPort.IsEnabled = true;
      this.tbProxyAddress.Focus();
    }

    private void chkUseProxy_Unchecked(object sender, RoutedEventArgs e)
    {
      this.tbProxyAddress.IsEnabled = false;
      this.tbProxyPort.IsEnabled = false;
    }

    private void StatusLiveMultimediaSystem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
#if (!RELEASE)
      LoadDebugWindow();
#endif
    }

    private void lbEventsLiveMultimediaSystem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
    }

    private void cmdSelectLanguage_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var windowSelectLanguage = new SelectLangauge();
        windowSelectLanguage.ShowDialog();

        if (LiveMultimediaServiceConnection.IsSelectLanguage)
        {
          var splashScreen = new SplashScreen("SplashScreen.png");
          splashScreen.Show(false);

          LoadCulture();
          SetCulture();
          SetStatusConnectToLiveMultimedia(StatusLiveMultimediaServer);

          using (var rkLiveMultimediaSystem = Registry.CurrentUser.CreateSubKey(RegistryKeyName))
          {
            rkLiveMultimediaSystem.SetValue(RegistryValueName, LiveMultimediaServiceConnection.Language);
          }

          splashScreen.Close(TimeSpan.FromTicks(1));
        }
      }
      catch (Exception ex)
      {
        ShowErrorMessage(ex, "cmdSelectLanguage_Click");
      }
    }

    private void cmdAbout_Click(object sender, RoutedEventArgs e)
    {
      ApplicationAbout();
    }

    private async void cmdExit_Click(object sender, RoutedEventArgs e)
    {
      await ApplicationShutdown();
    }

    private void ApplicationShow()
    {
      this.NotifyContextMenu.MenuItems["Open"].Visible = false;
      this.WindowState = System.Windows.WindowState.Normal;
      this.ShowInTaskbar = true;
      this.Show();
      this.Activate();
    }

    private async Task ApplicationShutdown()
    {
      NotifyIconApplication.Visible = false;
      await ExitFromTasks();
      await ApplicationLogoutAsync();

      isExitFromApplication = true;
      System.Windows.Application app = System.Windows.Application.Current;      
      app.Shutdown();      
    }

    private void ApplicationAbout()
    {
      try
      {
        Process.Start(@"http://www.live-mm.com/About.aspx");
      }
      catch (Exception ex)
      {
        ShowErrorMessage(ex, "ApplicationAbout");
      }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == Key.Return)
      {
        //await ApplicationStartStopAsync();
      }
    }

    private void tbUsername_GotFocus(object sender, RoutedEventArgs e)
    {
      var control = sender as System.Windows.Controls.TextBox;

      control.SelectAll();
      
      if (IsAlwaysShowTip)
      {
        var ToolTop = control.ToolTip as System.Windows.Controls.ToolTip;
        if (ToolTop != null) ToolTop.IsOpen = true;
      }
    }

    private void pbPassword_GotFocus(object sender, RoutedEventArgs e)
    {
      var control = sender as System.Windows.Controls.PasswordBox;

      control.SelectAll();

      if (IsAlwaysShowTip)
      {
        var ToolTop = control.ToolTip as System.Windows.Controls.ToolTip;
        if (ToolTop != null) ToolTop.IsOpen = true;
      }
    }

    private async void tbUsername_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      var control = sender as System.Windows.Controls.TextBox;

      if (e.Key == Key.Return)
      {
        if (CheckGoodString(this.tbUsername.Text) && CheckGoodString(this.pbPassword.Password))
        {
          await ApplicationStartStopAsync();
        }
        else
        {
          if (!CheckGoodString(this.tbUsername.Text))
          {
            var ToolTop = this.tbUsername.ToolTip as System.Windows.Controls.ToolTip;            
            if (ToolTop != null)
            {
              ToolTop.Content = "Вы должны ввести имя пользователя";
              ToolTop.IsOpen = true;
            }
          }

          if (CheckGoodString(this.tbUsername.Text) && !CheckGoodString(this.pbPassword.Password))
          {
            this.pbPassword.Focus();
          }
        }
      }
    }

    private async void pbPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == Key.Return)
      {
        if (CheckGoodString(this.tbUsername.Text) && CheckGoodString(this.pbPassword.Password))
        {
          await ApplicationStartStopAsync();
        }

        if (!CheckGoodString(this.tbUsername.Text) && CheckGoodString(this.pbPassword.Password))
        {
          this.tbUsername.Focus();
        }
      }
    }

    private void tbUsername_LostFocus(object sender, RoutedEventArgs e)
    {
      var control = sender as System.Windows.Controls.TextBox;

      if (IsAlwaysShowTip)
      {
        var ToolTop = control.ToolTip as System.Windows.Controls.ToolTip;
        if (ToolTop != null) ToolTop.IsOpen = false;
      }

      //var control = sender as System.Windows.Controls.TextBox;

      //if (CheckGoodString(control.Text))
      //{
      //  if (!CheckFormatUsername(control.Text))
      //  {
      //    control.Focus();

      //    //var t = control.ToolTip as System.Windows.Controls.ToolTip;
      //    //if (t != null)
      //    //{
      //    //  t.IsOpen = true;
      //    //}      
      //  }
      //  else
      //  {
      //    //LoadSpeech();
      //  }
      //}

    }

    private void pbPassword_LostFocus(object sender, RoutedEventArgs e)
    {
      var control = sender as System.Windows.Controls.PasswordBox;

      if (IsAlwaysShowTip)
      {
        var ToolTop = control.ToolTip as System.Windows.Controls.ToolTip;
        if (ToolTop != null) ToolTop.IsOpen = false;
      }
    }

    #region Debug App

    Window DebugWindow = null;
    DataTable DebugTable;
    System.Windows.Controls.DataGrid DebugGridView;
    System.Windows.Controls.Button DebugStartStopEvents;
    bool IsDebugStop=false;

    void DebugWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      e.Cancel = true;
      IsDebugStop = true;
      DebugStartStopEvents.Content = "Start";
      DebugWindow.Hide();
    }

    void DebugClearEvents_Click(object sender, RoutedEventArgs e)
    {
      DebugTable.Rows.Clear();
    }

    void DebugStartStopEvents_Click(object sender, RoutedEventArgs e)
    {
      System.Windows.Controls.Button cmdPause = (System.Windows.Controls.Button)sender;
      if (cmdPause.Content.ToString() == "Stop")
      {
        IsDebugStop = true;
        cmdPause.Content = "Start";
        //DebugWindow.Dispatcher.BeginInvoke(() => { cmdPause.Content = "Start"; });
      }
      else
      {
        IsDebugStop = false;
        cmdPause.Content = "Stop";
        //DebugWindow.Dispatcher.BeginInvoke(() => { cmdPause.Content = "Stop"; });
      }        
    }

    private void LoadDebugWindow()
    {
      try
      {
        if (DebugWindow==null)
        {
          DebugWindow = new Window();
          DebugWindow.Title = TitleLiveMultimediaServer;
          DebugWindow.Background = BrushBackground;
          DebugWindow.Owner = this;
          DebugWindow.Width = 500;
          DebugWindow.Height = 300;
          DebugWindow.ResizeMode = ResizeMode.CanResize;
          DebugWindow.ShowInTaskbar = false;
          DebugWindow.WindowStartupLocation = WindowStartupLocation.Manual;
          DebugWindow.Closing += DebugWindow_Closing;

          var DebugClearEvents = new System.Windows.Controls.Button();
          DebugClearEvents.Content = "Clear events";
          DebugClearEvents.Click += DebugClearEvents_Click;

          DebugStartStopEvents = new System.Windows.Controls.Button();
          DebugStartStopEvents.Content = "Stop";
          DebugStartStopEvents.Click += DebugStartStopEvents_Click;

          CreateDebugGrid();
          var DebugGridStackPanel = new StackPanel();
          DebugGridStackPanel.Children.Add(DebugGridView);

          var DebugStackPanel = new StackPanel();
          DebugStackPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
          DebugStackPanel.VerticalAlignment = System.Windows.VerticalAlignment.Top;
          DebugStackPanel.Children.Add(DebugClearEvents);
          DebugStackPanel.Children.Add(DebugStartStopEvents);
          DebugStackPanel.Children.Add(DebugGridStackPanel);

          var DebugScrollViewer = new ScrollViewer();
          DebugScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
          DebugScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
          DebugScrollViewer.Content = DebugStackPanel;

          DebugWindow.Content = DebugScrollViewer;

          //var DebugCommandStackPanel = new StackPanel();
          //DebugCommandStackPanel.Children.Add(DebugClearEvents);

          //CreateDebugGrid();
          //var DebugGridStackPanel = new StackPanel();          
          //DebugGridStackPanel.Children.Add(DebugGridView);

          ////var DebugScrollViewer = new ScrollViewer();
          ////DebugScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
          ////DebugScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
          ////DebugScrollViewer.Content = DebugGridStackPanel;         

          //var DebugStackPanel = new StackPanel();
          //DebugStackPanel.Children.Add(DebugCommandStackPanel);
          //DebugStackPanel.Children.Add(DebugGridStackPanel);
          ////DebugStackPanel.Children.Add(DebugScrollViewer);          

          //DebugWindow.Content = DebugStackPanel;

        }

        IsDebugStop = false;
        DebugStartStopEvents.Content = "Stop";
        DebugWindow.Show();
      }
      catch (Exception ex)
      {
        ShowErrorMessage(ex, "LoadDebugWindow");
      }
    }

    private void CreateDebugGrid()
    {      
      DebugTable = new DataTable("DebugTable");
      DebugTable.Columns.Add(new DataColumn("Date", typeof(string)));
      DebugTable.Columns.Add(new DataColumn("Time", typeof(string)));
      DebugTable.Columns.Add(new DataColumn("DateTime", typeof(DateTime)));
      DebugTable.Columns.Add(new DataColumn("Action", typeof(string)));
      DebugTable.Columns.Add(new DataColumn("MultimediaFileGUID", typeof(string)));
      DebugTable.Columns.Add(new DataColumn("Chunk", typeof(int)));
      DebugTable.Columns.Add(new DataColumn("IsStopTransfer", typeof(bool)));
      DebugTable.Columns.Add(new DataColumn("MultimediaFileMemoryName", typeof(string)));
      DebugTable.Columns.Add(new DataColumn("TaskId", typeof(int)));
      DebugTable.Columns.Add(new DataColumn("Note", typeof(string)));

      DebugGridView = new System.Windows.Controls.DataGrid();
      DebugGridView.AutoGenerateColumns = true;
      DebugGridView.ItemsSource = DebugTable.AsDataView();      
    }

    private void WriteDebug(string Action, string MultimediaFileGUID, long MultimediaFileChunkCount, bool IsStopTransfer, string MultimediaFileMemoryName, int TaskId, string Note)
    {
      #region Define vars
      DateTime Now; string Year; string Month; string Day; string Hour; string Minute; string Second; string Milisecond;
      DataRow DebugGridRow;
      #endregion Define vars

#if (!RELEASE)
      if (DebugWindow != null && DebugWindow.IsVisible && !IsDebugStop)
      {
        try
        {
          Now = DateTime.Now;
          Year = Now.Year.ToString(); Month = Now.Month.ToString(); Day = Now.Day.ToString();
          Hour = Now.Hour.ToString(); Minute = Now.Minute.ToString(); Second = Now.Second.ToString(); Milisecond = Now.Millisecond.ToString();

          DebugWindow.Dispatcher.BeginInvoke(() =>
          {
            DebugGridRow = DebugTable.NewRow();
            DebugGridRow["Date"] = Now.ToShortDateString();
            DebugGridRow["Time"] = Hour + ":" + Minute + ":" + Second + "." + Milisecond;
            DebugGridRow["DateTime"] = Now;
            DebugGridRow["Action"] = Action;
            DebugGridRow["MultimediaFileGUID"] = MultimediaFileGUID;
            DebugGridRow["Chunk"] = MultimediaFileChunkCount;
            DebugGridRow["IsStopTransfer"] = IsStopTransfer;
            DebugGridRow["MultimediaFileMemoryName"] = MultimediaFileMemoryName;
            DebugGridRow["TaskId"] = TaskId;
            DebugGridRow["Note"] = Note;
            DebugTable.Rows.Add(DebugGridRow);
          }
          );
        }
        catch (Exception)
        {
        }
      }
#endif
    }

    private async Task WriteDebugAsync(string Action, string MultimediaFileGUID, long MultimediaFileChunkCount, bool IsStopTransfer, string MultimediaFileMemoryName, int TaskId, string Note)
    {
      #region Define vars
      DateTime Now; string Year; string Month; string Day; string Hour; string Minute; string Second; string Milisecond;
      DataRow DebugGridRow;
      #endregion Define vars

#if (!RELEASE)
      if (DebugWindow != null)
      {
        if (DebugWindow.IsVisible && !IsDebugStop)
        {
          await Task.Factory.StartNew(async () =>
          {
            //var CountTime = new Stopwatch(); CountTime.Start();
            //Trace.TraceError(string.Format("LocalSetMultimediaFileBuffer Start: ChunkCount={0} Range1={1} Range2={2}", ChunkCount, Range1, Range2));
            try
            {
              Now = DateTime.Now;
              Year = Now.Year.ToString(); Month = Now.Month.ToString(); Day = Now.Day.ToString();
              Hour = Now.Hour.ToString(); Minute = Now.Minute.ToString(); Second = Now.Second.ToString(); Milisecond = Now.Millisecond.ToString();

              lock (DebugTable)
              {
                DebugGridRow = DebugTable.NewRow();
                DebugGridRow["Date"] = Now.ToShortDateString();
                DebugGridRow["Time"] = Hour + ":" + Minute + ":" + Second + "." + Milisecond;
                DebugGridRow["DateTime"] = Now;
                DebugGridRow["Action"] = Action;
                DebugGridRow["MultimediaFileGUID"] = MultimediaFileGUID;
                DebugGridRow["Chunk"] = MultimediaFileChunkCount;
                DebugGridRow["IsStopTransfer"] = IsStopTransfer;
                DebugGridRow["MultimediaFileMemoryName"] = MultimediaFileMemoryName;
                DebugGridRow["TaskId"] = TaskId;
                DebugGridRow["Note"] = Note;
              }
              await DebugWindow.Dispatcher.BeginInvoke(() => DebugTable.Rows.InsertAt(DebugGridRow, 0));
            }
            catch (Exception)
            {
            }
            //CountTime.Stop(); Trace.TraceError(string.Format("LocalSetMultimediaFileBuffer Stop: Elapsed={0} ChunkCount={1} Range1={2} Range2={3}", CountTime.ElapsedMilliseconds, ChunkCount, Range1, Range2));
          });

          //try
          //{
          //  Now = DateTime.Now;
          //  Year = Now.Year.ToString(); Month = Now.Month.ToString(); Day = Now.Day.ToString();
          //  Hour = Now.Hour.ToString(); Minute = Now.Minute.ToString(); Second = Now.Second.ToString(); Milisecond = Now.Millisecond.ToString();

          //  lock (DebugTable)
          //  {
          //    DebugGridRow = DebugTable.NewRow();
          //    DebugGridRow["Date"] = Now.ToShortDateString();
          //    DebugGridRow["Time"] = Hour + ":" + Minute + ":" + Second + "." + Milisecond;
          //    DebugGridRow["DateTime"] = Now;
          //    DebugGridRow["Action"] = Action;
          //    DebugGridRow["MultimediaFileGUID"] = MultimediaFileGUID;
          //    DebugGridRow["Chunk"] = MultimediaFileChunkCount;
          //    DebugGridRow["IsStopTransfer"] = IsStopTransfer;
          //    DebugGridRow["MultimediaFileMemoryName"] = MultimediaFileMemoryName;
          //    DebugGridRow["TaskId"] = TaskId;
          //    DebugGridRow["Note"] = Note;              
          //  }
          //  await DebugWindow.Dispatcher.BeginInvoke(() => DebugTable.Rows.InsertAt(DebugGridRow, 0) );
          //}
          //catch (Exception)
          //{
          //}
        }
      }
#endif
    }

    private async Task WriteDebugAsync(string Action, string MultimediaFileGUID, long MultimediaFileChunkCount, bool IsStopTransfer, string MultimediaFileMemoryName, string Note)
    {
      #region Define vars
      DateTime Now; string Year; string Month; string Day; string Hour; string Minute; string Second; string Milisecond;
      DataRow DebugGridRow;
      #endregion Define vars

#if (!RELEASE)
      if (DebugWindow != null)
      {
        if (DebugWindow.IsVisible && !IsDebugStop)
        {
          try
          {
            Now = DateTime.Now;
            Year = Now.Year.ToString(); Month = Now.Month.ToString(); Day = Now.Day.ToString();
            Hour = Now.Hour.ToString(); Minute = Now.Minute.ToString(); Second = Now.Second.ToString(); Milisecond = Now.Millisecond.ToString();

            lock (DebugTable)
            {
              DebugGridRow = DebugTable.NewRow();
              DebugGridRow["Date"] = Now.ToShortDateString();
              DebugGridRow["Time"] = Hour + ":" + Minute + ":" + Second + "." + Milisecond;
              DebugGridRow["DateTime"] = Now;
              DebugGridRow["Action"] = Action;
              DebugGridRow["MultimediaFileGUID"] = MultimediaFileGUID;
              DebugGridRow["Chunk"] = MultimediaFileChunkCount;
              DebugGridRow["IsStopTransfer"] = IsStopTransfer;
              DebugGridRow["MultimediaFileMemoryName"] = MultimediaFileMemoryName;
              DebugGridRow["Note"] = Note;
            }
            await DebugWindow.Dispatcher.BeginInvoke(() => DebugTable.Rows.InsertAt(DebugGridRow, 0));
          }
          catch (Exception)
          {
          }
        }
      }
#endif
    }

    #endregion Debug App

    #region Write Log

    private async Task TracingAsync(string Procedure, enumTypeLog TypeLog, string Message = null)
    {
      string Scope = "LiveMultimediaServer";

      try
      {        
        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          await LiveMultimediaService.TracingAsync(AccountKey, TypeLog, Scope, Procedure, Message, null, null, UserToken);
        }
      }
      catch (Exception)
      {
      }
    }

    private async Task TraceInformationAsync(string Procedure, string Message)
    {
      string Scope = "LiveMultimediaServer";

      try
      {
        using (LiveMultimediaServiceClient LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          await LiveMultimediaService.TraceInformationAsync(AccountKey, Scope, Procedure, Message, null, null, UserToken);         
        }
      }
      catch (Exception ex)
      {
        ShowErrorMessage(ex, "WriteLog");
      }
    }

    private void WriteEventLog(string Procedure, Exception ex)
    {
    }

    private void ShowErrorMessage(string ErrorMessage)
    {
      SetStatusConnectToLiveMultimedia(enumLiveMultimedia.Disconnected);
      System.Windows.MessageBox.Show(ErrorMessage, TitleLiveMultimediaServer);
    }

    private void ShowErrorMessage(Exception ex, string NameFunction)
    {
      SetStatusConnectToLiveMultimedia(enumLiveMultimedia.Error);

#if (RELEASE)
      System.Windows.MessageBox.Show("Error", TitleLiveMultimediaServer, MessageBoxButton.OK);
#elif (DEBUG)
      System.Windows.MessageBox.Show(DateTime.Now.ToString() + " Error in " + NameFunction + ": " + ex.ToString(), TitleLiveMultimediaServer, MessageBoxButton.OK);
#else
      System.Windows.MessageBox.Show(DateTime.Now.ToString() + " Error in " + NameFunction + ": " + ex.ToString(), TitleLiveMultimediaServer, MessageBoxButton.OK);
#endif
    }

    #endregion Write Log

    public bool CheckGoodString(string CheckString)
    {
      bool IsSuccess = (!string.IsNullOrEmpty(CheckString) && !string.IsNullOrWhiteSpace(CheckString));
      return IsSuccess;
    }

    public bool CheckFormatUsername(string CheckString)
    {
      bool IsSuccess = false;

      var IsEmail = CheckString.Contains("@");
      var IsFormatEmail = true;

      IsSuccess = IsEmail && IsFormatEmail;

      return IsSuccess;
    }
  }

}
