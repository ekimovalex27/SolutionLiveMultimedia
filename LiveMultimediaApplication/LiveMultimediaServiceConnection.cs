using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

using LiveMultimediaApplication.LiveMultimediaService;

namespace LiveMultimediaApplication
{
  class LiveMultimediaServiceConnection
  {
    public static string Language { get; set; }
    public static string LanguageDefault = "en";
    public static bool IsSelectLanguage { get; set; }

    public static List<LocalizationElement> ListLocalization;

#if (RELEASE)
    private BasicHttpsBinding _binding;
#elif (DEBUG)
    private BasicHttpBinding _binding;
#else
    private BasicHttpsBinding _binding;
#endif

    private EndpointAddress _endpoint;

#if (RELEASE)
    public BasicHttpsBinding Binding
    {
      get { return _binding; }
    }
#elif (DEBUG)
    public BasicHttpBinding Binding
    {
      get { return _binding; }
    }
#else
    public BasicHttpsBinding Binding
    {
      get { return _binding; }
    }
#endif

    public EndpointAddress EndPoint
    {
      get { return _endpoint; }
    }

    public LiveMultimediaServiceConnection()
    {
#if (RELEASE)
      _binding = new BasicHttpsBinding();
#elif (DEBUG)
      _binding = new BasicHttpBinding();
#else
    _binding = new BasicHttpsBinding();
#endif
      _binding.Name = "BindingLiveMultimediaService";
      _binding.CloseTimeout = new TimeSpan(10, 01, 00);
      _binding.OpenTimeout = new TimeSpan(10, 01, 00);
      _binding.ReceiveTimeout = new TimeSpan(10, 01, 00);
      _binding.SendTimeout = new TimeSpan(10, 01, 00);
      _binding.MaxBufferSize = 2147483647;
      _binding.MaxReceivedMessageSize = 2147483647;
      _binding.TransferMode = TransferMode.Streamed;
      _binding.ReaderQuotas.MaxArrayLength = 2147483647;

#if (RELEASE)
      _endpoint = new EndpointAddress("https://service.live-mm.com/LiveMultimediaService.svc");
#elif (DEBUG)
      _endpoint = new EndpointAddress("http://service.live-mm.com:8080/LiveMultimediaService.svc");
      _endpoint = new EndpointAddress("http://service.live-mm.com/LiveMultimediaService.svc");
#else
      _endpoint = new EndpointAddress("https://service.live-mm.com/LiveMultimediaService.svc");
#endif

      //_endpoint = new EndpointAddress("http://service.live-mm.com/LiveMultimediaService.svc");
      //_endpoint = new EndpointAddress("http://livemultimediadebug.cloudapp.net/LiveMultimediaService.svc");
      //_endpoint = new EndpointAddress("http://livemultimediaserviceusa.cloudapp.net/LiveMultimediaService.svc");
      //_endpoint = new EndpointAddress("https://livemultimediaserviceeurope.cloudapp.net/LiveMultimediaService.svc");
      //_endpoint = new EndpointAddress("http://livemultimediaservice.trafficmanager.net/LiveMultimediaService.svc");
      //_endpoint = new EndpointAddress("https://livemultimediaservice.trafficmanager.net/LiveMultimediaService.svc");
      //_endpoint = new EndpointAddress("http://livemultimediaservice.trafficmanager.net/LiveMultimediaService.svc");
    }

    public static string GetElementLocalization(string ElementName, string DefaultElementValue)
    {
      string ElementValue;

      try
      {
        if (ListLocalization != null)
        {
          var LocalizationDictionaryItem = ListLocalization.Single(LocalizationElement => LocalizationElement.ElementName == ElementName);
          ElementValue = LocalizationDictionaryItem.ElementValue;
        }
        else
        {
          ElementValue = DefaultElementValue;
        }
      }
      catch (Exception)
      {
        ElementValue = DefaultElementValue;
      }

      return ElementValue;
    }

  }
}
