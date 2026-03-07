using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace JG.GameContent.Diagnostics
{
    public sealed class IdReferenceValidator : IContentValidator
    {
        static readonly ConcurrentDictionary<Type, MemberEntry[]> _memberCache = new();

        public void Validate(ContentCatalogue catalogue, IDiagnosticSink sink)
        {
            foreach (var def in catalogue.GetAllDefs())
            {
                if (def == null) continue;
                var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
                ValidateObject(def, def.GetType(), def.Id, catalogue, sink, visited);
            }
        }

        void ValidateObject(object target, Type targetType, string owningDefId,
            ContentCatalogue catalogue, IDiagnosticSink sink, HashSet<object> visited)
        {
            if (target == null || targetType == null) return;
            if (!visited.Add(target)) return;

            var entries = GetMemberEntries(targetType);
            foreach (var entry in entries)
            {
                var value = entry.GetValue(target);

                if (entry.IdRef != null)
                {
                    // This member has [IdReference] - validate the string value(s)
                    if (value is string strVal)
                    {
                        ValidateReference(strVal, entry, owningDefId, catalogue, sink);
                    }
                    else if (value is IEnumerable enumerable && !(value is string))
                    {
                        foreach (var item in enumerable)
                        {
                            if (item is string s)
                                ValidateReference(s, entry, owningDefId, catalogue, sink);
                        }
                    }
                    continue;
                }

                // Recurse into nested objects
                if (value == null || value is string || value is UnityEngine.Object) continue;

                if (value is IEnumerable collection)
                {
                    foreach (var item in collection)
                    {
                        if (item == null || item is string || item is UnityEngine.Object) continue;
                        var itemType = item.GetType();
                        if (ShouldRecurseInto(itemType))
                            ValidateObject(item, itemType, owningDefId, catalogue, sink, visited);
                    }
                }
                else if (ShouldRecurseInto(entry.MemberType))
                {
                    ValidateObject(value, entry.MemberType, owningDefId, catalogue, sink, visited);
                }
            }
        }

        void ValidateReference(string idValue, MemberEntry entry, string owningDefId,
            ContentCatalogue catalogue, IDiagnosticSink sink)
        {
            if (string.IsNullOrWhiteSpace(idValue))
            {
                if (!entry.IdRef.Optional)
                {
                    sink.Report(new ContentDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Category = DiagnosticCategory.IdReference,
                        DefId = owningDefId,
                        FieldPath = entry.FieldPath,
                        Message = $"Required {entry.IdRef.TargetType.Name} reference is empty.",
                        Detail = $"Set '{entry.FieldPath}' to a valid {entry.IdRef.TargetType.Name} Id."
                    });
                }
                return;
            }

            if (!catalogue.HasDef(entry.IdRef.TargetType, idValue))
            {
                sink.Report(new ContentDiagnostic
                {
                    Severity = entry.IdRef.Optional ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.IdReference,
                    DefId = owningDefId,
                    FieldPath = entry.FieldPath,
                    Message = $"Referenced {entry.IdRef.TargetType.Name} '{idValue}' does not exist.",
                    ExpectedValue = $"A valid {entry.IdRef.TargetType.Name} Id",
                    ActualValue = idValue,
                    Detail = $"Check that '{idValue}' is spelled correctly and that the mod defining it is loaded."
                });
            }
        }

        static bool ShouldRecurseInto(Type type)
        {
            if (type == null) return false;
            if (type.IsPrimitive || type.IsEnum) return false;
            if (type == typeof(string)) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;
            if (typeof(Newtonsoft.Json.Linq.JToken).IsAssignableFrom(type)) return false;
            if (type.IsValueType) return false;
            return true;
        }

        static MemberEntry[] GetMemberEntries(Type type)
        {
            return _memberCache.GetOrAdd(type, t =>
            {
                var list = new List<MemberEntry>();
                var members = t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var mem in members)
                {
                    if (mem is not FieldInfo && mem is not PropertyInfo) continue;

                    Type memberType;
                    Func<object, object> getter;

                    if (mem is FieldInfo fi)
                    {
                        memberType = fi.FieldType;
                        getter = fi.GetValue;
                    }
                    else if (mem is PropertyInfo pi && pi.CanRead && pi.GetIndexParameters().Length == 0)
                    {
                        memberType = pi.PropertyType;
                        getter = obj =>
                        {
                            try { return pi.GetValue(obj); }
                            catch { return null; }
                        };
                    }
                    else continue;

                    var idRef = mem.GetCustomAttribute<IdReferenceAttribute>();

                    list.Add(new MemberEntry
                    {
                        FieldPath = $"{t.Name}.{mem.Name}",
                        MemberType = memberType,
                        IdRef = idRef,
                        GetValue = getter
                    });
                }
                return list.ToArray();
            });
        }

        sealed class MemberEntry
        {
            public string FieldPath;
            public Type MemberType;
            public IdReferenceAttribute IdRef;
            public Func<object, object> GetValue;
        }

        sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
