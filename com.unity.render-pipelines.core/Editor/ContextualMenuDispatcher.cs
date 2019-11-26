using System;
using UnityEngine;


namespace UnityEditor.Rendering
{
    static class ContextualMenuDispatcher
    {
        [MenuItem("CONTEXT/Camera/Remove Component")]
        static void RemoveCameraComponent(MenuCommand command)
        {
            if (!DispatchRemoveComponent(command.context as Camera))
            {
                //preserve built-in behavior
                Undo.DestroyObjectImmediate(command.context);
            }
        }

        static bool DispatchRemoveComponent<T>(T component)
            where T : Component
        {
            Type type = RenderPipelineEditorUtility.FetchFirstCompatibleTypeUsingScriptableRenderPipelineExtension<IRemoveAdditionalDataContextualMenu<T>>();
            if (type != null)
            {
                IRemoveAdditionalDataContextualMenu<T> instance = (IRemoveAdditionalDataContextualMenu<T>)Activator.CreateInstance(type);
                instance.RemoveComponent(component);
                return true;
            }
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
        void RemoveComponent(T component);
    }
}
