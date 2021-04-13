namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(UnityEngine.Rendering.Universal.CinemachineUniversalPixelPerfect)), CanEditMultipleObjects]
    internal class CinemachineUniversalPixelPerfectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This Cinemachine extension is now deprecated and doesn't function properly. Instead, use the one from Cinemachine v2.4.0 or newer.", MessageType.Error);
        }
    }
}
