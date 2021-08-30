using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UnityEditor.Rendering
{
    static class ContextualMenuDispatcher
    {
        [MenuItem("CONTEXT/Camera/Remove Component")]
        static void RemoveCameraComponent(MenuCommand command)
        {
            Camera camera = command.context as Camera;
            string error;

            if (!DispatchRemoveComponent(camera))
            {
                //preserve built-in behavior
                if (CanRemoveComponent(camera, out error))
                    Undo.DestroyObjectImmediate(command.context);
                else
                    EditorUtility.DisplayDialog("Can't remove component", error, "Ok");
            }
        }

        static bool DispatchRemoveComponent<T>(T component)
            where T : Component
        {
            Type type = RenderPipelineEditorUtility.FetchFirstCompatibleTypeUsingScriptableRenderPipelineExtension<IRemoveAdditionalDataContextualMenu<T>>();
            if (type != null)
            {
                IRemoveAdditionalDataContextualMenu<T> instance = (IRemoveAdditionalDataContextualMenu<T>)Activator.CreateInstance(type);
                instance.RemoveComponent(component, ComponentDependencies(component));
                return true;
            }
            return false;
        }

        static IEnumerable<Component> ComponentDependencies(Component component)
            => component.gameObject
            .GetComponents<Component>()
            .Where(c => c != component
                && c.GetType()
                    .GetCustomAttributes(typeof(RequireComponent), true)
                    .Count(att => att is RequireComponent rc
                        && (rc.m_Type0 == component.GetType()
                            || rc.m_Type1 == component.GetType()
                            || rc.m_Type2 == component.GetType())) > 0);

        static bool CanRemoveComponent(Component component, out string error)
        {
            var dependencies = ComponentDependencies(component);
            if (dependencies.Count() == 0)
            {
                error = null;
                return true;
            }

            Component firstDependency = dependencies.First();
            error = $"Can't remove {component.GetType().Name} because {firstDependency.GetType().Name} depends on it.";
            return false;
        }
    }

    /// <summary>
    /// Interface that should be used with [ScriptableRenderPipelineExtension(type))] attribute to dispatch ContextualMenu calls on the different SRPs
    /// </summary>
    /// <typeparam name="T">This must be a component that require AdditionalData in your SRP</typeparam>
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
}
