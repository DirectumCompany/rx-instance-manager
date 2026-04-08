using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Dynamic;

namespace RXInstanceManager
{
  public partial class MainWindow : Window
  {
    #region Работа с Grid.

    /// <summary>
    /// YamlDotNet may attach nested mappings as Dictionary&lt;object, object&gt; rather than string-keyed dictionary.
    /// </summary>
    private static Dictionary<string, object> GetContextMenuMap(object raw)
    {
      if (raw == null)
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

      if (raw is IDictionary<string, object> sd)
        return new Dictionary<string, object>(sd, StringComparer.OrdinalIgnoreCase);

      if (raw is IDictionary<object, object> od)
      {
        var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<object, object> kv in od)
          d[kv.Key != null ? kv.Key.ToString() : string.Empty] = kv.Value;
        return d;
      }

      if (raw is IDictionary legacy)
      {
        var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry e in legacy)
          d[e.Key != null ? e.Key.ToString() : string.Empty] = e.Value;
        return d;
      }

      throw new InvalidOperationException("rxman.config: contextMenu must be a YAML mapping.");
    }

    private void LoadConfig()
    {
      string rxInstManConfigFilePath = Path.Combine(AppContext.BaseDirectory, Constants.RXInstanceManagerConfigFileNane);


      if (!File.Exists(rxInstManConfigFilePath))
      {
        var contextMenu = new ContextMenuClass();

        contextMenu.ChangeProject = true;
        contextMenu.CreateProject = true;
        contextMenu.CloneProject = true;
        contextMenu.UpdateConfig = true;
        contextMenu.CheckServices = true;
        contextMenu.RunDDSWithOutDeploy = false;
        contextMenu.InfoContext = false;
        contextMenu.CmdAdminContext = true;
        contextMenu.ClearLogContext = true;
        contextMenu.ClearLogAllInstancesContext = true;
        contextMenu.ConfigContext = false;
        contextMenu.ProjectConfigContext = false;
        contextMenu.ConvertDBsContext = false;
        contextMenu.RemoveProjectDataContext = true;
        contextMenu.RemoveInstance = true;
        contextMenu.OpenRXFolder = false;
        contextMenu.OpenLogFolder = false;

        var config = new Config();
        config.LogViewer = "";
        config.NeedCheckAfterSet = false;
        config.ContextMenu = contextMenu;
        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
          .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
          .Build();
        var yaml = serializer.Serialize(config);
        File.WriteAllText(rxInstManConfigFilePath, yaml);
      }


      using (var yamlReader = new StreamReader(rxInstManConfigFilePath))
      {
        try
        {
          AppHandlers.logger.Info(string.Format("Чтение настроек из {0}", rxInstManConfigFilePath));
          var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
          dynamic ymlData = deserializer.Deserialize<ExpandoObject>(yamlReader.ReadToEnd());
          _configRxInstMan = new Config();
          var contextMenu = new ContextMenuClass();
          _configRxInstMan.ContextMenu = contextMenu;
          _configRxInstMan.LogViewer = ymlData.logViewer != null ? ymlData.logViewer.ToString() : string.Empty;
          _configRxInstMan.LogViewerExists = !string.IsNullOrWhiteSpace(_configRxInstMan.LogViewer) && File.Exists(_configRxInstMan.LogViewer);
          if (!string.IsNullOrWhiteSpace(_configRxInstMan.LogViewer) && !_configRxInstMan.LogViewerExists)
            AppHandlers.logger.Error(string.Format("Файл LogViewer {0} не найден", _configRxInstMan.LogViewer));
          _configRxInstMan.NeedCheckAfterSet = ymlData.needCheckAfterSet is bool needCheck
            ? needCheck
            : string.Equals(ymlData.needCheckAfterSet?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

          object contextMenuRaw = ((IDictionary<string, object>)ymlData).TryGetValue("contextMenu", out object cm) ? cm : null;
          var menu = GetContextMenuMap(contextMenuRaw);
          bool GetContext(string key)
          {
            if (!menu.TryGetValue(key, out object v))
              return true;
            if (v is bool b)
              return b;
            return string.Equals(v?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
          }

          _configRxInstMan.ContextMenu.ChangeProject = GetContext("changeProject");
          _configRxInstMan.ContextMenu.CreateProject = GetContext("createProject");
          _configRxInstMan.ContextMenu.CloneProject = GetContext("cloneProject");
          _configRxInstMan.ContextMenu.UpdateConfig = GetContext("updateConfig");
          _configRxInstMan.ContextMenu.CheckServices = GetContext("checkServices");
          _configRxInstMan.ContextMenu.RunDDSWithOutDeploy = GetContext("runDDSWithOutDeploy");
          _configRxInstMan.ContextMenu.InfoContext = GetContext("infoContext");
          _configRxInstMan.ContextMenu.CmdAdminContext = GetContext("cmdAdminContext");
          _configRxInstMan.ContextMenu.ClearLogContext = GetContext("clearLogContext");
          _configRxInstMan.ContextMenu.ClearLogAllInstancesContext = GetContext("clearLogAllInstancesContext");
          _configRxInstMan.ContextMenu.ConfigContext = GetContext("configContext");
          _configRxInstMan.ContextMenu.ProjectConfigContext = GetContext("projectConfigContext");
          _configRxInstMan.ContextMenu.ConvertDBsContext = GetContext("convertDBsContext");
          _configRxInstMan.ContextMenu.RemoveProjectDataContext = GetContext("removeProjectDataContext");
          _configRxInstMan.ContextMenu.RemoveInstance = GetContext("removeInstance");
          _configRxInstMan.ContextMenu.OpenRXFolder = GetContext("openRXFolder");
          _configRxInstMan.ContextMenu.OpenLogFolder = GetContext("openLogFolder");

        }
        catch (Exception ex)
        {
          AppHandlers.logger.Error(ex.Message);
          throw;
        }
      }
    }

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
      ApplyInstancesGridHeight();
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
      ApplyInstancesGridHeight();
    }

#endregion

#region Работа с визуальными эффектами.

    private void ActionButtonVisibleChanging(string status = null, Instance instance = null)
    {
      if (instance == null)
        instance = _instance;

      ButtonDDSStart.IsEnabled = true;
      ButtonCDSStart.IsEnabled = true;
      ButtonStop.IsEnabled = true;
      ButtonStart.IsEnabled = true;
      ChangeProject.IsEnabled = true;
      CreateProject.IsEnabled = true;
      CloneProject.IsEnabled = true;
      UpdateConfig.IsEnabled = true;
      RunDDSWithOutDeploy.IsEnabled = true;
      ConvertDBsContext.IsEnabled = true;


      ButtonStart.Visibility = Visibility.Collapsed;
      ButtonStop.Visibility = Visibility.Collapsed;
      ButtonDDSStart.Visibility = Visibility.Collapsed;
      ButtonCDSStart.Visibility = Visibility.Collapsed;
      ButtonRXStart.Visibility = Visibility.Collapsed;
      ButtonLogViewer.Visibility = Visibility.Collapsed;

      var IsVisibleContextButton = instance == null || string.IsNullOrEmpty(instance.Code) ? Visibility.Collapsed : Visibility.Visible;

      Func<bool, Visibility> isVisibleContextButton = (need_show) => !need_show || instance == null || string.IsNullOrEmpty(instance.Code) ? Visibility.Collapsed : Visibility.Visible;
      ChangeProject.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.ChangeProject);
      CreateProject.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.CreateProject);
      CloneProject.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.CloneProject);
      UpdateConfig.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.UpdateConfig);
      CheckServices.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.CheckServices);
      RunDDSWithOutDeploy.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.RunDDSWithOutDeploy);
      InfoContext.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.InfoContext);
      CmdAdminContext.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.CmdAdminContext);
      ClearLogContext.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.ClearLogContext);
      ClearLogAllInstancesContext.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.ClearLogAllInstancesContext);
      ConfigContext.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.ConfigContext);
      ProjectConfigContext.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.ProjectConfigContext);
      ConvertDBsContext.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.ConvertDBsContext);
      RemoveProjectDataContext.Visibility = Visibility.Collapsed; // TODO: отключена, т.к. работает ненадежно isVisibleContextButton(_configRxInstMan.ContextMenu.RemoveProjectDataContext);
      OpenRXFolder.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.OpenRXFolder);
      OpenLogFolder.Visibility = isVisibleContextButton(_configRxInstMan.ContextMenu.OpenLogFolder);

      status = instance == null || string.IsNullOrEmpty(instance.Code) ? status : instance.Status;
        
      switch (status)
      {
        case Constants.InstanceStatus.Stopped:
          ButtonDDSStart.Visibility = Visibility.Visible;
          ButtonCDSStart.Visibility = Visibility.Visible;
          ButtonRXStart.Visibility = Visibility.Collapsed;
          ButtonStop.Visibility = Visibility.Collapsed;
          ButtonStart.Visibility = Visibility.Visible;
          if (_configRxInstMan.LogViewerExists)
            ButtonLogViewer.Visibility = Visibility.Visible;
          else
            ButtonLogViewer.Visibility = Visibility.Collapsed;
          break;
        case Constants.InstanceStatus.Working:
          ButtonDDSStart.Visibility = Visibility.Visible;
          ButtonCDSStart.Visibility = Visibility.Visible;
          ButtonRXStart.Visibility = Visibility.Visible;
          ButtonStop.Visibility = Visibility.Visible;
          ButtonStart.Visibility = Visibility.Collapsed;
          if (_configRxInstMan.LogViewerExists)
            ButtonLogViewer.Visibility = Visibility.Visible;
          else
            ButtonLogViewer.Visibility = Visibility.Collapsed;
          break;
        case Constants.InstanceStatus.Update:
          ButtonDDSStart.Visibility = Visibility.Visible;
          ButtonDDSStart.IsEnabled = false;
          ButtonCDSStart.Visibility = Visibility.Visible;
          ButtonCDSStart.IsEnabled = false;
          ButtonRXStart.Visibility = Visibility.Visible;
          ButtonStop.Visibility = Visibility.Visible;
          ButtonStop.IsEnabled = false;
          ButtonStart.Visibility = Visibility.Collapsed;
          if (_configRxInstMan.LogViewerExists)
            ButtonLogViewer.Visibility = Visibility.Visible;
          else
            ButtonLogViewer.Visibility = Visibility.Collapsed;

          ButtonStart.IsEnabled = false;
          ChangeProject.IsEnabled = false;
          CreateProject.IsEnabled = false;
          CloneProject.IsEnabled = false;
          UpdateConfig.IsEnabled = false;
          RunDDSWithOutDeploy.IsEnabled = false;
          ConvertDBsContext.IsEnabled = false;
          break;
      }
      SetVisibleDevStudiosButtons(instance);
    }

    private void SetVisibleDevStudiosButtons(Instance instance)
    {
      if (instance == null)
      {
        ButtonDDSStart.Visibility = Visibility.Collapsed;
        RunDDSWithOutDeploy.Visibility = Visibility.Collapsed;
        ButtonCDSStart.Visibility = Visibility.Collapsed;
        
        return;
      }

      var ddsPath = RXInstanceManager.AppHelper.GetDDSPath(instance.InstancePath);
      var cdsPath = RXInstanceManager.AppHelper.GetCDSPath(instance.InstancePath, instance.Code);
      
      if (!File.Exists(ddsPath))
      {
        ButtonDDSStart.Visibility = Visibility.Collapsed;
        RunDDSWithOutDeploy.Visibility = Visibility.Collapsed;
      }
      
      if (!File.Exists(cdsPath))
        ButtonCDSStart.Visibility = Visibility.Collapsed;
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
