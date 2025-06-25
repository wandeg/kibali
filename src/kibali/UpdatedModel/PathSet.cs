using System;
using System.Collections.Generic;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kibali.UpdatedModel;

public class PathSet
{
    [JsonPropertyName("schemeKeys")]
    public SortedSet<string> SchemeKeys { get; set; } = new SortedSet<string>();

    [JsonPropertyName("methods")]
    public SortedSet<string> Methods { get; set; } = new SortedSet<string>();

    [JsonPropertyName("paths")]
    public SortedDictionary<string, PathObject>? Paths { get; set; }

}
