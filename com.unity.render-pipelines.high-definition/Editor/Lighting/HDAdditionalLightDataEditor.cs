using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HDAdditionalLightData))]
    class HDAdditionalLightDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }

        [MenuItem("CONTEXT/HDAdditionalLightData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            ContextualMenuDispatcher.RemoveAdditionalData<HDAdditionalLightData>(command);
        }
    }
}
