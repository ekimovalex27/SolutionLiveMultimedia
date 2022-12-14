using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace LiveMultimediaServer
{
    /// <summary>
    /// Обеспечивает зависящее от конкретного приложения поведение, дополняющее класс Application по умолчанию.
    /// </summary>
    sealed partial class App : Application
    {
    /// <summary>
    /// Инициализирует одноэлементный объект приложения.  Это первая выполняемая строка разрабатываемого
    /// кода; поэтому она является логическим эквивалентом main() или WinMain().
    /// </summary>
    public App()
    {
      this.InitializeComponent();
      this.Suspending += OnSuspending;
    }

    /// <summary>
    /// Вызывается при обычном запуске приложения пользователем.  Будут использоваться другие точки входа,
    /// например, если приложение запускается для открытия конкретного файла.
    /// </summary>
    /// <param name="e">Сведения о запросе и обработке запуска.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
#if DEBUG
      if (System.Diagnostics.Debugger.IsAttached)
      {
        this.DebugSettings.EnableFrameRateCounter = true;
      }
#endif

      //AEV
#if DEBUG
      LiveMultimediaServiceConnection.Language = "en";
#endif

      Frame rootFrame = Window.Current.Content as Frame;
      
      // Не повторяйте инициализацию приложения, если в окне уже имеется содержимое,
      // только обеспечьте активность окна
      if (rootFrame == null)
      {
        // Создание фрейма, который станет контекстом навигации, и переход к первой странице
        rootFrame = new Frame();

        rootFrame.NavigationFailed += OnNavigationFailed;
        rootFrame.Navigated += OnNavigatedTo;

        if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
        {
          //TODO: Загрузить состояние из ранее приостановленного приложения
        }

        // Размещение фрейма в текущем окне
        Window.Current.Content = rootFrame;

        //Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
        //    rootFrame.CanGoBack ?
        //    Windows.UI.Core.AppViewBackButtonVisibility.Visible :
        //    Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
      }

      if (e.PrelaunchActivated == false)
      {
        if (rootFrame.Content == null)
        {
          // Если стек навигации не восстанавливается для перехода к первой странице,
          // настройка новой страницы путем передачи необходимой информации в качестве параметра
          // параметр
          rootFrame.Navigate(typeof(MainPage), e.Arguments);
          //rootFrame.Navigate(typeof(Language), e.Arguments);
        }
        // Обеспечение активности текущего окна
        Window.Current.Activate();

        //AEV Для обработки события кнопки "Назад"
        Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
        //rootFrame.Navigated
      }
    }

    //AEV Обработка события кнопки "Назад"
    private void App_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
    {
      var rootFrame = Window.Current.Content as Frame;
      if (rootFrame == null) return;

      // Navigate back if possible, and if the event has not already been handled .
      if (rootFrame.CanGoBack && !e.Handled)
      {
        e.Handled = true;
        rootFrame.GoBack();
      }
    }

    //AEV Отображение кнопки "Назад"
    private void OnNavigatedTo(object sender, NavigationEventArgs e)
    {
      Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                 (sender as Frame).CanGoBack ?
                 Windows.UI.Core.AppViewBackButtonVisibility.Visible :
                 Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
    }

    /// <summary>
    /// Вызывается в случае сбоя навигации на определенную страницу
    /// </summary>
    /// <param name="sender">Фрейм, для которого произошел сбой навигации</param>
    /// <param name="e">Сведения о сбое навигации</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
      throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }

    /// <summary>
    /// Вызывается при приостановке выполнения приложения.  Состояние приложения сохраняется
    /// без учета информации о том, будет ли оно завершено или возобновлено с неизменным
    /// содержимым памяти.
    /// </summary>
    /// <param name="sender">Источник запроса приостановки.</param>
    /// <param name="e">Сведения о запросе приостановки.</param>
    private void OnSuspending(object sender, SuspendingEventArgs e)
    {
      var deferral = e.SuspendingOperation.GetDeferral();
      //TODO: Сохранить состояние приложения и остановить все фоновые операции
      deferral.Complete();
    }

  }
}
