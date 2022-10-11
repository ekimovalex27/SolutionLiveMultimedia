using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LiveMultimediaMarket
{
  public class LanguageInfo
  {
    public string Language { get; set; }
    public string NativeName { get; set; }
    public string DisplayName { get; set; }
  }

  public class LocalizationElement
  {
    public string ElementName { get; set; }
    public string ElementValue { get; set; }
  }

  public class MultimediaFile
  {
    public string FullPath { get; set; }
    public string DisplayName { get; set; }
    public string MultimediaFileGUID { get; set; }
    public string TypeMultimedia { get; set; }
    public string Album { get; set; }
    public bool isSelectMultimediaFile { get; set; }
    public string Title { get; set; }
    public string Subject { get; set; }
    public string Category { get; set; }
    public string Keywords { get; set; }
    public string Comments { get; set; }
    public string Source { get; set; }
    public string Author { get; set; }
  }
  public static class LiveMultimediaService
  {
    #region Define vars
    readonly static string BaseAddressService = "http://service.live-mm.com/LiveMultimediaService.svc/";
    #endregion Define vars

    public static async Task<Tuple<LanguageInfo[], string>> GetLanguagesAsync(string Language)
    {
      Tuple<LanguageInfo[], string> returnValue;

      try
      {
        using (var client = new HttpClient())
        {
          client.BaseAddress = new Uri(BaseAddressService);
          client.DefaultRequestHeaders.Accept.Clear();
          client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

          var response = await client.GetAsync("api/v1/Languages/" + Language);
          response.EnsureSuccessStatusCode();

          var resultJson = await response.Content.ReadAsStringAsync();
          var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

          var ListLanguages = parseResultJson[parseResultJson.First.Path].Children().Select(
            item => new LanguageInfo { DisplayName = (string)item["DisplayName"], Language = (string)item["Language"], NativeName = (string)item["NativeName"] }).ToList();

          var ErrorMessage = (string)parseResultJson[parseResultJson.First.Next.Path];

          returnValue = new Tuple<LanguageInfo[], string>(ListLanguages.ToArray(), ErrorMessage);
        }
      }
      catch (Exception ex)
      {
        returnValue = new Tuple<LanguageInfo[], string>(new List<LanguageInfo>().ToArray(), ex.Message);
      }

      return returnValue;
    }

    public static async Task<Tuple<LocalizationElement[], string, string>> LocalGetLocalizationAsync(string AccountKey, string Language)
    {
      Tuple<LocalizationElement[], string, string> returnValue;

      try
      {
        using (var client = new HttpClient())
        {
          client.BaseAddress = new Uri(BaseAddressService);
          client.DefaultRequestHeaders.Accept.Clear();
          client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

          var UriTemplate = string.Format("api/v1/Localization/Local?AccountKey={0}&Language={1}", AccountKey, Language);
          var response = await client.GetAsync(UriTemplate);
          response.EnsureSuccessStatusCode();

          var resultJson = await response.Content.ReadAsStringAsync();
          var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

          var ListFile = parseResultJson[parseResultJson.First.Path].Children().Select(
            item => new LocalizationElement { ElementName = (string)item["ElementName"], ElementValue = (string)item["ElementValue"] }).ToList();
          var NativeName = (string)parseResultJson[parseResultJson.First.Next.Path];
          var ErrorMessage = (string)parseResultJson[parseResultJson.First.Next.Next.Path];

          returnValue = new Tuple<LocalizationElement[], string, string>(ListFile.ToArray(), NativeName, ErrorMessage);
        }
      }
      catch (Exception ex)
      {
        returnValue = new Tuple<LocalizationElement[], string, string>(new List<LocalizationElement>().ToArray(), "", ex.Message);
      }

      return returnValue;
    }

    public static async Task<Tuple<string, string>> LocalLoginAsync(string AccountKey, string Username, string Password)
    {
      Tuple<string, string> returnValue;

      try
      {
        using (var client = new HttpClient())
        {
          client.BaseAddress = new Uri(BaseAddressService);
          client.DefaultRequestHeaders.Accept.Clear();
          client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

          var UriTemplate = string.Format("api/v1/Login/Local?AccountKey={0}&Username={1}&Password={2}", AccountKey, Username, Password);
          var response = await client.GetAsync(UriTemplate);
          response.EnsureSuccessStatusCode();

          var resultJson = await response.Content.ReadAsStringAsync();
          var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

          returnValue = new Tuple<string, string>(parseResultJson[parseResultJson.First.Path].ToString(), parseResultJson[parseResultJson.First.Next.Path].ToString());
        }
      }
      catch (Exception ex)
      {
        returnValue = new Tuple<string, string>("", ex.Message);
      }

      return returnValue;
    }

    public static async Task<Tuple<MultimediaFile[], string>> LocalGetListMultimediaFilesAsync(string AccountKey, string UserToken)
    {
      Tuple<MultimediaFile[], string> returnValue;

      try
      {
        using (var client = new HttpClient())
        {
          client.BaseAddress = new Uri(BaseAddressService);
          client.DefaultRequestHeaders.Accept.Clear();
          client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

          var UriTemplate = string.Format("api/v1/MultimediaFiles/Local?AccountKey={0}&UserToken={1}", AccountKey, UserToken);
          var response = await client.GetAsync(UriTemplate);
          response.EnsureSuccessStatusCode();

          var resultJson = await response.Content.ReadAsStringAsync();
          var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

          var ListFile = parseResultJson[parseResultJson.First.Path].Children().Select(
            item => new MultimediaFile { FullPath = (string)item["FullPath"], MultimediaFileGUID = (string)item["MultimediaFileGUID"], TypeMultimedia = (string)item["TypeMultimedia"] }).ToList();

          returnValue = new Tuple<MultimediaFile[], string>(ListFile.ToArray(), "");
        }
      }
      catch (Exception ex)
      {
        returnValue = new Tuple<MultimediaFile[], string>(new List<MultimediaFile>().ToArray(), ex.Message);
      }

      return returnValue;
    }

    public static async Task<Tuple<string[], string>> LocalGetMultimediaFileGUIDAsync(string AccountKey, string UserToken)
    {
      Tuple<string[], string> returnValue;

      try
      {
        using (var client = new HttpClient())
        {
          client.BaseAddress = new Uri(BaseAddressService);
          client.DefaultRequestHeaders.Accept.Clear();
          client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

          var UriTemplate = string.Format("api/v1/MultimediaFileGUID/Local?AccountKey={0}&UserToken={1}", AccountKey, UserToken);
          var response = await client.GetAsync(UriTemplate);
          response.EnsureSuccessStatusCode();

          var resultJson = await response.Content.ReadAsStringAsync();
          var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

          var ErrorMessage = (string)parseResultJson[parseResultJson.First.Next.Path];

          if (LiveMultimediaLibrary.CheckGoodString(ErrorMessage)) throw new AggregateException(ErrorMessage);

          var ListMultimediaFileGUID = parseResultJson[parseResultJson.First.Path].Children().Select(item => (string)item);

          returnValue = new Tuple<string[], string>(ListMultimediaFileGUID.ToArray(), "");
        }
      }
      catch (Exception ex)
      {
        returnValue = new Tuple<string[], string>(new List<string>().ToArray(), ex.Message);
      }

      return returnValue;
    }

    public static async Task<string> LocalSetMultimediaFileAttributesAsync(string AccountKey, string UserToken, long MultimediaFileLength, int SpeedServer, string IdJob)
    {
      //ЭТО МЕТОД POST!!!
      string returnValue;

      try
      {
        using (var client = new HttpClient())
        {
          client.BaseAddress = new Uri(BaseAddressService);
          client.DefaultRequestHeaders.Accept.Clear();
          client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

          var UriTemplate = string.Format("api/v1/MultimediaFileAttributes/Local?AccountKey={0}&UserToken={1}&MultimediaFileLength={2}&SpeedServer={3}&IdJob={4}", AccountKey, UserToken, MultimediaFileLength, SpeedServer, IdJob);
          var response = await client.GetAsync(UriTemplate);
          response.EnsureSuccessStatusCode();

          var resultJson = await response.Content.ReadAsStringAsync();
          var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

          returnValue = (string)parseResultJson[parseResultJson.First.Next.Path];
        }
      }
      catch (Exception ex)
      {
        returnValue = ex.Message;
      }

      return returnValue;
    }

    public static async Task<string> LocalSetMultimediaFileBufferAsync(string AccountKey, string UserToken, byte[] MultimediaFileBuffer, bool IsStopTransfer, string IdJob)
    {
      string returnValue;
      //ЭТО МЕТОД POST!!!
      try
      {
        using (var client = new HttpClient())
        {
          client.BaseAddress = new Uri(BaseAddressService);
          client.DefaultRequestHeaders.Accept.Clear();
          client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

          var UriTemplate = string.Format("api/v1//MultimediaFileBuffer/Local?AccountKey={0}&UserToken={1}&MultimediaFileBuffer={2}&IsStopTransfer={3}&IdJob={4}", AccountKey, UserToken, MultimediaFileBuffer, IsStopTransfer, IdJob);
          var response = await client.GetAsync(UriTemplate);
          response.EnsureSuccessStatusCode();

          var resultJson = await response.Content.ReadAsStringAsync();
          var parseResultJson = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

          returnValue = (string)parseResultJson[parseResultJson.First.Next.Path];
        }
      }
      catch (Exception ex)
      {
        returnValue = ex.Message;
      }

      return returnValue;
    }
  }
}
