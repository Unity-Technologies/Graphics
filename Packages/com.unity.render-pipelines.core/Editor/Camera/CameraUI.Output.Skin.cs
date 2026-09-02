using UnityEngine;

namespace UnityEditor.Rendering
{
    public static partial class CameraUI
    {
        public static partial class Output
        {
            /// <summary>
            /// Styles
            /// </summary>
            public static class Styles
            {
                /// <summary>
                /// Header of the section
                /// </summary>
                public static readonly GUIContent header = L10n.TextContent("Output", "These settings control how the camera output is formatted.", null, null);

#if ENABLE_MULTIPLE_DISPLAYS
                /// <summary>
                /// Target display content
                /// </summary>
                public static readonly GUIContent targetDisplay = EditorGUIUtility.TrTextContent("Target Display");
#endif

                /// <summary>
                /// Viewport
                /// </summary>
                public static readonly GUIContent viewport = L10n.TextContent("Viewport Rect", "Four values that indicate where on the screen HDRP draws this Camera view. Measured in Viewport Coordinates (values in the range of [0, 1]).", null, null);

                /// <summary>
                /// Allow dynamic resolution content
                /// </summary>
                public static readonly GUIContent allowDynamicResolution = L10n.TextContent("Allow Dynamic Resolution", "Whether to support dynamic resolution.", null, null);

                /// <summary>
                /// Depth content
                /// </summary>
                public static readonly GUIContent depth = L10n.TextContent("Depth", null, null, null);
            }
        }
    }
}
