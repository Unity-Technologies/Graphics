using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        class Styles
        {
            public static GUIContent projectionSettingsHeaderContent { get; } = EditorGUIUtility.TrTextContent("Projection");

            public static GUIContent clippingPlaneMultiFieldTitle = EditorGUIUtility.TrTextContent("Clipping Planes");

            public static readonly GUIContent projectionContent = EditorGUIUtility.TrTextContent("Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.");
            public static readonly GUIContent sizeContent = EditorGUIUtility.TrTextContent("Size");
            public static readonly GUIContent fieldOfViewContent = EditorGUIUtility.TrTextContent("Field of View", "The height of the Camera’s view angle, measured in degrees along the local Y axis.");
            public static readonly GUIContent FOVAxisModeContent = EditorGUIUtility.TrTextContent("Field of View Axis", "Field of view axis.");
            public static readonly GUIContent physicalCameraContent = EditorGUIUtility.TrTextContent("Physical Camera", "Enables Physical camera mode for FOV calculation. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift).");
            public static readonly GUIContent nearPlaneContent = EditorGUIUtility.TrTextContent("Near", "The closest point relative to the camera that drawing occurs.");
            public static readonly GUIContent farPlaneContent = EditorGUIUtility.TrTextContent("Far", "The furthest point relative to the camera that drawing occurs.");
        }
    }
}
