//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebAndLoadTestService
{
  using System;
  using System.Collections.Generic;
  using System.Text;
  using Microsoft.VisualStudio.TestTools.WebTesting;

  using System.ServiceModel;
  using LiveMultimediaService;

  public class WebTest1Coded : WebTest
  {
    BasicHttpBinding binding; EndpointAddress endpoint;

    private void InitEndPoint()
    {
      binding = new BasicHttpBinding();
      binding.Name = "BindingLiveMultimediaService";
      binding.CloseTimeout = new TimeSpan(10, 01, 00);
      binding.OpenTimeout = new TimeSpan(10, 01, 00);
      binding.ReceiveTimeout = new TimeSpan(10, 01, 00);
      binding.SendTimeout = new TimeSpan(10, 01, 00);
      binding.MaxBufferSize = 2147483647;
      binding.MaxReceivedMessageSize = 2147483647;
      binding.TransferMode = TransferMode.Streamed;
      binding.ReaderQuotas.MaxArrayLength = 2147483647;

#if (DEBUG==false)
        endpoint = new EndpointAddress("http://service.live-mm.com/LiveMultimediaService.svc");        
#endif
#if (DEBUG==true)
      endpoint = new EndpointAddress("http://127.0.0.1:8080/LiveMultimediaService.svc");
#endif
    }

    public WebTest1Coded()
    {
      this.PreAuthenticate = true;
      this.Proxy = "default";
      InitEndPoint();
    }

    public override IEnumerator<WebTestRequest> GetRequestEnumerator()
    {
      string UserToken = System.Guid.NewGuid().ToString();
      string KeyGuid = System.Guid.NewGuid().ToString();
      string MultimediaFileGUID = System.Guid.NewGuid().ToString();

      bool IsInitMultimediaFile;
      using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
      {
        LiveMultimediaSystem.Open();
        IsInitMultimediaFile = LiveMultimediaSystem.RemoteInitMultimediaFile(UserToken, KeyGuid, MultimediaFileGUID);
      }

      WebTestRequest request1 = new WebTestRequest("http://www.live-mm.com/");
      request1.ThinkTime = 2;

      //request1.Method = "POST";
      //request1.Encoding = System.Text.Encoding.GetEncoding("utf-8");
      //StringHttpBody request1Body = new StringHttpBody();
      //request1Body.ContentType = "";
      //request1Body.InsertByteOrderMark = false;
      //request1Body.BodyString = "";
      //request1.Body = request1Body;

      yield return request1;
      request1 = null;
    }
  }
}
