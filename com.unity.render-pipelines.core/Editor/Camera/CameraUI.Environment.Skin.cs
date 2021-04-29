using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary> Camera UI Shared Properties among SRP</summary>
    public static partial class CameraUI
    {
        /// <summary>
        /// Environment section
        /// </summary>
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
                public static readonly GUIContent header = EditorGUIUtility.TrTextContent("Environment", "These settings control what the camera background looks like.");

                /// <summary>
                /// Volume layer mask content
                /// </summary>
                public static readonly GUIContent volumeLayerMask = EditorGUIUtility.TrTextContent("Volume Mask", "This camera will only be affected by volumes in the selected scene-layers.");
            }
        }
    }
}
