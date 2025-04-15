using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RXInstanceManager
{
  public static class Instances
  {
    internal static readonly string YamlFilePath = $"{AppContext.BaseDirectory}{Constants.InstancesDBFileName}";
    public static List<Instance> instances;
    public static List<string> instancesFolders;

    public static void UpdateYaml()
    {
      var serializer = new YamlDotNet.Serialization.SerializerBuilder()
          .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
          .Build();
      var yaml = serializer.Serialize(instancesFolders);
      File.WriteAllText(Instances.YamlFilePath, yaml);
    }

    public static void Save(this Instance instance)
    {
      if (!Instances.instancesFolders.Where(i => i == instance.InstancePath).Any())
      {
        Instances.instancesFolders.Add(instance.InstancePath);
        UpdateYaml();
      }
    }

    public static void Add(string instancePath)
    {
      if (!Instances.instancesFolders.Where(i => i == instancePath).Any())
      {
        Instances.instances.Add(new Instance(instancePath));
        Instances.instancesFolders.Add(instancePath);
        UpdateYaml();
      }
    }

    public static List<Instance> Get()
    {
      if (instances == null)
      {
        instances = new List<Instance>();
        foreach (var folder in instancesFolders)
          instances.Add(new Instance(folder));
      }

      return instances;
    }

    public static void Delete(Instance instance)
    {
      var index = Instances.instancesFolders.FindIndex(i => i == instance.InstancePath);
      Instances.instances.RemoveAt(index);
      Instances.instancesFolders.RemoveAt(index);
      UpdateYaml();
    }

    public static void Create()
    {
      Instances.instancesFolders = new List<string>();
      if (File.Exists(Instances.YamlFilePath))
      {
        using (var yamlReader = new StreamReader(Instances.YamlFilePath))
        {
          var deserialize = new YamlDotNet.Serialization.DeserializerBuilder()
                                          .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                                          .Build();
          Instances.instancesFolders = deserialize.Deserialize<List<string>>(yamlReader);
        }
      }
    }
  }
}
