using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    static class RemoveComponentUtils
    {
        public static IEnumerable<Component> ComponentDependencies([DisallowNull] Component component)
        {
            if (component == null)
                yield break;

            var componentType = component.GetType();
            foreach (var c in component.gameObject.GetComponents<Component>())
            {
                foreach (var rc in c.GetType().GetCustomAttributes(typeof(RequireComponent), true).Cast<RequireComponent>())
                {
                    if (rc.m_Type0 == componentType || rc.m_Type1 == componentType || rc.m_Type2 == componentType)
                    {
                        yield return c;
                        break;
                    }
                }
            }
        }

        public static bool CanRemoveComponent([DisallowNull] Component component, IEnumerable<Component> dependencies)
        {
            if (dependencies.Count() == 0)
                return true;

            Component firstDependency = dependencies.First();
            string error = $"Can't remove {component.GetType().Name} because {firstDependency.GetType().Name} depends on it.";
            EditorUtility.DisplayDialog("Can't remove component", error, "OK");
            return false;
        }

        public static bool RemoveComponent([DisallowNull] Component component, IEnumerable<Component> dependencies)
        {
            var additionalDatas = dependencies
                    .Where(c => c != component && c is IAdditionalData)
                    .ToList();

            if (!RemoveComponentUtils.CanRemoveComponent(component, dependencies.Where(c => !additionalDatas.Contains(c))))
                return false;

            bool removed = true;
            var isAssetEditing = EditorUtility.IsPersistent(component);
            try
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StartAssetEditing();
                }
                Undo.SetCurrentGroupName($"Remove {component.GetType()} and additional data components");

                // The components with RequireComponent(typeof(T)) also contain the AdditionalData attribute, proceed with the remove
                foreach (var additionalDataComponent in additionalDatas)
                {
                    if (additionalDataComponent != null)
                    {
                        Undo.DestroyObjectImmediate(additionalDataComponent);
                    }
                }
                Undo.DestroyObjectImmediate(component);
            }
            catch
            {
                removed = false;
            }
            finally
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StopAssetEditing();
                }
            }

            return removed;
        }

        public static void RemoveComponent([DisallowNull] Component comp)
        {
            var dependencies = RemoveComponentUtils.ComponentDependencies(comp);
            if (!RemoveComponent(comp, dependencies))
            {
                //preserve built-in behavior
                if (RemoveComponentUtils.CanRemoveComponent(comp, dependencies))
                    Undo.DestroyObjectImmediate(comp);
            }
        }
    }
}
