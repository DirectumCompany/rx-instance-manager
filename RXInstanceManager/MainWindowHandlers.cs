using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace RXInstanceManager
{
  public partial class MainWindow : Window
  {
    #region Работа с Grid.

    private void LoadInstances()
    {
      LoadInstancesItems();
    }

    private void LoadInstances(string instancePath)
    {
      if (string.IsNullOrEmpty(instancePath))
        LoadInstancesItems();
      else
        LoadInstancesItems(instancePath);
    }

    private void LoadInstancesItems()
    {
      var instances = Instances.Get();
      GridInstances.ItemsSource = instances.OrderByDescending(x => x.Status).ThenBy(x => x.InstancePath);
      CollectionViewSource.GetDefaultView(GridInstances.ItemsSource).Refresh();
      GridInstances.UpdateLayout();
      if (GridInstances.Items.Count > 0)
      {
        var item = GridInstances.Items[0] as Instance;
        GridInstances.SelectedItem = item;
        GridInstances.ScrollIntoView(item);
        GridInstances.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
      }
      else
      {
        GridInstances.SelectedItem = null;
        GridInstances.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
      }
    }

    private void LoadInstancesItems(string instancePath)
    {
      var instances = Instances.Get();
      GridInstances.ItemsSource = instances.OrderByDescending(x => x.Status).ThenBy(x => x.InstancePath);

      if (!string.IsNullOrEmpty(instancePath) && GridInstances.Items.Count > 0)
      {
        CollectionViewSource.GetDefaultView(GridInstances.ItemsSource).Refresh();
        GridInstances.UpdateLayout();
        var item = GridInstances.Items[0] as Instance;
        for (int i = 0; i < GridInstances.Items.Count; i++)
        {
          item = GridInstances.Items[i] as Instance;
          if (item.InstancePath == instancePath)
            break;
        }
        GridInstances.SelectedItem = item;
        GridInstances.ScrollIntoView(item);
        GridInstances.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
      }
    }

    #endregion

    #region Работа с визуальными эффектами.

    private void ActionButtonVisibleChanging(string status = null)
    {
      ButtonStart.Visibility = Visibility.Collapsed;
      ButtonStop.Visibility = Visibility.Collapsed;
      ButtonDDSStart.Visibility = Visibility.Collapsed;
      ButtonRXStart.Visibility = Visibility.Collapsed;
      ButtonLogsFolder.Visibility = Visibility.Collapsed;

      var IsVisibleContextButton = _instance == null || string.IsNullOrEmpty(_instance.Code) ? Visibility.Collapsed : Visibility.Visible;
      ChangeProject.Visibility = IsVisibleContextButton;
      CreateProject.Visibility = IsVisibleContextButton;
      CloneProject.Visibility = IsVisibleContextButton;
      UpdateConfig.Visibility = IsVisibleContextButton;
      RunDDSWithOutDeploy.Visibility = IsVisibleContextButton;
      InfoContext.Visibility = IsVisibleContextButton;
      CmdAdminContext.Visibility = IsVisibleContextButton;
      ClearLogContext.Visibility = IsVisibleContextButton;
      ClearLogAllInstancesContext.Visibility = IsVisibleContextButton;
      ConfigContext.Visibility = IsVisibleContextButton;
      ProjectConfigContext.Visibility = IsVisibleContextButton;
      ConvertDBsContext.Visibility = IsVisibleContextButton;
      RemoveProjectDataContext.Visibility = IsVisibleContextButton;

      status = _instance == null || string.IsNullOrEmpty(_instance.Code) ? status : _instance.Status;
        
      switch (status)
      {
        case Constants.InstanceStatus.Stopped:
          ButtonDDSStart.Visibility = Visibility.Visible;
          ButtonRXStart.Visibility = Visibility.Collapsed;
          ButtonStop.Visibility = Visibility.Collapsed;
          ButtonStart.Visibility = Visibility.Visible;
          ButtonLogsFolder.Visibility = Visibility.Visible;
          break;
        case Constants.InstanceStatus.Working:
          ButtonDDSStart.Visibility = Visibility.Visible;
          ButtonRXStart.Visibility = Visibility.Visible;
          ButtonStop.Visibility = Visibility.Visible;
          ButtonStart.Visibility = Visibility.Collapsed;
          ButtonLogsFolder.Visibility = Visibility.Visible;
          break;
      }
    }

    #endregion

    #region Проверки перед выполнением действий.

    private static bool ValidateBeforeAddInstance(string instancePath)
    {
      if (!Directory.Exists(instancePath))
      {
        MessageBox.Show($"Папка {instancePath} не существует");
        return false;
      }

      var instances = Instances.Get();
      if (instances.Any(x => x.InstancePath == instancePath))
      {
        var code = instances.First(x => x.InstancePath == instancePath).Code;
        MessageBox.Show($"Выбранная папка уже является папкой экзампляра {code}",
                        "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        return false;
      }

      var instanceDLPath = System.IO.Path.Combine(instancePath, "DirectumLauncher.exe");
      if (!File.Exists(instanceDLPath))
      {
        MessageBox.Show("Выбранная папка не является папкой экземпляра DirectumRX (Не найден DirectumLauncher)",
                        "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        return false;
      }

      var configYamlPath = AppHelper.GetConfigYamlPath(instancePath);
      if (!File.Exists(configYamlPath))
      {
        var configExFile = AppHelper.GetConfigYamlExamplePath(instancePath);
        if (!File.Exists(configExFile))
        {
          MessageBox.Show("Выбранная папка не является папкой экземпляра DirectumRX (Не найден config.yml или config.yml.example)",
                          "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
          return false;
        }
      }

      return true;
    }

    private static bool ValidateBeforeInstallInstance(Dictionary<string, string> yamlValues)
    {
      if (_instance == null)
      {
        MessageBox.Show("Не выбран экзампляр");
        return false;
      }

      var instances = Instances.Get();
      if (instances.Count(x => x.Code == _instance.Code) > 1)
      {
        MessageBox.Show($"Экземпляр DirectumRX с кодом \"{_instance.Code}\" уже добавлен");
        return false;
      }

      var isServiceExists = AppHandlers.ServiceExists(_instance);
      if (isServiceExists)
      {
        MessageBox.Show($"Служба ServiceRunner с именем \"{_instance.ServiceName}\" уже утановлена");
        return false;
      }

      var name = yamlValues.GetConfigStringValue("variables.instance_name");
      if (string.IsNullOrWhiteSpace(name))
      {
        MessageBox.Show("Необходимо указать код в параметре \"variables.instance_name\" файла config.yml");
        return false;
      }

      var protocol = yamlValues.GetConfigStringValue("variables.protocol");
      if (protocol != "http")
      {
        MessageBox.Show("Поддержтвается только протокол http, скорректируйте параметр \"variables.protocol\" файла config.yml");
        return false;
      }

      var host = yamlValues.GetConfigStringValue("variables.host_fqdn");
      if (host == "host_name.example.com")
      {
        MessageBox.Show("Указан некорректный хост, скорректируйте параметр \"variables.host_fqdn\" файла config.yml");
        return false;
      }

      var port = yamlValues.GetConfigIntValue("variables.http_port");
      if (!port.HasValue || port.Value <= 0)
      {
        MessageBox.Show("Указан некорректный порт в параметре \"variables.http_port\" файла config.yml");
        return false;
      }

      if (instances.Any(x => x.Code != _instance.Code && x.Port == port))
      {
        var instance = instances.FirstOrDefault(x => x.Code != _instance.Code && x.Port == port);
        MessageBox.Show($"Указанный порт уже используется экземпляром \"{instance.Code}\"");
        return false;
      }

      var dbEngine = yamlValues.GetConfigStringValue("common_config.DATABASE_ENGINE");
      if (dbEngine != "mssql" && dbEngine != "postgres")
      {
        var code = instances.FirstOrDefault(x => x.Code != _instance.Code && x.DBName == _instance.DBName);
        MessageBox.Show("Указана некорректная СУДБ в параметре \"common_config.DATABASE_ENGINE\" файла config.yml");
        return false;
      }

      var storagePath = yamlValues.GetConfigStringValue("variables.home_path");
      if (string.IsNullOrWhiteSpace(storagePath))
      {
        MessageBox.Show("Не удалось вычислить путь к папке хранилища из параметра \"variables.home_path\" файла config.yml");
        return false;
      }

      if (instances.Any(x => x.Code != _instance.Code && x.StoragePath == storagePath))
      {
        var instance = instances.FirstOrDefault(x => x.Code != _instance.Code && x.StoragePath == storagePath);
        MessageBox.Show($"Указанная папка хранилища используется экземпляром \"{instance.Code}\"");
        return false;
      }

      var connection = yamlValues.GetConfigStringValue("common_config.CONNECTION_STRING");
      var dbName = AppHelper.GetDBNameFromConnectionString(dbEngine, connection);
      if (!string.IsNullOrWhiteSpace(dbName))
      {
        if (instances.Any(x => x.Code != _instance.Code && x.DBName == dbName))
        {
          var instance = instances.FirstOrDefault(x => x.Code != _instance.Code && x.DBName == dbName);
          MessageBox.Show($"Указанная база данных уже используется экземпляром \"{instance.Code}\"");
          return false;
        }
      }

      var sourcesPath = yamlValues.GetConfigStringValue("services_config.DevelopmentStudio.GIT_ROOT_DIRECTORY");
      if (!string.IsNullOrWhiteSpace(sourcesPath))
      {
        if (instances.Any(x => x.Code != _instance.Code && x.SourcesPath == sourcesPath))
        {
          var instance = instances.FirstOrDefault(x => x.Code != _instance.Code && x.SourcesPath == sourcesPath);
          MessageBox.Show($"Указанная папка исходников используется экземпляром \"{instance.Code}\"");
          return false;
        }

        var localProtocol = yamlValues.GetConfigStringValue("services_config.DevelopmentStudio.LOCAL_WEB_PROTOCOL");
        if (!string.IsNullOrWhiteSpace(localProtocol) && localProtocol != "http")
        {
          MessageBox.Show("Указан некорректный протокол в параметре \"services_config.DevelopmentStudio.LOCAL_WEB_PROTOCOL\" файла config.yml");
          return false;
        }

        var localPort = yamlValues.GetConfigIntValue("services_config.DevelopmentStudio.LOCAL_SERVER_HTTP_PORT");
        if (localPort.HasValue && localPort != port)
        {
          MessageBox.Show("Указан некорректный порт в параметре \"services_config.DevelopmentStudio.LOCAL_SERVER_HTTP_PORT\" файла config.yml");
          return false;
        }
      }

      return true;
    }

    #endregion
  }
}
