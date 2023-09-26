using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Threading.Tasks;

namespace RXInstanceManager
{
  public partial class MainWindow : Window
  {
    private async Task UpdateInstanceGridAsync()
    {
      while (true)
      {
        await Task.Delay(TimeSpan.FromSeconds(2));

        foreach (var instance in GridInstances.Items.Cast<Instance>())
        {
          if (!string.IsNullOrEmpty(instance.Code))
          {
            var status = AppHandlers.GetServiceStatus(instance);
            if (status != instance.Status)
              LoadInstances(_instance.InstancePath);
          }
        }
      }
    }

    private async Task UpdateInstanceDataAsync()
    {
      while (true)
      {
        await Task.Delay(TimeSpan.FromSeconds(2));

        foreach (var instance in GridInstances.Items.Cast<Instance>())
        {
          var idx = Instances.instances.FindIndex(i => i.InstancePath == instance.InstancePath);
          var inst = Instances.instances[idx];
          var configYamlPath = AppHelper.GetConfigYamlPath(inst.InstancePath);
          if (File.Exists(configYamlPath))
          {
            var changeTime = AppHelper.GetFileChangeTime(configYamlPath);
            if (instance.ConfigChanged == null || changeTime.MoreThanUpToSeconds(inst.ConfigChanged))
            {
              var yamlValues = YamlSimple.Parser.ParseFile(configYamlPath);

              var protocol = yamlValues.GetConfigStringValue("variables.protocol");
              var host = yamlValues.GetConfigStringValue("variables.host_fqdn");

              instance.DBEngine = yamlValues.GetConfigStringValue("common_config.DATABASE_ENGINE");
              var connection = yamlValues.GetConfigStringValue("common_config.CONNECTION_STRING");
              instance.ServerDB = AppHelper.GetServerFromConnectionString(instance.DBEngine, connection);
              var dbName = AppHelper.GetDBNameFromConnectionString(instance.DBEngine, connection);
              if (dbName == "{{ database }}")
                dbName = yamlValues.GetConfigStringValue("variables.database");
              instance.DBName = dbName ?? string.Empty;

              instance.Name = yamlValues.GetConfigStringValue("variables.purpose");
              instance.ProjectConfigPath = yamlValues.GetConfigStringValue("variables.project_config_path");
              instance.Port = yamlValues.GetConfigIntValue("variables.http_port") ?? 0;
              instance.URL = AppHelper.GetClientURL(protocol, host, instance.Port);
              instance.StoragePath = yamlValues.GetConfigStringValue("variables.home_path");
              if (instance.StoragePath == "{{ home_path_src }}")
                instance.StoragePath = yamlValues.GetConfigStringValue("variables.home_path_src");
              instance.SourcesPath = yamlValues.GetConfigStringValue("services_config.DevelopmentStudio.GIT_ROOT_DIRECTORY");
              instance.PlatformVersion = AppHandlers.GetInstancePlatformVersion(instance.InstancePath);
              instance.SolutionVersion = AppHandlers.GetInstanceSolutionVersion(instance.InstancePath);

              instance.Status = AppHandlers.GetServiceStatus(instance);
              instance.ConfigChanged = changeTime;

              LoadInstances(_instance.InstancePath);
            }
          }
          else
          {
            instance.DBEngine = string.Empty;
            instance.ServerDB = string.Empty;
            instance.DBName = string.Empty;
            instance.Name = string.Empty;
            instance.ProjectConfigPath = string.Empty;
            instance.Port = 0;
            instance.URL = string.Empty;
            instance.StoragePath = string.Empty;
            instance.SourcesPath = string.Empty;
            instance.PlatformVersion = string.Empty;
            instance.SolutionVersion = string.Empty;
            instance.Status = Constants.InstanceStatus.NotInstalled;
            
          }

        }
      }
    }
  }
}
