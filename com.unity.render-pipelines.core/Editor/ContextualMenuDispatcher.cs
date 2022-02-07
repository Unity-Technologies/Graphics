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
    static class ContextualMenuDispatcher
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

        [InitializeOnLoadMethod]
        static void RegisterAdditionalDataMenus()
        {
            foreach (var additionalData in TypeCache.GetTypesDerivedFrom<IAdditionalData>())
            {
                if (additionalData.GetCustomAttributes(typeof(RequireComponent), true).FirstOrDefault() is RequireComponent rc)
                {
                    string types = rc.m_Type0.Name;
                    if (rc.m_Type1 != null)
                        types += $", {rc.m_Type1.Name}";
                    if (rc.m_Type2 != null)
                        types += $", {rc.m_Type2.Name}";

                    MenuManager.AddMenuItem($"CONTEXT/{additionalData.Name}/Remove Component",
                        string.Empty,
                        false,
                        0,
                        () => EditorUtility.DisplayDialog($"Remove {additionalData.Name} is blocked", $"You can not delete this component, you will have to remove the {types}.", "OK"),
                        () => true);
                }
            }
        }

        static void RemoveComponent<T>(MenuCommand command)
            where T : Component
        {
            T comp = command.context as T;

            if (!DispatchRemoveComponent<T>(comp))
            {
                //preserve built-in behavior
                if (RemoveComponentUtils.CanRemoveComponent(comp, RemoveComponentUtils.ComponentDependencies(comp)))
                    Undo.DestroyObjectImmediate(command.context);
            }
        }

        static bool DispatchRemoveComponent<T>(T component)
            where T : Component
        {
            try
            {
                var instance = new RemoveAdditionalDataContextualMenu<T>();
                instance.RemoveComponent(component, RemoveComponentUtils.ComponentDependencies(component));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Interface that should be used with [ScriptableRenderPipelineExtension(type))] attribute to dispatch ContextualMenu calls on the different SRPs
    /// </summary>
    /// <typeparam name="T">This must be a component that require AdditionalData in your SRP</typeparam>
    [Obsolete("The menu items are handled automatically for components with the AdditionalComponentData attribute", false)]
    public interface IRemoveAdditionalDataContextualMenu<T>
        where T : Component
    {
        /// <summary>
        /// Remove the given component
        /// </summary>
        /// <param name="component">The component to remove</param>
        /// <param name="dependencies">Dependencies.</param>
        void RemoveComponent(T component, IEnumerable<Component> dependencies);
    }

    internal class RemoveAdditionalDataContextualMenu<T>
        where T : Component
    {
        /// <summary>
        /// Remove the given component
        /// </summary>
        /// <param name="component">The component to remove</param>
        /// <param name="dependencies">Dependencies.</param>
        public void RemoveComponent(T component, IEnumerable<Component> dependencies)
        {
            var additionalDatas = dependencies
                .Where(c => c != component && typeof(IAdditionalData).IsAssignableFrom(c.GetType()))
                .ToList();

            if (!RemoveComponentUtils.CanRemoveComponent(component, dependencies.Where(c => !additionalDatas.Contains(c))))
                return;

            var isAssetEditing = EditorUtility.IsPersistent(component);
            try
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StartAssetEditing();
                }
                Undo.SetCurrentGroupName($"Remove {typeof(T)} additional data components");

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
            finally
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }
    }
}
