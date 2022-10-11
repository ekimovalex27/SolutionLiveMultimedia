using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Data;
using System.Web;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net;
using System.Diagnostics;
using System.Collections.Specialized;

using Bing;

using LiveMultimediaData;
using LiveMultimediaOAuth;
using LiveMultimediaREST;

using System.Security.Cryptography;
using System.Net.Http;
using System.ServiceModel.Syndication;

namespace LiveMultimediaDataLayer
{
  public static class LiveMultimediaDL
    {

    #region Hash

    static SHA512 SHA512Hash = SHA512.Create();

#if DEBUG
    public static async Task UpdateHash(long IdUser=0, string Password=null)
    {
      string sqlCommand;

      var TableUsers = new DataTable("Table");
      TableUsers.Columns.Add(new DataColumn("IdUser", typeof(long)));
      TableUsers.Columns.Add(new DataColumn("Username", typeof(string)));
      TableUsers.Columns.Add(new DataColumn("Password", typeof(string)));
      TableUsers.Columns.Add(new DataColumn("SourceData", typeof(string)));
      TableUsers.Columns.Add(new DataColumn("HashData", typeof(string)));

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        if (IdUser > 0)
          sqlCommand = "SELECT IdUser, Username, Password FROM [dbo].[Users] WHERE [IdUser]="+ IdUser.ToString(); //Исправление пароля для заданного пользователя
        else
          sqlCommand = "SELECT IdUser, Username, Password FROM [dbo].[Users]";//Исправление пароля для всех пользователей        
                
        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var readerUsers = await cmd.ExecuteReaderAsync())
        {
          DataRow RowUsers; IDataRecord record;

          while (await readerUsers.ReadAsync())
          {
            record = readerUsers as IDataRecord;

            RowUsers = TableUsers.NewRow();
            RowUsers["IdUser"] = Convert.ToInt64(record["IdUser"]);
            RowUsers["Username"] = record["Username"] as string;

            if (IdUser > 0)
              RowUsers["Password"] = Password;
            else
              RowUsers["Password"] = record["Password"] as string;

            RowUsers["SourceData"] = GetSourceDataForHash(record["Username"] as string, RowUsers["Password"] as string);
            RowUsers["HashData"] = GetSHA512Hash(RowUsers["SourceData"] as string);
            TableUsers.Rows.Add(RowUsers);
          }
        }

        foreach (DataRow row in TableUsers.Rows)
        {
          sqlCommand = "UPDATE Users SET [Password]='" + row["HashData"].ToString() + "' WHERE [IdUser]=" + row["IdUser"].ToString();
          //sqlCommand = "UPDATE Users SET [Note]='" + row["Password"].ToString() + "' WHERE [IdUser]=" + row["IdUser"].ToString();
          
          cmd = new SqlCommand(sqlCommand, cn);
          cmd.CommandType = CommandType.Text;
          //await cmd.ExecuteNonQueryAsync();

          //Debug.WriteLine("{0} {1} {2} {3}", row["IdUser"].ToString(), row["Username"].ToString(), row["Password"].ToString(), sqlCommand);
          Debug.WriteLine(sqlCommand);
        }
      }
    }
#endif

    public static string GetSourceDataForHash(string Username, string Password)
    {
      var SourceData = "Алексей " + Username + " Екимов " + Password + " ООО Джэт САС, Санкт-Петербург. 2012";
      return SourceData;
    }

    public static string GetSHA512Hash(string input)
    {
      // Convert the input string to a byte array and compute the hash.      
      byte[] data = SHA512Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

      // Create a new Stringbuilder to collect the bytes and create a string.
      StringBuilder sBuilder = new StringBuilder();

      // Loop through each byte of the hashed data and format each one as a hexadecimal string.
      for (int i = 0; i < data.Length; i++)
      {
        sBuilder.Append(data[i].ToString("x2"));
      }

      // Return the hexadecimal string.
      return sBuilder.ToString();
    }

    // Verify a hash against a string
    public static bool VerifySHA512Hash(string input, string hash)
    {
      // Hash the input.
      string hashOfInput = LiveMultimediaDL.GetSHA512Hash(input);

      // Create a StringComparer an compare the hashes
      var comparer = StringComparer.OrdinalIgnoreCase;

      if (0 == comparer.Compare(hashOfInput, hash))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    #endregion Hash

    #region Define vars

    private const string FolderConfig = @"C:\LiveMultimediaWork\storage";
      private const string FolderPosition = @"c:\LiveMultimediaWork\position";

    private const string ConnectionString = "Server=tcp:***,1433;Initial Catalog=LiveMultimedia;Persist Security Info=False;User ID=***;Password=***;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

    #endregion Define vars

    #region Email push

    public static async Task<DataTable> GetEmailLanguagesAsync()
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        //var sqlCommand = "SELECT [Language] FROM [dbo].[Users] WHERE [IsEnabled]=1 GROUP BY [Language]";
        var sqlCommand = "SELECT [Language]+','+CONVERT(VARCHAR(7),COUNT([Language])) AS [Language] FROM [dbo].[Users] WHERE [IsEnabled]=1 GROUP BY [Language] ORDER BY COUNT([Language]) DESC";
        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    public static async Task<DataTable> GetEmailPushAsync()
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [IdEmailPush],[Name],[DateUser],[IsCompleted] FROM [EmailPush]";
        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    public static async Task<DataTable> GetEmailLanguageNotInMailAsync(long IdEmailPush)
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [Language] FROM [Users] WHERE [IsEnabled]=1 AND [IsSubscribe]=1 AND [Language] NOT IN (SELECT [Language] FROM [Email] WHERE [IdEmailPush]=" + IdEmailPush.ToString()+")";
        sqlCommand += " GROUP BY [Users].[Language]";
        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    public static async Task<DataTable> GetEmailAsync(long IdEmailPush)
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [IdEmail],[IdEmailPush],[Language],[DateAdd],[DateChange],SUBSTRING([EmailSubject],0,10) AS Subject,SUBSTRING([EmailBody],0,10) AS Body,[EmailSubject],[EmailBody]";
        sqlCommand += " FROM [Email] WHERE [IdEmailPush]=" + IdEmailPush.ToString();
        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    public static async Task<DataTable> GetIdEmailByLanguageAsync(long IdEmailPush, string Language)
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [IdEmail] FROM [Email] WHERE [IdEmailPush]=" + IdEmailPush.ToString() + " AND [Language]='" + Language + "'";
        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    public static async Task<DataTable> GetEmailByIdAsync(long IdEmail)
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [EmailSubject],[EmailBody] FROM [Email] WHERE [IdEmail]=" + IdEmail.ToString();
        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    public static async Task<DataTable> GetEmailSendAsync(long IdEmailPush)
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [EmailSend].*,[Email].[Language] FROM [EmailSend] INNER JOIN [Email] ON [Email].[IdEmail]=[EmailSend].[IdEmail]";
        sqlCommand += "WHERE [EmailSend].[IdEmailPush]=" + IdEmailPush.ToString();

        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    public static async Task<DataTable> GetEmailSendPrepareAsync(long IdEmailPush)
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        //var sqlCommand = "SELECT [Users].[FirstName]+' '+[Users].[LastName] AS [UserName],[Users].[Username] AS [UserEmail],[Users].[Language] FROM [Users]";
        //sqlCommand += " LEFT OUTER JOIN (SELECT [IdEmailPush],[UserEmail] FROM [EmailSend] WHERE [IdEmailPush]=" + IdEmailPush.ToString() + ") AS [EmailSend] ON [EmailSend].[UserEmail]=[Users].[Username]";
        //sqlCommand += " INNER JOIN (SELECT [Language] FROM [Email] WHERE [IdEmailPush]=" + IdEmailPush.ToString() + " GROUP BY [Language]) AS [UserLanguage] ON [UserLanguage].[Language]=[Users].[Language]";
        //sqlCommand += " WHERE [Users].[IsEnabled]=1 AND [Users].[IsSubscribe]=1 AND [EmailSend].[IdEmailPush] IS NULL";

        var sqlCommand = "SELECT [Users].[FirstName]+' '+[Users].[LastName] AS [UserName],[Users].[Username] AS [UserEmail],[Users].[Language],[Users].[UserDateRegistration],[EmailPush].[DateUser] FROM [Users]";
        sqlCommand += " LEFT OUTER JOIN (SELECT [IdEmailPush],[UserEmail] FROM [EmailSend] WHERE [IdEmailPush]=" + IdEmailPush.ToString() + ") AS [EmailSend] ON [EmailSend].[UserEmail]=[Users].[Username]";
        sqlCommand += " INNER JOIN (SELECT [Language] FROM [Email] WHERE [IdEmailPush]=" + IdEmailPush.ToString() + " GROUP BY [Language]) AS [UserLanguage] ON [UserLanguage].[Language]=[Users].[Language]";
        sqlCommand += " INNER JOIN (SELECT [IdEmailPush],[DateUser] FROM [EmailPush] WHERE [IdEmailPush]=" + IdEmailPush.ToString() + ") AS [EmailPush] ON [EmailPush].[IdEmailPush]=" + IdEmailPush.ToString();
        sqlCommand += " WHERE [Users].[IsEnabled]=1 AND [Users].[IsSubscribe]=1 AND [EmailSend].[IdEmailPush] IS NULL AND [Users].[UserDateRegistration]>=[EmailPush].[DateUser]";

        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    public static async Task AddEmailSendAsync(long IdEmailPush, long IdEmail, string UserName, string UserEmail)
    {
      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "INSERT INTO [EmailSend] (IdEmailPush,IdEmail,UserName,UserEmail,IsSended,Error)";
        sqlCommand += " VALUES ("+IdEmailPush.ToString()+","+IdEmail.ToString()+",N'"+UserName+"',N'"+UserEmail+"',0,NULL)";

        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;
        await cmd.ExecuteNonQueryAsync();
      }
    }

    public static async Task ChangeEmailSendAsync(long IdEmailSend, bool IsSended, string Error, bool IsError)
    {
      var DateSend = DateTime.Now;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        string sqlCommand;
        if (!IsError)
          sqlCommand = "UPDATE [EmailSend] SET IsSended=" + Convert.ToInt16(IsSended) + ",Error=N'" + Error + "',DateSend=CONVERT(DATETIME,'" + Date2ISO8601_126(DateSend) + "',126) WHERE [IdEmailSend]=" + IdEmailSend.ToString();
        else
          sqlCommand = "UPDATE [EmailSend] SET Error=N'" + Error + "' WHERE [IdEmailSend]=" + IdEmailSend.ToString();

        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;
        await cmd.ExecuteNonQueryAsync();
      }
    }

    private static string Date2ISO(DateTime DateForIso)
    {
      string sday, smonth, syear;
      if (DateForIso.Day <= 9)
        sday = "0" + DateForIso.Day.ToString();
      else
        sday = DateForIso.Day.ToString();

      if (DateForIso.Month <= 9)
        smonth = "0" + DateForIso.Month.ToString();
      else
        smonth = DateForIso.Month.ToString();

      syear = DateForIso.Year.ToString();

      return syear + smonth + sday;
    }

    private static string Date2ISO8601_126(DateTime DateForIso)
    {
      string sday, smonth, syear;
      string shour, sminute, ssecond;

      if (DateForIso.Day <= 9)
        sday = "0" + DateForIso.Day.ToString();
      else
        sday = DateForIso.Day.ToString();

      if (DateForIso.Month <= 9)
        smonth = "0" + DateForIso.Month.ToString();
      else
        smonth = DateForIso.Month.ToString();

      syear = DateForIso.Year.ToString();

      if (DateForIso.Hour <= 9)
        shour = "0" + DateForIso.Hour.ToString();
      else
        shour = DateForIso.Hour.ToString();

      if (DateForIso.Minute <= 9)
        sminute = "0" + DateForIso.Minute.ToString();
      else
        sminute = DateForIso.Minute.ToString();

      if (DateForIso.Second <= 9)
        ssecond = "0" + DateForIso.Second.ToString();
      else
        ssecond = DateForIso.Second.ToString();

      return syear + "-"+smonth + "-"+sday+"T"+ shour+":"+ sminute+":"+ ssecond+".000";
    }

    #endregion Email push

    #region Authentificaton
    public static async Task<string> LoginAsync(string Username, string Password, enumTypeUser IdTypeUser)
    {
      string UserToken = "";

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("Login", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@UserName", Username);
        parameter.SqlDbType = SqlDbType.VarChar;

        parameter = cmd.Parameters.AddWithValue("@Password", Password);
        parameter.SqlDbType = SqlDbType.VarChar;

        parameter = cmd.Parameters.AddWithValue("@IdTypeUser", (int)IdTypeUser);
        parameter.SqlDbType = SqlDbType.Int;

        var objUserToken = await cmd.ExecuteScalarAsync();
        if (objUserToken != null)
          UserToken = objUserToken.ToString();
        else
          UserToken = "";
      }
      return UserToken;
    }

    public static async Task<bool> LogoutAsync(string UserToken, enumTypeUser IdTypeUser)
      {
        bool IsSuccess=false;

        using (SqlConnection cn = new SqlConnection(ConnectionString))
        {
          await cn.OpenAsync();

          SqlCommand cmd = new SqlCommand("Logout", cn);
          cmd.CommandType = CommandType.StoredProcedure;

          SqlParameter parameter;

          parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
          parameter.SqlDbType = SqlDbType.VarChar;

          parameter = cmd.Parameters.AddWithValue("@IdTypeUser", (int)IdTypeUser);
          parameter.SqlDbType = SqlDbType.Int;

          parameter = cmd.Parameters.AddWithValue("@IsSuccess", UserToken);
          parameter.SqlDbType = SqlDbType.Bit;
          parameter.Direction = ParameterDirection.Output;

          await cmd.ExecuteNonQueryAsync();

          IsSuccess = (bool)cmd.Parameters["@IsSuccess"].Value;
        }
        return IsSuccess;
      }

    public static async Task<Tuple<long, string>> RegisterNewUserAsync(string FirstName, string LastName, string Username, string Password, int IdTariffPlan, string Language)
    {
      #region Define vars
      long IdUser;
      string ErrorMessage = "";
      Tuple<long, string> returnValue;
      #endregion Define vars

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("RegisterNewUser", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@FirstName", FirstName);
        parameter.SqlDbType = SqlDbType.NVarChar;
        parameter.Size = 50;

        parameter = cmd.Parameters.AddWithValue("@LastName", LastName);
        parameter.SqlDbType = SqlDbType.NVarChar;
        parameter.Size = 50;

        parameter = cmd.Parameters.AddWithValue("@UserName", Username);
        parameter.SqlDbType = SqlDbType.NVarChar;
        parameter.Size = 50;

        parameter = cmd.Parameters.AddWithValue("@Password", Password);
        parameter.SqlDbType = SqlDbType.NVarChar;
        parameter.Size = 128;

        parameter = cmd.Parameters.AddWithValue("@IdTariffPlan", IdTariffPlan);
        parameter.SqlDbType = SqlDbType.Int;

        parameter = cmd.Parameters.AddWithValue("@Language", Language);
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Size = 10;

        parameter = cmd.Parameters.AddWithValue("@Message", ErrorMessage);
        parameter.SqlDbType = SqlDbType.NVarChar;
        parameter.Direction = ParameterDirection.Output;
        parameter.Size = 250;
        parameter.Value = "";

        var aIdUser = await cmd.ExecuteScalarAsync();
        if (aIdUser != null)
        {
          IdUser = Convert.ToInt64(aIdUser);          
          if (IdUser > 0)
          {
            var NoError = "";
            returnValue = new Tuple<long, string>(IdUser, NoError);
          }
          else
          {
            returnValue = new Tuple<long, string>(IdUser, cmd.Parameters["@Message"].Value as string);
          }
        }
        else
        {
          IdUser = 0;
          returnValue = new Tuple<long, string>(IdUser, cmd.Parameters["@Message"].Value.ToString());
        }
      }

      return returnValue;
    }

    public static async Task<DataTable> GetUserByUserTokenAsync(string UserToken, int IdTypeUser)
    {
      #region Define vars
      DataTable Table;
      #endregion Define vars

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var cmd = new SqlCommand("GetUserByUserToken", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        parameter = cmd.Parameters.AddWithValue("@IdTypeUser", IdTypeUser);
        parameter.SqlDbType = SqlDbType.Int;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    public static async Task UpdateUserInfoAsync(long IdUser, string Password)
    {
      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("UpdateUserInfo", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@IdUser", IdUser);
        parameter.SqlDbType = SqlDbType.BigInt;

        parameter = cmd.Parameters.AddWithValue("@Password", Password);
        parameter.SqlDbType = SqlDbType.VarChar;

        var UserDateUpdate = DateTime.Now;
        parameter = cmd.Parameters.AddWithValue("@UserDateUpdate", UserDateUpdate);
        parameter.SqlDbType = SqlDbType.DateTime2;

        await cmd.ExecuteNonQueryAsync();
      }
    }

    #endregion Authentificaton

    public static async Task<DataTable> GetListMultimediaFiles(string UserToken, int IdTypeUser)
    {
      long IdUser;
      DataTable MultimediaFileTable;
      MultimediaFileTable = CreateTableMultimediaFileNames();

      DataRow MultimediaFileRow;
      IdUser = await GetIdUserByUserTokenAsync(UserToken, IdTypeUser);

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = cn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "GetListMultimediaFiles";

        SqlParameter parameter = new SqlParameter();
        parameter.ParameterName = "@IdUser";
        parameter.SqlDbType = SqlDbType.Int;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = IdUser;
        cmd.Parameters.Add(parameter);

        SqlDataReader ListMultimediaFiles = await cmd.ExecuteReaderAsync();

        while (await ListMultimediaFiles.ReadAsync())
        {
          IDataRecord record = (IDataRecord)ListMultimediaFiles;
          MultimediaFileRow = MultimediaFileTable.NewRow();

          MultimediaFileRow["FullPath"] = record["FullPath"];
          MultimediaFileRow["DisplayName"] = record["DisplayName"];
          MultimediaFileRow["MultimediaFileGUID"] = record["MultimediaFileGUID"];
          MultimediaFileRow["TypeMultimedia"] = record["TypeMultimedia"];
          MultimediaFileRow["Album"] = record["Album"];

          MultimediaFileTable.Rows.Add(MultimediaFileRow);
        }
        ListMultimediaFiles.Close();
      }
      return MultimediaFileTable;
    }

    public static async Task ListMultimediaFilesAddAsync(string UserToken, DataTable MultimediaFileTable)
    {
      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("ListMultimediaFilesAdd", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        parameter = cmd.Parameters.AddWithValue("@ListMultimediaFiles", MultimediaFileTable);
        parameter.SqlDbType = SqlDbType.Structured;
        parameter.TypeName = "ListMultimediaFiles";

        await cmd.ExecuteNonQueryAsync();
      }
    }

    public static async Task ListMultimediaFilesRemoveAsync(string UserToken, DataTable MultimediaFileTable)
    {
      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("ListMultimediaFilesRemove", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        parameter = cmd.Parameters.AddWithValue("@ListGuids", MultimediaFileTable);
        parameter.SqlDbType = SqlDbType.Structured;
        parameter.TypeName = "ListGuids";

        await cmd.ExecuteNonQueryAsync();
      }
    }

    public static async Task<DataTable> GetListMultimediaSourceAsync(string UserToken)
    {
      Int64 IdUser;
      DataTable MultimediaSourceTable = new DataTable("MultimediaSource");
      DataRow MultimediaSourceRow;

      MultimediaSourceTable.Columns.Add(new DataColumn("IdTypeMultimediaSource", typeof(int)));
      MultimediaSourceTable.Columns.Add(new DataColumn("NameMultimediaSource", typeof(string)));
      MultimediaSourceTable.Columns.Add(new DataColumn("TitleMultimediaSource", typeof(string)));
      MultimediaSourceTable.Columns.Add(new DataColumn("StyleWidth", typeof(int)));
      MultimediaSourceTable.Columns.Add(new DataColumn("StyleHeight", typeof(int)));
      MultimediaSourceTable.Columns.Add(new DataColumn("StyleForeColor", typeof(string)));
      MultimediaSourceTable.Columns.Add(new DataColumn("StyleBackColor", typeof(string)));
      MultimediaSourceTable.Columns.Add(new DataColumn("StyleBorderColor", typeof(string)));
      MultimediaSourceTable.Columns.Add(new DataColumn("StyleFontSize", typeof(Int32)));
      MultimediaSourceTable.Columns.Add(new DataColumn("StyleName", typeof(string)));

      IdUser = await GetIdUserByUserTokenAsync(UserToken, 2);

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("GetListMultimediaSource", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@IdUser", IdUser);
        parameter.SqlDbType = SqlDbType.Int;

        using (SqlDataReader ListMultimediaSource = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo))
        {
          IDataRecord record;

          while (await ListMultimediaSource.ReadAsync())
          {
            record = ListMultimediaSource as IDataRecord;
            MultimediaSourceRow = MultimediaSourceTable.NewRow();

            MultimediaSourceRow["IdTypeMultimediaSource"] = record["IdTypeMultimediaSource"];
            MultimediaSourceRow["NameMultimediaSource"] = record["NameMultimediaSource"];
            MultimediaSourceRow["TitleMultimediaSource"] = record["TitleMultimediaSource"];
            MultimediaSourceRow["StyleWidth"] = record["StyleWidth"];
            MultimediaSourceRow["StyleHeight"] = record["StyleHeight"];
            MultimediaSourceRow["StyleForeColor"] = record["StyleForeColor"];
            MultimediaSourceRow["StyleBackColor"] = record["StyleBackColor"];
            MultimediaSourceRow["StyleBorderColor"] = record["StyleBorderColor"];
            MultimediaSourceRow["StyleFontSize"] = record["StyleFontSize"];
            MultimediaSourceRow["StyleName"] = record["StyleName"];

            MultimediaSourceTable.Rows.Add(MultimediaSourceRow);
          }
        }
      }
      return MultimediaSourceTable;
    }

    public static async Task<long> GetIdUserByUserTokenAsync(string UserToken, int IdTypeUser)
    {
      long IdUser = 0;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var cmd = new SqlCommand("GetIdUserByUserToken", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        parameter = cmd.Parameters.AddWithValue("@IdTypeUser", IdTypeUser);
        parameter.SqlDbType = SqlDbType.Int;

        parameter = new SqlParameter();
        parameter.ParameterName = "@IdUser";
        parameter.SqlDbType = SqlDbType.Int;
        parameter.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(parameter);

        await cmd.ExecuteNonQueryAsync();

        var sIdUser = Convert.ToString(cmd.Parameters["@IdUser"].Value);
        if (!long.TryParse(sIdUser, out IdUser))
        {
          IdUser = 0;
        }
      }

      return IdUser;
    }

    public static long GetIdUserDemo()
    {
      long IdUserDemo = 0;

      using (var cn = new SqlConnection(ConnectionString))
      {
        cn.Open();

        var cmd = new SqlCommand("GetIdUserDemo", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = new SqlParameter();
        parameter.ParameterName = "@IdUserDemo";
        parameter.SqlDbType = SqlDbType.Int;
        parameter.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(parameter);

        cmd.ExecuteNonQuery();

        var sIdUserDemo = Convert.ToString(cmd.Parameters["@IdUserDemo"].Value);
        if (!long.TryParse(sIdUserDemo, out IdUserDemo))
        {
          IdUserDemo = 0;
        }
      }

      return IdUserDemo;
    }

    #region Playlist
    public static async Task<bool> PlaylistSave(string UserToken, string Playlist)
    {
      bool IsSuccess = false;

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = cn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "PlaylistSave";

        SqlParameter parameter;

        parameter = new SqlParameter();
        parameter.ParameterName = "@UserToken";
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = UserToken;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@Playlist";
        parameter.SqlDbType = SqlDbType.NVarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = Playlist;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IsSuccess";
        parameter.SqlDbType = SqlDbType.Bit;
        parameter.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(parameter);

        await cmd.ExecuteNonQueryAsync();

        IsSuccess = (bool)cmd.Parameters["@IsSuccess"].Value;
      }

      return IsSuccess;
    }

    public static async Task<DataTable> PlaylistLoad(string UserToken)
    {
      DataTable tablePlaylist;
      SqlDataReader ListPlaylist;

      tablePlaylist = new DataTable("TablePlaylist");
      tablePlaylist.Columns.Add(new DataColumn("IdPlaylist", typeof(Int64)));
      tablePlaylist.Columns.Add(new DataColumn("Playlist", typeof(string)));

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = cn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "PlaylistLoad";

        SqlParameter parameter;

        parameter = new SqlParameter();
        parameter.ParameterName = "@UserToken";
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = UserToken;
        cmd.Parameters.Add(parameter);

        ListPlaylist = await cmd.ExecuteReaderAsync();

        DataRow RowPlaylist;
        while (await ListPlaylist.ReadAsync())
        {
          IDataRecord record = (IDataRecord)ListPlaylist;
          RowPlaylist = tablePlaylist.NewRow();
          RowPlaylist["IdPlaylist"] = Convert.ToInt64(record["IdPlaylist"]);
          RowPlaylist["Playlist"] = record["Playlist"].ToString().Trim();
          tablePlaylist.Rows.Add(RowPlaylist);
        }
        ListPlaylist.Close();
      }

      return tablePlaylist;
    }

    public static async Task<bool> PlaylistDelete(string UserToken, Int64 IdPlaylist)
    {
      bool IsSuccess;

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = cn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "PlaylistDelete";

        SqlParameter parameter;

        parameter = new SqlParameter();
        parameter.ParameterName = "@UserToken";
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = UserToken;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IdPlaylist";
        parameter.SqlDbType = SqlDbType.BigInt;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = IdPlaylist;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IsSuccess";
        parameter.SqlDbType = SqlDbType.Bit;
        parameter.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(parameter);

        await cmd.ExecuteNonQueryAsync();

        IsSuccess = (bool)cmd.Parameters["@IsSuccess"].Value;
      }

      return IsSuccess;
    }

    public static async Task<bool> PlaylistItemSave(string UserToken, Int64 IdPlaylist, int IdTypeMultimediaSource, string IdMultimediaItem, string MultimediaItem)
    {
      bool IsSuccess;

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = cn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "PlaylistItemSave";

        SqlParameter parameter;

        parameter = new SqlParameter();
        parameter.ParameterName = "@UserToken";
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = UserToken;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IdPlaylist";
        parameter.SqlDbType = SqlDbType.BigInt;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = IdPlaylist;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IdTypeMultimediaSource";
        parameter.SqlDbType = SqlDbType.Int;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = IdTypeMultimediaSource;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IdMultimediaItem";
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = IdMultimediaItem;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@MultimediaItem";
        parameter.SqlDbType = SqlDbType.NVarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = MultimediaItem;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IsSuccess";
        parameter.SqlDbType = SqlDbType.Bit;
        parameter.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(parameter);

        await cmd.ExecuteNonQueryAsync();

        IsSuccess = (bool)cmd.Parameters["@IsSuccess"].Value;
      }

      return IsSuccess;
    }

    public static async Task<bool> PlaylistItemRemove(string UserToken, Int64 IdPlaylistItem)
    {
      bool IsSuccess;

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = cn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "PlaylistItemRemove";

        SqlParameter parameter;

        parameter = new SqlParameter();
        parameter.ParameterName = "@UserToken";
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = UserToken;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IdPlaylistItem";
        parameter.SqlDbType = SqlDbType.BigInt;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = IdPlaylistItem;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IsSuccess";
        parameter.SqlDbType = SqlDbType.Bit;
        parameter.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(parameter);

        await cmd.ExecuteNonQueryAsync();

        IsSuccess = (bool)cmd.Parameters["@IsSuccess"].Value;
      }

      return IsSuccess;
    }

    #endregion Playlist

    #region OAuth

    public static async Task<DataTable> OAuthGetTypeMultimediaAsync(int IdTypeMultimediaSource)
    {
      var Table = new DataTable("TypeMultimediaOAuth");

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT * FROM [dbo].[TypeMultimediaOAuth] WHERE [IdTypeMultimediaOAuth]=(SELECT IdTypeMultimediaOAuth FROM [dbo].[TypeMultimediaSource] WHERE [IdTypeMultimediaSource]=" + IdTypeMultimediaSource.ToString() + ")";
        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;
        
        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        {
          IDataRecord record; DataRow Row;

          var IsFirstRow = true;
          while (await reader.ReadAsync())
          {
            record = reader as IDataRecord;

            if (IsFirstRow)
            {
              IsFirstRow = false;
              for (int i = 0; i < record.FieldCount; i++)
              {
                Table.Columns.Add(new DataColumn(record.GetName(i), record.GetFieldType(i)));
              }
            }

            Row = Table.NewRow();
            for (int i = 0; i < record.FieldCount; i++)
            {
              Row[i] = record.GetValue(i);
            }

            Table.Rows.Add(Row);
          }
        }
      }

      return Table;
    }

    public static async Task<bool> OAuthSetToken(string UserToken, int IdTypeMultimediaSource, OAuthToken OAuthUserToken)
    {
      bool IsSuccess;

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("OAuthSetToken", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        parameter = cmd.Parameters.AddWithValue("@IdTypeMultimediaSource", IdTypeMultimediaSource);
        parameter.SqlDbType = SqlDbType.Int;

        parameter = cmd.Parameters.AddWithValue("@AccessToken", OAuthUserToken.AccessToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        parameter = cmd.Parameters.AddWithValue("@RefreshToken", OAuthUserToken.RefreshToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        DateTime dt;
        try
        {
          dt = Convert.ToDateTime(OAuthUserToken.ExpiresIn);
        }
        catch (Exception)
        {
          dt = DateTime.MinValue;
        }

        parameter = cmd.Parameters.AddWithValue("@AccessTokenExpires", dt);
        parameter.SqlDbType = SqlDbType.DateTime2;

        parameter = cmd.Parameters.AddWithValue("@RefreshTokenExpires", DateTime.MinValue);
        parameter.SqlDbType = SqlDbType.DateTime2;

        parameter = new SqlParameter();
        parameter.ParameterName = "@IsSuccess";
        parameter.SqlDbType = SqlDbType.Bit;
        parameter.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(parameter);

        await cmd.ExecuteNonQueryAsync();

        IsSuccess = (bool)cmd.Parameters["@IsSuccess"].Value;
      }

      return IsSuccess;
    }

    public static async Task<DataTable> OAuthGetTokenAsync(string UserToken, int IdTypeMultimediaSource)
    {
      DataTable Table;

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("OAuthGetToken", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        parameter = cmd.Parameters.AddWithValue("@IdTypeMultimediaSource", IdTypeMultimediaSource);
        parameter.SqlDbType = SqlDbType.Int;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }
      }

      return Table;
    }

    #endregion OAuth

    public static async Task<string> GetUserTokenClient(string UserToken)
    {
      string UserTokenClient;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var cmd = new SqlCommand("GetUserTokenClient", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@UserTokenServer", UserToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        var aUserTokenClient = await cmd.ExecuteScalarAsync();

        if (aUserTokenClient != null)
          UserTokenClient = aUserTokenClient.ToString();
        else
          UserTokenClient = null;
      }

      // Не знаю, зачем этот код долгое время работал в тесте
      //using (var cn = new SqlConnection(ConnectionString))
      //{
      //  await cn.OpenAsync();

      //  var cmd = new SqlCommand("SELECT TOP 1 [UserToken] FROM [UserTokenClient] WHERE IdUser=5", cn);
      //  cmd.CommandType = CommandType.Text;

      //  var aUserTokenClient = await cmd.ExecuteScalarAsync();

      //  if (aUserTokenClient != null)
      //    UserTokenClient = aUserTokenClient.ToString();
      //  else
      //    UserTokenClient = null;
      //}

      return UserTokenClient;
    }

    public static async Task TraceAsync(int IdMultimediaServer, enumTypeLog IdTypeLog, string ServerIp = null, int? ServerPort = null, string ClientIp = null, int? ClientPort = null, string Scope = null, string Procedure = null, string Message = null, string UserToken = null)
      {
        using (SqlConnection cn = new SqlConnection(ConnectionString))
        {
          await cn.OpenAsync();

          SqlCommand cmd = new SqlCommand("LogLiveMultimedia", cn);
          cmd.CommandType = CommandType.StoredProcedure;

          SqlParameter parameter;

          parameter = cmd.Parameters.AddWithValue("@ServerIp", ServerIp);
          parameter.SqlDbType = SqlDbType.VarChar;

          parameter = cmd.Parameters.AddWithValue("@ServerPort", ServerPort);
          parameter.SqlDbType = SqlDbType.Int;

          parameter = cmd.Parameters.AddWithValue("@ClientIp", ClientIp);
          parameter.SqlDbType = SqlDbType.VarChar;

          parameter = cmd.Parameters.AddWithValue("@ClientPort", ClientPort);
          parameter.SqlDbType = SqlDbType.Int;

          parameter = cmd.Parameters.AddWithValue("@IdMultimediaServer", IdMultimediaServer);
          parameter.SqlDbType = SqlDbType.Int;

          parameter = cmd.Parameters.AddWithValue("@Scope", Scope);
          parameter.SqlDbType = SqlDbType.VarChar;

          parameter = cmd.Parameters.AddWithValue("@Procedure", Procedure);
          parameter.SqlDbType = SqlDbType.VarChar;

          parameter = cmd.Parameters.AddWithValue("@Message", Message);
          parameter.SqlDbType = SqlDbType.NVarChar;

          parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
          parameter.SqlDbType = SqlDbType.VarChar;

          parameter = cmd.Parameters.AddWithValue("@IdTypeLog", (int)IdTypeLog);
          parameter.SqlDbType = SqlDbType.Int;

          await cmd.ExecuteNonQueryAsync();
        }
      }

    #region New source
    public static async Task<DataTable> GetSourceAsync(int IdSource, string UserToken)
    {
      #region Define vars
      DataTable TableSource = new DataTable("TableSource");
      DataRow RowSource;
      //string ColumnName;
      #endregion Define vars

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("GetSource", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@IdSource", IdSource);
        parameter.SqlDbType = SqlDbType.Int;

        parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.IsNullable = true;

        using (SqlDataReader ListMultimediaSource = await cmd.ExecuteReaderAsync())
        {
          IDataRecord record;

          var IsFirstRow = true;

          while (await ListMultimediaSource.ReadAsync())
          {
            record = ListMultimediaSource as IDataRecord;

            if (IsFirstRow)
            {
              IsFirstRow = false;
              for (int i = 0; i < record.FieldCount; i++)
              {
                TableSource.Columns.Add(new DataColumn(record.GetName(i), record.GetFieldType(i)));
              }
            }

            RowSource = TableSource.NewRow();
            for (int i = 0; i < record.FieldCount; i++)
            {
              RowSource[i] = record.GetValue(i);
            }

            TableSource.Rows.Add(RowSource);
          }
        }
      }
      return TableSource;
    }

    public static async Task<DataTable> GetFoldersByFolderHomeAsync(int IdTypeMultimediaSource, string UserToken)
      {
        IDataRecord record;

        var TableItems = new DataTable("TableItems");        
        TableItems.Columns.Add(new DataColumn("Id", typeof(string)));
        TableItems.Columns.Add(new DataColumn("Name", typeof(string)));
        TableItems.Columns.Add(new DataColumn("IdTypeMultimediaItem", typeof(string)));

        DataRow MultimediaRow;

        using (SqlConnection cn = new SqlConnection(ConnectionString))
        {
          await cn.OpenAsync();

          var cmd = new SqlCommand("HomeGetFoldersByFolder", cn);
          cmd.CommandType = CommandType.StoredProcedure;

          SqlParameter parameter;

          parameter = cmd.Parameters.AddWithValue("@IdTypeMultimediaSource", IdTypeMultimediaSource);
          parameter.SqlDbType = SqlDbType.Int;

          parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
          parameter.SqlDbType = SqlDbType.VarChar;

          var readerHome = await cmd.ExecuteReaderAsync();          
          while (await readerHome.ReadAsync())
          {
            record = (IDataRecord)readerHome;
            MultimediaRow = TableItems.NewRow();
            MultimediaRow["Id"] = record["Album"];
            MultimediaRow["Name"] = record["Album"];
            MultimediaRow["IdTypeMultimediaItem"] = record["IdTypeMultimediaItem"];
            TableItems.Rows.Add(MultimediaRow);
          }
          readerHome.Close();
        }
        return TableItems;
      }

    public static async Task<DataTable> GetItemsByFolderHomeAsync(int IdTypeMultimediaSource, string IdItem, string UserToken)
    {
      IDataRecord record;

      var TableSourceItem = new DataTable("TableSourceItem");
      TableSourceItem.Columns.Add(new DataColumn("Id", typeof(string)));
      TableSourceItem.Columns.Add(new DataColumn("Name", typeof(string)));
      TableSourceItem.Columns.Add(new DataColumn("IdTypeMultimediaItem", typeof(string)));

      DataRow MultimediaRow;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var cmd = new SqlCommand("HomeGetItemsByFolder", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@IdTypeMultimediaSource", IdTypeMultimediaSource);
        parameter.SqlDbType = SqlDbType.Int;

        parameter = cmd.Parameters.AddWithValue("@IdItem", IdItem);
        parameter.SqlDbType = SqlDbType.NVarChar;

        parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
        parameter.SqlDbType = SqlDbType.VarChar;

        var readerHome = await cmd.ExecuteReaderAsync();
        while (await readerHome.ReadAsync())
        {
          record = (IDataRecord)readerHome;
          MultimediaRow = TableSourceItem.NewRow();
          MultimediaRow["Id"] = record["MultimediaFileGUID"].ToString().ToUpper();
          MultimediaRow["Name"] = record["DisplayName"];
          MultimediaRow["IdTypeMultimediaItem"] = record["IdTypeMultimediaItem"];

          TableSourceItem.Rows.Add(MultimediaRow);
        }
        readerHome.Close();
      }

      return TableSourceItem;
    }

    public static async Task<DataTable> GetSourceColorAsync(int IdTypeMultimediaSource)
    {
      #region Define vars
      DataTable TableMultimediaItemColor = new DataTable("TableMultimediaItemColor");
      TableMultimediaItemColor.Columns.Add(new DataColumn("StyleBackColor", typeof(string)));
      TableMultimediaItemColor.Columns.Add(new DataColumn("StyleForeColor", typeof(string)));
      #endregion Define vars

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT StyleBackColor, StyleForeColor FROM [dbo].[TypeMultimediaSource] WHERE [IdTypeMultimediaSource]=" + IdTypeMultimediaSource.ToString();
        SqlCommand cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (SqlDataReader readerMultimediaItemColor = await cmd.ExecuteReaderAsync())
        {
          DataRow RowColor;
          IDataRecord record;

          while (await readerMultimediaItemColor.ReadAsync())
          {
            record = readerMultimediaItemColor as IDataRecord;

            RowColor = TableMultimediaItemColor.NewRow();
            RowColor["StyleBackColor"] = record["StyleBackColor"] as string;
            RowColor["StyleForeColor"] = record["StyleForeColor"] as string;
            TableMultimediaItemColor.Rows.Add(RowColor);
          }
        }
      }
      return TableMultimediaItemColor;
    }

    public static async Task<DataTable> SelectSourceAsync(string IdSource = null)
    {
      #region Define vars
      DataTable TableSource = new DataTable("TableSource");
      DataColumn[] keys = new DataColumn[1];
      int IdTypeMultimediaSource;
      string Where;
      #endregion Define vars

      if (JetSASLibrary.CheckGoodString(IdSource))
      {
        if (!int.TryParse(IdSource, out IdTypeMultimediaSource)) return TableSource;
        if (IdTypeMultimediaSource <= 0) return TableSource;
        Where = " WHERE IdTypeMultimediaSource="+ IdTypeMultimediaSource.ToString();
      }
      else
      {
        Where = "";
      }

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT * FROM [dbo].[TypeMultimediaSource]"+ Where;
        SqlCommand cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (SqlDataReader ListMultimediaSource = await cmd.ExecuteReaderAsync())
        {
          IDataRecord record;
          DataRow RowSource;

          var IsFirstRow = true;

          while (await ListMultimediaSource.ReadAsync())
          {
            record = ListMultimediaSource as IDataRecord;

            #region Fill headers
            if (IsFirstRow)
            {
              IsFirstRow = false;
              for (int i = 0; i < record.FieldCount; i++)
              {
                TableSource.Columns.Add(new DataColumn(record.GetName(i), record.GetFieldType(i)));
              }
              keys[0] = TableSource.Columns[0];
              TableSource.PrimaryKey = keys;
            }
            #endregion Fill headers

            RowSource = TableSource.NewRow();
            for (int i = 0; i < record.FieldCount; i++)
            {
              RowSource[i] = record.GetValue(i);
            }

            TableSource.Rows.Add(RowSource);
          }
        }
      }

      return TableSource;
    }

    public static async Task<DataTable> SelectSourceByIdAsync(int IdTypeMultimediaSource)
      {
        #region Define vars
        DataTable TableSource = new DataTable("TableSource");
        #endregion Define vars

        using (SqlConnection cn = new SqlConnection(ConnectionString))
        {
          await cn.OpenAsync();

          var sqlCommand = "SELECT * FROM [dbo].[TypeMultimediaSource] WHERE IdTypeMultimediaSource=" + IdTypeMultimediaSource.ToString();
          SqlCommand cmd = new SqlCommand(sqlCommand, cn);
          cmd.CommandType = CommandType.Text;

          using (SqlDataReader ListMultimediaSource = await cmd.ExecuteReaderAsync())
          {
            IDataRecord record;
            DataRow RowSource;

            var IsFirstRow = true;

            while (await ListMultimediaSource.ReadAsync())
            {
              record = ListMultimediaSource as IDataRecord;

              #region Fill headers
              if (IsFirstRow)
              {
                IsFirstRow = false;
                for (int i = 0; i < record.FieldCount; i++)
                {
                  TableSource.Columns.Add(new DataColumn(record.GetName(i), record.GetFieldType(i)));
                }
              }
              #endregion Fill headers

              RowSource = TableSource.NewRow();
              for (int i = 0; i < record.FieldCount; i++)
              {
                RowSource[i] = record.GetValue(i);
              }

              TableSource.Rows.Add(RowSource);
            }
          }
        }
        return TableSource;
      }

    public static async Task<string> SelectOAuthFilterAsync(int IdTypeMultimediaSource)
    {
      #region Define vars
      string OAuthFilter;
      #endregion Define vars

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT OAuthFilter FROM [dbo].[TypeMultimediaOAuthFilter] WHERE " +
          "IdTypeMultimediaOAuth=(SELECT IdTypeMultimediaOAuth FROM[dbo].[TypeMultimediaSource] WHERE IdTypeMultimediaSource=" + IdTypeMultimediaSource.ToString() + ") AND " +
          "IdTypeMultimediaItem=(SELECT TypeMultimediaItem FROM[dbo].[TypeMultimediaSource] WHERE IdTypeMultimediaSource=" + IdTypeMultimediaSource.ToString() + ")";

        SqlCommand cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (SqlDataReader ListMultimediaSource = await cmd.ExecuteReaderAsync())
        {
          await ListMultimediaSource.ReadAsync();
          var record = ListMultimediaSource as IDataRecord;
          OAuthFilter = record.GetValue(0).ToString();
        }
      }
      return OAuthFilter;
    }

    public static async Task<string> SelectMimeTypeByMultimediaFileGUID(string MultimediaFileGUID)
    {
      #region Define vars
      //DataTable TableSource = new DataTable("TableMimeType");
      string MimeType;
      #endregion Define vars

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [MimeType] FROM [TypeMultimedia]";
        sqlCommand += " INNER JOIN (SELECT [TypeMultimedia] FROM [MultimediaFile] WHERE [MultimediaFileGUID]='" + MultimediaFileGUID + "') AS mfg";
        sqlCommand += " ON [TypeMultimedia].Extension=mfg.[TypeMultimedia]";

        SqlCommand cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        var returnValue = await cmd.ExecuteScalarAsync();
        if (returnValue != null)
          MimeType = returnValue as string;
        else
          MimeType = "";
        //using (SqlDataReader ListMultimediaSource = await cmd.ExecuteReaderAsync())
        //{
        //  IDataRecord record;
        //  DataRow RowSource;

        //  var IsFirstRow = true;

        //  while (await ListMultimediaSource.ReadAsync())
        //  {
        //    record = ListMultimediaSource as IDataRecord;

        //    #region Fill headers
        //    if (IsFirstRow)
        //    {
        //      IsFirstRow = false;
        //      for (int i = 0; i < record.FieldCount; i++)
        //      {
        //        TableSource.Columns.Add(new DataColumn(record.GetName(i), record.GetFieldType(i)));
        //      }
        //    }
        //    #endregion Fill headers

        //    RowSource = TableSource.NewRow();
        //    for (int i = 0; i < record.FieldCount; i++)
        //    {
        //      RowSource[i] = record.GetValue(i);
        //    }

        //    TableSource.Rows.Add(RowSource);
        //  }
        //}
      }
      return MimeType;
      //return TableSource;
    }

    public static async Task<DataTable> SelectMultimediaExtensions()
    {
      #region Define vars
      DataTable TableExtension = new DataTable("TableExtension");
      #endregion Define vars

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [TypeMultimediaItem].[TypeMultimediaItem],[TypeMultimediaItem].[TitleMultimediaItem],[TypeMultimedia].Extension,[TypeMultimediaItem].[IsLocalizeTitle],[TypeMultimediaItem].[IsTranslateTitle] FROM [TypeMultimedia]";
        sqlCommand += " INNER JOIN [TypeMultimediaItem] ON [TypeMultimedia].[IdTypeMultimediaItem]=[TypeMultimediaItem].[IdTypeMultimediaItem]";
        sqlCommand += " WHERE [TypeMultimedia].[IsAvailable]=1";
        sqlCommand += " ORDER BY [TypeMultimediaItem].[OrderByTypeMultimedia]";

        SqlCommand cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (SqlDataReader ListMultimediaSource = await cmd.ExecuteReaderAsync())
        {
          IDataRecord record;
          DataRow RowSource;

          var IsFirstRow = true;
          while (await ListMultimediaSource.ReadAsync())
          {
            record = ListMultimediaSource as IDataRecord;

            #region Fill headers
            if (IsFirstRow)
            {
              IsFirstRow = false;
              for (int i = 0; i < record.FieldCount; i++)
              {
                TableExtension.Columns.Add(new DataColumn(record.GetName(i), record.GetFieldType(i)));
              }
            }
            #endregion Fill headers

            RowSource = TableExtension.NewRow();
            for (int i = 0; i < record.FieldCount; i++)
            {
              RowSource[i] = record.GetValue(i);
            }

            TableExtension.Rows.Add(RowSource);
          }
        }
      }
      return TableExtension;
    }

    public static async Task<DataTable> SelectMultimediaExtensionsByMultimediaSourceAsync(int IdTypeMultimediaItem)
    {
      #region Define vars
      DataTable TableExtension = new DataTable("TableExtension");
      #endregion Define vars

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [TypeMultimedia].[Extension] AS [Extension],[TypeMultimediaItem].[TypeMultimediaItem] AS [Type] FROM [TypeMultimedia]";
        sqlCommand += " INNER JOIN [TypeMultimediaSource] ON [TypeMultimedia].[IdTypeMultimediaItem]=[TypeMultimediaSource].[TypeMultimediaItem]";
        sqlCommand += " INNER JOIN [TypeMultimediaItem] ON [TypeMultimedia].[IdTypeMultimediaItem]=[TypeMultimediaItem].[IdTypeMultimediaItem]";
        sqlCommand += " WHERE [TypeMultimediaSource].[IdTypeMultimediaSource]="+ IdTypeMultimediaItem.ToString();

        SqlCommand cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (SqlDataReader ListMultimediaSource = await cmd.ExecuteReaderAsync())
        {
          IDataRecord record;
          DataRow RowSource;

          var IsFirstRow = true;
          while (await ListMultimediaSource.ReadAsync())
          {
            record = ListMultimediaSource as IDataRecord;

            #region Fill headers
            if (IsFirstRow)
            {
              IsFirstRow = false;
              for (int i = 0; i < record.FieldCount; i++)
              {
                TableExtension.Columns.Add(new DataColumn(record.GetName(i), record.GetFieldType(i)));
              }
            }
            #endregion Fill headers

            RowSource = TableExtension.NewRow();
            for (int i = 0; i < record.FieldCount; i++)
            {
              RowSource[i] = record.GetValue(i);
            }

            TableExtension.Rows.Add(RowSource);
          }
        }
      }
      return TableExtension;
    }

    #endregion New source

    //public static async Task<string> OAuthGetRefreshTokenAsync(string UserToken, int IdTypeMultimediaSource)
    //{
    //  string RefreshToken;

    //  using (SqlConnection cn = new SqlConnection(ConnectionString))
    //  {
    //    await cn.OpenAsync();

    //    SqlCommand cmd = new SqlCommand("OAuthGetRefreshToken", cn);
    //    cmd.CommandType = CommandType.StoredProcedure;

    //    SqlParameter parameter;

    //    parameter = cmd.Parameters.AddWithValue("@UserToken", UserToken);
    //    parameter.SqlDbType = SqlDbType.VarChar;

    //    parameter = cmd.Parameters.AddWithValue("@IdTypeMultimediaSource", IdTypeMultimediaSource);
    //    parameter.SqlDbType = SqlDbType.Int;

    //    var varRefreshToken = await cmd.ExecuteScalarAsync();
    //    if (varRefreshToken != null)
    //      efreshToken = varRefreshToken.ToString();
    //    else
    //      RefreshToken = null;
    //  }

    //  return RefreshToken;
    //}

    #region News
    //public static async Task<DataTable> GetItemsMarketNewsAsync()
    //{
    //  DataTable Table;

    //  using (var cn = new SqlConnection(ConnectionString))
    //  {
    //    await cn.OpenAsync();

    //    var sqlCommand = "SELECT [Id]=[MarketId],[Name]=";
    //    sqlCommand += "CASE (SELECT COUNT(*) FROM [NewsMarket] AS [CountNewsMarket] WHERE [CountNewsMarket].[Country]=[NewsMarket].[Country])";
    //    sqlCommand += "WHEN 1 THEN [Country]";
    //    sqlCommand += "ELSE [Country]+'|'+[Language]";
    //    sqlCommand += "END,";
    //    sqlCommand += "0 AS [IdTypeMultimediaItem]";
    //    sqlCommand += "FROM [NewsMarket] ORDER BY [Country]";

    //    var cmd = new SqlCommand(sqlCommand, cn);
    //    cmd.CommandType = CommandType.Text;

    //    using (var reader = await cmd.ExecuteReaderAsync())
    //    {
    //      Table = await GetTableFromReader(reader);
    //    }

    //    foreach (DataRow item in Table.Rows)
    //    {
    //      item["IdTypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Folder);
    //    }

    //  }

    //  return Table;
    //}

    public static async Task<DataTable> GetItemsMarketNewsGoogleAsync()
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        //var sqlCommand = "SELECT [Id]=[MarketId],[Name]=";
        //sqlCommand += "CASE (SELECT COUNT(*) FROM [NewsMarket] AS [CountNewsMarket] WHERE [CountNewsMarket].[Country]=[NewsMarket].[Country])";
        //sqlCommand += "WHEN 1 THEN [Country]";
        //sqlCommand += "ELSE [Country]+'|'+[Language]";
        //sqlCommand += "END,";
        //sqlCommand += "0 AS [IdTypeMultimediaItem]";
        //sqlCommand += "FROM [NewsMarket] ORDER BY [Country]";

        var sqlCommand = "SELECT [Id]=[ned]+'|'+[Language]+'|'+[ISO_639-3],[Name]=";
        sqlCommand += "CASE (SELECT COUNT(*) FROM [NewsMarketGoogle] AS [CountNewsMarket] WHERE [CountNewsMarket].[CountryEnglish]=[NewsMarketGoogle].[CountryEnglish])";
        sqlCommand += "WHEN 1 THEN [CountryEnglish]";
        sqlCommand += "ELSE [CountryEnglish]+'|'+[LanguageEnglish]";
        sqlCommand += "END,";
        sqlCommand += "0 AS [IdTypeMultimediaItem]";
        sqlCommand += "FROM [NewsMarketGoogle] WHERE [FlagMarketEnabled]=1 ORDER BY [CountryEnglish]";

        //var sqlCommand = "SELECT[Id]=[ned],[Name]=";
        //sqlCommand += "CASE([CountryNative])";
        //sqlCommand += " WHEN [CountryEnglish] THEN [CountryNative]";
        //sqlCommand += " ELSE [CountryNative]+' ('+[CountryEnglish]+')'";
        //sqlCommand += " END";
        //sqlCommand += ",";
        //sqlCommand += "0 AS [IdTypeMultimediaItem]";
        //sqlCommand += " FROM [NewsMarketGoogle] WHERE [FlagRSS]=1 ORDER BY [CountryEnglish]";

        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }

        //foreach (DataRow item in Table.Rows)
        //{
        //  item["IdTypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Folder);
        //}
      }

      return Table;
    }

    public static async Task<DataTable> GetItemsCategoryNewsAsync()
    {
      DataTable Table;

      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [Id]=[NewsCategory],[Name]=[NameCategory],0 AS [IdTypeMultimediaItem] FROM [NewsCategory]";

        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
          Table = await GetTableFromReader(reader);
        }

        foreach (DataRow item in Table.Rows)
        {
          item["IdTypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Folder);
        }
      }

      return Table;
    }


    // Version 5
    // Bing News Search API Documentation: https://www.microsoft.com/cognitive-services/en-us/Bing-news-search-API/documentation
    // News Search API - V5: https://dev.cognitive.microsoft.com/docs/services/56b43f72cf5ff8098cef380a/operations/56f02400dbe2d91900c68553
    // News Search API Reference: https://msdn.microsoft.com/en-us/library/dn760793(v=bsynd.50).aspx
    // News Search API Guide: https://msdn.microsoft.com/en-us/library/dn760783(v=bsynd.50).aspx
    // Market Codes: https://msdn.microsoft.com/en-us/library/dn783426.aspx

    #region Bing Types
    private class BingThumbnail
    {
      string contentUrl { get; set; }
      ushort height { get; set; }
      ushort width { get; set; }
    }

    private class BingImage
    {
      BingOrganization provider { get; set; }
      BingThumbnail thumbnai { get; set; }
      string url { get; set; }
    }

    private class BingOrganization
    {
      string _type { get; set; }
      string name { get; set; }
    }

    private class BingNewsArticle
    {
      public string category { get; set; }
      public BingNewsArticle[] clusteredArticles { get; set; }
      public string datePublished { get; set; }
      public string description { get; set; }
      public bool headline { get; set; }
      public string id { get; set; }
      public BingImage image { get; set; }
      public string name { get; set; }
      public BingOrganization[] provider { get; set; }
      public string url { get; set; }
      public string urlPingSuffix { get; set; }
    }

    private class BingRelatedTopic
    {
      BingNewsArticle relatedNews { get; set; }
      string name { get; set; }
      string webSearchUrl { get; set; }
    }

    private class BingInstrumentation
    {
      string pageLoadPingUrl { get; set; }
      string pingUrlBase { get; set; }
    }

    private class BingNews
    {
      public string _type { get; set; }
      public string id { get; set; }
      BingInstrumentation instrumentation { get; set; }
      public string readLink { get; set; }
      public BingRelatedTopic[] relatedTopics { get; set; }
      public long totalEstimatedMatches { get; set; }
      public BingNewsArticle[] value { get; set; }
    }
    #endregion Bing Types

    public static async Task<DataTable> GetItemsNewsAsync(string MarketId, string NewsCategory)
    {
      #region Define vars
      string Query, EncodedQuery;
      DataTable Table;
      #endregion Define vars

      Table = new DataTable("TableItemsNews");
      Table.Columns.Add(new DataColumn("Id", typeof(string)));
      Table.Columns.Add(new DataColumn("Name", typeof(string)));
      Table.Columns.Add(new DataColumn("IdTypeMultimediaItem", typeof(string)));

      #region Get Query
      using (var cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        var sqlCommand = "SELECT [Query] FROM [NewsCategory] WHERE [NewsCategory]='" + NewsCategory + "'";

        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        Query = (await cmd.ExecuteScalarAsync()) as string;

        var aQuery = Query.Split(new char[] {'+'});

        var sb = new StringBuilder();
        foreach (var s in aQuery)
        {
          sb.Append(HttpUtility.UrlEncode(s) + "+");
        }

        //EncodedQuery = sb.ToString()+"Россия";
        //EncodedQuery = sb.ToString()+"Canada";

        EncodedQuery = sb.ToString();
        EncodedQuery=EncodedQuery.Remove(EncodedQuery.Length - 1);
      }
      #endregion Get Query

      #region Get News      
      // Version 2
      //var clientNews = new BingSearchContainer(new Uri("https://api.datamarket.azure.com/Bing/search"));
      //clientNews.Credentials = new NetworkCredential("accountKey", "***");

      try
      {
        var client = new HttpClient();

        #region  Google news

        //var aMarketId = MarketId.Split(new char[] { '-' });
        var aMarketId = MarketId.Split(new char[] { '_' });
        var nl = aMarketId[0].Trim().ToLower();
        //var ned = MarketId.Replace("-", "_");
        var ned = MarketId;

        string topic;
        switch (Query)
        {
          case "Business":
            topic = "b";
            break;

          case "Entertainment":
            topic = "e";
            break;

          case "Health":
            topic = "m";
            break;

          case "Politics":
            topic = "b";
            break;

          case "Science+Technology":
            topic = "t";
            break;

          case "Sports":
            topic = "s";
            break;

          case "United+States":
            topic = "b";
            break;

          case "World":
            topic = "w";
            break;

          default:
            topic = "h"; //Other
            break;
        }

        var query = string.Format("topic={0}&nl={1}&ned={2}", topic, nl, ned);
        var uri = "https://news.google.com/news/section?cf=all&output=rss&num=50&" + query;


        //var feed = new SyndicationFeed("Feed Title", "Feed Description", new Uri(uri), "FeedID", DateTime.Now);
        //Rss20FeedFormatter rssFormatter = new Rss20FeedFormatter(feed);

        var reader = System.Xml.XmlReader.Create(uri);
        SyndicationFeed feed2 = SyndicationFeed.Load(reader);

        DataRow row; string Name;
        foreach (var item in feed2.Items)
        {
          row = Table.NewRow();

          row["Id"] = HttpUtility.ParseQueryString(item.Links[0].Uri.ToString())["url"];

          Name = item.Title.Text;

          if (Name.Contains("|"))
          {
            row["Name"] = Name.Replace("|", ".");
          }
          else
          {
            row["Name"] = Name;
          }

          row["IdTypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Audio);

          Table.Rows.Add(row);
        }

        //var response = await client.GetAsync(uri);

        //var stoken = await response.Content.ReadAsStringAsync();
        //var jss = new JavaScriptSerializer();

        //var d = jss.Deserialize<Dictionary<string, string>>(stoken);


        //foreach (var item in d.value)
        //{
        //row = Table.NewRow();

        //row["Id"] = HttpUtility.ParseQueryString(item.url)["r"];

        //Name = item.name;
        //if (Name.Contains("|"))
        //{
        //  row["Name"] = Name.Replace("|", ".");
        //}
        //else
        //{
        //  row["Name"] = Name;
        //}
        ////Name = await GetTitle(item.Url);

        //row["IdTypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Audio);

        //Table.Rows.Add(row);
        //}

        #endregion  Google news

        #region Bing news
        //// Request headers
        //client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "88f10871b9724851b370bec7a25fa1d8");

        //// Request parameters
        //var queryString = HttpUtility.ParseQueryString(string.Empty);
        //queryString["q"] = Query;
        //queryString["q"] = HttpUtility.HtmlEncode("Бизнес") + "+" + HttpUtility.HtmlEncode("Финансы");
        //queryString["mkt"] = MarketId;
        //queryString["setLang"] = MarketId.Split(new char[] { '-' })[0];
        //queryString["count"] = "50";
        //queryString["offset"] = "0";
        //queryString["safeSearch"] = "Moderate";
        //queryString["textDecorations"] = "false";


        //var query = string.Format("q={0}&mkt={1}&setLang={2}&count={3}&offset={4}&safeSearch={5}&textDecorations={6}&freshness={7}", EncodedQuery, MarketId, "RU", "30", "0", "Moderate", "false", "Day");
        //var uri = "https://api.cognitive.microsoft.com/bing/v5.0/news/search?" + query;

        //// Request parameters
        ////queryString["Category"] = "Business";
        ////var uri = "https://api.cognitive.microsoft.com/bing/v5.0/news/?" + queryString;

        //var response = await client.GetAsync(uri);

        //var stoken = await response.Content.ReadAsStringAsync();
        //var jss = new JavaScriptSerializer();
        //var d = jss.Deserialize<BingNews>(stoken);

        //DataRow row; string Name;
        //foreach (var item in d.value)
        //{
        //  row = Table.NewRow();

        //  row["Id"] = HttpUtility.ParseQueryString(item.url)["r"];

        //  Name = item.name;
        //  if (Name.Contains("|"))
        //  {
        //    row["Name"] = Name.Replace("|", ".");
        //  }
        //  else
        //  {
        //    row["Name"] = Name;
        //  }
        //  //Name = await GetTitle(item.Url);

        //  row["IdTypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Audio);

        //  Table.Rows.Add(row);
        //}
        #endregion Bing news

      }
      catch (Exception ex)
      {
      }

      //var marketData = clientNews.News(Query, "EnableHighlighting", MarketId, "Off", null, null, null, NewsCategory, "Date");

      //marketData = marketData.AddQueryOption("$top", 30);
      //var newsResults = marketData.Execute();

      //DataRow row; string Name;
      //foreach (var item in marketData)
      //{
      //  row = Table.NewRow();

      //  row["Id"] = item.Url;

      //  Name = item.Title;
      //  if (Name.Contains("|"))
      //  {
      //    row["Name"] = Name.Replace("|", ".");
      //  }
      //  //Name = await GetTitle(item.Url);
      //  row["Name"] = Name;

      //  row["IdTypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Audio);

      //  Table.Rows.Add(row);
      //}
      #endregion Get News

      return Table;
    }

    public static async Task<long> GetMultimediaJobAttributesNews(string IdItem)
    {
      var IdItemSize = long.MaxValue;

      await Task.Yield();

      return IdItemSize;
    }

    #endregion News

    #region OneDrive

    public static async Task<DataTable> GetItemsByNoneOneDriveAsync(int IdTypeMultimediaSource, string IdItem, string AccessToken)
    {
      #region Define vars
      //string RootFolder;
      #endregion Define vars

      var filter = await OAuthGetOAuthFilterAsync(IdTypeMultimediaSource);

      var ApiUrl = @"https://api.onedrive.com/v1.0/";
      var LiveMulimediaREST = new REST(ApiUrl, AccessToken, "", filter);
      var ListItems = await LiveMulimediaREST.GetItemsByNoneOneDriveAsync(IdItem);
      var TableItems = ConvertFromOAuthToTableItems(ListItems);

      return TableItems;
    }

    public static async Task<DataTable> GetListMultimediaSourceOneDrive(string AccessToken)
    {
      DataTable TableContentMultimediaSource = new DataTable();
      DataRow Row;

      TableContentMultimediaSource.TableName = "TableContentMultimediaSource";
      TableContentMultimediaSource.Columns.Add(new DataColumn("Name", typeof(string)));
      TableContentMultimediaSource.Columns.Add(new DataColumn("Id", typeof(string)));
      TableContentMultimediaSource.Columns.Add(new DataColumn("TypeMultimediaItem", typeof(string)));

      List<OAuthObjectFolder> ListObjectFolder = null;

      var ApiUrl = @"https://apis.live.net/v5.0/";
      var LiveMulimediaREST = new REST(ApiUrl, AccessToken, "me/skydrive/files");
      ListObjectFolder = LiveMulimediaREST.GetListMultimediaFolderOneDrive();

      //foreach (LiveMultimediaOAuth.OAuthObjectFolder ObjectFolder in ListObjectFolder)
      //{
      //  Row = TableContentMultimediaSource.NewRow();
      //  Row["Name"] = ObjectFolder.Name;
      //  Row["Id"] = ObjectFolder.Id;
      //  TableContentMultimediaSource.Rows.Add(Row);
      //}

      foreach (OAuthObjectFolder ObjectFolder in ListObjectFolder)
      {
        Row = TableContentMultimediaSource.NewRow();
        Row["Name"] = ObjectFolder.Name;
        Row["Id"] = ObjectFolder.Id;

        switch (ObjectFolder.Type)
        {
          case "Folder":
            Row["TypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Folder);
            break;
          case "Audio":
            Row["TypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Audio);
            break;
          case "Video":
            Row["TypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Video);
            break;
          case "Image":
            Row["TypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Picture);
            break;
          case "Document":
            Row["TypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Document);
            break;
          default:
            Row["TypeMultimediaItem"] = null;
            break;
        }

        TableContentMultimediaSource.Rows.Add(Row);
      }

      //Группировка
      ////IEnumerable<IGrouping<string, LiveMultimediaOAuth.OAuthObjectFolder>> query = ListObjectFolder.GroupBy(ObjectFolder => ObjectFolder.Name);
      //IEnumerable<IGrouping<string, string>> query = ListObjectFolder.GroupBy(ObjectFolder => ObjectFolder.Name, ObjectFolder => ObjectFolder.Id);

      //DataRow Row;

      ////Type Mytype = typeof(LiveMultimediaOAuth.OAuthObjectFolder);
      ////System.Reflection.MemberInfo[] Mymembersinfoarray = Mytype.GetMembers();

      //foreach (IGrouping<string, string> MultimediaSource in query)
      ////foreach (IGrouping<string, LiveMultimediaOAuth.OAuthObjectFolder> MultimediaSource in query)
      //{
      //  Row = TableContentMultimediaSource.NewRow();
      //  Row["Name"] = MultimediaSource.Key;
      //  Row["Id"] = MultimediaSource.First();
      //  TableContentMultimediaSource.Rows.Add(Row);
      //}

      await Task.Yield();

      return TableContentMultimediaSource;
    }

    public static DataTable GetItemsByFolderOneDrive(string[] ListIdAlbum, string AccessToken)
    {
      var TableMultimediaItem = new DataTable("TableMultimediaItem");
      TableMultimediaItem.Columns.Add(new DataColumn("Id", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Name", typeof(string)));      
      TableMultimediaItem.Columns.Add(new DataColumn("Source", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("TypeMultimediaItem", typeof(string)));

      DataRow Row;

      List<OAuthObjectAudio> ListMultimediaObject = null;
      string ApiUrl = @"https://apis.live.net/v5.0/";
      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken, "me/skydrive");
      ListMultimediaObject = LiveMulimediaREST.GetListMultimediaItem(ListIdAlbum);

      IEnumerable<OAuthObjectAudio> ListMultimediaObjectSort = ListMultimediaObject.OrderBy(MultimediaObject => MultimediaObject.Name);

      string[] a; string TypeMultimedia;
      foreach (OAuthObjectAudio MultimediaObject in ListMultimediaObjectSort)
      {
        Row = TableMultimediaItem.NewRow();

        a = MultimediaObject.Name.Split(new Char[] { '.' });
        TypeMultimedia = a[a.Length - 1];

        Row["Id"] = MultimediaObject.Id;
        Row["Name"] = MultimediaObject.Name;        
        Row["Source"] = MultimediaObject.Source;
        Row["TypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Audio);

        TableMultimediaItem.Rows.Add(Row);
      }

      return TableMultimediaItem;
    }

    public static async Task<long> GetMultimediaJobAttributesOneDrive(string AccessToken, string IdItem)
    {
      //var ApiUrl = @"https://api.onedrive.com/v1.0/";
      var ApiUrl = @"https://graph.microsoft.com/v1.0/me/";

      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken);
      var IdItemSize = await LiveMulimediaREST.GetMultimediaItemSourceOneDrive(IdItem);

      return IdItemSize;
    }

    #endregion OneDrive

    #region Google Drive

    public static async Task<DataTable> GetItemsByNoneGoogleDriveAsync(int IdTypeMultimediaSource, string IdItem, string AccessToken)
    {
      #region Define vars
      //string RootFolder;
      #endregion Define vars

      var ApiUrl = @"https://www.googleapis.com/drive/v3/files";

      var filter = await OAuthGetOAuthFilterAsync(IdTypeMultimediaSource);      
      var LiveMulimediaREST = new REST(ApiUrl, AccessToken, "", filter);
      var ListItems = await LiveMulimediaREST.GetItemsByNoneGoogleDriveAsync(IdItem);

      var TableItems = ConvertFromOAuthToTableItems(ListItems);

      return TableItems;
    }

    public static DataTable GetListMultimediaSourceGoogleDrive(string AccessToken)
    {
      List<OAuthObjectFolder> ListItems = null;
      string ApiUrl = @"https://www.googleapis.com/drive/v2/files";
      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken, "");
      ListItems = LiveMulimediaREST.GetListMultimediaFolderGoogleDrive();

      var TableItems = ConvertFromOAuthToTableItems(ListItems);

      return TableItems;
    }

    public static DataTable GetItemsByFolderGoogleDrive(string[] ListIdAlbum, string AccessToken)
    {
      var TableMultimediaItem = new DataTable("TableMultimediaItem");
      TableMultimediaItem.Columns.Add(new DataColumn("Id", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Name", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("TypeMultimediaItem", typeof(string)));

      DataRow Row;

      List<OAuthObjectAudio> ListMultimediaObject = null;
      string ApiUrl = @"https://www.googleapis.com/drive/v2/files";
      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken);
      ListMultimediaObject = LiveMulimediaREST.GetListMultimediaItemGoogleDrive(ListIdAlbum);

      foreach (OAuthObjectAudio MultimediaObject in ListMultimediaObject)
      {
        Row = TableMultimediaItem.NewRow();

        Row["Id"] = MultimediaObject.Id;
        Row["Name"] = MultimediaObject.Name;
        Row["TypeMultimediaItem"] = Convert.ToInt32(enumTypeMultimediaItem.Audio);

        TableMultimediaItem.Rows.Add(Row);
      }

      return TableMultimediaItem;
    }

    public static async Task<long> GetMultimediaJobAttributesGoogleDrive(string AccessToken, string IdItem)
    {
      var ApiUrl = @"https://www.googleapis.com/drive/v3";

      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken);
      var IdItemSize = await LiveMulimediaREST.GetMetadataGoogleDriveAsync(IdItem);

      return IdItemSize;
    }

    #endregion Google Drive

    #region VKontakte

    public static async Task<DataTable> GetItemsByNoneVKontakteAsync(int IdTypeMultimediaSource, string IdItem, string AccessToken)
    {
      var ApiUrl = @"https://api.vk.com/method";
      var RootFolder = "/execute";
      var filter = await OAuthGetOAuthFilterAsync(IdTypeMultimediaSource);

      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken, RootFolder, filter);
      var ListItems = await LiveMulimediaREST.GetItemsByNoneVKontakteAsync(IdItem);

      //Сортировка
      //var ListSortedItems = ListItems.OrderBy(OAuthObjectFolder => OAuthObjectFolder.Name);

      var TableItems = ConvertFromOAuthToTableItems(ListItems);

      return TableItems;
    }

    public static async Task<string> GetDownloadURLVKontakteAsync(string IdItem, int IdTypeMultimediaSource, string AccessToken)
    {
      var ApiUrl = @"https://api.vk.com/method";
      var RootFolder = "/execute";
      var filter = await OAuthGetOAuthFilterAsync(IdTypeMultimediaSource);

      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken, RootFolder, filter);
      var DownloadURL = await LiveMulimediaREST.GetDownloadURLVKontakte(IdItem);

      return DownloadURL;
    }

    public static async Task<long> GetMultimediaJobAttributesVKontakte(string IdItem, int IdTypeMultimediaSource, string AccessToken)
    {
      //await Task.Yield();
      long IdItemSize;

      var DownloadURL = await GetDownloadURLVKontakteAsync(IdItem, IdTypeMultimediaSource, AccessToken);

      try
      {
        var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(DownloadURL);
        //myHttpWebRequest.AddRange(0, 0);

        using (var myHttpWebResponse = (HttpWebResponse)await myHttpWebRequest.GetResponseAsync())
        {
          var OAuthObjectHeader = myHttpWebResponse.Headers["Content-Length"];
          if (JetSASLibrary.CheckGoodString(OAuthObjectHeader)) //Link to VKontakte player
          {
            IdItemSize = Convert.ToInt64(OAuthObjectHeader);
          }
          else //Direcct link to file
          {
            var aContentRange = myHttpWebResponse.Headers["Content-Range"].Split(new char[] { '/' });
            IdItemSize = Convert.ToInt64(aContentRange[1]);
          }
        }
      }
      catch (Exception)
      {
        IdItemSize = -1;
      }

      //var IdItemSize = long.MaxValue;
      return IdItemSize;
    }

    #endregion VKontakte

    #region Dropbox
    public static async Task<DataTable> GetItemsByNoneDropboxAsync(int IdTypeMultimediaSource, string IdItem, string AccessToken)
    {      
      var ApiUrl = @"https://api.dropboxapi.com/2";
      var RootFolder = "/files";

      var ListFilterExtensions = new StringDictionary();

      var TableFilters = await SelectMultimediaExtensionsByMultimediaSourceAsync(IdTypeMultimediaSource);
      foreach (DataRow item in TableFilters.Rows)
      {
        ListFilterExtensions.Add(item.Field<string>("Extension"), item.Field<string>("Type"));
      }

      var LiveMulimediaREST = new REST(ApiUrl, AccessToken, RootFolder, "");
      var ListItems = await LiveMulimediaREST.GetItemsByNoneDropboxAsync(IdItem, ListFilterExtensions);

      var TableItems = ConvertFromOAuthToTableItems(ListItems);

      return TableItems;
    }

    public static async Task<long> GetMultimediaJobAttributesDropbox(string IdItem, string AccessToken)
    {
      var ApiUrl = @"https://api.dropboxapi.com/2";
      var RootFolder = "/files";

      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken, RootFolder, "");
      var IdItemSize = (await LiveMulimediaREST.GetMetadataDropboxAsync(IdItem)).Item2;

      return IdItemSize;
    }

    public static async Task<string> GetDownloadURLDropboxAsync(string IdItem, string AccessToken)
    {
      var ApiUrl = @"https://api.dropboxapi.com/2";
      var RootFolder = "/files";

      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken, RootFolder, "");
      var path_lower = (await LiveMulimediaREST.GetMetadataDropboxAsync(IdItem)).Item1;

      var ApiUrlDownload = @"https://content.dropboxapi.com";
      var DownloadUrl = ApiUrlDownload + "/1/files/auto" + path_lower;

      return DownloadUrl;
    }

    #endregion Dropbox

    #region YandexDisk

    #region Group by Folder
    public static async Task<DataTable> GetFoldersByFolderYandexDiskAsync(int IdTypeMultimediaSource, string IdItem, string AccessToken)
    {
      var ApiUrl = @"https://cloud-api.yandex.net/v1";
      var RootFolder = "/disk/resources";
      var filter = await OAuthGetOAuthFilterAsync(IdTypeMultimediaSource);

      var LiveMulimediaREST = new REST(ApiUrl, AccessToken, RootFolder, filter);
      var ListItems = await LiveMulimediaREST.GetItemsYandexDiskAsync(IdItem);

      var query = ListItems.GroupBy(MultimediaItem => MultimediaItem.IdFromObject, MultimediaItem => MultimediaItem.IdFromObject);

      var ListGroupedItems = new List<OAuthObjectFolder>(); OAuthObjectFolder MultimediaGroupedItem;
      foreach (IGrouping<string, string> AlbumGroup in query)
      {
        MultimediaGroupedItem = new OAuthObjectFolder();
        MultimediaGroupedItem.Id = AlbumGroup.Key;
        MultimediaGroupedItem.Name = AlbumGroup.Key;
        MultimediaGroupedItem.Type = "Folder";

        ListGroupedItems.Add(MultimediaGroupedItem);
      }    

      var TableItems = ConvertFromOAuthToTableItems(ListGroupedItems);

      return TableItems;
    }

    public static async Task<DataTable> GetItemsByFolderYandexDiskAsync(int IdTypeMultimediaSource, string IdItem, string AccessToken)
    {
      var ApiUrl = @"https://cloud-api.yandex.net/v1";
      var RootFolder = "/disk/resources";
      var filter = await OAuthGetOAuthFilterAsync(IdTypeMultimediaSource);

      var LiveMulimediaREST = new REST(ApiUrl, AccessToken, RootFolder, filter);
      var ListItems = await LiveMulimediaREST.GetItemsYandexDiskAsync(IdItem);

      var query = ListItems.Where(MultimediaItem => MultimediaItem.IdFromObject==IdItem);

      var ListFilteredItems = new List<OAuthObjectFolder>(); OAuthObjectFolder MultimediaFilteredItem;
      foreach (var item in query)
      {
        MultimediaFilteredItem = new OAuthObjectFolder();
        MultimediaFilteredItem.Id = item.Id;
        MultimediaFilteredItem.Name = item.Name;
        MultimediaFilteredItem.Type = item.Type;

        ListFilteredItems.Add(MultimediaFilteredItem);
      }

      var TableItems = ConvertFromOAuthToTableItems(ListFilteredItems);

      return TableItems;
    }
    #endregion Group by Folder

    public static async Task<long> GetMultimediaJobAttributesYandexDisk(string IdItem, string AccessToken)
    {
      var ApiUrl = @"https://cloud-api.yandex.net/v1";
      var RootFolder = "/disk/resources";

      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken, RootFolder, "");
      var IdItemSize = await LiveMulimediaREST.GetMetadataYandexDiskAsync(IdItem);

      return IdItemSize;
    }

    public static async Task<string> GetDownloadURLYandexDiskAsync(string IdItem, string AccessToken)
    {
      var ApiUrl = @"https://cloud-api.yandex.net/v1";
      var RootFolder = "/disk/resources/download";

      REST LiveMulimediaREST = new REST(ApiUrl, AccessToken, RootFolder, "");
      var DownloadUrl = await LiveMulimediaREST.GetDownloadURLYandexDiskAsync(IdItem);

      return DownloadUrl;
    }

    #endregion YandexDisk

    #region Private functions

    #region OAuth
    private static async Task<string> OAuthGetOAuthFilterAsync(int IdTypeMultimediaSource)
    {
      var Table = new DataTable("TableOAuthFilter");

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        // Вариант с расположением фильтров в таблице [TypeMultimedia]
        //var sqlCommand = "SELECT [OAuthFilter] FROM [dbo].[TypeMultimedia] ";
        //sqlCommand += "INNER JOIN (SELECT [TypeMultimediaItem] FROM [dbo].[TypeMultimediaSource] ";
        //sqlCommand += "WHERE IdTypeMultimediaSource=" + IdTypeMultimediaSource.ToString() + ") AS TMS ON TMS.[TypeMultimediaItem]=[TypeMultimedia].[TypeMultimediaItem] GROUP BY [OAuthFilter]";

        // Вариант с расположением фильтров в таблице [TypeMultimediaOAuthFilter]
        var sqlCommand = "SELECT [OAuthFilter] FROM [dbo].[TypeMultimediaOAuthFilter] ";
        sqlCommand += "INNER JOIN (SELECT [TypeMultimediaItem],[IdTypeMultimediaOAuth] FROM [dbo].[TypeMultimediaSource] WHERE IdTypeMultimediaSource=" + IdTypeMultimediaSource.ToString() + ") AS TMS ";
        sqlCommand += "ON TMS.[TypeMultimediaItem]=[TypeMultimediaOAuthFilter].[IdTypeMultimediaItem] AND TMS.[IdTypeMultimediaOAuth]=[TypeMultimediaOAuthFilter].[IdTypeMultimediaOAuth] ";
        sqlCommand += "GROUP BY [OAuthFilter]";

        var cmd = new SqlCommand(sqlCommand, cn);
        cmd.CommandType = CommandType.Text;

        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        {
          IDataRecord record; DataRow Row;

          var IsFirstRow = true;
          while (await reader.ReadAsync())
          {
            record = reader as IDataRecord;

            if (IsFirstRow)
            {
              IsFirstRow = false;
              for (int i = 0; i < record.FieldCount; i++)
              {
                Table.Columns.Add(new DataColumn(record.GetName(i), record.GetFieldType(i)));
              }
            }

            Row = Table.NewRow();
            for (int i = 0; i < record.FieldCount; i++)
            {
              Row[i] = record.GetValue(i);
            }

            Table.Rows.Add(Row);
          }
        }
      }

      var filter = "";
      foreach (DataRow RowOAuthFilter in Table.Rows)
      {
        filter = RowOAuthFilter["OAuthFilter"].ToString();
      }

      return filter;
    }

    private static DataTable ConvertFromOAuthToTableItems(List<OAuthObjectFolder> ListItems)
    {
      #region Define vars
      DataTable TableItems = new DataTable("TableItems");
      DataRow Row;
      #endregion Define vars

      #region Define columns      
      TableItems.Columns.Add(new DataColumn("Id", typeof(string)));
      TableItems.Columns.Add(new DataColumn("Name", typeof(string)));
      TableItems.Columns.Add(new DataColumn("IdTypeMultimediaItem", typeof(int)));
      #endregion Define columns

      int IdTypeMultimediaItem;

      #region Convert to table
      foreach (OAuthObjectFolder ObjectFolder in ListItems)
      {
        Row = TableItems.NewRow();
        
        Row["Id"] = ObjectFolder.Id;
        Row["Name"] = ObjectFolder.Name;

        #region Define TypeMultimediaItem

        switch (ObjectFolder.Type.ToLower())
        {
          case "folder":
            IdTypeMultimediaItem = Convert.ToInt32(enumTypeMultimediaItem.Folder);
            break;
          case "audio":
            IdTypeMultimediaItem = Convert.ToInt32(enumTypeMultimediaItem.Audio);
            break;
          case "video":
            IdTypeMultimediaItem = Convert.ToInt32(enumTypeMultimediaItem.Video);
            break;
          case "image":
            // enumTypeMultimediaItem.Picture;
            IdTypeMultimediaItem = Convert.ToInt32(enumTypeMultimediaItem.Unsupported);
            break;
          case "document":
            // enumTypeMultimediaItem.Document;
            IdTypeMultimediaItem = Convert.ToInt32(enumTypeMultimediaItem.Unsupported);
            break;
          case "unsupported":
          default:
            IdTypeMultimediaItem = Convert.ToInt32(enumTypeMultimediaItem.Unsupported);
            break;
        }
        #endregion Define TypeMultimediaItem

        Row["IdTypeMultimediaItem"] = IdTypeMultimediaItem;

        TableItems.Rows.Add(Row);
      }
      #endregion Convert to table

      return TableItems;
    }
    #endregion OAuth

    #region Get List Multimedia Source

    public static async Task<DataTable> GetListMultimediaSourceAlbums(string UserToken)
    {
      Int64 IdUser;
      DataTable TableContentMultimediaSource = new DataTable("TableContentMultimediaSource");
      TableContentMultimediaSource.Columns.Add(new DataColumn("Name", typeof(string)));
      TableContentMultimediaSource.Columns.Add(new DataColumn("Id", typeof(string)));

      DataRow MultimediaFileRow;
      IdUser = await GetIdUserByUserTokenAsync(UserToken, 2);

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand("GetContentMultimediaSourceAlbum", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter parameter;

        parameter = cmd.Parameters.AddWithValue("@IdUser", IdUser);
        parameter.SqlDbType = SqlDbType.Int;

        SqlDataReader ListAlbumsReader = await cmd.ExecuteReaderAsync();

        while (await ListAlbumsReader.ReadAsync())
        {
          IDataRecord record = (IDataRecord)ListAlbumsReader;
          MultimediaFileRow = TableContentMultimediaSource.NewRow();
          MultimediaFileRow["Name"] = record["Album"];
          MultimediaFileRow["Id"] = record["Album"];
          TableContentMultimediaSource.Rows.Add(MultimediaFileRow);
        }
        ListAlbumsReader.Close();
      }
      return TableContentMultimediaSource;
    }

    private static async Task<DataTable> GetListMultimediaSourcePlaylists(string UserToken)
    {
      Int64 IdUser;
      DataTable TableContentMultimediaSource = new DataTable("TableContentMultimediaSource");
      TableContentMultimediaSource.Columns.Add(new DataColumn("Name", typeof(string)));
      TableContentMultimediaSource.Columns.Add(new DataColumn("Id", typeof(string)));

      DataRow MultimediaFileRow;
      IdUser = await GetIdUserByUserTokenAsync(UserToken, 2);

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = cn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "GetContentMultimediaSourcePlaylist";

        SqlParameter parameter;

        parameter = new SqlParameter();
        parameter.ParameterName = "@UserToken";
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = UserToken;
        cmd.Parameters.Add(parameter);

        SqlDataReader ListPlaylistReader = await cmd.ExecuteReaderAsync();

        while (await ListPlaylistReader.ReadAsync())
        {
          IDataRecord record = (IDataRecord)ListPlaylistReader;

          MultimediaFileRow = TableContentMultimediaSource.NewRow();
          MultimediaFileRow["Id"] = record["IdPlaylist"];
          MultimediaFileRow["Name"] = record["Playlist"];
          TableContentMultimediaSource.Rows.Add(MultimediaFileRow);
        }
        ListPlaylistReader.Close();
      }
      return TableContentMultimediaSource;
    }

    private static DataTable GetListMultimediaSourceFacebook(string AccessToken)
    {
      DataTable dta = new DataTable();

      dta.TableName = "TableContentMultimediaSource";
      dta.Columns.Add(new DataColumn("Album", typeof(string)));

      DataTable dt = GetListMultimediaItemFacebook(new string[1], "");
      List<MultimediaFile> lmf = new List<MultimediaFile>();
      foreach (DataRow dr in dt.Rows)
      {
        MultimediaFile mf = new MultimediaFile();
        mf.Album = dr["Album"].ToString();
        lmf.Add(mf);
      }
      IEnumerable<IGrouping<string, string>> query = lmf.GroupBy(MultimediaFile => MultimediaFile.Album, MultimediaFile => MultimediaFile.Album);

      DataRow dra;
      foreach (IGrouping<string, string> AlbumGroup in query)
      {
        dra = dta.NewRow();
        dra["Album"] = AlbumGroup.Key;
        dta.Rows.Add(dra);
      }

      return dta;
    }

    private static DataTable GetListMultimediaSourceRadio(string UserToken)
    {
      DataTable dta = new DataTable();

      dta.TableName = "TableContentMultimediaSource";
      dta.Columns.Add(new DataColumn("Album", typeof(string)));

      DataTable dt = GetListMultimediaItemRadio(new string[1], "");
      List<MultimediaFile> lmf = new List<MultimediaFile>();
      foreach (DataRow dr in dt.Rows)
      {
        MultimediaFile mf = new MultimediaFile();
        mf.Album = dr["Album"].ToString();
        lmf.Add(mf);
      }
      IEnumerable<IGrouping<string, string>> query = lmf.GroupBy(MultimediaFile => MultimediaFile.Album, MultimediaFile => MultimediaFile.Album);

      DataRow dra;
      foreach (IGrouping<string, string> AlbumGroup in query)
      {
        dra = dta.NewRow();
        dra["Album"] = AlbumGroup.Key;
        dta.Rows.Add(dra);
      }

      return dta;
    }

    private static async Task<DataTable> GetListMultimediaSourceNews(string UserToken, string LanguageNews)
    {
      string DefaultLanguageNews = "en";

      DataTable TableContentMultimediaSource = new DataTable();
      DataRow Row;

      TableContentMultimediaSource.TableName = "TableContentMultimediaSource";
      TableContentMultimediaSource.Columns.Add(new DataColumn("Name", typeof(string)));
      TableContentMultimediaSource.Columns.Add(new DataColumn("Id", typeof(string)));

      #region Set Category News
      Row = TableContentMultimediaSource.NewRow();
      Row["Name"] = "Business";
      Row["Id"] = 1;
      TableContentMultimediaSource.Rows.Add(Row);

      Row = TableContentMultimediaSource.NewRow();
      Row["Name"] = "Entertainment";
      Row["Id"] = 2;
      TableContentMultimediaSource.Rows.Add(Row);

      Row = TableContentMultimediaSource.NewRow();
      Row["Name"] = "Health";
      Row["Id"] = 3;
      TableContentMultimediaSource.Rows.Add(Row);

      Row = TableContentMultimediaSource.NewRow();
      Row["Name"] = "Politics";
      Row["Id"] = 4;
      TableContentMultimediaSource.Rows.Add(Row);

      Row = TableContentMultimediaSource.NewRow();
      Row["Name"] = "Sports";
      Row["Id"] = 5;
      TableContentMultimediaSource.Rows.Add(Row);

      Row = TableContentMultimediaSource.NewRow();
      Row["Name"] = "US";
      Row["Id"] = 6;
      TableContentMultimediaSource.Rows.Add(Row);

      Row = TableContentMultimediaSource.NewRow();
      Row["Name"] = "World";
      Row["Id"] = 7;
      TableContentMultimediaSource.Rows.Add(Row);

      Row = TableContentMultimediaSource.NewRow();
      Row["Name"] = "Science And Technology";
      Row["Id"] = 8;
      TableContentMultimediaSource.Rows.Add(Row);
      #endregion Set Category News

      if (DefaultLanguageNews != LanguageNews.ToLower())
      {
        #region Translate Category News
        AdmAccessToken admToken;
        string headerValue;
        AdmAuthentication admAuth = new AdmAuthentication(UserData.clientID, UserData.clientSecret);
        admToken = await admAuth.GetAccessTokenAsync();
        DateTime tokenReceived = DateTime.Now;
        headerValue = "Bearer " + admToken.access_token;

        //Set Authorization header before sending the request
        HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
        httpRequestProperty.Method = "POST";
        httpRequestProperty.Headers.Add("Authorization", headerValue);

        var TranslatorClient = new TranslatorService.LanguageServiceClient();

        List<string> ListNewsCategory = new List<string>();
        foreach (DataRow itemRow in TableContentMultimediaSource.Rows)
        {
          ListNewsCategory.Add(itemRow["Name"].ToString());
        }

        TranslatorService.TranslateArrayResponse[] TranslateResponse;

        using (OperationContextScope scope = new OperationContextScope(TranslatorClient.InnerChannel))
        {
          System.ServiceModel.OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

          TranslatorService.TranslateOptions TranslateOptions = new TranslatorService.TranslateOptions();
          TranslateOptions.Category = "general";
          TranslateOptions.ContentType = "text/plain"; //"text/plain" OR "text/html"

          TranslateResponse = TranslatorClient.TranslateArray("", ListNewsCategory.ToArray(), DefaultLanguageNews, LanguageNews, TranslateOptions);
        }

        int i = 0;
        foreach (DataRow itemRow in TableContentMultimediaSource.Rows)
        {
          itemRow["Name"] = TranslateResponse[i].TranslatedText;
          i++;
        }
        #endregion Translate Category News
      }

      return TableContentMultimediaSource;
    }

    #endregion Get List Multimedia Source

    #region Get List Multimedia Items

    public static async Task<DataTable> GetListMultimediaItemAlbum(string UserToken, string[] ListIdAlbum)
    {
      Int64 IdUser;
      DataTable MultimediaFileTable;
      MultimediaFileTable = CreateTableMultimediaFileNames();

      DataRow MultimediaFileRow;

      IdUser = await GetIdUserByUserTokenAsync(UserToken, 2);

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = cn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "GetListMultimediaFilesByAlbum";

        SqlParameter parameter;
        parameter = new SqlParameter();
        parameter.ParameterName = "@IdUser";
        parameter.SqlDbType = SqlDbType.Int;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = IdUser;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@Album";
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = ListIdAlbum[0];
        cmd.Parameters.Add(parameter);

        SqlDataReader ListMultimediaFiles = await cmd.ExecuteReaderAsync();

        while (await ListMultimediaFiles.ReadAsync())
        {
          IDataRecord record = (IDataRecord)ListMultimediaFiles;
          MultimediaFileRow = MultimediaFileTable.NewRow();

          MultimediaFileRow["Name"] = record["DisplayName"];
          MultimediaFileRow["Id"] = record["MultimediaFileGUID"].ToString().ToUpper();
          MultimediaFileRow["Album"] = record["Album"];
          MultimediaFileRow["TypeMultimedia"] = record["TypeMultimedia"];
          MultimediaFileRow["IsEmbeddable"] = false;

          MultimediaFileTable.Rows.Add(MultimediaFileRow);
        }
        ListMultimediaFiles.Close();
      }
      return MultimediaFileTable;


      //IEnumerable<MultimediaFile> queryOrderByDisplayName = ListMultimedia.OrderBy(MultimediaFile => MultimediaFile.DisplayName);

      //DataRow drMultimediaFile;
      //foreach (MultimediaFile mf in queryOrderByDisplayName)
      //{
      //  drMultimediaFile = dtListMultimedia.NewRow();

      //  drMultimediaFile["DisplayName"] = mf.DisplayName;
      //  drMultimediaFile["MultimediaFileGUID"] = mf.MultimediaFileGUID;
      //  drMultimediaFile["Album"] = mf.Album;

      //  dtListMultimedia.Rows.Add(drMultimediaFile);
      //}
    }

    private static async Task<DataTable> GetListMultimediaItemPlaylist(string UserToken, string[] ListIdAlbum)
    {
      DataTable MultimediaFileTable;
      MultimediaFileTable = CreateTableMultimediaFileNames();

      DataRow MultimediaFileRow;

      using (SqlConnection cn = new SqlConnection(ConnectionString))
      {
        await cn.OpenAsync();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = cn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "GetListMultimediaItemPlaylist";

        SqlParameter parameter;

        parameter = new SqlParameter();
        parameter.ParameterName = "@UserToken";
        parameter.SqlDbType = SqlDbType.VarChar;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = UserToken;
        cmd.Parameters.Add(parameter);

        parameter = new SqlParameter();
        parameter.ParameterName = "@IdPlaylist";
        parameter.SqlDbType = SqlDbType.BigInt;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = Convert.ToInt64(ListIdAlbum[0]);
        cmd.Parameters.Add(parameter);

        SqlDataReader ListMultimediaItem = await cmd.ExecuteReaderAsync();

        string[] a; string TypeMultimedia;
        while (await ListMultimediaItem.ReadAsync())
        {
          IDataRecord record = (IDataRecord)ListMultimediaItem;
          MultimediaFileRow = MultimediaFileTable.NewRow();

          a = record["MultimediaItem"].ToString().Split(new Char[] { '.' });
          TypeMultimedia = a[a.Length - 1];

          MultimediaFileRow["IdFromObject"] = record["IdPlaylistItem"].ToString();
          MultimediaFileRow["Id"] = record["IdMultimediaItem"].ToString();
          MultimediaFileRow["Name"] = record["MultimediaItem"].ToString();
          MultimediaFileRow["Album"] = record["IdTypeMultimediaSource"].ToString();
          MultimediaFileRow["TypeMultimedia"] = TypeMultimedia;
          MultimediaFileRow["IsEmbeddable"] = false;

          MultimediaFileTable.Rows.Add(MultimediaFileRow);
        }
        ListMultimediaItem.Close();
      }
      return MultimediaFileTable;
    }

    private static DataTable GetListMultimediaItemFacebook(string[] ListIdAlbum, string AccessToken)
    {
      DataTable dt = CreateTableMultimediaFileNames();

      DataRow dr;
      string Album;

      string UserConfig = FolderConfig + @"\content_facebook";
      if (File.Exists(UserConfig) == true)
      {
        string[] a;
        List<MultimediaFile> lmf = new List<MultimediaFile>();
        StreamReader sr = new StreamReader(UserConfig, Encoding.UTF8);
        while (!sr.EndOfStream)
        {
          string FullPath = sr.ReadLine();
          string DisplayName = sr.ReadLine();
          string MultimediaFileGUID = sr.ReadLine();
          string TypeAudio = sr.ReadLine();

          a = FullPath.Split(new Char[] { '\\' });
          Album = a[a.Length - 2];

          MultimediaFile mf = new MultimediaFile();

          mf.FullPath = FullPath;
          mf.DisplayName = DisplayName;
          mf.MultimediaFileGUID = MultimediaFileGUID;
          mf.TypeMultimedia = TypeAudio;
          mf.Album = Album;

          lmf.Add(mf);
        }
        sr.Close();

        IEnumerable<MultimediaFile> queryOrderByAlbum = lmf.OrderBy(MultimediaFile => MultimediaFile.Album);

        foreach (MultimediaFile mf in queryOrderByAlbum)
        {
          dr = dt.NewRow();

          dr["FullPath"] = mf.FullPath;
          dr["DisplayName"] = mf.DisplayName;
          dr["MultimediaFileGUID"] = mf.MultimediaFileGUID;
          dr["TypeAudio"] = mf.TypeMultimedia;
          dr["isSelectMultimediaFile"] = false;
          dr["Album"] = mf.Album;

          dt.Rows.Add(dr);
        }
      }

      return dt;
    }

    private static DataTable GetListMultimediaItemVKontakte(string[] ListIdAlbum, string AccessToken)
    {
      DataTable TableMultimediaItem = new DataTable();
      TableMultimediaItem = CreateTableMultimediaFileNames();

      DataRow Row;

      List<OAuthObjectAudio> ListMultimediaObject = null;
      string ApiUrl = @"https://api.vk.com/method/";
      LiveMultimediaREST.REST LiveMulimediaREST = new REST(ApiUrl, AccessToken);
      ListMultimediaObject = LiveMulimediaREST.GetListMultimediaItemVKontakte(ListIdAlbum);

      IEnumerable<LiveMultimediaOAuth.OAuthObjectAudio> ListMultimediaObjectSort = ListMultimediaObject.OrderBy(MultimediaObject => MultimediaObject.Name);

      string[] a; string TypeMultimedia;
      foreach (LiveMultimediaOAuth.OAuthObjectAudio MultimediaObject in ListMultimediaObjectSort)
      {
        Row = TableMultimediaItem.NewRow();

        a = MultimediaObject.Name.Split(new Char[] { '.' });
        TypeMultimedia = a[a.Length - 1];

        Row["Id"] = MultimediaObject.Id;
        Row["Name"] = MultimediaObject.Name;
        Row["IsEmbeddable"] = MultimediaObject.IsEmbeddable;
        Row["Source"] = MultimediaObject.Source;
        Row["Link"] = MultimediaObject.Link;
        Row["Title"] = MultimediaObject.Title;
        Row["Artist"] = MultimediaObject.Artist;
        Row["Album"] = MultimediaObject.Album;
        Row["AlbumArtist"] = MultimediaObject.AlbumArtist;

        TableMultimediaItem.Rows.Add(Row);
      }

      return TableMultimediaItem;
    }

    private static DataTable GetListMultimediaItemRadio(string[] ListIdAlbum, string AccessToken)
    {
      DataTable dt = CreateTableMultimediaFileNames();

      DataRow dr;
      string Album;

      string UserConfig = FolderConfig + @"\content_radio";
      if (File.Exists(UserConfig) == true)
      {
        string[] a;
        List<MultimediaFile> lmf = new List<MultimediaFile>();
        StreamReader sr = new StreamReader(UserConfig, Encoding.UTF8);
        while (!sr.EndOfStream)
        {
          string FullPath = sr.ReadLine();
          string DisplayName = sr.ReadLine();
          string MultimediaFileGUID = sr.ReadLine();
          string TypeAudio = sr.ReadLine();

          a = FullPath.Split(new Char[] { '\\' });
          Album = a[a.Length - 2];

          MultimediaFile mf = new MultimediaFile();

          mf.FullPath = FullPath;
          mf.DisplayName = DisplayName;
          mf.MultimediaFileGUID = MultimediaFileGUID;
          mf.TypeMultimedia = TypeAudio;
          mf.Album = Album;

          lmf.Add(mf);
        }
        sr.Close();

        IEnumerable<MultimediaFile> queryOrderByAlbum = lmf.OrderBy(MultimediaFile => MultimediaFile.Album);

        foreach (MultimediaFile mf in queryOrderByAlbum)
        {
          dr = dt.NewRow();

          dr["FullPath"] = mf.FullPath;
          dr["DisplayName"] = mf.DisplayName;
          dr["MultimediaFileGUID"] = mf.MultimediaFileGUID;
          dr["TypeAudio"] = mf.TypeMultimedia;
          dr["isSelectMultimediaFile"] = false;
          dr["Album"] = mf.Album;

          dt.Rows.Add(dr);
        }
      }

      return dt;
    }

    private static async Task<DataTable> GetListMultimediaItemNews(string UserToken, string[] ListIdAlbum, string CultureNameNews, string LanguageNews, string KeywordNews)
    {
      string DefaultLanguageNews = "en";
      string LanguageCultureNameNews;

      DataTable TableMultimediaItem = new DataTable();
      TableMultimediaItem = CreateTableMultimediaFileNames();

      try
      {
        DataRow Row;
        int IdNews = Convert.ToInt32(ListIdAlbum[0]);

        #region Set Id News
        string queryNews; string NewsCategory;

        switch (IdNews)
        {
          case 1:
            queryNews = "Business";
            NewsCategory = "rt_Business";
            break;
          case 2:
            queryNews = "Entertainment";
            //NewsCategory = "rt_Entertainment";
            NewsCategory = null;
            break;
          case 3:
            queryNews = "Health";
            NewsCategory = "rt_Health";
            break;
          case 4:
            queryNews = "Politics";
            //NewsCategory = "rt_Politics";
            NewsCategory = null;
            break;
          case 5:
            queryNews = "Sports";
            NewsCategory = "rt_Sports";
            break;
          case 6:
            queryNews = "United States";
            //NewsCategory = "rt_US";
            NewsCategory = null;
            break;
          case 7:
            queryNews = "World";
            NewsCategory = "rt_World";
            break;
          case 8:
            //queryNews = "Science";
            queryNews = "Technology";
            //NewsCategory = "rt_ScienceAndTechnology";
            NewsCategory = null;
            break;
          default:
            //queryNews = "Всё";
            queryNews = "World";
            NewsCategory = "rt_World";
            break;
        }
        #endregion Set Id News

        #region Prepare translate
        AdmAccessToken admToken;
        string headerValue;
        AdmAuthentication admAuth = new AdmAuthentication(UserData.clientID, UserData.clientSecret);
        admToken = await admAuth.GetAccessTokenAsync();
        DateTime tokenReceived = DateTime.Now;
        headerValue = "Bearer " + admToken.access_token;

        //Set Authorization header before sending the request
        HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
        httpRequestProperty.Method = "POST";
        httpRequestProperty.Headers.Add("Authorization", headerValue);

        var TranslatorClient = new TranslatorService.LanguageServiceClient();

        TranslatorService.TranslateOptions TranslateOptions = new TranslatorService.TranslateOptions();
        TranslateOptions.Category = "general";
        TranslateOptions.ContentType = "text/plain"; //"text/plain" OR "text/html"

        #endregion Prepare translate

        #region Get Language of CultureNameNews
        var aCultureNameNews = CultureNameNews.Split(new Char[] { '-' });
        if (aCultureNameNews.Length == 1)
          LanguageCultureNameNews = aCultureNameNews[0].ToLower();
        else
          LanguageCultureNameNews = aCultureNameNews[1].ToLower();
        #endregion Get Language of CultureNameNews

        if (string.IsNullOrEmpty(KeywordNews) || string.IsNullOrWhiteSpace(KeywordNews))
        {
          if (DefaultLanguageNews != LanguageCultureNameNews)
          {
            #region Translate keyword news
            string translationKeywordNews;
            using (OperationContextScope scope = new OperationContextScope(TranslatorClient.InnerChannel))
            {
              System.ServiceModel.OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
              translationKeywordNews = TranslatorClient.Translate("", queryNews, DefaultLanguageNews, LanguageCultureNameNews, TranslateOptions.ContentType, TranslateOptions.Category);
            }
            KeywordNews = translationKeywordNews;
            #endregion Translate keyword news
          }
        }

        #region Get News
        var clientNews = new BingSearchContainer(new Uri("https://api.datamarket.azure.com/Data.ashx/Bing/Search/v1/"));
        clientNews.Credentials = new NetworkCredential("accountKey", "***");

        var marketData = clientNews.News(KeywordNews, "EnableHighlighting", CultureNameNews, "Off", null, null, null, NewsCategory, "Date");

        marketData = marketData.AddQueryOption("$top", 30);
        var newsResults = marketData.Execute();

        foreach (var item in marketData)
        {
          Row = TableMultimediaItem.NewRow();

          Row["Id"] = item.ID;
          Row["IsEmbeddable"] = true;

          string Name;

          //Name=item.Description;
          Name = item.Title;
          //Name = await GetTitle(item.Url);

          if (!Name.Contains("|"))
          {
            Row["Name"] = Name;
          }
          else
          {
            Row["Name"] = Name.Replace("|", ".");
          }

          Row["Source"] = item.Url;
          Row["Link"] = item.Url;
          Row["Title"] = item.Title;
          Row["Artist"] = item.Source;
          Row["Album"] = item.Source;
          Row["AlbumArtist"] = item.Description;

          TableMultimediaItem.Rows.Add(Row);
        }
        #endregion Get News

        if (LanguageCultureNameNews != LanguageNews.ToLower())
        {
          #region Translate Title News

          List<string> ListTitleNews = new List<string>();
          foreach (DataRow itemRow in TableMultimediaItem.Rows)
          {
            ListTitleNews.Add(itemRow["Name"].ToString());
          }

          TranslatorService.TranslateArrayResponse[] TranslateResponse;

          using (OperationContextScope scope = new OperationContextScope(TranslatorClient.InnerChannel))
          {
            System.ServiceModel.OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

            TranslateResponse = TranslatorClient.TranslateArray("", ListTitleNews.ToArray(), LanguageCultureNameNews, LanguageNews, TranslateOptions);
          }

          int i = 0;
          foreach (DataRow itemRow in TableMultimediaItem.Rows)
          {
            itemRow["Name"] = TranslateResponse[i].TranslatedText;
            i++;
          }
          #endregion Translate Title News
        }
      }
      catch (Exception)
      {
      }

      return TableMultimediaItem;
    }

    #endregion Get List Multimedia Items

    private static async Task<DataTable> GetTableFromReader(SqlDataReader reader)
    {
      var returnValue = new DataTable();

      IDataRecord record; DataRow Row;

      var IsFirstRow = true;
      while (await reader.ReadAsync())
      {
        record = reader as IDataRecord;

        if (IsFirstRow)
        {
          IsFirstRow = false;
          for (int i = 0; i < record.FieldCount; i++)
          {
            returnValue.Columns.Add(new DataColumn(record.GetName(i), record.GetFieldType(i)));
          }
        }

        Row = returnValue.NewRow();
        for (int i = 0; i < record.FieldCount; i++)
        {
          Row[i] = record.GetValue(i);
        }

        returnValue.Rows.Add(Row);
      }

      return returnValue;
    }

    private static async Task<string> GetTitle(string urlNews)
    {
      string title = null;

      try
      {
        string AlchemyAPIKey = "1b3711f22d37af04c8c436c168f118365d3716d2";
        string AlchemyAPI = "http://access.alchemyapi.com/calls/url/URLGetTitle?";

        string content = string.Format("url={0}&apikey={1}&outputMode={2}", HttpUtility.UrlEncode(urlNews), AlchemyAPIKey, "json");

        HttpWebRequest request = WebRequest.Create(AlchemyAPI) as HttpWebRequest;
        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";

        using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
        {
          await writer.WriteAsync(content);
        }

        var response = request.GetResponse() as HttpWebResponse;

        if (response != null)
        {
          string stoken;
          using (var stream = response.GetResponseStream())
          {
            var encode = Encoding.GetEncoding("utf-8");
            using (var readStream = new StreamReader(stream, encode))
            {
              stoken = await readStream.ReadToEndAsync();
            }
          }

          var tokenData = new Dictionary<string, string>();
          tokenData = deserializeJson(stoken);

          if (tokenData.ContainsKey("title"))
          {
            title = tokenData["title"];
          }
        }
      }
      catch (Exception)
      {
        title = null;
      }

      return title;
    }

    private static Dictionary<string, string> deserializeJson(string json)
    {
      var jss = new JavaScriptSerializer();
      var d = jss.Deserialize<Dictionary<string, string>>(json);
      return d;
    }

    private static DataTable CreateTableMultimediaFileNames()
    {
      var TableMultimediaItem = new DataTable();
      TableMultimediaItem.TableName = "TableMultimediaItem";

      // Client
      TableMultimediaItem.Columns.Add(new DataColumn("FullPath", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("DisplayName", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("MultimediaFileGUID", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("TypeMultimedia", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("isSelectMultimediaFile", typeof(bool)));
      TableMultimediaItem.Columns.Add(new DataColumn("Subject", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Category", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Keywords", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Comments", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Author", typeof(string)));

      //OAuth
      TableMultimediaItem.Columns.Add(new DataColumn("Id", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Name", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("IdFromObject", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("IsEmbeddable", typeof(bool)));
      TableMultimediaItem.Columns.Add(new DataColumn("Source", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Link", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Title", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Artist", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("Album", typeof(string)));
      TableMultimediaItem.Columns.Add(new DataColumn("AlbumArtist", typeof(string)));

      return TableMultimediaItem;
    }

    #endregion Private functions
  }
}
