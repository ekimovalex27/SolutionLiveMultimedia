using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Net.Http;
using Windows.Data.Json;

using Newtonsoft.Json;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.AccessCache;

using LiveMultimediaMarket;

public class BindModel
{
  public Windows.UI.Color Color { get; set; }
}

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LiveMultimediaServer
{
  /// <summary>
  /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
  /// </summary>
  
  public sealed partial class MainPage : Page
  {
    #region Define vars  

    private ObservableCollection<MultimediaFile> ListMultimediaFile = new ObservableCollection<MultimediaFile>();
    private ConcurrentBag<Tuple<string, CancellationTokenSource>> ListJob = new ConcurrentBag<Tuple<string, CancellationTokenSource>>();

    private int MultimediaFileBufferLength;

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

    #endregion Define vars

    public MainPage()
    {
      InitializeComponent();

      Init();
    }

    private async void Init()
    {
      try
      {
        //StorageApplicationPermissions.FutureAccessList.Clear();

        //var folderpicker = new FolderPicker();
        //folderpicker.ViewMode = PickerViewMode.List;
        //folderpicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
        //folderpicker.FileTypeFilter.Add(".mp3");
        //var folder = await folderpicker.PickSingleFolderAsync();
        //if (folder != null)
        //{
        //  var token = StorageApplicationPermissions.FutureAccessList.Add(folder);

        //  //StorageApplicationPermissions.FutureAccessList.Clear();
        //  folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
        //}

        //  StorageApplicationPermissions.FutureAccessList.Clear();

        //if (!LiveMultimediaLibrary.CheckGoodString(LiveMultimediaServiceConnection.AccessListToken) || !StorageApplicationPermissions.FutureAccessList.ContainsItem(LiveMultimediaServiceConnection.AccessListToken))
        if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(LiveMultimediaServiceConnection.AccessListToken))
        {
          var folderpicker = new FolderPicker();
          folderpicker.ViewMode = PickerViewMode.List;
          folderpicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
          folderpicker.FileTypeFilter.Add(".mp3");
          var folder = await folderpicker.PickSingleFolderAsync();
          if (folder != null)
          {
            LiveMultimediaServiceConnection.AccessListToken= StorageApplicationPermissions.FutureAccessList.Add(folder);
            //var max = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.MaximumItemsAllowed;
          }
        }
        else
        {
          var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(LiveMultimediaServiceConnection.AccessListToken);
        }

        //FileOpenPicker openPicker = new FileOpenPicker();
        //openPicker.ViewMode = PickerViewMode.List;
        ////openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        //openPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

        //openPicker.FileTypeFilter.Add(".mp3");
        ////openPicker.FileTypeFilter.Add(".jpg");
        ////openPicker.FileTypeFilter.Add(".jpeg");
        ////openPicker.FileTypeFilter.Add(".png");

        //StorageFile file = await openPicker.PickSingleFileAsync();
        //if (file != null)
        //{
        //  // Application now has read/write access to the picked file
        //  //OutputTextBlock.Text = "Picked photo: " + file.Name;
        //}
        //else
        //{
        //  //OutputTextBlock.Text = "Operation cancelled.";
        //}
      }
      catch (Exception ex)
      {
      }

      try
      {
        var file = await StorageFile.GetFileFromPathAsync(@"\\192.168.1.254\volume_1\Multimedia\music\NAST\Музыка\chumakov_en.mp3");
      }
      catch (Exception ex)
      {
      }

      try
      {
        var file = await StorageFile.GetFileFromPathAsync(@"\\192.168.1.254\volume_1\Multimedia\music\NAST\Музыка\chumakov_ru.mp3");
      }
      catch (Exception ex)
      {
      }

      try
      {
        var file = await StorageFile.GetFileFromPathAsync(@"\\192.168.1.254\volume_1\Multimedia\music\NAST\Музыка\mp3_Music\Blue System\03 - Twilight (1989)\07 - Blue System - Twilight - Little Jeannie.mp3");
      }
      catch (Exception ex)
      {
      }

      try
      {
        var file = await StorageFile.GetFileFromPathAsync(@"D:\LiveMultimediaDemo\audio\!!Счёт.mp3");
      }
      catch (Exception ex)
      {
      }
     
      StackPanelSignInProgress.Visibility = Visibility.Collapsed;
      SignInError.Visibility = Visibility.Collapsed;

      InitCulture();

      //await SelectLanguageInfo();

      rootPivot.SelectedIndex = 0;
      SignInUsername.Focus(FocusState.Programmatic);

#if DEBUG
      SignInUsername.Text = "***";
      SignInPassword.Password = "***";
#endif
    }

    private void InitCulture()
    {
//      //LiveMultimediaServiceConnection.Language = (string)localSettings.Values["Language"];
//      if (LiveMultimediaLibrary.CheckEmptyString(LiveMultimediaServiceConnection.Language))
//      {
//        LanguageSave(LiveMultimediaServiceConnection.LanguageDefault);
//      }

//#if DEBUG
//      LanguageSave(LiveMultimediaServiceConnection.LanguageDefault);
//#endif
    }

    private async void ListLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //if (ListLanguage.SelectedItem != null)
      //{
      //  var newLanguage = (ListLanguage.SelectedItem as LiveMultimediaMarket.LanguageInfo).Language;

      //  if (newLanguage != LiveMultimediaServiceConnection.Language)
      //  {
      //    LanguageSave(newLanguage);
      //    await SelectLanguageInfo();
      //  }
      //}
    }

    //private async Task SelectLanguageInfo()
    //{
    //  LanguageProgressRing.IsActive = true;

    //  ListLanguage.ItemsSource = await LiveMultimediaMarket.LiveMultimediaService.GetLanguagesAsync("");
    //  ListLanguage.SelectedItem = ListLanguage.Items.First(item => (item as LiveMultimediaMarket.LanguageInfo).Language == LiveMultimediaServiceConnection.Language);
    //  ListLanguage.ScrollIntoView(ListLanguage.SelectedItem);

    //  await LoadCulture();
    //  SetCulture();

    //  LanguageProgressRing.IsActive = false;
    //}

    //private void LanguageSave(string newLanguage)
    //{
    //  LiveMultimediaServiceConnection.Language = newLanguage;
    //  localSettings.Values["Language"] = newLanguage;
    //}

    private async Task LoadCulture()
    {
      var returnValue = await LiveMultimediaService.LocalGetLocalizationAsync(LiveMultimediaServiceConnection.AccountKey, LiveMultimediaServiceConnection.Language);

      if (!LiveMultimediaLibrary.CheckGoodString(returnValue.Item3))
      {
        LiveMultimediaServiceConnection.ListLocalization = returnValue.Item1.ToList();
      }
      else
      {
        LiveMultimediaServiceConnection.ListLocalization = null;
      }
    }

    private void SetCulture()
    {
      ErrorUsernameEmpty = "Please enter a valid e-mail address";
      ErrorPasswordEmpty = "Please enter a valid password";
      ErrorLogon = "Sign in failure";

      //      this.NotifyMenuItemStart.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemStart_Start", "Start");
      //      this.NotifyMenuItemOpen.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemOpen", "Open");
      //      this.NotifyMenuItemViewEvents.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemViewEvents", "View Events");
      //      this.NotifyMenuItemAbout.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemAbout", "About");
      //      this.NotifyMenuItemExit.Text = LiveMultimediaServiceConnection.GetElementLocalization("NotifyMenuItemExit", "Exit");

      //      this.cmdAddMultimediaFile.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdAddMultimediaFile", "Add File");
      //      this.cmdAddMultimediaFolder.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdAddMultimediaFolder", "Add Folder");
      //      this.cmdConnectDisconnect.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdConnectDisconnect_Start", "Start");
      //      this.cmdSelectLanguage.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdSelectLanguage", "Language");
      //      this.cmdAbout.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdAbout", "About");
      //      this.cmdExit.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdExit", "Exit");

      //      this.lblUsername.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblUsername", "Username");
      //      this.lblPassword.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblPassword", "Password");
      //      this.chkAutoConnect.Content = LiveMultimediaServiceConnection.GetElementLocalization("chkAutoConnect", "Auto connect after start");
      //      this.chkUseProxy.Content = LiveMultimediaServiceConnection.GetElementLocalization("chkUseProxy", "Use Proxy");
      //      this.lblProxyPort.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblProxyPort", "Port");
      //      this.lblStatus1FilesInList.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblStatus1FilesInList", "Number of files in the list");
      //      this.lblStatus2SearchFiles.Content = LiveMultimediaServiceConnection.GetElementLocalization("lblStatus2SearchFiles", "Search of files");

      //      ErrorUsernameEmpty = LiveMultimediaServiceConnection.GetElementLocalization("ErrorUsernameEmpty", "Username can't be empty");
      //      ErrorPasswordEmpty = LiveMultimediaServiceConnection.GetElementLocalization("ErrorPasswordEmpty", "Password can't be empty");
      //      ErrorProxyEmpty = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyEmpty", "Proxy Address can't be empty");
      //      ErrorProxyPortEmpty = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyPortEmpty", "Proxy Port can't be empty");
      //      ErrorProxyPort = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyPort", "Proxy port error");
      //      ErrorProxyAddress = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyAddress", "Proxy Address error");
      //      ErrorProxyFormat = LiveMultimediaServiceConnection.GetElementLocalization("ErrorProxyFormat", "Use this format of proxy server");
      //      ErrorLogon = LiveMultimediaServiceConnection.GetElementLocalization("ErrorLogon", "Logon failure to");

      //      DeleteQuestion = LiveMultimediaServiceConnection.GetElementLocalization("DeleteQuestion", "Do you want to delete selected") + "?";
      //      DeleteTitle = LiveMultimediaServiceConnection.GetElementLocalization("DeleteTitle", "Delete files from list");
      //      SelectFileTitle = LiveMultimediaServiceConnection.GetElementLocalization("SelectFileTitle", "Select multimedia files");

      //      ExtensionAllFiles = LiveMultimediaServiceConnection.GetElementLocalization("ExtensionAllFiles", "All files");

      //      LoadTypeMultimedia();

      //#if (RELEASE)
      //      this.StatusLiveMultimediaSystem.ToolTip = LiveMultimediaServiceConnection.GetElementLocalization("StatusLiveMultimediaSystem", "State of the") + " " + TitleLiveMultimediaServer;
      //#endif
    }

    private async void cmdSignIn_Click(object sender, RoutedEventArgs e)
    {
      SignInError.Visibility = Visibility.Collapsed;
      SignInError.Text = "";

      #region Check Username and Password
      var Username = SignInUsername.Text.Trim(); var Password = SignInPassword.Password;

      if (!LiveMultimediaLibrary.CheckGoodString(Username))
      {
        SignInError.Text = ErrorUsernameEmpty;
        SignInError.Visibility = Visibility.Visible;
        SignInUsername.Focus(FocusState.Programmatic);
        return;
      }

      if (!LiveMultimediaLibrary.CheckGoodString(Password))
      {
        SignInError.Text = ErrorPasswordEmpty;
        SignInError.Visibility = Visibility.Visible;
        SignInPassword.Focus(FocusState.Programmatic);
        return;
      }
      #endregion #region Check Username and Password

      StackPanelSignInProgress.Visibility = Visibility.Visible;
      SignInProgress.IsActive = true;
      cmdSignIn.IsEnabled = false;
      cmdSignUp.IsEnabled = false;
      cmdSignUp.IsEnabled = false;

      var returnValue = await LiveMultimediaService.LocalLoginAsync(LiveMultimediaServiceConnection.AccountKey, Username, Password);

      if (!LiveMultimediaLibrary.CheckGoodString(returnValue.Item2))
      {
        LiveMultimediaServiceConnection.UserToken = returnValue.Item1;
      }
      else
      {
        LiveMultimediaServiceConnection.UserToken = "";
      }

      if (LiveMultimediaServiceConnection.UserToken != "")
      {
        var returnValueFile = await LiveMultimediaService.LocalGetListMultimediaFilesAsync(LiveMultimediaServiceConnection.AccountKey, LiveMultimediaServiceConnection.UserToken);
        if (!LiveMultimediaLibrary.CheckGoodString(returnValueFile.Item2))
        {
          var aListMultimediaFile = returnValueFile.Item1;

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
            gwListMultimediaFile.ItemsSource = ListMultimediaFile;

            //For DEBUG and TEST 
            var ct_new = new CancellationToken();

            await CheckRequestMultimediaFileBufferAsync(ct_new);
          }

          rootPivot.SelectedIndex = 2;
        }
      }
      else
      {
        SignInError.Visibility = Visibility.Visible;
        SignInError.Text = ErrorLogon;
      }

      StackPanelSignInProgress.Visibility = Visibility.Collapsed;
      cmdSignIn.IsEnabled = true;
      cmdSignUp.IsEnabled = true;
      cmdSignUp.IsEnabled = true;
      SignInProgress.IsActive = false;
    }

    private void cmdSignUp_Click(object sender, RoutedEventArgs e)
    {

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

          if (ct.IsCancellationRequested) break;
          var returnValue = await LiveMultimediaService.LocalGetMultimediaFileGUIDAsync(LiveMultimediaServiceConnection.AccountKey, LiveMultimediaServiceConnection.UserToken);
          ListRequest = returnValue.Item1;
          ErrorMessage = returnValue.Item2;

          CountTime.Stop();
          //await WriteDebugAsync("Check Request", "", 0, false, "", "Delta Check=" + CountTime.ElapsedMilliseconds.ToString());

          if (LiveMultimediaLibrary.CheckGoodString(ErrorMessage)) throw new ArgumentException(ErrorMessage);

          #endregion Get Multimedia Request

          if (ct.IsCancellationRequested) break;

          #region Parse Multimedia Request
          if (ListRequest != null)
          {
            if (ListRequest.Length > 0)
            {
              string MultimediaFileGUID;
              long Range1, Range2;

              //await WriteDebugAsync("Task.Run Start", "", 0, false, "", "ListRequest.Length=" + ListRequest.Length.ToString());

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
              //await WriteDebugAsync("Task.Run Stop", "", 0, false, "", "Task.Run Delta=" + CountTime.ElapsedMilliseconds.ToString());
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
          //await WriteDebugAsync("Check Request Error", "", 0, false, "", exceptionRequest.ToString());
        }

        #region Calculation DelayCurrent
        if (!isRequestError)
        {
          if (isRequestErrorPrevious)
          {
            isRequestErrorPrevious = false;
            //ErrorRequest = "";
            //SetStatusConnectToLiveMultimedia(enumLiveMultimedia.RequestOk);
          }
        }
        else
        {
          isRequestErrorPrevious = true;
          //ErrorRequest = exceptionRequest.Message;
          //SetStatusConnectToLiveMultimedia(enumLiveMultimedia.RequestError);
        }
        #endregion Calculation DelayCurrent
      }
    }

    private async Task PrepareAttributes(string IdJob, string MultimediaFileGUID, CancellationToken ct)
    {
      try
      {
        //await WriteDebugAsync("PrepareAttributes Start", MultimediaFileGUID, 0, false, IdJob, 0, "");

        #region Check incoming parameters
        if (!LiveMultimediaLibrary.CheckGoodString(MultimediaFileGUID)) throw new ArgumentException("MultimediaFileGUID is empty", "MultimediaFileGUID");
        if (!LiveMultimediaLibrary.CheckGoodString(IdJob)) throw new ArgumentException("IdJob is empty", "IdJob");
        if (ct == null) throw new ArgumentException("Cancelation token is null", "ct");

        #endregion Check incoming parameters

        #region Parse Filename
        var FullPath = GetFullPathByMultimediafileGUID(MultimediaFileGUID);
        var aFullPath = FullPath.Split(new char[] { '\\' });
        var FileName = aFullPath[aFullPath.Length - 1];
        #endregion Parse Filename


        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(FullPath);

        Windows.Storage.StorageFolder externalDevices = Windows.Storage.KnownFolders.RemovableDevices;

        var disks = await externalDevices.GetFoldersAsync();

        // Get the first child folder, which represents the SD card.
        Windows.Storage.StorageFolder sdCard = (await externalDevices.GetFoldersAsync()).FirstOrDefault();


        long MultimediaFileLength;
        using (var fs = new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8, FileOptions.Asynchronous))
        {
          MultimediaFileLength = fs.Length;
        }

        #region Check incoming parameters for MultimediaFileLength
        if (MultimediaFileLength <= 0) throw new ArgumentException("Multimedia File Length must be more zero", "MultimediaFileLength");
        #endregion Check incoming parameters for MultimediaFileLength

        int SpeedServer = 16000;
        var returnValue = await LiveMultimediaService.LocalSetMultimediaFileAttributesAsync(LiveMultimediaServiceConnection.AccountKey, LiveMultimediaServiceConnection.UserToken, MultimediaFileLength, SpeedServer, IdJob);
        if (LiveMultimediaLibrary.CheckGoodString(returnValue)) throw new ArgumentException(returnValue);

        //await WriteDebugAsync("PrepareAttributes Stop", MultimediaFileGUID, 0, false, IdJob, 0, "");
      }
      catch (Exception ex)
      {
        //WriteEventLog("PrepareAttributes", ex);
      }
    }

    private string GetFullPathByMultimediafileGUID(string MultimediaFileGUID)
    {
      string FullPath;

      try
      {
        var MultimediaFileFound = ListMultimediaFile.Single(MultimediaFile => MultimediaFile.MultimediaFileGUID == MultimediaFileGUID);
        FullPath = MultimediaFileFound.FullPath;
      }
      catch (Exception)
      {
        FullPath = "";
      }
      return FullPath;
    }


    private async Task SendMultimediaFileBufferAsync(string MultimediaFileGUID, string IdJob, long Range1, long Range2, CancellationToken ct)
    {
      #region Define vars
      string FullPath = "";
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

        //await WriteDebugAsync("Send: read file Start", MultimediaFileGUID, 0, false, IdJob, TaskId, FileName);
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
        //await WriteDebugAsync("Send: read file Stop", MultimediaFileGUID, 0, false, IdJob, TaskId, "Delta Read file=" + CountTime.ElapsedMilliseconds.ToString());

        if (ct.IsCancellationRequested) return;

        //await WriteDebugAsync("Set buffer Start", MultimediaFileGUID, 0, IsStopTransfer, IdJob, TaskId, FileName);
        CountTime.Restart();

        #region Send multimedia file buffer

        var returnValue = await LiveMultimediaService.LocalSetMultimediaFileBufferAsync(LiveMultimediaServiceConnection.AccountKey, LiveMultimediaServiceConnection.UserToken, MultimediaFileBuffer, IsStopTransfer, IdJob);
        if (LiveMultimediaLibrary.CheckGoodString(returnValue)) throw new ArgumentException(returnValue);

        #endregion Send multimedia file buffer

        CountTime.Stop();
        //await WriteDebugAsync("Set buffer Stop", MultimediaFileGUID, 0, IsStopTransfer, IdJob, TaskId, "Delta Set Buffer=" + CountTime.ElapsedMilliseconds.ToString());
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        //WriteDebug("SendMultimediaFileBufferAsync", MultimediaFileGUID, 0, true, IdJob, TaskId, ex.Message + ". FullPath=" + (FullPath ?? ""));
        //WriteEventLog("SendMultimediaFileBufferAsync", ex);
      }
      catch (Exception ex)
      {
        //WriteDebug("SendMultimediaFileBufferAsync", MultimediaFileGUID, 0, true, IdJob, TaskId, ex.ToString() + ". FullPath=" + (FullPath ?? ""));
        //WriteEventLog("SendMultimediaFileBufferAsync", ex);
      }
      #endregion Catch

      #region Finally
      finally
      {
        if (CountTime.IsRunning) CountTime.Stop();
      }
      #endregion Finally
    }

    private async Task CancelSendJobAsync(string IdJob)
    {
      await Task.Yield();

      try
      {
        //await WriteDebugAsync("CancelSendJobAsync Start", "", 0, true, IdJob, 0, "");

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

        //await WriteDebugAsync("CancelSendJobAsync Stop", "", 0, true, IdJob, 0, "");
      }
      catch (Exception ex)
      {
        //WriteEventLog("CancelSendAsync", ex);
      }
    }

    #region Write Debug
    #endregion Write Debug

    #region API

    //private async Task<IEnumerable<LanguageInfo>> GetLanguagesAsync()
    //{
    //  IEnumerable<LiveMultimediaService.LanguageInfo> returnValue;

    //  try
    //  {
    //    using (var client = new HttpClient())
    //    {
    //      client.BaseAddress = new Uri(BaseAddressService);
    //      client.DefaultRequestHeaders.Accept.Clear();
    //      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    //      var response = await client.GetAsync("api/v1/Languages/" + LiveMultimediaServiceConnection.Language);
    //      response.EnsureSuccessStatusCode();

    //      var resultJson = await response.Content.ReadAsStringAsync();
    //      var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

    //      returnValue = parseResultJson[parseResultJson.First.Path].Children().Select(
    //        item => new LiveMultimediaService.LanguageInfo { DisplayName = (string)item["DisplayName"], Language = (string)item["Language"], NativeName = (string)item["NativeName"] }).ToList();
    //    }
    //  }
    //  catch (Exception)
    //  {
    //    returnValue = new List<LiveMultimediaService.LanguageInfo>();
    //  }

    //  return returnValue;
    //}

    //private async Task<Tuple<string, string>> LocalLoginAsync(string AccountKey, string Username, string Password)
    //{
    //  Tuple<string, string> returnValue;

    //  try
    //  {
    //    using (var client = new HttpClient())
    //    {
    //      client.BaseAddress = new Uri(BaseAddressService);
    //      client.DefaultRequestHeaders.Accept.Clear();
    //      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    //      var UriTemplate = string.Format("api/v1/Login/Local?AccountKey={0}&Username={1}&Password={2}", AccountKey, Username, Password);
    //      var response = await client.GetAsync(UriTemplate);
    //      response.EnsureSuccessStatusCode();

    //      var resultJson = await response.Content.ReadAsStringAsync();
    //      var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

    //      returnValue = new Tuple<string, string>(parseResultJson[parseResultJson.First.Path].ToString(), parseResultJson[parseResultJson.First.Next.Path].ToString());
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    returnValue = new Tuple<string, string>("", ex.Message);
    //  }

    //  return returnValue;
    //}

    //private async Task<Tuple<MultimediaFile[], string>> LocalGetListMultimediaFilesAsync(string AccountKey, string UserToken)
    //{
    //  Tuple<LiveMultimediaService.MultimediaFile[], string> returnValue;

    //  try
    //  {
    //    using (var client = new HttpClient())
    //    {
    //      client.BaseAddress = new Uri(BaseAddressService);
    //      client.DefaultRequestHeaders.Accept.Clear();
    //      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    //      var UriTemplate = string.Format("api/v1/MultimediaFiles/Local?AccountKey={0}&UserToken={1}", AccountKey, UserToken);
    //      var response = await client.GetAsync(UriTemplate);
    //      response.EnsureSuccessStatusCode();

    //      var resultJson = await response.Content.ReadAsStringAsync();
    //      var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

    //      var ListFile = parseResultJson[parseResultJson.First.Path].Children().Select(
    //        item => new MultimediaFile { FullPath = (string)item["FullPath"], MultimediaFileGUID = (string)item["MultimediaFileGUID"], TypeMultimedia = (string)item["TypeMultimedia"] }).ToList();

    //      returnValue = new Tuple<MultimediaFile[], string>(ListFile.ToArray(), "");
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    returnValue = new Tuple<MultimediaFile[], string>(new List<MultimediaFile>().ToArray(), ex.Message);
    //  }

    //  return returnValue;
    //}

    //private async Task<Tuple<string[], string>> LocalGetMultimediaFileGUIDAsync(string AccountKey, string UserToken)
    //{
    //  Tuple<string[], string> returnValue;

    //  try
    //  {
    //    using (var client = new HttpClient())
    //    {
    //      client.BaseAddress = new Uri(BaseAddressService);
    //      client.DefaultRequestHeaders.Accept.Clear();
    //      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    //      var UriTemplate = string.Format("api/v1/MultimediaFileGUID/Local?AccountKey={0}&UserToken={1}", AccountKey, UserToken);
    //      var response = await client.GetAsync(UriTemplate);
    //      response.EnsureSuccessStatusCode();

    //      var resultJson = await response.Content.ReadAsStringAsync();
    //      var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

    //      var ErrorMessage = (string)parseResultJson[parseResultJson.First.Next.Path];

    //      if (LiveMultimediaLibrary.CheckGoodString(ErrorMessage)) throw new AggregateException(ErrorMessage);

    //      var ListMultimediaFileGUID = parseResultJson[parseResultJson.First.Path].Children().Select(item => (string)item);

    //      returnValue = new Tuple<string[], string>(ListMultimediaFileGUID.ToArray(), "");
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    returnValue = new Tuple<string[], string>(new List<string>().ToArray(), ex.Message);
    //  }

    //  return returnValue;
    //}

    //private async Task<string> LocalSetMultimediaFileAttributesAsync(string AccountKey, string UserToken, long MultimediaFileLength, int SpeedServer, string IdJob)
    //{
    //  //ЭТО МЕТОД POST!!!
    //  string returnValue;

    //  try
    //  {
    //    using (var client = new HttpClient())
    //    {
    //      client.BaseAddress = new Uri(BaseAddressService);
    //      client.DefaultRequestHeaders.Accept.Clear();
    //      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    //      var UriTemplate = string.Format("api/v1/MultimediaFileAttributes/Local?AccountKey={0}&UserToken={1}&MultimediaFileLength={2}&SpeedServer={3}&IdJob={4}", AccountKey, UserToken, MultimediaFileLength, SpeedServer, IdJob);
    //      var response = await client.GetAsync(UriTemplate);
    //      response.EnsureSuccessStatusCode();

    //      var resultJson = await response.Content.ReadAsStringAsync();
    //      var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

    //      returnValue = (string)parseResultJson[parseResultJson.First.Next.Path];
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    returnValue = ex.Message;
    //  }

    //  return returnValue;
    //}

    //private async Task<string> LocalSetMultimediaFileBufferAsync(string AccountKey, string UserToken, byte[] MultimediaFileBuffer, bool IsStopTransfer, string IdJob)
    //{
    //  string returnValue;
    //  //ЭТО МЕТОД POST!!!
    //  try
    //  {
    //    using (var client = new HttpClient())
    //    {
    //      client.BaseAddress = new Uri(BaseAddressService);
    //      client.DefaultRequestHeaders.Accept.Clear();
    //      client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    //      var UriTemplate = string.Format("api/v1//MultimediaFileBuffer/Local?AccountKey={0}&UserToken={1}&MultimediaFileBuffer={2}&IsStopTransfer={3}&IdJob={4}", AccountKey, UserToken, MultimediaFileBuffer, IsStopTransfer, IdJob);
    //      var response = await client.GetAsync(UriTemplate);
    //      response.EnsureSuccessStatusCode();

    //      var resultJson = await response.Content.ReadAsStringAsync();
    //      var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

    //      returnValue = (string)parseResultJson[parseResultJson.First.Next.Path];
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    returnValue = ex.Message;
    //  }

    //  return returnValue;
    //}

    #endregion API

    SolidColorBrush PaneBackgroundSelected = new SolidColorBrush(Windows.UI.Colors.Blue);
    SolidColorBrush PaneBackgroundUnSelected = new SolidColorBrush(Windows.UI.Colors.LightGray);
    SolidColorBrush PaneForeground= new SolidColorBrush(Windows.UI.Colors.White);

    private void HamburgerButton_Click(object sender, RoutedEventArgs e)
    {
      MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;

      if (MySplitView.IsPaneOpen)
      {
        //ButtonMenuHome.Visibility = Visibility.Collapsed;
      }
    }

    private void StackPanelHome_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
      ((StackPanel)sender).Background = PaneBackgroundSelected;
    }

    private void StackPanelHome_PointerExited(object sender, PointerRoutedEventArgs e)
    {
      ((StackPanel)sender).Background = PaneBackgroundUnSelected;
    }

    private void StackPanelLanguage_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
      ((StackPanel)sender).Background = PaneBackgroundSelected;
    }

    private void StackPanelLanguage_PointerExited(object sender, PointerRoutedEventArgs e)
    {
      ((StackPanel)sender).Background = PaneBackgroundUnSelected;
    }

    private void StackPanelLanguage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
      this.Frame.Navigate(typeof(Language));
    }

    private void cmdPaneLanguage_Click(object sender, RoutedEventArgs e)
    {
      this.Frame.Navigate(typeof(Language));
    }

    private void StackPanelSettings_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
      ((StackPanel)sender).Background = PaneBackgroundSelected;
    }

    private void StackPanelSettings_PointerExited(object sender, PointerRoutedEventArgs e)
    {
      ((StackPanel)sender).Background = PaneBackgroundUnSelected;
    }

  }

}
