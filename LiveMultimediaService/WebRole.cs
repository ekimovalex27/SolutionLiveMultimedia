using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace JetSAS
{
  public class WebRole : RoleEntryPoint
  {
    public override bool OnStart()
    {
      // Чтобы включить AzureLocalStorageTraceListner, раскомментируйте соответствующий раздел в файле web.config  

      // Я закомментировал ниже три строки, так как есть ошибка выполнения в AzureLocalStorageTraceListener.cs при выполнении строки:
      // directory.Path = RoleEnvironment.GetLocalResource("LiveMultimediaService.svclog").RootPath;

      //DiagnosticMonitorConfiguration diagnosticConfig = DiagnosticMonitor.GetDefaultInitialConfiguration();
      //diagnosticConfig.Directories.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);
      //diagnosticConfig.Directories.DataSources.Add(AzureLocalStorageTraceListener.GetLogDirectory());

      // Дополнительные сведения по управлению изменениями конфигурации
      // см. раздел MSDN по ссылке http://go.microsoft.com/fwlink/?LinkId=166357.

      return base.OnStart();
    }
  }
}
