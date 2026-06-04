namespace RXInstanceManager
{
  public class DesktopConfiguration
  {
    public string Name { get; set; }

    public string Details { get; set; }

    public bool IsActive { get; set; }

    public string DisplayName => IsActive ? $"{Name} (активная)" : Name;

    public DesktopConfiguration Clone()
    {
      return new DesktopConfiguration
      {
        Name = Name,
        Details = Details,
        IsActive = IsActive,
      };
    }
  }
}