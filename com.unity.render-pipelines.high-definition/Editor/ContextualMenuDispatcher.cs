namespace UnityEditor.Rendering.HighDefinition
{
    static class ContextualMenuDispatcher
    {
        [MenuItem("CONTEXT/HDAdditionalReflectionData/Remove Component")]
        [MenuItem("CONTEXT/HDAdditionalCameraData/Remove Component")]
        [MenuItem("CONTEXT/HDAdditionalLightData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            RemoveAdditionalDataUtils.RemoveAdditionalData(command);
        }
    }
}
