using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    internal static class RemoveComponentUtils
    {
        public static IEnumerable<Component> ComponentDependencies(Component component)
           => component.gameObject
           .GetComponents<Component>()
           .Where(c => c != component
               && c.GetType()
                   .GetCustomAttributes(typeof(RequireComponent), true)
                   .Count(att => att is RequireComponent rc
                       && (rc.m_Type0 == component.GetType()
                           || rc.m_Type1 == component.GetType()
                           || rc.m_Type2 == component.GetType())) > 0);

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
    public static class ContextualMenuDispatcher
    {
        [MenuItem("CONTEXT/ReflectionProbe/Remove Component")]
        static void RemoveReflectionProbeComponent(MenuCommand command)
        {
            RemoveComponent<ReflectionProbe>(command);
        }

        [MenuItem("CONTEXT/Light/Remove Component")]
        static void RemoveLightComponent(MenuCommand command)
        {
            RemoveComponent<Light>(command);
        }

        [MenuItem("CONTEXT/Camera/Remove Component")]
        static void RemoveCameraComponent(MenuCommand command)
        {
            RemoveComponent<Camera>(command);
        }

        /// <summary>
        /// Removes a <see cref="IAdditionalData"/> and it's components defined by <see cref="RequireComponent"/>
        /// </summary>
        /// <typeparam name="T">A <see cref="MonoBehaviour"/> that is an <see cref="IAdditionalData"/></typeparam>
        /// <exception cref="Exception">If the given <see cref="MonoBehaviour"/> is not an <see cref="IAdditionalData"/></exception>
        public static void RemoveAdditionalData<T>(MenuCommand command)
            where T : Component, IAdditionalData
        {
            if (typeof(T).GetCustomAttributes(typeof(RequireComponent), true).FirstOrDefault() is RequireComponent rc)
            {
                using (ListPool<Type>.Get(out var componentsToRemove))
                {
                    componentsToRemove.Add(rc.m_Type0);
                    if (rc.m_Type1 != null)
                        componentsToRemove.Add(rc.m_Type1);
                    if (rc.m_Type2 != null)
                        componentsToRemove.Add(rc.m_Type2);

                    if (EditorUtility.DisplayDialog(
                        title: $"Are you sure you want to proceed?",
                        message: $"This operation will also remove {string.Join($"{Environment.NewLine} - ", componentsToRemove)}.",
                        ok: $"Remove everything",
                        cancel: "Cancel"))
                    {
                        T additionalData = command.context as T;
                        foreach (var type in componentsToRemove)
                        {
                            RemoveComponent(type, additionalData.GetComponent(type));
                        }
                    }
                }
            }
            else
                throw new Exception($"Missing attribute {typeof(RequireComponent).FullName} on type {typeof(T).FullName}");
        }

        static void RemoveComponent<T>(MenuCommand command)
            where T : Component
        {
            T comp = command.context as T;
            RemoveComponent(typeof(T), comp);
        }

        /// <summary>
        /// Removes the component and it's additional datas
        /// </summary>
        /// <param name="type">Type to remove</param>
        /// <param name="comp">The component</param>
        public static void RemoveComponent(Type type, Component comp)
        {
            if (!RemoveAdditionalDataUtils.RemoveComponent(type, comp, RemoveComponentUtils.ComponentDependencies(comp)))
            {
                //preserve built-in behavior
                if (RemoveComponentUtils.CanRemoveComponent(comp, RemoveComponentUtils.ComponentDependencies(comp)))
                    Undo.DestroyObjectImmediate(comp);
            }
        }
    }

    static class RemoveAdditionalDataUtils
    {
        /// <summary>
        /// Remove the given component
        /// </summary>
        /// <param name="type">The type of the component</param>
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
                Undo.SetCurrentGroupName($"Remove {type} and additional data components");

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
