using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Windows.Forms;



namespace RXInstanceManager
{

  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    internal static Instance _instance;
    public static List<Instance> instances2 = new List<Instance>();

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
      if (WindowState == WindowState.Minimized)
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
      LoadInstances();
      ActionButtonVisibleChanging();
      StartAsyncHandlers();
      m_notifyIcon = new System.Windows.Forms.NotifyIcon();
      m_notifyIcon.BalloonTipText = "The app has been minimised. Click the tray icon to show.";
      m_notifyIcon.BalloonTipTitle = "RXInstanceManager";
      m_notifyIcon.Text = "RXInstanceManager";
      m_notifyIcon.Icon = new System.Drawing.Icon("App.ico");
      m_notifyIcon.Click += new EventHandler(m_notifyIcon_Click);
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

    private void ButtonStart_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      if (_instance == null)
        return;

      try
      {
        var serviceStatus = AppHandlers.GetServiceStatus(_instance);
        if (serviceStatus == Constants.InstanceStatus.Stopped)
          AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), "all up", true, true);
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    private void ButtonStop_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      if (_instance == null || _instance.Status != Constants.InstanceStatus.Working)
        return;

      try
      {
        var serviceStatus = AppHandlers.GetServiceStatus(_instance);
        if (serviceStatus == Constants.InstanceStatus.Working)
          AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), "all down", true, true);
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
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
        if (string.IsNullOrEmpty(_instance.StoragePath))
        {
          System.Windows.MessageBox.Show("Не указана папка исходников");
          return;
        }
        AppHandlers.LaunchProcess(AppHelper.GetDDSPath(_instance.InstancePath), true);
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

    private void ButtonInstruction_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);

      try
      {
        if (File.Exists("readme.txt"))
          Dialogs.ShowFileContentDialog("readme.txt");
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

    private void ChangeProject_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", _instance.Code);
        if (!string.IsNullOrEmpty(_instance.ProjectConfigPath))
          openFileDialog.InitialDirectory = Path.GetDirectoryName(_instance.ProjectConfigPath);
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          var config_filename = openFileDialog.FileName;
          try
          {
            AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), string.Format("map set {0} -rundds=False -need_pause", config_filename), true, true);
          }
          catch (Exception ex)
          {
            AppHandlers.ErrorHandler(_instance, ex);
          }

        }
      }
    }

    private void CreateProject_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", _instance.Code);
        openFileDialog.InitialDirectory = string.IsNullOrEmpty(_instance.ProjectConfigPath) ? "C:\\" : Path.GetDirectoryName(_instance.ProjectConfigPath);
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          var config_filename = openFileDialog.FileName;
          try
          {
            AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), string.Format("map create_project {0} -rundds=False -need_pause", config_filename), true, true);
          }
          catch (Exception ex)
          {
            AppHandlers.ErrorHandler(_instance, ex);
          }

        }
      }
    }

    private void RunDDSWithOutDeploy_Click(object sender, RoutedEventArgs e)
    {
      AppHandlers.InfoHandler(_instance, MethodBase.GetCurrentMethod().Name);
      using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
      {
        var filter = string.Format("configs for {0}|{0}_*.yml;{0}_*.yaml|YAML-файлы|*.yml;*.yaml|All files (*.*)|*.*", _instance.Code);
        openFileDialog.InitialDirectory = string.IsNullOrEmpty(_instance.ProjectConfigPath) ? "C:\\" : Path.GetDirectoryName(_instance.ProjectConfigPath);
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
        openFileDialog.InitialDirectory = string.IsNullOrEmpty(_instance.ProjectConfigPath) ? "C:\\" : Path.GetDirectoryName(_instance.ProjectConfigPath);
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
        AppHandlers.ExecuteCmdCommand($"cd /d {_instance.InstancePath}", true);
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

      Dialogs.ShowInformationDialog(_instance.ToString());
    }

    #endregion

    private void StartAsyncHandlers()
    {
#pragma warning disable CS4014
      // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
      UpdateInstanceGridAsync();
      UpdateInstanceDataAsync();
#pragma warning restore CS4014
    }

    private void HiddenButton_Click(object sender, RoutedEventArgs e)
    {

      if (_instance == null)
        return;

      try
      {
        AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath), "map current -need_pause", true, true);
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(_instance, ex);
      }
    }

    private void ButtonLogsFolder_Click(object sender, RoutedEventArgs e)
    {
      var configYamlPath = AppHelper.GetConfigYamlPath(_instance.InstancePath);
      var yamlValues = YamlSimple.Parser.ParseFile(configYamlPath);
      var instance_name = yamlValues.GetConfigStringValue("variables.instance_name");
      var logs_folder = yamlValues.GetConfigStringValue("logs_path.LOGS_PATH");
      if (logs_folder.Contains("{{ instance_name }}"))
      {
        logs_folder = logs_folder.Replace("{{ instance_name }}", instance_name);
      }
      AppHandlers.LaunchProcess(logs_folder);
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
        openFileDialog.InitialDirectory = string.IsNullOrEmpty(_instance.ProjectConfigPath) ? "C:\\" : Path.GetDirectoryName(_instance.ProjectConfigPath);
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

    private void CloneProject_Click(object sender, RoutedEventArgs e)
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
          var config_filename = openFileDialog.FileName;
          try
          {
            AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath),
                                      string.Format("map clone_project {0} {1} -rundds=False -need_pause", _instance.ProjectConfigPath, config_filename),
                                      true, true);
          }
          catch (Exception ex)
          {
            AppHandlers.ErrorHandler(_instance, ex);
          }

        }
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
          var configYamlPath = AppHelper.GetConfigYamlPath(openFileDialog.FileName);
          var yamlValues = YamlSimple.Parser.ParseFile(configYamlPath);
          var home_path = yamlValues.GetConfigStringValue("variables.home_path");
          Directory.Delete(home_path, true);
          AppHandlers.LaunchProcess(AppHelper.GetDoPath(_instance.InstancePath),
                                    string.Format("map set {0} -rundds=False -need_pause", currentProjectConfig),
                                    true, true);
        }
      }
    }
  }
}
