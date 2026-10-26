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
                public static readonly GUIContent header = L10n.TextContent("Rendering", "These settings control for the specific rendering features for this camera.", null, null);

                /// <summary>
                /// antialiasing content
                /// </summary>
                public static readonly GUIContent antialiasing = L10n.TextContent("Post Anti-aliasing", "The postprocess anti-aliasing method to use.", null, null);

                /// <summary>
                /// dithering content
                /// </summary>
                public static readonly GUIContent dithering = L10n.TextContent("Dithering", "Applies 8-bit dithering to the final render to reduce color banding.", null, null);

                /// <summary>
                /// stopNaNs content
                /// </summary>
                public static readonly GUIContent stopNaNs = L10n.TextContent("Stop NaNs", "Automatically replaces NaN/Inf in shaders by a black pixel to avoid breaking some effects. This will slightly affect performances and should only be used if you experience NaN issues that you can't fix.", null, null);

                /// <summary>
                /// cullingMask content
                /// </summary>
                public static readonly GUIContent cullingMask = L10n.TextContent("Culling Mask", null, null, null);

                /// <summary>
                /// occlusionCulling content
                /// </summary>
                public static readonly GUIContent occlusionCulling = L10n.TextContent("Occlusion Culling", null, null, null);

                /// <summary>
                /// renderingPath content
                /// </summary>
                public static readonly GUIContent renderingPath = L10n.TextContent("Custom Frame Settings", "Define the custom Frame Settings for this Camera to use.", null, null);

                /// <summary>
                /// exposureTarget content
                /// </summary>
                public static readonly GUIContent exposureTarget = L10n.TextContent("Exposure Target", "The object used as a target for centering the Exposure's Procedural Mask metering mode when target object option is set (See Exposure Volume Component).", null, null);
            }
        }
    }
}
