using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    internal partial class UniversalRenderPipelineGlobalSettingsUI
    {
        internal class Styles
        {
            public static readonly GUIContent renderingLayersLabel = EditorGUIUtility.TrTextContent("Rendering Layers (3D)", "The list of rendering layer names. When using decals, increasing the layer count inscreases the required memory bandwidth and decreases the performance.");
            public static readonly GUIContent stripUnusedPostProcessingVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused Post Processing Variants", "Controls whether strips automatically post processing shader variants based on VolumeProfile components. It strips based on VolumeProfiles in project and not scenes that actually uses it.");
            public static readonly GUIContent stripUnusedVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused Variants", "Controls whether strip disabled keyword variants if the feature is enabled.");

            public static readonly GUIContent stripUnusedLODCrossFadeVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused LOD Cross Fade Variants", "Controls whether strip off variants if the LOD Cross Fade feature is disabled. It strips based on the UniversalRenderingPipelineAsset.enableLODCrossFade property.");
            public static readonly GUIContent stripScreenCoordOverrideVariants = EditorGUIUtility.TrTextContent("Strip Screen Coord Override Variants", "Controls whether Screen Coordinates Override shader variants are stripped.");
        }
    }
}
