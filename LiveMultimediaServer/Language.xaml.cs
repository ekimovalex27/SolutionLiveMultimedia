using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using LiveMultimediaMarket;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

namespace LiveMultimediaServer
{
  /// <summary>
  /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
  /// </summary>
  public sealed partial class Language : Page
  {
    public Language()
    {
      this.InitializeComponent();

      Init();
    }

    private async void Init()
    {
//#if DEBUG
//      LiveMultimediaServiceConnection.Language = "en";
//#endif
      await SelectLanguageInfoAsync();
    }

    private async Task SelectLanguageInfoAsync()
    {
      LanguageProgressRing.IsActive = true;

      var returnValue= await LiveMultimediaService.GetLanguagesAsync(LiveMultimediaServiceConnection.Language);
      if (LiveMultimediaLibrary.CheckEmptyString(returnValue.Item2))
      {
        GridViewLanguage.ItemsSource = returnValue.Item1;
      }

      LanguageProgressRing.IsActive = false;
    }

    private async Task GetLocalizationAsync()
    {
      var returnValue = await LiveMultimediaService.LocalGetLocalizationAsync(LiveMultimediaServiceConnection.AccountKey, LiveMultimediaServiceConnection.Language);

      if (LiveMultimediaLibrary.CheckEmptyString(returnValue.Item3))
      {
        LiveMultimediaServiceConnection.ListLocalization = returnValue.Item1.ToList();
      }
    }

    private void GridViewLanguage_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    private async void GridViewLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (GridViewLanguage.SelectedItem != null)
      {
        var newLanguage = (GridViewLanguage.SelectedItem as LanguageInfo).Language;

        if (newLanguage != LiveMultimediaServiceConnection.Language)
        {
          LiveMultimediaServiceConnection.Language = newLanguage;

          await GetLocalizationAsync();
          SetCulture();

          BackToPreviousPage();
        }
      }
    }


    private void BackToPreviousPage()
    {
      var rootFrame = Window.Current.Content as Frame;
      if (rootFrame == null) return;

      // Navigate back if possible, and if the event has not already been handled .
      if (rootFrame.CanGoBack) rootFrame.GoBack();
    }


    private void SetCulture()
    {
      var rootFrame = Window.Current.Content as Frame;
      var cmdSignIn=rootFrame.FindName("cmdSignIn") as Button;

      //rootFrame.Navigate(typeof(MainPage),

      //ErrorUsernameEmpty = "Please enter a valid e-mail address";
      //ErrorPasswordEmpty = "Please enter a valid password";
      //ErrorLogon = "Sign in failure";

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

    ////AEV
    //protected override void OnNavigatedTo(NavigationEventArgs e)
    //{
    //  //var rootFrame = Window.Current.Content as Frame;

    //  //string myPages = "";
    //  //foreach (PageStackEntry page in rootFrame.BackStack)
    //  //{
    //  //  myPages += page.SourcePageType.ToString() + "\n";
    //  //}
    //  //stackCount.Text = myPages;

    //  //if (rootFrame.CanGoBack)
    //  if ((Window.Current.Content as Frame).CanGoBack)
    //  {
    //    // Show UI in title bar if opted-in and in-app backstack is not empty.
    //    Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;
    //  }
    //  else
    //  {
    //    // Remove the UI from the title bar if in-app back stack is empty.
    //    Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
    //  }
    //}

  }
}
