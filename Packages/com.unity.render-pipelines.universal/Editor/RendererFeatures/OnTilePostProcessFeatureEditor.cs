namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(OnTilePostProcessFeature))]
    internal class OnTilePostProcessFeatureEditor : Editor
    {
        #region Serialized Properties
        private SerializedProperty m_UseFallbackProperty;
        #endregion

        static class Styles
        {
            public static readonly string k_NoSettingsHelpBox = L10n.Tr("This feature performs post-processing operation in tile memory. There are currently no available settings, they might be added later.");
            public static readonly string k_NeedsOnTileValidation = L10n.Tr("On Tile PostProcessing feature needs the 'On Tile Validation' set on the Renderer. Otherwise, this render feature will fallback to texture sampling mode (slow off-tile rendering)");
        }

        private void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(Styles.k_NeedsOnTileValidation, MessageType.Info);
            EditorGUILayout.HelpBox(Styles.k_NoSettingsHelpBox, MessageType.Info);
        }
    }
}
