using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    internal partial class UniversalRenderPipelineGlobalSettingsUI
    {
        internal class Styles
        {
            public static readonly GUIContent lightLayersLabel = EditorGUIUtility.TrTextContent("Light Layer Names (3D)", "If the Light Layers feature is enabled in the URP Asset, Unity allocates memory for processing Light Layers. In the Deferred Rendering Path, this allocation includes an extra render target in GPU memory, which reduces performance.");
            public static readonly GUIContent lightLayerName0 = EditorGUIUtility.TrTextContent("Light Layer 0", "The display name for Light Layer 0.");
            public static readonly GUIContent lightLayerName1 = EditorGUIUtility.TrTextContent("Light Layer 1", "The display name for Light Layer 1.");
            public static readonly GUIContent lightLayerName2 = EditorGUIUtility.TrTextContent("Light Layer 2", "The display name for Light Layer 2.");
            public static readonly GUIContent lightLayerName3 = EditorGUIUtility.TrTextContent("Light Layer 3", "The display name for Light Layer 3.");
            public static readonly GUIContent lightLayerName4 = EditorGUIUtility.TrTextContent("Light Layer 4", "The display name for Light Layer 4.");
            public static readonly GUIContent lightLayerName5 = EditorGUIUtility.TrTextContent("Light Layer 5", "The display name for Light Layer 5.");
            public static readonly GUIContent lightLayerName6 = EditorGUIUtility.TrTextContent("Light Layer 6", "The display name for Light Layer 6.");
            public static readonly GUIContent lightLayerName7 = EditorGUIUtility.TrTextContent("Light Layer 7", "The display name for Light Layer 7.");

            public static readonly GUIContent decalLayersLabel = EditorGUIUtility.TrTextContent("Decal Layer Names (3D)", "If the Decal Layers feature is enabled in the URP Asset, Unity allocates memory for processing Decal Layers. In the Deferred Rendering Path, this allocation includes an extra render target in GPU memory, which reduces performance.");
            public static readonly GUIContent decalLayerName0 = EditorGUIUtility.TrTextContent("Decal Layer 0", "The display name for Decal Layer 0.");
            public static readonly GUIContent decalLayerName1 = EditorGUIUtility.TrTextContent("Decal Layer 1", "The display name for Decal Layer 1.");
            public static readonly GUIContent decalLayerName2 = EditorGUIUtility.TrTextContent("Decal Layer 2", "The display name for Decal Layer 2.");
            public static readonly GUIContent decalLayerName3 = EditorGUIUtility.TrTextContent("Decal Layer 3", "The display name for Decal Layer 3.");
            public static readonly GUIContent decalLayerName4 = EditorGUIUtility.TrTextContent("Decal Layer 4", "The display name for Decal Layer 4.");
            public static readonly GUIContent decalLayerName5 = EditorGUIUtility.TrTextContent("Decal Layer 5", "The display name for Decal Layer 5.");
            public static readonly GUIContent decalLayerName6 = EditorGUIUtility.TrTextContent("Decal Layer 6", "The display name for Decal Layer 6.");
            public static readonly GUIContent decalLayerName7 = EditorGUIUtility.TrTextContent("Decal Layer 7", "The display name for Decal Layer 7.");

            public static readonly GUIContent stripDebugVariantsLabel = EditorGUIUtility.TrTextContent("Strip Debug Variants", "When disabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.");
            public static readonly GUIContent stripUnusedPostProcessingVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused Post Processing Variants", "Controls whether strips automatically post processing shader variants based on VolumeProfile components. It strips based on VolumeProfiles in project and not scenes that actually uses it.");
            public static readonly GUIContent stripUnusedVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused Variants", "Controls whether strip disabled keyword variants if the feature is enabled.");

            public static readonly GUIContent enableRenderGraphLabel = EditorGUIUtility.TrTextContent("Use Render Graph", "");
            public static readonly GUIContent stripUnusedLODCrossFadeVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused LOD Cross Fade Variants", "Controls whether strip off variants if the LOD Cross Fade feature is disabled. It strips based on the UniversalRenderingPipelineAsset.enableLODCrossFade property.");
        }
    }
}
