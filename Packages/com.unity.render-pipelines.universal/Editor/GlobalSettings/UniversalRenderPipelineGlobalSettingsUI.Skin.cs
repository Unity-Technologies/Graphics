using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    internal partial class UniversalRenderPipelineGlobalSettingsUI
    {
        internal class Styles
        {
            public const int defaultVolumeLabelWidth = 260;

            public static readonly GUIContent defaultVolumeProfileHeaderLabel = EditorGUIUtility.TrTextContent("Default Volume Profile");
            public static readonly GUIContent defaultVolumeProfileLabel = EditorGUIUtility.TrTextContent("Volume Profile", "Settings that will be applied project-wide to all Volumes by default when Universal Render Pipeline is active.");

            public static readonly GUIContent renderingLayersLabel = EditorGUIUtility.TrTextContent("Rendering Layers (3D)", "The list of rendering layer names. When using decals, increasing the layer count inscreases the required memory bandwidth and decreases the performance.");
        }
    }
}
