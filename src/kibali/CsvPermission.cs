using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kibali;

public class CsvPermission
{
    public string Scheme { get; set; }
    public string Method { get; set; }
    public string Url { get; set; }
    public string Permissions { get; set; }

    public string SourceFile { get; set; }
}
