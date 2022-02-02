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
    }
}
