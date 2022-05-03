using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static class ContextualMenuDispatcher
    {
        [MenuItem("CONTEXT/UniversalAdditionalCameraData/Remove Component")]
        [MenuItem("CONTEXT/UniversalAdditionalLightData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            RemoveAdditionalDataUtils.RemoveAdditionalData(command);
        }
    }
}
