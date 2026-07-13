using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RXInstanceManager
{
  public partial class MainWindow : Window
  {
    private async Task UpdateInstanceGridAsync()
    {
      while (true)
      {
        await Task.Delay(TimeSpan.FromSeconds(2));

        var instances = await Dispatcher.InvokeAsync(() =>
          GridInstances.Items.Cast<Instance>().ToList());

        foreach (var instance in instances)
        {
          if (string.IsNullOrEmpty(instance.Code))
            continue;

          if (instance.Status == Constants.InstanceStatus.Update)
            continue;

          var status = AppHandlers.GetServiceStatus(instance);
          if (status == instance.Status)
            continue;

          var instancePath = instance.InstancePath;
          await Dispatcher.InvokeAsync(() =>
          {
            instance.Status = status;
            LoadInstances(instancePath);
            if (_instance != null && _instance.InstancePath == instancePath)
              ActionButtonVisibleChanging(instance: instance);
          });
        }
      }
    }

    private async Task UpdateInstanceDataAsync()
    {
      while (true)
      {
        await Task.Delay(TimeSpan.FromSeconds(2));

        var instances = await Dispatcher.InvokeAsync(() =>
          GridInstances.Items.Cast<Instance>().ToList());

        foreach (var instance in instances)
        {
          var idx = Instances.instances.FindIndex(i => i.InstancePath == instance.InstancePath);
          if (idx < 0)
            continue;

          var inst = Instances.instances[idx];
          var configYamlPath = AppHelper.GetConfigYamlPath(inst.InstancePath);
          if (!File.Exists(configYamlPath))
          {
            if (inst.Status == Constants.InstanceStatus.NotInstalled)
              continue;

            try
            {
              AppHandlers.UpdateInstanceData(inst);
            }
            catch (Exception ex)
            {
              AppHandlers.ErrorHandler(inst, ex);
              continue;
            }

            var instancePath = inst.InstancePath;
            await RefreshInstanceUiAsync(instancePath);
            continue;
          }

          var changeTime = AppHelper.GetFileChangeTime(configYamlPath);
          if (changeTime.EqualsUpToSeconds(inst.ConfigChanged))
            continue;

          try
          {
            AppHandlers.UpdateInstanceData(inst);
          }
          catch (Exception ex)
          {
            AppHandlers.ErrorHandler(inst, ex);
            continue;
          }

          await RefreshInstanceUiAsync(inst.InstancePath);
        }
      }
    }

    private Task RefreshInstanceUiAsync(string instancePath)
    {
      return Dispatcher.InvokeAsync(() =>
      {
        LoadInstances(instancePath);
        if (_instance != null && _instance.InstancePath == instancePath)
          ActionButtonVisibleChanging(instance: _instance);
      }).Task;
    }
  }
}
