using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.ServiceProcess;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading.Tasks;
using NLog;
using System.Dynamic;
using YamlDotNet.Serialization;

namespace RXInstanceManager
{
  public static class AppHandlers
  {
    private static Logger logger = LogManager.GetCurrentClassLogger();


    #region Работа с конфигом.

    public static void UpdateInstanceData(Instance instance)
    {
      if (instance == null || string.IsNullOrEmpty(instance.InstancePath))
        return;

      var idx = Instances.instances.FindIndex(i => i.InstancePath == instance.InstancePath);
      var inst = Instances.instances[idx];

      var configFilePath = AppHelper.GetConfigYamlPath(instance.InstancePath);
      if (File.Exists(configFilePath))
      {
        using (var reader = new StreamReader(configFilePath))
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
          inst.PlatformVersion = GetInstancePlatformVersion(inst.InstancePath);
          inst.SolutionVersion = GetInstanceSolutionVersion(inst.InstancePath);

          var repositories = ymlData.services_config["DevelopmentStudio"]["REPOSITORIES"]["repository"];
          inst.WorkingRepositoryName = String.Empty;
          foreach (var repository in repositories)
          {
            if (repository["@solutionType"] == "Work")
            {
              if (String.IsNullOrEmpty(inst.WorkingRepositoryName))
                inst.WorkingRepositoryName = System.IO.Path.Combine(inst.SourcesPath, repository["@folderName"]);
              else
              {
                inst.WorkingRepositoryName = inst.SourcesPath;
                break;
              }
            }
          }
          inst.Status = AppHandlers.GetServiceStatus(inst);
          inst.ConfigChanged = AppHelper.GetFileChangeTime(configFilePath);
        }

      }
      else
      {
        inst.DBEngine = string.Empty;
        inst.ServerDB = string.Empty;
        inst.DBName = string.Empty;
        inst.Name = string.Empty;
        inst.ProjectConfigPath = string.Empty;
        inst.Port = 0;
        inst.URL = string.Empty;
        inst.StoragePath = string.Empty;
        inst.SourcesPath = string.Empty;
        inst.PlatformVersion = string.Empty;
        inst.SolutionVersion = string.Empty;
        inst.Status = Constants.InstanceStatus.NotInstalled;
        inst.WorkingRepositoryName = string.Empty;
      }
    }

    public static string GetConfigStringValue(this Dictionary<string, string> values, string key)
    {
      return values.ContainsKey(key) ? values[key] : string.Empty;
    }

    public static int? GetConfigIntValue(this Dictionary<string, string> values, string key)
    {
      if (!values.ContainsKey(key))
        return null;

      int value;
      if (int.TryParse(values[key], out value))
        return value;

      return null;
    }

    #endregion

    #region Работа с ServiceRunner.

    public static bool ServiceExists(Instance instance)
    {
      return ServiceController.GetServices().Any(s => s.ServiceName == instance.ServiceName);
    }

    public static string GetServiceStatus(Instance instance)
    {
      return GetServiceStatus(instance.ServiceName);
    }

    public static string GetServiceStatus(string serviceName)
    {
      var serviceStatus = Constants.InstanceStatus.Stopped;

      using (var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName))
      {
        if (service != null)
          serviceStatus = service.Status == ServiceControllerStatus.Running ?
              Constants.InstanceStatus.Working :
              Constants.InstanceStatus.Stopped;
      }

      return serviceStatus;
    }

    #endregion

    #region Работа с версиями.

    public static string GetInstancePlatformVersion(string instancePath)
    {
      var platformBuildsPath = AppHelper.GetPlatformBuildsPath(instancePath);
      return GetSolutionVersion(platformBuildsPath);
    }

    public static string GetInstanceSolutionVersion(string instancePath)
    {
      var solutionBuildsPath = AppHelper.GetDirectumRXBuildsPath(instancePath);
      return GetSolutionVersion(solutionBuildsPath);
    }

    private static string GetSolutionVersion(string solutionBuildsPath)
    {
      if (!Directory.Exists(solutionBuildsPath))
        return Constants.NullVersion;

      var manifestFile = Path.Combine(solutionBuildsPath, "manifest.json");
      if (File.Exists(manifestFile))
      {
        var json = File.ReadAllText(manifestFile);
        var solution = JsonSerializer.Deserialize<Solution>(json);
        return solution.Version;
      }
      else
      {
        var directoryInfo = new DirectoryInfo(solutionBuildsPath);
        var subDirectories = directoryInfo.GetDirectories();
        if (subDirectories.Count(x => x.Name.StartsWith("4.")) == 1)
          return subDirectories.FirstOrDefault(x => x.Name.StartsWith("4.")).Name;
        else
          return string.Empty;
      }
    }

    #endregion

    #region Работа с логом.

    public static void ShowMainLog()
    {
      var log = Path.Combine(Constants.LogPath, DateTime.Today.ToString("yyyy-MM-dd") + ".log");
      LaunchProcess("notepad.exe", log, false, false);
    }

    public static void InfoHandler(Instance instance, string message)
    {
      var code = instance != null ? instance.Code : string.Empty;
      var path = instance != null ? instance.InstancePath : string.Empty;
      var logBody = string.Format($"Code: {code}, Path: {path}, Message: {message}");
      logger.Info(logBody);
    }

    public static void ErrorHandler(Instance instance, Exception exception)
    {
      var code = instance != null ? instance.Code : string.Empty;
      var path = instance != null ? instance.InstancePath : string.Empty;
      var logBody = string.Format($"Code: {code}, Path {path}, Message: {exception.Message}, {exception.StackTrace}");
      logger.Error(logBody);
      if (exception.InnerException != null)
      {
        logger.Error(string.Format($"Message: {exception.InnerException.Message}, {exception.InnerException.StackTrace}"));
        if (exception.InnerException.InnerException != null)
          logger.Error(string.Format($"Message: {exception.InnerException.InnerException.Message}, {exception.InnerException.InnerException.StackTrace}"));
      }
      //ShowMainLog();
    }

    #endregion

    #region Работа с процессами.

    public static void LaunchProcess(string fileName)
    {
      LaunchProcess(fileName, false);
    }

    public static void LaunchProcess(string fileName, bool asAdmin)
    {
      LaunchProcess(fileName, null, asAdmin, false);
    }

    public static void LaunchProcess(string fileName, string args, bool asAdmin, bool waitForExit)
    {
      using (var process = new Process())
      {
        process.StartInfo.FileName = fileName;

        if (!string.IsNullOrEmpty(args))
        {
          if (!waitForExit && asAdmin)
            args = args.Replace(" /K ", " /C ");

          process.StartInfo.Arguments = args;
        }

        if (asAdmin)
          process.StartInfo.Verb = "runas";

        try
        {
          process.Start();

          if (waitForExit)
            process.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
          if (ex.Message != "Операция была отменена пользователем")
            throw ex;
        }
      }
    }

    public static void ExecuteCmdCommand(string command)
    {
      ExecuteCmdCommand(command, false);
    }

    public static void ExecuteCmdCommand(string command, bool asAdmin)
    {
      LaunchProcess("cmd", "\"cmd /K " + command + "\"", asAdmin, true);
    }

    public static void ExecuteCmdCommands(bool asAdmin, bool waitForExit, params string[] commands)
    {
      LaunchProcess("cmd", "\"cmd /K " + string.Join(" & ", commands) + "\"", asAdmin, waitForExit);
    }

    public static void ExecuteDoCommands(string instancePath, params string[] commands)
    {
      var command = $"cd {instancePath} & " + string.Join(" & ", commands);
      ExecuteCmdCommand(command, true);
    }

    #endregion
  }
}
