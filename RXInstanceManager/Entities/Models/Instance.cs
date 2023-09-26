using System;
using System.Text;
using System.IO;

namespace RXInstanceManager
{

  public class Instance
  {
    public Instance()
    {

    }

    public Instance(string instancePath)
    {
      try
      {
        var configYamlPath = AppHelper.GetConfigYamlPath(instancePath);
        this.Status = Constants.InstanceStatus.NotInstalled;
        this.InstancePath = instancePath;
        if (File.Exists(configYamlPath))
        {
          //TODO сделать проверки при добавлении
          /*
          if (!File.Exists(configYamlPath))
          {
            System.Windows.MessageBox.Show(string.Format("Папка '{0}' папка не является папкой экземпляра DirectumRX (Не найден config.yml)", configYamlPath),
                                           "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
          }
          */
          var yamlValues = YamlSimple.Parser.ParseFile(configYamlPath);
          var instanceCode = yamlValues.GetConfigStringValue("variables.instance_name");
          /*
          if (string.IsNullOrEmpty(instanceCode))
          {
            System.Windows.MessageBox.Show(string.Format("В config.yml инстанса '{0}' не указана переменная instance_name", instancePath),
                                           "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
          }
          */
          var protocol = yamlValues.GetConfigStringValue("variables.protocol");
          var host = yamlValues.GetConfigStringValue("variables.host_fqdn");
          var connection = yamlValues.GetConfigStringValue("common_config.CONNECTION_STRING");
          var dbEngine = yamlValues.GetConfigStringValue("common_config.DATABASE_ENGINE");
          var dbName = AppHelper.GetDBNameFromConnectionString(dbEngine, connection);
          if (dbName == "{{ database }}")
            dbName = yamlValues.GetConfigStringValue("variables.database");

          this.Code = instanceCode;
          this.ServiceName = $"{Constants.Service}_{instanceCode}";
          this.DBEngine = dbEngine;
          this.ServerDB = AppHelper.GetServerFromConnectionString(this.DBEngine, connection);
          this.DBName = dbName ?? string.Empty;
          this.Name = yamlValues.GetConfigStringValue("variables.purpose");
          this.ProjectConfigPath = yamlValues.GetConfigStringValue("variables.project_config_path");
          this.Port = yamlValues.GetConfigIntValue("variables.http_port") ?? 0;
          this.URL = AppHelper.GetClientURL(protocol, host, this.Port);
          this.StoragePath = yamlValues.GetConfigStringValue("variables.home_path");
          if (this.StoragePath == "{{ home_path_src }}")
            this.StoragePath = yamlValues.GetConfigStringValue("variables.home_path_src");
          this.SourcesPath = yamlValues.GetConfigStringValue("services_config.DevelopmentStudio.GIT_ROOT_DIRECTORY");
          this.PlatformVersion = AppHandlers.GetInstancePlatformVersion(instancePath);
          this.SolutionVersion = AppHandlers.GetInstanceSolutionVersion(instancePath);
          this.Status = AppHandlers.GetServiceStatus(this);
          this.ConfigChanged = AppHelper.GetFileChangeTime(configYamlPath);
        }
      }
      catch (Exception ex)
      {
        AppHandlers.ErrorHandler(null, ex);
      }

    }

    public string Code { get; set; }

    public string PlatformVersion { get; set; }

    public string SolutionVersion { get; set; }

    public string Name { get; set; }

    public string ProjectConfigPath { get; set; }

    public int Port { get; set; }

    public string URL { get; set; }

    public string DBName { get; set; }

    public string DBEngine { get; set; }

    public string ServerDB { get; set; }

    public string ServiceName { get; set; }

    public string InstancePath { get; set; }

    public string StoragePath { get; set; }

    public string SourcesPath { get; set; }

    public string Status { get; set; }

    public DateTime ConfigChanged { get; set; }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine("Код:                " + Code ?? string.Empty);
      builder.AppendLine("URL:                " + URL ?? string.Empty);
      builder.AppendLine("Версия платформы:   " + PlatformVersion ?? string.Empty);
      builder.AppendLine("Версия решения:     " + SolutionVersion ?? string.Empty);
      builder.AppendLine("Путь до инстанса:   " + InstancePath ?? string.Empty);
      builder.AppendLine("Имя службы:         " + ServiceName ?? string.Empty);
      builder.AppendLine("SQL-движок:         " + DBEngine ?? string.Empty);
      builder.AppendLine("Сервер БД:          " + ServerDB ?? string.Empty);
      builder.AppendLine("");
      builder.AppendLine("");
      builder.AppendLine("Конфиг проекта: " + ProjectConfigPath ?? string.Empty);
      builder.AppendLine("");

      if (File.Exists(ProjectConfigPath))
      {
        var readText = File.ReadAllLines(ProjectConfigPath);
        foreach (var s in readText)
        {
          builder.AppendLine(s);
        }
      }
      return builder.ToString();
    }
  }
}
