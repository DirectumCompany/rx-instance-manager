using System;
using System.Windows;

namespace RXInstanceManager
{
  public partial class App : Application
  {
    protected async override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
      var mainWindow = new MainWindow();
      MainWindow = mainWindow;
      MainWindow.Show();
      await mainWindow.StartAsyncHandlers();
    }
  }
}