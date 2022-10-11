#region using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ServiceModel;
using System.Threading;
using System.Windows.Threading;
using System.IO;
using System.Data;
using System.Diagnostics;
using System.Net;
using LiveMultimediaTest.LiveMultimediaService;
#endregion using

namespace LiveMultimediaTest
{
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  /// 
  public partial class MainWindow : Window
  {
    #region Define variables
    string MultimediaFileGUID;

    BasicHttpBinding binding; EndpointAddress endpoint;
    ServicePoint servicepointLiveMultimediaService;

    private CancellationTokenSource tokenRequest = null;
    private int CountRunningClients=0;

    Thickness labelBorder = new Thickness(1, 1, 1, 1);
    SolidColorBrush labelBrushStart = new SolidColorBrush(Colors.White);
    SolidColorBrush labelBrush = new SolidColorBrush(Colors.SkyBlue);
    SolidColorBrush labelBrushOK = new SolidColorBrush(Colors.Green);
    SolidColorBrush labelBrushBAD = new SolidColorBrush(Colors.Red);
    SolidColorBrush labelBrushCancel = new SolidColorBrush(Colors.Blue);
    SolidColorBrush labelBrushTimeout = new SolidColorBrush(Colors.Yellow);

    int labelHeight = 26;
    double ProgressBarWidth=0;
    #endregion Define variables

    public MainWindow()
    {
      InitializeComponent();
      InitComponent();
    }

    private void InitEndPoint()
    {
      try
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

#if (!DEBUG)
        endpoint = new EndpointAddress("http://service.live-mm.com/LiveMultimediaService.svc");
#elif (!TEST)
        endpoint = new EndpointAddress("http://127.0.0.1:8080/LiveMultimediaService.svc");
#else
        endpoint = new EndpointAddress("http://service.live-mm.com/LiveMultimediaService.svc");
#endif
        servicepointLiveMultimediaService = ServicePointManager.FindServicePoint(endpoint.Uri);        
      }
      catch (Exception ex)
      {
        ErrorException(ex, "InitEndPoint");
      }
    }

    private void InitComponent()
    {
      InitEndPoint();

#if (!DEBUG)
      this.tbUsername.Clear();
      this.pbPassword.Clear();
      this.tbCountClients.Text = "1";
      this.tbMultimediaFileGUID.Text="";
#elif (!TEST)
      this.tbUsername.Text = "***";
      this.pbPassword.Password = "***";
      this.tbCountClients.Text = "2";
      this.tbMultimediaFileGUID.Text = "";
#else
      this.tbUsername.Text = "demo@live-mm.com";
      this.pbPassword.Password = "12345678qQ";
      this.tbCountClients.Text = "20";
      this.tbMultimediaFileGUID.Text = "";
#endif

      this.tbService.Text = endpoint.Uri.ToString();
      this.btStart.IsEnabled = false;
      this.btStop.IsEnabled = false;

      //servicepointLiveMultimediaService.ConnectionLimit = ServicePointManager.DefaultPersistentConnectionLimit;
      servicepointLiveMultimediaService.ConnectionLimit = 100000;
      this.tbMaxConnection.Text = servicepointLiveMultimediaService.ConnectionLimit.ToString();
    }

    private async void btStart_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(tbMultimediaFileGUID.Text) || string.IsNullOrWhiteSpace(tbMultimediaFileGUID.Text))
      {
        ErrorMessage("GUID is empty", "Start Test");
        return;
      }

      if (tbMultimediaFileGUID.Text != MultimediaFileGUID)
      {
        ErrorMessage("Username or Password was changed. Press 'Get List' and choice Multimedia", "Start Test");
        return;
      }

      this.tbFullSpeed.Text = "0";

      int CountClient = Convert.ToInt32(this.tbCountClients.Text);
      for (int i = 0; i < CountClient; i++) CreateScreenItems(i);

      if (tokenRequest == null) 
        tokenRequest = new CancellationTokenSource();
      else if (tokenRequest.IsCancellationRequested) tokenRequest = new CancellationTokenSource();
      
      //Рабочий
      await Task.Run(() =>
      {
        Parallel.For(0, CountClient, async i =>
        {
          await StartClientAsync2(i, tokenRequest.Token);
        }
        );
      }
      );

      this.btStop.IsEnabled = true;
      CountRunningClients = CountRunningClients + CountClient;

      ////Рабочий без UI
      //await Task.Run(() =>
      //{
      //  Parallel.For(0, CountClient, async i =>
      //  {
      //    await StartClientAsync4(i);
      //  }
      //  );
      //}
      //);

      //await CreateClientAsync(CountClient);
    }

    private void StopTest()
    {
      if (tokenRequest != null)
        if (!tokenRequest.IsCancellationRequested) tokenRequest.Cancel();
    }

    private void btStop_Click(object sender, RoutedEventArgs e)
    {      
      this.btStop.IsEnabled = false;
      StopTest();
    }

    private void CreateScreenItems(int CountClient)
    {
      int CountCurrentClient=CountRunningClients+CountClient + 1;

      Label lblRunClient = new Label();
      lblRunClient.Content = "Client " + CountCurrentClient.ToString();
      lblRunClient.HorizontalAlignment = HorizontalAlignment.Left; lblRunClient.VerticalAlignment = VerticalAlignment.Top;
      lblRunClient.Width = 72; lblRunClient.Height = labelHeight;
      lblRunClient.BorderThickness = labelBorder; lblRunClient.BorderBrush = labelBrush;

      Thickness marginLabel = lblRunClient.Margin;
      //marginLabel.Top = this.lblListMultimedia.Margin.Top + this.lblListMultimedia.ActualHeight + 5 + (double)(CountCurrentClient-1)*25;
      marginLabel.Top = this.lblMaxConnection.Margin.Top + this.lblMaxConnection.ActualHeight + 5 + (double)(CountCurrentClient - 1) * 25;
      marginLabel.Left = 5;
      lblRunClient.Margin = marginLabel;
      
      ////////////////////////////////////////////////////////////////////////////////////////////////
      ProgressBar pbClient = new ProgressBar();
      pbClient.Name = "pbClient" + CountCurrentClient.ToString();

      if (ProgressBarWidth == 0) ProgressBarWidth = this.gridRun.ActualWidth;

      Thickness marginProgressBar = pbClient.Margin;
      marginProgressBar.Left = lblRunClient.Margin.Left + lblRunClient.Width + 5;
      marginProgressBar.Top = lblRunClient.Margin.Top + 8;
      pbClient.Width = ProgressBarWidth - 435; pbClient.Height = 10;
      pbClient.Margin = marginProgressBar;
      pbClient.HorizontalAlignment = HorizontalAlignment.Left;
      pbClient.VerticalAlignment = VerticalAlignment.Top;
      ////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblTime = new Label();
      lblTime.Name = "lblTime" + CountCurrentClient.ToString();
      lblTime.Content = "0";
      lblTime.HorizontalAlignment = HorizontalAlignment.Left; lblTime.VerticalAlignment = VerticalAlignment.Top;
      lblTime.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblTime.Width = 47; lblTime.Height = labelHeight;
      lblTime.BorderThickness = labelBorder; lblTime.BorderBrush = labelBrush;

      Thickness marginTime = lblTime.Margin;
      marginTime.Top = lblRunClient.Margin.Top;
      marginTime.Left = pbClient.Margin.Left + pbClient.Width + 5;
      lblTime.Margin = marginTime;
      ////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblTimeAll = new Label();
      lblTimeAll.Name = "lblTimeAll" + CountCurrentClient.ToString();
      lblTimeAll.Content = "0";
      lblTimeAll.HorizontalAlignment = HorizontalAlignment.Left; lblTimeAll.VerticalAlignment = VerticalAlignment.Top;
      lblTimeAll.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblTimeAll.Width = 55; lblTimeAll.Height = labelHeight;
      lblTimeAll.BorderThickness = labelBorder; lblTimeAll.BorderBrush = labelBrush;

      Thickness marginTimeAll = lblTimeAll.Margin;
      marginTimeAll.Top = lblRunClient.Margin.Top;
      marginTimeAll.Left = lblTime.Margin.Left + lblTime.Width + 5;
      lblTimeAll.Margin = marginTimeAll;
      ////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblChunk = new Label();
      lblChunk.Name = "lblChunk" + CountCurrentClient.ToString();
      lblChunk.Content = "0";
      lblChunk.HorizontalAlignment = HorizontalAlignment.Left; lblChunk.VerticalAlignment = VerticalAlignment.Top;
      lblChunk.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblChunk.Width = 36; lblChunk.Height = labelHeight;
      lblChunk.BorderThickness = labelBorder; lblChunk.BorderBrush = labelBrush;

      Thickness marginChunk = lblChunk.Margin;
      marginChunk.Top = lblRunClient.Margin.Top;
      marginChunk.Left = lblTimeAll.Margin.Left + lblTimeAll.Width + 5;
      lblChunk.Margin = marginChunk;
      ////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblDelay = new Label();
      lblDelay.Name = "lblDelay" + CountCurrentClient.ToString();
      lblDelay.Content = "0";
      lblDelay.HorizontalAlignment = HorizontalAlignment.Left; lblDelay.VerticalAlignment = VerticalAlignment.Top;
      lblDelay.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblDelay.Width = 46; lblDelay.Height = labelHeight;
      lblDelay.BorderThickness = labelBorder; lblDelay.BorderBrush = labelBrush;

      Thickness marginDelay = lblDelay.Margin;
      marginDelay.Top = lblRunClient.Margin.Top;
      marginDelay.Left = lblChunk.Margin.Left + lblChunk.Width + 5;
      lblDelay.Margin = marginDelay;
      ////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblSpeed = new Label();
      lblSpeed.Name = "lblSpeed" + CountCurrentClient.ToString();
      lblSpeed.Content = "0";
      lblSpeed.HorizontalAlignment = HorizontalAlignment.Left; lblSpeed.VerticalAlignment = VerticalAlignment.Top;
      lblSpeed.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblSpeed.Width = 67; lblSpeed.Height = labelHeight;
      lblSpeed.BorderThickness = labelBorder; lblSpeed.BorderBrush = labelBrush;

      Thickness marginSpeed = lblSpeed.Margin;
      marginSpeed.Top = lblRunClient.Margin.Top;
      marginSpeed.Left = lblDelay.Margin.Left + lblDelay.Width + 5;
      lblSpeed.Margin = marginSpeed;
      ////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblSize = new Label();
      lblSize.Name = "lblSize" + CountCurrentClient.ToString();
      lblSize.Content = "0";
      lblSize.HorizontalAlignment = HorizontalAlignment.Left; lblSize.VerticalAlignment = VerticalAlignment.Top;
      lblSize.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblSize.Width = 67; lblSize.Height = labelHeight;
      lblSize.BorderThickness = labelBorder; lblSize.BorderBrush = labelBrush;

      Thickness marginSize = lblSize.Margin;
      marginSize.Top = lblSpeed.Margin.Top;
      marginSize.Left = lblSpeed.Margin.Left + lblSpeed.Width + 5;
      lblSize.Margin = marginSize;
      ////////////////////////////////////////////////////////////////////////////////////////////////

      this.gridRun.Children.Add(lblRunClient);
      this.gridRun.Children.Add(pbClient);
      this.gridRun.Children.Add(lblTime);
      this.gridRun.Children.Add(lblTimeAll);
      this.gridRun.Children.Add(lblChunk);
      this.gridRun.Children.Add(lblDelay);
      this.gridRun.Children.Add(lblSpeed);
      this.gridRun.Children.Add(lblSize);
    }

    Object lockobject = new object();

    private async Task StartClientAsync2(int CountClient, CancellationToken ct)
    {
      #region Define vars

      string Username = ""; string Password = ""; string UserTokenServer;

      byte[] MultimediaFileBuffer = null; int MultimediaFileChunkCount; long MultimediaFileLength = 0;
      string KeyGuid;
      int Speed; int Size = 0; int AverageSpeed = 0; long AverageDelay = 0;
      long MaxDelay = 0;
      
      int FlagResponse = -1;
      int CountRetry; int MaxCountRetry = 1;

      const int CountControlConst = 25;
      const int CountControlClient = 8;

      int FirstCountClient = CountControlConst + (CountRunningClients + CountClient) * CountControlClient;

      MultimediaFileChunkCount = 0;
      bool IsStopTransfer = false; bool IsFirstFlush = true;
      byte[] FullMultimediaFileBuffer;
      byte[] aMultimediaFileLength = new byte[8]; byte[] aIsStopTransfer = new byte[1]; byte[] aFlagResponse = new byte[4];

      #endregion Define vars

      #region Get Controls
      Label lblRunClient=null;  ProgressBar pb = null;
      Label lblTime = null; Label lblTimeAll = null; Label lblChunk = null; Label lblDelay = null; Label lblSpeed = null; Label lblSize = null;

      await this.gridRun.Dispatcher.InvokeAsync((Action)(() =>
      {
        lblRunClient = (Label)this.gridRun.Children[FirstCountClient];
        pb = (ProgressBar)this.gridRun.Children[FirstCountClient + 1];
        lblTime = (Label)this.gridRun.Children[FirstCountClient + 2];
        lblTimeAll = (Label)this.gridRun.Children[FirstCountClient + 3];
        lblChunk = (Label)this.gridRun.Children[FirstCountClient + 4];
        lblDelay = (Label)this.gridRun.Children[FirstCountClient + 5];
        lblSpeed = (Label)this.gridRun.Children[FirstCountClient + 6];
        lblSize = (Label)this.gridRun.Children[FirstCountClient + 7];

        pb.Minimum = 1; pb.Value = 1;

        Username = this.tbUsername.Text;
        Password = this.pbPassword.Password;
      }
      ));
      #endregion Get Controls

      if (ct.IsCancellationRequested) { await lblRunClient.Dispatcher.InvokeAsync((Action)(() => lblRunClient.Background = labelBrushCancel)); return; }

      #region Login
      await this.tbUsername.Dispatcher.InvokeAsync((Action)(() => Username = this.tbUsername.Text));
      await this.pbPassword.Dispatcher.InvokeAsync((Action)(() => Password = this.pbPassword.Password));
      UserTokenServer = await RemoteLoginAsync(Username, Password);
      if (UserTokenServer==null) {await lblRunClient.Dispatcher.InvokeAsync((Action)(() => lblRunClient.Background = labelBrushBAD)); return; }
      #endregion Login

      #region Logout for Cancellation
      if (ct.IsCancellationRequested)
      {
        await lblRunClient.Dispatcher.InvokeAsync((Action)(() => lblRunClient.Background = labelBrushCancel));
        await RemoteLogoutAsync(UserTokenServer);
        return;
      }
      #endregion Logout for Cancellation

      var Delta = new Stopwatch(); var CountTime = new Stopwatch(); bool IsReadyClient; bool IsFirstRequest; bool IsClientTimeout=false;

      #region Read Multimedia File
      while (!IsStopTransfer && !ct.IsCancellationRequested)
      {
        #region Prepare Var
        MultimediaFileChunkCount++;
        
        MultimediaFileLength = 0; MultimediaFileBuffer = null;

        KeyGuid = System.Guid.NewGuid().ToString();

        IsReadyClient = false; IsFirstRequest = true; IsClientTimeout = false;
        FullMultimediaFileBuffer = null; CountRetry = 1;
        await lblDelay.Dispatcher.InvokeAsync((Action)(() =>           
        {
          lblDelay.Background = labelBrushStart;
          lblDelay.Content = CountRetry.ToString();          
        }          
        ));
        #endregion Prepare Var

        Delta.Restart();
        CountTime.Restart();

        #region Read one chunk buffer from server
        while (!IsReadyClient && !IsClientTimeout && !ct.IsCancellationRequested)
        {
          try
          {            
            using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
            {
              FullMultimediaFileBuffer = await LiveMultimediaSystem.RemoteGetMultimediaFileBufferAsync(UserTokenServer, KeyGuid, MultimediaFileGUID, MultimediaFileChunkCount, IsFirstRequest);
            }

            if (IsFirstRequest)
            {
              IsFirstRequest = false;
              await Task.Delay(1000);
            }
            else if (FullMultimediaFileBuffer != null)
              IsReadyClient = true;
            else if (CountTime.Elapsed.TotalSeconds > 60)
            {
              FullMultimediaFileBuffer = null;
              IsClientTimeout = true;
            }
            else
              await Task.Delay(50);

            if (!IsReadyClient)
            {
              CountRetry++;
              await this.gridRun.Dispatcher.InvokeAsync((Action)(() => lblDelay.Content = CountRetry.ToString() ));              
            }
          }
          catch (Exception ex)
          {
            if (CountTime.IsRunning) CountTime.Stop();
            FullMultimediaFileBuffer = null;
            MessageBox.Show(ex.ToString(), "StartClientAsync2");
          }
        }
        #endregion Read one chunk buffer from server

        if (Delta.IsRunning) Delta.Stop();

        if (MaxCountRetry < CountRetry) MaxCountRetry = CountRetry;

        if (IsClientTimeout) await this.gridRun.Dispatcher.InvokeAsync((Action)(() => lblDelay.Background = labelBrushBAD));

        #region Check FullMultimediaFileBuffer
        if (FullMultimediaFileBuffer != null)
        {
          using (MemoryStream ms = new MemoryStream(FullMultimediaFileBuffer))
          {
            await ms.ReadAsync(aFlagResponse, 0, 4);
            FlagResponse = BitConverter.ToInt32(aFlagResponse, 0);

            if (FlagResponse == 0)
            {
              await ms.ReadAsync(aIsStopTransfer, 0, 1);
              IsStopTransfer = BitConverter.ToBoolean(aIsStopTransfer, 0);

              await ms.ReadAsync(aMultimediaFileLength, 0, 8);
              MultimediaFileLength = BitConverter.ToInt64(aMultimediaFileLength, 0);

              MultimediaFileBuffer = new byte[ms.Capacity - (4 + 1 + 8)];
              await ms.ReadAsync(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length);
            }
            else
            {
              MultimediaFileBuffer = null;
            }
          }
        }
        else
        {
          IsStopTransfer = true;
          MultimediaFileBuffer = null;
        }
        #endregion Check FullMultimediaFileBuffer

        #region Check MultimediaFileBuffer
        if (MultimediaFileBuffer != null && MultimediaFileLength > 0)
        {
          Speed = (int)((8 * FullMultimediaFileBuffer.Length / Delta.Elapsed.TotalSeconds) / 1024);
          Size = Size + MultimediaFileBuffer.Length;
          AverageDelay = AverageDelay + Delta.ElapsedMilliseconds;
          AverageSpeed = AverageSpeed + Speed;

          if (MaxDelay < Delta.ElapsedMilliseconds) MaxDelay = Delta.ElapsedMilliseconds;

          await this.gridRun.Dispatcher.InvokeAsync((Action)(() =>
          {
            lblTime.Content = Delta.ElapsedMilliseconds.ToString();
            lblTimeAll.Content = (Convert.ToDouble(lblTimeAll.Content) + Delta.Elapsed.Seconds).ToString();
            lblChunk.Content = MultimediaFileChunkCount.ToString();
            lblSpeed.Content = Speed.ToString();
            lblSize.Content = Size.ToString();
          }
          ));

          if (IsFirstFlush)
          {
            IsFirstFlush = false;
            await pb.Dispatcher.InvokeAsync((Action)(() => pb.Maximum = (double)(MultimediaFileLength / MultimediaFileBuffer.Length)));
          }
          await pb.Dispatcher.InvokeAsync((Action)(() => pb.Value++));
        }
        #endregion Check MultimediaFileBuffer
      }
      #endregion Read Multimedia File        

      #region Logout
      await RemoteLogoutAsync(UserTokenServer);
      #endregion Logout

      if (ct.IsCancellationRequested) { await lblRunClient.Dispatcher.InvokeAsync((Action)(() => lblRunClient.Background = labelBrushCancel)); }

      #region Calculation average and total

      AverageDelay = (AverageDelay / MultimediaFileChunkCount);
      AverageSpeed = (AverageSpeed / MultimediaFileChunkCount);      

      await this.gridRun.Dispatcher.InvokeAsync((Action)(() =>
      {
        lblTime.Content = MaxDelay.ToString();
        lblSpeed.Content = AverageSpeed.ToString();

        if (Size == MultimediaFileLength && Size > 0)
          lblSize.Background = labelBrushOK;
        else
          lblSize.Background = labelBrushBAD;

        lblDelay.Content = MaxCountRetry.ToString();
        if (IsClientTimeout) lblDelay.Background = labelBrushTimeout;
      }
      ));

      await this.gridRun.Dispatcher.InvokeAsync((Action)(() =>
      {
        lock (lockobject)
        {
          string FullSpeed = (Convert.ToInt32(this.tbFullSpeed.Text) + AverageSpeed).ToString();
          this.tbFullSpeed.Text = FullSpeed;
        }
      }
      ));

      #endregion Calculation average and total
    }

    private async Task<bool> LoadMultimediaFromServerAsync(string UserToken)
    {
      bool IsSuccess;
      try
      {
        using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
        {
          LiveMultimediaSystem.Open();
          this.cbListMultimedia.ItemsSource = await LiveMultimediaSystem.RemoteGetListMultimediaFilesAsync(UserToken);
        }

        if (this.cbListMultimedia.Items.Count > 0) this.cbListMultimedia.SelectedIndex = 0;
        IsSuccess = true;
      }
      catch (Exception ex)
      {
        IsSuccess = false;
        ErrorException(ex, "LoadMultimediaFromServerAsync");
      }

      return IsSuccess;
    }

    private async void btGetListMultimedia_Click(object sender, RoutedEventArgs e)
    {
      string UserTokenServer = await RemoteLoginAsync(this.tbUsername.Text, this.pbPassword.Password);
      if (UserTokenServer==null) return;

      bool IsSuccess = await LoadMultimediaFromServerAsync(UserTokenServer);
      if (!IsSuccess)
      {
        this.tbMultimediaFileGUID.Text = "";
        ErrorMessage("Load List Multimedia", "GetListMultimedia");
      }

      await RemoteLogoutAsync(UserTokenServer);
      this.btStart.IsEnabled = true;
    }

    private async Task<string> RemoteLoginAsync(string Username, string Password)
    {
      string UserTokenServer;

      try
      {
        using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
        {
          LiveMultimediaSystem.Open();
          UserTokenServer = await LiveMultimediaSystem.RemoteLoginAsync(Username, Password);
          if (!CheckGoodString(UserTokenServer)) throw new ArgumentException("UserTokenServer.Empty", "UserTokenServer is empty");
          if (!CheckFormatGUID(UserTokenServer)) throw new ArgumentException("UserTokenServer.Format", "UserTokenServer is not GUID");
        }
      }
      catch (Exception ex)
      {
        UserTokenServer = null;
        ErrorException(ex, "LoginLiveMultimediaSystem");
      }

      return UserTokenServer;
    }

    private async Task<bool> RemoteLogoutAsync(string UserTokenServer)
    {
      bool IsSuccess;
      try
      {
        using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
        {
          LiveMultimediaSystem.Open();
          IsSuccess=await LiveMultimediaSystem.RemoteLogoutAsync(UserTokenServer);
          if (!IsSuccess) throw new ArgumentException("Logout", "Bad Logout");
        }
      }
      catch (Exception ex)
      {
        IsSuccess = false;
        ErrorException(ex, "LogoutLiveMultimediaSystemAsync");
      }

      return IsSuccess;
    }

    private void cbListMultimedia_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      System.Collections.IList ListMultimediaSelected = e.AddedItems;
      if (ListMultimediaSelected.Count>0)
      {
        MultimediaFile SelectedMultimediaFile = (MultimediaFile)ListMultimediaSelected[0];
        this.tbMultimediaFileGUID.Text = SelectedMultimediaFile.MultimediaFileGUID;
        MultimediaFileGUID = this.tbMultimediaFileGUID.Text;
      }
    }

    private void tbUsername_TextChanged(object sender, TextChangedEventArgs e)
    {
      MultimediaFileGUID = "";

      if (this.btStart!=null) this.btStart.IsEnabled = false;
      if (this.btStop!=null) this.btStop.IsEnabled = false;
      if (this.btGetListMultimedia!=null) this.btGetListMultimedia.IsEnabled = true;
    }

    private void pbPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
      MultimediaFileGUID = "";

      if (this.btStart != null) this.btStart.IsEnabled = false;
      if (this.btStop != null) this.btStop.IsEnabled = false;
      if (this.btGetListMultimedia != null) this.btGetListMultimedia.IsEnabled = true;
    }

    private void pbPassword_GotFocus(object sender, RoutedEventArgs e)
    {
      pbPassword.SelectAll();
    }
    
    private void ErrorException(Exception ex, string NameFunction)
    {
      System.Windows.MessageBox.Show(DateTime.Now.ToString() + " Error in " + NameFunction + ": " + ex.ToString(), "Live Multimedia System Test", MessageBoxButton.OK);
    }

    private void ErrorMessage(string Message, string NameFunction)
    {
      System.Windows.MessageBox.Show(DateTime.Now.ToString() + " Error " + NameFunction + ": " + Message, "Live Multimedia System Test", MessageBoxButton.OK);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      StopTest();
    }

    private void tbMaxConnection_TextChanged(object sender, TextChangedEventArgs e)
    {
      int MaxConnection;

      if (servicepointLiveMultimediaService != null)
      {
        bool IsSuccess = Int32.TryParse(this.tbMaxConnection.Text, out MaxConnection);
        if (IsSuccess)
          servicepointLiveMultimediaService.ConnectionLimit = MaxConnection;
        else
        {
          MessageBox.Show("Incorrect number format. Please, use digitals");
          this.tbMaxConnection.Text = servicepointLiveMultimediaService.ConnectionLimit.ToString();
        }
      }
    }

    private bool CheckGoodString(string stringCheck)
    {
      bool IsSuccess = (!string.IsNullOrEmpty(stringCheck) && !string.IsNullOrWhiteSpace(stringCheck));
      return IsSuccess;
    }

    private bool CheckFormatGUID(string sGUID)
    {
      Guid ResultGUID;
      bool IsSuccess = Guid.TryParseExact(sGUID, "D", out ResultGUID);
      return IsSuccess;
    }

  }
}

/*
     //private void CreateScreenHeaders()
    //{
    //  Label lblRunClient = new Label();
    //  lblRunClient.Name = "Client";
    //  lblRunClient.Content = "Client";
    //  lblRunClient.HorizontalAlignment = HorizontalAlignment.Left; lblRunClient.VerticalAlignment = VerticalAlignment.Top;
    //  lblRunClient.Width = 67; lblRunClient.Height = labelHeight;
    //  lblRunClient.BorderThickness = labelBorder; lblRunClient.BorderBrush = labelBrush;

    //  Thickness marginLabel = lblRunClient.Margin;
    //  marginLabel.Top = this.tbCountClients.Margin.Top + this.tbCountClients.ActualHeight + 5;
    //  marginLabel.Left = 5;
    //  lblRunClient.Margin = marginLabel;
    //  ////////////////////////////////////////////////////////////////////////////////////////////////
    //  Label lblProgressBar = new Label();
    //  lblProgressBar.Name = "Client";
    //  lblProgressBar.Content = "Client";
    //  lblProgressBar.HorizontalAlignment = HorizontalAlignment.Left; lblRunClient.VerticalAlignment = VerticalAlignment.Top;
    //  lblProgressBar.Width = 67; lblRunClient.Height = labelHeight;
    //  lblProgressBar.BorderThickness = labelBorder; lblRunClient.BorderBrush = labelBrush;

    //  Thickness marginProgressBar = lblRunClient.Margin;
    //  marginLabel.Top = this.tbCountClients.Margin.Top + this.tbCountClients.ActualHeight + 5;
    //  marginLabel.Left = 5;
    //  lblRunClient.Margin = marginLabel;


    //  ProgressBar pbClient = new ProgressBar();
    //  pbClient.Name = "pbClient" + (CountClient + 1).ToString();

    //  if (ProgressBarWidght == 0) ProgressBarWidght = this.gridRun.ActualWidth;

    //  Thickness marginProgressBar = pbClient.Margin;
    //  marginProgressBar.Left = lblRunClient.Margin.Left + lblRunClient.Width + 5;
    //  marginProgressBar.Top = lblRunClient.Margin.Top + 8;
    //  pbClient.Width = ProgressBarWidght - (290 + 67 + 5); pbClient.Height = 10;
    //  pbClient.Margin = marginProgressBar;
    //  pbClient.HorizontalAlignment = HorizontalAlignment.Left;
    //  pbClient.VerticalAlignment = VerticalAlignment.Top;
    //  ////////////////////////////////////////////////////////////////////////////////////////////////
    //  Label lblTime = new Label();
    //  lblTime.Name = "lblTime" + (CountClient + 1).ToString();
    //  lblTime.Content = "0";
    //  lblTime.HorizontalAlignment = HorizontalAlignment.Left; lblTime.VerticalAlignment = VerticalAlignment.Top;
    //  lblTime.HorizontalContentAlignment = HorizontalAlignment.Right;
    //  lblTime.Width = 40; lblTime.Height = labelHeight;
    //  lblTime.BorderThickness = labelBorder; lblTime.BorderBrush = labelBrush;

    //  Thickness marginTime = lblTime.Margin;
    //  marginTime.Top = lblRunClient.Margin.Top;
    //  marginTime.Left = pbClient.Margin.Left + pbClient.Width + 5;
    //  lblTime.Margin = marginTime;
    //  ////////////////////////////////////////////////////////////////////////////////////////////////
    //  Label lblChunk = new Label();
    //  lblChunk.Name = "lblChunk" + (CountClient + 1).ToString();
    //  lblChunk.Content = "0";
    //  lblChunk.HorizontalAlignment = HorizontalAlignment.Left; lblChunk.VerticalAlignment = VerticalAlignment.Top;
    //  lblChunk.HorizontalContentAlignment = HorizontalAlignment.Right;
    //  lblChunk.Width = 36; lblChunk.Height = labelHeight;
    //  lblChunk.BorderThickness = labelBorder; lblChunk.BorderBrush = labelBrush;

    //  Thickness marginChunk = lblChunk.Margin;
    //  marginChunk.Top = lblRunClient.Margin.Top;
    //  marginChunk.Left = lblTime.Margin.Left + lblTime.Width + 5;
    //  lblChunk.Margin = marginChunk;
    //  ////////////////////////////////////////////////////////////////////////////////////////////////
    //  Label lblDelay = new Label();
    //  lblDelay.Name = "lblDelay" + (CountClient + 1).ToString();
    //  lblDelay.Content = "0";
    //  lblDelay.HorizontalAlignment = HorizontalAlignment.Left; lblDelay.VerticalAlignment = VerticalAlignment.Top;
    //  lblDelay.HorizontalContentAlignment = HorizontalAlignment.Right;
    //  lblDelay.Width = 46; lblDelay.Height = labelHeight;
    //  lblDelay.BorderThickness = labelBorder; lblDelay.BorderBrush = labelBrush;

    //  Thickness marginDelay = lblDelay.Margin;
    //  marginDelay.Top = lblRunClient.Margin.Top;
    //  marginDelay.Left = lblChunk.Margin.Left + lblChunk.Width + 5;
    //  lblDelay.Margin = marginDelay;
    //  ////////////////////////////////////////////////////////////////////////////////////////////////
    //  Label lblSpeed = new Label();
    //  lblSpeed.Name = "lblSpeed" + (CountClient + 1).ToString();
    //  lblSpeed.Content = "0 Кб/с";
    //  lblSpeed.HorizontalAlignment = HorizontalAlignment.Left; lblSpeed.VerticalAlignment = VerticalAlignment.Top;
    //  lblSpeed.HorizontalContentAlignment = HorizontalAlignment.Right;
    //  lblSpeed.Width = 67; lblSpeed.Height = labelHeight;
    //  lblSpeed.BorderThickness = labelBorder; lblSpeed.BorderBrush = labelBrush;

    //  Thickness marginSpeed = lblSpeed.Margin;
    //  marginSpeed.Top = lblRunClient.Margin.Top;
    //  marginSpeed.Left = lblDelay.Margin.Left + lblDelay.Width + 5;
    //  lblSpeed.Margin = marginSpeed;
    //  ////////////////////////////////////////////////////////////////////////////////////////////////
    //  Label lblSize = new Label();
    //  lblSize.Name = "lblSize" + (CountClient + 1).ToString();
    //  lblSize.Content = "0 bytes";
    //  lblSize.HorizontalAlignment = HorizontalAlignment.Left; lblSize.VerticalAlignment = VerticalAlignment.Top;
    //  lblSize.HorizontalContentAlignment = HorizontalAlignment.Right;
    //  lblSize.Width = 67; lblSize.Height = labelHeight;
    //  lblSize.BorderThickness = labelBorder; lblSize.BorderBrush = labelBrush;

    //  Thickness marginSize = lblSize.Margin;
    //  marginSize.Top = lblSpeed.Margin.Top;
    //  marginSize.Left = lblSpeed.Margin.Left + lblSpeed.Width + 5;
    //  lblSize.Margin = marginSize;
    //  ////////////////////////////////////////////////////////////////////////////////////////////////

    //  this.gridRun.Children.Add(lblRunClient);
    //  this.gridRun.Children.Add(pbClient);
    //  this.gridRun.Children.Add(lblTime);
    //  this.gridRun.Children.Add(lblChunk);
    //  this.gridRun.Children.Add(lblDelay);
    //  this.gridRun.Children.Add(lblSpeed);
    //  this.gridRun.Children.Add(lblSize);
    //}
    private async Task CreateClientAsync(int CountClient)
    {
      await StartClientAsync4(0);

      //for (int i = 0; i < CountClient; i++)
      //{
      //  await Task.Run(() =>
      //  {
      //    //StartClient(i);
      //    StartClientAsync2(i);          
      //  }
      //  );
      //}

      //await Task.Run(() =>
      //{
      //  Parallel.For(0, CountClient, async i =>
      //  {
      //    await StartClientAsync(i);
      //  }
      //  );
      //}
      //);

      //tokenRequest = new CancellationTokenSource();
      ////await StartClientAsync(tokenRequest.Token, 1);
      ////await StartClientAsync(tokenRequest, 1);

      //tokenRequest = new CancellationTokenSource();
      ////await StartClientAsync(tokenRequest.Token, 2);
      ////await StartClientAsync(tokenRequest, 2);

      ////for (int i = 0; i < CountClient; i++)
      ////{
      ////  tokenRequest = new CancellationTokenSource();
      ////  await StartClientAsync(tokenRequest.Token, i + 1);
      ////}

      ////for (int i = 0; i < CountClient; i++)
      ////{
      ////  await Task.Factory.StartNew(async() =>
      ////  {
      ////    tokenRequest = new CancellationTokenSource();
      ////    await StartClientAsync(tokenRequest.Token, i + 1);
      ////  });
      ////}

      ////await Task.Factory.StartNew(() =>
      ////{
      ////  tokenRequest = new CancellationTokenSource();
      ////  Parallel.For(0, CountClient, async i  =>
      ////  {
      ////    tokenRequest = new CancellationTokenSource();
      ////    //await Task.Factory.StartNew(() => StartClientAsync(tokenRequest.Token, i + 1));
      ////    await StartClientAsync(tokenRequest.Token, i + 1);
      ////  });
      ////});
    } 
    private async Task StartClientAsync3()
    {
      string MultimediaFileGUID = "27108BF7-24EF-4005-9836-6E4F417504CD";
      //int MultimediaFileChunkCount;
      string KeyGuid = System.Guid.NewGuid().ToString();

      int CountClient = Convert.ToInt32(this.tbCountClients.Text);

      using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
      {
        LiveMultimediaSystem.Open();
        byte[] FullMultimediaFileBuffer;

        FullMultimediaFileBuffer = await LiveMultimediaSystem.RemoteGetMultimediaFileBufferAsync(UserToken, KeyGuid, MultimediaFileGUID, CountClient);

        ////MultimediaFileChunkCount = 0;        
        //await Task.Run(() =>
        //{
        //  Parallel.For(1, CountClient+1, async MultimediaFileChunkCount =>
        //  {
        //    FullMultimediaFileBuffer = await LiveMultimediaSystem.RemoteGetMultimediaFileBufferAsync(UserToken + "|" + KeyGuid, MultimediaFileGUID, MultimediaFileChunkCount);
        //  }
        //  );
        //}
        //);

        ////for (int i = 0; i < 20; i++)
        ////{
        ////  MultimediaFileChunkCount++;
        ////  await Task.Run(() =>
        ////  {
        ////    FullMultimediaFileBuffer = LiveMultimediaSystem.RemoteGetMultimediaFileBuffer(UserToken + "|" + KeyGuid, MultimediaFileGUID, MultimediaFileChunkCount);
        ////  }
        ////  );          
        ////}
      }
      //MessageBox.Show("Done");
    }

    private async Task StartClientAsync4(int CountClient)
    {
      string MultimediaFileGUID;

      if ((double)((CountClient) / 2) > 0)
        MultimediaFileGUID = "27108BF7-24EF-4005-9836-6E4F417504CD";
      else
        MultimediaFileGUID = "A56291CC-D35F-456A-B1B3-B0EC2F8D5A95";

      byte[] MultimediaFileBuffer = null; int MultimediaFileChunkCount; long MultimediaFileLength = 0;
      string KeyGuid = System.Guid.NewGuid().ToString();

      using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
      {
        LiveMultimediaSystem.Open();

        MultimediaFileChunkCount = 0;
        bool IsStopTransfer = false; bool IsFirstFlush = true;
        byte[] FullMultimediaFileBuffer;
        byte[] aMultimediaFileLength = new byte[8]; byte[] aIsStopTransfer = new byte[1];

        bool IsInitMultimediaFile = await LiveMultimediaSystem.RemoteInitMultimediaFileAsync(UserToken, KeyGuid, MultimediaFileGUID);

        if (!IsInitMultimediaFile) return;

        while (!IsStopTransfer)
        {
          MultimediaFileChunkCount++;

          MultimediaFileLength = 0; MultimediaFileBuffer = null;

          FullMultimediaFileBuffer = await LiveMultimediaSystem.RemoteGetMultimediaFileBufferAsync(UserToken, KeyGuid, MultimediaFileGUID, MultimediaFileChunkCount);
          
          if (FullMultimediaFileBuffer != null)
          {
            using (MemoryStream ms = new MemoryStream(FullMultimediaFileBuffer))
            {
              await ms.ReadAsync(aMultimediaFileLength, 0, 8);
              MultimediaFileLength = BitConverter.ToInt64(aMultimediaFileLength, 0);

              await ms.ReadAsync(aIsStopTransfer, 0, 1);
              IsStopTransfer = BitConverter.ToBoolean(aIsStopTransfer, 0);

              MultimediaFileBuffer = new byte[ms.Capacity - 9];
              await ms.ReadAsync(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length);
            }
          }
          else
          {
            IsStopTransfer = true;
            MultimediaFileBuffer = null;
          }

          if (MultimediaFileLength > 0 && MultimediaFileBuffer != null)
          {
            if (IsFirstFlush)
            {
              IsFirstFlush = false;
            }
          }
          else
          {
            IsStopTransfer = true;
          }
        }
      }
    }

    private async Task StartClientAsync(int CountClient)
    {
      //string MultimediaFileGUID = "27108BF7-24EF-4005-9836-6E4F417504CD";
      string MultimediaFileGUID;

      if ((double)((CountClient + 1) / 2) > 0)
        MultimediaFileGUID = "27108BF7-24EF-4005-9836-6E4F417504CD";
      else
        MultimediaFileGUID = "A56291CC-D35F-456A-B1B3-B0EC2F8D5A95";

      byte[] MultimediaFileBuffer = null; int MultimediaFileChunkCount; long MultimediaFileLength = 0;
      string KeyGuid = System.Guid.NewGuid().ToString();
      DateTime StartTime; DateTime StopTime; TimeSpan Delta;
      int Speed; int Size = 0; int AverageSpeed = 0; int AverageDelay = 0;

      const int CountControlConst = 10;
      const int CountControlClient = 7;

      ProgressBar pb = null; Label lblTime = null; Label lblChunk = null; Label lblDelay = null; Label lblSpeed = null; Label lblSize = null;

      pb = (ProgressBar)this.gridRun.Children[(CountClient) * CountControlClient + 1 + CountControlConst];
      pb.Minimum = 1; pb.Value = 1;

      lblTime = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 2 + CountControlConst];
      lblChunk = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 3 + CountControlConst];
      lblDelay = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 4 + CountControlConst];
      lblSpeed = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 5 + CountControlConst];
      lblSize = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 6 + CountControlConst];

      using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
      {
        LiveMultimediaSystem.Open();

        MultimediaFileChunkCount = 0;
        bool IsStopTransfer = false; bool IsFirstFlush = true;
        byte[] FullMultimediaFileBuffer;
        byte[] aMultimediaFileLength = new byte[8]; byte[] aIsStopTransfer = new byte[1];

        while (!IsStopTransfer)
        {
          MultimediaFileChunkCount++;

          MultimediaFileLength = 0; MultimediaFileBuffer = null;

          StartTime = DateTime.Now;

          FullMultimediaFileBuffer = await LiveMultimediaSystem.RemoteGetMultimediaFileBufferAsync(UserToken, KeyGuid, MultimediaFileGUID, MultimediaFileChunkCount);

          StopTime = DateTime.Now; Delta = StopTime.Subtract(StartTime);

          if (FullMultimediaFileBuffer != null)
          {
            using (MemoryStream ms = new MemoryStream(FullMultimediaFileBuffer))
            {
              await ms.ReadAsync(aMultimediaFileLength, 0, 8);
              MultimediaFileLength = BitConverter.ToInt64(aMultimediaFileLength, 0);

              await ms.ReadAsync(aIsStopTransfer, 0, 1);
              IsStopTransfer = BitConverter.ToBoolean(aIsStopTransfer, 0);

              MultimediaFileBuffer = new byte[ms.Capacity - 9];
              await ms.ReadAsync(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length);
            }
          }
          else
          {
            IsStopTransfer = true;
            MultimediaFileBuffer = null;
          }

          if (MultimediaFileLength > 0 && MultimediaFileBuffer != null)
          {
            Speed = (int)((8 * MultimediaFileBuffer.Length / Delta.TotalSeconds) / 1024);
            Size = Size + MultimediaFileBuffer.Length;
            AverageDelay = AverageDelay + Delta.Milliseconds;
            AverageSpeed = AverageSpeed + Speed;

            lblTime.Content = (Convert.ToInt32(lblTime.Content) + Delta.Seconds).ToString();
            lblChunk.Content = MultimediaFileChunkCount.ToString();
            lblDelay.Content = Delta.Milliseconds.ToString();
            lblSpeed.Content = Speed.ToString() + " Кб/с";
            lblSize.Content = Size.ToString() + " bytes";

            if (IsFirstFlush)
            {
              IsFirstFlush = false;
              pb.Maximum = (double)(MultimediaFileLength / MultimediaFileBuffer.Length);
            }
            pb.Value++;
          }
          else
          {
            IsStopTransfer = true;
          }
        }

        AverageDelay = (int)(AverageDelay / pb.Value);
        AverageSpeed = (int)(AverageSpeed / pb.Value);
        lblDelay.Content = AverageDelay.ToString();
        lblSpeed.Content = AverageSpeed.ToString() + " Кб/с";
        if (Size == MultimediaFileLength)
          lblSize.Background = labelBrushOK;
        else
          lblSize.Background = labelBrushBAD;
      }
    }

    private void StartClient(int CountClient)
    {
      string MultimediaFileGUID = "27108BF7-24EF-4005-9836-6E4F417504CD";
      //A56291CC-D35F-456A-B1B3-B0EC2F8D5A95
      byte[] MultimediaFileBuffer = null; int MultimediaFileChunkCount; long MultimediaFileLength = 0;
      string KeyGuid = System.Guid.NewGuid().ToString();
      DateTime StartTime; DateTime StopTime; TimeSpan Delta;
      int Speed; int Size = 0; int AverageSpeed = 0; int AverageDelay = 0;

      const int CountControlConst = 10;
      const int CountControlClient = 7;

      ProgressBar pb = null; Label lblTime = null; Label lblChunk = null; Label lblDelay = null; Label lblSpeed = null; Label lblSize = null;

      this.gridRun.Dispatcher.Invoke((Action)(() =>
      {
        pb = (ProgressBar)this.gridRun.Children[(CountClient) * CountControlClient + 1 + CountControlConst];
        pb.Minimum = 1; pb.Value = 1;

        lblTime = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 2 + CountControlConst];
        lblChunk = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 3 + CountControlConst];
        lblDelay = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 4 + CountControlConst];
        lblSpeed = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 5 + CountControlConst];
        lblSize = (Label)this.gridRun.Children[(CountClient) * CountControlClient + 6 + CountControlConst];
      }
      ));

      using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
      {
        LiveMultimediaSystem.Open();

        MultimediaFileChunkCount = 0;
        bool IsStopTransfer = false; bool IsFirstFlush = true;
        byte[] FullMultimediaFileBuffer;
        byte[] aMultimediaFileLength = new byte[8]; byte[] aIsStopTransfer = new byte[1];

        while (!IsStopTransfer)
        {
          MultimediaFileChunkCount++;

          MultimediaFileLength = 0; MultimediaFileBuffer = null;

          StartTime = DateTime.Now;

          FullMultimediaFileBuffer = LiveMultimediaSystem.RemoteGetMultimediaFileBuffer(UserToken, KeyGuid, MultimediaFileGUID, MultimediaFileChunkCount);

          StopTime = DateTime.Now; Delta = StopTime.Subtract(StartTime);

          if (FullMultimediaFileBuffer != null)
          {
            using (MemoryStream ms = new MemoryStream(FullMultimediaFileBuffer))
            {
              ms.Read(aMultimediaFileLength, 0, 8);
              MultimediaFileLength = BitConverter.ToInt64(aMultimediaFileLength, 0);

              ms.Read(aIsStopTransfer, 0, 1);
              IsStopTransfer = BitConverter.ToBoolean(aIsStopTransfer, 0);

              MultimediaFileBuffer = new byte[ms.Capacity - 9];
              ms.Read(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length);
            }
          }
          else
          {
            IsStopTransfer = true;
            MultimediaFileBuffer = null;
          }

          if (MultimediaFileLength > 0 && MultimediaFileBuffer != null)
          {
            Speed = (int)((8 * MultimediaFileBuffer.Length / Delta.TotalSeconds) / 1024);
            Size = Size + MultimediaFileBuffer.Length;
            AverageDelay = AverageDelay + Delta.Milliseconds;
            AverageSpeed = AverageSpeed + Speed;

            this.gridRun.Dispatcher.Invoke((Action)(() =>
            {
              lblTime.Content = (Convert.ToInt32(lblTime.Content) + Delta.Seconds).ToString();
              lblChunk.Content = MultimediaFileChunkCount.ToString();
              lblDelay.Content = Delta.Milliseconds.ToString();
              lblSpeed.Content = Speed.ToString() + " Кб/с";
              lblSize.Content = Size.ToString() + " bytes";
            }
            ));

            if (IsFirstFlush)
            {
              IsFirstFlush = false;
              this.gridRun.Dispatcher.Invoke((Action)(() => pb.Maximum = (double)(MultimediaFileLength / MultimediaFileBuffer.Length)));
            }
            this.gridRun.Dispatcher.Invoke((Action)(() => pb.Value++));
          }
          else
          {
            IsStopTransfer = true;
          }
        }

        this.gridRun.Dispatcher.Invoke((Action)(() =>
        {
          AverageDelay = (int)(AverageDelay / pb.Value);
          AverageSpeed = (int)(AverageSpeed / pb.Value);
          lblDelay.Content = AverageDelay.ToString();
          lblSpeed.Content = AverageSpeed.ToString() + " Кб/с";
          if (Size == MultimediaFileLength)
            lblSize.Background = labelBrushOK;
          else
            lblSize.Background = labelBrushBAD;
        }
        ));
      }
    }

    private async void btStart_Click(object sender, RoutedEventArgs e)
    {
      if (UserToken == "" || UserToken == null)
      {
        await LoginToLiveMultimediaSystemAsync();
        if (UserToken == "")
        {
          MessageBox.Show("UserToken is empty");
          return;
        }
      }

      tokenRequest = new CancellationTokenSource();
      await TestClientAsync(tokenRequest.Token);
    }

    private void btStartAll_Click(object sender, RoutedEventArgs e)
    {
      int CountClients = Convert.ToInt32(this.tbCountClients.Text);
      for (int i = 0; i < CountClients; i++) btStart_Click(null, null);
    }

    private async Task TestClientAsync(CancellationToken ct)
    {
      Label lblRunClient = new Label();
      lblRunClient.Content = "Client " + (CountRunningClients + 1).ToString();
      lblRunClient.HorizontalAlignment = HorizontalAlignment.Left; lblRunClient.VerticalAlignment = VerticalAlignment.Top;
      lblRunClient.Width = 67; lblRunClient.Height = labelHeight;
      lblRunClient.BorderThickness = labelBorder; lblRunClient.BorderBrush = labelBrush;

      Thickness marginLabel = lblRunClient.Margin;
      marginLabel.Top = this.tbCountClients.Margin.Top+this.tbCountClients.ActualHeight+5+(double)CountRunningClients * 25;
      marginLabel.Left = 5;
      lblRunClient.Margin = marginLabel;
////////////////////////////////////////////////////////////////////////////////////////////////
      ProgressBar pbClient = new ProgressBar();
      pbClient.Name = "pbClient" + (CountRunningClients + 1).ToString();

      if (ProgressBarWidght == 0) ProgressBarWidght = this.gridRun.ActualWidth;      

      Thickness marginProgressBar = pbClient.Margin;
      marginProgressBar.Left = lblRunClient.Margin.Left + lblRunClient.Width+5;
      marginProgressBar.Top = lblRunClient.Margin.Top+8;
      pbClient.Width = ProgressBarWidght - 290; pbClient.Height = 10;
      pbClient.Margin = marginProgressBar;
      pbClient.HorizontalAlignment = HorizontalAlignment.Left;
      pbClient.VerticalAlignment = VerticalAlignment.Top;
////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblTime = new Label();
      lblTime.Name = "lblTime" + (CountRunningClients + 1).ToString();
      lblTime.Content = "0";
      lblTime.HorizontalAlignment = HorizontalAlignment.Left; lblTime.VerticalAlignment = VerticalAlignment.Top;
      lblTime.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblTime.Width = 40; lblTime.Height = labelHeight;
      lblTime.BorderThickness = labelBorder; lblTime.BorderBrush = labelBrush;

      Thickness marginTime = lblTime.Margin;
      marginTime.Top = lblRunClient.Margin.Top;
      marginTime.Left = pbClient.Margin.Left + pbClient.Width + 5;
      lblTime.Margin = marginTime;
////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblChunk = new Label();
      lblChunk.Name = "lblChunk" + (CountRunningClients + 1).ToString();
      lblChunk.Content = "0";
      lblChunk.HorizontalAlignment = HorizontalAlignment.Left; lblChunk.VerticalAlignment = VerticalAlignment.Top;
      lblChunk.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblChunk.Width = 36; lblChunk.Height = labelHeight;
      lblChunk.BorderThickness = labelBorder; lblChunk.BorderBrush = labelBrush;

      Thickness marginChunk = lblChunk.Margin;
      marginChunk.Top = lblRunClient.Margin.Top;
      marginChunk.Left = lblTime.Margin.Left + lblTime.Width + 5;
      lblChunk.Margin = marginChunk;
////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblDelay = new Label();
      lblDelay.Name = "lblDelay" + (CountRunningClients + 1).ToString();
      lblDelay.Content = "0";
      lblDelay.HorizontalAlignment = HorizontalAlignment.Left; lblDelay.VerticalAlignment = VerticalAlignment.Top;
      lblDelay.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblDelay.Width = 46; lblDelay.Height = labelHeight;
      lblDelay.BorderThickness = labelBorder; lblDelay.BorderBrush = labelBrush;

      Thickness marginDelay = lblDelay.Margin;
      marginDelay.Top = lblRunClient.Margin.Top;
      marginDelay.Left = lblChunk.Margin.Left + lblChunk.Width + 5;
      lblDelay.Margin = marginDelay;
////////////////////////////////////////////////////////////////////////////////////////////////
      Label lblSpeed = new Label();
      lblSpeed.Name = "lblSpeed" + (CountRunningClients + 1).ToString();
      lblSpeed.Content = "0 Кб/с";
      lblSpeed.HorizontalAlignment = HorizontalAlignment.Left; lblSpeed.VerticalAlignment = VerticalAlignment.Top;
      lblSpeed.HorizontalContentAlignment = HorizontalAlignment.Right;
      lblSpeed.Width = 67; lblSpeed.Height = labelHeight;
      lblSpeed.BorderThickness = labelBorder; lblSpeed.BorderBrush = labelBrush;

      Thickness marginSpeed = lblSpeed.Margin;
      marginSpeed.Top = lblRunClient.Margin.Top;
      marginSpeed.Left = lblDelay.Margin.Left + lblDelay.Width + 5;      
      lblSpeed.Margin = marginSpeed;
////////////////////////////////////////////////////////////////////////////////////////////////
      CountRunningClients++;

      this.gridRun.Children.Add(lblRunClient);
      this.gridRun.Children.Add(pbClient);
      this.gridRun.Children.Add(lblTime);
      this.gridRun.Children.Add(lblChunk);      
      this.gridRun.Children.Add(lblDelay);
      this.gridRun.Children.Add(lblSpeed);

      await StartClientAsync(CountRunningClients);
    }

    private async Task StartClientAsync(int CountClient)
    {
      string MultimediaFileGUID = "27108BF7-24EF-4005-9836-6E4F417504CD";
      //A56291CC-D35F-456A-B1B3-B0EC2F8D5A95
      byte[] MultimediaFileBuffer = null; int MultimediaFileChunkCount; long MultimediaFileLength = 0;
      string KeyGuid = System.Guid.NewGuid().ToString();
      DateTime StartTime; DateTime StopTime; TimeSpan Delta; int Speed; int AverageSpeed = 0; int AverageDelay = 0;

      const int CountControlConst = 10;
      const int CountControlClient = 6;

      try
      {
        ProgressBar pb = (ProgressBar)this.gridRun.Children[(CountClient - 1) * CountControlClient + 1 + CountControlConst];
        Label lblTime = (Label)this.gridRun.Children[(CountClient - 1) * CountControlClient + 2 + CountControlConst];
        Label lblChunk = (Label)this.gridRun.Children[(CountClient - 1) * CountControlClient + 3 + CountControlConst];
        Label lblDelay = (Label)this.gridRun.Children[(CountClient - 1) * CountControlClient + 4 + CountControlConst];
        Label lblSpeed = (Label)this.gridRun.Children[(CountClient - 1) * CountControlClient + 5 + CountControlConst];

        pb.Minimum = 1; pb.Value = 1;

        using (LiveMultimediaServiceClient LiveMultimediaSystem = new LiveMultimediaServiceClient(binding, endpoint))
        {
          LiveMultimediaSystem.Open();

          MultimediaFileChunkCount = 0;
          bool IsStopTransfer = false; bool IsFirstFlush = true;
          byte[] FullMultimediaFileBuffer;
          byte[] aMultimediaFileLength = new byte[8]; byte[] aIsStopTransfer = new byte[1];

          while (!IsStopTransfer)
          {
            MultimediaFileChunkCount++;

            MultimediaFileLength = 0; MultimediaFileBuffer = null;

            StartTime = DateTime.Now;
            FullMultimediaFileBuffer = await LiveMultimediaSystem.RemoteGetMultimediaFileBufferAsync(UserToken + "|" + KeyGuid, MultimediaFileGUID, MultimediaFileChunkCount);
            StopTime = DateTime.Now; Delta = StopTime.Subtract(StartTime);

            if (FullMultimediaFileBuffer != null)
            {
              using (MemoryStream ms = new MemoryStream(FullMultimediaFileBuffer))
              {
                await ms.ReadAsync(aMultimediaFileLength, 0, 8);
                MultimediaFileLength = BitConverter.ToInt64(aMultimediaFileLength, 0);

                await ms.ReadAsync(aIsStopTransfer, 0, 1);
                IsStopTransfer = BitConverter.ToBoolean(aIsStopTransfer, 0);

                MultimediaFileBuffer = new byte[ms.Capacity - 9];
                await ms.ReadAsync(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length);
              }
            }
            else
            {
              IsStopTransfer = true;
            }

            if (MultimediaFileLength > 0 && MultimediaFileBuffer != null)
            {
              lblTime.Content = (Convert.ToInt32(lblTime.Content) + Delta.Seconds).ToString();

              lblChunk.Content = MultimediaFileChunkCount.ToString();

              lblDelay.Content = Delta.Milliseconds.ToString();
              AverageDelay = AverageDelay + Delta.Milliseconds;

              Speed = (int)((8 * MultimediaFileBuffer.Length / Delta.TotalSeconds) / 1024);
              lblSpeed.Content = Speed.ToString() + " Кб/с";
              AverageSpeed = AverageSpeed + Speed;

              if (IsFirstFlush)
              {
                IsFirstFlush = false;
                pb.Maximum = (double)(MultimediaFileLength / MultimediaFileBuffer.Length);
              }
              //Write to test file for compare
              //Response.OutputStream.Write(MultimediaFileBuffer, 0, MultimediaFileBuffer.Length);
              pb.Value++;
            }
            else
            {
              IsStopTransfer = true;
            }
          }
          AverageDelay = (int)(AverageDelay / pb.Value);
          AverageSpeed = (int)(AverageSpeed / pb.Value);
          lblDelay.Content = AverageDelay.ToString();
          lblSpeed.Content = AverageSpeed.ToString() + " Кб/с";
        }
      }
      catch (Exception ex)
      {
        ErrorMessage(ex, "StartClientAsync");
      }
    }   


*/
