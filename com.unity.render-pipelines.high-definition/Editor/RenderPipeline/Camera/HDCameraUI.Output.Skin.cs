using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        partial class Output
        {
            class Styles
            {
                public static readonly GUIContent header = EditorGUIUtility.TrTextContent("Output", "These settings control how the camera output is formatted.");

#if ENABLE_MULTIPLE_DISPLAYS
                public static readonly GUIContent targetDisplay = EditorGUIUtility.TrTextContent("Target Display");
#endif

#if ENABLE_VR && ENABLE_XR_MANAGEMENT
                public static readonly GUIContent xrRenderingContent = EditorGUIUtility.TrTextContent("XR Rendering");
#endif

                public static readonly GUIContent depth = EditorGUIUtility.TrTextContent("Depth");
                public static readonly GUIContent viewport = EditorGUIUtility.TrTextContent("Viewport Rect", "Four values that indicate where on the screen HDRP draws this Camera view. Measured in Viewport Coordinates (values in the range of [0, 1]).");
                public static readonly GUIContent allowDynamicResolution = EditorGUIUtility.TrTextContent("Allow Dynamic Resolution", "Whether to support dynamic resolution.");

                public const string msaaWarningMessage = "Manual MSAA target set with deferred rendering. This will lead to undefined behavior.";
            }
        }
    }
}
