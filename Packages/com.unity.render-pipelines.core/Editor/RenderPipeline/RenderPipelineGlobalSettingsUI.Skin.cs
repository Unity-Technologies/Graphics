using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// UI for global settings
    /// </summary>
    public static partial class RenderPipelineGlobalSettingsUI
    {
        /// <summary>A collection of GUIContent for use in the inspector</summary>
        public static class Styles
        {
            /// <summary>
            /// Global label width
            /// </summary>
            public const int labelWidth = 220;

            /// <summary>
            /// Shader Stripping
            /// </summary>
            public static readonly GUIContent shaderStrippingSettingsLabel = EditorGUIUtility.TrTextContent("Shader Stripping", "Shader Stripping settings");

            /// <summary>
            /// Shader Variant Log Level
            /// </summary>
            public static readonly GUIContent shaderVariantLogLevelLabel = EditorGUIUtility.TrTextContent("Shader Variant Log Level", "Controls the level of logging of shader variant information outputted during the build process. Information appears in the Unity Console when the build finishes.");

            /// <summary>
            /// Export Shader Variants
            /// </summary>
            public static readonly GUIContent exportShaderVariantsLabel = EditorGUIUtility.TrTextContent("Export Shader Variants", "Controls whether to output shader variant information to a file.");
        }
    }
}
