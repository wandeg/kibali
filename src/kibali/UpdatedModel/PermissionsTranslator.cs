using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kibali.UpdatedModel;

public static class PermissionsTranslator
{
    public static PermissionsDocument? Deserialize(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<PermissionsDocument>(json);
    }

    public static void Serialize(string filePath, PermissionsDocument root)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(root, options);
        File.WriteAllText(filePath, json);
    }

    // Translation logic
    public static SortedDictionary<string, PathObject> TranslatePaths(SortedDictionary<string, string> oldPaths)
    {
        var result = new SortedDictionary<string, PathObject>();
        foreach (var kv in oldPaths)
        {
            var dict = ParsePathValue(kv.Value);
            var pathObj = new PathObject();
            foreach (var entry in dict)
            {
                switch (entry.Key.ToLowerInvariant())
                {
                    case "least":
                        pathObj.Least = new SortedSet<string>(entry.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        break;
                    case "alsorequires":
                        pathObj.AlsoRequires = new SortedSet<string>(entry.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        break;
                    case "includedproperties":
                        pathObj.IncludedProperties = new SortedSet<string>(entry.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        break;
                    default:
                        break;
                }
            }
            result[kv.Key] = pathObj;
        }
        return result;
    }

    // Helper: parse "key1=value1;key2=value2" to Dictionary<string, string>
    public static SortedDictionary<string, string> ParsePathValue(string value)
    {
        var dict = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value)) return dict;
        var pairs = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2)
                dict[kv[0].Trim()] = kv[1].Trim();
        }
        return dict;
    }

    public static SortedDictionary<string, Kibali.UpdatedModel.Scheme> TranslateSchemes(SortedDictionary<string, Kibali.Scheme> oldSchemes)
    {
        var schemes = new SortedDictionary<string, Kibali.UpdatedModel.Scheme>();
        foreach (var (key, oldScheme) in oldSchemes)
        {
            var newScheme = new Kibali.UpdatedModel.Scheme
            {
                AdminDisplayName = oldScheme.AdminDisplayName,
                AdminDescription = oldScheme.AdminDescription,
                UserDisplayName = oldScheme.UserDisplayName,
                UserDescription = oldScheme.UserDescription,
                RequiresAdminConsent = oldScheme.RequiresAdminConsent,
                PrivilegeLevel = oldScheme.PrivilegeLevel,
            };
            schemes[key] = newScheme;
        }
       
        return schemes;
    }

    public static Kibali.UpdatedModel.OwnerInfo TranslateOwners(Kibali.OwnerInfo oldOwnerInfo)
    {
        return new Kibali.UpdatedModel.OwnerInfo
        {
            OwnerSecurityGroup = oldOwnerInfo.OwnerSecurityGroup
        };
    }

    public static Kibali.UpdatedModel.PermissionsDocument Translate(Kibali.PermissionsDocument oldRoot)
    {
        var newRoot = new Kibali.UpdatedModel.PermissionsDocument
        {
            Schema = oldRoot.Schema,
            Permissions = new SortedDictionary<string, Permission>()
        };
        if (oldRoot.Permissions == null) return newRoot;
        foreach (var (permKey, permOld) in oldRoot.Permissions)
        {
            var permNew = new Permission
            {
                AuthorizationType = permOld.AuthorizationType,
                Schemes = TranslateSchemes(permOld.Schemes),
                OwnerInfo = TranslateOwners(permOld.OwnerInfo),
                PathSets = new List<PathSet>()
            };
            if (permOld.PathSets != null)
            {
                foreach (var pathSetOld in permOld.PathSets)
                {
                    var pathSetNew = new PathSet
                    {
                        SchemeKeys = pathSetOld.SchemeKeys,
                        Methods = pathSetOld.Methods,
                        Paths = pathSetOld.Paths != null ? TranslatePaths(pathSetOld.Paths) : null
                    };
                    permNew.PathSets.Add(pathSetNew);
                }
            }
            newRoot.Permissions[permKey] = permNew;
        }
        return newRoot;
    }
}
