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
                public static readonly GUIContent targetDisplay = L10n.TextContent("Target Display", null, null, null);
#endif

#if ENABLE_VR && ENABLE_XR_MANAGEMENT
                public static readonly GUIContent xrRenderingContent = L10n.TextContent("XR Rendering", null, null, null);
#endif

                public const string msaaWarningMessage = "Manual MSAA target set with deferred rendering. This will lead to undefined behavior.";
            }
        }
    }
}
