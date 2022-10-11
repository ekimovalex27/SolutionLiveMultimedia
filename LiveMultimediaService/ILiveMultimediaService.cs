using System;
using System.ServiceModel;

using System.IO;
using System.Threading.Tasks;
using System.Collections.Specialized;

using System.ServiceModel.Web;

using LiveMultimediaData;
using LiveMultimediaOAuth;
using JetSAS.StorageInterfacesService;

namespace JetSAS
{
  [ServiceContract(Namespace = "LiveMultimediaService")]
  public interface ILiveMultimediaService
  {
    [OperationContract]
    [WebGet(UriTemplate = "/Login/Local?AccountKey={AccountKey}&Username={Username}&Password={Password}")]
    Task<Tuple<string, string>> LocalLogin(string AccountKey, string Username, string Password);

    [OperationContract]
    [WebGet(UriTemplate = "/Logout/Local?AccountKey={AccountKey}&UserToken={UserToken}")]
    Task<string> LocalLogout(string AccountKey, string UserToken);

    [OperationContract]
    [WebGet(UriTemplate = "/MultimediaFiles/Local?AccountKey={AccountKey}&UserToken={UserToken}")]
    Task<Tuple<MultimediaFile[], string>> LocalGetListMultimediaFiles(string AccountKey, string UserToken);

    [OperationContract]
    //[WebInvoke(Method = "POST", UriTemplate = "/LocalListMultimediaFilesAdd?AccountKey={AccountKey}&UserToken={UserToken}&ListMultimediaFiles={ListMultimediaFiles}")]
    [WebInvoke(Method = "POST", UriTemplate = "/MultimediaFiles/Local/{AccountKey}/{UserToken}/ListMultimediaFiles")]
    Task<Tuple<string[], string>> LocalListMultimediaFilesAdd(string AccountKey, string UserToken, MultimediaFile[] ListMultimediaFiles);

    [OperationContract]
    //[WebInvoke(Method = "DELETE", UriTemplate = "/LocalListMultimediaFilesRemove?AccountKey={AccountKey}&UserToken={UserToken}&ListMultimediaFiles={ListMultimediaFiles}")]
    [WebInvoke(Method = "DELETE", UriTemplate = "/MultimediaFiles/Local/{AccountKey}/{UserToken}/ListMultimediaFiles")]
    Task<string> LocalListMultimediaFilesRemove(string AccountKey, string UserToken, MultimediaFile[] ListMultimediaFiles);

    [OperationContract]
    [WebGet(UriTemplate = "/Login/Remote?AccountKey={AccountKey}&Username={Username}&Password={Password}")]
    Task<Tuple<string, string>> RemoteLogin(string AccountKey, string Username, string Password);

    [OperationContract]
    [WebGet(UriTemplate = "/Logout/Remote?AccountKey={AccountKey}&UserToken={UserToken}")]
    Task<string> RemoteLogout(string AccountKey, string UserToken);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/Login/Remote?AccountKey={AccountKey}&FirstName={FirstName}&LastName={LastName}&Username={Username}&Password={Password}&IdTariffPlan={IdTariffPlan}&Language={Language}")]
    Task<Tuple<string, string>> RemoteRegisterNewUser(string AccountKey, string FirstName, string LastName, string Username, string Password, int IdTariffPlan, string Language);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/UpdateUserInfo/Remote?AccountKey={AccountKey}&UserToken={UserToken}&OldPassword={OldPassword}&NewPassword={NewPassword}")]
    Task<string> RemoteUpdateUserInfo(string AccountKey, string UserToken, string OldPassword, string NewPassword);

    [OperationContract]
    [WebGet(UriTemplate = "/GetListMultimediaSource?UserToken={UserToken}")]
    Task<MultimediaSource[]> GetListMultimediaSource(string UserToken);

    [OperationContract]
    [WebGet(UriTemplate = "/Settings/Local?AccountKey={AccountKey}&UserToken={UserToken}")]
    Task<string[]> LocalGetSettings(string AccountKey, string UserToken);

    // ------------------------------------------------------------

    [OperationContract]
    [WebGet(UriTemplate = "/MultimediaFileGUID/Local?AccountKey={AccountKey}&UserToken={UserToken}")]
    Task<Tuple<string[], string>> LocalGetMultimediaFileGUID(string AccountKey, string UserToken);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/MultimediaFileBuffer/Local?AccountKey={AccountKey}&UserToken={UserToken}&MultimediaFileBuffer={MultimediaFileBuffer}&IsStopTransfer={IsStopTransfer}&IdJob={IdJob}")]
    Task<string> LocalSetMultimediaFileBuffer(string AccountKey, string UserToken, byte[] MultimediaFileBuffer, bool IsStopTransfer, string IdJob);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/MultimediaFileAttributes/Local?AccountKey={AccountKey}&UserToken={UserToken}&MultimediaFileLength={MultimediaFileLength}&SpeedServer={SpeedServer}&IdJob={IdJob}")]
    Task<string> LocalSetMultimediaFileAttributes(string AccountKey, string UserToken, long MultimediaFileLength, int SpeedServer, string IdJob);

    #region OAuth

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/OAuth/Token/{AccountKey}/{UserToken}/{Id}/OAuthUserToken")]
    Task<bool> OAuthSetToken(string AccountKey, string UserToken, string Id, OAuthToken OAuthUserToken);

    #endregion OAuth

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/Playlist/Remote?UserToken={UserToken}&Playlist={Playlist}")]
    Task<string> RemotePlaylistSave(string UserToken, string Playlist);

    [OperationContract]
    [WebGet(UriTemplate = "/Playlist/Remote?UserToken={UserToken}")]
    Task<PlaylistObject[]> RemotePlaylistLoad(string UserToken);

    [OperationContract]
    [WebInvoke(Method = "DELETE", UriTemplate = "/Playlist/Remote?UserToken={UserToken}&IdPlaylist={IdPlaylist}")]
    Task<bool> RemotePlaylistDelete(string UserToken, Int64 IdPlaylist);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/PlaylistItem/Remote?UserToken={UserToken}&IdPlaylist={IdPlaylist}&IdTypeMultimediaSource={IdTypeMultimediaSource}&IdMultimediaItem={IdMultimediaItem}&MultimediaItem={MultimediaItem}")]
    Task<bool> RemotePlaylistItemSave(string UserToken, Int64 IdPlaylist, int IdTypeMultimediaSource, string IdMultimediaItem, string MultimediaItem);

    [OperationContract]
    [WebInvoke(Method = "DELETE", UriTemplate = "/PlaylistItem/Remote?UserToken={UserToken}&IdPlaylistItem={IdPlaylistItem}")]
    Task<bool> RemotePlaylistItemRemove(string UserToken, Int64 IdPlaylistItem);

    [OperationContract]
    [WebGet(UriTemplate = "/Languages/{Language}")]
    Task<Tuple<LanguageInfo[], string>> GetLanguages(string Language);

    [OperationContract]
    [WebGet(UriTemplate = "/Localization/Local?AccountKey={AccountKey}&Language={Language}")]
    Task<Tuple<LocalizationElement[], string, string>> LocalGetLocalization(string AccountKey, string Language);

    [OperationContract]
    [WebGet(UriTemplate = "/Localization/Remote?AccountKey={AccountKey}&Language={Language}")]
    Task<Tuple<LocalizationElement[], string, string>> RemoteGetLocalization(string AccountKey, string Language);

    [OperationContract]
    [WebGet(UriTemplate = "/Multimedia/Items/{Language}?AccountKey={AccountKey}&Id={Id}&GroupBy={GroupBy}&OrderBy={OrderBy}&UserToken={UserToken}")]
    Task<Tuple<MultimediaItem[], string>> GetItems(string AccountKey, string Language = null, string Id = null, string GroupBy = null, string OrderBy = null, string UserToken = null);

    [OperationContract]
    [WebGet(UriTemplate = "/BreadCrumbs/{Language}?AccountKey={AccountKey}&Source={Source}&ParentId={ParentId}&UserToken={UserToken}")]
    Task<Tuple<BreadCrumps[], string>> GetBreadCrumbs(string AccountKey, string Language = null, string Source = null, string ParentId = null, string UserToken = null);

    [OperationContract]
    [WebGet(UriTemplate = "/Multimedia/CheckAuthorization?AccountKey={AccountKey}&Id={Id}&UserToken={UserToken}")]
    Task<StringDictionary> CheckAuthorization(string AccountKey, string Id, string UserToken = null);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/Multimedia/CreateJob?AccountKey={AccountKey}&Id={Id}&UniqueMultimediaRequest={UniqueMultimediaRequest}")]
    Task<Tuple<StringDictionary, string>> RemoteCreateMultimediaJob(string AccountKey, string Id, string UniqueMultimediaRequest);

    [OperationContract]
    [WebInvoke(Method = "DELETE", UriTemplate = "/Multimedia/DeleteJob?AccountKey={AccountKey}&UserToken={UserToken}&IdJob={IdJob}")]
    Task<string> RemoteCancelMultimediaJob(string AccountKey, string UserToken, string IdJob);

    [OperationContract]
    [WebGet(UriTemplate = "/Multimedia/Get?AccountKey={AccountKey}&IdJob={IdJob}&Range1={Range1}&Range2={Range2}")]
    Task<Stream> GetMultimedia(string AccountKey, string IdJob, long Range1, long Range2);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/Multimedia/Type/{Language}?AccountKey={AccountKey}")]
    //[WebInvoke(Method = "POST", UriTemplate = "/GetTypeMultimedia/{Language}?AccountKey={AccountKey}")]
    //[WebGet(UriTemplate = "/GetTypeMultimedia/{AccountKey}/{Language}")]
    //[WebGet(UriTemplate = "/GetTypeMultimedia?accountkey={accountkey}&language={Language}")]
    Task<Tuple<LocalizationElement, string>> GetTypeMultimedia(string AccountKey, string Language);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/Tracing?AccountKey={AccountKey}&UserToken={UserToken}",BodyStyle=WebMessageBodyStyle.Wrapped)]
    Task Tracing(string AccountKey, enumTypeLog TypeLog, string Scope, string Procedure, string Message, string ClientIp = null, int? ClientPort = null, string UserToken = null);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/Traceing/Information?AccountKey={AccountKey}&UserToken={UserToken}", BodyStyle = WebMessageBodyStyle.Wrapped)]
    Task TraceInformation(string AccountKey, string Scope, string Procedure, string Message, string ClientIp = null, int? ClientPort = null, string UserToken = null);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/Tracing/Warning?AccountKey={AccountKey}&UserToken={UserToken}", BodyStyle = WebMessageBodyStyle.Wrapped)]
    Task TraceWarning(string AccountKey, string Scope, string Procedure, string Message, string ClientIp = null, int? ClientPort = null, string UserToken = null);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/Tracing/Error?AccountKey={AccountKey}&UserToken={UserToken}", BodyStyle = WebMessageBodyStyle.Wrapped)]
    Task TraceError(string AccountKey, string Scope, string Procedure, string Message, string ClientIp = null, int? ClientPort = null, string UserToken = null);
  }
}
