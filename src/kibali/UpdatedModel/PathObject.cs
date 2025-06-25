using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Kibali.UpdatedModel;

// Strongly-typed translation for path values
public class PathObject
{
    [JsonPropertyName("least")]
    public SortedSet<string>? Least { get; set; }

    [JsonPropertyName("alsoRequires")]
    public SortedSet<string>? AlsoRequires { get; set; }

    [JsonPropertyName("includedProperties")]
    public SortedSet<string>? IncludedProperties { get; set; }

}