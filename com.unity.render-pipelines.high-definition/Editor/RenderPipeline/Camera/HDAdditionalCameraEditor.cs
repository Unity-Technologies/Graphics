using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HDAdditionalCameraData))]
    class HDAdditionalCameraDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }

        [MenuItem("CONTEXT/HDAdditionalCameraData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            ContextualMenuDispatcher.RemoveAdditionalData<HDAdditionalCameraData>(command);
        }
    }
}
