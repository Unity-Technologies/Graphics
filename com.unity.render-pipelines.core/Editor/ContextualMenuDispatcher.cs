using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Helper methods for overriding contextual menus
    /// </summary>
    static class ContextualMenuDispatcher
    {
        [MenuItem("CONTEXT/ReflectionProbe/Remove Component")]
        [MenuItem("CONTEXT/Light/Remove Component")]
        [MenuItem("CONTEXT/Camera/Remove Component")]
        static void RemoveComponentWithAdditionalData(MenuCommand command)
        {
            RemoveComponentUtils.RemoveComponent(command.context as Component);
        }
    }
}
