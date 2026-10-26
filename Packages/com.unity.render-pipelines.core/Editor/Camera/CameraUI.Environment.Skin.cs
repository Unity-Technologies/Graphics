using UnityEngine;

namespace UnityEditor.Rendering
{
    public static partial class CameraUI
    {
        public static partial class Environment
        {
            /// <summary>
            /// Styles
            /// </summary>
            public static class Styles
            {
                /// <summary>
                /// Header of the section
                /// </summary>
                public static readonly GUIContent header = L10n.TextContent("Environment", "These settings control what the camera background looks like.", null, null);

                /// <summary>
                /// Volume layer mask content
                /// </summary>
                public static readonly GUIContent volumeLayerMask = L10n.TextContent("Volume Mask", "This camera will only be affected by volumes in the selected scene-layers.", null, null);
            }
        }
    }
}
