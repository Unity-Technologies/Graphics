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
                public static readonly GUIContent header = EditorGUIUtility.TrTextContent("Output", "These settings control how the camera output is formatted.");

#if ENABLE_MULTIPLE_DISPLAYS
                /// <summary>
                /// Target display content
                /// </summary>
                public static readonly GUIContent targetDisplay = EditorGUIUtility.TrTextContent("Target Display");
#endif

                /// <summary>
                /// Viewport
                /// </summary>
                public static readonly GUIContent viewport = EditorGUIUtility.TrTextContent("Viewport Rect", "Four values that indicate where on the screen HDRP draws this Camera view. Measured in Viewport Coordinates (values in the range of [0, 1]).");

                /// <summary>
                /// Allow dynamic resolution content
                /// </summary>
                public static readonly GUIContent allowDynamicResolution = EditorGUIUtility.TrTextContent("Allow Dynamic Resolution", "Whether to support dynamic resolution.");

                /// <summary>
                /// Depth content
                /// </summary>
                public static readonly GUIContent depth = EditorGUIUtility.TrTextContent("Depth");
            }
        }
    }
}
