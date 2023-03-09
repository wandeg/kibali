﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Kibali
{
    public class ProtectedResource
    {
        // Permission -> (Methods,Scheme) -> Path  (Darrel's format)
        // (Schemes -> Permissions) -> restriction -> target  (Kanchan's format)
        // target -> restrictions -> schemes -> Ordered Permissions (CSDL Format) 

        // path -> Method -> Schemes -> Permissions  (Inverted format) 
        
        // (Path, Method) -> Schemes -> Permissions (Docs)
        // (Path, Method) -> Scheme(delegated) -> Permissions (Graph Explorer Tab)
        // Permissions(delegated) (Graph Explorer Permissions List)
        // Schemas -> Permissions ( AAD Onboarding)
        private Dictionary<string, Dictionary<string, HashSet<string>>> leastPrivilegedPermissions { get; set; } = new ();

        public string Url { get; set; }
        public Dictionary<string, Dictionary<string, List<AcceptableClaim>>> SupportedMethods { get; set; } = new Dictionary<string, Dictionary<string, List<AcceptableClaim>>>();

        public ProtectedResource(string url)
        {
            Url = url;
        }

        public void AddRequiredClaims(string permission, PathSet pathSet, string[] leastPrivilegedPermissionSchemes)
        {
            foreach (var supportedMethod in pathSet.Methods)
            {
                var supportedSchemes = new Dictionary<string, List<AcceptableClaim>>();
                foreach (var supportedScheme in pathSet.SchemeKeys)
                {
                    if (!supportedSchemes.ContainsKey(supportedScheme))
                    {
                        supportedSchemes.Add(supportedScheme, new List<AcceptableClaim>());
                    }
                    var isLeastPrivilege = leastPrivilegedPermissionSchemes.Contains(supportedScheme);
                    supportedSchemes[supportedScheme].Add(new AcceptableClaim(permission, pathSet.AlsoRequires, isLeastPrivilege));
                }
                if (!this.SupportedMethods.ContainsKey(supportedMethod))
                {
                    this.SupportedMethods.Add(supportedMethod, supportedSchemes);
                } else
                {
                    Update(this.SupportedMethods[supportedMethod], supportedSchemes);
                };
            }
        }

        public IEnumerable<PermissionsError> ValidateLeastPrivilegePermissions(string permission, PathSet pathSet, string[] leastPrivilegedPermissionSchemes)
        {
            ComputeLeastPrivilegeEntries(permission, pathSet, leastPrivilegedPermissionSchemes);
            var mismatchedSchemes = ValidateMismatchedSchemes(permission, pathSet, leastPrivilegedPermissionSchemes);
            var duplicateErrors = ValidateDuplicatedScopes();
            return mismatchedSchemes.Union(duplicateErrors);
        }

        

        private void ComputeLeastPrivilegeEntries(string permission, PathSet pathSet, IEnumerable<string> leastPrivilegedPermissionSchemes)
        {
            foreach (var supportedMethod in pathSet.Methods)
            {
                var schemeLeastPrivilegeScopes = new Dictionary<string, HashSet<string>>();
                foreach (var supportedScheme in pathSet.SchemeKeys)
                {
                    if (!leastPrivilegedPermissionSchemes.Contains(supportedScheme))
                    {
                        continue;
                    }
                    if (!schemeLeastPrivilegeScopes.ContainsKey(supportedScheme))
                    {
                        schemeLeastPrivilegeScopes.Add(supportedScheme, new HashSet<string>());
                    }
                    schemeLeastPrivilegeScopes[supportedScheme].Add(permission);
                }
                if (!this.leastPrivilegedPermissions.TryGetValue(supportedMethod, out var methodLeastPrivilegeScopes))
                {
                    this.leastPrivilegedPermissions.Add(supportedMethod, schemeLeastPrivilegeScopes);
                }
                else
                {
                    UpdatePrivilegedPermissions(methodLeastPrivilegeScopes, schemeLeastPrivilegeScopes, supportedMethod);
                }   
            }
        }

        private HashSet<PermissionsError> ValidateDuplicatedScopes()
        {
            var errors = new HashSet<PermissionsError>();
            foreach (var methodScopes in this.leastPrivilegedPermissions)
            {
                var method = methodScopes.Key;
                foreach (var schemeScope in methodScopes.Value)
                {
                    var scopes = schemeScope.Value;
                    var scheme = schemeScope.Key;
                    if (scopes.Count > 1 && !IsFalsePositiveDuplicate(method, scopes))
                    {
                        errors.Add(new PermissionsError
                        {
                            Path = this.Url,
                            ErrorCode = PermissionsErrorCode.DuplicateLeastPrivilegeScopes,
                            Message = string.Format(StringConstants.DuplicateLeastPrivilegeSchemeErrorMessage, string.Join(", ", scopes), scheme, method),
                        });
                    }
                }
            }
            return errors;
        }

        /// <summary>
        /// Check if the duplicate is a false positive.
        /// </summary>
        /// <param name="method">HTTP Method.</param>
        /// <param name="scopes">Duplicated permission scopes.</param>
        /// <returns>True if the duplicate is a false positive (invalid).</returns>
        private bool IsFalsePositiveDuplicate(string method, HashSet<string> scopes)
        {
            // GET operations can be done by ReadWrite permissions but we should only have one Read permission
            // which is the least privileged for Read operations.
            if (method == "GET")
            {
                var groupedOperations = scopes.GroupBy(x => x.Split('.')[1]).ToDictionary(g => g.Key, g => g.Count());
                groupedOperations.TryGetValue("Read", out int readCount);
                groupedOperations.TryGetValue("ReadBasic", out int readBasicCount);
                readCount += readBasicCount;
                return readCount == 1;
            }
            return false;
        }

        private HashSet<PermissionsError> ValidateMismatchedSchemes(string permission, PathSet pathSet, IEnumerable<string> leastPrivilegePermissionSchemes)
        {
            var mismatchedPrivilegeSchemes = leastPrivilegePermissionSchemes.Except(pathSet.SchemeKeys);
            var errors = new HashSet<PermissionsError>();
            if (mismatchedPrivilegeSchemes.Any())
            {
                var invalidSchemes = string.Join(", ", mismatchedPrivilegeSchemes);
                var expectedSchemes = string.Join(", ", pathSet.SchemeKeys);
                errors.Add(new PermissionsError
                {
                    Path = this.Url,
                    ErrorCode = PermissionsErrorCode.InvalidLeastPrivilegeScheme,
                    Message = string.Format(StringConstants.UnexpectedLeastPrivilegeSchemeErrorMessage, invalidSchemes, permission, expectedSchemes),
                });
            }
            return errors;
        }

        private void UpdatePrivilegedPermissions(Dictionary<string, HashSet<string>> existingPermissions, Dictionary<string, HashSet<string>> newPermissions, string method)
        {
            foreach (var newPermission in newPermissions)
            {
                if (existingPermissions.TryGetValue(newPermission.Key, out var existingPermission))
                {
                    existingPermission.UnionWith(newPermission.Value);
                }
                else
                {
                    existingPermissions[newPermission.Key] = newPermission.Value;
                }
            }
        }

        private void Update(Dictionary<string, List<AcceptableClaim>> existingSchemes, Dictionary<string, List<AcceptableClaim>> newSchemes)
        {
            
            foreach(var newScheme in newSchemes)
            {
                if (existingSchemes.TryGetValue(newScheme.Key, out var existingScheme))
                {
                    existingScheme.AddRange(newScheme.Value);
                } 
                else
                {
                    existingSchemes[newScheme.Key] = newScheme.Value;
                }
            }
        }

        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("url");
            writer.WriteStringValue(Url);
            writer.WritePropertyName("methods");
            WriteSupportedMethod(writer, this.SupportedMethods);
            
            writer.WriteEndObject();
        }

        private void WriteSupportedMethod(Utf8JsonWriter writer, Dictionary<string, Dictionary<string, List<AcceptableClaim>>> supportedMethods)
        {
            writer.WriteStartObject();
            foreach (var item in supportedMethods)
            {
                writer.WritePropertyName(item.Key);
                WriteSupportedSchemes(writer, item.Value);
            }
            writer.WriteEndObject();
        }

        public void WriteSupportedSchemes(Utf8JsonWriter writer, Dictionary<string, List<AcceptableClaim>> methodClaims)
        {
            writer.WriteStartObject();
            foreach (var item in methodClaims)
            {
                writer.WritePropertyName(item.Key);
                WriteAcceptableClaims(writer, item.Value);
            }
            writer.WriteEndObject();
        }

        public void WriteAcceptableClaims(Utf8JsonWriter writer, List<AcceptableClaim> schemes)
        {
            writer.WriteStartArray();
            foreach (var item in schemes.OrderByDescending(c => c.Least))
            {
                item.Write(writer);
            }
            writer.WriteEndArray();
        }

        public string GeneratePermissionsTable(Dictionary<string, List<AcceptableClaim>> methodClaims)
        {
            var permissionsStub = new List<string> { "Not supported." };
            var markdownBuilder = new MarkDownBuilder();
            markdownBuilder.StartTable("Permission type", "Least privileged permission", "Higher privileged permissions");
            var least = string.Empty;
            var higher = string.Empty;

            var delegatedWorkScopes = methodClaims.TryGetValue("DelegatedWork", out List<AcceptableClaim> claims) ? claims.OrderByDescending(c => c.Least).Select(c => c.Permission) : permissionsStub;
            (least, higher) = ExtractScopes(delegatedWorkScopes);
            markdownBuilder.AddTableRow("Delegated (work or school account)", least, higher);

            var delegatedPersonalScopes = methodClaims.TryGetValue("DelegatedPersonal", out claims) ? claims.OrderByDescending(c => c.Least).Select(c => c.Permission) : permissionsStub;
            (least, higher) = ExtractScopes(delegatedPersonalScopes);
            markdownBuilder.AddTableRow("Delegated (personal Microsoft account)", least, higher);

            var appOnlyScopes = methodClaims.TryGetValue("Application", out claims) ? claims.OrderByDescending(c => c.Least).Select(c => c.Permission) : permissionsStub;
            (least, higher) = ExtractScopes(appOnlyScopes);
            markdownBuilder.AddTableRow("Application", least, higher);
            markdownBuilder.EndTable();
            return markdownBuilder.ToString();
        }

        public string FetchLeastPrivilege(string method = null, string scheme = null)
        {
            var output = string.Empty;
            var leastPrivilege = new Dictionary<string, Dictionary<string, HashSet<string>>>();
            if (method != null && scheme != null)
            {
                if (!leastPrivilege.ContainsKey(method))
                {
                    leastPrivilege[method] = new Dictionary<string, HashSet<string>>();
                }
                leastPrivilege[method][scheme] = this.SupportedMethods[method][scheme].Where(p => p.Least == true).Select(p => p.Permission).ToHashSet();
            }
            if (method != null && scheme == null)
            {
                this.SupportedMethods.TryGetValue(method, out var supportedSchemes);
                if (supportedSchemes == null)
                {
                    return output;
                }
                foreach (var supportedScheme in supportedSchemes)
                {
                    if (!leastPrivilege.ContainsKey(method))
                    {
                        leastPrivilege[method] = new Dictionary<string, HashSet<string>>();
                    }
                    leastPrivilege[method][supportedScheme.Key] = supportedScheme.Value.Where(p => p.Least == true).Select(p => p.Permission).ToHashSet();
                }
            }
            if (method == null && scheme != null)
            {
                foreach (var supportedMethod in this.SupportedMethods)
                {
                    supportedMethod.Value.TryGetValue(scheme, out var supportedSchemeClaims);
                    if (supportedSchemeClaims == null)
                    {
                        return output;
                    }
                    if (!leastPrivilege.ContainsKey(supportedMethod.Key))
                    {
                        leastPrivilege[supportedMethod.Key] = new Dictionary<string, HashSet<string>>();
                    }
                    leastPrivilege[supportedMethod.Key][scheme] = supportedSchemeClaims.Where(p => p.Least == true).Select(p => p.Permission).ToHashSet();
                }
            }
            if (method == null && scheme == null)
            {
                foreach (var supportedMethod in this.SupportedMethods)
                {
                    foreach (var supportedScheme in supportedMethod.Value)
                    {
                        if (!leastPrivilege.ContainsKey(supportedMethod.Key))
                        {
                            leastPrivilege[supportedMethod.Key] = new Dictionary<string, HashSet<string>>();
                        }
                        leastPrivilege[supportedMethod.Key][supportedScheme.Key] = supportedScheme.Value.Where(p => p.Least == true).Select(p => p.Permission).ToHashSet();
                    }
                }
            }
            var builder = new StringBuilder();
            foreach (var methodEntry in leastPrivilege)
            {
                builder.AppendLine();
                builder.AppendLine(methodEntry.Key);
                foreach (var schemeEntry in methodEntry.Value)
                {
                    builder.AppendLine($"|{schemeEntry.Key} |{string.Join(";", schemeEntry.Value)}|");
                    builder.AppendLine();
                }
                builder.AppendLine();
            }
            output = builder.ToString();
            return output;
        
        private (string least, string higher) ExtractScopes(IEnumerable<string> orderedScopes)
        {
            var least = orderedScopes.First();
            var others = orderedScopes.Skip(1);
            var higher = others.Any() ? string.Join(", ", others) : "Not supported.";
            return (least, higher);
        }
    }

}
