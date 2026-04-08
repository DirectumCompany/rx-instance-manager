namespace RXInstanceManager
{

  public class ContextMenuClass
  {
    public bool ChangeProject { get; set; } //Сменить проект
    public bool CreateProject { get; set; } //Создать проект
    public bool CloneProject { get; set; } //Создать копию текущего проекта
    public bool UpdateConfig { get; set; } //Пропатчить config.yml
    public bool CheckServices { get; set; } //Проверить сервисы
    public bool RunDDSWithOutDeploy { get; set; } //Открыть DDS без deploy
    public bool InfoContext { get; set; } //Сводка по инстансу
    public bool CmdAdminContext { get; set; } //Запустить cmd (от администратора)
    public bool ClearLogContext { get; set; } //Очистить логи
    public bool ClearLogAllInstancesContext { get; set; } //Очистить логи всех инстансов
    public bool ConfigContext { get; set; } //Открыть config.yml
    public bool ProjectConfigContext { get; set; } //Открыть конфиг проекта
    public bool ConvertDBsContext { get; set; } //Конвертировать БД проектов
    public bool RemoveProjectDataContext { get; set; } //Удалить БД и домашний каталог выбранного проекта
    public bool RemoveInstance { get; set; } //Убрать инстанс из списка
    public bool OpenRXFolder { get; set; } //Открыть папку инстанса
    public bool OpenLogFolder { get; set; } //Открыть папку инстанса
  }

  public class Config
  {
    public string LogViewer { get; set; }
    internal bool LogViewerExists { get; set; }

    public bool NeedCheckAfterSet { get; set; }

    public ContextMenuClass ContextMenu { get; set; }

  }
}
