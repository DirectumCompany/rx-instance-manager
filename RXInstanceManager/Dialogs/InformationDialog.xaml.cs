using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace RXInstanceManager
{
  /// <summary>
  /// Логика взаимодействия для Window1.xaml
  /// </summary>
  public partial class InformationDialog : Window
  {
    public string Value { get; set; }
    public string Path { get; set; }

    public InformationDialog()
    {
      InitializeComponent();
    }

    private void Window_Activated(object sender, EventArgs e)
    {
      if (string.IsNullOrEmpty(Value))
      {
        if (!string.IsNullOrEmpty(Path))
          Input.Text = File.ReadAllText(Path);
      }
      else
        Input.Text = Value;
    }
  }
}
