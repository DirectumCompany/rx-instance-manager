using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXInstanceManager
{
  public static class Dialogs
  {
    public static string ShowEnterValueDialog(string emptyValue, string defaultValue = null)
    {
      var dialog = new EnterValueDialog(defaultValue);

      var dialogResult = dialog.ShowDialog();
      if (!dialogResult.HasValue || !dialogResult.Value)
        return null;

      return dialog.Value;
    }

    public static void ShowInformation(string information)
    {
      var dialog = new InformationDialog();
      dialog.Value = information;
      dialog.Show();
    }
    public static void ShowFileContentDialog(string path)
    {
      var dialog = new InformationDialog();
      dialog.Path = path;
      dialog.ShowDialog();
    }
  }
}
