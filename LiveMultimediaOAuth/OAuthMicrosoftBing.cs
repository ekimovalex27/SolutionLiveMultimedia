using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
//using System.ServiceModel.Channels;

namespace LiveMultimediaOAuth
{
  // https://github.com/matthiasxc/WinPhone-AzureTranslation/blob/master/My_Translation_App/Models/AdmAccessToken.cs
  //public class AdmAccessToken
  //{
  //  // required for deserialization
  //  public string access_token { get; set; }
  //  public string token_type { get; set; }
  //  public string expires_in { get; set; }
  //  public string scope { get; set; }

  //  // properties and methods for determining expired tokens
  //  public DateTime tokenEndTime { get; set; }

  //  public bool IsExpired()
  //  {
  //    //return false;
  //    DateTime now = DateTime.Now;
  //    double secondsLeft = tokenEndTime.Subtract(now).TotalSeconds;
  //    if (secondsLeft < 30)
  //      return true;
  //    else
  //      return false;
  //  }

  //  public void Initalize()
  //  {
  //    tokenEndTime = DateTime.Now.Add(new TimeSpan(0, 0, 600));
  //  }
  //}


  /*********************************/
  //http://code.msdn.microsoft.com/windowsazure/CSAzureBingTranslatorSample-7c3a2d9b/sourcecode?fileId=73948&pathId=1712312852
  /*********************************/

  /****************************** Module Header ******************************\
  Module Name:  AdmAccessToken.cs
  Project:      CSTranslatorForAzure
  Copyright (c) Microsoft Corporation.
 
  The sample code demonstrates how to use Bing translator API when you get it 
  from Azure marked place.

  This is a Admin Access Token entity class.
 
  This source is subject to the Microsoft Public License.
  See http://www.microsoft.com/en-us/openness/resources/licenses.aspx#MPL
  All other rights reserved.
 
  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
  EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
  WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
  \***************************************************************************/

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class AdmAccessToken
    {
      [DataMember]
      public string access_token { get; set; }
      [DataMember]
      public string token_type { get; set; }
      [DataMember]
      public string expires_in { get; set; }
      [DataMember]
      public string scope { get; set; }
    }

  /****************************** Module Header ******************************\
  Module Name:  AdmAuthentication.cs
  Project:      CSTranslatorForAzure
  Copyright (c) Microsoft Corporation.
 
  The sample code demonstrates how to use Bing translator API when you get it 
  from Azure marked place.

  This is a Admin authentication class. 
  You can create a authentication instance with this class, GetAccessToken method
  will return a AdmAccessToken instance.
 
  This source is subject to the Microsoft Public License.
  See http://www.microsoft.com/en-us/openness/resources/licenses.aspx#MPL
  All other rights reserved.
 
  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
  EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
  WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
  \***************************************************************************/

    public class AdmAuthentication
    {
      public static readonly string DatamarketAccessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
      private string clientId;
      private string cientSecret;
      private string request;

      public AdmAuthentication(string clientId, string clientSecret)
      {
        this.clientId = clientId;
        this.cientSecret = clientSecret;
        //If clientid or client secret has special characters, encode before sending request
        this.request = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientId), HttpUtility.UrlEncode(clientSecret));
      }

      public async Task<AdmAccessToken> GetAccessTokenAsync()
      {
        return await HttpPostAsync(DatamarketAccessUri, this.request);
      }

      public AdmAccessToken GetAccessToken()
      {
        return HttpPost(DatamarketAccessUri, this.request);
      }

      private async Task<AdmAccessToken> HttpPostAsync(string DatamarketAccessUri, string requestDetails)
      {
        //Prepare OAuth request 
        WebRequest webRequest = WebRequest.Create(DatamarketAccessUri);
        webRequest.ContentType = "application/x-www-form-urlencoded";
        webRequest.Method = "POST";
        byte[] bytes = Encoding.ASCII.GetBytes(requestDetails);
        webRequest.ContentLength = bytes.Length;
        using (Stream outputStream = await webRequest.GetRequestStreamAsync())
        {
          outputStream.Write(bytes, 0, bytes.Length);
        }
        using (WebResponse webResponse = await webRequest.GetResponseAsync())
        {
          DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));
          //Get deserialized object from JSON stream
          AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());
          return token;
        }
      }

      private AdmAccessToken HttpPost(string DatamarketAccessUri, string requestDetails)
      {
        //Prepare OAuth request 
        WebRequest webRequest = WebRequest.Create(DatamarketAccessUri);
        webRequest.ContentType = "application/x-www-form-urlencoded";
        webRequest.Method = "POST";
        byte[] bytes = Encoding.ASCII.GetBytes(requestDetails);
        webRequest.ContentLength = bytes.Length;
        using (Stream outputStream = webRequest.GetRequestStream())
        {
          outputStream.Write(bytes, 0, bytes.Length);
        }
        using (WebResponse webResponse = webRequest.GetResponse())
        {
          DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));
          //Get deserialized object from JSON stream
          AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());
          return token;
        }
      }
    }


  /****************************** Module Header ******************************\
  Module Name:  UserData.cs
  Project:      CSTranslatorForAzure
  Copyright (c) Microsoft Corporation.
 
  The sample code demonstrates how to use Bing translator API when you get it 
  from Azure marked place.

  This class store user's data.
 
  This source is subject to the Microsoft Public License.
  See http://www.microsoft.com/en-us/openness/resources/licenses.aspx#MPL
  All other rights reserved.
 
  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
  EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
  WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
  \***************************************************************************/

    public static class UserData
    {
      public static string clientID = "***";
      public static string clientSecret = "***";
    }

}
