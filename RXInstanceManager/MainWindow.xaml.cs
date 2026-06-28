using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Reflection;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.Win32;
using NLog;

namespace RXInstanceManager
{

  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    internal static Instance _instance;
    public static List<Instance> instances2 = new List<Instance>();
    internal static Config _configRxInstMan;

    #region WPFToTray
    // https://possemeeg.wordpress.com/2007/09/06/minimize-to-tray-icon-in-wpf/
    private System.Windows.Forms.NotifyIcon m_notifyIcon;

    void OnClose(object sender, System.ComponentModel.CancelEventArgs args)
    {
      m_notifyIcon.Dispose();
      m_notifyIcon = null;
    }

    private WindowState m_storedWindowState = WindowState.Normal;
    void OnStateChanged(object sender, EventArgs args)
    {
      if (WindowState == WindowState.Minimized && !Properties.Settings.Default.DisableMinimizeToTray)
      {
        Hide();
        if (m_notifyIcon != null)
          m_notifyIcon.ShowBalloonTip(2000);
      }
      else
        m_storedWindowState = WindowState;
    }
    void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
      CheckTrayIcon();
    }

    void m_notifyIcon_Click(object sender, EventArgs e)
    {
      Show();
      WindowState = m_storedWindowState;
    }
    void CheckTrayIcon()
    {
      ShowTrayIcon(!IsVisible);
    }

    void ShowTrayIcon(bool show)
    {
      if (m_notifyIcon != null)
        m_notifyIcon.Visible = show;
    }
    #endregion

    public MainWindow()
    {
      InitializeComponent();
      if (!Directory.Exists(Constants.LogPath))
        Directory.CreateDirectory(Constants.LogPath);
      Instances.Create();
      LoadConfig();
      LoadInstances();
      ActionButtonVisibleChanging();
      m_notifyIcon = new System.Windows.Forms.NotifyIcon();
      m_notifyIcon.BalloonTipText = "The app has been minimised. Click the tray icon to show.";
      m_notifyIcon.BalloonTipTitle = "RXInstanceManager";
      m_notifyIcon.Text = "RXInstanceManager";
      m_notifyIcon.Icon = new System.Drawing.Icon("App.ico");
      m_notifyIcon.Click += new EventHandler(m_notifyIcon_Click);
      TrayStatus();
      Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      ApplyPreferredWindowWidth();
      ApplyInstancesGridHeight();
      Dispatcher.BeginInvoke(new Action(ApplyInstancesGridHeight), DispatcherPriority.Loaded);
      StartAsyncHandlers();
    }

    /// <summary>
    /// Set window width to fit DataGrid columns without horizontal scroll, capped at 90% of primary screen width (DIP).
    /// </summary>
    private void ApplyPreferredWindowWidth()
    {
      double columnsTotal = 0;
      foreach (DataGridColumn col in GridInstances.Columns)
      {
        if (col.Width.IsAbsolute)
          columnsTotal += col.Width.Value;
      }

      const double borderHorizontalMargins = 32;
      double verticalScrollReserve = SystemParameters.VerticalScrollBarWidth;
      const double chromeFudge = 40;
      double needed = columnsTotal + borderHorizontalMargins + verticalScrollReserve + chromeFudge;

      double maxAllowed = SystemParameters.PrimaryScreenWidth * 0.9;
      double minW = MinWidth > 0 ? MinWidth : 960;
      Width = Math.Max(minW, Math.Min(needed, maxAllowed));
    }

    /// <summary>
    /// Binds DataGrid viewport height to the card: explicit Height so the internal ScrollViewer gets a finite
    /// extent (Stretch + only MaxHeight often yields no vertical scroll in WPF).
    /// </summary>
    private void ApplyInstancesGridHeight()
    {
      GridInstances.UpdateLayout();
      double cardH = InstancesCardBorder.ActualHeight;
      if (cardH > 1)
        UpdateInstancesGridHeightForCard(cardH);
    }

    private void InstancesCardBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (e.NewSize.Height > 1)
        UpdateInstancesGridHeightForCard(e.NewSize.Height);
    }

    private void UpdateInstancesGridHeightForCard(double cardHeight)
    {
      GridInstances.ClearValue(FrameworkElement.MinHeightProperty);
      GridInstances.ClearValue(FrameworkElement.MaxHeightProperty);
      GridInstances.Height = cardHeight;
    }

    private void GridInstances_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var instance = GridInstances.SelectedItem as Instance;
      if (instance != null)
      {
        _instance = instance;
        ActionButtonVisibleChanging(instance.Status);
      }
    }

    #region ActionHandlers

    private async void ButtonStart_ClickAsync(object sender, RoutedEventArgs e)
    {
      var instance = _instance;
      AppHandlers.InfoHandler(instance, MethodBase.GetCurrentMethod().Name);

      if (instance == null)
        return;

      try
      {
        var serviceStatus = AppHandlers.GetServiceStatus(instance);
        if (serviceStatus == Constants.InstanceStatus.Stopped)
        {
          ChangeGridStatus(instance, Constants.InstanceStatus.Update);
          await Task.Run(() => AppHandlers.LaunchProcess(AppHelper.GetDoPath(instance.InstancePath), "all up", true, true));
          ChangeGridStatus(instance, Constants.InstanceStatus.Working);
        }
      }
      catch (Exception ex)
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Stopped);
        AppHandlers.ErrorHandler(instance, ex);
      }
    }

    private async void ButtonStop_ClickAsync(object sender, RoutedEventArgs e)
    {
      var instance = _instance;
      AppHandlers.InfoHandler(instance, MethodBase.GetCurrentMethod().Name);

      if (instance == null || instance.Status != Constants.InstanceStatus.Working)
        return;

      try
      {
        var serviceStatus = AppHandlers.GetServiceStatus(instance);
        if (serviceStatus == Constants.InstanceStatus.Working)
        {
          ChangeGridStatus(instance, Constants.InstanceStatus.Update);
          await Task.Run(() => AppHandlers.LaunchProcess(AppHelper.GetDoPath(instance.InstancePath), "all down", true, true));
          ChangeGridStatus(instance, Constants.InstanceStatus.Stopped);
        }
      }
      catch (Exception ex)
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Stopped);
        AppHandlers.ErrorHandler(instance, ex);
      }
    }

    private void ButtonAdd_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      using (var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog())
      {
        openFolderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
        DialogResult result = openFolderDialog.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(openFolderDialog.SelectedPath))
        {
          Instances.Add(openFolderDialog.SelectedPath);
          LoadInstances(openFolderDialog.SelectedPath);
        }
      }
    }

    private void ButtonDDSStart_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      try
      {
        AppHandlers.LaunchProcess(AppHelper.GetDDSPath(_instance.InstancePath), true);
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    private void ButtonCDSStart_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (string.IsNullOrEmpty(_instance.StoragePath))
        {
          System.Windows.MessageBox.Show("Не указана папка исходников");
          return;
        }
        AppHandlers.LaunchProcess(AppHelper.GetCDSPath(_instance.InstancePath, _instance.Code), true);
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    private void ButtonRXStart_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      try
      {
        if (!string.IsNullOrEmpty(_instance.URL))
          AppHandlers.LaunchProcess(_instance.URL);
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    #endregion


    #region ContextHandlers

    private void ConfigContext_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      try
      {
        var configYamlPath = AppHelper.GetConfigYamlPath(_instance.InstancePath);
        if (File.Exists(configYamlPath))
          AppHandlers.LaunchProcess(AppHelper.GetConfigYamlPath(_instance.InstancePath));
        else
          System.Windows.MessageBox.Show("Конфигурационный файл не найден");
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    private void ProjectConfigContext_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      try
      {
        var configYamlPath = _instance.ProjectConfigPath;
        if (File.Exists(configYamlPath))
          AppHandlers.LaunchProcess(configYamlPath);
        else
          System.Windows.MessageBox.Show(string.Format("Конфигурационный файл не найден {0}", configYamlPath));
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    private void InstancesContextMenu_Opened(object sender, RoutedEventArgs e)
    {
      UpdateCopyContextMenu();
      UpdateChangeProjectMenu();
      UpdateCreateProjectMenu();
      UpdateCloneProjectMenu();
    }

    private void UpdateCopyContextMenu()
    {
      CopyDbNameContext.Header = FormatCopyMenuHeader("БД", _instance?.DBName);
      CopyUrlContext.Header = FormatCopyMenuHeader("WebClient", _instance?.URL);
      CopyIntegrationUrlContext.Header = FormatCopyMenuHeader("Integration", GetIntegrationUrl(_instance));
      CopyPublicApiContext.Header = FormatCopyMenuHeader("PublicAPI", GetPublicApiUrl(_instance));
      CopyVersionContext.Header = FormatCopyMenuHeader("RX", _instance?.SolutionVersion);
      CopyPlatformVersionContext.Header = FormatCopyMenuHeader("Sungero", _instance?.PlatformVersion);
    }

    private static string FormatCopyMenuHeader(string label, string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return $"{label}: (пусто)";

      return $"{label}: {value}";
    }

    private static string GetIntegrationUrl(Instance instance)
    {
      if (instance == null || string.IsNullOrWhiteSpace(instance.URL))
        return string.Empty;

      if (Uri.TryCreate(instance.URL, UriKind.Absolute, out Uri webClientUri))
        return new Uri(webClientUri, "/Integration/odata").ToString();

      return instance.URL.Replace("/Client", "/Integration/odata");
    }

    private static string GetPublicApiUrl(Instance instance)
    {
      if (instance == null || string.IsNullOrWhiteSpace(instance.URL))
        return string.Empty;

      if (Uri.TryCreate(instance.URL, UriKind.Absolute, out Uri webClientUri))
        return new Uri(webClientUri, "/Client/api/public").ToString();

      return instance.URL.Replace("/Client", "/Client/api/public");
    }

    private void UpdateChangeProjectMenu()
    {
      ChangeProject.Click -= ChangeProjectFromFile_ClickAsync;
      ChangeProject.Items.Clear();

      var instance = _instance;
      if (instance == null || string.IsNullOrEmpty(instance.Code))
        return;

      if (!AppHelper.IsPlatformVersionGreaterThan26_1(instance.PlatformVersion))
      {
        ChangeProject.Click += ChangeProjectFromFile_ClickAsync;
        return;
      }

      var configurationNames = AppHelper.GetDesktopConfigurationNames(instance.InstancePath);
      if (configurationNames.Count == 0)
      {
        ChangeProject.Items.Add(new MenuItem { Header = "(нет конфигураций)", IsEnabled = false });
        return;
      }

      foreach (var name in configurationNames)
      {
        var item = new MenuItem { Header = name, Tag = name };
        item.Click += ChangeProjectConfiguration_ClickAsync;
        ChangeProject.Items.Add(item);
      }
    }

    private async void ChangeProjectConfiguration_ClickAsync(object sender, RoutedEventArgs e)
    {
      var configurationName = ((MenuItem)sender).Tag as string;
      if (string.IsNullOrWhiteSpace(configurationName))
        return;

      await RunChangeProjectFromConfigurationAsync(_instance, configurationName);
    }

    private async void ChangeProjectFromFile_ClickAsync(object sender, RoutedEventArgs e)
    {
      var instance = _instance;
      AppHandlers.InfoHandler(instance, MethodBase.GetCurrentMethod().Name);
      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", instance.Code);
        if (!string.IsNullOrEmpty(instance.ProjectConfigPath))
          openFileDialog.InitialDirectory = Path.GetDirectoryName(instance.ProjectConfigPath);
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
          await RunChangeProjectFromFileAsync(instance, openFileDialog.FileName);
      }
    }

    private async Task RunChangeProjectFromConfigurationAsync(Instance instance, string configurationName)
    {
      AppHandlers.InfoHandler(instance, MethodBase.GetCurrentMethod().Name);

      string needCheck = _configRxInstMan.NeedCheckAfterSet ? "True" : "False";
      var lastStatus = instance.Status;

      try
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Update);
        //await Task.Run(() => AppHandlers.LaunchProcess(AppHelper.GetDoPath(instance.InstancePath),
         /// string.Format("map set_ds \"'{0}'\" -need_pause -need_check={1}", configurationName, needCheck),
          //true,
          //true));

        await Task.Run(() => AppHandlers.LaunchProcess("cmd",
                                                  string.Format("cmd /C {1} map set_ds  \"'{0}'\" -need_pause -need_check={2}",
                                                  configurationName, AppHelper.GetDoPath(instance.InstancePath), needCheck),
                                                  true, true));

        ChangeGridStatus(instance, lastStatus);
      }
      catch (Exception ex)
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Stopped);
        AppHandlers.ErrorHandler(instance, ex);
      }
    }

    private async Task RunChangeProjectFromFileAsync(Instance instance, string configFilePath)
    {
      string needCheck = _configRxInstMan.NeedCheckAfterSet ? "True" : "False";
      var lastStatus = instance.Status;

      try
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Update);
        await Task.Run(() => AppHandlers.LaunchProcess(AppHelper.GetDoPath(instance.InstancePath),
          string.Format("map set {0} -rundds=False -need_pause -need_check={1}", configFilePath, needCheck),
          true,
          true));
        ChangeGridStatus(instance, lastStatus);
      }
      catch (Exception ex)
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Stopped);
        AppHandlers.ErrorHandler(instance, ex);
      }
    }

    private void UpdateCreateProjectMenu()
    {
      CreateProject.Click -= CreateProjectFromFile_ClickAsync;
      CreateProject.Items.Clear();

      var instance = _instance;
      if (instance == null || string.IsNullOrEmpty(instance.Code))
        return;

      if (!AppHelper.IsPlatformVersionGreaterThan26_1(instance.PlatformVersion))
      {
        CreateProject.Click += CreateProjectFromFile_ClickAsync;
        return;
      }

      var configurationNames = AppHelper.GetDesktopConfigurationNames(instance.InstancePath);
      if (configurationNames.Count == 0)
      {
        CreateProject.Items.Add(new MenuItem { Header = "(нет конфигураций)", IsEnabled = false });
        return;
      }

      foreach (var name in configurationNames)
      {
        var item = new MenuItem { Header = name, Tag = name };
        item.Click += CreateProjectConfiguration_ClickAsync;
        CreateProject.Items.Add(item);
      }
    }

    private async void CreateProjectConfiguration_ClickAsync(object sender, RoutedEventArgs e)
    {
      var configurationName = ((MenuItem)sender).Tag as string;
      if (string.IsNullOrWhiteSpace(configurationName))
        return;

      await RunCreateProjectFromConfigurationAsync(_instance, configurationName);
    }

    private async void CreateProjectFromFile_ClickAsync(object sender, RoutedEventArgs e)
    {
      var instance = _instance;
      AppHandlers.InfoHandler(instance, MethodBase.GetCurrentMethod().Name);

      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", instance.Code);
        openFileDialog.InitialDirectory = string.IsNullOrEmpty(instance.ProjectConfigPath) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : Path.GetDirectoryName(instance.ProjectConfigPath);
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
          return;

        await RunCreateProjectFromFileAsync(instance, openFileDialog.FileName);
      }
    }

    private async Task RunCreateProjectFromConfigurationAsync(Instance instance, string configurationName)
    {
      AppHandlers.InfoHandler(instance, MethodBase.GetCurrentMethod().Name);

      try
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Update);

        await Task.Run(() => AppHandlers.LaunchProcess("cmd",
                                                  string.Format("cmd /K {1} map create_project_ds  \"'{0}'\"",
                                                  configurationName, AppHelper.GetDoPath(instance.InstancePath)),
                                                  true, true));

        ChangeGridStatus(instance, Constants.InstanceStatus.Working);
      }
      catch (Exception ex)
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Stopped);
        AppHandlers.ErrorHandler(instance, ex);
      }
    }

    private async Task RunCreateProjectFromFileAsync(Instance instance, string configFilePath)
    {
      AppHandlers.InfoHandler(instance, MethodBase.GetCurrentMethod().Name);

      try
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Update);
        await Task.Run(() => AppHandlers.LaunchProcess("cmd",
                                                  string.Format("cmd /K {1} map create_project {0} -rundds=False -need_pause",
                                                  configFilePath, AppHelper.GetDoPath(instance.InstancePath)),
                                                  true, true));
        ChangeGridStatus(instance, Constants.InstanceStatus.Working);
      }
      catch (Exception ex)
      {
        ChangeGridStatus(instance, Constants.InstanceStatus.Stopped);
        AppHandlers.ErrorHandler(instance, ex);
      }
    }

    private void RunDDSWithOutDeploy_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", _instance.Code);
        openFileDialog.InitialDirectory = string.IsNullOrEmpty(_instance.ProjectConfigPath) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : Path.GetDirectoryName(_instance.ProjectConfigPath);
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          var config_filename = openFileDialog.FileName;
          try
          {
            AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), string.Format("map dds_wo_deploy {0} -need_pause", config_filename), true, false);
          }
          catch (Exception ex)
          {
            AppHandlers.ErrorHandler(_instance, ex);
          }

        }
      }
    }

    private void UpdateConfig_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", _instance.Code);
        openFileDialog.InitialDirectory = string.IsNullOrEmpty(_instance.ProjectConfigPath) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : Path.GetDirectoryName(_instance.ProjectConfigPath);
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          var config_filename = openFileDialog.FileName;
          try
          {

            AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), string.Format("map update_config {0} -rundds=False -need_pause", config_filename), true, true);
          }
          catch (Exception ex)
          {
            AppHandlers.ErrorHandler(_instance, ex);
          }
        }
      }
    }

    private void ClearLogContext_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      if (_instance == null)
        return;
      try
      {
        AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), string.Format("map clear_log"), true, true);
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    private void CmdAdminContext_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      if (_instance == null)
        return;

      try
      {
        Task.Run(() => AppHandlers.ExecuteCmdCommand($"cd /d {_instance.InstancePath}", true));
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    private void InfoContext_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      if (_instance == null)
        return;

      Dialogs.ShowInformation(_instance.ToString());
    }

    private void ConfigurationsContext_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      if (_instance == null)
        return;

      var configYamlPath = AppHelper.GetConfigYamlPath(_instance.InstancePath);
      if (!File.Exists(configYamlPath))
      {
        System.Windows.MessageBox.Show(
          "Файл config.yml не найден.",
          "Конфигурации",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
        return;
      }

      var configurations = AppHelper.GetDesktopConfigurations(_instance.InstancePath);
      var dialog = new ConfigurationsDialog(_instance.InstancePath, configurations, _configRxInstMan.EditConfigurations)
      {
        Owner = this,
      };
      dialog.ShowDialog();

      if (dialog.HasSavedChanges)
      {
        AppHandlers.UpdateInstanceData(_instance);
        LoadInstances(_instance.InstancePath);
      }
    }

    #endregion

    private void StartAsyncHandlers()
    {
      _ = UpdateInstanceGridAsync();
      _ = UpdateInstanceDataAsync();
    }

    private void HiddenButton_Click(object sender, RoutedEventArgs e)
    {

      if (_instance == null)
        return;

      try
      {
        Task.Run(() => AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), "map current -need_pause", true, true));
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    private void ButtonDisableMinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
      if (Properties.Settings.Default.DisableMinimizeToTray)
      {
        Properties.Settings.Default.DisableMinimizeToTray = false;
      }
      else
      {
        Properties.Settings.Default.DisableMinimizeToTray = true;
      }
      Properties.Settings.Default.Save();
      TrayStatus();
    }

    private void RunDirectumLogViewer_Click(object sender, RoutedEventArgs e)
    {
      if (Directory.Exists(_instance.LogFolder))
      {
        try
        {
          if (OperatingSystem.IsWindows())
          {
            using (var regKey = Registry.CurrentUser.OpenSubKey(@"Software\JsonLogViewerSettings", false))
            {
              if (regKey != null && (string)regKey.GetValue("LogsPath") != _instance.LogFolder)
                AppHandlers.ExecuteCmdCommands(true, false, "REG ADD HKCU\\Software\\JsonLogViewerSettings /v LogsPath /t REG_SZ /d \"" + _instance.LogFolder + "\" /f");
            }
          }
          AppHandlers.LaunchProcess(_configRxInstMan.LogViewer);
        }
        catch (Exception ex)
        {
          AppHandlers.ErrorHandler(_instance, ex);
        }
      }
      else
        System.Windows.MessageBox.Show($"Папка {_instance.LogFolder} не существует.", "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }

    private void ClearLogAllInstancesContext_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      foreach (var instance in Instances.instances)
      {
        try
        {
          AppHandlers.LaunchProcess(AppHelper.GetDoPath(instance.InstancePath), string.Format("map clear_log"), true, true);
        }
        catch (Exception ex)
        {
          AppHandlers.ErrorHandler(instance, ex);
        }
      }
    }

    private void ConvertDBsContext_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", _instance.Code);
        openFileDialog.InitialDirectory = string.IsNullOrEmpty(_instance.ProjectConfigPath) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : Path.GetDirectoryName(_instance.ProjectConfigPath);
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 1;
        openFileDialog.Multiselect = true;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          var currentProjectConfig = _instance.ProjectConfigPath;
          foreach (var config_filename in openFileDialog.FileNames)
          {
            try
            {
              var serviceStatus = AppHandlers.GetServiceStatus(_instance);
              if (serviceStatus == Constants.InstanceStatus.Working)
                AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), "all down", true, true);
              AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), string.Format("map update_config {0} --confirm=False -rundds=False", config_filename), true, true);
              AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), string.Format("do db convert"), true, true);
            }
            catch (Exception ex)
            {
              AppHandlers.ErrorHandler(_instance, ex);
            }
          }
          AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), string.Format("map set {0} -rundds=False -need_pause", currentProjectConfig), true, true);
        }
      }
    }

    private void RemoveInstance_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      try
      {
        var acceptResult = System.Windows.MessageBox.Show($"Подтвердите удаление инстанса из списка \"{_instance.InstancePath}\"",
                                           "Подтверждение удаления", MessageBoxButton.YesNo);
        if (acceptResult != MessageBoxResult.Yes)
          return;
        Instances.Delete(_instance);
        LoadInstances();
        _instance = GridInstances.SelectedItem as Instance;
        ActionButtonVisibleChanging();
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }

    }

    private void UpdateCloneProjectMenu()
    {
      CloneProject.Click -= CloneProjectFromFile_Click;
      CloneProject.Items.Clear();

      var instance = _instance;
      if (instance == null || string.IsNullOrEmpty(instance.Code))
        return;

      if (!AppHelper.IsPlatformVersionGreaterThan26_1(instance.PlatformVersion))
      {
        CloneProject.Click += CloneProjectFromFile_Click;
        return;
      }

      var activeConfiguration = AppHelper.GetActiveDesktopConfiguration(instance.InstancePath);
      if (string.IsNullOrWhiteSpace(activeConfiguration))
      {
        CloneProject.Items.Add(new MenuItem { Header = "(нет активной конфигурации)", IsEnabled = false });
        return;
      }

      var targetConfigurations = AppHelper.GetDesktopConfigurationNames(instance.InstancePath, activeConfiguration);
      if (targetConfigurations.Count == 0)
      {
        CloneProject.Items.Add(new MenuItem { Header = "(нет других конфигураций)", IsEnabled = false });
        return;
      }

      foreach (var name in targetConfigurations)
      {
        var item = new MenuItem { Header = name, Tag = name };
        item.Click += CloneProjectConfiguration_Click;
        CloneProject.Items.Add(item);
      }
    }

    private void CloneProjectConfiguration_Click(object sender, RoutedEventArgs e)
    {
      var targetConfiguration = ((MenuItem)sender).Tag as string;
      if (string.IsNullOrWhiteSpace(targetConfiguration))
        return;

      var instance = _instance;
      var currentConfiguration = AppHelper.GetActiveDesktopConfiguration(instance.InstancePath);
      if (string.IsNullOrWhiteSpace(currentConfiguration))
        return;

      RunCloneProjectFromConfiguration(instance, currentConfiguration, targetConfiguration);
    }

    private void CloneProjectFromFile_Click(object sender, RoutedEventArgs e)
    {
      var instance = _instance;
      AppHandlers.InfoHandler(instance, MethodBase.GetCurrentMethod().Name);
      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", instance.Code);
        openFileDialog.InitialDirectory = Path.GetDirectoryName(instance.ProjectConfigPath);
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          RunCloneProjectFromFile(instance, instance.ProjectConfigPath, openFileDialog.FileName);
        }
      }
    }

    private void RunCloneProjectFromConfiguration(Instance instance, string currentConfiguration, string targetConfiguration)
    {
      AppHandlers.InfoHandler(instance, MethodBase.GetCurrentMethod().Name);
      try
      {
        //AppHandlers.LaunchProcess(AppHelper.GetDoPath(instance.InstancePath),
         // string.Format("map clone_project_ds {0} {1}", currentConfiguration, targetConfiguration),
          //true, true);
        AppHandlers.LaunchProcess("cmd",
          string.Format("cmd /K {2} map clone_project_ds \"'{0}'\" \"'{1}'\" -need_pause",
            currentConfiguration, targetConfiguration, AppHelper.GetDoPath(instance.InstancePath)),
          true, true);
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(instance, ex);
      }
    }

    private void RunCloneProjectFromFile(Instance instance, string currentProjectConfig, string targetProjectConfig)
    {
      try
      {
        AppHandlers.LaunchProcess("cmd",
          string.Format("cmd /K {2} map clone_project {0} {1} -rundds=False -need_pause",
            currentProjectConfig, targetProjectConfig, AppHelper.GetDoPath(instance.InstancePath)),
          true, true);
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(instance, ex);
      }
    }

    private void RemoveProjectDataContext_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", _instance.Code);
        openFileDialog.InitialDirectory = Path.GetDirectoryName(_instance.ProjectConfigPath);
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          var currentProjectConfig = _instance.ProjectConfigPath;

          var serviceStatus = AppHandlers.GetServiceStatus(_instance);
          if (serviceStatus == Constants.InstanceStatus.Working)
            AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), "all down", true, true);
          AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath),
                                    string.Format("map update_config {0} --confirm=False -rundds=False", openFileDialog.FileName),
                                    true, true);
          AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath),
                                    string.Format("do db drop"),
                                    true, true);
          Directory.Delete(_instance.StoragePath, true);

          string needCheck = "False";
          if (_configRxInstMan.NeedCheckAfterSet)
            needCheck = "True";
          AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), string.Format("map set {0} -confirm=False -rundds=False -need_pause -need_check={1}",
                                    currentProjectConfig, needCheck), true, true);
        }
      }
    }

    private void OpenRXFolder_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.LaunchProcess(_instance.InstancePath);
    }

    private void TrayStatus()
    {
      if (Properties.Settings.Default.DisableMinimizeToTray)
      {
        ButtonDisableMinimizeToTray.Content = "Tray\nis off";
      }
      else
      {
        ButtonDisableMinimizeToTray.Content = "Tray\nis on";
      }
    }

    private void CheckServices_Click(object sender, RoutedEventArgs e)
    {

      if (_instance == null)
        return;

      try
      {

        Task.Run(() => AppHandlers.LaunchProcess("cmd",
                                  string.Format("cmd /K {0} all check",
                                  AppHelper.GetDoPath(_instance.InstancePath)),
                                  true, true));
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }

    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
      if (Directory.Exists(_instance.LogFolder))
        AppHandlers.LaunchProcess(_instance.LogFolder);
      else
        System.Windows.MessageBox.Show($"Папка {_instance.LogFolder} не существует.", "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }

    private void CopyDBNameContext_Click(object sender, RoutedEventArgs e)
    {
      if (_instance == null)
        return;

      System.Windows.Clipboard.SetText(_instance.DBName ?? string.Empty);
    }

    private void CopyUrlContext_Click(object sender, RoutedEventArgs e)
    {
      if (_instance == null)
        return;

      System.Windows.Clipboard.SetText(_instance.URL ?? string.Empty);
    }

    private void CopyIntegrationUrlContext_Click(object sender, RoutedEventArgs e)
    {
      if (_instance == null)
        return;

      System.Windows.Clipboard.SetText(GetIntegrationUrl(_instance));
    }

    private void CopyPublicApiContext_Click(object sender, RoutedEventArgs e)
    {
      if (_instance == null)
        return;

      System.Windows.Clipboard.SetText(GetPublicApiUrl(_instance));
    }

    private void CopyVersionContext_Click(object sender, RoutedEventArgs e)
    {
      if (_instance == null)
        return;

      System.Windows.Clipboard.SetText(_instance.SolutionVersion ?? string.Empty);
    }

    private void CopyPlatformVersionContext_Click(object sender, RoutedEventArgs e)
    {
      if (_instance == null)
        return;

      System.Windows.Clipboard.SetText(_instance.PlatformVersion ?? string.Empty);
    }

    private void ChangeGridStatus(Instance instance, string status)
    {
      instance.Status = status;
      ActionButtonVisibleChanging(instance: instance);
      LoadInstances(instance.InstancePath);
    }
  }
}
