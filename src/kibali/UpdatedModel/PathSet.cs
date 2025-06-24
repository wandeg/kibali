using System;
using System.Collections.Generic;
using System.Security;
using System.Text.Json;

namespace Kibali.UpdatedModel;

public class PathSet
{
    public SortedSet<string> SchemeKeys { get; set; } = new SortedSet<string>();
    public SortedSet<string> Methods { get; set; } = new SortedSet<string>();
    public string AlsoRequires { get; set; }
    public SortedSet<string> ExcludedProperties { get; set; } = new SortedSet<string>();
    public SortedSet<string> IncludedProperties { get; set; } = new SortedSet<string>();
    

    public SortedDictionary<string, SortedDictionary<string, object>> Paths
    {
        get
        {
            if (paths == null)
            {
                paths = new SortedDictionary<string, SortedDictionary<string, object>>();
            }
            return paths;
        }
        set { paths = value; }
    }
    private SortedDictionary<string, SortedDictionary<string, object>> paths;
   
}
