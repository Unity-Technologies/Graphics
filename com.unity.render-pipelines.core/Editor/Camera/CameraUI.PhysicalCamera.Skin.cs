using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary> Camera UI Shared Properties among SRP</summary>
    public static partial class CameraUI
    {
        /// <summary>
        /// Physical camera content content
        /// </summary>
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
                public static readonly GUIContent cameraBody = EditorGUIUtility.TrTextContent("Camera Body");

                /// <summary>
                /// Sensor type content
                /// </summary>
                public static readonly GUIContent sensorType = EditorGUIUtility.TrTextContent("Sensor Type", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");

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
                public static readonly GUIContent sensorSize = EditorGUIUtility.TrTextContent("Sensor Size", "The size of the camera sensor in millimeters.");

                /// <summary>
                /// Gate Fit
                /// </summary>
                public static readonly GUIContent gateFit = EditorGUIUtility.TrTextContent("Gate Fit", "Determines how the rendered area (resolution gate) fits into the sensor area (film gate).");

                // Lens
                /// <summary>
                /// Lens content
                /// </summary>
                public static readonly GUIContent lens = EditorGUIUtility.TrTextContent("Lens");

                /// <summary>
                /// Focal Length content
                /// </summary>
                public static readonly GUIContent focalLength = EditorGUIUtility.TrTextContent("Focal Length", "The simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.");

                /// <summary>
                /// Shift content
                /// </summary>
                public static readonly GUIContent shift = EditorGUIUtility.TrTextContent("Shift", "Offset from the camera sensor. Use these properties to simulate a shift lens. Measured as a multiple of the sensor size.");

                /// <summary>
                /// ISO content
                /// </summary>
                public static readonly GUIContent ISO = EditorGUIUtility.TrTextContent("ISO", "Sets the light sensitivity of the Camera sensor. This property affects Exposure if you set its Mode to Use Physical Camera.");

                /// <summary>
                /// Shutter Speed content
                /// </summary>
                public static readonly GUIContent shutterSpeed = EditorGUIUtility.TrTextContent("Shutter Speed", "The amount of time the Camera sensor is capturing light.");

                /// <summary>
                /// Aperture content
                /// </summary>
                public static readonly GUIContent aperture = EditorGUIUtility.TrTextContent("Aperture", "The f-stop (f-number) of the lens. Lower values give a wider lens aperture.");

                /// <summary>
                /// Focus Distance content
                /// </summary>
                public static readonly GUIContent focusDistance = EditorGUIUtility.TrTextContent("Focus Distance", "The distance from the camera where objects appear sharp when Depth Of Field is enabled.");

                // Aperture Shape

                /// <summary>
                /// Aperture Shape content
                /// </summary>
                public static readonly GUIContent apertureShape = EditorGUIUtility.TrTextContent("Aperture Shape", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");

                /// <summary>
                /// Blade Count content
                /// </summary>
                public static readonly GUIContent bladeCount = EditorGUIUtility.TrTextContent("Blade Count", "The number of blades in the lens aperture. Higher values give a rounder aperture shape.");

                /// <summary>
                /// Curvature content
                /// </summary>
                public static readonly GUIContent curvature = EditorGUIUtility.TrTextContent("Curvature", "Controls the curvature of the lens aperture blades. The minimum value results in fully-curved, perfectly-circular bokeh, and the maximum value results in visible aperture blades.");

                /// <summary>
                /// Barrel Clipping content
                /// </summary>
                public static readonly GUIContent barrelClipping = EditorGUIUtility.TrTextContent("Barrel Clipping", "Controls the self-occlusion of the lens, creating a cat's eye effect.");

                /// <summary>
                /// Anamorphism content
                /// </summary>
                public static readonly GUIContent anamorphism = EditorGUIUtility.TrTextContent("Anamorphism", "Use the slider to stretch the sensor to simulate an anamorphic look.");
            }
        }
    }
}
