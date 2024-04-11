using UnityEngine;

namespace UnityEditor.Rendering
{
    public partial class LightUI
    {
        /// <summary>
        /// Styles
        /// </summary>
        public static class Styles
        {
            /// <summary>Title with "General"</summary>
            public static readonly GUIContent generalHeader = EditorGUIUtility.TrTextContent("General");
            /// <summary>Title with "Shape"</summary>
            public static readonly GUIContent shapeHeader = EditorGUIUtility.TrTextContent("Shape");
            /// <summary>Title with "Rendering"</summary>
            public static readonly GUIContent renderingHeader = EditorGUIUtility.TrTextContent("Rendering");
            /// <summary>Title with "Emission"</summary>
            public static readonly GUIContent emissionHeader = EditorGUIUtility.TrTextContent("Emission");
            /// <summary>Title with "Shadows"</summary>
            public static readonly GUIContent shadowHeader = EditorGUIUtility.TrTextContent("Shadows");
            /// <summary>Title with "Light Layer"</summary>
            public static readonly GUIContent lightLayer = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Specifies the Rendering Layers that the Light affects. This Light illuminates Renderers with matching Rendering Layer flags.");

            // Emission
            /// <summary>Label with "Color"</summary>
            public static readonly GUIContent color = EditorGUIUtility.TrTextContent("Color", "Specifies the color this Light emits.");
            /// <summary>Label with "Color"</summary>
            public static readonly GUIContent lightAppearance = EditorGUIUtility.TrTextContent("Light Appearance", "Specifies the mode for this Light's color is calculated.");
            /// <summary>List of the appearance options </summary>
            public static readonly GUIContent[] lightAppearanceOptions = new[]
            {
                EditorGUIUtility.TrTextContent("Color"),
                EditorGUIUtility.TrTextContent("Filter and Temperature")
            };
            /// <summary>List of the appearance units </summary>
            public static readonly GUIContent[] lightAppearanceUnits = new[]
            {
                EditorGUIUtility.TrTextContent("Kelvin")
            };
            /// <summary>Label for color filter</summary>
            public static readonly GUIContent colorFilter = EditorGUIUtility.TrTextContent("Filter", "Specifies a color which tints the Light source.");
            /// <summary>Label for color temperature</summary>
            public static readonly GUIContent colorTemperature = EditorGUIUtility.TrTextContent("Temperature", "Specifies a temperature (in Kelvin) used to correlate a color for the Light. For reference, White is 6500K.");

            /// <summary>When using Preset of Light Component, only a subset of properties are supported.  Unsupported properties are hidden.</summary>
            public static readonly string unsupportedPresetPropertiesMessage = L10n.Tr("When using Preset of Light Component, only a subset of properties are supported.  Unsupported properties are hidden.");

            /// <summary>Label for light intensity (with light units)</summary>
            public static readonly GUIContent lightIntensity = EditorGUIUtility.TrTextContent("Intensity", "Sets the strength of the Light. Use the drop-down to select the light units to use.");

            /// <summary>Label for light's lux at distance property</summary>
            public static readonly GUIContent luxAtDistance = EditorGUIUtility.TrTextContent("At", "Sets the distance, in meters, where a surface receives the amount of light equivalent to the provided number of Lux.");

            /// <summary>Label for light's enable spot reflector property</summary>
            public static readonly GUIContent enableSpotReflector = EditorGUIUtility.TrTextContent("Reflector", "When enabled, simulates a physically correct Spot Light using a reflector. This means the narrower the Outer Angle, the more intense the Spot Light.  When disabled, the intensity of the Light matches the one of a Point Light and thus remains constant regardless of the Outer Angle.");
        }
    }
}
