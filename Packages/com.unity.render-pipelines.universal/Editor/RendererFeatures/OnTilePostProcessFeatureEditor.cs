using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(OnTilePostProcessFeature))]
    internal class OnTilePostProcessFeatureEditor : Editor, IOwningRendererDataConsumer
    {
        #region Serialized Properties
        private SerializedProperty m_UseFallbackProperty;
        #endregion

        static class Styles
        {
            public static readonly string k_NoSettingsHelpBox = L10n.Tr("Only pixel local post-processing effects are supported. For example color adjustments, vignette or film grain. There are currently no available settings.");
            public static readonly string k_TileOnlyModeOffWarning = L10n.Tr("Tile-Only Mode is not enabled on this Renderer. This feature will fallback to texture sampling mode. This uses more GPU bandwidth and can reduce performance.");
        }

        private void OnEnable()
        {
        }

        /// <summary>
        /// The renderer data that owns the feature when the inspector is drawn.
        /// </summary>
        public ScriptableRendererData owningRendererData { get; set; }

        public override void OnInspectorGUI()
        {
            var rendererData = (this as IOwningRendererDataConsumer).owningRendererData as UniversalRendererData;
            if (rendererData == null || !rendererData.tileOnlyMode)
                EditorGUILayout.HelpBox(Styles.k_TileOnlyModeOffWarning, MessageType.Warning);
            EditorGUILayout.HelpBox(Styles.k_NoSettingsHelpBox, MessageType.Info);
        }
    }
}
