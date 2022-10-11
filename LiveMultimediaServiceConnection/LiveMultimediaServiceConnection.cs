using System;

using System.ServiceModel;
using Microsoft.Azure;

namespace LiveMultimediaServiceConnection
{
  public class LiveMultimediaServiceConnection
    {
      private BasicHttpBinding _binding;
      private EndpointAddress _endpoint;

      public BasicHttpBinding Binding
      {
        get { return _binding; }
      }

      public EndpointAddress EndPoint
      {
        get { return _endpoint; }
      }

      public LiveMultimediaServiceConnection()
      {
        _binding = new BasicHttpBinding();
        _binding.Name = "BindingLiveMultimediaService";
        _binding.CloseTimeout = new TimeSpan(10, 01, 00);
        _binding.OpenTimeout = new TimeSpan(10, 01, 00);
        _binding.ReceiveTimeout = new TimeSpan(10, 01, 00);
        _binding.SendTimeout = new TimeSpan(10, 01, 00);
        _binding.MaxBufferSize = 2147483647;
        _binding.MaxReceivedMessageSize = 2147483647;
        _binding.TransferMode = TransferMode.Streamed;
        _binding.ReaderQuotas.MaxArrayLength = 2147483647;
        _binding.ReaderQuotas.MaxStringContentLength = 2147483647; //Получение длинной ссылки на новости
        //_binding.ReaderQuotas.MaxBytesPerRead = 524288;

        var ServiceAddress = CloudConfigurationManager.GetSetting("EndpointAddress");
        _endpoint = new EndpointAddress(ServiceAddress);
      }
    }
}
