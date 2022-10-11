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

using System.Data;
using System.Net;
using System.Net.Mail;
using LiveMultimediaDataLayer;

namespace LiveMultimediaEmailPush
{

  public class LanguageInfo
  {
    public string Language { get; set; }
  }

  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      Start();
    }

    public async void Start()
    {
      //textBlockEmailEditor.Visibility = Visibility.Collapsed;

      await GetEmailLanguageAsync();
      await GetEmailPushAsync();      
    }

    public async Task GetEmailLanguageAsync()
    {
      var DataViewLanguage = (await LiveMultimediaDL.GetEmailLanguagesAsync()).AsDataView();
      var ListLanguages = new List<LanguageInfo>(); LanguageInfo LanguageItem;
      foreach (DataRowView item in DataViewLanguage)
      {
        LanguageItem = new LanguageInfo();
        LanguageItem.Language = item.Row.ItemArray[0].ToString();
        ListLanguages.Add(LanguageItem);
      }

      listBoxEmailLanguage.ItemsSource = ListLanguages.ToArray();
      listBoxEmailLanguage.SelectedIndex = 0;
    }

    public async Task GetEmailPushAsync()
    {
      dataGridEmailPush.ItemsSource = (await LiveMultimediaDL.GetEmailPushAsync()).AsDataView();
      dataGridEmailPush.SelectedIndex = 0;
    }

    public async Task GetEmailLanguageNotInMailAsync()
    {
      try
      {
        var IdEmailPush = (dataGridEmailPush.SelectedItem as DataRowView).Row.Field<long>("IdEmailPush");
        var DataViewLanguage = (await LiveMultimediaDL.GetEmailLanguageNotInMailAsync(IdEmailPush)).AsDataView();

        var ListLanguages = new List<LanguageInfo>(); LanguageInfo LanguageItem;
        foreach (DataRowView item in DataViewLanguage)
        {
          LanguageItem = new LanguageInfo();
          LanguageItem.Language = item.Row.ItemArray[0].ToString();
          ListLanguages.Add(LanguageItem);
        }
        listBoxEmailLanguageInMail.ItemsSource = ListLanguages.ToArray();
        listBoxEmailLanguageInMail.SelectedIndex = 0;
      }
      catch (Exception)
      {
      }
    }

    public async Task GetEmailAsync()
    {
      try
      {
        var IdEmailPush = (dataGridEmailPush.SelectedItem as DataRowView).Row.Field<long>("IdEmailPush");
        dataGridEmail.ItemsSource = (await LiveMultimediaDL.GetEmailAsync(IdEmailPush)).AsDataView();

        var CountColumns = dataGridEmail.Columns.Count;

        dataGridEmail.Columns[CountColumns - 1].Visibility = Visibility.Hidden;
        dataGridEmail.Columns[CountColumns - 2].Visibility = Visibility.Hidden;
      }
      catch (Exception)
      {
      }
    }

    public async Task<long> GetIdEmailByLanguageAsync(long IdEmailPush, string Language)
    {
      long IdEmail;

      try
      {
        var Table = await LiveMultimediaDL.GetIdEmailByLanguageAsync(IdEmailPush, Language);
        IdEmail = Table.Rows[0].Field<long>("IdEmail");
      }
      catch (Exception)
      {
        IdEmail = 0;
      }

      return IdEmail;
    }

    public async Task GetEmailSendPrepareAsync()
    {
      try
      {
        var IdEmailPush = (dataGridEmailPush.SelectedItem as DataRowView).Row.Field<long>("IdEmailPush");
        dataGridEmailSendPrepare.ItemsSource = (await LiveMultimediaDL.GetEmailSendPrepareAsync(IdEmailPush)).AsDataView();
      }
      catch (Exception)
      {
      }
    }

    public async Task GetEmailSendAsync()
    {
      try
      {
        var IdEmailPush = (dataGridEmailPush.SelectedItem as DataRowView).Row.Field<long>("IdEmailPush");
        dataGridEmailSend.ItemsSource = (await LiveMultimediaDL.GetEmailSendAsync(IdEmailPush)).AsDataView();
      }
      catch (Exception)
      {
      }
    }

    private async void dataGridEmailPush_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      cmdPrepareEmailSend.IsEnabled = false;
      cmdEmailSend.IsEnabled = false;

      await GetEmailLanguageNotInMailAsync();
      await GetEmailAsync();      
      await GetEmailSendPrepareAsync();
      await GetEmailSendAsync();

      if (dataGridEmailSendPrepare.Items.Count > 0) cmdPrepareEmailSend.IsEnabled = true;
      if (dataGridEmailSend.Items.Count > 0) cmdEmailSend.IsEnabled = true;
    }

    private async void cmdPrepareEmailSend_Click(object sender, RoutedEventArgs e)
    {
      #region Define vars
      long IdEmailPush;
      long IdEmail;
      string UserName;
      string UserEmail;
      string Language;
      #endregion Define vars

      try
      {
        var IsCompleted = (dataGridEmailPush.SelectedItem as DataRowView).Row.Field<bool>("IsCompleted");
        if (!IsCompleted)
        {
          if (dataGridEmailSendPrepare.SelectedItems.Count > 0)
          {
            var reply = MessageBox.Show("Подготовить письма для отправки для выбранных пользователей?", "Подготовка писем для выбранных пользователей", MessageBoxButton.YesNo);
            if (reply == MessageBoxResult.No) return;

            IdEmailPush = (dataGridEmailPush.SelectedItem as DataRowView).Row.Field<long>("IdEmailPush");

            pbStatusOperation.Minimum = 0; pbStatusOperation.Maximum = dataGridEmailSendPrepare.SelectedItems.Count; pbStatusOperation.Value = 0;
            foreach (DataRowView item in dataGridEmailSendPrepare.SelectedItems)
            {
              UserName = item.Row.Field<string>("UserName");
              UserEmail = item.Row.Field<string>("UserEmail");
              Language = item.Row.Field<string>("Language");

              IdEmail = await GetIdEmailByLanguageAsync(IdEmailPush, Language);

              await LiveMultimediaDL.AddEmailSendAsync(IdEmailPush, IdEmail, UserName, UserEmail);

              pbStatusOperation.Value += 1;
            }

            await GetEmailSendPrepareAsync();
            await GetEmailSendAsync();

            MessageBox.Show("Подготовка списка для выбранных пользователей завершена", "Подготовка списка для выбранных пользователей");
            pbStatusOperation.Value = 0;
          }
          else
          {
            MessageBox.Show("Не выбраны пользователи", "Подготовка писем для выбранных пользователей");
          }
        }
        else
        {
          MessageBox.Show("Выбранная рассылка уже завершена", "Подготовка писем для выбранных пользователей");
        }        
      }
      catch (Exception)
      {
      }
    }

    private async void cmdEmailSend_Click(object sender, RoutedEventArgs e)
    {
      #region Define vars
      long IdEmailSend;
      long IdEmail;
      string UserName, UserEmail;
      DataTable Table;
      string EmailSubject, EmailBody;
      bool IsSended; string Error;
      #endregion Define vars

      #region Define SMTP parameters
      var SmtpFrom = "support@live-mm.com";
      var SmtpBcc = "support@live-mm.com";
      var SmtpHost = "mail.nic.ru";
      var SmtpPort = 25;
      var SmtpEnableSsl = false;
      var SmtpDefaultCredentials = false;
      var SmtpUserName = "support@live-mm.com";
      var SmtpPassword = "Takeiteasy2";
      #endregion Define SMTP parameters

      try
      {
        var IsCompleted = (dataGridEmailPush.SelectedItem as DataRowView).Row.Field<bool>("IsCompleted");
        if (!IsCompleted)
        {
          if (dataGridEmailSend.SelectedItems.Count > 0)
          {
            var reply = MessageBox.Show("Отправить письма выбранным пользователям?", "Отправка писем выбранным пользователям", MessageBoxButton.YesNo);
            if (reply == MessageBoxResult.No) return;

            pbStatusOperation.Minimum = 0; pbStatusOperation.Maximum = dataGridEmailSend.SelectedItems.Count; pbStatusOperation.Value = 0;
            foreach (DataRowView item in dataGridEmailSend.SelectedItems)
            {
              #region Get values for email
              IdEmailSend = item.Row.Field<long>("IdEmailSend");
              IdEmail = item.Row.Field<long>("IdEmail");
              UserName = item.Row.Field<string>("UserName");
              UserEmail = item.Row.Field<string>("UserEmail");
              IsSended = item.Row.Field<bool>("IsSended");

              //UserEmail = "***";

              Table = await LiveMultimediaDL.GetEmailByIdAsync(IdEmail);
              EmailSubject = Table.Rows[0].Field<string>("EmailSubject");
              EmailBody = Table.Rows[0].Field<string>("EmailBody");
              #endregion Get values for email

              if (!string.IsNullOrEmpty(EmailSubject) && !string.IsNullOrEmpty(EmailBody))
              {
                if (IsSended)
                {
                  reply = MessageBox.Show("Письмо уже было отправлено пользователю " + UserName + " по адресу " + UserEmail + ". Повторить отправку?", "Повторная отправка", MessageBoxButton.YesNo);
                }
                else
                  reply = MessageBoxResult.Yes;

                if (reply == MessageBoxResult.Yes)
                {
                  #region Send email to user
                  try
                  {
                    #region Define mail
                    var from = new MailAddress(SmtpFrom, "Live Multimedia Market", Encoding.UTF8);
                    var to = new MailAddress(UserEmail);
                    var bcc = new MailAddress(SmtpBcc);
                    var mail = new MailMessage(from, to);

                    mail.Bcc.Add(bcc);
                    mail.IsBodyHtml = true;
                    mail.DeliveryNotificationOptions = DeliveryNotificationOptions.Never;
                    #endregion Define mail

                    #region Define subject
                    mail.SubjectEncoding = Encoding.UTF8;
                    mail.Subject = EmailSubject;
                    #endregion Define subject

                    #region Define body
                    EmailBody = EmailBody.Replace("%UserName%", UserName);
                    EmailBody = EmailBody.Replace("%UserEmail%", UserEmail);

                    mail.BodyEncoding = Encoding.UTF8;
                    mail.Body = EmailBody;
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

                    IsSended = true; Error = "";
                    await LiveMultimediaDL.ChangeEmailSendAsync(IdEmailSend, IsSended, Error, false);
                  }
                  catch (Exception ex)
                  {
                    Error = ex.Message;
                    await LiveMultimediaDL.ChangeEmailSendAsync(IdEmailSend, IsSended, Error, true);
                  }
                  #endregion Send email to user
                }
              }
              else
              {
                Error = "Тема письма или тело письма пустые";
                await LiveMultimediaDL.ChangeEmailSendAsync(IdEmailSend, IsSended, Error, true);
              }

              pbStatusOperation.Value += 1;
            }

            await GetEmailSendAsync();

            MessageBox.Show("Рассылка писем закончена", "Рассылка писем пользователям");
            pbStatusOperation.Value = 0;
          }
          else
          {
            MessageBox.Show("Не выбраны пользователи", "Рассылка писем пользователям");
          }
        }
        else
        {
          MessageBox.Show("Выбранная рассылка уже завершена", "Рассылка писем пользователям");
        }
      }
      catch (Exception ex)
      {
      }
    }

    private void dataGridEmail_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      var grid = (DataGrid)sender;

      string EmailPart;
      switch (grid.CurrentCell.Column.Header.ToString())
      {
        case "Subject":
          EmailPart = ((DataRowView)grid.SelectedItem).Row.Field<string>("EmailSubject");
          break;
        case "Body":
          EmailPart = ((DataRowView)grid.SelectedItem).Row.Field<string>("EmailBody");
          break;
        default:
          EmailPart = null;
          break;
      }

      //textBlockEmailEditor.Text = EmailPart;
      //textBlockEmailEditor.Visibility = Visibility.Visible;
    }
  }
}
