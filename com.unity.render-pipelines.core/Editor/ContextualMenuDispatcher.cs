using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    internal static class RemoveComponentUtils
    {
        public static IEnumerable<Component> ComponentDependencies(Component component)
        {
            foreach (var c in component.gameObject.GetComponents<Component>())
            {
                foreach (var rc in c.GetType().GetCustomAttributes(typeof(RequireComponent), true).Cast<RequireComponent>())
                {
                    if (rc.m_Type0 == component.GetType() || rc.m_Type1 == component.GetType() || rc.m_Type2 == component.GetType())
                        yield return c;
                }
            }
        }

        public static bool CanRemoveComponent(Component component, IEnumerable<Component> dependencies)
        {
            if (dependencies.Count() == 0)
                return true;

            Component firstDependency = dependencies.First();
            string error = $"Can't remove {component.GetType().Name} because {firstDependency.GetType().Name} depends on it.";
            EditorUtility.DisplayDialog("Can't remove component", error, "Ok");
            return false;
        }
    }

    /// <summary>
    /// Helper methods for overriding contextual menus
    /// </summary>
    public class ContextualMenuDispatcher
    {
        [MenuItem("CONTEXT/ReflectionProbe/Remove Component")]
        [MenuItem("CONTEXT/Light/Remove Component")]
        [MenuItem("CONTEXT/Camera/Remove Component")]
        static void RemoveComponentWithAdditionalData(MenuCommand command)
        {
            RemoveComponent(command.context as Component);
        }

        /// <summary>
        /// Removes a <see cref="IAdditionalData"/> and it's components defined by <see cref="RequireComponent"/>
        /// </summary>
        /// <typeparam name="T">A <see cref="MonoBehaviour"/> that is an <see cref="IAdditionalData"/></typeparam>
        /// <exception cref="Exception">If the given <see cref="MonoBehaviour"/> is not an <see cref="IAdditionalData"/></exception>
        public static void RemoveAdditionalData<T>(MenuCommand command)
            where T : Component, IAdditionalData
        {
            var additionalData = command.context as IAdditionalData;
            using (ListPool<Type>.Get(out var componentsToRemove))
            {
                if (!TryGetComponentsToRemove(command.context as IAdditionalData, componentsToRemove, out var error))
                    throw error;

                if (EditorUtility.DisplayDialog(
                    title: $"Are you sure you want to proceed?",
                    message: $"This operation will also remove {string.Join($"{Environment.NewLine} - ", componentsToRemove)}.",
                    ok: $"Remove everything",
                    cancel: "Cancel"))
                {
                    RemoveAdditionalDataComponent(additionalData, componentsToRemove);
                }
            }
        }

        internal static bool TryGetComponentsToRemove(
            IAdditionalData additionalData,
            List<Type> componentsToRemove,
            [NotNullWhen(false)] out Exception error)
        {
            var type = additionalData.GetType();
            var requiredComponents = type.GetCustomAttributes(typeof(RequireComponent), true).Cast<RequireComponent>();

            if (!requiredComponents.Any())
            {
                error = new Exception($"Missing attribute {typeof(RequireComponent).FullName} on type {type.FullName}");
                return false;
            }

            foreach (var rc in requiredComponents)
            {
                componentsToRemove.Add(rc.m_Type0);
                if (rc.m_Type1 != null)
                    componentsToRemove.Add(rc.m_Type1);
                if (rc.m_Type2 != null)
                    componentsToRemove.Add(rc.m_Type2);
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Removes the component and it's additional datas
        /// </summary>
        /// <param name="comp">The component</param>
        internal static void RemoveComponent([DisallowNull] Component comp)
        {
            var dependencies = RemoveComponentUtils.ComponentDependencies(comp);
            if (!RemoveAdditionalDataUtils.RemoveComponent(comp, dependencies))
            {
                //preserve built-in behavior
                if (RemoveComponentUtils.CanRemoveComponent(comp, dependencies))
                    Undo.DestroyObjectImmediate(comp);
            }
        }

        internal static void RemoveAdditionalDataComponent([DisallowNull] IAdditionalData additionalData, List<Type> componentsTypeToRemove)
        {
            var additionalDataComponent = additionalData as Component;
            using (ListPool<Component>.Get(out var components))
            {
                // Fetch all components
                foreach (var type in componentsTypeToRemove)
                {
                    components.Add(additionalDataComponent.GetComponent(type));
                }

                // Remove all of them
                foreach (var mono in components)
                {
                    RemoveComponent(mono);
                }
            }
        }
    }

    static class RemoveAdditionalDataUtils
    {
        /// <summary>
        /// Remove the given component
        /// </summary>
        /// <param name="component">The component to remove</param>
        /// <param name="dependencies">Dependencies.</param>
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
    }
}
