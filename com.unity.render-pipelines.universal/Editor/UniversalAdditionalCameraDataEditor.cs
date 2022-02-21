using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    // Disable the GUI for additional camera data
    [CustomEditor(typeof(UniversalAdditionalCameraData))]
    class UniversalAdditionalCameraDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }

        [MenuItem("CONTEXT/UniversalAdditionalCameraData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            ContextualMenuDispatcher.RemoveAdditionalData<UniversalAdditionalCameraData>(command);
        }
    }
}
