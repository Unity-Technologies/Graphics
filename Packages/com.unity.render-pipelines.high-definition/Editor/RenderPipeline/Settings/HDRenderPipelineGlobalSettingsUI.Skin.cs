using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    internal partial class HDRenderPipelineGlobalSettingsUI
    {
        internal class Styles
        {
            public const int labelWidth = 220;
            public const int defaultVolumeLabelWidth = 260;

            public static readonly GUIContent defaultVolumeProfileSectionLabel = EditorGUIUtility.TrTextContent("Default Volume Profile");
            public static readonly GUIContent defaultVolumeProfileAssetLabel = EditorGUIUtility.TrTextContent("Volume Profile", "Settings that will be applied project-wide to all Volumes by default when High Definition Render Pipeline is active.");
            public static readonly GUIContent lookDevVolumeProfileSectionLabel = EditorGUIUtility.TrTextContent("LookDev Volume Profile");
            public static readonly GUIContent lookDevVolumeProfileAssetLabel = EditorGUIUtility.TrTextContent("Volume Profile");

            public static readonly GUIContent frameSettingsLabel = EditorGUIUtility.TrTextContent("Frame Settings (Default Values)");

            public static readonly GUIContent customPostProcessOrderLabel = EditorGUIUtility.TrTextContent("Custom Post Process Orders");

            public static readonly GUIContent resourceLabel = EditorGUIUtility.TrTextContent("Resources");
            public static readonly GUIContent renderPipelineResourcesContent = EditorGUIUtility.TrTextContent("Player Resources", "Set of resources that need to be loaded when creating stand alone");
            public static readonly GUIContent renderPipelineEditorResourcesContent = EditorGUIUtility.TrTextContent("Editor Resources", "Set of resources that need to be loaded for working in editor");

            public static readonly GUIContent generalSettingsLabel = EditorGUIUtility.TrTextContent("Miscellaneous");

            public static readonly GUIContent defaultRenderingLayerMaskLabel = EditorGUIUtility.TrTextContent("Default Mesh Rendering Layer Mask", "The Default Rendering Layer Mask for newly created Renderers.");
            public static readonly GUIContent renderingLayersLabel = EditorGUIUtility.TrTextContent("Rendering Layers");
            public static readonly GUIContent renderingLayerNamesLabel = EditorGUIUtility.TrTextContent("Rendering Layer Names");
        }
    }
}
