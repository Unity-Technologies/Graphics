using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        partial class Environment
        {
            class Styles
            {
                public static readonly GUIContent backgroundType = EditorGUIUtility.TrTextContent("Background Type", "Specifies the type of background the Camera applies when it clears the screen before rendering a frame. Be aware that when setting this to None, the background is never cleared and since HDRP shares render texture between cameras, you may end up with garbage from previous rendering.");
                public static readonly GUIContent backgroundColor = EditorGUIUtility.TrTextContent("Background Color", "The Background Color used to clear the screen when selecting Background Color before rendering.");
                public static readonly GUIContent volumeAnchorOverride = EditorGUIUtility.TrTextContent("Volume Anchor Override");
                public static readonly GUIContent probeLayerMask = EditorGUIUtility.TrTextContent("Probe Layer Mask", "The layer mask to use to cull probe influences.");
            }
        }
    }
}
