using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RXInstanceManager
{
  public partial class ConfigurationsDialog : Window
  {
    private readonly string _instancePath;
    private readonly bool _editEnabled;
    private readonly ObservableCollection<DesktopConfiguration> _configurations;
    private DesktopConfiguration _selectedConfiguration;
    private bool _isLoadingEditor;
    private bool _isDirty;

    public bool HasSavedChanges { get; private set; }

    public ConfigurationsDialog(string instancePath, IEnumerable<DesktopConfiguration> configurations, bool editEnabled)
    {
      InitializeComponent();
      _instancePath = instancePath;
      _editEnabled = editEnabled;
      _configurations = new ObservableCollection<DesktopConfiguration>(
        (configurations ?? Enumerable.Empty<DesktopConfiguration>()).Select(x => x.Clone()));
      ConfigurationsList.ItemsSource = _configurations;
      ApplyEditMode();

      var selected = _configurations.FirstOrDefault(x => x.IsActive) ?? _configurations.FirstOrDefault();
      if (selected != null)
        ConfigurationsList.SelectedItem = selected;
      else
        ConfigurationDetails.IsEnabled = false;
    }

    private void ApplyEditMode()
    {
      ConfigurationDetails.IsReadOnly = !_editEnabled;
      AddConfigurationButton.Visibility = _editEnabled ? Visibility.Visible : Visibility.Collapsed;
      SaveButton.Visibility = _editEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ConfigurationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      CommitEditorChanges();

      _selectedConfiguration = ConfigurationsList.SelectedItem as DesktopConfiguration;
      _isLoadingEditor = true;
      if (_selectedConfiguration != null)
      {
        ConfigurationDetails.IsEnabled = true;
        ConfigurationDetails.Text = _selectedConfiguration.Details ?? string.Empty;
      }
      else
      {
        ConfigurationDetails.IsEnabled = false;
        ConfigurationDetails.Text = string.Empty;
      }

      _isLoadingEditor = false;
    }

    private void ConfigurationDetails_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (!_editEnabled || _isLoadingEditor || _selectedConfiguration == null)
        return;

      _isDirty = true;
    }

    private void AddConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
      CommitEditorChanges();

      var configurationName = Dialogs.ShowEnterValueDialog("имя конфигурации");
      if (string.IsNullOrWhiteSpace(configurationName))
        return;

      configurationName = configurationName.Trim();
      if (_configurations.Any(x => string.Equals(x.Name, configurationName, System.StringComparison.OrdinalIgnoreCase)))
      {
        MessageBox.Show($"Конфигурация «{configurationName}» уже существует.", "Конфигурации", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      var templateSource = (_selectedConfiguration ?? ConfigurationsList.SelectedItem as DesktopConfiguration)?.Details
        ?? _configurations.FirstOrDefault()?.Details;
      var configuration = new DesktopConfiguration
      {
        Name = configurationName,
        Details = AppHelper.BuildDesktopConfigurationTemplate(templateSource, configurationName),
        IsActive = false,
      };

      _configurations.Add(configuration);
      ConfigurationsList.SelectedItem = configuration;
      _isDirty = true;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      if (!SaveChanges(showSuccessMessage: true))
        return;
    }

    private bool SaveChanges(bool showSuccessMessage)
    {
      CommitEditorChanges();

      if (_configurations.Count == 0)
      {
        MessageBox.Show("Добавьте хотя бы одну конфигурацию.", "Конфигурации", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      var error = AppHelper.SaveDesktopConfigurations(_instancePath, _configurations.ToList());
      if (!string.IsNullOrEmpty(error))
      {
        MessageBox.Show(error, "Конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }

      ReloadConfigurationsFromFile();
      _isDirty = false;
      HasSavedChanges = true;

      if (showSuccessMessage)
        MessageBox.Show("Конфигурации сохранены в config.yml.", "Конфигурации", MessageBoxButton.OK, MessageBoxImage.Information);

      return true;
    }

    private void ReloadConfigurationsFromFile()
    {
      var selectedName = _selectedConfiguration?.Name;
      var reloaded = AppHelper.GetDesktopConfigurations(_instancePath);

      _configurations.Clear();
      foreach (var configuration in reloaded)
        _configurations.Add(configuration);

      var selected = _configurations.FirstOrDefault(x => string.Equals(x.Name, selectedName, System.StringComparison.OrdinalIgnoreCase))
        ?? _configurations.FirstOrDefault(x => x.IsActive)
        ?? _configurations.FirstOrDefault();
      ConfigurationsList.SelectedItem = selected;
    }

    private void CommitEditorChanges()
    {
      if (!_editEnabled || _selectedConfiguration == null || _isLoadingEditor)
        return;

      _selectedConfiguration.Details = ConfigurationDetails.Text ?? string.Empty;

      if (AppHelper.TryParseConfigurationYaml(_selectedConfiguration.Details, out object parsedConfiguration, out _))
      {
        var parsedName = AppHelper.GetConfigurationName(parsedConfiguration);
        if (!string.IsNullOrWhiteSpace(parsedName))
          _selectedConfiguration.Name = parsedName;
      }

      ConfigurationsList.Items.Refresh();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      if (!_editEnabled)
        return;

      CommitEditorChanges();

      if (!_isDirty)
        return;

      var result = MessageBox.Show(
        "Есть несохранённые изменения. Сохранить перед закрытием?",
        "Конфигурации",
        MessageBoxButton.YesNoCancel,
        MessageBoxImage.Question);

      if (result == MessageBoxResult.Cancel)
      {
        e.Cancel = true;
        return;
      }

      if (result == MessageBoxResult.Yes && !SaveChanges(showSuccessMessage: false))
        e.Cancel = true;
    }
  }
}
