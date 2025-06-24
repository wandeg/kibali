using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Kibali.UpdatedModel
{
    public class Permission
    {
        public string Note { get; set; }
        public bool Implicit { get; set; } = false;
        public string PrivilegeLevel { get; set; }
        public SortedDictionary<string, Scheme> Schemes { get; set; } = new SortedDictionary<string, Scheme>();
        public List<PathSet> PathSets { get; set; } = new List<PathSet>();
        public OwnerInfo OwnerInfo { get; set; } = new();
        public string AuthorizationType { get; set; }
        public string DocumentationWebUrl { get; set; }
        
    }

}
