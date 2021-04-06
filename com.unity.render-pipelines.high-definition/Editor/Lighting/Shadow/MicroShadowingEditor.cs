using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(MicroShadowing))]
    sealed class MicroShadowingEditor : VolumeComponentEditor
    {
        static public readonly string k_DirectionnalWarning = "Micro Shadows only works with directional Lights";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.HelpBox(k_DirectionnalWarning, MessageType.Info);
        }
    }
}
