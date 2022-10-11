using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceModel.Activation;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Runtime.Serialization.Json;
using System.Collections.Concurrent;
using System.Collections.Specialized;

using System.ServiceModel.Channels;
using System.Net.Mail;
using System.Security.Cryptography;

using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

//using Microsoft.WindowsAzure.Storage.File;

using StackExchange.Redis; //Azure Cache Redis
//using Microsoft.WindowsAzure.Diagnostics;

using System.Web.Script.Serialization;
using System.Net.Http;
//using System.Web.Security;

using LiveMultimediaData;
using LiveMultimediaDataLayer;
using LiveMultimediaOAuth;
using JetSAS.StorageInterfacesService;
using System.Net.Http.Headers;

namespace JetSAS
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
  //[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  //[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)] 
  [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)] 

  public class LiveMultimediaService : ILiveMultimediaService
  {
    #region Define vars

    const string TitleLiveMultimediaSystem = "Live Multimedia Market";
    const string ServerAccountKey = "****";

    #region Windows Azure
    const int MaxCountQueueAzure = 32;

    static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
    static CloudQueueClient queueCloud = storageAccount.CreateCloudQueueClient();
    //static CloudBlobClient blobCloud = storageAccount.CreateCloudBlobClient();
    //static CloudBlobContainer containerCloud = blobCloud.GetContainerReference("livemultimediabuffer");

    //static ConnectionMultiplexer CacheConnection = LoadRedisConnection();
    static ConnectionMultiplexer CacheConnection = LazyConnectionCache.Connection;

    #endregion Windows Azure

    #region Parameters for service
    static int WaitTimeReplySeconds = Convert.ToInt32(CloudConfigurationManager.GetSetting("WaitTimeReplySeconds"));
    static int MaxCountRequestInQueue = Convert.ToInt32(CloudConfigurationManager.GetSetting("MaxCountRequestInQueue"));
    static string[] ListSmtpParameters = GetSmtpParameters();
    static bool IsDebugWrite = Convert.ToBoolean(CloudConfigurationManager.GetSetting("IsDebugWrite"));
    static bool IsRefreshTranslate = Convert.ToBoolean(CloudConfigurationManager.GetSetting("IsRefreshTranslate"));
    static string UrlMultimediaService = CloudConfigurationManager.GetSetting("UrlMultimediaService");    
    #endregion Parameters for service

    #region Localization
    private const string Project = "LiveMultimediaSystem";
    private const string LocalizationAccountKey = "***";
    private const string DefaultLanguage = "en";
    
    static string[] ListLanguagesForTranslate = LoadLanguagesForTranslate();
    #endregion Localization

    static List<MultimediaServer> ListMultimediaServer = LoadMultimediaServer();
    static SHA512 SHA512Hash = SHA512.Create();
    static long IdUserDemo = GetIdUserDemo();

    #region Old
    const string FolderConvertAudio = @"c:\LiveMultimediaWork\convertaudio";
    const string PathTargetMusic = @"c:\LiveMultimediaWork\music";
    const int CountMemoryFiles = 9;
    #endregion Old

    #endregion Define vars

    //public LiveMultimediaService()
    //{
    //  //int a = 1;
    //  DataLayer = new LiveMultimediaDL();
    //}

    //public LiveMultimediaService(ILiveMultimediaDL dalR)
    //{
    //  DataLayer = dalR;
    //}

    #region Authorization of users

    /// <summary>
    /// Authorization of user for Live Multimedia Server
    /// </summary>
    /// <param name="AccountKey"></param>
    /// <param name="Username"></param>
    /// <param name="Password"></param>
    /// <returns>UserToken of current session</returns>
    public async Task<Tuple<string, string>> LocalLogin(string AccountKey, string Username, string Password)
    {
      #region Define vars
      string UserToken;
      Tuple<string, string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Local);
        CheckGoodString("Username", Username);
        CheckGoodString("Password", Password);
        #endregion Check incoming parameters

        #region Verify Hash password
        var SourceData = LiveMultimediaDL.GetSourceDataForHash(Username, Password);
        var HashData = LiveMultimediaDL.GetSHA512Hash(SourceData);
        #endregion Verify Hash password

        UserToken = await LiveMultimediaDL.LoginAsync(Username, HashData, enumTypeUser.Local);

        CheckGoodString("UserToken", UserToken);
        CheckFormatGUID("UserToken", UserToken);

        var queueClient = queueCloud.GetQueueReference(GetClientQueueName(UserToken));
        await queueClient.CreateAsync();

        await TraceServiceAsync("LocalLogin", enumTypeLog.Information, Username, UserToken);

        var ErrorMessage = "";
        returnValue = new Tuple<string, string>(UserToken, ErrorMessage);        
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        UserToken = "";
        returnValue = new Tuple<string, string>(UserToken, ex.Message);
        TraceLogError(UserToken, "LocalLogin with Parameter", ex.ToString());
        TraceService("LocalLogin with Parameter", enumTypeLog.Error, "Username=" + (Username ?? "") + ". " + ex.ToString(), null);
      }
      catch (Exception ex)
      {
        UserToken = "";
        returnValue = new Tuple<string, string>(UserToken, CollectErrorService(ex));
        TraceLogError(UserToken, "LocalLogin", ex.ToString());
        TraceService("LocalLogin", enumTypeLog.Error, "Username=" + (Username ?? "") + ". " + ex.ToString(), null);
      }
      #endregion Catch

      return returnValue;
    }

    /// <summary>
    /// Authorization of user for Live Multimedia Market
    /// </summary>
    /// <param name="AccountKey"></param>
    /// <param name="Username"></param>
    /// <param name="Password"></param>
    /// <returns>UserToken of current session</returns>
    public async Task<Tuple<string, string>> RemoteLogin(string AccountKey, string Username, string Password)
    {
      #region Define vars
      string UserToken;
      Tuple<string, string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);
        CheckGoodString("Username", Username);
        CheckGoodString("Password", Password);
        #endregion Check incoming parameters

        #region Verify Hash password
        var SourceData = LiveMultimediaDL.GetSourceDataForHash(Username, Password);
        var HashData = LiveMultimediaDL.GetSHA512Hash(SourceData);
        #endregion Verify Hash password

        UserToken = await LiveMultimediaDL.LoginAsync(Username, HashData, enumTypeUser.Remote);

        JetSASLibrary.CheckGoodString("UserToken", UserToken);
        CheckFormatGUID("UserToken", UserToken);
        //await CheckUserToken("UserToken", UserToken, enumTypeUser.Remote);

        await TraceServiceAsync("RemoteLogin", enumTypeLog.Information, Username, UserToken);

        var ErrorMessage = "";
        returnValue = new Tuple<string, string>(UserToken, ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        UserToken = "";
        returnValue = new Tuple<string, string>(UserToken, ex.Message);
        TraceLogError(UserToken, "RemoteLogin with Parameter", ex.ToString());
        TraceService("RemoteLogin with Parameter", enumTypeLog.Error, "Username=" + (Username ?? "") + ". " + ex.ToString(), null);
      }
      catch (Exception ex)
      {
        UserToken = "";
        returnValue = new Tuple<string, string>(UserToken, CollectErrorService(ex));
        TraceLogError(UserToken, "RemoteLogin", ex.ToString());
        TraceService("RemoteLogin", enumTypeLog.Error, "Username=" + (Username ?? "") + ". " + ex.ToString(), null);
      }
      #endregion Catch

      return returnValue;
    }

    public async Task<string> LocalLogout(string AccountKey, string UserToken)
    {
      #region Define vars
      string returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Local);
        CheckGoodString("UserToken", UserToken);
        CheckFormatGUID("UserToken", UserToken);
        #endregion Check incoming parameters

        CloudQueue queueClient = queueCloud.GetQueueReference(GetClientQueueName(UserToken));
        await queueClient.DeleteIfExistsAsync();

        var IsSuccess = await LiveMultimediaDataLayer.LiveMultimediaDL.LogoutAsync(UserToken, enumTypeUser.Local);
        if (IsSuccess)
        {
          await TraceServiceAsync("LocalLogout", enumTypeLog.Information, null, UserToken);
          returnValue = "";
        }
        else
        {
          await TraceServiceAsync("LocalLogout", enumTypeLog.Error, "LiveMultimediaDataLayer.LiveMultimediaDL.Logout", UserToken);
          returnValue = "Internal error of sign out";
        }
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = ex.Message;
        TraceLogError(UserToken, "LocalLogout with Parameter", ex.ToString());
        TraceService("LocalLogout", enumTypeLog.Error, ex.ToString(), UserToken);
      }
      catch (Exception ex)
      {
        returnValue = CollectErrorService(ex);
        TraceLogError(UserToken, "LocalLogout", ex.ToString());
        TraceService("LocalLogout", enumTypeLog.Error, ex.ToString(), UserToken);
      }
      #endregion Catch

      return returnValue;
    }

    public async Task<string> RemoteLogout(string AccountKey, string UserToken)
    {
      #region Define vars
      string returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);
        CheckGoodString("UserToken", UserToken);
        CheckFormatGUID("UserToken", UserToken);
        //await CheckUserToken("UserToken", UserToken, enumTypeUser.Remote);
        #endregion Check incoming parameters

        var IsSuccess = await LiveMultimediaDataLayer.LiveMultimediaDL.LogoutAsync(UserToken, enumTypeUser.Remote);
        if (IsSuccess)
        {
          await TraceServiceAsync("RemoteLogout", enumTypeLog.Information, null, UserToken);
          returnValue = "";
        }
        else
        {
          Trace.TraceError("RemoteLogout. LiveMultimediaDataLayer.LiveMultimediaDL.Logout=false. UserToken={0}", UserToken);
          //await TraceServiceAsync("RemoteLogout", enumTypeLog.Error, "LiveMultimediaDataLayer.LiveMultimediaDL.Logout", UserToken);
          returnValue = "Internal error of sign out";
        }
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = ex.Message;
        TraceLogError(UserToken, "RemoteLogout with Parameter", ex.ToString());
        TraceService("RemoteLogout", enumTypeLog.Error, ex.ToString(), UserToken);
      }
      catch (Exception ex)
      {
        returnValue = CollectErrorService(ex);
        TraceLogError(UserToken, "RemoteLogout", ex.ToString());
        TraceService("RemoteLogout", enumTypeLog.Error, ex.ToString(), UserToken);
      }
      #endregion Catch

      return returnValue;
    }

    public async Task<Tuple<string, string>> RemoteRegisterNewUser(string AccountKey, string FirstName, string LastName, string Username, string Password, int IdTariffPlan, string Language)
    {
      #region Define vars
      string UserToken = "";
      string ErrorMessage;
      Tuple<string, string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);

        CheckGoodString("FirstName", FirstName);
        CheckGoodString("LastName", LastName);
        CheckGoodString("Username", Username);
        CheckGoodString("Password", Password);

        #region Check format e-mail
        var aUsername=Username.ToLower().Split(new char[] { '@' });
        if (aUsername.Length != 2) throw new ArgumentException("Username do not have format of e-mail", "Username");
        if (aUsername[1] == "live-mm.com") throw new ArgumentException("Bad Username", "Username");
        #endregion Check format e-mail

        if (IdTariffPlan <= 0) IdTariffPlan = 1;

        Language = ParseLanguage(Language);

        #endregion Check incoming parameters

        #region Trim parameters
        FirstName = FirstName.Trim();
        LastName = LastName.Trim();
        Username = Username.Trim();
        #endregion Trim parameters

        #region Hash password
        var SourceData = LiveMultimediaDL.GetSourceDataForHash(Username, Password);
        var HashData = LiveMultimediaDL.GetSHA512Hash(SourceData);
        var ResultVerify = LiveMultimediaDL.VerifySHA512Hash(SourceData, HashData);
        if (!ResultVerify) throw new ArgumentException("Internal error of registration", "Username");
        #endregion Hash password

        //var returnRegisterNewUser = await LiveMultimediaDL.RegisterNewUserAsync(FirstName, LastName, Username, HashData, IdTariffPlan, Language);

        //For Debug
#if (!DEBUG)
        var returnRegisterNewUser = await LiveMultimediaDL.RegisterNewUserAsync(FirstName, LastName, Username, HashData, IdTariffPlan, Language);
#else
        var returnRegisterNewUser = new Tuple<long, string>(5, "");
        Username = "***";
        Password = "***";
#endif

        if (JetSASLibrary.CheckGoodString(returnRegisterNewUser.Item2) || returnRegisterNewUser.Item1 <= 0)
        {
          var ErrorMessageNewUser = returnRegisterNewUser.Item2 ?? "Internal error of registration";
          throw new ArgumentException(ErrorMessageNewUser, "Username");
        }

        returnValue = await RemoteLogin(AccountKey, Username, Password);
        if (JetSASLibrary.CheckGoodString(returnValue.Item2)) throw new ArgumentException(returnValue.Item2, "UserToken");

        UserToken = returnValue.Item1;

        await TraceServiceAsync("RemoteRegisterNewUser", enumTypeLog.Information, Username, UserToken);

        // Registration and sign in is correct. Send confirm e-mail about registration
        await Task.Factory.StartNew(async () =>
        {
          await SendEmailRegistrationAsync(UserToken, FirstName, LastName, Username, Language);
        });

        var NoError ="";
        returnValue = new Tuple<string, string>(UserToken, NoError);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        UserToken = "";
        returnValue = new Tuple<string, string>(UserToken, ex.Message);

        ErrorMessage= ex.ToString() +
           ", Firstname=" + (FirstName ?? "") +
           ", LastName=" + (LastName ?? "") +
           ", Username=" + (Username ?? "") +
           ", Language=" + (Language ?? "");

        TraceLogError("", "RemoteRegisterNewUser with Parameter", ErrorMessage);
        TraceService("RemoteRegisterNewUser", enumTypeLog.Error, ErrorMessage, null);
      }

      catch (Exception ex)
      {
        UserToken = "";
        returnValue = new Tuple<string, string>(UserToken, ex.Message);

        ErrorMessage = ex.ToString() +
           ", Firstname=" + (FirstName ?? "") +
           ", LastName=" + (LastName ?? "") +
           ", Username=" + (Username ?? "") +
           ", Language=" + (Language ?? "");

        TraceLogError("", "RemoteRegisterNewUser", ErrorMessage);
        TraceService("RemoteRegisterNewUser", enumTypeLog.Error, ErrorMessage, null);
      }
      #endregion Catch

      return returnValue;
    }

    public async Task<string> RemoteUpdateUserInfo(string AccountKey, string UserToken, string OldPassword, string NewPassword)
    {
      #region Define vars
      string returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);

        CheckGoodString("UserToken", UserToken);
        CheckFormatGUID("UserToken", UserToken);

        CheckGoodString("OldPassword", OldPassword);
        CheckGoodString("NewPassword", NewPassword);
        if (OldPassword==NewPassword) throw new ArgumentException("The old password and the new password are the same");

        var IsDemoUser = await CheckIdUserDemoAsync(UserToken, enumTypeUser.Remote);
        if (IsDemoUser) throw new ArgumentException("You cannot change your password in demo mode");
        #endregion Check incoming parameters

        var TableUser = await LiveMultimediaDL.GetUserByUserTokenAsync(UserToken, (int)enumTypeUser.Remote);
        if (TableUser.Rows.Count != 1) throw new ArgumentException("", new Exception("LiveMultimediaDL.GetUserByUserTokenAsync: TableUser.Rows.Count != 1"));

        #region Get current user data

        var IdUser = TableUser.Rows[0].Field<long>("IdUser");
        if (IdUser <= 0) throw new ArgumentException("", new Exception("LiveMultimediaDL.GetUserByUserTokenAsync IdUser less or equil zero"));

        var Username = TableUser.Rows[0].Field<string>("Username");
        if (!JetSASLibrary.CheckGoodString(Username)) throw new ArgumentException("", new Exception("LiveMultimediaDL.GetUserByUserTokenAsync Username is empty"));

        var CurrentHashData = TableUser.Rows[0].Field<string>("Password");
        if (!JetSASLibrary.CheckGoodString(CurrentHashData)) throw new ArgumentException("", new Exception("LiveMultimediaDL.GetUserByUserTokenAsync Password is empty"));

        var FirstName = TableUser.Rows[0].Field<string>("FirstName");
        if (!JetSASLibrary.CheckGoodString(FirstName)) throw new ArgumentException("", new Exception("LiveMultimediaDL.GetUserByUserTokenAsync FirstName is empty"));

        var LastName = TableUser.Rows[0].Field<string>("LastName");
        if (!JetSASLibrary.CheckGoodString(LastName)) throw new ArgumentException("", new Exception("LiveMultimediaDL.GetUserByUserTokenAsync LastName is empty"));

        var Language = TableUser.Rows[0].Field<string>("Language");
        if (!JetSASLibrary.CheckGoodString(Language)) throw new ArgumentException("", new Exception("LiveMultimediaDL.GetUserByUserTokenAsync Language is empty"));

        #endregion Get current user data

        #region Hash old password
        var OldSourceData = LiveMultimediaDL.GetSourceDataForHash(Username, OldPassword);
        var OldHashData = LiveMultimediaDL.GetSHA512Hash(OldSourceData);
        #endregion Hash old password

        if (OldHashData != CurrentHashData) throw new ArgumentException("Old password is incorrect");

        #region Hash new password
        var NewSourceData = LiveMultimediaDL.GetSourceDataForHash(Username, NewPassword);
        var NewHashData = LiveMultimediaDL.GetSHA512Hash(NewSourceData);
        #endregion Hash new password

        await LiveMultimediaDL.UpdateUserInfoAsync(IdUser, NewHashData);

        await TraceServiceAsync("RemoteUpdateUserInfo", enumTypeLog.Information, "Updating user info is correct " + Username, UserToken);

        //Update password is correct.Send confirm e-mail about update user info
        await Task.Factory.StartNew(async () =>
        {
          await SendEmailUpdateUserInfoAsync(UserToken, FirstName, LastName, Username, Language);
        });

        returnValue = "";
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        if (ex.InnerException == null)
        {
          await TraceServiceAsync("RemoteUpdateUserInfo with Parameter", enumTypeLog.Error, ex.ToString(), UserToken);
          returnValue = ex.Message;
        }
        else
        {                    
          returnValue = CollectErrorService(ex.InnerException);
          await TraceServiceAsync("RemoteUpdateUserInfo with Parameter", enumTypeLog.Error, returnValue+". Exception: "+ex.InnerException.ToString(), UserToken);
        }
      }

      catch (Exception ex)
      {                
        returnValue = CollectErrorService(ex);
        await TraceServiceAsync("RemoteUpdateUserInfo", enumTypeLog.Error, returnValue + ". Exception: " + ex.ToString(), UserToken);
      }
      #endregion Catch

      return returnValue;
    }

    #endregion Authorization of users

    private async Task<Tuple<MultimediaFile[], string>> GetListMultimediaFiles(string AccountKey, string UserToken, enumTypeUser TypeUser)
    {
      #region Define vars
      DataTable MultimediaFileTable;
      List<MultimediaFile> ListMultimediaFile;
      Tuple<MultimediaFile[], string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, TypeUser);
        await FullCheckUserToken("UserToken", UserToken, TypeUser);
        #endregion Check incoming parameters

        MultimediaFileTable = await LiveMultimediaDataLayer.LiveMultimediaDL.GetListMultimediaFiles(UserToken, (int)TypeUser);

        #region Fill list
        ListMultimediaFile = new List<MultimediaFile>();
        MultimediaFile mf;
        foreach (DataRow dr in MultimediaFileTable.Rows)
        {
          mf = new MultimediaFile();
          mf.FullPath = dr["FullPath"].ToString();
          mf.MultimediaFileGUID = dr["MultimediaFileGUID"].ToString();
          mf.TypeMultimedia = dr["TypeMultimedia"].ToString();

          ListMultimediaFile.Add(mf);
        }
        #endregion Fill list

        returnValue = new Tuple<MultimediaFile[], string>(ListMultimediaFile.ToArray(), "");
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        ListMultimediaFile = new List<MultimediaFile>();
        returnValue = new Tuple<MultimediaFile[], string>(ListMultimediaFile.ToArray(), ex.Message);
        TraceLogError(UserToken, "GetListMultimediaFiles with Parameter", ex.ToString());
      }
      catch (Exception ex)
      {
        ListMultimediaFile = new List<MultimediaFile>();
        returnValue = new Tuple<MultimediaFile[], string>(ListMultimediaFile.ToArray(), CollectErrorService(ex));
        TraceLogError(UserToken, "GetListMultimediaFiles", ex.ToString());
      }
      #endregion Catch

      return returnValue;
    }

    public async Task<Tuple<MultimediaFile[], string>> LocalGetListMultimediaFiles(string AccountKey, string UserToken)
    {
      #region Define vars
      Tuple<MultimediaFile[], string> returnValue;
      #endregion Define vars

      returnValue = await GetListMultimediaFiles(AccountKey, UserToken, enumTypeUser.Local);

      return returnValue;
    }

    //public async Task<Tuple<MultimediaFile[], string>> RemoteGetListMultimediaFiles(string AccountKey, string UserToken)
    //{
    //  #region Define vars
    //  Tuple<MultimediaFile[], string> returnValue;
    //  #endregion Define vars

    //  returnValue = await GetListMultimediaFiles(AccountKey, UserToken, enumTypeUser.Local);

    //  return returnValue;
    //}

    public async Task<Tuple<string[], string>> LocalListMultimediaFilesAdd(string AccountKey, string UserToken, MultimediaFile[] ListMultimediaFiles)
    {
      #region Define vars
      List<string> ListGuids;
      DataTable MultimediaFileTable = new DataTable(); DataRow MultimediaFileRow;
      Tuple<string[], string> returnValue;
      #endregion Define vars

      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Local);
        await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Local);
        if (ListMultimediaFiles == null) throw new ArgumentException("ListMultimediaFiles is null", "ListMultimediaFiles");
        if (ListMultimediaFiles.Length==0) throw new ArgumentException("ListMultimediaFiles is empty", "ListMultimediaFiles");
        #endregion Check incoming parameters

        #region Define columns
        MultimediaFileTable.Columns.Add(new DataColumn("FullPath", typeof(string)));
        MultimediaFileTable.Columns.Add(new DataColumn("DisplayName", typeof(string)));
        MultimediaFileTable.Columns.Add(new DataColumn("MultimediaFileGUID", typeof(Guid)));
        MultimediaFileTable.Columns.Add(new DataColumn("TypeMultimedia", typeof(string)));
        MultimediaFileTable.Columns.Add(new DataColumn("Album", typeof(string)));
        #region Later
        //MultimediaFileTable.Columns.Add(new DataColumn("isSelectMultimediaFile", typeof(bool)));
        //MultimediaFileTable.Columns.Add(new DataColumn("Title", typeof(string)));
        //MultimediaFileTable.Columns.Add(new DataColumn("Subject", typeof(string)));
        //MultimediaFileTable.Columns.Add(new DataColumn("Category", typeof(string)));
        //MultimediaFileTable.Columns.Add(new DataColumn("Keywords", typeof(string)));
        //MultimediaFileTable.Columns.Add(new DataColumn("Comments", typeof(string)));
        //MultimediaFileTable.Columns.Add(new DataColumn("Source", typeof(string)));
        //MultimediaFileTable.Columns.Add(new DataColumn("Author", typeof(string)));
        #endregion Later
        #endregion Define columns

        #region Fill Table
        for (int i = 0; i < ListMultimediaFiles.Length; i++)
        {
          var aFullPath = ListMultimediaFiles[i].FullPath.Split(new Char[] { '\\' });
          var Album = aFullPath[aFullPath.Length - 2];

          MultimediaFileRow = MultimediaFileTable.NewRow();

          MultimediaFileRow["FullPath"] = ListMultimediaFiles[i].FullPath;
          MultimediaFileRow["DisplayName"] = GetDisplayName(MultimediaFileRow["FullPath"].ToString());
          MultimediaFileRow["MultimediaFileGUID"] = System.Guid.NewGuid();
          MultimediaFileRow["TypeMultimedia"] = GetTypeAudio(MultimediaFileRow["DisplayName"].ToString());
          MultimediaFileRow["Album"] = Album;

          MultimediaFileTable.Rows.Add(MultimediaFileRow);
        }
        #endregion Fill Table

        await LiveMultimediaDataLayer.LiveMultimediaDL.ListMultimediaFilesAddAsync(UserToken, MultimediaFileTable);

        #region Fill list guids
        ListGuids = new List<string>();
        foreach (DataRow Row in MultimediaFileTable.Rows)
        {
          ListGuids.Add(Row["MultimediaFileGUID"].ToString().ToUpper());
        }
        #endregion Fill list guids

        var ErrorMessage = "";
        returnValue = new Tuple<string[], string>(ListGuids.ToArray(), ErrorMessage);
      }
      catch (ArgumentException ex)
      {
        ListGuids = new List<string>();
        returnValue = new Tuple<string[], string>(ListGuids.ToArray(), ex.Message);
        TraceLogError(UserToken, "LocalAddListMultimediaFiles with Parameter", ex.ToString());
      }
      catch (Exception ex)
      {
        ListGuids = new List<string>();
        returnValue = new Tuple<string[], string>(ListGuids.ToArray(), CollectErrorService(ex));
        TraceLogError(UserToken, "LocalAddListMultimediaFiles", ex.ToString());
      }

      return returnValue;
    }

    public async Task<string> LocalListMultimediaFilesRemove(string AccountKey, string UserToken, MultimediaFile[] ListMultimediaFiles)
    {
      #region Define vars
      string returnValue;
      #endregion Define vars

      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Local);
        await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Local);
        #endregion Check incoming parameters

        DataTable tableGuids = new DataTable();
        DataRow MultimediaFileRow;

        tableGuids.Columns.Add(new DataColumn("MultimediaFileGUID", typeof(string)));

        for (int i = 0; i < ListMultimediaFiles.Length; i++)
        {
          if (CheckFormatGUID(ListMultimediaFiles[i].MultimediaFileGUID))
          {
            MultimediaFileRow = tableGuids.NewRow();
            MultimediaFileRow["MultimediaFileGUID"] = ListMultimediaFiles[i].MultimediaFileGUID;
            tableGuids.Rows.Add(MultimediaFileRow);
          }
        }
        if (tableGuids.Rows.Count > 0)
          await LiveMultimediaDataLayer.LiveMultimediaDL.ListMultimediaFilesRemoveAsync(UserToken, tableGuids);
        else
          throw new ArgumentException("ListMultimediaFiles is empty or contains bad guids", "LocalListMultimediaFilesRemove");

        returnValue = "";

      }
      catch (ArgumentException ex)
      {
        returnValue = ex.Message;
        TraceService("LocalListMultimediaFilesRemove with Parameter", enumTypeLog.Error, ex.ToString(), UserToken);
      }
      catch (Exception ex)
      {
        returnValue = CollectErrorService(ex);
        TraceService("LocalListMultimediaFilesRemove", enumTypeLog.Error, ex.ToString(), UserToken);
      }

      return returnValue;
    }
    
    private async Task SendEmailRegistrationAsync(string UserToken, string FirstName, string LastName, string Username, string Language)
    {
      string mailSubject, mailBody;

      #region Try
      try
      {
        #region Define SMTP parameters
        var SmtpFrom = ListSmtpParameters[0];
        var SmtpBcc = ListSmtpParameters[1];
        var SmtpHost = ListSmtpParameters[2];
        var SmtpPort = Convert.ToInt32(ListSmtpParameters[3]);
        var SmtpEnableSsl = Convert.ToBoolean(ListSmtpParameters[4]);
        var SmtpDefaultCredentials = Convert.ToBoolean(ListSmtpParameters[5]);
        var SmtpUserName = ListSmtpParameters[6];
        var SmtpPassword = ListSmtpParameters[7];
        #endregion Define SMTP parameters

        #region Get localization subject and body of mail

        mailSubject = "The successful registration in the";
        mailBody = "Hello. Thank you for registering in the. To get started, go to the. We are happy to answer your questions and listen to suggestions. With respect. Team";

        #region Translate mail subject
        var returnElement = await GetLocalizationElementAsync(Language, enumTopic.Service, "Mail_RegistrationUser_Subject", IsRefreshTranslate, mailSubject);
        if (JetSASLibrary.CheckGoodString(returnElement.Item2))
        {
          TraceService("SendEmailRegistrationAsync", enumTypeLog.Error, "Language=" + (Language ?? "") + "Mail_RegistrationUser_Subject, Exception=" + returnElement.Item2, UserToken);
        }
        mailSubject = returnElement.Item1;
        #endregion Translate mail subject

        #region Translate mail body
        returnElement = await GetLocalizationElementAsync(Language, enumTopic.Service, "Mail_RegistrationUser_Body", IsRefreshTranslate, mailBody);
        if (JetSASLibrary.CheckGoodString(returnElement.Item2))
        {
          TraceService("SendEmailRegistrationAsync", enumTypeLog.Error, "Language=" + (Language ?? "") + "Mail_RegistrationUser_Body, Exception=" + returnElement.Item2, UserToken);
        }

        mailBody = returnElement.Item1;
        #endregion Translate mail body

        #endregion Get localization subject and body of mail

        var aMailBody = mailBody.Split(new Char[] { '.' });

        var from = new MailAddress(SmtpFrom, TitleLiveMultimediaSystem, Encoding.UTF8);
        var to = new MailAddress(Username);        
        var mail = new MailMessage(from, to);

        var bcc = new MailAddress(SmtpBcc);
        mail.Bcc.Add(bcc);

        mail.IsBodyHtml = true;
        mail.DeliveryNotificationOptions = DeliveryNotificationOptions.Never;

        #region Define subject
        mail.SubjectEncoding = Encoding.UTF8;
        mail.Subject = mailSubject + " " + TitleLiveMultimediaSystem;
        #endregion Define subject

        #region Define body
        mail.BodyEncoding = Encoding.UTF8;
        mail.Body = "<p>" + aMailBody [0]+ ", " + FirstName + " " + LastName + "." + "</p>";
        mail.Body += "<p>" + aMailBody[1] + " " + "<a href=\"https://www.live-mm.com\">" + TitleLiveMultimediaSystem + "</a>" + "." + "</p>";
        mail.Body += "<p>" + aMailBody[2] + " " + "<a href=\"https://www.live-mm.com\">" + TitleLiveMultimediaSystem + "</a>" + "." + "</p>";
        mail.Body += "<p>" + aMailBody[3] + "." + "</p>";
        mail.Body += "</br>";
        mail.Body += "<p>" + aMailBody[4] + "," + "</p>";
        mail.Body += "<p>" + aMailBody[5] + " " + "<a href=\"https://www.live-mm.com\">" + TitleLiveMultimediaSystem + "</a>" + "." + "</p>";
        mail.Body += "</p>";
        mail.Body += "<p>" + "<a href=\"mailto:" + SmtpFrom + "\">" + SmtpFrom+"</a>" + "</p>";
        #endregion Define body

        #region Send mail

        SmtpClient smtp = new SmtpClient(SmtpHost, SmtpPort);
        smtp.EnableSsl = SmtpEnableSsl;
        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtp.UseDefaultCredentials = SmtpDefaultCredentials;
        smtp.Credentials = new NetworkCredential(SmtpUserName, SmtpPassword);
        await smtp.SendMailAsync(mail);
        mail.Dispose();

        #endregion Send mail

        await TraceServiceAsync("SendEmailRegistrationAsync", enumTypeLog.Information, "User registration email sent. Username=" + Username + ". Language=" + (Language ?? ""), UserToken);
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        TraceLogError(UserToken, "SendEmailRegistrationAsync", ex.ToString() +
          ", Firstname=" + (FirstName ?? "") +
          ", LastName=" + (LastName ?? "") +
          ", Username=" + (Username ?? "") +
          ", Language=" + (Language ?? ""));

        TraceServiceAsync("SendEmailRegistrationAsync", enumTypeLog.Error, ex.ToString() + ". Language=" + (Language ?? ""), UserToken);
      }
      #endregion Catch
    }

    private async Task SendEmailUpdateUserInfoAsync(string UserToken, string FirstName, string LastName, string Username, string Language)
    {
      string mailSubject, mailBody;

      #region Try
      try
      {
        #region Define SMTP parameters
        var SmtpFrom = ListSmtpParameters[0];
        var SmtpBcc = ListSmtpParameters[1];
        var SmtpHost = ListSmtpParameters[2];
        var SmtpPort = Convert.ToInt32(ListSmtpParameters[3]);
        var SmtpEnableSsl = Convert.ToBoolean(ListSmtpParameters[4]);
        var SmtpDefaultCredentials = Convert.ToBoolean(ListSmtpParameters[5]);
        var SmtpUserName = ListSmtpParameters[6];
        var SmtpPassword = ListSmtpParameters[7];
        #endregion Define SMTP parameters

        mailSubject = "The successful action in the";
        mailBody = "Hello.You just changed information about user.We are happy to answer your questions and listen to suggestions.With respect.Team";

        #region Get localization subject and body of mail

        #region Translate mail subject
        var returnElement = await GetLocalizationElementAsync(Language, enumTopic.Service, "Mail_UpdateUserInfo_Subject", IsRefreshTranslate, mailSubject);
        if (JetSASLibrary.CheckGoodString(returnElement.Item2)) //Error
        {
          TraceService("SendEmailUpdateUserInfoAsync", enumTypeLog.Error, "Language=" + (Language ?? "") + "Mail_UpdateUserInfo_Subject, Exception=" + returnElement.Item2, UserToken);
        }
        mailSubject = returnElement.Item1;
        #endregion Translate mail subject

        #region Translate mail body
        IsRefreshTranslate = true;
        returnElement = await GetLocalizationElementAsync(Language, enumTopic.Service, "Mail_UpdateUserInfo_Body", IsRefreshTranslate, mailBody);
        if (JetSASLibrary.CheckGoodString(returnElement.Item2))
        {
          TraceService("SendEmailUpdateUserInfoAsync", enumTypeLog.Error, "Language=" + (Language ?? "") + "Mail_UpdateUserInfo_Body, Exception=" + returnElement.Item2, UserToken);
        }

        mailBody = returnElement.Item1;
        #endregion Translate mail body

        #endregion Get localization subject and body of mail

        var aMailBody = mailBody.Split(new Char[] { '.' });

        var from = new MailAddress(SmtpFrom, TitleLiveMultimediaSystem, Encoding.UTF8);
        var to = new MailAddress(Username);
        var mail = new MailMessage(from, to);

        var bcc = new MailAddress(SmtpBcc);
        mail.Bcc.Add(bcc);

        mail.IsBodyHtml = true;
        mail.DeliveryNotificationOptions = DeliveryNotificationOptions.Never;

        #region Define subject
        mail.SubjectEncoding = Encoding.UTF8;
        mail.Subject = mailSubject + " " + TitleLiveMultimediaSystem;
        #endregion Define subject

        #region Define body
        mail.BodyEncoding = Encoding.UTF8;
        mail.Body = "";
        mail.Body += "<html>";
        mail.Body += "<head><META HTTP-EQUIV=\"content-type\" CONTENT=\"text/html; charset=UTF-8\"></head>";
        mail.Body += "<body>";
        mail.Body += "<span style=\"float:left\"><img src=\"https://www.live-mm.com/images/LiveMultimediaMarket64.png\"/></span>";
        mail.Body += "<p>" + aMailBody[0] + ", " + FirstName + " " + LastName + "." + "</p>"; //Hello.
        mail.Body += "<p>" + aMailBody[1] + " " + "<a href=\"https://www.live-mm.com\">" + TitleLiveMultimediaSystem + "</a>" + "." + "</p>"; //You just changed information about user.
        mail.Body += "</br>";
        mail.Body += "<p>" + aMailBody[2] + "." + "</p>"; //We are happy to answer your questions and listen to suggestions.
        mail.Body += "</br>";
        mail.Body += "<p>" + aMailBody[3] + "," + "</p>"; //With respect.
        mail.Body += "<p>" + aMailBody[4] + " " + "<a href=\"https://www.live-mm.com\">" + TitleLiveMultimediaSystem + "</a>" + "." + "</p>"; //Team
        mail.Body += "</br>";
        mail.Body += "<p>" + "<a href=\"mailto:" + SmtpFrom + "\">" + SmtpFrom + "</a>" + "</p>";
        mail.Body += "</body>";
        mail.Body += "</html>";
        #endregion Define body

        #region Send mail

        var smtp = new SmtpClient(SmtpHost, SmtpPort);
        smtp.EnableSsl = SmtpEnableSsl;
        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtp.UseDefaultCredentials = SmtpDefaultCredentials;
        smtp.Credentials = new NetworkCredential(SmtpUserName, SmtpPassword);
        await smtp.SendMailAsync(mail);
        mail.Dispose();

        #endregion Send mail

        await TraceServiceAsync("SendEmailUpdateUserInfoAsync", enumTypeLog.Information, "Send email about update user info is correct. " + Username, UserToken);
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        TraceLogError(UserToken, "SendEmailUpdateUserPasswordAsync", ex.ToString() +
          ", Firstname=" + (FirstName ?? "") +
          ", LastName=" + (LastName ?? "") +
          ", Username=" + (Username ?? "") +
          ", Language=" + (Language ?? ""));

        await TraceServiceAsync("SendEmailUpdateUserPasswordAsync", enumTypeLog.Error, ex.ToString() + ". Language=" + (Language ?? ""), UserToken);
      }
      #endregion Catch
    }

    public async Task<MultimediaSource[]> GetListMultimediaSource(string UserToken)
    {
      DataTable TableMultimediaSource = await LiveMultimediaDataLayer.LiveMultimediaDL.GetListMultimediaSourceAsync(UserToken);
      MultimediaSource[] ListMultimediaSource = new MultimediaSource[TableMultimediaSource.Rows.Count];
      MultimediaSource MultimediaSourceItem;
      int i = 0;
      foreach (DataRow MultimediaSourceRow in TableMultimediaSource.Rows)
      {
        MultimediaSourceItem = new MultimediaSource();

        MultimediaSourceItem.IdTypeMultimediaSource = (int)MultimediaSourceRow["IdTypeMultimediaSource"];
        MultimediaSourceItem.NameMultimediaSource = MultimediaSourceRow["NameMultimediaSource"] as string;
        MultimediaSourceItem.TitleMultimediaSource = MultimediaSourceRow["TitleMultimediaSource"] as string;
        MultimediaSourceItem.StyleWidth = (int)MultimediaSourceRow["StyleWidth"];
        MultimediaSourceItem.StyleHeight = (int)MultimediaSourceRow["StyleHeight"];
        MultimediaSourceItem.StyleForeColor = MultimediaSourceRow["StyleForeColor"] as string;
        MultimediaSourceItem.StyleFontSize = (Int32)MultimediaSourceRow["StyleFontSize"];
        MultimediaSourceItem.StyleBackColor = MultimediaSourceRow["StyleBackColor"] as string;
        MultimediaSourceItem.StyleBorderColor = MultimediaSourceRow["StyleBorderColor"] as string;
        MultimediaSourceItem.StyleName = MultimediaSourceRow["StyleName"] as string;

        ListMultimediaSource[i] = MultimediaSourceItem;

        i++;
      }
      return ListMultimediaSource;
    }

    #region Home Albums

    public async Task<string[]> LocalGetSettings(string AccountKey, string UserToken)
    {
      var LiveMultimediaServiceSettings = await GetSettings(AccountKey, UserToken, enumTypeUser.Local);
      return LiveMultimediaServiceSettings;
    }

    private string GetClientQueueName(string UserToken)
    {
      return ("client-" + UserToken.ToLower().Replace("-", ""));
    }

    private string GetJobName(StringCollection ListKeys)
    {
      var jobName = new StringBuilder();

      foreach (string item in ListKeys)
      {
        jobName.Append(item.ToLower().Replace("-", ""));
      }

      return jobName.ToString();
    }

    private string GetUserTokenFromJobName(string IdJob)
    {
      string UserTokenClient;
      Guid guidUserToken;

      if (System.Guid.TryParse(IdJob.Substring(32), out guidUserToken))
      {
        UserTokenClient = guidUserToken.ToString();
      }
      else
      {
        UserTokenClient = "";
      }
      return UserTokenClient;
    }

    private string GetFullJobName(string IdJob, long ChunkCount)
    {
      string FullJobName;

      try
      {
        FullJobName = IdJob + "|" + ChunkCount.ToString();
      }
      catch (Exception)
      {
        FullJobName = "";
      }
      
      return FullJobName;
    }

    private string GetJobNameAttributes(string IdJob)
    {
      var JobNameAttributes = "Attributes_" + IdJob;
      return JobNameAttributes;
    }

    public async Task<Tuple<string[], string>> LocalGetMultimediaFileGUID(string AccountKey, string UserToken)
    {
      #region Define vars      
      Tuple<string[], string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Local);
        CheckGoodString("UserToken", UserToken);
        CheckFormatGUID("UserToken", UserToken);
        #endregion Check incoming parameters

        var queueClient = queueCloud.GetQueueReference(GetClientQueueName(UserToken));

        IEnumerable<CloudQueueMessage> ListMessagesInQueue;
        ConcurrentBag<CloudQueueMessage> ListAllMessages = new ConcurrentBag<CloudQueueMessage>();

        int MaxCountRepeat = MaxCountRequestInQueue / MaxCountQueueAzure;
        for (int i = 0; i < MaxCountRepeat; i++)
        {
          ListMessagesInQueue = await queueClient.GetMessagesAsync(MaxCountQueueAzure);
          if (ListMessagesInQueue.Count() == 0) break;
          foreach (var ListMessagesItem in ListMessagesInQueue) ListAllMessages.Add(ListMessagesItem);
        }

        var ListMultimedia = ListAllMessages.Select(message => message.AsString).ToList();
        if (ListAllMessages.Count() > 0) await Task.Run(() => { foreach (var queuMessage in ListAllMessages) queueClient.DeleteMessageAsync(queuMessage); });

        var ErrorMessage = "";
        returnValue = new Tuple<string[], string>(ListMultimedia.ToArray(), ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = new Tuple<string[], string>(new List<string>().ToArray(), ex.Message);
        TraceLogError(UserToken, "LocalGetMultimediaFileGUID with Parameter", ex.ToString());
      }
      catch (Exception ex)
      {
        returnValue = new Tuple<string[], string>(new List<string>().ToArray(), CollectErrorService(ex));
        TraceLogError(UserToken, "LocalGetMultimediaFileGUID", ex.ToString());
      }
      #endregion Catch

      return returnValue;
    }

    private async Task<bool> CheckReadyClient(string UserTokenClient)
    {
      bool IsCheckReady;

      CloudQueue queueClient = queueCloud.GetQueueReference(GetClientQueueName(UserTokenClient));
      IsCheckReady = await queueClient.ExistsAsync();
      if (IsCheckReady)
      {
        //IsCheckReady = await LiveMultimediaDataLayer.LiveMultimediaDL.CheckRunClient(UserTokenClient); //Процедура ещё не готова. И нужна ли?
        IsCheckReady = true;
      }

      return IsCheckReady;
    }

    public async Task<string> LocalSetMultimediaFileBuffer(string AccountKey, string UserToken, byte[] MultimediaFileBuffer, bool IsStopTransfer, string IdJob)
    {
      //Trace.TraceError("LocalSetMultimediaFileBuffer: Start={0:hh:mm:ss.fff} ChunkCount={1} IdJob={2}", DateTime.Now, ChunkCount, IdJob);
      //return "";

      #region Define vars
      string ErrorMessage;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Local);
        CheckGoodString("UserToken", UserToken);
        CheckFormatGUID("UserToken", UserToken);

        //await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Local);

        if (MultimediaFileBuffer == null) throw new ArgumentException("MultimediaFileBuffer is null", "MultimediaFileBuffer");
        if (MultimediaFileBuffer.Length == 0) throw new ArgumentException("MultimediaFileBuffer Length equal zero", "MultimediaFileBuffer");

        if (!CheckGoodString(IdJob)) throw new ArgumentException("IdJob is empty", "IdJob");
        //Добавить проверку для корректности имени MultimediaFileName ?
        #endregion Check incoming parameters

        var blobName = IdJob;

        #region Write MultimediaFileBuffer

        await Task.Run(async () =>
        {
          try
          {
            var cache = CacheConnection.GetDatabase();
            await cache.SetAddAsync(blobName, MultimediaFileBuffer, CommandFlags.None);

            // Установить устаревание кэша в длительность мультимедиа
#if DEBUG
            await cache.KeyExpireAsync(blobName, new TimeSpan(1, 1, 10));
#else
        await cache.KeyExpireAsync(blobName, new TimeSpan(1, 1, 10));
#endif
          }
          catch (Exception ex)
          {
          }
        }
        );

        #endregion Write MultimediaFileBuffer

        ErrorMessage = "";
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        ErrorMessage = ex.Message;
        TraceLogError(UserToken, "LocalSetMultimediaFileBuffer with Parameter", "Exception: " + ex.ToString());
      }

      catch (Exception ex)
      {
        ErrorMessage = CollectErrorService(ex);
        TraceLogError(UserToken, "LocalSetMultimediaFileBuffer", ex.ToString() + ", IsStopTransfer=" + IsStopTransfer.ToString() + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return ErrorMessage;
    }

    public async Task<string> LocalSetMultimediaFileAttributes(string AccountKey, string UserToken, long MultimediaFileLength, int SpeedServer, string IdJob)
    {
      #region Define vars
      string ErrorMessage;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Local);
        CheckGoodString("UserToken", UserToken);
        CheckFormatGUID("UserToken", UserToken);

        //await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Local);

        if (MultimediaFileLength <= 0) throw new ArgumentException("MultimediaFileLength is less zero", "MultimediaFileLength");

        if (!CheckGoodString(IdJob)) throw new ArgumentException("IdJob is empty", "IdJob");
        //Добавить проверку для корректности имени MultimediaFileName 
        #endregion Check incoming parameters

        #region Write MultimediaFile Attributes

        var blobName = GetJobNameAttributes(IdJob);
        var cache = CacheConnection.GetDatabase();
        RedisValue[] MultimediaAttributes = new RedisValue[2];
        MultimediaAttributes[0] = MultimediaFileLength;
        MultimediaAttributes[1] = SpeedServer;
        await cache.SetAddAsync(blobName, MultimediaAttributes, CommandFlags.None);

        // Установить устаревание кэша
//#if DEBUG
//        await cache.KeyExpireAsync(blobName, new TimeSpan(1, 0, 30));
//#else
//        await cache.KeyExpireAsync(blobName, new TimeSpan(0, 0, 120));
//#endif
        // Пока устаревание 1 сутки
        await cache.KeyExpireAsync(blobName, new TimeSpan(24, 0, 0));

        #endregion Write MultimediaFile Attributes

        ErrorMessage = "";
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        ErrorMessage = ex.Message;
        TraceLogError(UserToken, "LocalSetMultimediaFileAttributes", ex.ToString() + ", MultimediaFileLength=" + MultimediaFileLength.ToString() + ", IdJob=" + (IdJob ?? ""));
      }

      catch (Exception ex)
      {
        ErrorMessage = CollectErrorService(ex);
        TraceLogError(UserToken, "LocalSetMultimediaFileAttributes", ex.ToString() + ", MultimediaFileLength=" + MultimediaFileLength.ToString() + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return ErrorMessage;
    }

    #region Get multimedia buffer
    private async Task<MemoryStream> GetMultimediaFileBufferHome(string UserToken, string UserTokenClient, string MultimediaFileGUID, string IdJob, long Range1, long Range2)
    {
      #region Define vars
      MultimediaStream multimediaStream;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckFormatGUID("MultimediaFileGUID", MultimediaFileGUID);
        #endregion Check incoming parameters

        var ClientQueueName = GetClientQueueName(UserTokenClient);
        var blobNameAttributes = GetJobNameAttributes(IdJob);
        var cache = CacheConnection.GetDatabase();
        var rvb = await cache.SetMembersAsync(blobNameAttributes, CommandFlags.None);
        var MultimediaFileLength = Convert.ToInt64(rvb[1]);

        multimediaStream = new MultimediaStream(IdJob, MultimediaFileGUID, ClientQueueName, MultimediaFileLength, Range1, Range2);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        multimediaStream = new MultimediaStream();
        TraceLogError(UserToken, "GetMultimediaFileBufferHome with Parameter", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", MultimediaFileGUID=" + (MultimediaFileGUID ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      catch (Exception ex)
      {
        multimediaStream = new MultimediaStream();
        TraceLogError(UserToken, "GetMultimediaFileBufferHome", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", MultimediaFileGUID=" + (MultimediaFileGUID ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return multimediaStream;
    }

    private MemoryStream GetMultimediaFileBufferNews(string UserToken, string IdItem, long IdItemLength, string IdJob, long Range1, long Range2)
    {
      #region Define vars
      MultimediaStreamNews multimediaStream = null;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        #endregion Check incoming parameters

        var ApiUrl = @"https://api.onedrive.com/v1.0";
        var DownloadUrl = ApiUrl + "/drive/items/" + IdItem + "/content";
        var OAuthAuthorizationHeader = "Authorization";

        multimediaStream = new MultimediaStreamNews(IdJob, IdItem, IdItemLength, Range1, Range2, DownloadUrl, OAuthAuthorizationHeader);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        multimediaStream = new MultimediaStreamNews();
        TraceLogError(UserToken, "GetMultimediaFileBufferNews with Parameter", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      catch (Exception ex)
      {
        multimediaStream = new MultimediaStreamNews();
        TraceLogError(UserToken, "GetMultimediaFileBufferNews", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return multimediaStream;
    }

    private MemoryStream GetMultimediaFileBufferOneDrive(string UserToken, string IdItem, long IdItemLength, string IdJob, long Range1, long Range2, string AccessToken)
    {
      #region Define vars
      MultimediaStreamOAuth multimediaStream =null;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        #endregion Check incoming parameters

        var ApiUrl = @"https://api.onedrive.com/v1.0";
        var DownloadUrl = ApiUrl + "/drive/items/" + IdItem + "/content";
        var OAuthAuthorizationHeader = "Authorization";

        multimediaStream = new MultimediaStreamOAuth(IdJob, IdItemLength, Range1, Range2, AccessToken, DownloadUrl, OAuthAuthorizationHeader);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferOneDrive with Parameter", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      catch (Exception ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferOneDrive", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return multimediaStream;
    }

    private MemoryStream GetMultimediaFileBufferGoogleDrive(string UserToken, string IdItem, long IdItemLength, string IdJob, long Range1, long Range2, string AccessToken)
    {
      #region Define vars
      MultimediaStreamOAuth multimediaStream = null;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        #endregion Check incoming parameters

        var ApiUrl = @"https://www.googleapis.com";
        var DownloadUrl = ApiUrl + "/drive/v2/files/" + IdItem + "?alt=media";
        var OAuthAuthorizationHeader = "Authorization";

        multimediaStream = new MultimediaStreamOAuth(IdJob, IdItemLength, Range1, Range2, AccessToken, DownloadUrl, OAuthAuthorizationHeader);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferGoogleDrive with Parameter", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      catch (Exception ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferGoogleDrive", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return multimediaStream;
    }

    private async Task<MemoryStream> GetMultimediaFileBufferVKontakte(string UserToken, string IdItem, long IdItemLength, string IdJob, long Range1, long Range2, int IdTypeMultimediaSource, string AccessToken)
    {
      #region Define vars
      MultimediaStreamOAuth multimediaStream = null;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        #endregion Check incoming parameters

        var DownloadUrl = await LiveMultimediaDL.GetDownloadURLVKontakteAsync(IdItem, IdTypeMultimediaSource, AccessToken);
        var OAuthAuthorizationHeader = "";

        multimediaStream = new MultimediaStreamOAuth(IdJob, IdItemLength, Range1, Range2, AccessToken, DownloadUrl, OAuthAuthorizationHeader);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferVKontakte with Parameter", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      catch (Exception ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferVKontakte", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return multimediaStream;
    }

    private async Task<MemoryStream> GetMultimediaFileBufferDropbox(string UserToken, string IdItem, long IdItemLength, string IdJob, long Range1, long Range2, string AccessToken)
    {
      #region Define vars
      MultimediaStreamOAuth multimediaStream = null;
      #endregion Define vars

      #region Try
      try
      {
        var DownloadUrl = await LiveMultimediaDL.GetDownloadURLDropboxAsync(IdItem, AccessToken);
        var OAuthAuthorizationHeader = "Authorization";

        multimediaStream = new MultimediaStreamOAuth(IdJob, IdItemLength, Range1, Range2, AccessToken, DownloadUrl, OAuthAuthorizationHeader);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferGoogleDrive with Parameter", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      catch (Exception ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferGoogleDrive", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return multimediaStream;
    }

    private async Task<MemoryStream> GetMultimediaFileBufferYandexDisk(string UserToken, string IdItem, long IdItemLength, string IdJob, long Range1, long Range2, string AccessToken)
    {
      #region Define vars
      MultimediaStreamOAuth multimediaStream = null;
      #endregion Define vars

      #region Try
      try
      {
        var DownloadUrl = await LiveMultimediaDL.GetDownloadURLYandexDiskAsync(IdItem, AccessToken);
        var OAuthAuthorizationHeader = "Authorization";

        multimediaStream = new MultimediaStreamOAuth(IdJob, IdItemLength, Range1, Range2, AccessToken, DownloadUrl, OAuthAuthorizationHeader);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferYandexDisk with Parameter", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      catch (Exception ex)
      {
        multimediaStream = new MultimediaStreamOAuth();
        TraceLogError(UserToken, "GetMultimediaFileBufferYandexDisk", ex.ToString() + ", UserToken=" + (UserToken ?? "") + ", IdItem=" + (IdItem ?? "") + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return multimediaStream;
    }
    #endregion Get multimedia buffer

    #region Create multimedia job
    private async Task<StringDictionary> RemoteCreateMultimediaJobHome(string IdJob, string IdItem, string UserTokenClient)
    {
      #region Define vars
      string MultimediaFileLength, ServerSpeed, MimeType;
      StringDictionary ListAttributes;
      #endregion Define vars

      #region First, check exist Attributes
      var blobNameAttributes = GetJobNameAttributes(IdJob);
      var cache = CacheConnection.GetDatabase();
      if (!await cache.KeyExistsAsync(blobNameAttributes, CommandFlags.None))
      {
        #region Queue message for start preparing buffers
        CloudQueue queueClient = queueCloud.GetQueueReference(GetClientQueueName(UserTokenClient));

        string queueMessage = Convert.ToInt32(enumActionMultimedia.StartTransferAttributes).ToString() + "|" + IdJob + "|" + IdItem;
        CloudQueueMessage message = new CloudQueueMessage(queueMessage);
        await queueClient.AddMessageAsync(message);
        #endregion Queue message for start prepare buffers
      }
      #endregion First, check exist Attributes

      #region Check exist blob with attributes        

      var ctsCheckExists = new CancellationTokenSource(new TimeSpan(0, 0, WaitTimeReplySeconds));

      try
      {
        while (!ctsCheckExists.IsCancellationRequested)
        {
          if (await cache.KeyExistsAsync(blobNameAttributes, CommandFlags.None))
          {
            break;
          }
          else
          {
            await Task.Delay(100);
          }
        }
      }
      catch (Exception)
      {
        ctsCheckExists.Cancel();
      }

      if (ctsCheckExists.IsCancellationRequested)
      {
        ctsCheckExists.Dispose();
        throw new ArgumentException("Live Multimedia Server is not ready");
      }

      ctsCheckExists.Dispose();
      #endregion Check exist blob with attributes        

      #region Read blob with attributes
      RedisValue[] rvb = null;
      rvb = await cache.SetMembersAsync(blobNameAttributes, CommandFlags.None);
      MultimediaFileLength = Convert.ToString(rvb[1]);
      ServerSpeed = Convert.ToString(rvb[0]);
      #endregion Read blob with attributes

      #region Fill List Attributes
      MimeType = await LiveMultimediaDL.SelectMimeTypeByMultimediaFileGUID(IdItem);

      ListAttributes = new StringDictionary();

      ListAttributes.Add("MultimediaFileLength", MultimediaFileLength);
      ListAttributes.Add("ServerSpeed", ServerSpeed);
      ListAttributes.Add("MimeType", MimeType);
      ListAttributes.Add("AccessToken", "");

      #endregion Fill List Attributes

      return ListAttributes;
    }

    private async Task<StringDictionary> RemoteCreateMultimediaJobNews(string IdItem)
    {
      #region Define vars
      string MultimediaFileLength, ServerSpeed, MimeType;
      StringDictionary ListAttributes;
      #endregion Define vars

      //var aIdItem = IdItem.Split(new char[] { '|' });
      //if (aIdItem.Length != 3) throw new ArgumentException("IdItem is bad for News", "IdItem", new Exception("IdItem don't contains all three parameters"));

      //var LanguageFromTranslate = aIdItem[0];
      //var LanguageToTranslate = aIdItem[1];
      //var NewsURL = aIdItem[2];

      //#region Check Language
      //LanguageFromTranslate = ParseLanguage(LanguageFromTranslate);
      //LanguageToTranslate = ParseLanguage(LanguageToTranslate);
      //#endregion Check Language

      //var text = await GetTextNewsFromURL(NewsURL);

      //if (LanguageFromTranslate != LanguageToTranslate)
      //{
      //  //text = await TranslateNews(text, LanguageFromTranslate, LanguageToTranslate);
      //  var returnValue = await TranslateTextAsync(LanguageFromTranslate, LanguageToTranslate, text);
      //  text = returnValue.Item1;
      //}

      //var aSpeak = await SpeakNews(text, LanguageToTranslate);

      var returnValueOAuth = await LiveMultimediaDL.GetMultimediaJobAttributesNews(IdItem);

      if (returnValueOAuth <= 0) throw new ArgumentException("Error access to News", "UserToken", new Exception("MultimediaFileLength is less or equil zero"));
      MultimediaFileLength = returnValueOAuth.ToString();

      ServerSpeed = "32000";
      MimeType = "audio/mp3";

      #region Fill List Attributes
      ListAttributes = new StringDictionary();

      ListAttributes.Add("MultimediaFileLength", MultimediaFileLength);
      ListAttributes.Add("ServerSpeed", ServerSpeed);
      ListAttributes.Add("MimeType", MimeType);
      ListAttributes.Add("AccessToken", "");
      #endregion Fill List Attributes

      return ListAttributes;
    }

    private async Task<StringDictionary> RemoteCreateMultimediaJobOneDrive(string UserToken, string IdItem, int IdTypeMultimediaSource)
    {
      #region Define vars
      string MultimediaFileLength, ServerSpeed, MimeType;
      StringDictionary ListAttributes;
      #endregion Define vars

      #region Read blob with attributes

      var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
      if (!JetSASLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("Error authentication to OneDrive", "UserToken", new Exception("AccessToken is empty"));

      var returnValueOAuth = await LiveMultimediaDL.GetMultimediaJobAttributesOneDrive(AccessToken, IdItem);

      if (returnValueOAuth <= 0) throw new ArgumentException("Error access to OneDrive", "UserToken", new Exception("MultimediaFileLength is less or equil zero"));
      MultimediaFileLength = returnValueOAuth.ToString();
      ServerSpeed = "32000"; //Get from Service config for OneDrive
      MimeType = await LiveMultimediaDL.SelectOAuthFilterAsync(IdTypeMultimediaSource);
      #endregion Read blob with attributes

      #region Fill List Attributes
      ListAttributes = new StringDictionary();

      ListAttributes.Add("MultimediaFileLength", MultimediaFileLength);
      ListAttributes.Add("ServerSpeed", ServerSpeed);
      ListAttributes.Add("MimeType", MimeType);
      ListAttributes.Add("AccessToken", AccessToken);
      #endregion Fill List Attributes

      return ListAttributes;
    }

    private async Task<StringDictionary> RemoteCreateMultimediaJobGoogleDrive(string UserToken, string IdItem, int IdTypeMultimediaSource)
    {
      #region Define vars
      string MultimediaFileLength, ServerSpeed, MimeType;
      StringDictionary ListAttributes;
      #endregion Define vars

      #region Read blob with attributes

      var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
      if (!JetSASLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("Error authentication to Google Drive", "UserToken", new Exception("AccessToken is empty"));

      var returnValueOAuth = await LiveMultimediaDL.GetMultimediaJobAttributesGoogleDrive(AccessToken, IdItem);

      if (returnValueOAuth <= 0) throw new ArgumentException("Error access to Google Drive", "UserToken", new Exception("MultimediaFileLength is less or equil zero"));
      MultimediaFileLength = returnValueOAuth.ToString();
      ServerSpeed = "32000"; //Get from Service config for Google Drive
      MimeType = await LiveMultimediaDL.SelectOAuthFilterAsync(IdTypeMultimediaSource);
      #endregion Read blob with attributes

      #region Fill List Attributes
      ListAttributes = new StringDictionary();

      ListAttributes.Add("MultimediaFileLength", MultimediaFileLength);
      ListAttributes.Add("ServerSpeed", ServerSpeed);
      ListAttributes.Add("MimeType", MimeType);
      ListAttributes.Add("AccessToken", AccessToken);
      #endregion Fill List Attributes

      return ListAttributes;
    }

    private async Task<StringDictionary> RemoteCreateMultimediaJobVKontakte(string UserToken, string IdItem, int IdTypeMultimediaSource)
    {
      #region Define vars
      string MultimediaFileLength, ServerSpeed, MimeType;
      StringDictionary ListAttributes;
      #endregion Define vars

      #region Read blob with attributes

      var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
      if (!JetSASLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("Error authentication to VKontakte", "UserToken", new Exception("AccessToken is empty"));

      var returnValueOAuth = await LiveMultimediaDL.GetMultimediaJobAttributesVKontakte(IdItem, IdTypeMultimediaSource, AccessToken);

      if (returnValueOAuth <= 0) throw new ArgumentException("Error access to VKontakte", "UserToken", new Exception("MultimediaFileLength is less or equil zero"));
      MultimediaFileLength = returnValueOAuth.ToString();
      ServerSpeed = "32000"; //Get from Service config for VKontakte
      MimeType = await LiveMultimediaDL.SelectOAuthFilterAsync(IdTypeMultimediaSource);
      #endregion Read blob with attributes

      #region Fill List Attributes
      ListAttributes = new StringDictionary();

      ListAttributes.Add("MultimediaFileLength", MultimediaFileLength);
      ListAttributes.Add("ServerSpeed", ServerSpeed);
      ListAttributes.Add("MimeType", MimeType);
      ListAttributes.Add("AccessToken", AccessToken);
      #endregion Fill List Attributes

      return ListAttributes;
    }

    private async Task<StringDictionary> RemoteCreateMultimediaJobDropbox(string UserToken, string IdItem, int IdTypeMultimediaSource)
    {
      #region Define vars
      string MultimediaFileLength, ServerSpeed, MimeType;
      StringDictionary ListAttributes;
      #endregion Define vars

      #region Read blob with attributes

      var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
      if (!JetSASLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("Error authentication to Dropbox", "UserToken", new Exception("AccessToken is empty"));

      var returnValueOAuth = await LiveMultimediaDL.GetMultimediaJobAttributesDropbox(IdItem, AccessToken);

      if (returnValueOAuth <= 0) throw new ArgumentException("Error access to Dropbox", "UserToken", new Exception("MultimediaFileLength is less or equil zero"));
      MultimediaFileLength = returnValueOAuth.ToString();
      ServerSpeed = "32000"; //Get from Service config for VKontakte
      MimeType = await LiveMultimediaDL.SelectOAuthFilterAsync(IdTypeMultimediaSource);
      #endregion Read blob with attributes

      #region Fill List Attributes
      ListAttributes = new StringDictionary();

      ListAttributes.Add("MultimediaFileLength", MultimediaFileLength);
      ListAttributes.Add("ServerSpeed", ServerSpeed);
      ListAttributes.Add("MimeType", MimeType);
      ListAttributes.Add("AccessToken", AccessToken);
      #endregion Fill List Attributes

      return ListAttributes;
    }

    private async Task<StringDictionary> RemoteCreateMultimediaJobYandexDisk(string UserToken, string IdItem, int IdTypeMultimediaSource)
    {
      #region Define vars
      string MultimediaFileLength, ServerSpeed, MimeType;
      StringDictionary ListAttributes;
      #endregion Define vars

      #region Read blob with attributes

      var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
      if (!JetSASLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("Error authentication to YandexDisk", "UserToken", new Exception("AccessToken is empty"));

      var returnValueOAuth = await LiveMultimediaDL.GetMultimediaJobAttributesYandexDisk(IdItem, AccessToken);

      if (returnValueOAuth <= 0) throw new ArgumentException("Error access to YandexDisk", "UserToken", new Exception("MultimediaFileLength is less or equil zero"));
      MultimediaFileLength = returnValueOAuth.ToString();
      ServerSpeed = "32000"; //Get from Service config for YandexDisk
      MimeType = await LiveMultimediaDL.SelectOAuthFilterAsync(IdTypeMultimediaSource);
      #endregion Read blob with attributes

      #region Fill List Attributes
      ListAttributes = new StringDictionary();

      ListAttributes.Add("MultimediaFileLength", MultimediaFileLength);
      ListAttributes.Add("ServerSpeed", ServerSpeed);
      ListAttributes.Add("MimeType", MimeType);
      ListAttributes.Add("AccessToken", AccessToken);
      #endregion Fill List Attributes

      return ListAttributes;
    }

    public async Task<Tuple<StringDictionary, string>> RemoteCreateMultimediaJob(string AccountKey, string Id, string UniqueMultimediaRequest)
    {
      #region Define vars
      string UserToken = "";
      string IdSource, IdItem;
      int IdTypeMultimediaSource;
      string IdJob, UserTokenClient;
      bool IsRequiredAuthorization;
      StringDictionary ListAttributes;
      Tuple<StringDictionary, string> returnValue;
      #endregion Define vars

      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);

        string originalid;
        #region Decrypt Id
        if (!CheckGoodString(Id)) throw new ArgumentException("Id is bad", "Id");
        DecryptId(Id, out originalid, out UserToken);
        #endregion Decrypt Id

        #region Decrypt Id of source
        if (!CheckGoodString(originalid)) throw new ArgumentException("Id is bad", "Id");
        DecryptSource(originalid, out IdSource, out IdItem);

        if (!int.TryParse(IdSource, out IdTypeMultimediaSource)) throw new ArgumentException("Id is bad", "IdSource");
        if (IdTypeMultimediaSource < 0) throw new ArgumentException("Id is bad", "IdSource");
        if (!CheckGoodString(IdItem)) throw new ArgumentException("Id is bad", "IdItem");
        #endregion Decrypt Id of source

        #region Get IsRequiredAuthorization for IdItem
        IsRequiredAuthorization = true; // Only NOW, after get it from provider
        #endregion Get IsRequiredAuthorization for IdItem

        #region Check UserToken
        if (CheckGoodString(UserToken))
        {
          CheckFormatGUID("UserToken", UserToken);
          await CheckUserToken("UserToken", UserToken, enumTypeUser.Remote);
        }
        else
        {
          if (IsRequiredAuthorization) throw new ArgumentException("UserToken is required", "UserToken");
        }
        #endregion Check UserToken

        //Установить проверку
        CheckGoodString("UniqueMultimediaRequest", UniqueMultimediaRequest);
        //CheckFormatGUID("UniqueMultimediaRequest", UniqueMultimediaRequest);
        #endregion Check incoming parameters

        #region Get Job Name
        var ListKeys = new StringCollection();
        ListKeys.Add(UniqueMultimediaRequest);
        ListKeys.Add(UserToken);
        IdJob = GetJobName(ListKeys);
        #endregion Get Job Name

        var IdParentTypeMultimediaSource = await GetIdParentTypeMultimediaSourceAsync(IdTypeMultimediaSource);
        UserTokenClient = "";

        #region Switch IdParentTypeMultimediaSource
        switch (IdParentTypeMultimediaSource)
        {
          case 10: //Favorites
            ListAttributes = new StringDictionary();
            break;
          case 11: //Home
            #region Get UserTokenClient
            UserTokenClient = await LiveMultimediaDL.GetUserTokenClient(UserToken);
            CheckGoodString("UserToken", UserTokenClient);
            CheckFormatGUID("UserToken", UserTokenClient);
            #endregion Get UserTokenClient         

            ListAttributes = await RemoteCreateMultimediaJobHome(IdJob,  IdItem, UserTokenClient);
            break;
          case 13: //Audiobooks
            ListAttributes = new StringDictionary();
            break;
          case 14: //News
            ListAttributes = await RemoteCreateMultimediaJobNews(IdItem);
            break;
          case 15: //Learning
            ListAttributes = new StringDictionary();
            break;
          case 19: //OneDrive
            ListAttributes = await RemoteCreateMultimediaJobOneDrive(UserToken, IdItem, IdTypeMultimediaSource);
            break;
          case 20: //Google Drive
            ListAttributes = await RemoteCreateMultimediaJobGoogleDrive(UserToken, IdItem, IdTypeMultimediaSource);
            break;
          case 21: //VKontakte
            ListAttributes = await RemoteCreateMultimediaJobVKontakte(UserToken, IdItem, IdTypeMultimediaSource);
            break;
          case 22: //iCloud
            ListAttributes = new StringDictionary();
            break;
          case 23: //Dropbox
            ListAttributes = await RemoteCreateMultimediaJobDropbox(UserToken, IdItem, IdTypeMultimediaSource);
            break;
          case 24: //YandexDisk
            ListAttributes = await RemoteCreateMultimediaJobYandexDisk(UserToken, IdItem, IdTypeMultimediaSource);
            break;
          default:
            throw new ArgumentException("Id is bad", "IdTypeMultimediaSource");
        }
        #endregion Switch IdParentTypeMultimediaSource

        ListAttributes.Add("IdJob", IdJob);

        #region Set hash fields for GetMultimedia
        var cache = CacheConnection.GetDatabase();
        var fieldNameCache = "field_" + IdJob;
        if (!await cache.KeyExistsAsync(fieldNameCache, CommandFlags.None))
        {
          var hashFields = new HashEntry[6];

          hashFields[0] = new HashEntry("UserToken", UserToken);
          hashFields[1] = new HashEntry("IdSource", IdSource);
          hashFields[2] = new HashEntry("IdItem", IdItem);
          hashFields[3] = new HashEntry("UserTokenClient", UserTokenClient);
          hashFields[4] = new HashEntry("IdItemLength", ListAttributes["MultimediaFileLength"]);
          hashFields[5] = new HashEntry("AccessToken", ListAttributes["AccessToken"]);

          await cache.HashSetAsync(fieldNameCache, hashFields, CommandFlags.None);          

          // Пока устаревание 1 сутки
          await cache.KeyExpireAsync(fieldNameCache, new TimeSpan(24, 0, 0));
        }
        #endregion Set hash fields for GetMultimedia        

        var ErrorMessage = "";
        returnValue = new Tuple<StringDictionary, string>(ListAttributes, ErrorMessage);
      }

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = new Tuple<StringDictionary, string>(new StringDictionary(), ex.Message);
        TraceLogError(UserToken, "RemoteCreateMultimediaJob with Parameter", "Exception=" + ex.ToString());
      }

      catch (Exception ex)
      {
        returnValue = new Tuple<StringDictionary, string>(new StringDictionary(), CollectErrorService(ex));
        TraceLogError(UserToken, "RemoteCreateMultimediaJob with Parameter", "AccountKey=" + (AccountKey ?? "") + ", Id=" + (Id ?? "") + ". Exception=" + ex.ToString());
      }
      #endregion Catch

      return returnValue;
    }

    #endregion Create multimedia job

    public async Task<string> RemoteCancelMultimediaJob(string AccountKey, string UserToken, string IdJob)
    {
      //Trace.TraceError("RemoteCancelMultimediaJob RETURN !!!!!");
      //return "";

      #region Define vars
      string UserTokenClient;
      string returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);

        CheckGoodString("UserToken", UserToken);
        CheckFormatGUID("UserToken", UserToken);

        CheckGoodString("IdJob", IdJob);

        //Trace.TraceError("RemoteCancelMultimediaJob. UserToken={0} IdJob={1}", UserToken, IdJob);
        //UserTokenClient = await LiveMultimediaDataLayer.LiveMultimediaDL.GetUserTokenClient(UserToken);
        //if (!CheckGoodString(UserTokenClient)) throw new ArgumentException("Live Multimedia Server is not available", "UserToken");
        //if (!CheckFormatGUID(UserTokenClient)) throw new ArgumentException("UserToken is bad", "UserToken");

        //var LMSDataLayer = new LiveMultimediaDataLayer.DataLayer();
        //UserTokenClient = "";
        //Parallel.For(0, 2, async i =>
        //{
        //  Trace.TraceError("RemoteCancelMultimediaJob. Parallel Start. i={0} IdJob={1}", i, IdJob);
        //  UserTokenClient = await LMSDataLayer.GetUserTokenClient(UserToken);
        //  Trace.TraceError("RemoteCancelMultimediaJob. Parallel Stop. i={0} IdJob={1}", i, IdJob);
        //}
        //);

        //UserTokenClient = await LMSDataLayer.GetUserTokenClient(UserToken);

        UserTokenClient = await LiveMultimediaDataLayer.LiveMultimediaDL.GetUserTokenClient(UserToken);

        // Переделать вывод исключений
        CheckGoodString("UserTokenClient", UserTokenClient);
        CheckFormatGUID("UserTokenClient", UserTokenClient);

        //if (!CheckGoodString(UserTokenClient) || !CheckFormatGUID(UserTokenClient))
        //{
        //  UserTokenClient = IdJob.Substring(IdJob.Length / 2);
        //}

        #endregion Check incoming parameters

        #region Queue message for cancel prepare buffers
        var queueMessage = Convert.ToInt32(enumActionMultimedia.StopTransfer).ToString() + "|" + IdJob;
        CloudQueueMessage message = new CloudQueueMessage(queueMessage);
        var queueClient = queueCloud.GetQueueReference(GetClientQueueName(UserTokenClient));
        await queueClient.AddMessageAsync(message);
        #endregion Queue message for cancel prepare buffers

        ////Delay for working of Live Multimedia Server
        //await Task.Delay(5000);

        //var DebugWriteElapsed = new DebugWriteElapsed(IsDebugWrite);

        #region Delete unwanted blobs

        //// Work with blobs
        //var ListBlobs = containerCloud.ListBlobs(IdJob, true, BlobListingDetails.None);
        //var CountListBlobs = ListBlobs.Count();

        //await Task.Run(async () =>
        //{
        //  foreach (var blobItem in ListBlobs)
        //  {
        //    try
        //    {
        //      await ((CloudBlockBlob)blobItem).DeleteIfExistsAsync();
        //    }
        //    catch (Exception)
        //    {
        //    }
        //  }
        //  //Parallel.ForEach(ListBlobs, async blobItem =>
        //  //{
        //  //  await ((CloudBlockBlob)blobItem).DeleteIfExistsAsync();
        //  //}
        //  //);
        //}
        //);
        #endregion Delete unwanted blobs

        //DebugWriteElapsed.WriteLine("RemoteCancelMultimediaJob: CountListBlobs={0}, IdJob={1}", CountListBlobs, IdJob);

        var ErrorMessage = "";
        returnValue = ErrorMessage;
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = ex.Message;
        TraceLogError(UserToken, "RemoteCancelMultimediaJob with Parameter", "Exception=" + ex.ToString());
      }

      catch (Exception ex)
      {
        returnValue = CollectErrorService(ex);
        TraceLogError(UserToken, "RemoteCancelMultimediaJob with Parameter", "AccountKey=" + (AccountKey ?? "") + ", IdJob=" + (IdJob ?? "") + ". Exception=" + ex.ToString());
      }
      #endregion Catch

      return returnValue;
    }

    #endregion Home Albums

    #region Playlist
    public async Task<string> RemotePlaylistSave(string UserToken, string Playlist)
    {
      string ErrorMessage = "";

      try
      {
        await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Remote);

        if (!string.IsNullOrEmpty(Playlist) && !string.IsNullOrWhiteSpace(Playlist))
        {
          bool IsSuccess = await LiveMultimediaDataLayer.LiveMultimediaDL.PlaylistSave(UserToken, Playlist);
          if (IsSuccess)
            ErrorMessage = "";
          else
          {
#if (DEBUG)
            ErrorMessage = "LiveMultimediaDataLayer.LiveMultimediaDL.PlaylistSave. IsSuccess=false. UserToken=" + UserToken + ", Playlist=" + Playlist;
#else
            ErrorMessage = "Error of saving a playlist";
#endif
          }
        }
        else
          ErrorMessage = "Playlist can't be empty";

      }
      catch (Exception ex)
      {
#if (DEBUG)
        ErrorMessage = ex.ToString();
#else
     ErrorMessage = ex.Message;
#endif
        TraceLogError(UserToken, "RemotePlaylistSave", "Exception=" + ex.ToString());
      }

      return ErrorMessage;
    }

    public async Task<PlaylistObject[]> RemotePlaylistLoad(string UserToken)
    {
      DataTable tablePlaylist;
      PlaylistObject[] ListPlaylist;

      try
      {
        await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Remote);

        tablePlaylist = await LiveMultimediaDataLayer.LiveMultimediaDL.PlaylistLoad(UserToken);

        ListPlaylist = new PlaylistObject[tablePlaylist.Rows.Count];

        int i = 0;
        PlaylistObject Playlist;

        foreach (DataRow RowPlaylist in tablePlaylist.Rows)
        {
          Playlist = new PlaylistObject();
          Playlist.IdPlaylist = Convert.ToInt64(RowPlaylist["IdPlaylist"]);
          Playlist.Playlist = RowPlaylist["Playlist"].ToString();
          ListPlaylist[i] = Playlist;
          i++;
        }
      }
      catch (Exception ex)
      {
        ListPlaylist = null;
        TraceLogError(UserToken, "RemotePlaylistLoad", "Exception=" + ex.ToString());
      }

      return ListPlaylist;
    }

    public async Task<bool> RemotePlaylistDelete(string UserToken, Int64 IdPlaylist)
    {
      bool IsSuccess;

      try
      {
        await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Remote);

        IsSuccess = await LiveMultimediaDataLayer.LiveMultimediaDL.PlaylistDelete(UserToken, IdPlaylist);
      }
      catch (Exception ex)
      {
        IsSuccess = false;
        TraceLogError(UserToken, "RemotePlaylistDelete", "IdPlaylist=" + IdPlaylist.ToString() + ", Exception=" + ex.ToString());
      }

      return IsSuccess;
    }

    public async Task<bool> RemotePlaylistItemSave(string UserToken, Int64 IdPlaylist, int IdTypeMultimediaSource, string IdMultimediaItem, string MultimediaItem)
    {
      bool IsSuccess;

      try
      {
        await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Remote);
        CheckGoodString("IdMultimediaItem", IdMultimediaItem);

        IsSuccess = await LiveMultimediaDataLayer.LiveMultimediaDL.PlaylistItemSave(UserToken, IdPlaylist, IdTypeMultimediaSource, IdMultimediaItem, MultimediaItem);
      }
      catch (Exception ex)
      {
        IsSuccess = false;
        TraceLogError(UserToken, "RemotePlaylistItemSave", "IdPlaylist=" + IdPlaylist.ToString() + ", IdTypeMultimediaSource=" + IdTypeMultimediaSource + ", IdMultimediaItem=" + IdMultimediaItem + ", MultimediaItem=" + MultimediaItem + ", Exception=" + ex.ToString());
      }

      return IsSuccess;
    }

    public async Task<bool> RemotePlaylistItemRemove(string UserToken, Int64 IdPlaylistItem)
    {
      bool IsSuccess;

      try
      {
        await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Remote);

        IsSuccess = await LiveMultimediaDataLayer.LiveMultimediaDL.PlaylistItemRemove(UserToken, IdPlaylistItem);

      }
      catch (Exception ex)
      {
        IsSuccess = false;
        TraceLogError(UserToken, "RemotePlaylistItemRemove", "IdPlaylistItem=" + IdPlaylistItem.ToString() + ", Exception=" + ex.ToString());
      }

      return IsSuccess;
    }

    #endregion Playlist

    #region OAuth Token
    
    public async Task<bool> OAuthSetToken(string AccountKey, string UserToken, string Id, OAuthToken OAuthUserToken)
    {
      #region Define vars
      bool IsSuccess;
      string IdSource, IdItem;
      int IdTypeMultimediaSource;
      #endregion Define vars

      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);

        #region Decrypt Id of source
        JetSASLibrary.CheckGoodString("Id", Id);
        DecryptSource(Id, out IdSource, out IdItem);

        if (!JetSASLibrary.CheckGoodString(IdSource)) throw new ArgumentException("Id is bad", "Id", new Exception("IdSource is empty"));
        if (!int.TryParse(IdSource, out IdTypeMultimediaSource)) throw new ArgumentException("Id is bad", "Id", new Exception("Error convert IdSource to int: IdSource=" + IdSource));
        if (IdTypeMultimediaSource < 0) throw new ArgumentException("Id is bad", "Id", new Exception("IdTypeMultimediaSource less zero: IdTypeMultimediaSource=" + IdSource));
        #endregion Decrypt Id of source

        #region Check tokens
        if (!JetSASLibrary.CheckGoodString(OAuthUserToken.AccessToken) && !JetSASLibrary.CheckGoodString(OAuthUserToken.RefreshToken)) throw new ArgumentException("AccessToken and RefreshToken is empty", "Token");
        if (!JetSASLibrary.CheckGoodString(OAuthUserToken.RefreshToken))
        {
          OAuthUserToken.RefreshToken = null;
        }
        #endregion Check tokens

        await FullCheckUserToken("UserToken", UserToken, enumTypeUser.Remote);
        #endregion Check incoming parameters

        if (JetSASLibrary.CheckGoodString(OAuthUserToken.AccessToken))
        {
          if (OAuthUserToken.ExpiresIn == DateTime.MinValue.ToString())
          {
            OAuthUserToken.RefreshToken = OAuthUserToken.AccessToken;
          }
          else
          {
            var TokenExpiresIn = Convert.ToDateTime(OAuthUserToken.ExpiresIn);
            var DeltaTime = TokenExpiresIn.Subtract(DateTime.Now);            
            if (DeltaTime.TotalDays>=30) //Устанавливаю, что если AccessToken действует не менее 30 дней, то считать его RefreshToken
              OAuthUserToken.RefreshToken = OAuthUserToken.AccessToken;
          }
        }

        //1 Добавить время формирования токен отсюда и убрать время формирования  в хранимой DateAccessToken, DateRefreshToken
        //2 Сделать, что если какой-токен токен пустой, то его не обновлять и время не менять
        //3 Высчитывать или формировать поле устаревания для RefreshToken, если такое допустимо.
        //4 Добавить поле устаревания для RefreshToken в класс
        //*****

        IsSuccess = await LiveMultimediaDL.OAuthSetToken(UserToken, IdTypeMultimediaSource, OAuthUserToken);
      }
      catch (ArgumentException ex)
      {
        IsSuccess = false;
        TraceLogError(UserToken, "OAuthSetToken with Parameter", ex.ToString() + ", IdTypeMultimediaSource=" + Id.ToString());
      }
      catch (Exception ex)
      {
        IsSuccess = false;
        TraceLogError(UserToken, "OAuthSetToken", ex.ToString() + ", IdTypeMultimediaSource=" + Id.ToString());
      }

      return IsSuccess;
    }

    private async Task<bool> OAuthCheckTokenAsync(string UserToken, int IdTypeMultimediaSource)
    {
      #region Define vars
      bool IsSuccess;
      #endregion Define vars

      #region Try
      try
      {
        var token = await OAuthGetTokenAsync(UserToken, IdTypeMultimediaSource);

        IsSuccess = OAuthValidateAccessToken(token);
        if (!IsSuccess)
        {
          IsSuccess = OAuthValidateRefreshToken(token);
        }        
      }
      #endregion Try

      #region Catch
      catch (Exception)
      {
        IsSuccess = false;
      }
      #endregion Catch

      return IsSuccess;
    }

    //На будущее
    //private bool OAuthValidateToken(string TokenValue, string TokenExpire)
    //{
    //  #region Define vars
    //  bool IsSuccess;
    //  #endregion Define vars

    //  if (JetSASLibrary.CheckGoodString(TokenValue))
    //  {
    //    var TokenExpiresIn = Convert.ToDateTime(TokenExpire);
    //    if (TokenExpiresIn != DateTime.MinValue)
    //    {
    //      var TokenCompare = DateTime.Compare(TokenExpiresIn, DateTime.Now);

    //      if (TokenCompare >= 0)
    //        IsSuccess = true;
    //      else
    //        IsSuccess = false;
    //    }
    //    else
    //    {
    //      IsSuccess = true;
    //    }
    //  }
    //  else
    //    IsSuccess = false;

    //  return IsSuccess;
    //}

    private bool OAuthValidateAccessToken(OAuthToken token)
    {
      #region Define vars
      bool IsSuccess;
      #endregion Define vars

      if (token != null)
      {
        if (JetSASLibrary.CheckGoodString(token.AccessToken))
        {
          var TokenExpiresIn = Convert.ToDateTime(token.ExpiresIn);
          if (TokenExpiresIn != DateTime.MinValue)
          {
            var TokenCompare = DateTime.Compare(TokenExpiresIn, DateTime.Now);

            if (TokenCompare >= 0)
              IsSuccess = true;
            else
              IsSuccess = false;
          }
          else
          {
            IsSuccess = true;
          }
        }
        else
          IsSuccess = false;
      }
      else
        IsSuccess = false;

      return IsSuccess;
    }

    private bool OAuthValidateRefreshToken(OAuthToken token)
    {
      #region Define vars
      bool IsSuccess;
      #endregion Define vars

      if (JetSASLibrary.CheckGoodString(token.RefreshToken))
      {
          IsSuccess = true;
      }
      else
      {
        IsSuccess = false;
      }

      return IsSuccess;
    }

    private async Task<OAuthToken> OAuthGetTokenAsync(string UserToken, int IdTypeMultimediaSource)
    {
      #region Define vars
      OAuthToken token;
      #endregion Define vars

      #region Try
      try
      {
        var TableToken = await LiveMultimediaDL.OAuthGetTokenAsync(UserToken, IdTypeMultimediaSource);
        if (TableToken.Rows.Count > 0)
        {
          token = new OAuthToken();

          token.AccessToken = TableToken.Rows[0].Field<string>("AccessToken");
          if (LiveMultimediaLibrary.CheckGoodString(token.AccessToken))
          {
            token.ExpiresIn = TableToken.Rows[0].Field<DateTime>("AccessTokenExpires").ToString();
          }
          else
          {
            token.ExpiresIn = DateTime.MinValue.ToString();
          }
          token.RefreshToken = TableToken.Rows[0].Field<string>("RefreshToken");
        }
        else
        {
          throw new ArgumentException("OAuthGetTokenAsync: LiveMultimediaDL.OAuthGetTokenAsync return zero rows count. UserToken=" + UserToken + ",IdTypeMultimediaSource" + IdTypeMultimediaSource.ToString());
        }
      }
      #endregion Try

      #region Catch
      catch (ArgumentException)
      {
        Trace.TraceError("OAuthGetTokenAsync with Parameter: UserToken={0} IdTypeMultimediaSource={1}", (UserToken ?? ""), IdTypeMultimediaSource);

        token = null;
      }
      catch (Exception ex)
      {
        var ErrorMessage = CollectErrorService(string.Format("OAuthGetTokenAsync with Parameter: UserToken={0} IdTypeMultimediaSource={1}", (UserToken ?? ""), IdTypeMultimediaSource, ex.ToString()), ex);

        token = null;
      }
      #endregion Catch

      return token;
    }

    private async Task<string> OAuthRenewAccessToken(string UserToken, int IdTypeMultimediaSource)
    {
      #region Define vars
      string AccessToken;
      string OAuthUrlRefreshToken;
      int IdTypeMultimediaOAuth;
      #endregion Define vars

      #region Try
      try
      {
        var token = await OAuthGetTokenAsync(UserToken, IdTypeMultimediaSource);

        if (OAuthValidateAccessToken(token))
        {
          AccessToken = token.AccessToken;
        }
        else
        {
          if (!JetSASLibrary.CheckGoodString(token.RefreshToken)) throw new ArgumentException("Error OAuth", "Id", new Exception("OAuthGetAccessToken: RefreshToken is empty. UserToken=" + UserToken + ",IdTypeMultimediaSource=" + IdTypeMultimediaSource.ToString()));

          #region Get OAuth URL
          var TypeMultimediaOAuth = await LiveMultimediaDL.OAuthGetTypeMultimediaAsync(IdTypeMultimediaSource);
          if (TypeMultimediaOAuth.Rows.Count > 0)
          {
            OAuthUrlRefreshToken = TypeMultimediaOAuth.Rows[0].Field<string>("OAuthUrlRefreshToken");
            IdTypeMultimediaOAuth = TypeMultimediaOAuth.Rows[0].Field<int>("IdTypeMultimediaOAuth");
          }
          else
          {
            throw new ArgumentException("Error OAuth", "Id", new Exception("LiveMultimediaDL.OAuthGetTypeMultimediaAsync rows count is zero: IdSourceInt=" + IdTypeMultimediaSource.ToString()));
          }
          #endregion Get OAuth URL

          AccessToken = await OAuthRefreshAccessToken(UserToken, token.RefreshToken, IdTypeMultimediaSource, IdTypeMultimediaOAuth, OAuthUrlRefreshToken);
        }
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        AccessToken = null;
      }
      #endregion Catch

      return AccessToken;
    }

    private async Task<string> OAuthRefreshAccessToken(string UserToken, string RefreshToken, int IdTypeMultimediaSource, int IdTypeMultimediaOAuth, string OAuthUrlRefreshToken)
    {
      #region Define vars
      OAuthToken token;
      bool IsSuccess;
      string AccessToken;
      #endregion Define vars

      try
      {
        if (IdTypeMultimediaOAuth != 3 && IdTypeMultimediaOAuth!= 4 && IdTypeMultimediaOAuth != 5) //VKontakte, Dropbox,YandexDisk - Исправить на нормальную проверку, без привязки к номеру.
        {
          var aUrl = OAuthUrlRefreshToken.Split(new char[] { '?' });

          var OAuthUrl = aUrl[0];
          var postContent = aUrl[1] + "&refresh_token=" + RefreshToken;

          var request = WebRequest.Create(OAuthUrl) as HttpWebRequest;
          request.Method = "POST";
          //request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
          request.ContentType = "application/x-www-form-urlencoded";

          var StartExpires = DateTime.Now;

          using (var writer = new StreamWriter(request.GetRequestStream()))
          {
            writer.Write(postContent);
          }

          var response = await request.GetResponseAsync() as HttpWebResponse;
          if (response != null)
          {
            var serializer = new DataContractJsonSerializer(typeof(OAuthToken));
            token = serializer.ReadObject(response.GetResponseStream()) as OAuthToken;
            if (token != null)
            {
              if (token.ExpiresIn == "0" || string.IsNullOrEmpty(token.ExpiresIn))
              {
                token.ExpiresIn = DateTime.MinValue.ToString();
              }
              else
              {                
                DateTime Expires;
                Expires = StartExpires.AddSeconds(Convert.ToDouble(token.ExpiresIn));
                token.ExpiresIn = Expires.ToString();
              }
              IsSuccess = await LiveMultimediaDL.OAuthSetToken(UserToken, IdTypeMultimediaSource, token);
              if (IsSuccess)
                AccessToken = token.AccessToken;
              else
                AccessToken = null;
            }
            else
            {
              AccessToken = null;
            }
          }
          else
          {
            AccessToken = null;
          }
        }
        else //VKontakte, Dropbox, YandexDisk
        {
          token = new OAuthToken();
          token.RefreshToken = RefreshToken;
          token.AccessToken = token.RefreshToken;
          token.ExpiresIn = DateTime.MinValue.ToString();
          IsSuccess = await LiveMultimediaDL.OAuthSetToken(UserToken, IdTypeMultimediaSource, token);
          if (IsSuccess)
            AccessToken = token.AccessToken;
          else
            AccessToken = null;
        }
      }
      catch (ArgumentException ex)
      {
        AccessToken = null;
      }
      catch (Exception ex)
      {
        AccessToken = null;
      }

      return AccessToken;
    }

    #endregion OAuth Token

    /*
     * http://msdn.microsoft.com/en-us/library/ff512435.aspx
     * 
     */

    /// <summary>
    /// Get news from URL by urlNews
    /// </summary>
    /// <param name="urlNews"></param>
    /// <returns></returns>
    private static async Task<string> GetTextNewsFromURL(string urlNews, string LanguageISO_639_3)
    {
      #region Define vars
      string returnValue;
      #endregion Define vars

      #region Try
      try
      {
        //AlchemyAPI
        string AlchemyAPIKey = "1b3711f22d37af04c8c436c168f118365d3716d2";
        string AlchemyAPI = "http://access.alchemyapi.com/calls/url/URLGetText?";

        ////Reader
        //string ReaderAPIUrl = "https://www.readability.com/api/content/v1/parser?";
        //string ReaderAPIKey = "9fcd6c64222eee670556ebdcb544db1c2d5ea8a2";
        //string ReaderURI;

        ////Mercury Web Parser
        //string MercuryWebParserAPI = "https://mercury.postlight.com/parser?";
        //string MercuryWebParserAPIKey = "HGrDt1m357XlO92O5go0kAXsuopmE2NzWMUPpoDN";

        #region Read news
        try
        {
          #region AlchemyAPI
          //AlchemyAPI
          //http://www.ibm.com/watson/developercloud/alchemy-language/api/v1/#text_cleaned
          var client = new HttpClient();

          var query = string.Format("apikey={0}&url={1}&outputMode={2}&useMetadata={3}&sourceText={4}&language={5}",
            AlchemyAPIKey, urlNews, "json", "0", "cleaned", LanguageISO_639_3);
          var uri = AlchemyAPI + query;

          var response = await client.GetAsync(uri);

          var stoken = await response.Content.ReadAsStringAsync();
          var jss = new JavaScriptSerializer();
          var d = jss.Deserialize<Dictionary<string, string>>(stoken);
          returnValue = HttpUtility.HtmlDecode((d["text"] as string).Trim());
          #endregion AlchemyAPI

          #region Reader
          ////Reader
          ////content = String.Format("url={0}&token={1}&max_pages={2}", HttpUtility.UrlEncode(urlNews), ReaderAPIKey, 25);
          //ReaderURI = ReaderAPIUrl + "token=" + ReaderAPIKey + "&url=" + HttpUtility.UrlEncode(urlNews);
          //var request = WebRequest.Create(ReaderURI) as HttpWebRequest;
          //request.Method = "GET";
          //request.ContentType = "application/x-www-form-urlencoded";
          #endregion Reader

          #region Mercury Web Parser
          //// Mercury Web Parser
          ////https://mercury.postlight.com/web-parser/

          //var query = string.Format("url={0}", urlNews);
          //var uri = MercuryWebParserAPI + query;

          //var request = WebRequest.Create(uri) as HttpWebRequest;
          //request.Method = "GET";
          //request.ContentType = "application/json";
          //request.Headers.Add("x-api-key", MercuryWebParserAPIKey);

          //var response = await request.GetResponseAsync() as HttpWebResponse;
          //string stoken;
          //using (var stream = response.GetResponseStream())
          //{
          //  var encode = Encoding.GetEncoding("utf-8");
          //  using (var readStream = new StreamReader(stream, encode))
          //  {
          //    stoken = await readStream.ReadToEndAsync();
          //  }
          //}
          //var tokenData = new Dictionary<string, string>();
          //tokenData = deserializeJson(stoken);
          //returnValue = HttpUtility.HtmlDecode((tokenData["content"] as string).Trim());
          #endregion Mercury Web Parser

          //var response = await request.GetResponseAsync() as HttpWebResponse;
          //string stoken;
          //using (var stream = response.GetResponseStream())
          //{
          //  var encode = Encoding.GetEncoding("utf-8");
          //  using (var readStream = new StreamReader(stream, encode))
          //  {
          //    stoken = await readStream.ReadToEndAsync();
          //  }
          //}
          //var tokenData = new Dictionary<string, string>();
          //tokenData = deserializeJson(stoken);

          ////AlchemyAPI
          //returnValue = (tokenData["text"] as string).Trim();

          ////Reader
          //returnValue = HttpUtility.HtmlDecode(tokenData["content"]);

          var sb = new StringBuilder();
          string[] parts = returnValue.Split(new char[] { ' ', '\n', '\t', '\r', '\f', '\v', '\\', '\"', '©', '→', '«', '»' }, StringSplitOptions.RemoveEmptyEntries);
          int size = parts.Length;
          for (int i = 0; i < size; i++)
          {
            sb.AppendFormat("{0} ", parts[i]);
          }

          returnValue = sb.ToString().Replace("...", ".").Replace(" ,", ",");

          //Remove tegs < >
          var firstIn = returnValue.IndexOf("<"); int NextIn;
          while (firstIn >= 0)
          {
            NextIn = returnValue.IndexOf(">");
            if (NextIn > 0)
            {
              returnValue = returnValue.Remove(firstIn, NextIn - firstIn + 1).TrimStart();
              firstIn = returnValue.IndexOf("<");
            }
            else
            {
              firstIn = returnValue.IndexOf("<", firstIn+1);
            }
          }

          //Remove ASCII chars &....;
          firstIn = returnValue.IndexOf("&");
          while (firstIn >= 0)
          {
            NextIn = returnValue.IndexOf(";");
            if (NextIn > 0)
            {
              returnValue = returnValue.Remove(firstIn, NextIn - firstIn + 1).TrimStart();
              firstIn = returnValue.IndexOf("<");
            }
            else
              firstIn = -1;
          }
        }
        catch (WebException e)
        {
          returnValue = null; 
          var response = e.Response as HttpWebResponse;
          if (response != null)
          {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(OAuthError));
            //error = serializer.ReadObject(response.GetResponseStream()) as OAuthError;
          }
        }
        catch (IOException)
        {
          returnValue = null;
        }
        catch (Exception ex)
        {
          returnValue = null;
        }
        #endregion Read news
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = null;
        //TraceLogError("", "GetTextNewsFromURL with Parameter", ex.ToString() + ", urlNews=" + (urlNews ?? ""));
      }

      catch (Exception ex)
      {
        returnValue = null;
        //TraceLogError("", "GetTextNewsFromURL", ex.ToString() + ", urlNews=" + (urlNews ?? ""));
      }
      #endregion Catch

      return returnValue;
    }

    private async Task<string> TranslateNews(string text, string LanguageFromTranslate, string LanguageToTranslate)
    {
      #region Define vars
      string returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Prepare translation

        var admAuth = new AdmAuthentication(UserData.clientID, UserData.clientSecret);
        var admToken = await admAuth.GetAccessTokenAsync();
        var tokenReceived = DateTime.Now;
        var headerValue = "Bearer " + admToken.access_token;

        var ClientTranslator = new TranslatorService.LanguageServiceClient();

        //TranslatorService.TranslateOptions TranslateOptions = new TranslatorService.TranslateOptions();
        //TranslateOptions.Category = "general";
        //TranslateOptions.ContentType = "text/plain";

        //Set Authorization header before sending the request
        var httpRequestProperty = new HttpRequestMessageProperty();
        httpRequestProperty.Method = "POST";
        httpRequestProperty.Headers.Add("Authorization", headerValue);

        #endregion Prepare translation
       
        #region For TEMP LIMIT length of source
        int MaxCountSourceText = 1000;
        if (text.Length > MaxCountSourceText) text.Substring(0, MaxCountSourceText);
        #endregion For TEMP LIMIT length of source

        int MaxCountTranslateText = 10000;

        #region Translate
        try
        {
          using (var scope = new OperationContextScope(ClientTranslator.InnerChannel))
          {
            System.ServiceModel.OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

            //Разбиваем текст для перевода на куски заданной длины
            var ListSourceText = new List<string>();
            while (text.Length > MaxCountTranslateText)
            {
              ListSourceText.Add(text.Substring(0, MaxCountTranslateText));
              text = text.Substring(MaxCountTranslateText);
            }
            ListSourceText.Add(text);

            var aTranslate = await ClientTranslator.TranslateArrayAsync("", ListSourceText.ToArray(), LanguageFromTranslate, LanguageToTranslate, null/*TranslateOptions*/);

            returnValue = "";
            foreach (var itemTranslate in aTranslate)
            {
              returnValue += itemTranslate.TranslatedText;
            }
          }
        }
        catch (Exception ex)
        {
          returnValue = "";
        }
        #endregion Translate
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = "";
        TraceLogError("", "TranslateNews with Parameter", ex.ToString() + ", LanguageFromTranslate=" + (LanguageFromTranslate ?? "") + ", LanguageToTranslate=" + (LanguageToTranslate ?? ""));
      }

      catch (Exception ex)
      {
        returnValue = "";
        TraceLogError("", "TranslateNews", ex.ToString() + ", LanguageFromTranslate=" + (LanguageFromTranslate ?? "") + ", LanguageToTranslate=" + (LanguageToTranslate ?? ""));
      }
      #endregion Catch

      return returnValue;
    }

    private static async Task<string[]> SpeakNews(string text, string LanguageSpeak)
    {
      List<string> ListSpeakURL = new List<string>();

      #region Try
      try
      {
        #region Prepare translation

        var admAuth = new AdmAuthentication(UserData.clientID, UserData.clientSecret);
        var admToken = await admAuth.GetAccessTokenAsync();
        var headerValue = "Bearer " + admToken.access_token;

        var ClientTranslator = new TranslatorService.LanguageServiceClient();

        //Set Authorization header before sending the request
        var httpRequestProperty = new HttpRequestMessageProperty();
        httpRequestProperty.Method = "POST";
        httpRequestProperty.Headers.Add("Authorization", headerValue);

        #endregion Prepare translation

        int MaxCountSpeakText = 2000;

        #region For TEMP LIMIT length of source
        MaxCountSpeakText = 2000;
        if (text.Length > MaxCountSpeakText) text = text.Substring(0, MaxCountSpeakText);
        #endregion For TEMP LIMIT length of source        

        #region Разбиваем текст для речи на куски заданной длины
        var ListTranslationResult = new List<string>();
        while (text.Length > MaxCountSpeakText)
        {
          ListTranslationResult.Add(text.Substring(0, MaxCountSpeakText));
          text = text.Substring(MaxCountSpeakText);
        }
        ListTranslationResult.Add(text);
        #endregion Разбиваем текст для речи на куски заданной длины

        ListSpeakURL = new List<string>();
        string speakurl;
        foreach (var itemTranslationresult in ListTranslationResult)
        {
          speakurl = await ClientTranslator.SpeakAsync(headerValue, itemTranslationresult, LanguageSpeak, "audio/mp3", "MinSize"); //MinSize, MaxQuality
          ListSpeakURL.Add(speakurl);
        }
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        ListSpeakURL = new List<string>();
        //TraceLogError("", "SpeakNews with Parameter", ex.ToString() + ", LanguageSpeak=" + (LanguageSpeak ?? ""));
      }

      catch (Exception ex)
      {
        ListSpeakURL = new List<string>();
        //TraceLogError("", "SpeakNews", ex.ToString() + ", LanguageSpeak=" + (LanguageSpeak ?? ""));
      }
      #endregion Catch

      return ListSpeakURL.ToArray();
    }

    #region Localization

    /// <summary>
    /// Return list of languages available for translate
    /// </summary>
    /// <param name="Language">If "Language" is not null, then return list of native languages</param>
    /// <returns>Return list of languages available for translate</returns>
    public async Task<Tuple<LanguageInfo[], string>> GetLanguages(string Language=null)
    {
      #region Define vars
      Tuple<LanguageInfo[], string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        using (var StorageInterfaces = new StorageInterfacesClient())
        {
          var returnLanguages = await StorageInterfaces.GetLanguagesAsync(Language);
          if (JetSASLibrary.CheckGoodString(returnLanguages.Item2)) throw new AggregateException("StorageInterfaces.GetLanguagesAsync. Exception: " + returnLanguages.Item2);

          var ErrorMessage = "";
          returnValue = new Tuple<LanguageInfo[], string>(returnLanguages.Item1, ErrorMessage);
        }
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        var ErrorMessage = CollectErrorService(string.Format("GetLanguages: Language={0}", Language), ex);
        returnValue = new Tuple<LanguageInfo[], string>(new LanguageInfo[0], ErrorMessage);
      }
      #endregion Catch

      return returnValue;
    }

    /// <summary>
    /// Return localization for Live Multimedia Server
    /// </summary>
    /// <param name="AccountKey">Secure account key for application</param>
    /// <param name="Language">Language of localization</param>
    /// <returns>Returns native name of language and array of string pairs "Key"-"Value"</returns>
    public async Task<Tuple<LocalizationElement[], string, string>> LocalGetLocalization(string AccountKey, string Language)
    {
      #region Define vars      
      Tuple<LocalizationElement[], string, string> returnValue;
      string ErrorMessage;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Local);
        CheckGoodString("Language", Language);
        #endregion Check incoming parameters

        var returnLocalization = await GetLocalizationAsync(Language, "Local");
        var LocalizationDictionary = returnLocalization.Item1.ToArray();
        var NativeName = returnLocalization.Item2;

        ErrorMessage = "";
        returnValue = new Tuple<LocalizationElement[], string, string>(LocalizationDictionary, NativeName, ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        ErrorMessage = ex.Message;
        returnValue = new Tuple<LocalizationElement[], string, string>(new LocalizationElement[0], "", ErrorMessage);
        Trace.TraceError("LocalGetLocalization with Parameter. Language={0} Exception={1}", Language, ex.ToString());
      }
      catch (Exception ex)
      {
        ErrorMessage = CollectErrorService(string.Format("LocalGetLocalization: Language={0}", Language), ex);
        returnValue = new Tuple<LocalizationElement[], string, string>(new LocalizationElement[0], "", ErrorMessage);
      }
      #endregion Catch

      return returnValue;
    }

    /// <summary>
    /// Return localization for Live Multimedia Market
    /// </summary>
    /// <param name="AccountKey">Secure account key for application</param>
    /// <param name="Language">Language of localization</param>
    /// <returns>Returns native name of language and array of string pairs "Key"-"Value"</returns>
    public async Task<Tuple<LocalizationElement[], string, string>> RemoteGetLocalization(string AccountKey, string Language)
    {
      #region Define vars
      Tuple<LocalizationElement[], string, string> returnValue;
      string ErrorMessage;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);
        CheckGoodString("Language", Language);
        #endregion Check incoming parameters

        var returnLocalization = await GetLocalizationAsync(Language, "Remote");
        var LocalizationDictionary = returnLocalization.Item1.ToArray();
        var NativeName = returnLocalization.Item2;

        ErrorMessage = "";
        returnValue = new Tuple<LocalizationElement[], string, string>(LocalizationDictionary, NativeName, ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        ErrorMessage = ex.Message;
        returnValue = new Tuple<LocalizationElement[], string, string>(new LocalizationElement[0], "", ErrorMessage);
        TraceService("LocalGetLocalization with Parameter", enumTypeLog.Error, ", Language=" + (Language ?? "") + ", Exception=" + ex.ToString());
      }
      catch (Exception ex)
      {
        ErrorMessage = CollectErrorService(string.Format("RemoteGetLocalization: Language={0}", Language), ex);
        returnValue = new Tuple<LocalizationElement[], string, string>(new LocalizationElement[0], "", ErrorMessage);
      }
      #endregion Catch

      return returnValue;
    }

    private async Task<Tuple<List<LocalizationElement>, string>> GetLocalizationAsync(string Language, string Topic)
    {
      #region Define vars
      Tuple<List<LocalizationElement>, string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        using (var StorageInterfaces = new StorageInterfacesClient())
        {
          var returnLocalization = await StorageInterfaces.GetLocalizationAsync(LocalizationAccountKey, Language, DefaultLanguage, Project, Topic);
          if (JetSASLibrary.CheckGoodString(returnLocalization.Item3)) throw new AggregateException("StorageInterfaces.GetLocalizationAsync. Exception: " + returnLocalization.Item2);
          var ListLocalizationDictionary = returnLocalization.Item1.ToList();
          var NativeName = returnLocalization.Item2;
          returnValue = new Tuple<List<LocalizationElement>, string>(ListLocalizationDictionary, NativeName);
        }
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("GetLocalizationAsync: Language={0} Topic={1}", Language, Topic), ex);
      }
      #endregion Catch

      return returnValue;
    }

    private async Task<Tuple<List<LocalizationElement>, string>> GetLocalizationByListAsync(string Language, string Topic, List<LocalizationElement> ListDefaultElements)
    {
      #region Define vars
      List<LocalizationElement> ListLocalizationDictionary;
      Tuple<List<LocalizationElement>, string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        using (var StorageInterfaces = new StorageInterfacesClient())
        {
          var returnLocalization = await StorageInterfaces.GetLocalizationByListAsync(LocalizationAccountKey, Language, DefaultLanguage, Project, Topic, ListDefaultElements.ToArray());
          
          if (JetSASLibrary.CheckGoodString(returnLocalization.Item3)) throw new ArgumentException(returnLocalization.Item3);
          ListLocalizationDictionary = returnLocalization.Item1.ToList();
        }

        var ErrorMessage = "";
        returnValue = new Tuple<List<LocalizationElement>, string>(ListLocalizationDictionary, ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = new Tuple<List<LocalizationElement>, string>(new List<LocalizationElement>(), ex.Message);
        TraceLogError("", "GetLocalizationByListAsync with Parameter", "Language=" + (Language ?? "") + ", Topic=" + Topic + ", Exception=" + ex.ToString());
      }

      catch (Exception ex)
      {
        returnValue = new Tuple<List<LocalizationElement>, string>(new List<LocalizationElement>(), CollectErrorService(ex));
        TraceLogError("", "GetLocalizationByListAsync", "Language=" + (Language ?? "") + ", Topic=" + Topic + ", Exception=" + ex.ToString());
      }
      #endregion Catch

      return returnValue;
    }

    private async Task<Tuple<string, string>> GetLocalizationElementAsync(string Language, enumTopic Topic, string ElementName, bool IsRefreshTranslate, string DefaultValue)
    {
      #region Define vars      
      Tuple<string, string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        Tuple<string, string> returnLocalization;
        using (var StorageInterfaces = new StorageInterfacesClient())
        {
          returnLocalization = await StorageInterfaces.GetLocalizationElementAsync(LocalizationAccountKey, Language, DefaultLanguage, Project, Topic.ToString(), ElementName, IsRefreshTranslate, DefaultValue);
        }
        if (JetSASLibrary.CheckGoodString(returnLocalization.Item2)) throw new AggregateException("GetLocalizationElementAsync. Exception=" + returnLocalization.Item2);

        var ErrorMessage = "";
        returnValue = new Tuple<string, string>(returnLocalization.Item1, ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        returnValue = new Tuple<string, string>((DefaultValue ?? ""), ex.ToString());
        TraceService("GetLocalizationElementAsync", enumTypeLog.Error, "Language=" + Language + ", Topic=" + Topic.ToString() + ", ElementName" + ElementName + ", Exception=" + ex.ToString());
      }
      #endregion Catch

      return returnValue;
    }

    private string CheckForDefaultLanguage(string SourceLanguage)
    {
      if (SourceLanguage.ToLower() == "us") SourceLanguage = "en";
      return SourceLanguage;
    }

    private async Task<Tuple<List<string>, string>> TranslateArrayAsync(string Language, List<string> ListForTranslation)
    {
      #region Define vars
      Tuple<List<string>, string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        Language = CheckForDefaultLanguage(Language);
        using (var StorageInterfaces = new StorageInterfacesClient())
        {
          var returnLocalization = await StorageInterfaces.TranslateArrayAsync(LocalizationAccountKey, Language, DefaultLanguage, ListForTranslation.ToArray());
          if (JetSASLibrary.CheckGoodString(returnLocalization.Item2)) throw new ArgumentException(returnLocalization.Item2);

          var ErrorMessage = "";
          returnValue = new Tuple<List<string>, string>(returnLocalization.Item1.ToList(), ErrorMessage);
        }
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = new Tuple<List<string>, string>(new List<string>(), ex.Message);
        TraceLogError("", "TranslateArrayAsync with Parameter", "Language=" + (Language ?? "") + ", Exception=" + ex.ToString());
      }

      catch (Exception ex)
      {
        returnValue = new Tuple<List<string>, string>(new List<string>(), CollectErrorService(ex));
        TraceLogError("", "TranslateArrayAsync with Parameter", "Language=" + (Language ?? "") + ", Exception=" + ex.ToString());
      }
      #endregion Catch

      return returnValue;
    }

    private async Task<Tuple<List<string>, string>> TranslateArrayAsync(string TranslateFromLanguage, string TranslateToLanguage, List<string> ListForTranslation)
    {
      #region Define vars
      Tuple<List<string>, string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        using (var StorageInterfaces = new StorageInterfacesClient())
        {
          var returnLocalization = await StorageInterfaces.TranslateArrayAsync(LocalizationAccountKey, TranslateToLanguage, TranslateFromLanguage, ListForTranslation.ToArray());
          if (JetSASLibrary.CheckGoodString(returnLocalization.Item2)) throw new ArgumentException(returnLocalization.Item2);

          var ErrorMessage = "";
          returnValue = new Tuple<List<string>, string>(returnLocalization.Item1.ToList(), ErrorMessage);
        }
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = new Tuple<List<string>, string>(new List<string>(), ex.Message);
        TraceLogError("", "TranslateArrayAsync with Parameter", "TranslateFromLanguage=" + (TranslateFromLanguage ?? "") + ", Exception=" + ex.ToString());
      }

      catch (Exception ex)
      {
        returnValue = new Tuple<List<string>, string>(new List<string>(), CollectErrorService(ex));
        TraceLogError("", "TranslateArrayAsync with Parameter", "TranslateFromLanguage=" + (TranslateFromLanguage ?? "") + ", Exception=" + ex.ToString());
      }
      #endregion Catch

      return returnValue;
    }

    private async Task<Tuple<string, string>> TranslateTextAsync(string TranslateFromLanguage, string TranslateToLanguage, string text)
    {
      #region Define vars
      Tuple<string, string> returnValue;
      #endregion Define vars

      #region For TEMP LIMIT length of source
      int MaxCountSourceText = 1000;
      if (text.Length > MaxCountSourceText) text.Substring(0, MaxCountSourceText);
      #endregion For TEMP LIMIT length of source

      int MaxCountTranslateText = 10000;

      TranslateFromLanguage = CheckForDefaultLanguage(TranslateFromLanguage);
      TranslateToLanguage = CheckForDefaultLanguage(TranslateToLanguage);

      #region Try
      try
      {
        #region Разбиваем текст для перевода на куски заданной длины
        var ListSourceText = new List<string>();
        while (text.Length > MaxCountTranslateText)
        {
          ListSourceText.Add(text.Substring(0, MaxCountTranslateText));
          text = text.Substring(MaxCountTranslateText);
        }
        ListSourceText.Add(text);
        #endregion Разбиваем текст для перевода на куски заданной длины

        string[] aSourceText;
        using (var StorageInterfaces = new StorageInterfacesClient())
        {
          var returnLocalization = await StorageInterfaces.TranslateArrayAsync(LocalizationAccountKey, TranslateToLanguage, TranslateFromLanguage, ListSourceText.ToArray());
          if (JetSASLibrary.CheckGoodString(returnLocalization.Item2)) throw new ArgumentException(returnLocalization.Item2);
          aSourceText = returnLocalization.Item1;
        }

        var TranslatedText = "";
        foreach (var itemTranslate in aSourceText)
        {
          TranslatedText += itemTranslate;
        }

        var ErrorMessage = "";
        returnValue = new Tuple<string, string>(TranslatedText, ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = new Tuple<string, string>(null, ex.Message);
        TraceLogError("", "TranslateTextAsync with Parameter", "TranslateFromLanguage=" + (TranslateFromLanguage ?? "") + ", Exception=" + ex.ToString());
      }

      catch (Exception ex)
      {
        returnValue = new Tuple<string, string>(null, CollectErrorService(ex));
        TraceLogError("", "TranslateTextAsync with Parameter", "TranslateFromLanguage=" + (TranslateFromLanguage ?? "") + ", Exception=" + ex.ToString());
      }
      #endregion Catch

      return returnValue;
    }

    private bool CheckAvailableLanguage(string Language)
    {
      bool IsFound;

      try
      {
        Language = CheckForDefaultLanguage(Language);

        string FoundLanguage = ListLanguagesForTranslate.Single(LanguageItem => LanguageItem == Language);
        IsFound = true;
      }
      catch (Exception)
      {
        IsFound = false;
      }

      return IsFound;
    }

    private string ParseLanguage(string Language)
    {
      Language = CheckForDefaultLanguage(Language);

      if (!JetSASLibrary.CheckGoodString(Language))
      {
        Language = DefaultLanguage;
      }
      else
      {
        Language = Language.Trim().ToLower();
        if (!CheckAvailableLanguage(Language)) Language = DefaultLanguage;
      }

      return Language;
    }

    private static ConnectionMultiplexer LoadRedisConnection()
    {
      #region Define dars
      ConnectionMultiplexer CacheConnection;
      #endregion Define dars

      try
      {
        ////***@outlook.com
        CacheConnection = ConnectionMultiplexer.Connect("livemultimedia.redis.cache.windows.net,ssl=true,password=***");
      }
      catch (Exception ex)
      {
        CacheConnection=null;
        Trace.TraceError("LoadRedisConnection: " + ex.ToString());
      }
      
      return CacheConnection;
    }

    private static string[] LoadLanguagesForTranslate()
    {
      #region Define vars
      string[] ListLanguages;
      #endregion Define vars

      #region Try
      try
      {
        using (var StorageInterfaces = new StorageInterfacesClient())
        {
          var returnLanguages = StorageInterfaces.GetLanguages(null);
          if (JetSASLibrary.CheckGoodString(returnLanguages.Item2)) throw new ArgumentException(returnLanguages.Item2);

          ListLanguages = returnLanguages.Item1.Select(itemLanguage => itemLanguage.Language).ToArray();
        }
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        ListLanguages = new string[0];
        Trace.TraceError("LoadLanguagesForTranslate: " + ex.ToString());
      }
      #endregion Catch

      return ListLanguages;
    }

    #endregion Localization

    #region New getting source

    public async Task<Tuple<MultimediaItem[], string>> GetItems(string AccountKey, string Language = null, string Id = null, string GroupBy = null, string OrderBy = null, string UserToken = null)
    {
      #region Define vars
      string IdSource, IdItem;
      int IdTypeMultimediaSource;
      enumTypeMultimediaItem TypeMultimediaItem;
      List<MultimediaItem> ListMultimediaItem = new List<MultimediaItem>();
      bool IsHasChild, IsRequiredAuthorization, IsComingSoon, IsRepair;
      string TitleComingSoon, TitleRepair;
      Tuple<MultimediaItem[], string> returnValue;
      string ErrorMessage;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);

        #region Check Language
        Language = ParseLanguage(Language);
        #endregion Check Language

        #region Decrypt Id of source
        if (JetSASLibrary.CheckGoodString(Id))
        {
          DecryptSource(Id, out IdSource, out IdItem);
        }
        else
        {
          IdSource = null;
          IdItem = null;
        }

        if (JetSASLibrary.CheckGoodString(IdSource))
        {
          if (!int.TryParse(IdSource, out IdTypeMultimediaSource)) throw new ArgumentException("Id is bad", "Id", new Exception("Error convert IdSource to int: IdSource=" + IdSource));
          if (IdTypeMultimediaSource < 0) throw new ArgumentException("Id is bad", "Id", new Exception("IdSource less zero: IdSource=" + IdSource));
        }
        else
        {
          IdTypeMultimediaSource = 0;
        }
        #endregion Decrypt Id of source

        #region Get one row source by IdTypeMultimediaSource
        if (IdTypeMultimediaSource > 0)
        {
          var TableSource = await LiveMultimediaDL.SelectSourceByIdAsync(IdTypeMultimediaSource);
          IsHasChild = Convert.ToBoolean(TableSource.Rows[0][TableSource.Columns["IsHasChild"]]);
          IsRequiredAuthorization = Convert.ToBoolean(TableSource.Rows[0][TableSource.Columns["IsRequiredAuthorization"]]);
          IsComingSoon = Convert.ToBoolean(TableSource.Rows[0][TableSource.Columns["IsComingSoon"]]);
          IsRepair = Convert.ToBoolean(TableSource.Rows[0][TableSource.Columns["IsRepair"]]);
          TitleComingSoon = TableSource.Rows[0][TableSource.Columns["TitleComingSoon"]].ToString();
          TitleRepair = TableSource.Rows[0][TableSource.Columns["TitleRepair"]].ToString();
          TypeMultimediaItem = (enumTypeMultimediaItem)Convert.ToInt32(TableSource.Rows[0][TableSource.Columns["TypeMultimediaItem"]]);
        }
        else
        {
          IsHasChild = true;
          IsRequiredAuthorization = false;
          IsComingSoon = false;
          IsRepair = false;
          TitleComingSoon = "";
          TitleRepair = "";
          TypeMultimediaItem = enumTypeMultimediaItem.Source;
        }
        #endregion Get one row source by IdTypeMultimediaSource

        #region Check UserToken
        if (JetSASLibrary.CheckGoodString(UserToken))
        {
          CheckFormatGUID("UserToken", UserToken);
          await CheckUserToken("UserToken", UserToken, enumTypeUser.Remote);
        }
        else
        {
          if (IsRequiredAuthorization) throw new ArgumentException("UserToken is required", "UserToken");
        }
        #endregion Check UserToken

        #region Check grouping and sorting
        if (JetSASLibrary.CheckGoodString(GroupBy))
          GroupBy = GroupBy.Trim().ToLower();
        else
          GroupBy = "";

        if (JetSASLibrary.CheckGoodString(OrderBy))
          OrderBy = OrderBy.Trim().ToLower();
        else
          OrderBy = "";
        #endregion Check grouping and sorting

        #endregion Check incoming parameters

        #region Get list source or items
        if (TypeMultimediaItem == enumTypeMultimediaItem.Source && IsHasChild) // Get source
        {
          ListMultimediaItem = await GetSourceAsync(Language, IdTypeMultimediaSource, UserToken);
        }
        else // Get list items by source
        {
#if (DEBUG)
          IsComingSoon = false;
          IsRepair = false;
#endif
          if (!IsComingSoon && !IsRepair)
          {
            ListMultimediaItem = await GetItemsBySourceAsync(Language, IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken);
          }
          else
          {
            #region Define title item
            string TitleItem;
            if (IsComingSoon)
            {
              TitleItem = TitleComingSoon;
            }
            else
            {
              TitleItem = TitleRepair;
            }
            #endregion Define title item

            #region Define item "coming soon" or "in repair"
            var Item = new MultimediaItem();
            Item.Id = CryptSource(IdSource.ToString(), "");
            Item.Id = CryptSource("0", "");
            Item.IdSource = IdTypeMultimediaSource;
            Item.IdSource = 0;
            Item.Name = TitleItem;
            Item.StyleBackColor = "white";
            Item.StyleForeColor = "black";
            Item.TypeItem = Convert.ToInt32(enumTypeMultimediaItem.Document).ToString();
            Item.Url = "";

            #region Translate TitleComingSoon
            if (Language != DefaultLanguage)
            {
              var ListLocalization = new List<string>();
              ListLocalization.Add(TitleItem);
              var returnLocalization = await TranslateArrayAsync(Language, ListLocalization);
              if (JetSASLibrary.CheckGoodString(returnLocalization.Item2)) throw new ArgumentException(returnLocalization.Item2, "GetItems");

              Item.Name = returnLocalization.Item1[0];
            }
            #endregion Translate TitleComingSoon

            ListMultimediaItem.Add(Item);
            #endregion Define item "coming soon" or "in repair"
          }
        }
        #endregion Get list source or items

        ErrorMessage = "";
        returnValue = new Tuple<MultimediaItem[], string>(ListMultimediaItem.ToArray(), ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = new Tuple<MultimediaItem[], string>(new List<MultimediaItem>().ToArray(), ex.Message);
        Trace.TraceError("GetItems with Parameter: Language={0} Id={1} GroupBy={2} OrderBy={3} UserToken={4} Exception={5}",
          (Language ?? ""), (Id ?? ""), (GroupBy ?? ""), (OrderBy ?? ""), (UserToken ?? ""), ex.ToString());
      }
      catch (Exception ex)
      {
        ErrorMessage = CollectErrorService(string.Format("GetItems: Language={0} Id={1} GroupBy={2} OrderBy={3} UserToken={4} Exception={5}",
        (Language ?? ""), (Id ?? ""), (GroupBy ?? ""), (OrderBy ?? ""), (UserToken ?? ""), ex.ToString()), ex);
        returnValue = new Tuple<MultimediaItem[], string>(new List<MultimediaItem>().ToArray(), ErrorMessage);
      }
      #endregion Catch

      return returnValue;
    }

    private async Task<List<MultimediaItem>> GetSourceAsync(string Language, int IdSource, string UserToken)
    {
      #region Define vars
      bool IsLocalizeTitle, IsComingSoon, IsRepair;
      MultimediaItem multimediaItem;
      List<MultimediaItem> ListMultimediaItem;
      #endregion Define vars

      #region Try
      try
      {
        var TableSource = await LiveMultimediaDL.GetSourceAsync(IdSource, UserToken);
        ListMultimediaItem = new List<MultimediaItem>();

        List<string> ListLocalization = new List<string>();

        #region Convert from Table To List
        foreach (DataRow dr in TableSource.Rows)
        {
          #region Add item
          multimediaItem = new MultimediaItem();

          #region Fill SourceItem
          var IdTypeMultimediaSource = Convert.ToInt32(dr["IdTypeMultimediaSource"]);

          multimediaItem.Id = CryptSource(IdTypeMultimediaSource.ToString(), null);
          multimediaItem.StyleWidth = Convert.ToInt32(dr["StyleWidth"]); // 200;
          multimediaItem.StyleHeight = Convert.ToInt32(dr["StyleHeight"]); // 100;
          multimediaItem.StyleForeColor = dr["StyleForeColor"] as string;
          multimediaItem.StyleBackColor = dr["StyleBackColor"] as string;
          //multimediaItem.StyleFontFamily = dr["StyleFontFamily"] as string;
          //multimediaItem.StyleFontSize = Convert.ToInt32(dr["StyleFontSize"]);
          multimediaItem.IsEnabled = Convert.ToBoolean(dr["IsEnabled"]);
          multimediaItem.Description = dr["Description"] as string;
          multimediaItem.UrlImage = dr["ImageUrlLarge"] as string;
          multimediaItem.Url = "";          
          multimediaItem.TypeItem = Convert.ToInt32(enumTypeMultimediaItem.Source).ToString();
          multimediaItem.Name = dr["TitleMultimediaSource"] as string;

          IsLocalizeTitle = Convert.ToBoolean(dr["IsLocalizeTitle"]);
          IsComingSoon = Convert.ToBoolean(Convert.ToInt32(dr["IsComingSoon"]));
          IsRepair = Convert.ToBoolean(Convert.ToInt32(dr["IsRepair"]));

          if ((IsLocalizeTitle || IsComingSoon || IsRepair) && Language != DefaultLanguage)
          {
            ListLocalization.Add(multimediaItem.Name);
          }
          else
          {
            ListLocalization.Add("");
          }

          #region Localization
          //  if (Convert.ToBoolean(dr["IsLocalizeTitle"]) && Language != DefaultLanguage)
          //  {
          //    IsTranslateTitle=Convert.ToBoolean(dr["IsTranslateTitle"]);
          //    ElementName = "MultimediaSource_" + dr["NameMultimediaSource"] as string;

          //    var returnElementValue = await GetLocalizationElementAsync(Language, enumTopic.Service, ElementName, IsTranslateTitle, multimediaItem.Name);
          //    if (JetSASLibrary.CheckGoodString(returnElementValue.Item2))
          //    {
          //      TraceService("GetSourceAsync", enumTypeLog.Error, "Language=" + (Language ?? "") + ", IdSource="+IdSource.ToString() + returnElementValue.Item2, UserToken);
          //    }
          //    multimediaItem.Name = returnElementValue.Item1;
          //  }
          #endregion Localization

          #endregion Fill SourceItem

          ListMultimediaItem.Add(multimediaItem);
          #endregion Add item
        }
        #endregion Convert from Table To Array

        #region Translate
        //Временный on-line перевод. Сделать "умный" перевод массива.
        if (Language != DefaultLanguage)
        {
          var returnLocalization = await TranslateArrayAsync(Language, ListLocalization);
          if (JetSASLibrary.CheckGoodString(returnLocalization.Item2)) throw new ArgumentException(returnLocalization.Item2, "GetSourceAsync");

          var ArrayLocalization = returnLocalization.Item1; int i = 0;
          ListMultimediaItem.ForEach(item =>
          {
            if (JetSASLibrary.CheckGoodString(ArrayLocalization[i]))
            {
              item.Name = ArrayLocalization[i];              
            }
            i++;
          });
        }
        #endregion Translate
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {        
        throw new AggregateException(string.Format("GetSourceAsync: Language={0}, IdSource={1}, UserToken={2}", Language, IdSource, UserToken), ex);
      }
      #endregion Catch

      return ListMultimediaItem;
    }

    private async Task<int> GetIdParentTypeMultimediaSourceAsync(int IdTypeMultimediaSource)
    {
      int IdParentTypeMultimediaSource;

      #region Try
      try
      {
        var TableSource = await LiveMultimediaDL.SelectSourceAsync(IdTypeMultimediaSource.ToString());
        var FindRow = TableSource.Rows.Find(IdTypeMultimediaSource);
        IdParentTypeMultimediaSource = Convert.ToInt32(FindRow["IdParentTypeMultimediaSource"]);
        if (IdParentTypeMultimediaSource == 0) IdParentTypeMultimediaSource = IdTypeMultimediaSource;
      }
      #endregion Try

      #region Catch
      catch (Exception)
      {
        IdParentTypeMultimediaSource = -1;
      }
      #endregion Catch

      return IdParentTypeMultimediaSource;
    }

    private async Task<List<MultimediaItem>> GetItemsBySourceAsync(string Language, int IdTypeMultimediaSource, string IdItem, string GroupBy, string OrderBy, string UserToken)
    {
      #region Define vars
      DataTable TableMultimediaItem;
      enumTypeMultimediaItem TypeMultimediaItem;
      MultimediaItem Item;
      List<MultimediaItem> ListMultimediaItem;
      #endregion Define vars

      #region Try
      try
      {
        var IdParentTypeMultimediaSource = await GetIdParentTypeMultimediaSourceAsync(IdTypeMultimediaSource);

        #region Switch IdParentTypeMultimediaSource
        switch (IdParentTypeMultimediaSource)
        {
          case 10: //Favorites
            TableMultimediaItem = new DataTable();
            break;
          case 11: //Home
            TableMultimediaItem = await GetItemsHomeAsync(IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken);
            break;
          case 13: //Audiobooks
            TableMultimediaItem = new DataTable();
            break;
          case 14: //News
            TableMultimediaItem = await GetItemsNewsAsync(IdTypeMultimediaSource, IdItem, Language, true, UserToken);
            break;
          case 15: //Learning
            TableMultimediaItem = new DataTable();
            break;
          case 19: //OneDrive
            TableMultimediaItem = await GetItemsOneDriveAsync(IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken);
            break;
          case 20: //Google Drive
            TableMultimediaItem = await GetItemsGoogleDriveAsync(IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken);
            break;
          case 21: //VKontakte
            TableMultimediaItem = await GetItemsVKontakteAsync(IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken);
            break;
          case 22: //iCloud
            TableMultimediaItem = new DataTable();
            break;
          case 23: //Dropbox
            TableMultimediaItem = await GetItemsDropboxAsync(IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken);
            break;
          case 24: //YandexDisk
            TableMultimediaItem = await GetItemsYandexDiskAsync(IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken);
            break;
          default:
            TableMultimediaItem = new DataTable();
            break;
        }
        #endregion Switch IdParentTypeMultimediaSource

        #region Get color of item
        var dtColor = await LiveMultimediaDL.GetSourceColorAsync(IdTypeMultimediaSource);
        var StyleBackColor = dtColor.Rows[0].ItemArray[0] as string;
        var StyleForeColor = dtColor.Rows[0].ItemArray[1] as string;
        #endregion Get color of item

        #region Convert from Table to List
        ListMultimediaItem = new List<MultimediaItem>();
        foreach (DataRow Row in TableMultimediaItem.Rows)
        {
          Item = new MultimediaItem();          

          Item.Id = CryptSource(IdTypeMultimediaSource.ToString(), Row["Id"] as string);
          Item.IdSource = IdTypeMultimediaSource;
          Item.Name = Row["Name"] as string;
          Item.StyleBackColor = StyleBackColor;
          Item.StyleForeColor = StyleForeColor;
          //Item.IsRequiredAuthorization = true;
          Item.TypeItem = Row["IdTypeMultimediaItem"].ToString();

          TypeMultimediaItem = (enumTypeMultimediaItem)Convert.ToInt32(Row["IdTypeMultimediaItem"]);
          if (TypeMultimediaItem != enumTypeMultimediaItem.Folder && TypeMultimediaItem != enumTypeMultimediaItem.Unsupported)
            Item.Url = UrlMultimediaService + "?id=" + CryptId(Item.Id, UserToken);
          else
            Item.Url = "";

          if (TypeMultimediaItem == enumTypeMultimediaItem.Unsupported)
            Item.IsEnabled = false;
          else
            Item.IsEnabled = true;

          ListMultimediaItem.Add(Item);
        }
        #endregion Convert from Table to List

        #region Sorting by TypeMultimediaItem, then by Name
        ListMultimediaItem = ListMultimediaItem.OrderBy(multimediaItem => multimediaItem.TypeItem).ThenBy(multimediaItem => multimediaItem.Name).ToList();
        #endregion Sorting by TypeMultimediaItem, then by Name
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("GetItemsBySourceAsync: IdTypeMultimediaSource={0}, IdItem={1}, GroupBy={2}, OrderBy={3} UserToken={4}", IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken), ex);
      }
      #endregion Catch

      return ListMultimediaItem;
    }

    #region Get items
    private async Task<DataTable> GetItemsHomeAsync(int IdTypeMultimediaSource, string IdItem, string GroupBy, string OrderBy, string UserToken)
    {
      #region Define vars
      DataTable TableMultimediaItem;
      #endregion Define vars

      #region Try
      try
      {
        #region Switch GroupBy
        switch (GroupBy)
        {
          #region Group By folder
          default:// Group by Folder - временно, пока не сделано
          case "none": // Group by Folder - временно, пока не сделано
          case "folder": // Group by Folder
            if (CheckGoodString(IdItem))
            {
              #region Items are Files
              TableMultimediaItem = await LiveMultimediaDL.GetItemsByFolderHomeAsync(IdTypeMultimediaSource, IdItem, UserToken);
              #endregion Items are Files
            }
            else
            {
              #region Items are Folders
              TableMultimediaItem = await LiveMultimediaDL.GetFoldersByFolderHomeAsync(IdTypeMultimediaSource, UserToken);              
              #endregion Items are Folders
            }
            break;
          #endregion Group By folder
        }
        #endregion Switch GroupBy        
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("GetItemsHomeAsync: IdTypeMultimediaSource={0}, IdItem={1}, GroupBy={2}, OrderBy={3} UserToken={4}", IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken), ex);
      }
      #endregion Catch

      return TableMultimediaItem;
    }

    private async Task<DataTable> GetItemsNewsAsync(int IdTypeMultimediaSource, string IdItem, string Language, bool IsUseBubble, string UserToken)
    {
      #region Define vars
      DataTable TableMultimediaItem;
      string LanguageMarket;
      string MarketId, NewsCategory;
      string[] aMarketId;
      #endregion Define vars

      #region Try
      try
      {
        if (!JetSASLibrary.CheckGoodString(IdItem))
        {
          #region Items are countries (Market)
          TableMultimediaItem = await LiveMultimediaDL.GetItemsMarketNewsGoogleAsync();

          #region Fill list market
          LocalizationElement ItemLocalizationElement;
          var ListMarket = new List<LocalizationElement>();

          #region old
          //foreach (DataRow item in TableMultimediaItem.Rows)
          //{
          //  MarketId = item["Id"] as string;
          //  aMarketId = MarketId.Split(new char[] { '_' });
          //  LanguageMarket = aMarketId[0].Trim().ToLower();

          //  ItemLocalizationElement = new LocalizationElement();
          //  ItemLocalizationElement.ElementName = MarketId;

          //  aName = (item["Name"] as string).Split(new char[] { '|' });
          //  if (aName.Length > 1)
          //  {
          //    if (LanguageMarket == Language)
          //    {
          //      ItemLocalizationElement.ElementValue = aName[0];
          //    }
          //    else
          //    {
          //      //ItemLocalizationElement.ElementValue = aName[0] + " (" + aName[1].Substring(0, 1).ToUpperInvariant() + aName[1].Substring(1) + " language)";
          //      ItemLocalizationElement.ElementValue = aName[0] + " (" + aName[1].Substring(0, 1).ToUpperInvariant() + aName[1].Substring(1) + ")";
          //    }
          //  }
          //  else
          //  {
          //    ItemLocalizationElement.ElementValue = aName[0];
          //  }

          //  ListMarket.Add(ItemLocalizationElement);
          //}
          #endregion old

          foreach (DataRow item in TableMultimediaItem.Rows)
          {
            ItemLocalizationElement = new LocalizationElement();
            ItemLocalizationElement.ElementName = item["Id"] as string;
            ItemLocalizationElement.ElementValue = item["Name"] as string;

            ListMarket.Add(ItemLocalizationElement);
          }

          #endregion Fill list market

          #region Get market localization
          if (Language != DefaultLanguage)
          {
            var returnValue = await GetLocalizationByListAsync(Language, "NewsMarket", ListMarket);
            if (JetSASLibrary.CheckGoodString(returnValue.Item2)) throw new ArgumentException("Id is bad", "Id", new Exception(returnValue.Item2));
            ListMarket = returnValue.Item1;
          }
          #endregion Get market localization

          string[] aName;

          #region Refill table
          TableMultimediaItem.Clear(); DataRow dr;
          foreach (var item in ListMarket)
          {
            dr = TableMultimediaItem.NewRow();
            dr["Id"] = item.ElementName;

            aMarketId = item.ElementName.Split(new char[] { '|' });
            LanguageMarket = aMarketId[1].Trim().ToLower();

            aName = item.ElementValue.Split(new char[] { '|' });
            if (aName.Length > 1)
            {
              dr["Name"] = aName[0].Trim() + " (" + aName[1].Trim() + ")";

              //if (LanguageMarket == Language)
              //{
              //  dr["Name"] = aName[0].Trim();
              //}
              //else
              //{
              //  dr["Name"] = aName[0].Trim() + " (" + aName[1].Trim() + ")";
              //}
            }
            else
            {
              dr["Name"] = item.ElementValue.Trim();
            }

            dr["IdTypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Folder);

            TableMultimediaItem.Rows.Add(dr);
          }
          #endregion Refill table

          #endregion Items are countries (Market)
        }
        else
        {
          var aIdItem = IdItem.Split(new char[] { '|' });

          #region Switch
          switch (aIdItem.Length)
          {
            default:
            case 3: //Return from countries (Market)
              #region Items are News category
              //MarketId = IdItem;

              TableMultimediaItem = await LiveMultimediaDL.GetItemsCategoryNewsAsync();

              #region Fill list category
              LocalizationElement ItemLocalizationElement;
              var ListCategory = new List<LocalizationElement>();

              foreach (DataRow item in TableMultimediaItem.Rows)
              {
                ItemLocalizationElement = new LocalizationElement();

                ItemLocalizationElement.ElementName = item["Id"] as string;
                ItemLocalizationElement.ElementValue = item["Name"] as string;

                ListCategory.Add(ItemLocalizationElement);
              }
              #endregion Fill list category

              #region Get category localization
              if (Language != DefaultLanguage)
              {
                var returnValue = await GetLocalizationByListAsync(Language, "NewsCategory", ListCategory);
                if (JetSASLibrary.CheckGoodString(returnValue.Item2)) throw new ArgumentException("Id is bad", "Id", new Exception(returnValue.Item2));
                ListCategory = returnValue.Item1;
              }
              #endregion Get category localization

              #region Refill table
              TableMultimediaItem.Clear(); DataRow dr;
              foreach (var item in ListCategory)
              {
                dr = TableMultimediaItem.NewRow();
                dr["Id"] = IdItem + "|" + item.ElementName;
                dr["Name"] = item.ElementValue;
                dr["IdTypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Folder);
                TableMultimediaItem.Rows.Add(dr);
              }
              #endregion Refill table

              #endregion Items are News category
              break;
            case 4: //Return from News category
              #region Items are News
              MarketId = aIdItem[0];
              LanguageMarket = aIdItem[1];
              var LanguageISO_639_3 = aIdItem[2] as string;
              NewsCategory = aIdItem[3];

              //aMarketId = MarketId.Split(new char[] { '_' });
              //LanguageMarket = aMarketId[0].Trim().ToLower();

              //Get headers of news
              TableMultimediaItem = await LiveMultimediaDL.GetItemsNewsAsync(MarketId, NewsCategory);

              #region  Refill TableMultimediaItem
              for (int i = 0; i < TableMultimediaItem.Rows.Count; i++)
              {
                TableMultimediaItem.Rows[i]["Id"] = LanguageMarket + "|" + Language + "|" + LanguageISO_639_3 + "|" + TableMultimediaItem.Rows[i]["Id"];
              }
              #endregion  Refill TableMultimediaItem

              #region Translate header of news
              if (Language != LanguageMarket)
              {
                #region Parallel translate

                #region Convert to ConcurrentBag
                var bag = new ConcurrentBag<Tuple<string, string, int>>();
                foreach (DataRow item in TableMultimediaItem.Rows)
                {
                  //bag.Add(new Tuple<string, string, int>(LanguageMarket + "|" + Language + "|" + item["Id"], item["Name"] as string, Convert.ToInt32(enumTypeMultimediaItem.Audio)));
                  bag.Add(new Tuple<string, string, int>(item["Id"] as string, item["Name"] as string, Convert.ToInt32(enumTypeMultimediaItem.Audio)));
                }
                #endregion Convert to ConcurrentBag

                var ListTask = new ConcurrentBag<Task>();

                var ListBagNews = new ConcurrentBag<Tuple<string, string, int>>();
                Parallel.ForEach(bag, itemBag =>
                {
                  var newTask = Task.Run(async () =>
                  //var newTask = Task.Run(() =>
                  {
                    //if (ct.IsCancellationRequested) return;
                    var returnValue = await TranslateTextAsync(LanguageMarket, Language, itemBag.Item2);
                    if (returnValue.Item2 == "")
                    {
                      ListBagNews.Add(new Tuple<string, string, int>(itemBag.Item1, returnValue.Item1, itemBag.Item3));
                    }
                  }
                  );

                  ListTask.Add(newTask);
                }
                );
                Task.WaitAll(ListTask.ToArray());

                #endregion Parallel translate

                #region Fill TableMultimediaItem again with translated headers
                TableMultimediaItem.Clear();
                foreach (var item in ListBagNews)
                {
                  dr = TableMultimediaItem.NewRow();
                  dr["Id"] = item.Item1;
                  dr["Name"] = item.Item2;
                  dr["IdTypeMultimediaItem"] = item.Item3;
                  TableMultimediaItem.Rows.Add(dr);
                }
                #endregion Fill TableMultimediaItem again with translated headers
              }
              #endregion Translate header of news

              #endregion Items are News
              break;
          }
          #endregion Switch
        }
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("GetItemsNewsAsync: IdTypeMultimediaSource={0}, IdItem={1}, Language={2}, IsUseBubble={3} UserToken={4}", IdTypeMultimediaSource, IdItem, Language, IsUseBubble, UserToken), ex);
      }
      #endregion Catch

      return TableMultimediaItem;
    }

    private async Task<DataTable> GetItemsOneDriveAsync(int IdTypeMultimediaSource, string IdItem, string GroupBy, string OrderBy, string UserToken)
    {
      #region Define vars
      DataTable TableMultimediaItem;
      #endregion Define vars

      #region Try
      try
      {
        var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
        if (!LiveMultimediaLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("OAuthGetAccessToken: AccessToken is empty");

        #region Switch GroupBy
        switch (GroupBy)
        {
          #region Group By None
          default:
          case "":
          case "none": // Without grouping          
            TableMultimediaItem = await LiveMultimediaDL.GetItemsByNoneOneDriveAsync(IdTypeMultimediaSource, IdItem, AccessToken);
            break;
          #endregion Group By None

          #region Group by Folder
          case "folder": // Group by Folder
            if (JetSASLibrary.CheckGoodString(IdItem))
            {
              #region Items are Files
              //dt = await LiveMultimediaDL.GetSourceHomeItemOrderByFolderAsync(IdSource, IdItem, UserToken);
              var ListIdAlbum = new List<string>();
              ListIdAlbum.Add(IdItem);
              TableMultimediaItem = LiveMultimediaDL.GetItemsByFolderOneDrive(ListIdAlbum.ToArray(), AccessToken);

              #endregion Items are Files
            }
            else
            {
              #region Items are Folders
              TableMultimediaItem = await LiveMultimediaDL.GetListMultimediaSourceOneDrive(AccessToken);
              #endregion Items are Folders
            }
            break;
          #endregion Group By None
        }
        #endregion Group by Folder
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("GetItemsOneDriveAsync: IdTypeMultimediaSource={0}, IdItem={1}, GroupBy={2}, OrderBy={3} UserToken={4}", IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken), ex);
      }
      #endregion Catch

      return TableMultimediaItem;
    }

    private async Task<DataTable> GetItemsGoogleDriveAsync(int IdTypeMultimediaSource, string IdItem, string GroupBy, string OrderBy, string UserToken)
    {
      #region Define vars
      DataTable TableMultimediaItem;
      #endregion Define vars

      #region Try
      try
      {
        var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
        if (!LiveMultimediaLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("OAuthGetAccessToken: AccessToken is empty");

        #region Switch GroupBy
        switch (GroupBy)
        {
          #region Group By None
          default:
          case "":
          case "none": // Without grouping          
            TableMultimediaItem = await LiveMultimediaDL.GetItemsByNoneGoogleDriveAsync(IdTypeMultimediaSource, IdItem, AccessToken);
            break;
          #endregion Group By None

          #region Group By folder

          case "folder": // Group by Folder
            if (JetSASLibrary.CheckGoodString(IdItem))
            {
              #region Items are Files
              //dt = await LiveMultimediaDL.GetSourceHomeItemOrderByFolderAsync(IdSource, IdItem, UserToken);
              var ListIdAlbum = new List<string>();
              ListIdAlbum.Add(IdItem);
              TableMultimediaItem = LiveMultimediaDL.GetItemsByFolderGoogleDrive(ListIdAlbum.ToArray(), AccessToken);

              #endregion Items are Files
            }
            else
            {
              #region Items are Folders
              TableMultimediaItem = LiveMultimediaDL.GetListMultimediaSourceGoogleDrive(AccessToken);
              #endregion Items are Folders
            }

            break;
          #endregion Group By folder
        }
        #endregion Switch GroupBy
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("GetItemsGoogleDriveAsync: IdTypeMultimediaSource={0}, IdItem={1}, GroupBy={2}, OrderBy={3} UserToken={4}", IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken), ex);
      }
      #endregion Catch

      return TableMultimediaItem;
    }

    private async Task<DataTable> GetItemsVKontakteAsync(int IdTypeMultimediaSource, string IdItem, string GroupBy, string OrderBy, string UserToken)
    {
      #region Define vars
      DataTable TableMultimediaItem;
      #endregion Define vars

      #region Try
      try
      {
        var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
        if (!LiveMultimediaLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("OAuthGetAccessToken: AccessToken is empty");

        #region Switch GroupBy
        switch (GroupBy)
        {
          #region Group By None
          default:
          case "":
          case "none": // Without grouping          
          case "folder": //With grouping by folder. VKontakte has not grouping by folder
            TableMultimediaItem = await LiveMultimediaDL.GetItemsByNoneVKontakteAsync(IdTypeMultimediaSource, IdItem, AccessToken);
            break;
          #endregion Group By None

          #region Group By folder

          //case "folder": // Group by Folder
          //  if (JetSASLibrary.CheckGoodString(IdItem))
          //  {
          //    #region Items are Files
          //    var ListIdAlbum = new List<string>();
          //    ListIdAlbum.Add(IdItem);
          //    TableMultimediaItem = LiveMultimediaDL.GetItemsByFolderGoogleDrive(ListIdAlbum.ToArray(), AccessToken);

          //    #endregion Items are Files
          //  }
          //  else
          //  {
          //    #region Items are Folders
          //    TableMultimediaItem = LiveMultimediaDL.GetListMultimediaSourceGoogleDrive(AccessToken);
          //    #endregion Items are Folders
          //  }

          //  break;
            #endregion Group By folder
        }
        #endregion Switch GroupBy
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("GetItemsVKontakteAsync: IdTypeMultimediaSource={0}, IdItem={1}, GroupBy={2}, OrderBy={3} UserToken={4}", IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken), ex);
      }
      #endregion Catch

      return TableMultimediaItem;
    }

    private async Task<DataTable> GetItemsDropboxAsync(int IdTypeMultimediaSource, string IdItem, string GroupBy, string OrderBy, string UserToken)
    {
      #region Define vars
      DataTable TableMultimediaItem;
      #endregion Define vars

      #region Try
      try
      {
        var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
        if (!LiveMultimediaLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("OAuthGetAccessToken: AccessToken is empty");

        #region Switch GroupBy
        switch (GroupBy)
        {
          #region Group By None
          default:
          case "":
          case "none": // Without grouping          
            TableMultimediaItem = await LiveMultimediaDL.GetItemsByNoneDropboxAsync(IdTypeMultimediaSource, IdItem, AccessToken);
            break;
          #endregion Group By None

          #region Group By folder
          //case "folder": // Group by Folder
          //  if (JetSASLibrary.CheckGoodString(IdItem))
          //  {
          //    #region Items are Files
          //    //dt = await LiveMultimediaDL.GetSourceHomeItemOrderByFolderAsync(IdSource, IdItem, UserToken);
          //    var ListIdAlbum = new List<string>();
          //    ListIdAlbum.Add(IdItem);
          //    TableMultimediaItem = LiveMultimediaDL.GetItemsByFolderGoogleDrive(ListIdAlbum.ToArray(), AccessToken);

          //    #endregion Items are Files
          //  }
          //  else
          //  {
          //    #region Items are Folders
          //    TableMultimediaItem = LiveMultimediaDL.GetListMultimediaSourceGoogleDrive(AccessToken);
          //    #endregion Items are Folders
          //  }

          //  break;
            #endregion Group By folder
        }
        #endregion Switch GroupBy
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("GetItemsDropboxAsync: IdTypeMultimediaSource={0}, IdItem={1}, GroupBy={2}, OrderBy={3} UserToken={4}", IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken), ex);
      }
      #endregion Catch

      return TableMultimediaItem;
    }

    private async Task<DataTable> GetItemsYandexDiskAsync(int IdTypeMultimediaSource, string IdItem, string GroupBy, string OrderBy, string UserToken)
    {
      #region Define vars
      DataTable TableMultimediaItem;
      #endregion Define vars

      #region Try
      try
      {
        var AccessToken = await OAuthRenewAccessToken(UserToken, IdTypeMultimediaSource);
        if (!LiveMultimediaLibrary.CheckGoodString(AccessToken)) throw new ArgumentException("OAuthGetAccessToken: AccessToken is empty");

        #region Switch GroupBy
        switch (GroupBy)
        {
          #region Group By None
          //case "none": // Without grouping (temp)
          //  TableMultimediaItem = await LiveMultimediaDL.GetItemsByFolderYandexDiskAsync(IdTypeMultimediaSource, IdItem, AccessToken);
          //  break;
          #endregion Group By None

          #region Group By Folder
          default:
          case "":
          case "none": // Without grouping (temp)
          case "folder": // With grouping by folder
            if (!JetSASLibrary.CheckGoodString(IdItem))
            {
              #region Items are Folders
              TableMultimediaItem = await LiveMultimediaDL.GetFoldersByFolderYandexDiskAsync(IdTypeMultimediaSource, IdItem, AccessToken);
              #endregion Items are Folders
            }
            else
            {
              #region Items are Files
              TableMultimediaItem = await LiveMultimediaDL.GetItemsByFolderYandexDiskAsync(IdTypeMultimediaSource, IdItem, AccessToken);
              #endregion Items are Files
            }
            break;
            #endregion Group By Folder
        }
        #endregion Switch GroupBy
      }
      #endregion Try

      #region Catch
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("GetItemsYandexDiskAsync: IdTypeMultimediaSource={0}, IdItem={1}, GroupBy={2}, OrderBy={3} UserToken={4}", IdTypeMultimediaSource, IdItem, GroupBy, OrderBy, UserToken), ex);
      }
      #endregion Catch

      return TableMultimediaItem;
    }
    #endregion Get items

    public async Task<Tuple<BreadCrumps[], string>> GetBreadCrumbs(string AccountKey, string Language = null, string Source = null, string ParentId = null, string UserToken = null)
    {
      #region Define vars
      string sIdSource;
      string IdItem;
      int IdSource;
      int IdTypeMultimediaSource;
      bool IsRequiredAuthorization;

      List<BreadCrumps> ListBreadCrumbs = new List<BreadCrumps>();
      string ElementName;
      bool IsTranslateTitle;
      List<MultimediaSource> ListMultimediaItem = new List<MultimediaSource>();
      string BreadCrumbsName;
      BreadCrumps BreadCrumpsItem;
      Tuple<BreadCrumps[], string> returnValue;
      string ErrorMessage;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);

        #region Check Language
        Language = ParseLanguage(Language);
        #endregion Check Language

        #region Check Source
        if (CheckGoodString(Source))
        {
          DecryptSource(Source, out sIdSource, out IdItem);
        }
        else
        {
          sIdSource = null;
          IdItem = null;
          IsRequiredAuthorization=false;
        }

        if (CheckGoodString(sIdSource))
        {
          if (!int.TryParse(sIdSource, out IdSource)) throw new ArgumentException("Id is bad", "Id");
        }
        else
        {
          IdSource = 0;
        }
        #endregion Check Source

        #region Check UserToken
        if (CheckGoodString(UserToken))
        {
          CheckFormatGUID("UserToken", UserToken);
          await CheckUserToken("UserToken", UserToken, enumTypeUser.Remote);
        }
        else
        {
          //if (IsRequiredAuthorization) throw new ArgumentException("UserToken is required", "UserToken");
          UserToken = null;
        }
        #endregion Check UserToken

        #endregion Check incoming parameters

        if (IdSource>0)
        {
          #region Get bread crumps from Source
          var TableSource = await LiveMultimediaDL.SelectSourceAsync();

          var IdParent = IdSource;
          while (IdParent != 0)
          {
            var FindRow = TableSource.Rows.Find(IdParent);

            IdTypeMultimediaSource = Convert.ToInt32(FindRow["IdTypeMultimediaSource"]);
            IdParent = Convert.ToInt32(FindRow["IdParentTypeMultimediaSource"]);
            IsRequiredAuthorization = Convert.ToBoolean(Convert.ToInt32(FindRow["IsRequiredAuthorization"]));
            BreadCrumbsName = Convert.ToString(FindRow["TitleMultimediaSource"]);

            #region Localization
            if (Convert.ToBoolean(FindRow["IsLocalizeTitle"]) && Language != DefaultLanguage)
            {
              ElementName = "MultimediaSource_" + Convert.ToString(FindRow["NameMultimediaSource"]);
              IsTranslateTitle = Convert.ToBoolean(Convert.ToInt32(FindRow["IsTranslateTitle"]));
              
              var returnBreadCrumbsName = await GetLocalizationElementAsync(Language, enumTopic.Service, ElementName, IsTranslateTitle, BreadCrumbsName);
              if (JetSASLibrary.CheckGoodString(returnBreadCrumbsName.Item2))
              {
                TraceService("GetBreadCrumbs", enumTypeLog.Error, "Language=" + (Language ?? "") + ", IdSource=" + IdSource.ToString() + ", Exception="+returnBreadCrumbsName.Item2, UserToken);
              }
              BreadCrumbsName = returnBreadCrumbsName.Item1;

            }
            #endregion Localization

            BreadCrumpsItem = new BreadCrumps();
            BreadCrumpsItem.Id = CryptSource(IdTypeMultimediaSource.ToString(), null);
            BreadCrumpsItem.Name = BreadCrumbsName;
            BreadCrumpsItem.IsRequiredAuthorization = IsRequiredAuthorization;

            ListBreadCrumbs.Insert(0, BreadCrumpsItem);
          }

          #endregion Get bread crumps from Source

          #region Get bread crumps from Source Items
          if (CheckGoodString(IdItem))
          {

          }
          #endregion Get bread crumps from Source Items          
        }

        #region Insert crump "Main page"
        var returnBreadCrumbsMain = await GetLocalizationElementAsync(Language, enumTopic.Service, "BreadCrumbs_Name_Main", IsRefreshTranslate, "Main page");
        if (JetSASLibrary.CheckGoodString(returnBreadCrumbsMain.Item2))
        {
          TraceService("GetBreadCrumbs", enumTypeLog.Error, "Language=" + (Language ?? "") + ", IdSource=" + IdSource.ToString()+", BreadCrumbs_Name_Main"+ ", Exception="+returnBreadCrumbsMain.Item2, UserToken);
        }
        BreadCrumbsName = returnBreadCrumbsMain.Item1;

        BreadCrumpsItem = new BreadCrumps();
        BreadCrumpsItem.Id = "";
        BreadCrumpsItem.Name = BreadCrumbsName;

        ListBreadCrumbs.Insert(0, BreadCrumpsItem);
        #endregion Insert crump "Main page"

        //if (ListBreadCrumbs.Count() > 1 && CheckGoodString(IdItem)) 
        //{
        //  ListBreadCrumbs[ListBreadCrumbs.Count() - 1].Id = "";
        //}

        ErrorMessage = "";
        returnValue = new Tuple<BreadCrumps[], string>(ListBreadCrumbs.ToArray(), ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        ErrorMessage = ex.Message;
        returnValue = new Tuple<BreadCrumps[], string>(new List<BreadCrumps>().ToArray(), ErrorMessage);
        Trace.TraceError("GetBreadCrumbs with Parameter: " + ex.ToString() + ", UserToken=" + (UserToken ?? ""));
      }
      catch (Exception ex)
      {
        ErrorMessage = CollectErrorService(string.Format("GetBreadCrumbs: Language={0} Source={1} ParentId={2} UserToken={3}", Language, Source, ParentId, UserToken), ex);
        returnValue = new Tuple<BreadCrumps[], string>(new List<BreadCrumps>().ToArray(), ErrorMessage);
      }
      #endregion Catch

      return returnValue;
    }

    public async Task<StringDictionary> CheckAuthorization(string AccountKey, string Id, string UserToken = null)
    {
      #region Define vars
      string IdSource, IdItem;
      int IdTypeMultimediaSource;
      bool IsRequiredAuthorization, IsRequiredOAuth;
      int IdTypeMultimediaOAuth;
      bool IsSuccess;
      StringDictionary returnValue = new StringDictionary();
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);

        #region Decrypt Id of source
        JetSASLibrary.CheckGoodString("Id", Id);
        DecryptSource(Id, out IdSource, out IdItem);
        if (!JetSASLibrary.CheckGoodString(IdSource)) throw new ArgumentException("Id is bad", "Id", new Exception("IdSource less zero: IdSource=" + IdSource));
        if (!int.TryParse(IdSource, out IdTypeMultimediaSource)) throw new ArgumentException("Id is bad", "Id", new Exception("Error convert IdSource to int: IdSource=" + IdSource));
        if (IdTypeMultimediaSource < 0) throw new ArgumentException("Id is bad", "Id", new Exception("IdSource less zero: IdSource=" + IdSource));
        #endregion Decrypt Id of source
        #endregion Check incoming parameters
        
        #region Get one row source by IdTypeMultimediaSource
        var TableSource = await LiveMultimediaDL.SelectSourceByIdAsync(IdTypeMultimediaSource);
        IsRequiredAuthorization = Convert.ToBoolean(TableSource.Rows[0][TableSource.Columns["IsRequiredAuthorization"]]);
        IsRequiredOAuth = Convert.ToBoolean(TableSource.Rows[0][TableSource.Columns["IsRequiredOAuth"]]);
        IdTypeMultimediaOAuth = Convert.ToInt32(TableSource.Rows[0][TableSource.Columns["IdTypeMultimediaOAuth"]]);
        #endregion Get one row source by IdTypeMultimediaSource

        #region Check Authorization
        if (IsRequiredAuthorization)
        {
          if (JetSASLibrary.CheckGoodString(UserToken) && CheckFormatGUID(UserToken))
          {
            //UserToken is correct
            IsRequiredAuthorization = false;

            if (IsRequiredOAuth)
            {
              IsSuccess = await OAuthCheckTokenAsync(UserToken, IdTypeMultimediaSource);

              if (IsSuccess)
              {
                IsRequiredOAuth = false;

                returnValue.Add("IsRequiredAuthorization", IsRequiredAuthorization.ToString());
                returnValue.Add("IsRequiredOAuth", IsRequiredOAuth.ToString());
                returnValue.Add("OAuthUrlSignIn", null);
                returnValue.Add("OAuthUrlSignOut", null);
                returnValue.Add("OAuthUrlToken", null);
                returnValue.Add("Error", "");
              }
              else
              {
                var TypeMultimediaOAuth = await LiveMultimediaDL.OAuthGetTypeMultimediaAsync(IdTypeMultimediaSource);
                if (TypeMultimediaOAuth.Rows.Count > 0)
                {
                  //Проверка и на Demo, и заодно на валидность UserToken. Если нет, то исключение
                  var IsUserDemo = await CheckIdUserDemoAsync(UserToken, enumTypeUser.Remote);

                  returnValue.Add("IsRequiredAuthorization", IsRequiredAuthorization.ToString());
                  returnValue.Add("IsRequiredOAuth", IsRequiredOAuth.ToString());

                  if (!IsUserDemo)
                    returnValue.Add("OAuthUrlSignIn", TypeMultimediaOAuth.Rows[0].Field<string>("OAuthUrlSignIn"));
                  else
                    returnValue.Add("OAuthUrlSignIn", TypeMultimediaOAuth.Rows[0].Field<string>("OAuthUrlSignInDemo"));

                  returnValue.Add("OAuthUrlSignOut", TypeMultimediaOAuth.Rows[0].Field<string>("OAuthUrlSignOut"));
                  returnValue.Add("OAuthUrlToken", TypeMultimediaOAuth.Rows[0].Field<string>("OAuthUrlToken"));
                  returnValue.Add("Error", "");
                }
                else
                {
                  throw new ArgumentException("Id is bad", "Id", new Exception("LiveMultimediaDL.OAuthGetTypeMultimediaAsync rows count is zero: IdTypeMultimediaSource=" + IdTypeMultimediaSource.ToString()));
                }
              }
            }
            else
            {
              returnValue.Add("IsRequiredAuthorization", IsRequiredAuthorization.ToString());
              returnValue.Add("IsRequiredOAuth", IsRequiredOAuth.ToString());
              returnValue.Add("OAuthUrlSignIn", null);
              returnValue.Add("OAuthUrlSignOut", null);
              returnValue.Add("OAuthUrlToken", null);
            }
          }
          else
          {
            returnValue.Add("IsRequiredAuthorization", IsRequiredAuthorization.ToString());
            returnValue.Add("IsRequiredOAuth", null);
            returnValue.Add("OAuthUrlSignIn", null);
            returnValue.Add("OAuthUrlSignOut", null);
            returnValue.Add("OAuthUrlToken", null);
            returnValue.Add("Error", "");
          }
        }
        else
        {
          returnValue.Add("IsRequiredAuthorization", IsRequiredAuthorization.ToString());
          returnValue.Add("IsRequiredOAuth", null);
          returnValue.Add("OAuthUrlSignIn", null);
          returnValue.Add("OAuthUrlSignOut", null);
          returnValue.Add("OAuthUrlToken", null);
          returnValue.Add("Error", "");
        }
        #endregion Check Authorization
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        Trace.TraceError("CheckAuthorization with Parameter: Id={0} UserToken={1} Exception={2}", (Id ?? ""), (UserToken ?? ""), ex.ToString());

        returnValue.Add("IsRequiredAuthorization", null);
        returnValue.Add("IsRequiredOAuth", null);
        returnValue.Add("OAuthUrlSignIn", null);
        returnValue.Add("OAuthUrlSignOut", null);
        returnValue.Add("OAuthUrlToken", null);
        returnValue.Add("Error", ex.Message);
      }
      catch (Exception ex)
      {
        var ErrorMessage = CollectErrorService(string.Format("CheckAuthorization with Parameter: Id={0} UserToken={1} Exception={2}", (Id ?? ""), (UserToken ?? ""), ex.ToString()), ex);

        returnValue.Add("IsRequiredAuthorization", null);
        returnValue.Add("IsRequiredOAuth", null);
        returnValue.Add("OAuthUrlSignIn", null);
        returnValue.Add("OAuthUrlSignOut", null);
        returnValue.Add("OAuthUrlToken", null);
        returnValue.Add("Error", ErrorMessage);
      }
      #endregion Catch

      return returnValue;
    }

    public async Task<string[]> GetListGroupBy(string AccountKey, string Language = null, string Source = null, string UserToken = null)
    {
      List<string> ListGroupBy = new List<string>();
      await Task.Delay(1);
      return ListGroupBy.ToArray();
    }

    public async Task<string[]> GetListOrderBy(string AccountKey, string Language = null, string Source = null, string UserToken = null)
    {
      List<string> ListOrderBy = new List<string>();
      await Task.Delay(1);
      return ListOrderBy.ToArray();
    }

    private string CryptSource(string IdSource, string IdItem)
    {
      CheckGoodString("IdSource", IdSource);
      if (!CheckGoodString(IdItem)) IdItem = "";      

      var FormattedSource = IdSource + "#" + IdItem;

      //Crypt clear FormattedSource
      //var CryptedSource = FormattedSource;

      var bytes = Encoding.UTF8.GetBytes(FormattedSource);
      var CryptedSource = HttpServerUtility.UrlTokenEncode(bytes);
        //HttpUtility.UrlEncode(FormattedSource);
      return CryptedSource;
    }

    private void DecryptSource(string CryptedSource, out string IdSource, out string IdItem)
    {
      CheckGoodString("Source", CryptedSource);

      //Decrypt FormattedSource
      //var FormattedSource = CryptedSource;
      var bytes = HttpServerUtility.UrlTokenDecode(CryptedSource);
      var FormattedSource = Encoding.UTF8.GetString(bytes);

      //FormattedSource = HttpUtility.UrlDecode(CryptedSource);

      try
      {
        var aUncryptedSource = FormattedSource.Split(new char[] { '#' });

        IdSource = aUncryptedSource[0];
        IdItem = aUncryptedSource[1];
      }
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("DecryptSource: CryptedSource={0}", CryptedSource), ex);
      }
    }

    private string CryptId(string Id, string UserToken)
    {
      CheckGoodString("Id", Id);
      if (!CheckGoodString(UserToken)) UserToken = "";

      var FormattedSource = Id + "&" + UserToken;

      var bytes = Encoding.UTF8.GetBytes(FormattedSource);
      var CryptedId = HttpServerUtility.UrlTokenEncode(bytes);
      return CryptedId;
    }

    private void DecryptId(string CryptedId, out string Id, out string UserToken)
    {
      CheckGoodString("Source", CryptedId);

      var bytes = HttpServerUtility.UrlTokenDecode(CryptedId);
      var FormattedSource = Encoding.UTF8.GetString(bytes);

      try
      {
        var aUncryptedSource = FormattedSource.Split(new Char[] { '&' });

        Id = aUncryptedSource[0];
        UserToken = aUncryptedSource[1];
      }
      catch (Exception ex)
      {
        throw new AggregateException(string.Format("DecryptId: CryptedId={0}", CryptedId), ex);
      }
    }

    [DataContract]
    public class MultimediaStream : MemoryStream, IDisposable
    {
      private long length = 0;
      private long position = 0;

      private string IdJob, IdItem, ClientQueueName;

      private long Range1, Range2;

      private long nextRange1, nextRange2;
      private string nextblobNameBuffer;

      private List<Tuple<long, long, string>> ListNextRange;

      public MultimediaStream()
      {
        Range1 = -1; Range2 = -1;

        ListNextRange = new List<Tuple<long, long, string>>();

        position = 0;
      }

      public MultimediaStream(string IdJob, string IdItem, string ClientQueueName, long MultimediaLength, long Range1, long Range2)
      {
        this.IdJob = IdJob;
        this.ClientQueueName = ClientQueueName;
        this.IdItem = IdItem;
        this.length = MultimediaLength;
        this.Range1 = Range1;
        this.Range2 = Range2;

        ListNextRange = new List<Tuple<long, long, string>>();

        position = 0;
      }

      #region Stub
      [DataMember]
      public override bool CanRead
      {
        get
        {
          return true;
        }
      }

      [DataMember]
      public override bool CanWrite
      {
        get
        {
          return false;
        }
      }

      [DataMember]
      public override bool CanSeek
      {
        get
        {
          return false;
        }
      }

      [DataMember]
      public override long Length
      {
        get
        {
          return length;
        }
      }

      [DataMember]
      public override long Position
      {
        get
        {
          return position;
        }

        set
        {
          position = value;
        }
      }

      #endregion Stub

      /* Рабочая версия Read */
      //public override int Read(byte[] buffer, int offset, int count)
      //{
      //  #region Define vars
      //  int readCount = 0;
      //  #endregion Define vars

      //  if (Range1 == -1 && Range2 == -1) return 0;

      //  #region Calc current range
      //  long newRange1 = Range1 + position;
      //  long newRange2 = newRange1 + count;
      //  #endregion Calc current range

      //  if (newRange1 >= length) return 0;

      //  #region Try
      //  try
      //  {
      //    var blobNameBuffer = IdJob + Guid.NewGuid().ToString();

      //    #region Queue message for start preparing buffer
      //    CloudQueue queueClient = queueCloud.GetQueueReference(ClientQueueName);
      //    string queueMessage = Convert.ToInt32(enumActionMultimedia.StartTransfer).ToString() + "|" + blobNameBuffer + "|" + IdItem + "|" + newRange1.ToString() + "|" + newRange2.ToString();
      //    CloudQueueMessage message = new CloudQueueMessage(queueMessage);
      //    queueClient.AddMessage(message);
      //    #endregion Queue message for start prepare buffers          

      //    #region Read buffer from cache
      //    var cache = CacheConnection.GetDatabase();

      //    var taskReadBuffer = Task.Run<byte[]>(async () =>
      //    {
      //      #region Check exist buffer in cache
      //      var ctsCheckExists = new CancellationTokenSource(new TimeSpan(0, 0, WaitTimeReplySeconds));
      //      try
      //      {
      //        while (!ctsCheckExists.IsCancellationRequested)
      //        {
      //          if (await cache.KeyExistsAsync(blobNameBuffer, CommandFlags.None))
      //          {
      //            break;
      //          }
      //          else
      //          {
      //            await Task.Delay(100);
      //          }
      //        }
      //      }
      //      catch (Exception ex)
      //      {
      //        ctsCheckExists.Cancel();
      //      }

      //      if (ctsCheckExists.IsCancellationRequested)
      //      {
      //        ctsCheckExists.Dispose();
      //        throw new ArgumentException("Live Multimedia Server is not ready");
      //      }

      //      ctsCheckExists.Dispose();
      //      #endregion Check exist buffer in cache

      //      #region Get buffer from cache
      //      var rvb = await cache.SetMembersAsync(blobNameBuffer, CommandFlags.None);
      //      #endregion Get buffer from cache            

      //      return rvb[0];
      //    }
      //    );
      //    taskReadBuffer.Wait();
      //    taskReadBuffer.Result.CopyTo(buffer, 0);
      //    #endregion Read buffer from cache

      //    readCount = taskReadBuffer.Result.Length;
      //    position += readCount;
      //  }
      //  #endregion Try

      //  #region Catch
      //  catch (Exception ex)
      //  {
      //    readCount = 0;
      //  }
      //  #endregion Catch

      //  return readCount;
      //}

      public override int Read(byte[] buffer, int offset, int count)
      {
        #region Define vars
        int readCount;
        #endregion Define vars

        if (Range1 == -1 && Range2 == -1) return 0;

        #region Calc current range
        long newRange1 = Range1 + position;
        long newRange2 = newRange1 + count-1;
        #endregion Calc current range

        if (newRange1 >= length) return 0;

        byte[] newBuffer;

        var itemNextRange = ListNextRange.Find(item => item.Item1 == newRange1 && item.Item2 == newRange2);

        //Stopwatch test = new Stopwatch(); test.Start();

        //if (newRange1 == nextRange1 && newRange2 == nextRange2)
        if (itemNextRange!=null)
        {
          newBuffer = ReadBuffer(itemNextRange.Item3);
          ListNextRange.Remove(itemNextRange);
          //test.Stop(); Debug.WriteLine("Buffer from cache={0}. newRange1={1}. newRange2={2}. count={3}. ListNextRange.Count()={4}", test.Elapsed.Milliseconds, newRange1, newRange2, count, ListNextRange.Count());
        }
        else
        {
          ListNextRange.Clear();
          newBuffer = ReadBuffer(ReadQueueMessage(newRange1, newRange2));
          //test.Stop(); Debug.WriteLine("Buffer from read={0}. newRange1={1}. newRange2={2}. count={3}. ListNextRange.Count()={4}", test.Elapsed.Milliseconds, newRange1, newRange2, count, ListNextRange.Count());
        }

        if (newBuffer != null)
        {
          newBuffer.CopyTo(buffer, 0);
          readCount = newBuffer.Length;
          position += readCount;

          int maxNext = 1;
          for (int i = 0; i < maxNext; i++)
          {
            AddNextRange(count);
          }

          //#region Calc next range
          //nextRange1 = Range1 + position;
          //nextRange2 = nextRange1 + count;
          //if (nextRange1 < length)
          //{
          //  nextblobNameBuffer = ReadQueueMessage(nextRange1, nextRange2);
          //}          
          //#endregion Calc next range
        }
        else
        {
          readCount = 0;
        }

        return readCount;
      }

      private void AddNextRange(int count)
      {
        long lastPosition;

        if (ListNextRange.Count() == 0)
        {
          lastPosition = position;
        }
        else
        {
          var itemNextRange = ListNextRange.ElementAt(ListNextRange.Count() - 1);
          lastPosition = itemNextRange.Item2+1;
        }
        
        nextRange1 = lastPosition;
        nextRange2 = nextRange1 + count - 1;
        if (nextRange1 < length)
        {
          nextblobNameBuffer = ReadQueueMessage(nextRange1, nextRange2);
          ListNextRange.Add(new Tuple<long, long, string>(nextRange1, nextRange2, nextblobNameBuffer));
        }
      }

      private string ReadQueueMessage(long newRange1, long newRange2)
      {
        #region Define vars
        string newblobNameBuffer;
        #endregion Define vars

        #region Try
        try
        {
          newblobNameBuffer = IdJob + Guid.NewGuid().ToString();

          #region Queue message for start preparing buffer
          var taskQueueMessage = Task.Run(async () =>
          {
            var queueClient = queueCloud.GetQueueReference(ClientQueueName);
            var queueMessage = Convert.ToInt32(enumActionMultimedia.StartTransfer).ToString() + "|" + newblobNameBuffer + "|" + IdItem + "|" + newRange1.ToString() + "|" + newRange2.ToString();
            var message = new CloudQueueMessage(queueMessage);
            await queueClient.AddMessageAsync(message);
          }
          );
          taskQueueMessage.Wait();
          #endregion Queue message for start preparing buffer
        }
        #endregion Try

        #region Catch
        catch (Exception)
        {
          newblobNameBuffer = null;
        }
        #endregion Catch

        return newblobNameBuffer;
      }

      private byte[] ReadBuffer(string blobNameBuffer)
      {
        #region Define vars
        byte[] newbuffer;
        #endregion Define vars

        if (!JetSASLibrary.CheckGoodString(blobNameBuffer)) return null;

        #region Try
        try
        {
          var cache = CacheConnection.GetDatabase();

          #region Read buffer from cache         
          var taskReadBuffer = Task.Run<byte[]>(async () =>
          {
            #region Check exist buffer in cache
            var ctsCheckExists = new CancellationTokenSource(new TimeSpan(0, 0, WaitTimeReplySeconds));
            try
            {
              while (!ctsCheckExists.IsCancellationRequested)
              {
                if (await cache.KeyExistsAsync(blobNameBuffer, CommandFlags.None))
                {
                  break;
                }
                else
                {
                  await Task.Delay(100);
                }
              }
            }
            catch (Exception)
            {
              ctsCheckExists.Cancel();
            }

            if (ctsCheckExists.IsCancellationRequested)
            {
              ctsCheckExists.Dispose();
              throw new ArgumentException("Live Multimedia Server is not ready");
            }

            ctsCheckExists.Dispose();
            #endregion Check exist buffer in cache

            #region Get buffer from cache
            var rvb = await cache.SetMembersAsync(blobNameBuffer, CommandFlags.None);
            #endregion Get buffer from cache            

            return rvb[0];
          }
          );
          taskReadBuffer.Wait();
          newbuffer = new byte[taskReadBuffer.Result.Length];
          taskReadBuffer.Result.CopyTo(newbuffer, 0);
          #endregion Read buffer from cache
        }
        #endregion Try

        #region Catch
        catch (Exception)
        {
          newbuffer = null;
        }
        #endregion Catch

        return newbuffer;
      }

      public override void Write(byte[] buffer, int offset, int count)
      {
        length = buffer.Length;
        base.Write(buffer, offset, count);
      }

      public override long Seek(long offset, SeekOrigin origin)
      {
        return 0;
      }

      public override void SetLength(long value)
      {
      }

      public override void Flush()
      {
      }

      protected override void Dispose(bool disposing)
      {
        base.Dispose(disposing);
      }
    }

    [DataContract]
    public class MultimediaStreamOAuth : MemoryStream, IDisposable
    {
      #region Define vars
      private long length = 0;
      private long position = 0;

      private string IdJob, IdItem;
      private long Range1, Range2;
      private string AccessToken;
      private string DownloadUrl;
      private string OAuthAuthorizationHeader;
      #endregion Define vars

      public MultimediaStreamOAuth()
      {
        Range1 = -1; Range2 = -1;
        position = 0;
      }

      public MultimediaStreamOAuth(string IdJob, long IdItemLength, long Range1, long Range2, string AccessToken, string DownloadUrl, string OAuthAuthorizationHeader)
      {
        this.IdJob = IdJob;
        this.length = IdItemLength;
        this.Range1 = Range1;
        this.Range2 = Range2;
        this.AccessToken = AccessToken;
        this.DownloadUrl = DownloadUrl;
        this.OAuthAuthorizationHeader = OAuthAuthorizationHeader;
        position = 0;
      }

      #region Stub
      [DataMember]
      public override bool CanRead
      {
        get
        {
          return true;
        }
      }

      [DataMember]
      public override bool CanWrite
      {
        get
        {
          return false;
        }
      }

      [DataMember]
      public override bool CanSeek
      {
        get
        {
          return false;
        }
      }

      [DataMember]
      public override long Length
      {
        get
        {
          return length;
        }
      }

      [DataMember]
      public override long Position
      {
        get
        {
          return position;
        }

        set
        {
          position = value;
        }
      }

      #endregion Stub

      //Решить вопрос с правильным вычислением диапазонов с учётом длины файла
      public override int Read(byte[] buffer, int offset, int count)
      {
        #region Define vars
        int readCount;
        #endregion Define vars

        if (Range1 == -1 && Range2 == -1) return 0;

        #region Calc current range
        long newRange1 = Range1 + position;
        long newRange2 = newRange1 + count - 1;
        if (newRange2 > this.length) newRange2 = this.length - 1;
        #endregion Calc current range

        if (newRange1 >= length) return 0;        

        #region Read buffer
        var taskReadBuffer = Task.Run<byte[]>(async () =>
        {
          byte[] MultimediaFileBuffer;

          try
          {
            var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(DownloadUrl);
            myHttpWebRequest.AddRange(newRange1, newRange2);
            if (!string.IsNullOrEmpty(OAuthAuthorizationHeader) && !string.IsNullOrEmpty(AccessToken))
            {
              myHttpWebRequest.Headers.Add(OAuthAuthorizationHeader, "Bearer " + AccessToken);
            }

            using (var myHttpWebResponse = (HttpWebResponse)await myHttpWebRequest.GetResponseAsync())
            {
              using (var streamResponse = myHttpWebResponse.GetResponseStream())
              {
                using (var result = new MemoryStream())
                {
                  await streamResponse.CopyToAsync(result);
                  result.Position = 0;
                  byte[] temp = new byte[count];
                  readCount = await result.ReadAsync(temp, 0, count);
                  MultimediaFileBuffer = new byte[readCount];
                  Array.Copy(temp, 0, MultimediaFileBuffer, 0, readCount);
                }
              }
            }
          }
          catch (Exception ex)
          {
            MultimediaFileBuffer = null;
          }

          return MultimediaFileBuffer;
        }
        );
        taskReadBuffer.Wait();
        if (taskReadBuffer.Result != null)
        {
          taskReadBuffer.Result.CopyTo(buffer, 0);
          readCount = taskReadBuffer.Result.Length;
          position += readCount;
        }
        else
        {
          readCount = 0;
        }
        #endregion Read buffer

        return readCount;
      }

      public override void Write(byte[] buffer, int offset, int count)
      {
        length = buffer.Length;
        base.Write(buffer, offset, count);
      }

      public override long Seek(long offset, SeekOrigin origin)
      {
        return 0;
      }

      public override void SetLength(long value)
      {
      }

      public override void Flush()
      {
      }

      protected override void Dispose(bool disposing)
      {
        base.Dispose(disposing);
      }
    }

    [DataContract]
    public class MultimediaStreamNews : MemoryStream, IDisposable
    {
      #region Define vars
      private long length = 0;
      private long position = 0;

      private string IdJob, IdItem;
      private long Range1, Range2;      

      private ConcurrentBag<Tuple<string, long>> ListBagSpeak;
      #endregion Define vars

      public MultimediaStreamNews()
      {
        Range1 = -1; Range2 = -1;
        position = 0;
      }

      public MultimediaStreamNews(string IdJob, string IdItem, long IdItemLength, long Range1, long Range2, string DownloadUrl, string OAuthAuthorizationHeader)
      {
        #region Init vars
        this.IdJob = IdJob;
        this.IdItem = IdItem;
        this.length = IdItemLength;
        this.Range1 = Range1;
        this.Range2 = Range2;
        position = 0;
        #endregion Init vars

        var aIdItem = IdItem.Split(new char[] { '|' });
        if (aIdItem.Length != 3) throw new ArgumentException("IdItem is bad for News", "IdItem", new Exception("IdItem don't contains all three parameters"));

        var LanguageFromTranslate =  aIdItem[0];
        var LanguageToTranslate = aIdItem[1];
        var LanguageISO_639_3 = aIdItem[2];

        if (LanguageFromTranslate == "us") LanguageFromTranslate = "en";
        if (LanguageToTranslate == "us") LanguageToTranslate = "en";

        var NewsURL = aIdItem[2];

        string text;

        #region Get News
        var taskReadNews = Task.Run(async () =>
        {
          string textTask;

          try
          {
            textTask = await GetTextNewsFromURL(NewsURL, LanguageISO_639_3);
          }
          catch (Exception ex)
          {
            textTask = null;
          }

          return textTask;
        }
        );
        taskReadNews.Wait();
        if (taskReadNews.Result != null)
        {
          text = taskReadNews.Result;
        }
        else
        {
          text = null;
        }
        #endregion Get News

        #region Translate News
        if (LanguageFromTranslate != LanguageToTranslate)
        {
          var taskTranslateNews = Task.Run(async () =>
          {
            string textTask;

            try
            {
              int MaxCountTranslateText = 10000;

              #region For TEMP LIMIT length of source
              int MaxCountSourceText = 1000;
              if (text.Length > MaxCountSourceText) text = text.Substring(0, MaxCountSourceText);
              #endregion For TEMP LIMIT length of source

              #region Разбиваем текст для перевода на куски заданной длины
              var ListSourceText = new List<string>();
              while (text.Length > MaxCountTranslateText)
              {
                ListSourceText.Add(text.Substring(0, MaxCountTranslateText));
                text = text.Substring(MaxCountTranslateText);
              }
              ListSourceText.Add(text);
              #endregion Разбиваем текст для перевода на куски заданной длины
              
              #region Translate
              textTask = "";
              using (var StorageInterfaces = new StorageInterfacesClient())
              {
                foreach (var item in ListSourceText)
                {
                  var returnLocalization = await StorageInterfaces.TranslateStringAsync(LocalizationAccountKey, LanguageFromTranslate, LanguageToTranslate, item);
                  if (JetSASLibrary.CheckGoodString(returnLocalization.Item2)) throw new ArgumentException(returnLocalization.Item2);
                  textTask += returnLocalization.Item1;
                }
              }
              #endregion Translate
            }
            catch (Exception ex)
            {
              textTask = null;
            }

            return textTask;
          }
          );
          taskTranslateNews.Wait();
          if (taskTranslateNews.Result != null)
          {
            text = taskTranslateNews.Result;
          }
          else
          {
            text = null;
          }
        }
        #endregion Translate News        

        #region Speak News
        string[] aSpeakURL;
        var taskSpeakNews = Task.Run(async () =>
        {
          string[] aSpeakTask;

          try
          {
            aSpeakTask = await SpeakNews(text, LanguageToTranslate);
          }
          catch (Exception ex)
          {
            aSpeakTask = null;
          }

          return aSpeakTask;
        }
        );
        taskSpeakNews.Wait();
        if (taskSpeakNews.Result != null)
        {
          aSpeakURL = taskSpeakNews.Result;
        }
        else
        {
          aSpeakURL = null;
        }
        #endregion Speak News

        #region Get Speak News length

        #region Convert to ConcurrentBag
        var bag = new ConcurrentBag<Tuple<string, long>>();
        for (int i = 0; i < aSpeakURL.Length; i++)
        {
          bag.Add(new Tuple<string, long>(aSpeakURL[i], 0));
        }
        #endregion Convert to ConcurrentBag

        var ListTask = new ConcurrentBag<Task>();

        ListBagSpeak = new ConcurrentBag<Tuple<string, long>>();
        Parallel.ForEach(bag, itemPath =>
        {
          //var newTask = Task.Run(async () =>
          var newTask = Task.Run(() =>
          {
            //if (ct.IsCancellationRequested) return;
            var lenspeak = GetContentLengthSpeak(itemPath.Item1);
            ListBagSpeak.Add(new Tuple<string, long>(itemPath.Item1, lenspeak));
          }
          );

          ListTask.Add(newTask);
        }
        );
        Task.WaitAll(ListTask.ToArray());

        length = ListBagSpeak.Sum(item=>item.Item2);

        #endregion Get Speak News length
      }

      #region Stub
      [DataMember]
      public override bool CanRead
      {
        get
        {
          return true;
        }
      }

      [DataMember]
      public override bool CanWrite
      {
        get
        {
          return false;
        }
      }

      [DataMember]
      public override bool CanSeek
      {
        get
        {
          return false;
        }
      }

      [DataMember]
      public override long Length
      {
        get
        {
          return length;
        }
      }

      [DataMember]
      public override long Position
      {
        get
        {
          return position;
        }

        set
        {
          position = value;
        }
      }

      #endregion Stub

      private async Task<long> GetContentLengthSpeakAsync(string speakurl)
      {
        long ContentLength;
        HttpWebRequest myHttpWebRequest;

        try
        {
          myHttpWebRequest = (HttpWebRequest)WebRequest.Create(speakurl);
          using (var myHttpWebResponse = (HttpWebResponse)await myHttpWebRequest.GetResponseAsync())
          {
            ContentLength = myHttpWebResponse.ContentLength;
          }
        }
        catch (Exception)
        {
          ContentLength = 0;
        }

        return ContentLength;
      }

      private long GetContentLengthSpeak(string speakurl)
      {
        long ContentLength;
        HttpWebRequest myHttpWebRequest;

        try
        {
          myHttpWebRequest = (HttpWebRequest)WebRequest.Create(speakurl);
          using (var myHttpWebResponse = myHttpWebRequest.GetResponse() as HttpWebResponse)
          {
            ContentLength = myHttpWebResponse.ContentLength;
          }
        }
        catch (Exception)
        {
          ContentLength = 0;
        }

        return ContentLength;
      }

      //Решить вопрос с правильным вычислением диапазонов с учётом длины файла
      public override int Read(byte[] buffer, int offset, int count)
      {
        #region Define vars
        int readCount;
        string DownloadUrl;
        #endregion Define vars

        if (Range1 == -1 && Range2 == -1) return 0;

        #region Calc current range
        long newRange1 = Range1 + position;
        long newRange2 = newRange1 + count - 1;
        if (newRange2 > this.length)
          newRange2 = this.length - 1;
        #endregion Calc current range

        if (newRange1 >= length) return 0;

        #region Read buffer
        var taskReadBuffer = Task.Run(async () =>
        {
          byte[] MultimediaFileBuffer;

          try
          {
            DownloadUrl = ListBagSpeak.ToArray()[0].Item1;

            var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(DownloadUrl);
            myHttpWebRequest.AddRange(newRange1, newRange2);

            using (var myHttpWebResponse = (HttpWebResponse)await myHttpWebRequest.GetResponseAsync())
            {
              using (var streamResponse = myHttpWebResponse.GetResponseStream())
              {
                using (var result = new MemoryStream())
                {
                  await streamResponse.CopyToAsync(result);
                  result.Position = 0;
                  byte[] temp = new byte[count];
                  readCount = await result.ReadAsync(temp, 0, count);
                  MultimediaFileBuffer = new byte[readCount];
                  Array.Copy(temp, 0, MultimediaFileBuffer, 0, readCount);
                }
              }
            }
          }
          catch (Exception ex)
          {
            MultimediaFileBuffer = null;
          }

          return MultimediaFileBuffer;
        }
        );
        taskReadBuffer.Wait();
        if (taskReadBuffer.Result != null)
        {
          taskReadBuffer.Result.CopyTo(buffer, 0);
          readCount = taskReadBuffer.Result.Length;
          position += readCount;
        }
        else
        {
          readCount = 0;
        }
        #endregion Read buffer
        
        return readCount;
      }

      public override void Write(byte[] buffer, int offset, int count)
      {
        length = buffer.Length;
        base.Write(buffer, offset, count);
      }

      public override long Seek(long offset, SeekOrigin origin)
      {
        return 0;
      }

      public override void SetLength(long value)
      {
      }

      public override void Flush()
      {
      }

      protected override void Dispose(bool disposing)
      {
        base.Dispose(disposing);
      }
    }

    private MemoryStream SetNotImplementedMultimedia()
    {
      //var returnValue = new Tuple<Stream, string>(new MemoryStream(), "Not Implemented");
      //return returnValue;
      return new MemoryStream();
    }

    public async Task<Stream> GetMultimedia(string AccountKey, string IdJob, long Range1, long Range2)
    {
      #region Define vars
      string IdSource;
      int IdTypeMultimediaSource; string IdItem;
      string UserToken, UserTokenClient;
      long IdItemLength;
      string AccessToken;
      MemoryStream returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey, enumTypeUser.Remote);
        CheckGoodString("IdJob", IdJob);

        #region Get data from IdJob
        var fieldNameCache = "field_" + IdJob;
        var cache = CacheConnection.GetDatabase();
        var hashFields = await cache.HashGetAllAsync(fieldNameCache, CommandFlags.None);
        if (hashFields == null) throw new ArgumentException("IdJob is bad", "IdJob");
        if (hashFields.Count() != 6 ) throw new ArgumentException("IdJob is bad", "IdJob");

        var List = new StringDictionary();
        foreach (var item in hashFields)
        {
          List.Add(item.Name, item.Value);
        }

        UserToken = List["UserToken"];
        IdSource = List["IdSource"];
        IdItem = List["IdItem"];
        UserTokenClient = List["UserTokenClient"];
        IdItemLength = Convert.ToInt64(List["IdItemLength"]);
        AccessToken = List["AccessToken"];

        int.TryParse(IdSource, out IdTypeMultimediaSource);
        #endregion Get data from IdJob

        #endregion Check incoming parameters

        var IdParentTypeMultimediaSource = await GetIdParentTypeMultimediaSourceAsync(IdTypeMultimediaSource);

        #region Switch IdParentTypeMultimediaSource
        switch (IdParentTypeMultimediaSource)
        {
          case 10: //Favorites
            returnValue = SetNotImplementedMultimedia();
            break;
          case 11: //Home
            returnValue = await GetMultimediaFileBufferHome(UserToken, UserTokenClient, IdItem, IdJob, Range1, Range2);
            break;
          case 13: //Audiobooks
            returnValue = SetNotImplementedMultimedia();
            break;
          case 14: //News
            returnValue = GetMultimediaFileBufferNews(UserToken, IdItem, IdItemLength, IdJob, Range1, Range2);
            break;
          case 15: //Learning
            returnValue = SetNotImplementedMultimedia();
            break;
          case 19: //OneDrive
            returnValue = GetMultimediaFileBufferOneDrive(UserToken, IdItem, IdItemLength, IdJob, Range1, Range2, AccessToken);
            break;
          case 20: //Google Drive
            returnValue = GetMultimediaFileBufferGoogleDrive(UserToken, IdItem, IdItemLength, IdJob, Range1, Range2, AccessToken);
            break;
          case 21: //VKontakte
            returnValue = await GetMultimediaFileBufferVKontakte(UserToken, IdItem, IdItemLength, IdJob, Range1, Range2, IdTypeMultimediaSource, AccessToken);
            break;
          case 22: //iCloud
            returnValue = SetNotImplementedMultimedia();
            break;
          case 23: //Dropbox
            returnValue = await GetMultimediaFileBufferDropbox(UserToken, IdItem, IdItemLength, IdJob, Range1, Range2, AccessToken);
            break;
          case 24: //YandexDisk
            returnValue = await GetMultimediaFileBufferYandexDisk(UserToken, IdItem, IdItemLength, IdJob, Range1, Range2, AccessToken);
            break;
          default:
            throw new ArgumentException("Id is bad", "IdParentTypeMultimediaSource");
        }
        #endregion Switch IdParentTypeMultimediaSource
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        returnValue = new MemoryStream();
        Trace.TraceError("GetMultimedia with Parameter: " + ex.ToString() + ", IdJob=" + (IdJob ?? ""));
      }
      catch (Exception ex)
      {
        returnValue = new MemoryStream();
        Trace.TraceError("GetMultimedia: " + ex.ToString() + ", IdJob=" + (IdJob ?? ""));
      }
      #endregion Catch

      return returnValue;
    }

    public async Task<Tuple<LocalizationElement, string>> GetTypeMultimedia(string AccountKey, string Language)
    {
      #region Define vars
      string TypeMultimediaItem, TitleMultimediaItem, ExtensionMultimediaItem;
      bool IsTranslateTitle, IsLocalizeTitle;
      LocalizationElement LocalizationTypeMultimedia;
      Tuple<LocalizationElement, string> returnValue;
      #endregion Define vars

      #region Try
      try
      {
        #region Check incoming parameters
        CheckServerAccountKey(AccountKey);

        #region Check Language
        Language = ParseLanguage(Language);
        #endregion Check Language

        #endregion Check incoming parameters

        #region Get multimedia extensions
        var TableMultimediaExtension = await LiveMultimediaDL.SelectMultimediaExtensions();

        var ListMultimediaExtension = new List<Tuple<string, string, string, bool, bool>>();

        #region Fill ListMultimediaExtension
        foreach (DataRow RowMultimediaExtension in TableMultimediaExtension.Rows)
        {
          TypeMultimediaItem = Convert.ToString(RowMultimediaExtension.ItemArray[0]);
          TitleMultimediaItem = Convert.ToString(RowMultimediaExtension.ItemArray[1]);
          ExtensionMultimediaItem = Convert.ToString(RowMultimediaExtension.ItemArray[2]);
          IsLocalizeTitle = Convert.ToBoolean(RowMultimediaExtension.ItemArray[3]);
          IsTranslateTitle = Convert.ToBoolean(RowMultimediaExtension.ItemArray[4]);

          ListMultimediaExtension.Add(new Tuple<string, string, string, bool, bool>(TypeMultimediaItem, TitleMultimediaItem, ExtensionMultimediaItem, IsLocalizeTitle, IsTranslateTitle));
        }
        #endregion Fill ListMultimediaExtension

        var GroupedListMultimediaExtension = ListMultimediaExtension.GroupBy(MultimediaExtension => MultimediaExtension.Item1);

        string FullMultimediaFileExtension = "";
        foreach (var item in GroupedListMultimediaExtension)
        {
          #region Localization
          TitleMultimediaItem = item.ToList()[0].Item2;
          IsLocalizeTitle = item.ToList()[0].Item4;
          IsTranslateTitle = item.ToList()[0].Item5;

          if (IsLocalizeTitle)
          {
            var returnTypeMultimediaItem = await GetLocalizationElementAsync(Language, enumTopic.Service, "TypeMultimediaItem_" + item.Key, IsTranslateTitle, TitleMultimediaItem);
            if (JetSASLibrary.CheckGoodString(returnTypeMultimediaItem.Item2))
            {
              TraceService("GetTypeMultimedia", enumTypeLog.Error, "Language=" + (Language ?? "") + ", Exception=" + returnTypeMultimediaItem.Item2);
            }
            TypeMultimediaItem = returnTypeMultimediaItem.Item1;

          }
          else
          {
            TypeMultimediaItem = TitleMultimediaItem;
          }
          #endregion Localization

          FullMultimediaFileExtension += TypeMultimediaItem + "=";

          foreach (var item2 in item)
          {
            FullMultimediaFileExtension += item2.Item3 + "|";
          }
          FullMultimediaFileExtension = FullMultimediaFileExtension.Remove(FullMultimediaFileExtension.Length - 1, 1);
          FullMultimediaFileExtension += "&";
        }
        FullMultimediaFileExtension = FullMultimediaFileExtension.Remove(FullMultimediaFileExtension.Length - 1, 1);
        #endregion Get multimedia extensions

        LocalizationTypeMultimedia = new LocalizationElement();
        LocalizationTypeMultimedia.ElementName = "ExtensionMultimedia";
        LocalizationTypeMultimedia.ElementValue = FullMultimediaFileExtension;

        var ErrorMessage = "";
        returnValue = new Tuple<LocalizationElement, string>(LocalizationTypeMultimedia, ErrorMessage);
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        LocalizationTypeMultimedia = new LocalizationElement();
        returnValue = new Tuple<LocalizationElement, string>(LocalizationTypeMultimedia, ex.Message);
        Trace.TraceError("GetTypeMultimedia with Parameter: " + ex.ToString() + ", Language=" + (Language ?? ""));
        TraceService("GetTypeMultimedia", enumTypeLog.Error, ex.ToString());
      }
      catch (Exception ex)
      {
        LocalizationTypeMultimedia = new LocalizationElement();
        returnValue = new Tuple<LocalizationElement, string>(LocalizationTypeMultimedia, ex.Message);
        Trace.TraceError("GetTypeMultimedia: " + ex.ToString() + ", Language=" + (Language ?? ""));
        TraceService("GetTypeMultimedia", enumTypeLog.Error, ex.ToString());
      }
      #endregion Catch
      
      return returnValue;
    }

    private Tuple<string, string> CollectErrorServicePrepare()
    {
      var GuidError = Guid.NewGuid().ToString();
      var returnValue = "An Error. Please contact support@live-mm.com with error number=" + GuidError;
      return new Tuple<string, string>(GuidError, returnValue);
    }

    private string CollectErrorService(string Message)
    {
      var newCollectErrorService = CollectErrorServicePrepare();
      Trace.TraceError(string.Format("CollectErrorService: GuidError={0} Message={1}", newCollectErrorService.Item1, Message));
      return newCollectErrorService.Item2;
    }

    private string CollectErrorService(Exception ex)
    {
      var newCollectErrorService = CollectErrorServicePrepare();
      Trace.TraceError(string.Format("CollectErrorService: GuidError={0} Exception={1}", newCollectErrorService.Item1, ex.ToString()));
      return newCollectErrorService.Item2;
    }

    private string CollectErrorService(string Message, Exception ex)
    {
      var newCollectErrorService = CollectErrorServicePrepare();
      Trace.TraceError(string.Format("CollectErrorService: GuidError={0} Message={1} Exception={2}", newCollectErrorService.Item1, Message, ex.ToString()));
      return newCollectErrorService.Item2;
    }

    //public async Task<MultimediaSource[]> GetSourceItems(string Language = null, string Source = null, string GroupBy = null, string OrderBy = null, string UserToken = null)
    //{
    //  #region Define vars

    //  string UserTokenServer = UserToken;
    //  MultimediaSource[] ListMultimediaItem;
    //  string IdSource;
    //  #endregion Define vars

    //  #region Check incoming parameters
    //  if (!CheckGoodString(Language)) Language = DefaultLanguage;
    //  if (!await CheckLanguageAsync(Language)) Language = DefaultLanguage;
    //  #endregion Check incoming parameters

    //  try
    //  {
    //    #region Try
    //    if (!CheckGoodString(Source))
    //    {
    //      #region Get default list sources
    //      ListMultimediaItem = await GetSourceItemsDefaultAsync(Language);
    //      #endregion Get top list Sources
    //    }
    //    else
    //    {
    //      #region Check incoming parameters
    //      if (!CheckGoodString(UserTokenServer)) throw new ArgumentException("UserTokenServer is empty", "UserTokenServer");
    //      if (!CheckFormatGUID(UserTokenServer)) throw new ArgumentException("UserTokenServer has bad format", "UserTokenServer");
    //      if (!await CheckUserToken(UserTokenServer, enumTypeUser.Remote)) throw new ArgumentException("UserTokenServer not found", "CheckUserToken");
    //      #endregion Check incoming parameters

    //      #region Read Cache

    //      //IDatabase cache = connection.GetDatabase();
    //      //if (await cache.KeyExistsAsync(id, CommandFlags.None))
    //      //{
    //      //  RedisValue[] rvb = await cache.SetMembersAsync(id, CommandFlags.None);
    //      //  await cache.KeyDeleteAsync(id, CommandFlags.None);

    //      //  IdSource = rvb[0];
    //      //  if (!CheckGoodString(IdSource)) throw new ArgumentException("Source is empty", "Source");
    //      //}
    //      //else
    //      //{
    //      //  throw new ArgumentException("id=" + id + " is not exists in the Cache", "id");
    //      //}

    //      #endregion Read Cache

    //      #region Define Source

    //      var aSource = Source.Split(new Char[] { '.' });
    //      var SelectedSource = aSource[0].ToLower();

    //      switch (SelectedSource)
    //      {
    //        case "favorites":
    //          ListMultimediaItem = null;
    //          break;
    //        case "home":
    //          ListMultimediaItem = null;
    //          break;
    //        case "cloud":
    //          ListMultimediaItem = null;
    //          break;
    //        case "audiobooks":
    //          ListMultimediaItem = null;
    //          break;
    //        case "news":
    //          ListMultimediaItem = null;
    //          break;
    //        case "learning":
    //          ListMultimediaItem = null;
    //          break;
    //        default:
    //          ListMultimediaItem = null;
    //          break;
    //      }
    //      #endregion Define Source
    //    }
    //    #endregion Try
    //  }
    //  catch (Exception)
    //  {
    //    #region Catch
    //    ListMultimediaItem = null;
    //    throw;
    //    #endregion Catch
    //  }

    //  return ListMultimediaItem;
    //}

    //public async Task SetSourceOrder(string AccountKey, string Source, string OrderBy, string UserToken)
    //{
    //  // Метод для установки сортировки источников для UserToken
    //  await Task.Delay(1);
    //}

    #endregion New getting source

    #region Write Log
    public async Task Tracing(string AccountKey, enumTypeLog TypeLog, string Scope, string Procedure, string Message, string ClientIp = null, int? ClientPort = null, string UserToken = null)
    {
      switch (TypeLog)
      {
        case enumTypeLog.Information:
          await TraceInformation(AccountKey, Scope, Procedure, Message, ClientIp, ClientPort, UserToken);
          break;
        case enumTypeLog.Warning:
          await TraceWarning(AccountKey, Scope, Procedure, Message, ClientIp, ClientPort, UserToken);
          break;
        case enumTypeLog.Error:
          await TraceError(AccountKey, Scope, Procedure, Message, ClientIp, ClientPort, UserToken);
          break;
      }
    }

    public async Task TraceInformation(string AccountKey, string Scope, string Procedure, string Message, string ClientIp=null, int? ClientPort=null, string UserToken = null)
    {
      await TraceAsync(AccountKey, Scope, Procedure, enumTypeLog.Information, ClientIp, ClientPort, Message, UserToken);
    }

    public async Task TraceWarning(string AccountKey, string Scope, string Procedure, string Message, string ClientIp = null, int? ClientPort = null, string UserToken = null)
    {
      await TraceAsync(AccountKey, Scope, Procedure, enumTypeLog.Warning, ClientIp, ClientPort, Message, UserToken);
    }

    public async Task TraceError(string AccountKey, string Scope, string Procedure, string Message, string ClientIp = null, int? ClientPort = null, string UserToken = null)
    {
      await TraceAsync(AccountKey, Scope, Procedure, enumTypeLog.Warning, ClientIp, ClientPort, Message, UserToken);
    }

    #endregion Write Log

  // ----------------------------------

  #region Private functions

    private static Dictionary<string, string> deserializeJson(string json)
    {
      var jss = new JavaScriptSerializer();
      var d = jss.Deserialize<Dictionary<string, string>>(json);
      return d;
    }

    private bool CheckFormatIpAddress(string IpAddress)
    {
      bool IsIpAddress;

      try
      {
        //if (!CheckGoodString(IpAddress)) throw new ArgumentException("IpAddress is empty", "IpAddress");
        var ParseAddress = IPAddress.Parse(IpAddress);

        IsIpAddress = true;
      }
      catch (Exception)
      {
        IsIpAddress = false;
      }

      return IsIpAddress;
    }

    private RemoteEndpointMessageProperty GetEndPointIpAddress()  
    {      
      System.ServiceModel.OperationContext context = System.ServiceModel.OperationContext.Current;
      MessageProperties messageProperties = context.IncomingMessageProperties;
      RemoteEndpointMessageProperty endpointProperty = messageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
      
      return endpointProperty;
    }

    private static List<MultimediaServer> LoadMultimediaServer()
    {
      #region Define vars
      List<MultimediaServer> ListMultimediaServer = new List<MultimediaServer>();
      MultimediaServer MultimediaServerItem;      
      #endregion Define vars

      #region Temp define data instead DataLayer      

      // Service
      MultimediaServerItem = new MultimediaServer();
      MultimediaServerItem.IdMultimediaServer = 1;
      MultimediaServerItem.AccountKey = "cS58AiAyseZwkV1mRN6ISp6Q6IAE47pMv3F2f8o1PXx";
      MultimediaServerItem.TypeUser = enumTypeUser.Local;

      ListMultimediaServer.Add(MultimediaServerItem);

      // Local Server Windows 7            
      MultimediaServerItem = new MultimediaServer();
      MultimediaServerItem.IdMultimediaServer = 2;
      MultimediaServerItem.AccountKey = "Fy28x7p4G7Qa9ZT99bT755a8EByXZb2ZtnE1YoN62Yr";
      MultimediaServerItem.TypeUser = enumTypeUser.Local;

      ListMultimediaServer.Add(MultimediaServerItem);

      // Remote web-server      
      MultimediaServerItem = new MultimediaServer();
      MultimediaServerItem.IdMultimediaServer = 3;
      MultimediaServerItem.AccountKey = "KdzzlWy5FNCIED26Las5Cz53nYr781y744fQmKbILH5";
      MultimediaServerItem.TypeUser = enumTypeUser.Remote;

      ListMultimediaServer.Add(MultimediaServerItem);

      #endregion Temp define data instead DataLayer

      //Для "посмотреть"
      //int minWorker, minIOC;
      //ThreadPool.GetMinThreads(out minWorker, out minIOC);

      ////int maxWorker, maxIOC;
      ////ThreadPool.GetMaxThreads(out maxWorker, out maxIOC);

      ////int workerThreads, completionPortThreads;
      ////ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

      return ListMultimediaServer;
    }

    private static long GetIdUserDemo()
    {
      #region Define vars
      long IdUserDemo;
      #endregion Define vars

      try
      {
        IdUserDemo = LiveMultimediaDL.GetIdUserDemo();
        if (IdUserDemo <= 0) throw new ArgumentException("Error getting IdUserDemo", "IdUserDemo");
      }
      catch (Exception ex)
      {
        throw;
      }

      return IdUserDemo;
    }

    private MultimediaServer GetMultimediaServerItem(string AccountKey)
    {
      #region Define vars      
      MultimediaServer FoundMultimediaServerItem;
      #endregion Define vars
    
      try
      {
        FoundMultimediaServerItem = ListMultimediaServer.Single(MultimediaServerItem => MultimediaServerItem.AccountKey == AccountKey);
      }
      catch (Exception)
      {
        FoundMultimediaServerItem = null;
      }

      return FoundMultimediaServerItem;
    }

    #region Trace

    private class DebugWriteElapsed:IDisposable
    {
      private bool IsDebugWrite;
      private Stopwatch CountTime=null;

      public DebugWriteElapsed(bool IsDebugWrite)
      {
        this.IsDebugWrite = IsDebugWrite;

        if (this.IsDebugWrite)
        {
          CountTime = new Stopwatch();
          CountTime.Start();
        }
      }

      public void Stop()
      {
        if (this.IsDebugWrite)
          if (CountTime.IsRunning) CountTime.Stop();
      }

      public void Restart()
      {
        if (this.IsDebugWrite)
          if (!CountTime.IsRunning) CountTime.Restart();
      }

      public void WriteLine(string format, params object[] args)
      {
        if (this.IsDebugWrite)
        {
          if (CountTime.IsRunning)
          {
            CountTime.Stop();
            Debug.Indent();
            Debug.Write(string.Format("{0} Elapsed={1} ", DateTime.Now, CountTime.ElapsedMilliseconds));
            Debug.WriteLine(format, args);
            Debug.Unindent();
          }
          else
          {
            Debug.Indent();
            Debug.Write(string.Format("{0} ", DateTime.Now));
            Debug.WriteLine(format, args);
            Debug.Unindent();
          }
        }
      }

      public void Dispose()
      {
        if (CountTime != null)
        {
          if (CountTime.IsRunning) CountTime.Stop();          
        }          
      }
    }

    private void TraceDebugWriteLine(string format, params object[] args)
    {
      if (IsDebugWrite)
        Debug.WriteLine(format, args);
    }

    private async Task<string> TraceAsync(string AccountKey, string Scope, string Procedure, enumTypeLog IdTypeLog, string ClientIp=null, int? ClientPort=null, string Message=null, string UserToken = null)
    {
      #region Define vars
      enumTypeUser? TypeUser;
      MultimediaServer MultimediaServerItem;
      int IdMultimediaServer;
      string MessageError;
      string ServerIp; int ServerPort;
      #endregion Define vars
     
      #region Try
      try
      {
        #region Check incoming parameters

        #region Common check
        CheckServerAccountKey(AccountKey);
        if (!CheckGoodString(Scope)) throw new ArgumentException("Scope is empty", "Scope");
        if (!CheckGoodString(Procedure)) throw new ArgumentException("Procedure is empty", "Procedure");
        #endregion Common check

        #region Get MultimediaServerItem
        MultimediaServerItem = GetMultimediaServerItem(AccountKey);
        if (MultimediaServerItem == null) throw new ArgumentException("Multimedia Server is null, but check AccountKey is good. E-mail to support@live-mm.com", "TypeUser");
        IdMultimediaServer = MultimediaServerItem.IdMultimediaServer;
        TypeUser = MultimediaServerItem.TypeUser;
        #endregion Get MultimediaServerItem

        #region Check Client info
        if (TypeUser == enumTypeUser.Remote)
        {
          if (!CheckGoodString(ClientIp)) throw new ArgumentException("ClientIp is empty", "ClientIp");
         if (!CheckFormatIpAddress(ClientIp)) throw new ArgumentException("ClientIp is bad", "ClientIp");
          if (ClientPort!=null)
          {
            if (ClientPort <= 0) throw new ArgumentException("ClientPort is bad", "ClientPort");
          }          
        }
        else
        {
          ClientIp = null; ClientPort = null;
        }
        #endregion Check Client info
                
        #region Check UserToken
        //if (CheckGoodString(UserToken))
        //{
        //  if (!CheckFormatGUID(UserToken)) throw new ArgumentException("UserToken has bad format", "UserToken");
        //  if (!await CheckUserToken(UserToken, enumTypeUser.Remote)) throw new ArgumentException("UserToken not found", "UserToken");
        //}
        #endregion Check UserToken

        #endregion Check incoming parameters

        #region Get Server Info
        if (IdMultimediaServer != 1) // Not Service
        {
          var EndPointInfo = GetEndPointIpAddress();

          ServerIp = EndPointInfo.Address;
          ServerPort = EndPointInfo.Port;
        }
        else // Need to define Service's IP
        {
          //String host = System.Net.Dns.GetHostName();
          //IPAddress[] a = System.Net.Dns.GetHostEntry(host).AddressList;
          //System.Net.IPAddress ip = System.Net.Dns.GetHostByName(host).AddressList[1]; //вот тут 0 на 1 изменил
          // Показ адреса в label'е.
          //label1.Text = ip.ToString();

          ServerIp = null;
          ServerPort = 0;          
        }
        #endregion Get Server Info

        await LiveMultimediaDL.TraceAsync(IdMultimediaServer, IdTypeLog, ServerIp, ServerPort, ClientIp, ClientPort, Scope, Procedure, Message, UserToken);

        MessageError = "";
      }
      #endregion Try

      #region Catch
      catch (ArgumentException ex)
      {
        MessageError = ex.Message;
        Trace.TraceError("TraceAsync with Parameter: " + ex.ToString() + ", AccountKey=" + (AccountKey ?? "") + ", Scope=" + (Scope ?? "") + ", Procedure=" + (Procedure ?? "") + ", UserToken=" + (UserToken ?? ""));
      }

      catch (Exception ex)
      {
        MessageError = "Exception of Tracing";
        TraceLogError(UserToken, "TraceAsync", CollectErrorService(ex) + ", AccountKey=" + (AccountKey ?? "") + ", Scope=" + (Scope ?? "") + ", Procedure=" + (Procedure ?? "") + ", UserToken=" + (UserToken ?? ""));
      }
      #endregion Catch

      return MessageError;
    }

    private async Task TraceServiceAsync(string Procedure, enumTypeLog IdTypeLog, string Message=null, string UserToken=null)
    {
      await TraceAsync(ServerAccountKey, "Service", Procedure, IdTypeLog, null, null, Message, UserToken);
    }

    private void TraceService(string Procedure, enumTypeLog IdTypeLog, string Message = null, string UserToken = null)
    {
      TraceAsync(ServerAccountKey, "Service", Procedure, IdTypeLog, null, null, Message, UserToken);
    }

  //private async Task WriteLogAsync(string UserToken, enumTypeUser TypeUser, string Site, string Procedure, enumTypeLog IdTypeLog, string Message, string ServerIp = null, int ServerPort = 0, string ClientIp = null, int ClientPort = 0)
  //{
  //  #region Define vars
  //  Exception exceptionMessage;
  //  #endregion Define vars

  //  try
  //  {
  //    #region Check incoming parameters
  //    if (TypeUser == enumTypeUser.Remote)
  //    {
  //      if (!CheckGoodString(ServerIp)) throw new ArgumentException("ServerIp is empty", "ServerIp");
  //      if (ServerPort <= 0) throw new ArgumentException("ServerIp is empty", "ServerIp");
  //      if (!CheckGoodString(ClientIp)) throw new ArgumentException("ClientIp is empty", "ClientIp");
  //      if (ClientPort <= 0) throw new ArgumentException("ServerIp is empty", "ServerIp");
  //    }
  //    #endregion Check incoming parameters

  //    #region Get client info

  //    if (TypeUser == enumTypeUser.Local)
  //    {
  //      System.ServiceModel.OperationContext context = System.ServiceModel.OperationContext.Current;
  //      MessageProperties messageProperties = context.IncomingMessageProperties;
  //      RemoteEndpointMessageProperty endpointProperty = messageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
  //      ServerIp = "";
  //      ServerPort = 0;
  //      ClientIp = endpointProperty.Address;
  //      ClientPort = endpointProperty.Port;
  //    }
  //    else
  //    {
  //      if (!CheckGoodString(ServerIp)) throw new ArgumentException("ServerIp is empty", "ServerIp");
  //      if (ServerPort<=0) throw new ArgumentException("ServerIp is empty", "ServerIp");
  //      if (!CheckGoodString(ClientIp)) throw new ArgumentException("ClientIp is empty", "ClientIp");
  //      if (ClientPort<=0) throw new ArgumentException("ServerIp is empty", "ServerIp");
  //    }
  //    #endregion Get client info

  //    #region Write log
  //    LiveMultimediaDL DataLayer = new LiveMultimediaDL();
  //    //exceptionMessage = await LiveMultimediaDataLayer.LiveMultimediaDL.WriteLog(UserToken, IdTypeUser, ClientIp, ClientPort, Site, Procedure, IdTypeLog, Message);
  //    exceptionMessage = await LiveMultimediaDataLayer.LiveMultimediaDL.TraceAsync(Site, Procedure, IdTypeLog, Message, ServerIp, ServerPort, ClientIp, ClientPort, UserToken);
  //    if (exceptionMessage != null)
  //    {
  //      throw new AggregateException("LiveMultimediaDataLayer.LiveMultimediaDL.WriteLog", exceptionMessage);
  //    }
  //    #endregion Write log
  //  }
  //  catch (ArgumentException ex)
  //  {
  //    Trace.TraceError("GetSource with Parameter: " + ex.ToString() + ", UserToken=" + UserToken ?? "");
  //  }
  //  catch (Exception ex)
  //  {
  //    TraceLogError(UserToken, "WriteLogAsync", ex.ToString());
  //  }
  //}

  private void TraceLogError(string UserToken, string Procedure, string Message)
  {
    Trace.TraceError("UserToken=" + (UserToken ?? "") + ", Error in " + (Procedure ?? "") + ", ErrorMessage:" + (Message ?? ""));
  }
  #endregion Trace
   
  private async Task<string[]> GetSettings(string AccountKey, string UserToken, enumTypeUser TypeUser)
  {
    #region Define vars
    List<string> ListSettings;
    #endregion Define vars

    #region Try
    try
    {
      #region Check incoming parameters
      CheckServerAccountKey(AccountKey, TypeUser);
      await FullCheckUserToken("UserToken", UserToken, TypeUser);
      #endregion Check incoming parameters

      ListSettings = new List<string>();

      #region Switch
      switch (TypeUser)
      {
        #region Local
        case enumTypeUser.Local:
          ListSettings.Add(Convert.ToString(CloudConfigurationManager.GetSetting("MultimediaFileBufferLength")));
          ListSettings.Add(Convert.ToString(CloudConfigurationManager.GetSetting("DelayRequestMilliseconds")));
          ListSettings.Add(Convert.ToString(CloudConfigurationManager.GetSetting("DelayMaxSeconds")));
          ListSettings.Add(Convert.ToString(CloudConfigurationManager.GetSetting("DelayErrorSeconds")));
          ListSettings.Add(Convert.ToString(CloudConfigurationManager.GetSetting("IdleTimeSeconds")));
          ListSettings.Add(Convert.ToString(CloudConfigurationManager.GetSetting("MaxConnection")));
          break;
        #endregion Local

        #region Remote
        case enumTypeUser.Remote:
          ListSettings.Add(Convert.ToString(CloudConfigurationManager.GetSetting("WaitTimeReplySeconds")));
          break;
        #endregion Remote

        default:
          break;
      }
      #endregion Switch
    }
    #endregion Try

    #region Catch
    catch (ArgumentException ex)
    {
      ListSettings = new List<string>();
      TraceLogError(UserToken, "LiveMultimediaService.GetSettings with Parameter", ex.ToString());
    }
    catch (Exception ex)
    {
      ListSettings = new List<string>();
      TraceLogError(UserToken, "LiveMultimediaService.GetSettings", "TypeUser=" + TypeUser.ToString() + ", Exception: " + ex.ToString());
    }
    #endregion Catch

    return ListSettings.ToArray();
  }

  private static string[] GetSmtpParameters()
  {
    string[] ListSettings;

    try
    {
      ListSettings = new string[8];
      ListSettings[0] = CloudConfigurationManager.GetSetting("SmtpFrom") as string;
      ListSettings[1] = CloudConfigurationManager.GetSetting("SmtpBcc") as string;
      ListSettings[2] = CloudConfigurationManager.GetSetting("SmtpHost") as string;
      ListSettings[3] = CloudConfigurationManager.GetSetting("SmtpPort") as string;
      ListSettings[4] = CloudConfigurationManager.GetSetting("SmtpEnableSsl") as string;
      ListSettings[5] = CloudConfigurationManager.GetSetting("SmtpDefaultCredentials") as string;
      ListSettings[6] = CloudConfigurationManager.GetSetting("SmtpUserName") as string;
      ListSettings[7] = CloudConfigurationManager.GetSetting("SmtpPassword") as string;
    }
    catch (Exception ex)
    {
      ListSettings = null;
      Trace.TraceError("Error in LiveMultimediaService.GetSmtpParameters: " + ex.ToString());
    }

    return ListSettings;
  }

  private void CheckServerAccountKey(string AccountKey)
  {
    try
    {
      var FoundServerAccountKey = ListMultimediaServer.Single(MultimediaServerItem => MultimediaServerItem.AccountKey == AccountKey);
    }
    catch (ArgumentNullException)
    {
      throw new ArgumentException("AccountKey is empty", "AccountKey");
    }
    catch (InvalidOperationException)
    {
      throw new ArgumentNullException("AccountKey is bad", "AccountKey");
    }
    catch (Exception)
    {
      throw;
    }
  }
 
    private void CheckServerAccountKey(string AccountKey, enumTypeUser TypeUser)
    {
      try
      {
        var FoundServerAccountKey = ListMultimediaServer.Single(MultimediaServerItem => (MultimediaServerItem.AccountKey == AccountKey && MultimediaServerItem.TypeUser == TypeUser));
      }
      catch (ArgumentNullException)
      {
        throw new ArgumentException("AccountKey is empty", "AccountKey");
      }
      catch (InvalidOperationException)
      {
        throw new ArgumentNullException("AccountKey is bad", "AccountKey");
      }
      catch (Exception)
      {
        throw new ArgumentException("AccountKey is bad", "AccountKey");
      }
    }

    private bool CheckGoodString(string stringCheck)
  {
    bool IsSuccess = (!string.IsNullOrEmpty(stringCheck) && !string.IsNullOrWhiteSpace(stringCheck));
    return IsSuccess;
  }

  private void CheckGoodString(string ParameterName, string stringCheck)
  {
    bool IsSuccess = (!string.IsNullOrEmpty(stringCheck) && !string.IsNullOrWhiteSpace(stringCheck));
    if (!IsSuccess) throw new ArgumentException(ParameterName + " is empty", ParameterName);
  }

  private bool CheckFormatGUID(string sGUID)
  {
    Guid ResultGUID;
    bool IsSuccess = Guid.TryParseExact(sGUID, "D", out ResultGUID);
    return IsSuccess;
  }

  private void CheckFormatGUID(string ParameterName, string sGUID)
  {
      if (!CheckFormatGUID(sGUID)) throw new ArgumentException(ParameterName+" is bad", ParameterName);
  }

    //private async Task<bool> CheckUserToken(string UserToken, enumTypeUser TypeUser)
    //{
    //  bool IsSuccess;
    //  try
    //  {
    //    long IdUser = await LiveMultimediaDataLayer.LiveMultimediaDL.GetIdUserByUserTokenAsync(UserToken, (int)TypeUser);
    //    if (IdUser > 0)
    //      IsSuccess = true;
    //    else
    //      IsSuccess = false;
    //  }
    //  catch (Exception ex)
    //  {
    //    IsSuccess = false;
    //    TraceLogError(UserToken, "LiveMultimediaService.CheckUserToken", "TypeUser=" + TypeUser.ToString() + ", Exception: " + ex.ToString());
    //  }

    //  return IsSuccess;
    //}

    private async Task<bool> CheckUserToken(string UserToken, enumTypeUser TypeUser)
    {
      #region Define vars
      bool IsSuccess;
      #endregion Define vars

      try
      {
        var IdUser = await LiveMultimediaDL.GetIdUserByUserTokenAsync(UserToken, (int)TypeUser);

        if (IdUser > 0)
          IsSuccess = true;
        else
          IsSuccess = false;
      }
      catch (Exception ex)
      {
        IsSuccess = false;
        TraceService("CheckUserToken", enumTypeLog.Error, ex.ToString(), UserToken);
      }

      return IsSuccess;
    }

    private async Task CheckUserToken(string ParameterName, string UserToken, enumTypeUser TypeUser)
    {
      try
      {
        var IdUser = await LiveMultimediaDL.GetIdUserByUserTokenAsync(UserToken, (int)TypeUser);

        if (IdUser <= 0)
        {
          throw new ArgumentException("UserToken not found", "UserToken");
        }
      }
      catch (Exception ex)
      {
        TraceService("CheckUserToken", enumTypeLog.Error, ex.ToString(), UserToken);
        throw;
      }
    }

    private async Task<bool> CheckIdUserDemoAsync(string UserToken, enumTypeUser TypeUser)
    {
      bool IsSuccess;

      try
      {
        var IdUser = await LiveMultimediaDL.GetIdUserByUserTokenAsync(UserToken, (int)TypeUser);
        if (IdUser <= 0)
        {
          throw new ArgumentException("UserToken not found", "UserToken");
        }
        if (IdUser == IdUserDemo)
          IsSuccess = true;
        else
          IsSuccess = false;
      }
      catch (Exception ex)
      {
        TraceService("CheckIdUserDemo", enumTypeLog.Error, ex.ToString(), UserToken);
        throw;
      }

      return IsSuccess;
    }

    private async Task FullCheckUserToken(string ParameterName, string UserToken, enumTypeUser TypeUser)
  {
    CheckGoodString(ParameterName, UserToken);
    CheckFormatGUID(ParameterName, UserToken);
    await CheckUserToken(ParameterName, UserToken, TypeUser);
  }

  //private static int GetMultimediaFileBufferLength()
  //{
  //  int MultimediaFileMemoryLength = Convert.ToInt32(CloudConfigurationManager.GetSetting("MultimediaFileBufferLength"));
  //  return MultimediaFileMemoryLength;
  //}

  private void ConvertTypeAudio(MultimediaFile mf, string SupportedTypeAudio)
  {
    string filename_temp = PathTargetMusic + @"\" + mf.MultimediaFileGUID + "." + mf.TypeMultimedia + ".temp"; ;
    string filename_support = PathTargetMusic + @"\" + mf.MultimediaFileGUID + "." + SupportedTypeAudio;

    string filename_source = FolderConvertAudio + @"\" + mf.MultimediaFileGUID + "." + mf.TypeMultimedia + ".temp"; ;
    string filename_target = FolderConvertAudio + @"\" + mf.MultimediaFileGUID + "." + SupportedTypeAudio;

    CreateFolder(FolderConvertAudio);

    if (File.Exists(filename_source) == true) { File.Delete(filename_source); }
    if (File.Exists(filename_target) == true) { File.Delete(filename_target); }
    File.Move(filename_temp, filename_source);

    // Конвертация....
    System.Diagnostics.ProcessStartInfo ConvertInfo = new ProcessStartInfo();
    ConvertInfo.FileName = @"C:\LiveMultimediaWork\convertaudio\convertaudio.cmd";
    ConvertInfo.Arguments = filename_source + " " + filename_target;

    System.Diagnostics.Process Convert = new System.Diagnostics.Process();
    Convert = Process.Start(ConvertInfo);
    Convert.WaitForExit();

    File.Move(filename_target, filename_support);
    File.Delete(filename_source);
  }

  private string GetTypeAudio(string DisplayName)
  {
    string[] a = DisplayName.Split(new Char[] { '.' });
    string TypeAudio = a[a.Length - 1].Trim().ToLower();
    return TypeAudio;
  }

  private string GetDisplayName(string FullPath)
  {
    string[] a = FullPath.Split(new Char[] { '\\' });
    string DisplayName = a[a.Length - 1];
    return DisplayName;
  }

  private void CreateFolder(string PathFolder)
  {
    if (Directory.Exists(PathFolder) == false)
    {
      Directory.CreateDirectory(PathFolder);
    }
  }

    #endregion Private functions
  }

  static class LazyConnectionCache
  {
    private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
    {
      return ConnectionMultiplexer.Connect("***.redis.cache.windows.net:6380,password=***,ssl=True,abortConnect=False");                                            
    });

    public static ConnectionMultiplexer Connection
    {
      get
      {
        return lazyConnection.Value;
      }
    }
  }

}
