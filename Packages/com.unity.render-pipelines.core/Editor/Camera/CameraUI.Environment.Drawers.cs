namespace UnityEditor.Rendering
{
    public static partial class CameraUI
    {
        /// <summary>
        /// Environment Section
        /// </summary>
        public static partial class Environment
        {
            /// <summary>Draws layer mask planes related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_Environment_VolumeLayerMask(ISerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.volumeLayerMask, Styles.volumeLayerMask);
            }
        }
    }
}
