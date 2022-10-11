using System;
using System.Windows;

using LiveMultimediaApplication.LiveMultimediaService;

namespace LiveMultimediaApplication
{
  /// <summary>
  /// Логика взаимодействия для SelectLangauge.xaml
  /// </summary>
  /// 

  public partial class SelectLangauge : Window
  {
    public SelectLangauge()
    {
      InitializeComponent();

      Title = LiveMultimediaServiceConnection.GetElementLocalization("TitleWindowSelectLanguage", "Select Language");
      cmdSaveLanguage.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdSaveLanguage", "Save");
      cmdCancelLanguage.Content = LiveMultimediaServiceConnection.GetElementLocalization("cmdCancelLanguage", "Cancel");

      try
      {
        var LiveConnection = new LiveMultimediaServiceConnection();
        using (var LiveMultimediaService = new LiveMultimediaServiceClient(LiveConnection.Binding, LiveConnection.EndPoint))
        {
          var returnLanguages=LiveMultimediaService.GetLanguages(LiveMultimediaServiceConnection.Language);
          ListLanguage.ItemsSource = returnLanguages.Item1;
        }
        if (ListLanguage.Items.Count > 0) this.ListLanguage.SelectedIndex = 0;
      }
      catch (Exception)
      {
        ListLanguage.ItemsSource = null;
      }
    }

    private void cmdSave_Click(object sender, RoutedEventArgs e)
    {
      if (ListLanguage.ItemsSource != null)
      {
        LiveMultimediaServiceConnection.Language = (ListLanguage.SelectedItem as LanguageInfo).Language;
        LiveMultimediaServiceConnection.IsSelectLanguage = true;
      }
      else
      {
        LiveMultimediaServiceConnection.IsSelectLanguage = false;
      }

      Close();
    }

    private void cmdCancel_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(LiveMultimediaServiceConnection.Language) || string.IsNullOrWhiteSpace(LiveMultimediaServiceConnection.Language))
      {
        LiveMultimediaServiceConnection.Language = LiveMultimediaServiceConnection.LanguageDefault;
        LiveMultimediaServiceConnection.IsSelectLanguage = true;
      }
      else
      {
        LiveMultimediaServiceConnection.IsSelectLanguage = false;
      }

      Close();
    }

    private void imageSelectLanguage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (ListLanguage.ItemsSource != null)
      {
        switch (ListLanguage.DisplayMemberPath)
        {
          case "DisplayName":
            ListLanguage.DisplayMemberPath = "NativeName";
            break;
          case "NativeName":
            ListLanguage.DisplayMemberPath = "DisplayName";
            break;
        }        
      }
    }
  }
}
