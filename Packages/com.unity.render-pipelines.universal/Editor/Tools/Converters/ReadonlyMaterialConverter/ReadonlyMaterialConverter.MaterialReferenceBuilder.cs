using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    internal class MaterialReferenceBuilder : IDisposable
    {
        [System.Diagnostics.DebuggerDisplay("{type.Name}")]
        public class MaterialReferenceInfo
        {
            public Type type;
            public List<(MemberInfo member, bool isArray)> materialAccessors;
        }

        List<MaterialReferenceInfo> m_MaterialReferenceCache;

        public MaterialReferenceBuilder()
        {
            BuildMaterialReferenceCache();
        }

        public void Dispose()
        {
            m_MaterialReferenceCache.Clear();
            m_MaterialReferenceCache = null;
        }

        public static bool TryGetFromMemberInfoAccessors(object obj, MemberInfo member, out Func<object> getter, out Action<object> setter)
        {
            getter = null;
            setter = null;

            if (obj == null || member == null)
                return false;

            if (member is PropertyInfo prop)
            {
                var getMethod = prop.GetGetMethod(true);
                var setMethod = prop.GetSetMethod(true);
                if (getMethod == null || setMethod == null)
                    return false;

                getter = () => getMethod.Invoke(obj, null);
                setter = newVal => setMethod.Invoke(obj, new object[] { newVal });
            }
            else if (member is FieldInfo field)
            {
                getter = () => field.GetValue(obj);
                setter = newVal => field.SetValue(obj, newVal);
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Filters a list of material accessors to prioritize "shared" variants.
        /// If both a regular and a corresponding "shared" accessor exist (e.g., "material" and "sharedMaterial"),
        /// only the "shared" accessor is kept to ensure safer and consistent material access.
        /// </summary>
        static List<(MemberInfo member, bool isArray)> GetSafeMaterialAccessors(List<(MemberInfo member, bool isArray)> materialAccessors)
        {
            var safeMaterialAccessors = new List<(MemberInfo member, bool isArray)>();

            // Collect all member names that include "shared" (case-insensitive),
            // so we know which "shared*" accessors exist.
            var sharedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < materialAccessors.Count; i++)
            {
                var name = materialAccessors[i].member.Name;
                if (name.IndexOf("shared", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sharedNames.Add(name);
                }
            }

            // Filter the input list:
            // - Always keep accessors whose names start with or contain "shared".
            // - For other accessors, only keep them if there isn't a corresponding
            //   "shared" version (i.e., "shared" + accessorName) in the list.
            for (int i = 0; i < materialAccessors.Count; i++)
            {
                var accessor = materialAccessors[i];
                var name = accessor.member.Name;

                if (name.IndexOf("shared", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Keep all "shared" accessors.
                    safeMaterialAccessors.Add(accessor);
                }
                else
                {
                    // Check if a "shared" variant exists; if not, keep this accessor.
                    var sharedName = "shared" + name;
                    if (!sharedNames.Contains(sharedName))
                    {
                        safeMaterialAccessors.Add(accessor);
                    }
                }
            }

            return safeMaterialAccessors;
        }


        void BuildMaterialReferenceCache()
        {
            m_MaterialReferenceCache = new List<MaterialReferenceInfo>();

            var allComponentTypes = TypeCache.GetTypesDerivedFrom<Component>();

            foreach (var type in allComponentTypes)
            {
                if (TryGetReferenceInfoFromType(type, out var referenceInfo))
                    m_MaterialReferenceCache.Add(referenceInfo);
            }
        }

        public static bool TryGetReferenceInfoFromType(Type type, out MaterialReferenceInfo referenceInfo)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var materialAccessors = new List<(MemberInfo member, bool isArray)>();

            foreach (var field in type.GetFields(flags))
            {
                if (field.FieldType == typeof(Material) || field.FieldType == typeof(Material[]))
                {
                    bool isPublic = field.IsPublic;
                    bool isSerialized = field.GetCustomAttribute<SerializeField>() != null;
                    bool nonSerialized = field.GetCustomAttribute<NonSerializedAttribute>() != null;
                    if (isPublic && !nonSerialized || !isPublic && isSerialized)
                        materialAccessors.Add((field, field.FieldType == typeof(Material[])));
                }
            }

            foreach (var prop in type.GetProperties(flags))
            {
                if (!prop.CanRead) continue;
                if (prop.GetIndexParameters().Length > 0) continue; // skip indexers

                if (prop.PropertyType == typeof(Material) || prop.PropertyType == typeof(Material[]))
                    materialAccessors.Add((prop, prop.PropertyType == typeof(Material[])));
            }

            if (materialAccessors.Count > 0)
            {
                referenceInfo = new MaterialReferenceInfo
                {
                    type = type,
                    materialAccessors = GetSafeMaterialAccessors(materialAccessors)
                };
                return true;
            }

            referenceInfo = null;
            return false;
        }

        /// <summary>
        /// Gets all of the types in the Material Reference lookup that are components. Used to determine whether to run the
        /// method directly or on the component
        /// </summary>
        /// <returns>List of types that are components</returns>
        public IEnumerable<MaterialReferenceInfo> GetMaterialReferenceLookUps() => m_MaterialReferenceCache;
    }
}
