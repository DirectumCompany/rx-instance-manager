
namespace RXInstanceManager
{
  public static class Constants
  {
    public const string LogPath = "logs";
    public const string Protocol = "http";
    public const string Host = "localhost";
    public const string Service = "DirectumRXServiceRunner";
    public const string NullVersion = "0.0.0";

    public static class InstanceStatus
    {
      public const string Working = "Working";
      public const string Stopped = "Stopped";
      public const string NeedInstall = "NeedInstall";
      public const string NotInstalled = "Not installed";
    }

    public static class EditEmptyValue
    {
      public const string Code = "Код";
      public const string Name = "Назначение";
      public const string DBName = "Имя БД";
      public const string URL = "URL";
      public const string HttpPort = "Http порт";
      public const string Service = "Имя службы";
      public const string InstancePath = "Путь";
      public const string StoragePath = "Путь к хранилищу";
      public const string SourcesPath = "Путь к исходникам";
    }
  }
}
