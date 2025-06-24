using System.Text.Json;

namespace Kibali.UpdatedModel;

public class ProvisioningInfo
{
    public string Id { get; set; }

    public string Scheme { get; set; }

    public bool IsHidden { get; set; }

    public bool IsEnabled { get; set; }

    public string Environment { get; set; }

    public string ResourceAppId { get; set; }

}
