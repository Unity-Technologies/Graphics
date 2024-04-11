using UnityEngine;

namespace UnityEditor.Rendering
{
    public static partial class CameraUI
    {
        /// <summary>
        /// Rendering section
        /// </summary>
        public static partial class Rendering
        {
            /// <summary>
            /// Styles
            /// </summary>
            public static class Styles
            {
                /// <summary>
                /// Header of the section
                /// </summary>
                public static readonly GUIContent header = EditorGUIUtility.TrTextContent("Rendering", "These settings control for the specific rendering features for this camera.");

                /// <summary>
                /// antialiasing content
                /// </summary>
                public static readonly GUIContent antialiasing = EditorGUIUtility.TrTextContent("Post Anti-aliasing", "The postprocess anti-aliasing method to use.");

                /// <summary>
                /// dithering content
                /// </summary>
                public static readonly GUIContent dithering = EditorGUIUtility.TrTextContent("Dithering", "Applies 8-bit dithering to the final render to reduce color banding.");

                /// <summary>
                /// stopNaNs content
                /// </summary>
                public static readonly GUIContent stopNaNs = EditorGUIUtility.TrTextContent("Stop NaNs", "Automatically replaces NaN/Inf in shaders by a black pixel to avoid breaking some effects. This will slightly affect performances and should only be used if you experience NaN issues that you can't fix.");

                /// <summary>
                /// cullingMask content
                /// </summary>
                public static readonly GUIContent cullingMask = EditorGUIUtility.TrTextContent("Culling Mask");

                /// <summary>
                /// occlusionCulling content
                /// </summary>
                public static readonly GUIContent occlusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling");

                /// <summary>
                /// renderingPath content
                /// </summary>
                public static readonly GUIContent renderingPath = EditorGUIUtility.TrTextContent("Custom Frame Settings", "Define the custom Frame Settings for this Camera to use.");

                /// <summary>
                /// exposureTarget content
                /// </summary>
                public static readonly GUIContent exposureTarget = EditorGUIUtility.TrTextContent("Exposure Target", "The object used as a target for centering the Exposure's Procedural Mask metering mode when target object option is set (See Exposure Volume Component).");
            }
        }
    }
}
