using System;
using System.Text.Json;

namespace Kibali.UpdatedModel;

public class Scheme
{
    public string AdminDisplayName { get; set; }
    public string AdminDescription { get; set; }
    public string UserDisplayName { get; set; }
    public string UserDescription { get; set; }
    public bool RequiresAdminConsent { get; set; }
    public bool IsPreauthorizationOnly { get; set; }
    public int PrivilegeLevel { get; set; }
}