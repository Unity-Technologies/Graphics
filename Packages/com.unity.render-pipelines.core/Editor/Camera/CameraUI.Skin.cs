using UnityEngine;

namespace UnityEditor.Rendering
{
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
            public static GUIContent projectionSettingsHeaderContent { get; } = L10n.TextContent("Projection", null, null, null);

            /// <summary>
            /// Clipping planes content
            /// </summary>
            public static GUIContent clippingPlaneMultiFieldTitle = L10n.TextContent("Clipping Planes", null, null, null);

            /// <summary>
            /// Projection Content
            /// </summary>
            public static readonly GUIContent projectionContent = L10n.TextContent("Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.", null, null);

            /// <summary>
            /// Size content
            /// </summary>
            public static readonly GUIContent sizeContent = L10n.TextContent("Size", null, null, null);

            /// <summary>
            /// FOV content
            /// </summary>
            public static readonly GUIContent fieldOfViewContent = L10n.TextContent("Field of View", "The height of the Camera's view angle, measured in degrees along the specified axis.", null, null);

            /// <summary>
            /// FOV Axis content
            /// </summary>
            public static readonly GUIContent FOVAxisModeContent = L10n.TextContent("Field of View Axis", "The axis the Camera's view angle is measured along.", null, null);

            /// <summary>
            /// Physical camera content
            /// </summary>
            public static readonly GUIContent physicalCameraContent = L10n.TextContent("Physical Camera", "Enables Physical camera mode for FOV calculation. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift).", null, null);

            /// <summary>
            /// Near plane content
            /// </summary>
            public static readonly GUIContent nearPlaneContent = L10n.TextContent("Near", "The closest point relative to the camera that drawing occurs.", null, null);

            /// <summary>
            /// Far plane content
            /// </summary>
            public static readonly GUIContent farPlaneContent = L10n.TextContent("Far", "The furthest point relative to the camera that drawing occurs.", null, null);

            /// <summary>
            /// Message displayed about unsupported fields for Camera Presets
            /// </summary>
            public static readonly string unsupportedPresetPropertiesMessage = L10n.Tr("When using Preset of Camera Component, only a subset of properties are supported.  Unsupported properties are hidden.", null);
        }
    }
}
