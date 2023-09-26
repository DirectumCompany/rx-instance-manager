using System;
using System.Text.Json.Serialization;

namespace RXInstanceManager
{
  public class Solution
  {
    [JsonPropertyName("runtime")]
    public string Runtime { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("version")]
    public string Version { get; set; }
  }
}
