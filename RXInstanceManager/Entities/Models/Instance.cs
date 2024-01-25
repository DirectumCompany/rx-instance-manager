using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Dynamic;
using YamlDotNet.Serialization;

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
          using (var reader = new StreamReader(configYamlPath))
          {
            var deserializer = new DeserializerBuilder().Build();
            dynamic ymlData = deserializer.Deserialize<ExpandoObject>(reader.ReadToEnd());

            var instanceCode = ymlData.variables["instance_name"];
            var protocol = ymlData.variables["protocol"];
            var host = ymlData.variables["host_fqdn"];
            var connection = ymlData.common_config["CONNECTION_STRING"];
            var dbEngine = ymlData.common_config["DATABASE_ENGINE"];
            var dbName = AppHelper.GetDBNameFromConnectionString(dbEngine, connection);
            if (dbName == "{{ database }}")
              dbName = ymlData.variables["database"];
            this.DBName = dbName ?? string.Empty;

            this.Code = instanceCode;
            this.ServiceName = $"{Constants.Service}_{instanceCode}";
            this.DBEngine = dbEngine;
            this.Name = ymlData.variables["purpose"];
            this.ProjectConfigPath = ymlData.variables["project_config_path"];
            this.Port = Convert.ToInt32(ymlData.variables["http_port"]);
            this.URL = AppHelper.GetClientURL(protocol, host, this.Port);
            this.StoragePath = ymlData.variables["home_path"];
            this.LogFolder = ymlData.logs_path["LOGS_PATH"];
            if (this.LogFolder.Contains("{{ instance_name }}"))
            {
              this.LogFolder = this.LogFolder.Replace("{{ instance_name }}", instanceCode);
            }

            this.SourcesPath = ymlData.services_config["DevelopmentStudio"]["GIT_ROOT_DIRECTORY"];
            if (this.SourcesPath == "{{ home_path_src }}")
              this.SourcesPath = ymlData.variables["home_path_src"];

            var repositories = ymlData.services_config["DevelopmentStudio"]["REPOSITORIES"]["repository"];
            this.WorkingRepositoryName = String.Empty;
            foreach (var repository in repositories)
            {
              if (repository["@solutionType"] == "Work")
              {
                if (String.IsNullOrEmpty(this.WorkingRepositoryName))
                  this.WorkingRepositoryName = System.IO.Path.Combine(this.SourcesPath, repository["@folderName"]);
                else
                {
                  this.WorkingRepositoryName = this.SourcesPath;
                  break;
                }
              }
            }
          }
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

    public string WorkingRepositoryName { get; set; }

    public string LogFolder { get; set; }

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
