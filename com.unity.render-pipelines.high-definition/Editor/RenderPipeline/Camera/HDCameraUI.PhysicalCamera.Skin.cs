using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        partial class PhysicalCamera
        {
            class Styles
            {
                // Camera Body
                public static readonly GUIContent cameraBody = EditorGUIUtility.TrTextContent("Camera Body");
                public static readonly GUIContent sensorType = EditorGUIUtility.TrTextContent("Sensor Type", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");
                public static readonly string[] apertureFormatNames = CameraEditor.Settings.ApertureFormatNames.ToArray();
                public static readonly Vector2[] apertureFormatValues = CameraEditor.Settings.ApertureFormatValues.ToArray();
                public static readonly int customPresetIndex = apertureFormatNames.Length - 1;
                public static readonly GUIContent ISO = EditorGUIUtility.TrTextContent("ISO", "Sets the light sensitivity of the Camera sensor. This property affects Exposure if you set its Mode to Use Physical Camera.");
                public static readonly GUIContent shutterSpeed = EditorGUIUtility.TrTextContent("Shutter Speed", "The amount of time the Camera sensor is capturing light.");

                public static readonly GUIContent sensorSize = EditorGUIUtility.TrTextContent("Sensor Size", "The size of the camera sensor in millimeters.");
                public static readonly GUIContent gateFit = EditorGUIUtility.TrTextContent("Gate Fit", "Determines how the rendered area (resolution gate) fits into the sensor area (film gate).");

                // Lens
                public static readonly GUIContent lens = EditorGUIUtility.TrTextContent("Lens");
                public static readonly GUIContent focalLength = EditorGUIUtility.TrTextContent("Focal Length", "The simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.");
                public static readonly GUIContent shift = EditorGUIUtility.TrTextContent("Shift", "Offset from the camera sensor. Use these properties to simulate a shift lens. Measured as a multiple of the sensor size.");
                public static readonly GUIContent aperture = EditorGUIUtility.TrTextContent("Aperture", "The f-stop (f-number) of the lens. Lower values give a wider lens aperture.");

                // Aperture Shape
                public static readonly GUIContent apertureShape = EditorGUIUtility.TrTextContent("Aperture Shape", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");
                public static readonly GUIContent bladeCount = EditorGUIUtility.TrTextContent("Blade Count", "The number of blades in the lens aperture. Higher values give a rounder aperture shape.");
                public static readonly GUIContent curvature = EditorGUIUtility.TrTextContent("Curvature", "Controls the curvature of the lens aperture blades. The minimum value results in fully-curved, perfectly-circular bokeh, and the maximum value results in visible aperture blades.");
                public static readonly GUIContent barrelClipping = EditorGUIUtility.TrTextContent("Barrel Clipping", "Controls the self-occlusion of the lens, creating a cat's eye effect.");
                public static readonly GUIContent anamorphism = EditorGUIUtility.TrTextContent("Anamorphism", "Use the slider to stretch the sensor to simulate an anamorphic look.");
            }
        }
    }
}
