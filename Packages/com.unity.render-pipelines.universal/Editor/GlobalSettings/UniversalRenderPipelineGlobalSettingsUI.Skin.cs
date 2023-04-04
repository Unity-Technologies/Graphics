using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    internal partial class UniversalRenderPipelineGlobalSettingsUI
    {
        internal class Styles
        {
            public const int labelWidth = 250;

            public static readonly GUIContent defaultVolumeProfileHeaderLabel = EditorGUIUtility.TrTextContent("Render Pipeline Default Volume Profile");
            public static readonly GUIContent defaultVolumeProfileLabel = EditorGUIUtility.TrTextContent("Render Pipeline Default Volume Profile");
            public static readonly GUIContent newVolumeProfileLabel = EditorGUIUtility.TrTextContent("New", "Create a new Render Pipeline Default Volume Profile");

            public static readonly GUIContent renderingLayersLabel = EditorGUIUtility.TrTextContent("Rendering Layers (3D)", "The list of rendering layer names. When using decals, increasing the layer count inscreases the required memory bandwidth and decreases the performance.");
        }
    }
}
