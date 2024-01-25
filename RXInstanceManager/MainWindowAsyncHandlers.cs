using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using System.Dynamic;
using YamlDotNet.Serialization;

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
              using (var reader = new StreamReader(configYamlPath))
              {
                var deserializer = new DeserializerBuilder().Build();
                dynamic ymlData = deserializer.Deserialize<ExpandoObject>(reader.ReadToEnd());

                var protocol = ymlData.variables["protocol"];
                var host = ymlData.variables["host_fqdn"];
                var connection = ymlData.common_config["CONNECTION_STRING"];

                inst.DBEngine = ymlData.common_config["DATABASE_ENGINE"];
                inst.ServerDB = AppHelper.GetServerFromConnectionString(inst.DBEngine, connection);
                var dbName = AppHelper.GetDBNameFromConnectionString(inst.DBEngine, connection);
                if (dbName == "{{ database }}")
                  dbName = ymlData.variables["database"];
                inst.DBName = dbName ?? string.Empty;

                inst.Name = ymlData.variables["purpose"];
                inst.ProjectConfigPath = ymlData.variables["project_config_path"];

                inst.Port = Convert.ToInt32(ymlData.variables["http_port"]);
                inst.URL = AppHelper.GetClientURL(protocol, host, inst.Port);
                inst.StoragePath = ymlData.variables["home_path"];
                inst.LogFolder = ymlData.logs_path["LOGS_PATH"];
                if (inst.LogFolder.Contains("{{ instance_name }}"))
                {
                  inst.LogFolder = inst.LogFolder.Replace("{{ instance_name }}", inst.Code);
                }
                inst.SourcesPath = ymlData.services_config["DevelopmentStudio"]["GIT_ROOT_DIRECTORY"];
                if (inst.SourcesPath == "{{ home_path_src }}")
                  inst.SourcesPath = ymlData.variables["home_path_src"];
                instance.PlatformVersion = AppHandlers.GetInstancePlatformVersion(instance.InstancePath);
                instance.SolutionVersion = AppHandlers.GetInstanceSolutionVersion(instance.InstancePath);

                var repositories = ymlData.services_config["DevelopmentStudio"]["REPOSITORIES"]["repository"];
                instance.WorkingRepositoryName = String.Empty;
                foreach (var repository in repositories)
                {
                  if (repository["@solutionType"] == "Work")
                  {
                    if (String.IsNullOrEmpty(instance.WorkingRepositoryName))
                      instance.WorkingRepositoryName = System.IO.Path.Combine(instance.SourcesPath, repository["@folderName"]);
                    else
                    {
                      instance.WorkingRepositoryName = instance.SourcesPath;
                      break;
                    }
                  }
                }
                instance.Status = AppHandlers.GetServiceStatus(instance);
                instance.ConfigChanged = changeTime;

                LoadInstances(_instance.InstancePath);
              }
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
            instance.WorkingRepositoryName = string.Empty;
          }
        }
      }
    }
  }
}
