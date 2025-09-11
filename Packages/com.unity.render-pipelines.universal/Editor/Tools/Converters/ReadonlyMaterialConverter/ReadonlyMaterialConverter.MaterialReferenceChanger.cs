using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.Rendering.Universal.MaterialReferenceBuilder;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    internal class MaterialReferenceChanger : IDisposable
    {
        MaterialReferenceBuilder m_Builder;

        public MaterialReferenceChanger()
        {
            m_Builder = new MaterialReferenceBuilder();
        }

        public void Dispose()
        {
            m_Builder?.Dispose();
            m_Builder = null;
        }

        internal static bool AreMaterialsEqual(Material a, Material b)
        {
            return a == b;
        }

        internal static bool AreMaterialsEqual(Material[] a, Material[] b)
        {
            if (a == null || b == null)
                return a == b; // both must be null to be equal

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (!AreMaterialsEqual(a[i], b[i]))
                    return false;
            }

            return true;
        }

        private static bool TryChangeMaterialArray(Func<object> getter, Action<object> setter)
        {
            var materials = getter() as Material[];
            if (materials == null)
                return true;

            bool setIsNeeded = false;
            for (int i = 0; i < materials.Length; ++i)
            {
                if (ReadonlyMaterialMap.TryGetMappingMaterial(materials[i], out var mappingMaterial))
                {
                    materials[i] = mappingMaterial;
                    setIsNeeded = true;
                }
            }

            if (setIsNeeded)
            {
                setter(materials);
                return AreMaterialsEqual(getter() as Material[], materials);
            }

            return true;
        }

        private static bool TryChangeMaterial(Func<object> getter, Action<object> setter)
        {
            var material = getter() as Material;
            if (ReadonlyMaterialMap.TryGetMappingMaterial(material, out var mappingMaterial))
            {
                setter(mappingMaterial);
                var updated = getter() as Material;
                return updated == mappingMaterial;
            }

            return true;
        }

        private static bool ReassignMaterialsFromInstance(object obj, MemberInfo member, bool isArray)
        {
            if (obj == null || member == null)
                return false;

            if (!TryGetFromMemberInfoAccessors(obj, member, out var getter, out var setter))
                return false;

            return (isArray) ? TryChangeMaterialArray(getter, setter) : TryChangeMaterial(getter, setter);
        }

        internal bool ReassignGameObjectMaterials(GameObject go, StringBuilder errors)
        {
            bool ok = true;

            foreach (var entry in m_Builder.GetMaterialReferenceLookUps())
            {
                var components = go.GetComponentsInChildren(entry.type);
                foreach (var component in components)
                {
                    try
                    {
                        UnityEngine.Component prefabSource = null;
                        if (PrefabUtility.IsPartOfPrefabInstance(component))
                            prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(component);

                        ok &= ReassignMaterials(component, prefabSource, entry, errors);
                    }
                    catch (Exception ex)
                    {
                        errors.Append($"{ex.Message} while trying to reassign materials from {component}.");
                    }
                }
            }

            return ok;
        }

        private bool ReassignMaterialsFromInstaceIfOverriden(object obj, object prefabObj, MemberInfo member, bool isArray, StringBuilder errors)
        {
            if (!TryGetFromMemberInfoAccessors(obj, member, out var getter, out var setter))
            {
                errors.AppendLine($"Unable to retrieve material accessors from {obj}");
                return false;
            }

            if (!TryGetFromMemberInfoAccessors(prefabObj, member, out var getterPrefab, out var setterPrefab))
            {
                errors.AppendLine($"Unable to retrieve material accessors from {obj}");
                return false;
            }

            if (isArray)
            {
                Material[] instanceMaterials = getter() as Material[];
                Material[] prefabMaterials = getterPrefab() as Material[];
                if (AreMaterialsEqual(instanceMaterials, prefabMaterials))
                    return true; // They are the same, nothing to do, as the materials must be changed from the prefab

                return TryChangeMaterialArray(getter, setter);
            }

            Material instanceMaterial = getter() as Material;
            Material prefabMaterial = getterPrefab() as Material;
            if (AreMaterialsEqual(instanceMaterial, prefabMaterial))
                return true; // They are the same, nothing to do, as the materials must be changed from the prefab

            return TryChangeMaterial(getter, setter);
        }

        public bool ReassignMaterials(object obj, object prefabObj, MaterialReferenceInfo entry, StringBuilder errors)
        {
            if (errors == null)
                throw new ArgumentNullException("You must provide a valid errors parameter");

            if (obj == null)
            {
                errors.AppendLine($"The given object to change material references is null");
                return false;
            }

            bool ok = true;
            foreach (var materialAccessor in entry.materialAccessors)
            {
                bool reassignOk = (prefabObj != null) ?
                    ReassignMaterialsFromInstaceIfOverriden(obj, prefabObj, materialAccessor.member, materialAccessor.isArray, errors):
                    ReassignMaterialsFromInstance(obj, materialAccessor.member, materialAccessor.isArray);

                if (!reassignOk)
                {
                    ok = false;
                    errors.AppendLine($"Unable to change material on {entry.type} with property {materialAccessor.member.Name}");
                }
            }
            return ok;
        }

        public bool ReassignUnityObjectMaterials(Object obj, StringBuilder errors)
        {
            if (obj == null)
            {
                return false;
            }

            bool reassignOk = true;
            if (obj is GameObject go)
            {
                // Iterate over all components, and reassign materials
                reassignOk = ReassignGameObjectMaterials(go, errors);
            }

            // Any other type, just get the mappings for that type and assing it through reflection
            else if (MaterialReferenceBuilder.TryGetReferenceInfoFromType(obj.GetType(), out var entry))
            {
                reassignOk = ReassignMaterials(obj, null, entry, errors);
            }

            if (reassignOk)
            {
                // Make sure the changes get saved
                EditorUtility.SetDirty(obj);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            else
                errors.AppendLine($"Could not reassign materials of {obj} with {obj.GetType()} type.");

            return reassignOk;
        }
    }
}
