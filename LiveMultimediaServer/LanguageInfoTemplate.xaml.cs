using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace LiveMultimediaServer
{
  public sealed partial class LanguageInfoTemplate : UserControl
  {

    public LiveMultimediaMarket.LanguageInfo LanguageInfoItem { get { return this.DataContext as LiveMultimediaMarket.LanguageInfo; } }

    public LanguageInfoTemplate()
    {
      this.InitializeComponent();

      this.DataContextChanged += (s, e) => Bindings.Update();
    }
  }
}
