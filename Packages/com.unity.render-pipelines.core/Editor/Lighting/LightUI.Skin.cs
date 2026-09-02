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
            public static readonly GUIContent generalHeader = L10n.TextContent("General", null, null, null);
            /// <summary>Title with "Shape"</summary>
            public static readonly GUIContent shapeHeader = L10n.TextContent("Shape", null, null, null);
            /// <summary>Title with "Rendering"</summary>
            public static readonly GUIContent renderingHeader = L10n.TextContent("Rendering", null, null, null);
            /// <summary>Title with "Emission"</summary>
            public static readonly GUIContent emissionHeader = L10n.TextContent("Emission", null, null, null);
            /// <summary>Title with "Shadows"</summary>
            public static readonly GUIContent shadowHeader = L10n.TextContent("Shadows", null, null, null);
            /// <summary>Title with "Light Layer"</summary>
            public static readonly GUIContent lightLayer = L10n.TextContent("Rendering Layer Mask", "Specifies the Rendering Layers that the Light affects. This Light illuminates Renderers with matching Rendering Layer flags.", null, null);

            // Emission
            /// <summary>Label with "Color"</summary>
            public static readonly GUIContent color = L10n.TextContent("Color", "Specifies the color this Light emits.", null, null);
            /// <summary>Label with "Color"</summary>
            public static readonly GUIContent lightAppearance = L10n.TextContent("Light Appearance", "Specifies the mode for this Light's color is calculated.", null, null);
            /// <summary>List of the appearance options </summary>
            public static readonly GUIContent[] lightAppearanceOptions = new[]
            {
                L10n.TextContent("Color", null, null, null),
                L10n.TextContent("Filter and Temperature", null, null, null)
            };
            /// <summary>List of the appearance units </summary>
            public static readonly GUIContent[] lightAppearanceUnits = new[]
            {
                L10n.TextContent("Kelvin", null, null, null)
            };
            /// <summary>Label for color filter</summary>
            public static readonly GUIContent colorFilter = L10n.TextContent("Filter", "Specifies a color which tints the Light source.", null, null);
            /// <summary>Label for color temperature</summary>
            public static readonly GUIContent colorTemperature = L10n.TextContent("Temperature", "Specifies a temperature (in Kelvin) used to correlate a color for the Light. For reference, White is 6500K.", null, null);

            /// <summary>When using Preset of Light Component, only a subset of properties are supported.  Unsupported properties are hidden.</summary>
            public static readonly string unsupportedPresetPropertiesMessage = L10n.Tr("When using Preset of Light Component, only a subset of properties are supported.  Unsupported properties are hidden.", null);

            /// <summary>Label for light intensity (with light units)</summary>
            public static readonly GUIContent lightIntensity = L10n.TextContent("Intensity", "Sets the strength of the Light. Use the drop-down to select the light units to use.", null, null);

            /// <summary>Label for light's lux at distance property</summary>
            public static readonly GUIContent luxAtDistance = L10n.TextContent("At", "Sets the distance, in meters, where a surface receives the amount of light equivalent to the provided number of Lux.", null, null);

            /// <summary>Label for light's enable spot reflector property</summary>
            public static readonly GUIContent enableSpotReflector = L10n.TextContent("Reflector", "When enabled, simulates a physically correct Spot Light using a reflector. This means the narrower the Outer Angle, the more intense the Spot Light.  When disabled, the intensity of the Light matches the one of a Point Light and thus remains constant regardless of the Outer Angle.", null, null);
        }
    }
}
