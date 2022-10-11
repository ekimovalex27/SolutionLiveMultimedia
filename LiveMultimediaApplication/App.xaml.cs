using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LiveMultimediaApplication
{
  /// <summary>
  /// Логика взаимодействия для App.xaml
  /// </summary>
  /// 

  public partial class App : Application
  {

    public enum ApplicationExitCode
    {
      Success = 0,
      Failure = 1,
      CantWriteToApplicationLog = 2,
      CantPersistApplicationState = 3
    }

    void App_Exit(object sender, ExitEventArgs e)
    {
      try
      {
        // Write entry to application log
        if (e.ApplicationExitCode == (int)ApplicationExitCode.Success)
        {
          //WriteApplicationLogEntry("Failure", e.ApplicationExitCode);
        }
        else
        {
          //WriteApplicationLogEntry("Success", e.ApplicationExitCode);
        }
      }
      catch
      {
        // Update exit code to reflect failure to write to application log
        e.ApplicationExitCode = (int)ApplicationExitCode.CantWriteToApplicationLog;
      }

      // Persist application state
      try
      {
        //PersistApplicationState();
      }
      catch
      {
        // Update exit code to reflect failure to persist application state
        e.ApplicationExitCode = (int)ApplicationExitCode.CantPersistApplicationState;
      }
    }

  }
}
