using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary> Camera UI Shared Properties among SRP</summary>
    public static partial class CameraUI
    {
        /// <summary>
        /// Styles
        /// </summary>
        public static class Styles
        {
            /// <summary>
            /// Projection section header
            /// </summary>
            public static GUIContent projectionSettingsHeaderContent { get; } = EditorGUIUtility.TrTextContent("Projection");

            /// <summary>
            /// Clipping planes content
            /// </summary>
            public static GUIContent clippingPlaneMultiFieldTitle = EditorGUIUtility.TrTextContent("Clipping Planes");

            /// <summary>
            /// Projection Content
            /// </summary>
            public static readonly GUIContent projectionContent = EditorGUIUtility.TrTextContent("Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.");

            /// <summary>
            /// Size content
            /// </summary>
            public static readonly GUIContent sizeContent = EditorGUIUtility.TrTextContent("Size");

            /// <summary>
            /// FOV content
            /// </summary>
            public static readonly GUIContent fieldOfViewContent = EditorGUIUtility.TrTextContent("Field of View", "The height of the Camera's view angle, measured in degrees along the specified axis.");

            /// <summary>
            /// FOV Axis content
            /// </summary>
            public static readonly GUIContent FOVAxisModeContent = EditorGUIUtility.TrTextContent("Field of View Axis", "The axis the Camera's view angle is measured along.");

            /// <summary>
            /// Physical camera content
            /// </summary>
            public static readonly GUIContent physicalCameraContent = EditorGUIUtility.TrTextContent("Physical Camera", "Enables Physical camera mode for FOV calculation. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift).");

            /// <summary>
            /// Near plane content
            /// </summary>
            public static readonly GUIContent nearPlaneContent = EditorGUIUtility.TrTextContent("Near", "The closest point relative to the camera that drawing occurs.");

            /// <summary>
            /// Far plane content
            /// </summary>
            public static readonly GUIContent farPlaneContent = EditorGUIUtility.TrTextContent("Far", "The furthest point relative to the camera that drawing occurs.");

            /// <summary>
            /// Message displayed about unsupported fields for Camera Presets
            /// </summary>
            public static readonly string unsupportedPresetPropertiesMessage = L10n.Tr("When using Preset of Camera Component, only a subset of properties are supported.  Unsupported properties are hidden.");
        }
    }
}
