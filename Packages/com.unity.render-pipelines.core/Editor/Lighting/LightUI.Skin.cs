using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Contains a set of methods to help render the inspectors of Lights across SRP's
    /// </summary>
    public partial class LightUI
    {
        /// <summary>
        /// Styles
        /// </summary>
        public static class Styles
        {
            /// <summary>
            /// General
            /// </summary>
            public static readonly GUIContent generalHeader = EditorGUIUtility.TrTextContent("General");

            /// <summary>
            /// Shape
            /// </summary>
            public static readonly GUIContent shapeHeader = EditorGUIUtility.TrTextContent("Shape");

            /// <summary>
            /// Rendering
            /// </summary>
            public static readonly GUIContent renderingHeader = EditorGUIUtility.TrTextContent("Rendering");

            /// <summary>
            /// Emission
            /// </summary>
            public static readonly GUIContent emissionHeader = EditorGUIUtility.TrTextContent("Emission");

            /// <summary>
            /// Shadows
            /// </summary>
            public static readonly GUIContent shadowHeader = EditorGUIUtility.TrTextContent("Shadows");

            /// <summary>
            /// Light Layer
            /// </summary>
            public static readonly GUIContent lightLayer = EditorGUIUtility.TrTextContent("Light Layer", "Specifies the current Light Layers that the Light affects. This Light illuminates corresponding Renderers with the same Light Layer flags.");

            // Emission

            /// <summary>
            /// Color
            /// </summary>
            public static readonly GUIContent color = EditorGUIUtility.TrTextContent("Color", "Specifies the color this Light emits.");


            /// <summary>
            /// Light Appearance
            /// </summary>
            public static readonly GUIContent lightAppearance = EditorGUIUtility.TrTextContent("Light Appearance", "Specifies the mode for this Light's color is calculated.");

            /// <summary>
            /// Options for Light Appearance
            /// </summary>
            public static readonly GUIContent[] lightAppearanceOptions = new[]
            {
                EditorGUIUtility.TrTextContent("Color"),
                EditorGUIUtility.TrTextContent("Filter and Temperature")
            };

            /// <summary>
            /// Units for Light Appearance
            /// </summary>
            public static readonly GUIContent[] lightAppearanceUnits = new[]
            {
                EditorGUIUtility.TrTextContent("Kelvin")
            };

            /// <summary>
            /// The color filter
            /// </summary>
            public static readonly GUIContent colorFilter = EditorGUIUtility.TrTextContent("Filter", "Specifies a color which tints the Light source.");

            /// <summary>
            /// Color Temperature
            /// </summary>
            public static readonly GUIContent colorTemperature = EditorGUIUtility.TrTextContent("Temperature", "Specifies a temperature (in Kelvin) used to correlate a color for the Light. For reference, White is 6500K.");

            /// <summary>
            /// When using Preset of Light Component, only a subset of properties are supported.  Unsupported properties are hidden.
            /// </summary>
            public static readonly string unsupportedPresetPropertiesMessage = L10n.Tr("When using Preset of Light Component, only a subset of properties are supported.  Unsupported properties are hidden.");
        }
    }
}
