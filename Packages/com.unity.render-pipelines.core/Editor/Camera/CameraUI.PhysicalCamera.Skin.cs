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
                /// Focal Length Content
                /// </summary>
                public static readonly GUIContent focalLength = EditorGUIUtility.TrTextContent("Focal Length", "The simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.");

                /// <summary>
                /// Shift content
                /// </summary>
                public static readonly GUIContent shift = EditorGUIUtility.TrTextContent("Shift", "Offset from the camera sensor. Use these properties to simulate a shift lens. Measured as a multiple of the sensor size.");
            }
        }
    }
}
