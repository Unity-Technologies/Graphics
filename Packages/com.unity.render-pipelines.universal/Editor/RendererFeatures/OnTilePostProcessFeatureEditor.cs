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
            public static readonly string k_NoSettingsHelpBox = L10n.Tr("Only pixel local post-processing effects are supported (for example color adjustments, vignette or film grain). There are currently no additional settings.", null);
            public static readonly string k_BuiltInPostProcessingIncompatible = L10n.Tr("The UniversalRendererData has post processing enabled, which conflicts with this feature. Only one form of post processing should be enabled. This feature will not be added.", null);
            public static readonly string k_TileOnlyModeOffWarning = L10n.Tr("Tile-Only Mode is not enabled on this Renderer. This feature will fallback to texture sampling mode. This uses more GPU bandwidth and can reduce performance.", null);
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
            {
                if (rendererData != null && rendererData.postProcessData != null)
                    EditorGUILayout.HelpBox(Styles.k_BuiltInPostProcessingIncompatible + "\n\n" + Styles.k_NoSettingsHelpBox, MessageType.Error);
                else
                    EditorGUILayout.HelpBox(Styles.k_TileOnlyModeOffWarning + "\n\n" + Styles.k_NoSettingsHelpBox, MessageType.Warning);
            }
            else
                EditorGUILayout.HelpBox(Styles.k_NoSettingsHelpBox, MessageType.Info);
        }
    }
}
