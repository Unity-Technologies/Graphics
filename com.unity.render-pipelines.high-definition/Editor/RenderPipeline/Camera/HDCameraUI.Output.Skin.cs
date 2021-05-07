using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        partial class Output
        {
            class Styles
            {
#if ENABLE_MULTIPLE_DISPLAYS
                public static readonly GUIContent targetDisplay = EditorGUIUtility.TrTextContent("Target Display");
#endif

#if ENABLE_VR && ENABLE_XR_MANAGEMENT
                public static readonly GUIContent xrRenderingContent = EditorGUIUtility.TrTextContent("XR Rendering");
#endif

                public const string msaaWarningMessage = "Manual MSAA target set with deferred rendering. This will lead to undefined behavior.";
            }
        }
    }
}
