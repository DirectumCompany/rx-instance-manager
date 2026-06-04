using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace RXInstanceManager
{
  public static class AppHelper
  {
    private const int ConfigYamlIndent = 4;

    public static string Base64EncodeFromUTF8(string plainText)
    {
      var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
      return System.Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64EncodeFromASCII(string plainText)
    {
      var plainTextBytes = System.Text.Encoding.ASCII.GetBytes(plainText);
      return System.Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64Encode(byte[] plainText)
    {
      return System.Convert.ToBase64String(plainText);
    }

    public static byte[] Base64Decode(string base64EncodedData)
    {
      return System.Convert.FromBase64String(base64EncodedData);
    }

    public static string Base64DecodeToUTF8(string base64EncodedData)
    {
      return System.Text.Encoding.UTF8.GetString(Base64Decode(base64EncodedData));
    }

    public static string Base64DecodeToASCII(string base64EncodedData)
    {
      return System.Text.Encoding.ASCII.GetString(Base64Decode(base64EncodedData));
    }

    public static bool ValidateInputCode(string code)
    {
      return code.Length > 1 && code.Length <= 10 && code.All(x => (x >= 'a' && x <= 'z') || (x >= '0' && x <= '9'));
    }

    public static bool ValidateInputDBName(string name)
    {
      return name.Length >= 3 && name.Length <= 25 && name.All(x => (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9') || (x == '_'));
    }

    public static bool ValidateInputPort(string port)
    {
      return port.Length <= 10 && port.All(x => (x >= '0' && x <= '9'));
    }

    public static string GetDirectumLauncherPath(string instancePath)
    {
      return Path.Combine(instancePath, "DirectumLauncher.exe");
    }

    public static string GetDoPath(string instancePath)
    {
      var bat = Path.Combine(instancePath, "do.bat");
      if (File.Exists(bat))
        return bat;
      var sh = Path.Combine(instancePath, "do.sh");
      if (File.Exists(sh))
        return sh;
      return Path.Combine(instancePath, "do");
    }

    public static string GetConfigYamlPath(string instancePath)
    {
      return Path.Combine(instancePath, "etc", "config.yml");
    }

    public static string GetConfigYamlExamplePath(string instancePath)
    {
      return Path.Combine(instancePath, "etc", "config.yml.example");
    }

    public static string GetBuildsPath(string instancePath)
    {
      return Path.Combine(instancePath, "etc", "_builds");
    }

    public static string GetPlatformBuildsPath(string instancePath)
    {
      var path = Path.Combine(instancePath, "etc", "_builds", "Platform");
      
      if (Directory.Exists(path))
        return path;

      return Path.Combine(instancePath, "etc", "_builds", "PlatformBuilds");
    }

    public static string GetDirectumRXBuildsPath(string instancePath)
    {
      var path = Path.Combine(instancePath, "etc", "_builds", "DirectumRX");

      if (Directory.Exists(path))
        return path;

      return Path.Combine(instancePath, "etc", "_builds", "base");

    }

    public static string GetDDSPath(string instancePath)
    {
      return Path.Combine(instancePath, "etc", "_builds", "DevelopmentStudio", "bin", "DevelopmentStudio.exe");
    }

    public static string GetCDSPath(string instancePath, string code)
    {
      return Path.Combine(instancePath, "etc", "_" + code, "_builds_bin", "DevelopmentStudioDesktop", "DevelopmentStudio.exe");
    }

    public static string GetLocalIPAddress()
    {
      var host = Dns.GetHostEntry(Dns.GetHostName());
      foreach (var ip in host.AddressList)
      {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
          return ip.ToString();
        }
      }
      throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    public static string GetClientURL(string protocol, string host, int port)
    {
      var real_host = host;
      if (real_host == "{{ host_ip }}")
      {
        real_host = GetLocalIPAddress();

      }

      return $"{protocol}://{real_host}:{port}/Client";
    }
    public static string GetClientURL(string protocol, string host, string port)
    {
      var real_host = host;
      if (real_host == "{{ host_ip }}")
      {
        real_host = GetLocalIPAddress();

      }

      return $"{protocol}://{real_host}:{port}/Client";
    }


    public static string GetDBNameFromConnectionString(string engine, string connectionString)
    {
      if (engine == "mssql")
      {
        var databaseNameParam = connectionString.Split(';').FirstOrDefault(x => x.Contains("initial catalog"));
        if (databaseNameParam != null)
          return databaseNameParam.Split('=')[1];
      }

      if (engine == "postgres")
      {
        var databaseNameParam = connectionString.Split(';').FirstOrDefault(x => x.Contains("database"));
        if (databaseNameParam != null)
          return databaseNameParam.Split('=')[1];
      }

      return null;
    }

    public static string GetServerFromConnectionString(string engine, string connectionString)
    {
      if (engine == "mssql")
      {
        var databaseNameParam = connectionString.Split(';').FirstOrDefault(x => x.Contains("data source"));
        if (databaseNameParam != null)
          return databaseNameParam.Split('=')[1];
      }

      if (engine == "postgres")
      {
        var databaseNameParam = connectionString.Split(';').FirstOrDefault(x => x.Contains("server"));
        if (databaseNameParam != null)
          return databaseNameParam.Split('=')[1];
      }

      return null;
    }

    public static bool CheckInstance(string url)
    {
      try
      {
        using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
        {
          var response = client.GetAsync(url).GetAwaiter().GetResult();
          return response.IsSuccessStatusCode;
        }
      }
      catch
      {
        return false;
      }
    }

    public static DateTime GetFileChangeTime(string path)
    {
      if (!File.Exists(path))
        return DateTime.MinValue;

      return File.GetLastWriteTime(path);
    }

    public static bool EqualsUpToSeconds(this DateTime dt1, DateTime dt2)
    {
      var date1 = dt1.Date.AddHours(dt1.Hour).AddMinutes(dt1.Minute).AddSeconds(dt1.Second);
      var date2 = dt2.Date.AddHours(dt2.Hour).AddMinutes(dt2.Minute).AddSeconds(dt2.Second);
      return date1.Equals(date2);
    }

    public static bool LessThanUpToSeconds(this DateTime dt1, DateTime dt2)
    {
      var date1 = dt1.Date.AddHours(dt1.Hour).AddMinutes(dt1.Minute).AddSeconds(dt1.Second);
      var date2 = dt2.Date.AddHours(dt2.Hour).AddMinutes(dt2.Minute).AddSeconds(dt2.Second);
      return date1 < date2;
    }

    public static bool MoreThanUpToSeconds(this DateTime dt1, DateTime dt2)
    {
      var date1 = dt1.Date.AddHours(dt1.Hour).AddMinutes(dt1.Minute).AddSeconds(dt1.Second);
      var date2 = dt2.Date.AddHours(dt2.Hour).AddMinutes(dt2.Minute).AddSeconds(dt2.Second);
      return date1 > date2;
    }

    /// <summary>
    /// True when platform version is strictly after the 26.1 release line (e.g. 26.2+, 27+).
    /// </summary>
    public static bool IsPlatformVersionGreaterThan26_1(string platformVersion)
    {
      if (string.IsNullOrWhiteSpace(platformVersion))
        return false;

      var parts = platformVersion.Split('.');
      if (parts.Length < 2)
        return false;

      if (!int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor))
        return false;

      if (major > 26)
        return true;
      if (major < 26)
        return false;

      return minor > 1;
    }

    public static string GetProjectConfigPath(dynamic ymlData, string platformVersion)
    {
      if (IsPlatformVersionGreaterThan26_1(platformVersion))
      {
        try
        {
          var path = ymlData.services_config["DevelopmentStudioDesktop"]["ACTIVE_CONFIGURATION"];
          return path?.ToString() ?? string.Empty;
        }
        catch
        {
          return string.Empty;
        }
      }

      return ymlData.variables["project_config_path"]?.ToString() ?? string.Empty;
    }

    public static string GetActiveDesktopConfiguration(string instancePath)
    {
      var configYamlPath = GetConfigYamlPath(instancePath);
      if (!File.Exists(configYamlPath))
        return string.Empty;

      using (var reader = new StreamReader(configYamlPath))
      {
        var deserializer = new DeserializerBuilder().Build();
        dynamic ymlData = deserializer.Deserialize<ExpandoObject>(reader.ReadToEnd());
        try
        {
          var active = ymlData.services_config["DevelopmentStudioDesktop"]["ACTIVE_CONFIGURATION"];
          return active?.ToString() ?? string.Empty;
        }
        catch
        {
          return string.Empty;
        }
      }
    }

    public static List<string> GetDesktopConfigurationNames(string instancePath, string excludeConfigurationName = null)
    {
      var names = new List<string>();
      foreach (var configuration in GetDesktopConfigurations(instancePath))
      {
        if (!string.IsNullOrWhiteSpace(excludeConfigurationName) &&
            string.Equals(configuration.Name, excludeConfigurationName, StringComparison.OrdinalIgnoreCase))
          continue;

        names.Add(configuration.Name);
      }

      return names;
    }

    public static List<DesktopConfiguration> GetDesktopConfigurations(string instancePath)
    {
      var configurations = new List<DesktopConfiguration>();
      var configYamlPath = GetConfigYamlPath(instancePath);
      if (!File.Exists(configYamlPath))
        return configurations;

      var activeConfiguration = GetActiveDesktopConfiguration(instancePath);
      using (var reader = new StreamReader(configYamlPath))
      {
        var deserializer = new DeserializerBuilder().Build();
        dynamic ymlData = deserializer.Deserialize<ExpandoObject>(reader.ReadToEnd());
        try
        {
          var configurationsRaw = ymlData.services_config["DevelopmentStudioDesktop"]["CONFIGURATIONS"]["configuration"];
          var serializer = new SerializerBuilder().Build();
          foreach (var configurationRaw in EnumerateConfigurationItems(configurationsRaw))
          {
            var name = GetConfigurationName(configurationRaw);
            if (string.IsNullOrWhiteSpace(name))
              continue;

            configurations.Add(new DesktopConfiguration
            {
              Name = name,
              IsActive = string.Equals(name, activeConfiguration, StringComparison.OrdinalIgnoreCase),
              Details = serializer.Serialize(NormalizeYamlObject(configurationRaw)),
            });
          }
        }
        catch
        {
        }
      }

      return configurations;
    }

    private static IEnumerable<object> EnumerateConfigurationItems(object configurations)
    {
      if (configurations == null)
        yield break;

      if (configurations is IEnumerable enumerable && configurations is not string)
      {
        foreach (var item in enumerable)
          yield return item;
        yield break;
      }

      yield return configurations;
    }

    public static string GetConfigurationName(object configuration)
    {
      if (configuration == null)
        return string.Empty;

      try
      {
        dynamic dynamicConfiguration = configuration;
        return dynamicConfiguration["@name"]?.ToString() ?? string.Empty;
      }
      catch
      {
        return string.Empty;
      }
    }

    private static object NormalizeYamlObject(object value)
    {
      if (value == null)
        return null;

      if (value is IDictionary<string, object> stringDictionary)
      {
        var normalized = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var pair in stringDictionary)
          normalized[pair.Key] = NormalizeYamlObject(pair.Value);
        return normalized;
      }

      if (value is IDictionary<object, object> objectDictionary)
      {
        var normalized = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var pair in objectDictionary)
          normalized[pair.Key?.ToString() ?? string.Empty] = NormalizeYamlObject(pair.Value);
        return normalized;
      }

      if (value is IDictionary legacyDictionary)
      {
        var normalized = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in legacyDictionary)
          normalized[entry.Key?.ToString() ?? string.Empty] = NormalizeYamlObject(entry.Value);
        return normalized;
      }

      if (value is IEnumerable enumerable && value is not string)
        return enumerable.Cast<object>().Select(NormalizeYamlObject).ToList();

      return value;
    }

    public static bool TryParseConfigurationYaml(string yaml, out object configuration, out string error)
    {
      configuration = null;
      error = null;

      if (string.IsNullOrWhiteSpace(yaml))
      {
        error = "Содержимое конфигурации не может быть пустым.";
        return false;
      }

      try
      {
        var deserializer = new DeserializerBuilder().Build();
        configuration = deserializer.Deserialize<object>(yaml);
        if (configuration == null)
        {
          error = "Не удалось разобрать YAML.";
          return false;
        }

        return true;
      }
      catch (Exception ex)
      {
        error = ex.Message;
        return false;
      }
    }

    public static void SetConfigurationName(object configuration, string name)
    {
      if (configuration is IDictionary<string, object> stringDictionary)
      {
        stringDictionary["@name"] = name;
        return;
      }

      if (configuration is IDictionary<object, object> objectDictionary)
      {
        objectDictionary["@name"] = name;
        return;
      }

      if (configuration is IDictionary legacyDictionary)
        legacyDictionary["@name"] = name;
    }

    public static string BuildDesktopConfigurationTemplate(string baseDetailsYaml, string name)
    {
      if (string.IsNullOrWhiteSpace(baseDetailsYaml))
        return $"@name: {name}";

      if (!TryParseConfigurationYaml(baseDetailsYaml, out object configuration, out _))
        return $"@name: {name}";

      SetConfigurationName(configuration, name);
      var serializer = new SerializerBuilder().Build();
      return serializer.Serialize(NormalizeYamlObject(configuration));
    }

    public static string SaveDesktopConfigurations(string instancePath, IReadOnlyList<DesktopConfiguration> configurations)
    {
      if (configurations == null || configurations.Count == 0)
        return "Список конфигураций пуст.";

      var configYamlPath = GetConfigYamlPath(instancePath);
      if (!File.Exists(configYamlPath))
        return "Файл config.yml не найден.";

      var parsedConfigurations = new List<object>();
      var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var configuration in configurations)
      {
        if (!TryParseConfigurationYaml(configuration.Details, out object parsedConfiguration, out string parseError))
          return $"Конфигурация «{configuration.Name}»: {parseError}";

        var name = GetConfigurationName(parsedConfiguration);
        if (string.IsNullOrWhiteSpace(name))
          name = configuration.Name;

        if (string.IsNullOrWhiteSpace(name))
          return "У каждой конфигурации должно быть указано имя (@name).";

        SetConfigurationName(parsedConfiguration, name);
        if (!names.Add(name))
          return $"Дублируется имя конфигурации: {name}";

        parsedConfigurations.Add(parsedConfiguration);
      }

      try
      {
        var originalContent = File.ReadAllText(configYamlPath);
        if (!TryPatchConfigurationsSection(originalContent, parsedConfigurations, out string patchedContent, out string patchError))
          return patchError;

        File.WriteAllText(configYamlPath, patchedContent);
        return null;
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }

    private static bool TryPatchConfigurationsSection(string fileContent, List<object> configurations, out string patchedContent, out string error)
    {
      patchedContent = null;
      error = null;

      var newline = fileContent.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
      var lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

      if (!TryLocateConfigurationsSection(lines, out int configurationsLineIndex, out int configurationsEndIndex, out int configurationsIndent))
      {
        if (!TryInsertConfigurationsSection(lines, configurations, out patchedContent, out error, newline))
          error ??= "Секция CONFIGURATIONS не найдена в config.yml.";
        return error == null;
      }

      var sectionLines = BuildConfigurationsSectionLines(configurationsIndent, configurations);
      var result = new List<string>();
      result.AddRange(lines.Take(configurationsLineIndex));
      result.AddRange(sectionLines);
      result.AddRange(lines.Skip(configurationsEndIndex));
      patchedContent = string.Join(newline, result);
      return true;
    }

    private static bool TryLocateConfigurationsSection(string[] lines, out int configurationsLineIndex, out int configurationsEndIndex, out int configurationsIndent)
    {
      configurationsLineIndex = -1;
      configurationsEndIndex = -1;
      configurationsIndent = -1;

      var desktopLineIndex = -1;
      var desktopIndent = -1;
      for (var i = 0; i < lines.Length; i++)
      {
        var trimmed = lines[i].TrimStart();
        if (!trimmed.StartsWith("DevelopmentStudioDesktop:", StringComparison.Ordinal))
          continue;

        desktopLineIndex = i;
        desktopIndent = GetLineIndent(lines[i]);
        break;
      }

      if (desktopLineIndex < 0)
        return false;

      for (var i = desktopLineIndex + 1; i < lines.Length; i++)
      {
        var line = lines[i];
        if (IsIgnorableYamlLine(line))
          continue;

        var indent = GetLineIndent(line);
        if (indent <= desktopIndent && line.TrimStart().Contains(':'))
          break;

        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith("CONFIGURATIONS:", StringComparison.Ordinal))
          continue;

        configurationsLineIndex = i;
        configurationsIndent = indent;
        configurationsEndIndex = lines.Length;
        for (var j = i + 1; j < lines.Length; j++)
        {
          var nextLine = lines[j];
          if (IsIgnorableYamlLine(nextLine))
            continue;

          var nextIndent = GetLineIndent(nextLine);
          if (nextIndent <= configurationsIndent && nextLine.TrimStart().Contains(':'))
          {
            configurationsEndIndex = j;
            break;
          }
        }

        return true;
      }

      return false;
    }

    private static bool TryInsertConfigurationsSection(string[] lines, List<object> configurations, out string patchedContent, out string error, string newline)
    {
      patchedContent = null;
      error = null;

      var desktopLineIndex = -1;
      var desktopIndent = -1;
      for (var i = 0; i < lines.Length; i++)
      {
        var trimmed = lines[i].TrimStart();
        if (!trimmed.StartsWith("DevelopmentStudioDesktop:", StringComparison.Ordinal))
          continue;

        desktopLineIndex = i;
        desktopIndent = GetLineIndent(lines[i]);
        break;
      }

      if (desktopLineIndex < 0)
      {
        error = "Секция DevelopmentStudioDesktop не найдена в config.yml.";
        return false;
      }

      var insertIndex = lines.Length;
      for (var i = desktopLineIndex + 1; i < lines.Length; i++)
      {
        var line = lines[i];
        if (IsIgnorableYamlLine(line))
          continue;

        var indent = GetLineIndent(line);
        if (indent <= desktopIndent && line.TrimStart().Contains(':'))
        {
          insertIndex = i;
          break;
        }
      }

      var sectionLines = BuildConfigurationsSectionLines(desktopIndent + ConfigYamlIndent, configurations);
      var result = new List<string>();
      result.AddRange(lines.Take(insertIndex));
      result.AddRange(sectionLines);
      result.AddRange(lines.Skip(insertIndex));
      patchedContent = string.Join(newline, result);
      return true;
    }

    private static List<string> BuildConfigurationsSectionLines(int configurationsIndent, List<object> configurations)
    {
      var wrapper = new Dictionary<string, object>
      {
        ["configuration"] = configurations.Count == 1 ? configurations[0] : configurations,
      };

      var innerYaml = SerializeConfigYaml(wrapper);
      var sectionLines = new List<string>
      {
        new string(' ', configurationsIndent) + "CONFIGURATIONS:",
      };

      foreach (var line in SplitYamlLines(innerYaml))
      {
        if (line.Length == 0)
        {
          sectionLines.Add(string.Empty);
          continue;
        }

        sectionLines.Add(new string(' ', configurationsIndent + ConfigYamlIndent) + line);
      }

      return sectionLines;
    }

    private static IEnumerable<string> SplitYamlLines(string yaml)
    {
      return yaml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }

    private static bool IsIgnorableYamlLine(string line)
    {
      return string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal);
    }

    private static int GetLineIndent(string line)
    {
      var indent = 0;
      foreach (var character in line)
      {
        if (character == ' ')
          indent++;
        else if (character == '\t')
          indent += ConfigYamlIndent;
        else
          break;
      }

      return indent;
    }

    private static string SerializeConfigYaml(object graph)
    {
      var serializer = new SerializerBuilder()
        .WithIndentedSequences()
        .Build();
      var emitterSettings = EmitterSettings.Default.WithBestIndent(ConfigYamlIndent);

      using (var writer = new StringWriter())
      {
        var emitter = new Emitter(writer, emitterSettings);
        serializer.Serialize(emitter, graph);
        return writer.ToString();
      }
    }

    private static IDictionary<string, object> GetOrCreateDictionary(IDictionary<string, object> parent, string key)
    {
      if (!parent.TryGetValue(key, out object value) || value == null)
      {
        var created = new ExpandoObject();
        parent[key] = created;
        return (IDictionary<string, object>)created;
      }

      if (value is IDictionary<string, object> stringDictionary)
        return stringDictionary;

      if (value is ExpandoObject expando)
        return expando;

      throw new InvalidOperationException($"Секция {key} в config.yml имеет неожиданный формат.");
    }
  }
}
