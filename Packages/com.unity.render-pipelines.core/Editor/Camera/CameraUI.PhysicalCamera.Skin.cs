using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public static partial class CameraUI
    {
        public static partial class PhysicalCamera
        {
            /// <summary>
            /// Styles
            /// </summary>
            public static class Styles
            {
                // Camera Body
                /// <summary>
                /// Camera Body content
                /// </summary>
                public static readonly GUIContent cameraBody = L10n.TextContent("Camera Body", null, null, null);

                /// <summary>
                /// Sensor type content
                /// </summary>
                public static readonly GUIContent sensorType = L10n.TextContent("Sensor Type", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.", null, null);

                /// <summary>
                /// Aperture format names
                /// </summary>
                public static readonly string[] apertureFormatNames = CameraEditor.Settings.ApertureFormatNames.ToArray();

                /// <summary>
                /// Aperture format values
                /// </summary>
                public static readonly Vector2[] apertureFormatValues = CameraEditor.Settings.ApertureFormatValues.ToArray();

                /// <summary>
                /// Custom preset index
                /// </summary>
                public static readonly int customPresetIndex = apertureFormatNames.Length - 1;

                /// <summary>
                /// Sensor size
                /// </summary>
                public static readonly GUIContent sensorSize = L10n.TextContent("Sensor Size", "The size of the camera sensor in millimeters.", null, null);

                /// <summary>
                /// Gate Fit
                /// </summary>
                public static readonly GUIContent gateFit = L10n.TextContent("Gate Fit", "Determines how the rendered area (resolution gate) fits into the sensor area (film gate).", null, null);

                // Lens
                /// <summary>
                /// Lens content
                /// </summary>
                public static readonly GUIContent lens = L10n.TextContent("Lens", null, null, null);

                /// <summary>
                /// Focal Length content
                /// </summary>
                public static readonly GUIContent focalLength = L10n.TextContent("Focal Length", "The simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.", null, null);

                /// <summary>
                /// Shift content
                /// </summary>
                public static readonly GUIContent shift = L10n.TextContent("Shift", "Offset from the camera sensor. Use these properties to simulate a shift lens. Measured as a multiple of the sensor size.", null, null);

                /// <summary>
                /// ISO content
                /// </summary>
                public static readonly GUIContent ISO = L10n.TextContent("ISO", "Sets the light sensitivity of the Camera sensor. This property affects Exposure if you set its Mode to Use Physical Camera.", null, null);

                /// <summary>
                /// Shutter Speed content
                /// </summary>
                public static readonly GUIContent shutterSpeed = L10n.TextContent("Shutter Speed", "The amount of time the Camera sensor is capturing light.", null, null);

                /// <summary>
                /// Aperture content
                /// </summary>
                public static readonly GUIContent aperture = L10n.TextContent("Aperture", "The f-stop (f-number) of the lens. Lower values give a wider lens aperture.", null, null);

                /// <summary>
                /// Focus Distance content
                /// </summary>
                public static readonly GUIContent focusDistance = L10n.TextContent("Focus Distance", "The distance from the camera where objects appear sharp when Depth Of Field is enabled.", null, null);

                // Aperture Shape

                /// <summary>
                /// Aperture Shape content
                /// </summary>
                public static readonly GUIContent apertureShape = L10n.TextContent("Aperture Shape", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.", null, null);

                /// <summary>
                /// Blade Count content
                /// </summary>
                public static readonly GUIContent bladeCount = L10n.TextContent("Blade Count", "The number of blades in the lens aperture. Higher values give a rounder aperture shape.", null, null);

                /// <summary>
                /// Curvature content
                /// </summary>
                public static readonly GUIContent curvature = L10n.TextContent("Curvature", "Controls the curvature of the lens aperture blades. The minimum value results in fully-curved, perfectly-circular bokeh, and the maximum value results in visible aperture blades.", null, null);

                /// <summary>
                /// Barrel Clipping content
                /// </summary>
                public static readonly GUIContent barrelClipping = L10n.TextContent("Barrel Clipping", "Controls the self-occlusion of the lens, creating a cat's eye effect.", null, null);

                /// <summary>
                /// Anamorphism content
                /// </summary>
                public static readonly GUIContent anamorphism = L10n.TextContent("Anamorphism", "Use the slider to stretch the sensor to simulate an anamorphic look.", null, null);
            }
        }
    }
}
