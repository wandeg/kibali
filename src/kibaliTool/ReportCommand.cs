using CsvHelper;
using Kibali;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KibaliTool
{
    internal class ReportCommand
    {
        public static async Task<int> Execute(DocumentCommandParameters documentCommandParameters)
        {
            PermissionsDocument doc;
            if (documentCommandParameters.SourcePermissionsFile != null)
            {
                using var stream = new FileStream(documentCommandParameters.SourcePermissionsFile, FileMode.Open);
                doc = PermissionsDocument.Load(stream);
            }
            else if (documentCommandParameters.SourcePermissionsFolder != null)
            {
                doc = PermissionsDocument.LoadFromFolder(documentCommandParameters.SourcePermissionsFolder);
            }
            else
            {
                throw new ArgumentException("Please provide a source permissions file or folder");
            }

            var authZChecker = new AuthZChecker();
            authZChecker.Load(doc);
            var privEntries = FindAllPrivs(authZChecker);
            FindAllPerms(authZChecker);

            return 0;

        }

        private static List<string> FindAllPrivs(AuthZChecker authZChecker)
        {
            var privEntries = new List<string>();
            var toWrite = new List<CsvPermission>();
            foreach (var resource in authZChecker.Resources)
            {
                var path = resource.Key;
                var least = resource.Value.FetchLeastPrivilege(null, null);
                foreach (var methodEntry in least)
                {

                    var method = methodEntry.Key;
                    foreach (var schemeEntry in methodEntry.Value)
                    {
                        var scheme = schemeEntry.Key;
                        var perms = schemeEntry.Value;
                        if (perms.Count > 1)
                        {
                            Console.WriteLine($"Duplicate least privilege entries for path {path} method {method} scheme {scheme}");
                            continue;
                        }
                        if (perms.Count == 0)
                        {
                            Console.WriteLine($"Missing least privilege entries for path {path} method {method} scheme {scheme}");
                            continue;
                        }
                        privEntries.Add($"{path};{method};{scheme};{perms.First()}");
                        var perm = new CsvPermission
                        {
                            Url = path.TrimStart('/'),
                            Method = method,
                            Scheme = scheme,
                            Permissions = perms.First(),
                            SourceFile = "permissions.json"
                        };
                        toWrite.Add(perm);
                    }
                }
            }
            using (var writer = new StreamWriter($"AllLeastPrivilegeEntries{DateTime.UtcNow:yyyy-MM-dd}.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(toWrite);
            }
            return privEntries;
        }

        private static List<string> FindAllPerms(AuthZChecker authZChecker)
        {
            var privEntries = new List<string>();
            var toWrite = new List<CsvPermission>();
            foreach (var resource in authZChecker.Resources)
            {
                var path = resource.Key;
                var methodEntries = resource.Value.SupportedMethods;
                foreach (var methodEntry in methodEntries)
                {

                    var method = methodEntry.Key;
                    foreach (var schemeEntry in methodEntry.Value)
                    {
                        var scheme = schemeEntry.Key;
                        var perms = schemeEntry.Value.Select(p => p.Permission);

                        ////privEntries.Add($"{path};{method};{scheme};{perms.First()}");
                        var perm = new CsvPermission
                        {
                            Url = path.TrimStart('/'),
                            Method = method,
                            Scheme = scheme,
                            Permissions = string.Join(";", perms),
                            SourceFile = "permissions.json"
                        };
                        toWrite.Add(perm);
                    }
                }
            }
            using (var writer = new StreamWriter($"AllPermissions{DateTime.UtcNow:yyyy-MM-dd}.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(toWrite);
            }
            return privEntries;
        }


    }
}
